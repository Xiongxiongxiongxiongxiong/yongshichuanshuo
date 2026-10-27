using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ValidateDesertDungeonAbilities
{
    static PlayerController player;static DesertDungeonEnemy owner;static DesertDungeonProjectile projectile;
    static int stage,trial;static float start,oldX,maxFeet;static bool warningChecked;static GameObject wall;
    static readonly List<string> log=new List<string>();
    static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static readonly int[] Kinds={0,1,2,3,4,5,6,6,7,7,7};
    static readonly DesertDungeonEnemy.Ability[] Abilities={DesertDungeonEnemy.Ability.Basic,DesertDungeonEnemy.Ability.Basic,DesertDungeonEnemy.Ability.Basic,DesertDungeonEnemy.Ability.Basic,DesertDungeonEnemy.Ability.Basic,DesertDungeonEnemy.Ability.Basic,DesertDungeonEnemy.Ability.Main,DesertDungeonEnemy.Ability.Secondary,DesertDungeonEnemy.Ability.Main,DesertDungeonEnemy.Ability.Main,DesertDungeonEnemy.Ability.Secondary};
    static float Age=>Time.time-start;
    static float Damage=>10000-player.GetComponent<Character>().CurrentHp;
    public static string Start(){Application.runInBackground=true;stage=trial=0;log.Clear();SessionState.SetString("DDEAbilityValidation","Running");EditorApplication.update-=Tick;EditorApplication.update+=Tick;return "Controlled ability and safe-pull tests running.";}
    static void Tick(){try{Run();}catch(Exception e){log.Add("FAILED "+e);Finish();}}
    static void Run()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;return;}
        if(stage==0)
        {
            if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene_19").isLoaded)return;
            player=UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();
            var f=new GameObject("TEMP Ability Floor",typeof(BoxCollider2D));f.layer=6;f.transform.position=new Vector3(300,0,0);f.GetComponent<BoxCollider2D>().size=new Vector2(70,1);
            SetupAttack();stage=1;return;
        }
        if(stage==1)
        {
            if(!warningChecked&&Age>.35f){log.Add(owner.kind+" "+owner.CurrentAbility+" warning damage="+Damage);warningChecked=true;}
            // Disable damage cooldowns to exercise shared per-cast victim accounting.
            player.GetComponent<Character>().invulnerable=false;
            if(Age<2.9f)return;
            log.Add(owner.kind+" "+owner.CurrentAbility+" trial="+trial+" totalDamage="+Damage);
            if(trial==10){var pull=player.GetComponent<PlayerSoulPull>();log.Add("Chain pull distance="+(pull!=null?pull.DistancePulled:0)+" actual="+Mathf.Abs(player.transform.position.x-oldX)+" inputEnabled="+player.inputcontroller.GamePlayer.Move.enabled);}
            UnityEngine.Object.Destroy(owner.gameObject);
            if(++trial<Kinds.Length){SetupAttack();return;}
            Spawn(7,new Vector2(280,.54f));Put(305);start=Time.time;stage=2;return;
        }
        if(stage==2)
        {
            if(Age<.2f)return;Fire(DesertDungeonProjectile.Style.SoulWave,302,.54f,12);maxFeet=0;start=Time.time;stage=3;return;
        }
        if(stage==3)
        {
            InputSystem.QueueStateEvent(Keyboard.current,Age<.16f?new KeyboardState(Key.Space):new KeyboardState());maxFeet=Mathf.Max(maxFeet,player.GetComponent<CapsuleCollider2D>().bounds.min.y);
            if(Age<1.1f)return;log.Add("Actual jump feet="+maxFeet+" wave avoided="+(Damage==0));trial=0;Put(305);start=Time.time;stage=4;return;
        }
        if(stage==4)
        {
            if(Age<.2f)return;player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;player.GetComponent<SpriteRenderer>().flipX=true;player.GetComponent<PhysicsCheck>().RefreshChecks();
            bool started=player.GetComponent<PlayerSlide>().TryStartSlide();var style=(DesertDungeonProjectile.Style)trial;Fire(style,302,trial==2||trial==3?.54f:1.1f,trial==0?7:trial==1?9:trial==4?8:12);
            log.Add(style+" slide started="+started);start=Time.time;stage=5;return;
        }
        if(stage==5)
        {
            if(Age<.75f)return;log.Add((DesertDungeonProjectile.Style)trial+" slide damage="+Damage);if(projectile!=null)UnityEngine.Object.Destroy(projectile.gameObject);
            if(++trial<5){Put(305);start=Time.time;stage=4;return;}
            Put(300);player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;owner.transform.position=new Vector3(310,.54f,0);BeginPull();stage=6;return;
        }
        if(stage==6)
        {
            if(Age<.55f)return;log.Add("Free pull distance="+player.GetComponent<PlayerSoulPull>().DistancePulled+" actual="+Mathf.Abs(player.transform.position.x-oldX));
            Put(300);player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;
            wall=new GameObject("TEMP Pull Wall",typeof(BoxCollider2D));wall.layer=6;wall.transform.position=new Vector3(300.9f,2,0);wall.GetComponent<BoxCollider2D>().size=new Vector2(.3f,4);Physics2D.SyncTransforms();BeginPull();stage=7;return;
        }
        if(stage==7)
        {
            if(Age<.55f)return;log.Add("Pull stops before wall="+(player.transform.position.x<300.7f)+" distance="+player.GetComponent<PlayerSoulPull>().DistancePulled);UnityEngine.Object.Destroy(wall);
            var cliff=new GameObject("TEMP Pull Cliff",typeof(BoxCollider2D));cliff.layer=6;cliff.transform.position=new Vector3(360,0,0);cliff.GetComponent<BoxCollider2D>().size=new Vector2(6,1);
            Put(362.5f);player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;owner.transform.position=new Vector3(370,.54f,0);BeginPull();stage=8;return;
        }
        if(stage==8)
        {
            if(Age<.55f)return;log.Add("Pull stops before ledge="+(player.transform.position.x<362.85f)+" x="+player.transform.position.x);UnityEngine.Object.Destroy(owner.gameObject);
            Spawn(6,new Vector2(300,.54f));Put(304);wall=new GameObject("TEMP Charge Wall",typeof(BoxCollider2D));wall.layer=6;wall.transform.position=new Vector3(303,2,0);wall.GetComponent<BoxCollider2D>().size=new Vector2(.3f,4);AimAndBegin(DesertDungeonEnemy.Ability.Main);start=Time.time;stage=9;return;
        }
        if(stage==9)
        {
            if(Age<1.2f)return;log.Add("Charge wall stun="+(owner.State==DesertDungeonEnemy.ActionState.Stunned));UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(owner.gameObject);
            Spawn(6,new Vector2(362,.54f));Put(366);player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;AimAndBegin(DesertDungeonEnemy.Ability.Main);start=Time.time;stage=10;return;
        }
        if(stage==10)
        {
            if(Age<1.5f)return;log.Add("Charge cliff safety="+(owner.transform.position.x<362.3f&&owner.transform.position.y>.3f));UnityEngine.Object.Destroy(owner.gameObject);
            Spawn(7,new Vector2(300,.54f));Put(310);Fire(DesertDungeonProjectile.Style.SoulFire,301,1.1f,9);projectile.maxRange=1;start=Time.time;stage=11;return;
        }
        if(stage==11)
        {
            if(Age<.4f)return;log.Add("Projectile expires at range="+(projectile==null));
            wall=new GameObject("TEMP Projectile Wall",typeof(BoxCollider2D));wall.layer=6;wall.transform.position=new Vector3(303,2,0);wall.GetComponent<BoxCollider2D>().size=new Vector2(.3f,4);Fire(DesertDungeonProjectile.Style.SoulChain,301,1.1f,8);start=Time.time;stage=12;return;
        }
        if(stage==12)
        {
            if(Age<.6f)return;log.Add("Projectile stops at wall="+(projectile==null));Finish();
        }
    }
    static void Put(float x)
    {
        var pull=player.GetComponent<PlayerSoulPull>();if(pull!=null)UnityEngine.Object.DestroyImmediate(pull);
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());player.ResetForNewGame();var hp=player.GetComponent<Character>();hp.maxHp=hp.CurrentHp=10000;hp.invulnerable=false;
        player.transform.position=new Vector3(x,.55f,0);var rb=player.GetComponent<Rigidbody2D>();rb.velocity=Vector2.zero;rb.constraints=RigidbodyConstraints2D.FreezeRotation|RigidbodyConstraints2D.FreezePositionX;Physics2D.SyncTransforms();
    }
    static void Spawn(int kind,Vector2 position)
    {
        owner=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildDesertDungeonEnemies.Prefabs+"/"+BuildDesertDungeonEnemies.Names[kind]+".prefab"),position,Quaternion.identity).GetComponent<DesertDungeonEnemy>();owner.detectionRadius=0;owner.patrolRadius=0;owner.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;
    }
    static void AimAndBegin(DesertDungeonEnemy.Ability ability)
    {
        typeof(DesertDungeonEnemy).GetField("target",Private).SetValue(owner,player);typeof(DesertDungeonEnemy).GetField("targetBody",Private).SetValue(owner,player.GetComponent<CapsuleCollider2D>());
        typeof(DesertDungeonEnemy).GetMethod("BeginAttack",Private).Invoke(owner,new object[]{ability});
    }
    static void SetupAttack()
    {
        Spawn(Kinds[trial],new Vector2(300,Kinds[trial]==4?1.5f:.54f));float x=trial==0||trial==3?301.5f:trial==8?301.5f:trial==9?303.5f:302.5f;Put(x);
        if(trial==10)player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;
        oldX=x;AimAndBegin(Abilities[trial]);warningChecked=false;start=Time.time;
    }
    static void Fire(DesertDungeonProjectile.Style style,float x,float y,int amount)
    {
        var go=new GameObject("TEMP Ability Projectile",typeof(SpriteRenderer));go.transform.position=new Vector3(x,y,0);var damage=go.AddComponent<Attack>();damage.enabled=false;damage.damage=amount;
        projectile=go.AddComponent<DesertDungeonProjectile>();projectile.owner=owner;projectile.frames=owner.effectFrames;projectile.style=style;projectile.damageSource=damage;projectile.velocity=Vector2.right*6;projectile.maxRange=5;projectile.victims=new HashSet<Character>();
    }
    static void BeginPull(){var p=player.GetComponent<PlayerSoulPull>();if(p==null)p=player.gameObject.AddComponent<PlayerSoulPull>();p.Begin(owner.transform,1<<6);oldX=player.transform.position.x;start=Time.time;}
    static void Finish(){EditorApplication.update-=Tick;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());SessionState.SetString("DDEAbilityValidation",string.Join("\n",log.ToArray()));}
}
