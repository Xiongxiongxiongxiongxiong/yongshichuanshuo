using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ValidateDesertDungeonEnemies
{
    static PlayerController player;
    static DesertDungeonEnemy enemy;
    static Attack probe;
    static int stage,index,side;
    static float start,before;
    static bool flash,lastFrame,mainArmor,secondaryArmor;
    static readonly HashSet<string> states=new HashSet<string>();
    static readonly List<string> results=new List<string>();
    static float Age=>Time.time-start;
    public static string Start()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play persistent first.");
        Application.runInBackground=true;stage=index=side=0;results.Clear();
        SessionState.SetString("DesertDungeonValidation","Running");EditorApplication.update-=Tick;EditorApplication.update+=Tick;return "Eight enemy AI, real J in both directions, flash/bar, death and elite skill armor tests scheduled.";
    }
    static void Tick(){try{Run();}catch(Exception e){results.Add("FAILED "+e);Finish();}}
    static void Run()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;return;}
        if(stage==0)
        {
            if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene_19").isLoaded)return;
            player=UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();
            var floor=new GameObject("TEMP Eight Enemy Floor",typeof(BoxCollider2D));floor.layer=6;floor.transform.position=new Vector3(220,0,0);floor.GetComponent<BoxCollider2D>().size=new Vector2(60,1);
            probe=new GameObject("TEMP Enemy Probe").AddComponent<Attack>();probe.enabled=false;probe.damage=5;SetupAI();stage=1;return;
        }
        if(stage==1||stage==2)
        {
            states.Add(enemy.CurrentAbility+"/"+enemy.State);
            if(enemy.IsElite&&enemy.State==DesertDungeonEnemy.ActionState.Windup&&enemy.CurrentAbility!=DesertDungeonEnemy.Ability.Basic)
            {
                bool main=enemy.CurrentAbility==DesertDungeonEnemy.Ability.Main;
                if(main?!mainArmor:!secondaryArmor)
                {
                    var hp=enemy.GetComponent<Character>();hp.invulnerable=false;float old=hp.CurrentHp;var state=enemy.State;hp.TakeDamage(probe);
                    results.Add(enemy.kind+" "+enemy.CurrentAbility+" damaged="+(old-hp.CurrentHp)+" armor="+(enemy.State==state));if(main)mainArmor=true;else secondaryArmor=true;
                }
            }
            if(Age<(enemy.IsElite?stage==1?5.5f:5f:4.5f))return;
            if(stage==1&&enemy.IsElite){enemy.GetComponent<Character>().CurrentHp=500;start=Time.time;stage=2;return;}
            results.Add(enemy.kind+" AI="+string.Join(",",states.ToArray())+" playerDamage="+(10000-player.GetComponent<Character>().CurrentHp));
            enemy.detectionRadius=0;side=0;SetupJ();stage=3;return;
        }
        if(stage==3)
        {
            InputSystem.QueueStateEvent(Keyboard.current,Age<.16f?new KeyboardState(Key.J):new KeyboardState());
            flash|=enemy.GetComponent<SpriteRenderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");
            if(Age<1.1f)return;
            float bar=enemy.GetComponentsInChildren<UnityEngine.UI.Image>().First(i=>i.name=="Health").fillAmount;
            results.Add(enemy.kind+" J "+side+" damage="+(before-enemy.GetComponent<Character>().CurrentHp)+" flash="+flash+" bar="+(bar<1));
            if(side++==0){SetupJ();return;}
            probe.damage=2000;var hp=enemy.GetComponent<Character>();hp.invulnerable=false;hp.TakeDamage(probe);results.Add(enemy.kind+" death collider off="+!enemy.GetComponent<Collider2D>().enabled);
            start=Time.time;lastFrame=false;stage=4;return;
        }
        if(stage==4)
        {
            if(enemy!=null)lastFrame|=enemy.GetComponent<SpriteRenderer>().sprite==enemy.frames[enemy.frames.Length-1];
            if(Age<1.5f)return;results.Add("Death final frame="+lastFrame+" removed="+(enemy==null));
            if(++index<8){SetupAI();stage=1;}else Finish();
        }
    }
    static void Put(float x)
    {
        player.ResetForNewGame();var hp=player.GetComponent<Character>();hp.maxHp=hp.CurrentHp=10000;hp.invulnerable=false;
        player.transform.position=new Vector3(x,.55f,0);var b=player.GetComponent<Rigidbody2D>();b.velocity=Vector2.zero;b.constraints=RigidbodyConstraints2D.FreezeRotation|RigidbodyConstraints2D.FreezePositionX;Physics2D.SyncTransforms();
    }
    static void SetupAI()
    {
        enemy=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildDesertDungeonEnemies.Prefabs+"/"+BuildDesertDungeonEnemies.Names[index]+".prefab"),new Vector3(220,index==4?1.5f:.54f,0),Quaternion.identity).GetComponent<DesertDungeonEnemy>();
        enemy.GetComponent<Character>().maxHp=enemy.GetComponent<Character>().CurrentHp=1000;enemy.GetComponent<Character>().OnHealthChange.Invoke(enemy.GetComponent<Character>());
        Put(222);player.inputcontroller.GamePlayer.Disable();probe.damage=5;states.Clear();mainArmor=secondaryArmor=false;start=Time.time;
    }
    static void SetupJ()
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        foreach(var p in UnityEngine.Object.FindObjectsOfType<DesertDungeonProjectile>())if(p.owner==enemy)UnityEngine.Object.Destroy(p.gameObject);
        enemy.transform.position=new Vector3(220,index==4?1.2f:.54f,0);enemy.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
        typeof(DesertDungeonEnemy).GetMethod("SetState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(enemy,new object[]{DesertDungeonEnemy.ActionState.Recover});
        float offset=BuildDesertDungeonEnemies.Sizes[index].x*.5f+.65f;Put(220+(side==0?-offset:offset));player.GetComponent<SpriteRenderer>().flipX=side==1;
        player.GetComponent<Animator>().Play("Base Layer.GreenIde",0,0);player.GetComponent<Animator>().Update(0);
        var hp=enemy.GetComponent<Character>();hp.CurrentHp=hp.maxHp;hp.invulnerable=false;hp.OnHealthChange.Invoke(hp);before=hp.CurrentHp;flash=false;start=Time.time;
    }
    static void Finish(){EditorApplication.update-=Tick;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());SessionState.SetString("DesertDungeonValidation",string.Join("\n",results.ToArray()));}
}
