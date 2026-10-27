using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ValidateCrystalForeman
{
    private static PlayerController player;
    private static CrystalPickaxeForeman elite;
    private static Attack probe;
    private static ForemanCrystalEffect wave;
    private static float start,before,maxFeet;
    private static int stage,side;
    private static bool slamArmor,throwArmor,flash,deathFrame;
    private static readonly HashSet<string> states=new HashSet<string>();
    private static readonly HashSet<int> crystals=new HashSet<int>();
    private static readonly List<string> results=new List<string>();
    private static float Age=>Time.time-start;

    public static string Start()
    {
        if(!Application.isPlaying) throw new InvalidOperationException("Play persistent scene first.");
        EditorApplication.update-=Tick; Application.runInBackground=true;
        results.Clear();states.Clear();crystals.Clear(); stage=side=0;
        slamArmor=throwArmor=false; SessionState.SetString("ForemanValidation","Running");
        EditorApplication.update+=Tick; return "Scheduled elite AI, armor, J, wave/jump and death tests.";
    }
    private static void PutPlayer(float x)
    {
        player.ResetForNewGame(); var hp=player.GetComponent<Character>();hp.maxHp=hp.CurrentHp=10000;
        player.transform.position=new Vector3(x,.55f,0);var rb=player.GetComponent<Rigidbody2D>();
        rb.constraints=RigidbodyConstraints2D.FreezeRotation|RigidbodyConstraints2D.FreezePositionX;rb.velocity=Vector2.zero;
        Physics2D.SyncTransforms();
    }
    private static void Tick()
    {
        try{Run();}catch(Exception error){results.Add("FAILED "+error);Finish();}
    }
    private static void Run()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;return;}
        if(stage==0)
        {
            var loader=UnityEngine.Object.FindObjectOfType<SceneLoader>();
            if(loader==null||!UnityEngine.SceneManagement.SceneManager.GetSceneByName("Menu").isLoaded)return;
            var field=typeof(SceneLoader).GetField("isLoading",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            if((bool)field.GetValue(loader))return;
            loader.LoadEventSO.RaiseLoadRequesEvent(AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene8.asset"),new Vector3(8,7.25f,0),true);
            stage=1;return;
        }
        if(stage==1)
        {
            if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene_8").isLoaded)return;
            player=UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();
            var floor=new GameObject("TEMP Elite Validation Floor",typeof(BoxCollider2D));floor.layer=6;
            floor.transform.position=new Vector3(220,0,0);floor.GetComponent<BoxCollider2D>().size=new Vector2(60,1);
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildCrystalForeman.PrefabPath),new Vector3(220,.54f,0),Quaternion.identity);
            elite=go.GetComponent<CrystalPickaxeForeman>();go.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            var damage=new GameObject("TEMP Player Damage Probe");probe=damage.AddComponent<Attack>();probe.enabled=false;probe.damage=5;
            PutPlayer(221.5f);player.inputcontroller.GamePlayer.Disable();start=Time.time;stage=2;return;
        }
        if(stage==2)
        {
            states.Add(elite.State.ToString());
            foreach(var projectile in UnityEngine.Object.FindObjectsOfType<MineOreProjectile>())
                if(projectile.owner==elite.GetComponent<Character>())crystals.Add(projectile.GetInstanceID());
            bool testSlam=!slamArmor&&elite.State==CrystalPickaxeForeman.CombatState.SlamWindup;
            bool testThrow=!throwArmor&&elite.State==CrystalPickaxeForeman.CombatState.ThrowWindup;
            if(testSlam||testThrow)
            {
                var hp=elite.GetComponent<Character>();hp.invulnerable=false;float old=hp.CurrentHp;
                var state=elite.State;hp.TakeDamage(probe);
                results.Add(state+" damage applied="+(old-hp.CurrentHp)+" not interrupted="+(elite.State==state));
                if(testSlam)slamArmor=true;else throwArmor=true;
            }
            if(Age<12.5f)return;
            if(elite.HasSkillArmor)return;
            results.Add("AI states="+string.Join(",",states.ToArray())+" triple volley count="+crystals.Count+
                " received player damage="+(10000-player.GetComponent<Character>().CurrentHp));
            elite.detectionRadius=0;
            SetupJ();stage=3;return;
        }
        if(stage==3)
        {
            InputSystem.QueueStateEvent(Keyboard.current,Age<.16f?new KeyboardState(Key.J):new KeyboardState());
            flash|=elite.GetComponent<SpriteRenderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");
            if(Age<1.05f)return;
            var hp=elite.GetComponent<Character>();
            float bar=elite.GetComponentsInChildren<UnityEngine.UI.Image>().First(image=>image.name=="Health").fillAmount;
            results.Add("J from "+(side==0?"left":"right")+" damage="+(before-hp.CurrentHp)+" flash="+flash+" barChanged="+(bar<1));
            if(side++==0){SetupJ();return;}
            PutPlayer(225);player.inputcontroller.GamePlayer.Disable();MakeWave();stage=4;return;
        }
        if(stage==4)
        {
            if(Age<1.4f)return;
            results.Add("Ground wave damage="+(10000-player.GetComponent<Character>().CurrentHp));
            PutPlayer(225);MakeWave();maxFeet=0;stage=5;return;
        }
        if(stage==5)
        {
            InputSystem.QueueStateEvent(Keyboard.current,Age<.16f?new KeyboardState(Key.Space):new KeyboardState());
            maxFeet=Mathf.Max(maxFeet,player.GetComponent<CapsuleCollider2D>().bounds.min.y);
            if(Age<1.4f)return;
            results.Add("Actual jump max feet="+maxFeet+" wave avoided="+(player.GetComponent<Character>().CurrentHp==10000));
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            var hp=elite.GetComponent<Character>();hp.invulnerable=false;probe.damage=1000;hp.TakeDamage(probe);
            results.Add("Lethal collider disabled="+!elite.GetComponent<Collider2D>().enabled+
                " effects cleared="+!UnityEngine.Object.FindObjectsOfType<MineOreProjectile>().Any(p=>p.owner==hp));
            start=Time.time;deathFrame=false;stage=6;return;
        }
        if(stage==6)
        {
            if(elite!=null)deathFrame|=elite.GetComponent<SpriteRenderer>().sprite==elite.frames[31];
            if(Age<1.5f)return;
            results.Add("Death last frame="+deathFrame+" destroyed="+(elite==null));Finish();
        }
    }
    private static void SetupJ()
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        PutPlayer(220+(side==0?-1.3f:1.3f));player.GetComponent<SpriteRenderer>().flipX=side==1;
        var animator=player.GetComponent<Animator>();animator.Play("Base Layer.GreenIde",0,0);animator.Update(0);
        elite.GetComponent<Character>().invulnerable=false;before=elite.GetComponent<Character>().CurrentHp;flash=false;start=Time.time;
    }
    private static void MakeWave()
    {
        var go=new GameObject("TEMP Foreman Wave",typeof(SpriteRenderer));go.transform.position=new Vector3(222,.54f,0);
        wave=go.AddComponent<ForemanCrystalEffect>();wave.wave=true;wave.owner=elite.GetComponent<Character>();
        wave.frames=elite.effectFrames;wave.direction=1;wave.groundLayer=1<<6;
        var attack=go.AddComponent<Attack>();attack.enabled=false;attack.damage=14;wave.damage=attack;start=Time.time;
    }
    private static void Finish()
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        SessionState.SetString("ForemanValidation",string.Join("\n",results.ToArray()));EditorApplication.update-=Tick;
    }
}
