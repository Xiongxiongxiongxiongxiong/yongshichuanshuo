using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class ValidateStonebackCaveBear
{
    static StonebackCaveBear bear;
    static PlayerController player;
    static Attack probe;
    static CaveBearRockWave wave;
    static int stage, side;
    static float start, before, maxFeet, waveDistance;
    static bool armor, flash, deathFrame;
    static readonly HashSet<string> states = new HashSet<string>();
    static readonly List<string> results = new List<string>();
    static readonly BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static float Age => Time.time - start;
    public static string Start()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play persistent first.");
        Application.runInBackground = true; stage = side = 0; armor = false; results.Clear(); states.Clear();
        SessionState.SetString("CaveBearValidation", "Running");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        return "Scheduled real AI, both-side J, half-health defense, shared slam hit, wave jump/slide and death tests.";
    }
    static void Tick() { try { Run(); } catch (Exception e) { results.Add("FAILED " + e); Finish(); } }
    static void Run()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (stage == 0)
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene_15").isLoaded) return;
            player = UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();
            var floor = new GameObject("TEMP Bear Floor", typeof(BoxCollider2D)); floor.layer = 6;
            floor.transform.position = new Vector3(220, 0, 0); floor.GetComponent<BoxCollider2D>().size = new Vector2(50, 1);
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildStonebackCaveBear.PrefabPath), new Vector3(220, .54f, 0), Quaternion.identity);
            bear = go.GetComponent<StonebackCaveBear>(); go.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            probe = new GameObject("TEMP Bear Damage Probe").AddComponent<Attack>(); probe.enabled = false; probe.damage = 5;
            Put(222); player.inputcontroller.GamePlayer.Disable(); start = Time.time; stage = 1; return;
        }
        if (stage == 1)
        {
            states.Add(bear.State.ToString());
            if (!armor && bear.State == StonebackCaveBear.CombatState.SlamWindup)
            {
                var hp = bear.GetComponent<Character>(); before = hp.CurrentHp; hp.invulnerable = false; hp.TakeDamage(probe);
                results.Add("Slam windup accepts damage=" + (before - hp.CurrentHp) + " uninterrupted=" + (bear.State == StonebackCaveBear.CombatState.SlamWindup)); armor = true;
            }
            if (Age < 9 || bear.HasSkillArmor) return;
            results.Add("Real AI states=" + string.Join(",", states.ToArray()) + " player damage=" + PlayerDamage());
            bear.detectionRadius = 0; SetupJ(false); stage = 2; return;
        }
        if (stage == 2 || stage == 6)
        {
            InputSystem.QueueStateEvent(Keyboard.current, Age < .16f ? new KeyboardState(Key.J) : new KeyboardState());
            flash |= bear.GetComponent<SpriteRenderer>().sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");
            if (Age < 1.05f) return;
            float bar = bear.GetComponentsInChildren<UnityEngine.UI.Image>().First(i => i.name == "Health").fillAmount;
            results.Add((stage == 6 ? "Guarded" : "Normal") + " J side=" + side + " damage=" + (before - bear.GetComponent<Character>().CurrentHp) + " flash=" + flash + " bar=" + bar);
            if (side++ == 0) { SetupJ(stage == 6); return; }
            if (stage == 2)
            {
                side = 0; Put(222); player.inputcontroller.GamePlayer.Disable();
                bear.GetComponent<Character>().CurrentHp = 50; bear.detectionRadius = 8; Set(StonebackCaveBear.CombatState.Patrol);
                start = Time.time; stage = 3; return;
            }
            Put(221.8f); player.inputcontroller.GamePlayer.Disable(); bear.detectionRadius = 0;
            bear.GetComponent<Character>().CurrentHp = 100; FaceRight(); Set(StonebackCaveBear.CombatState.SlamWindup);
            start = Time.time; stage = 7; return;
        }
        if (stage == 3)
        {
            if (bear.State != StonebackCaveBear.CombatState.Guard)
            { if (Age > 3) throw new Exception("Half-health guard did not activate"); return; }
            probe.damage = 10; probe.transform.position = new Vector3(222, 1, 0);
            var hp = bear.GetComponent<Character>(); hp.invulnerable = false; before = hp.CurrentHp; hp.TakeDamage(probe);
            results.Add("Half-health front damage=" + (before - hp.CurrentHp));
            Put(218.4f); player.inputcontroller.GamePlayer.Disable(); probe.transform.position = new Vector3(218, 1, 0);
            hp.invulnerable = false; before = hp.CurrentHp; hp.TakeDamage(probe);
            results.Add("Guard rear damage=" + (before - hp.CurrentHp) + " direction remains right=" + (bear.Facing == 1));
            start = Time.time; stage = 4; return;
        }
        if (stage == 4)
        {
            if (Age < 1.6f) return;
            probe.transform.position = new Vector3(222, 1, 0); var hp = bear.GetComponent<Character>(); hp.invulnerable = false; before = hp.CurrentHp; hp.TakeDamage(probe);
            results.Add("Guard ended=" + !bear.GetComponent<DirectionalDamageGuard>().IsGuarding + " rising front damage=" + (before - hp.CurrentHp));
            stage = 5; return;
        }
        if (stage == 5)
        {
            if (Age < 2.7f) return;
            results.Add("Guard on cooldown=" + (bear.GuardCooldownRemaining > 4) + " not repeated=" + (bear.State != StonebackCaveBear.CombatState.Guard));
            side = 0; bear.detectionRadius = 0; SetupJ(true); stage = 6; return;
        }
        if (stage == 7)
        {
            // Force invulnerability off to prove shared hit accounting, not merely a damage cooldown.
            player.GetComponent<Character>().invulnerable = false;
            if (Age < 1.8f) return;
            results.Add("Slam plus wave total=" + PlayerDamage() + " single hit=" + (PlayerDamage() == 20));
            Put(225); player.inputcontroller.GamePlayer.Disable(); MakeWave(); start = Time.time; stage = 8; return;
        }
        if (stage == 8)
        {
            if (wave != null) waveDistance = Mathf.Max(waveDistance, wave.DistanceTravelled);
            if (Age < 1) return;
            results.Add("Ground rock wave damage=" + PlayerDamage() + " range<=3=" + (waveDistance <= 3.01f));
            Put(225); start = Time.time; stage = 9; return;
        }
        if (stage == 9)
        {
            if (Age < .2f) return; MakeWave(); maxFeet = 0; start = Time.time; stage = 10; return;
        }
        if (stage == 10)
        {
            InputSystem.QueueStateEvent(Keyboard.current, Age < .16f ? new KeyboardState(Key.Space) : new KeyboardState());
            maxFeet = Mathf.Max(maxFeet, player.GetComponent<CapsuleCollider2D>().bounds.min.y);
            if (Age < 1.1f) return; results.Add("Actual jump feet=" + maxFeet + " avoids wave=" + (PlayerDamage() == 0));
            Put(225); start = Time.time; stage = 11; return;
        }
        if (stage == 11)
        {
            if (Age < .2f) return; player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            player.GetComponent<SpriteRenderer>().flipX = true; player.GetComponent<PhysicsCheck>().RefreshChecks();
            results.Add("Slide starts=" + player.GetComponent<PlayerSlide>().TryStartSlide()); MakeWave(); start = Time.time; stage = 12; return;
        }
        if (stage == 12)
        {
            if (Age < .7f) return; results.Add("Slide through wave unharmed=" + (PlayerDamage() == 0));
            Put(230); player.inputcontroller.GamePlayer.Disable();
            var wall = new GameObject("TEMP Wave Wall", typeof(BoxCollider2D)); wall.layer = 6; wall.transform.position = new Vector3(224, 2, 0); wall.GetComponent<BoxCollider2D>().size = new Vector2(.3f, 4);
            MakeWave(); start = Time.time; stage = 13; return;
        }
        if (stage == 13)
        {
            if (Age < .6f) return; results.Add("Wave stops at wall=" + (wave == null));
            MakeWave(); Set(StonebackCaveBear.CombatState.Recover); probe.damage = 1000;
            var hp = bear.GetComponent<Character>(); hp.invulnerable = false; hp.TakeDamage(probe);
            results.Add("Death collision off=" + !bear.GetComponent<BoxCollider2D>().enabled); start = Time.time; deathFrame = false; stage = 14; return;
        }
        if (stage == 14)
        {
            if (bear != null) deathFrame |= bear.GetComponent<SpriteRenderer>().sprite == bear.frames[31];
            if (Age < 1.5f) return; results.Add("Death final frame=" + deathFrame + " removed=" + (bear == null) + " wave cleared=" + (wave == null)); Finish();
        }
    }
    static float PlayerDamage() { return 10000 - player.GetComponent<Character>().CurrentHp; }
    static void Put(float x)
    {
        player.ResetForNewGame(); var hp = player.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = 10000; hp.invulnerable = false;
        player.transform.position = new Vector3(x, .55f, 0); var body = player.GetComponent<Rigidbody2D>(); body.velocity = Vector2.zero;
        body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX; Physics2D.SyncTransforms();
    }
    static void FaceRight() { typeof(StonebackCaveBear).GetField("facing", Private).SetValue(bear, 1); bear.GetComponent<SpriteRenderer>().flipX = false; }
    static void Set(StonebackCaveBear.CombatState state) { typeof(StonebackCaveBear).GetMethod("SetState", Private).Invoke(bear, new object[] { state }); }
    static void SetupJ(bool guarded)
    {
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        // The normal test starts on the left; the guard test starts in front (right).
        bool right = guarded ? side == 0 : side == 1;
        Put(220 + (right ? 1.6f : -1.6f)); player.GetComponent<SpriteRenderer>().flipX = right;
        player.GetComponent<Animator>().Play("Base Layer.GreenIde", 0, 0); player.GetComponent<Animator>().Update(0);
        var hp = bear.GetComponent<Character>(); hp.CurrentHp = guarded ? 50 : 100; hp.invulnerable = false; hp.OnHealthChange.Invoke(hp);
        FaceRight(); Set(guarded ? StonebackCaveBear.CombatState.Guard : StonebackCaveBear.CombatState.Recover);
        before = hp.CurrentHp; flash = false; start = Time.time;
    }
    static void MakeWave()
    {
        var go = new GameObject("TEMP Bear Wave", typeof(SpriteRenderer)); go.transform.position = new Vector3(222, .54f, 0);
        var damage = go.AddComponent<Attack>(); damage.enabled = false; damage.damage = 12;
        wave = go.AddComponent<CaveBearRockWave>(); wave.owner = bear; wave.wave = true; wave.frames = bear.effectFrames;
        wave.damageSource = damage; wave.victims = new HashSet<Character>(); waveDistance = 0;
    }
    static void Finish()
    {
        EditorApplication.update -= Tick; InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        SessionState.SetString("CaveBearValidation", string.Join("\n", results.ToArray()));
    }
}
