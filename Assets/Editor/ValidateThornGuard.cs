using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class ValidateThornGuard
{
    static PlayerController player;static ThornAntlerGuard elite;static Attack probe;static ThornRootAmbush root;
    static int stage,side;static float start,before;static bool armorCharge,armorRoot,flash;static readonly List<string> log=new List<string>();static readonly HashSet<string> states=new HashSet<string>();
    static float Age=>Time.time-start;
    public static string Start(){Application.runInBackground=true;stage=side=0;log.Clear();states.Clear();armorCharge=armorRoot=false;EditorApplication.update-=Tick;EditorApplication.update+=Tick;SessionState.SetString("ThornValidation","Running");return "Validation scheduled";}
    static void Put(float x){player.ResetForNewGame();var hp=player.GetComponent<Character>();hp.maxHp=hp.CurrentHp=10000;hp.invulnerable=false;player.transform.position=new Vector3(x,.55f,0);var b=player.GetComponent<Rigidbody2D>();b.velocity=Vector2.zero;b.constraints=RigidbodyConstraints2D.FreezePositionX|RigidbodyConstraints2D.FreezeRotation;Physics2D.SyncTransforms();}
    static void Tick(){try{Run();}catch(Exception e){log.Add("FAILED "+e);Finish();}}
    static void Run()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;return;}
        if(stage==0){var loader=UnityEngine.Object.FindObjectOfType<SceneLoader>();if(loader==null||!UnityEngine.SceneManagement.SceneManager.GetSceneByName("Menu").isLoaded)return;var f=typeof(SceneLoader).GetField("isLoading",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);if((bool)f.GetValue(loader))return;loader.LoadEventSO.RaiseLoadRequesEvent(AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene7.asset"),new Vector3(0,5.05f,0),true);stage=1;return;}
        if(stage==1){if(!UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene_7").isLoaded)return;player=UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();var floor=new GameObject("TEMP Thorn floor",typeof(BoxCollider2D));floor.layer=6;floor.transform.position=new Vector3(220,0,0);floor.GetComponent<BoxCollider2D>().size=new Vector2(60,1);var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildThornAntlerGuard.PrefabPath),new Vector3(220,.54f,0),Quaternion.identity);elite=go.GetComponent<ThornAntlerGuard>();go.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;probe=new GameObject("TEMP probe").AddComponent<Attack>();probe.enabled=false;probe.damage=5;Put(221.5f);player.inputcontroller.GamePlayer.Disable();stage=2;start=Time.time;return;}
        if(stage==2){states.Add(elite.State.ToString());bool c=!armorCharge&&elite.State==ThornAntlerGuard.CombatState.ChargeWindup;bool r=!armorRoot&&elite.State==ThornAntlerGuard.CombatState.RootWindup;if(c||r){var hp=elite.GetComponent<Character>();hp.invulnerable=false;float old=hp.CurrentHp;var state=elite.State;hp.TakeDamage(probe);log.Add(state+" damage="+(old-hp.CurrentHp)+" armor="+(state==elite.State));if(c)armorCharge=true;else armorRoot=true;}if(Age<16||elite.HasSkillArmor)return;log.Add("AI="+string.Join(",",states.ToArray())+" playerDamage="+(10000-player.GetComponent<Character>().CurrentHp));elite.detectionRadius=0;SetupJ();stage=3;return;}
        if(stage==3){InputSystem.QueueStateEvent(Keyboard.current,Age<.16f?new KeyboardState(Key.J):new KeyboardState());flash|=elite.GetComponent<SpriteRenderer>().sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");if(Age<1.05f)return;var hp=elite.GetComponent<Character>();log.Add("J "+side+" damage="+(before-hp.CurrentHp)+" flash="+flash+" bar="+elite.GetComponentsInChildren<UnityEngine.UI.Image>().First(i=>i.name=="Health").fillAmount);if(side++==0){SetupJ();return;}elite.enabled=false;Put(225);player.inputcontroller.GamePlayer.Disable();MakeRoot();stage=4;return;}
        if(stage==4){if(Age<.7f)return;log.Add("Root warning damage="+(10000-player.GetComponent<Character>().CurrentHp));stage=5;return;}
        if(stage==5){if(Age<1.05f)return;log.Add("Root hit damage="+(10000-player.GetComponent<Character>().CurrentHp)+" slow="+player.GetComponent<PlayerRootSlow>().Multiplier);stage=6;return;}
        if(stage==6){if(Age<2.2f)return;log.Add("Slow recovered="+player.GetComponent<PlayerRootSlow>().Multiplier);Put(225);MakeRoot();player.transform.position=new Vector3(228,.55f,0);Physics2D.SyncTransforms();stage=7;return;}
        if(stage==7){if(Age<1.5f)return;log.Add("Leave root warning avoids="+(player.GetComponent<Character>().CurrentHp==10000));Put(225);MakeRoot();root.WarningDuration=.1f;stage=8;return;}
        if(stage==8){if(Age<.04f)return;player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;player.GetComponent<PhysicsCheck>().RefreshChecks();log.Add("Slide started="+player.GetComponent<PlayerSlide>().TryStartSlide());stage=9;return;}
        if(stage==9){if(Age<.7f)return;log.Add("Slide root immunity="+(player.GetComponent<Character>().CurrentHp==10000));elite.enabled=true;Put(210);player.inputcontroller.GamePlayer.Disable();elite.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeRotation;var wall=new GameObject("TEMP wall",typeof(BoxCollider2D));wall.layer=6;wall.transform.position=new Vector3(223.4f,2,0);wall.GetComponent<BoxCollider2D>().size=new Vector2(.3f,4);typeof(ThornAntlerGuard).GetField("facing",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(elite,1);typeof(ThornAntlerGuard).GetMethod("Set",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(elite,new object[]{ThornAntlerGuard.CombatState.Charge});start=Time.time;stage=10;return;}
        if(stage==10){if(Age<.5f)return;log.Add("Charge wall stun="+(elite.State==ThornAntlerGuard.CombatState.Stunned));probe.damage=1000;var hp=elite.GetComponent<Character>();hp.invulnerable=false;hp.TakeDamage(probe);log.Add("Death collision off="+!elite.GetComponent<BoxCollider2D>().enabled);start=Time.time;stage=11;return;}
        if(stage==11){if(Age<1.5f)return;log.Add("Death removed="+(elite==null));Finish();}
    }
    static void SetupJ(){Put(side==0?218.8f:221.2f);player.GetComponent<SpriteRenderer>().flipX=side!=0;var hp=elite.GetComponent<Character>();hp.CurrentHp=hp.maxHp;hp.invulnerable=false;hp.OnHealthChange.Invoke(hp);before=hp.CurrentHp;flash=false;start=Time.time;}
    static void MakeRoot(){root=new GameObject("TEMP root",typeof(SpriteRenderer)).AddComponent<ThornRootAmbush>();root.transform.position=new Vector3(225,.53f,0);root.owner=elite.GetComponent<Character>();root.frames=elite.rootFrames;root.damage=root.gameObject.AddComponent<Attack>();root.damage.enabled=false;root.damage.damage=12;start=Time.time;}
    static void Finish(){EditorApplication.update-=Tick;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());SessionState.SetString("ThornValidation",string.Join("\n",log.ToArray()));}
}

