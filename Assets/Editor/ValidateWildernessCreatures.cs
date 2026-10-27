using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Exercises the real player input, enemy AI and physics; creates only temporary Play Mode objects.
public static class ValidateWildernessCreatures
{
    private static PlayerController player;
    private static WildernessCaveCreature[] enemies;
    private static WildernessCaveCreature fixture;
    private static readonly List<string> results = new List<string>();
    private static readonly HashSet<string> states = new HashSet<string>();
    private static int stage, species, side;
    private static float start, before, healthBeforeAI;
    private static bool projectileSeen, flashSeen, finalDeathFrame;
    private static Attack lethal;
    private static float Age => Time.time - start;

    public static string Start()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode with persistent scene first.");
        EditorApplication.update -= Tick;
        Application.runInBackground = true; results.Clear(); stage = 0;
        SessionState.SetString("WildernessCaveCreatureValidation", "Waiting for menu, then loading cave15");
        EditorApplication.update += Tick;
        return "Scheduled cave AI, J attacks in both directions, flash, health bar and death checks.";
    }
    private static void AtPlayer(Vector3 position)
    {
        player.ResetForNewGame();
        var hp = player.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = 10000;
        player.transform.position = position;
        var body = player.GetComponent<Rigidbody2D>(); body.velocity = Vector2.zero;
        body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        Physics2D.SyncTransforms();
    }
    private static void GroundPlayer(float x, float rayY)
    {
        var hit = Physics2D.Raycast(new Vector2(x, rayY), Vector2.down, 12, 1 << 6);
        if (!hit) throw new InvalidOperationException("Validation landing missing at " + x);
        AtPlayer(new Vector3(x, hit.point.y + .05f, 0));
    }
    private static void BeginAI(int index, float offset)
    {
        var e = enemies[index];
        GroundPlayer(e.transform.position.x + offset, index == 1 ? 9f : e.transform.position.y + 1.5f);
        player.inputcontroller.GamePlayer.Disable();
        states.Clear(); projectileSeen = false; start = Time.time;
        healthBeforeAI = player.GetComponent<Character>().CurrentHp;
    }
    private static void RecordAI(int index, string name)
    {
        bool cycle = states.Contains("Windup") && states.Contains("Attack");
        results.Add(name + " telegraph/attack=" + cycle + " states=" + string.Join(",", states.ToArray()) +
            " damage=" + (healthBeforeAI - player.GetComponent<Character>().CurrentHp) + " spore=" + projectileSeen);
    }
    private static void Tick()
    {
        try { Run(); }
        catch (Exception error)
        {
            results.Add("FAILED: " + error);
            Finish();
        }
    }
    private static void Run()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (stage == 0)
        {
            var loader = UnityEngine.Object.FindObjectOfType<SceneLoader>();
            if (loader == null || !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Menu").isLoaded) return;
            var loading = typeof(SceneLoader).GetField("isLoading", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if ((bool)loading.GetValue(loader)) return;
            loader.LoadEventSO.RaiseLoadRequesEvent(AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene15.asset"),BuildWildernessCaves.Spawn(15),true);
            stage = 1; return;
        }
        if (stage == 1)
        {
            enemies = UnityEngine.Object.FindObjectsOfType<WildernessCaveCreature>().OrderBy(e => (int)e.species).ToArray();
            if (enemies.Length != 3) return;
            player = UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();
            BeginAI(2, 3.2f); stage = 2; return;
        }
        if (stage >= 2 && stage <= 5)
        {
            int index = stage <= 3 ? 2 : stage == 4 ? 1 : 0;
            states.Add(enemies[index].State.ToString());
            projectileSeen |= UnityEngine.Object.FindObjectsOfType<CaveSporeProjectile>().Any(p => p.owner == enemies[index]);
            if (Age < (stage == 3 ? 2.3f : 4.6f)) return;
            RecordAI(index, stage == 2 ? "Spitter ranged" : stage == 3 ? "Spitter close" : stage == 4 ? "Moth swoop/recover/return" : "Lizard pounce/recover");
            if (stage == 2) BeginAI(2, 1.1f);
            else if (stage == 3) BeginAI(1, -1f);
            else if (stage == 4) BeginAI(0, 2.7f);
            else
            {
                var floor = new GameObject("TEMP Mine Validation Floor", typeof(BoxCollider2D));
                floor.layer = 6; floor.transform.position = new Vector3(210, 0, 0);
                floor.GetComponent<BoxCollider2D>().size = new Vector2(40, 1);
                var kill = new GameObject("TEMP Validation Damage"); lethal = kill.AddComponent<Attack>(); lethal.enabled = false; lethal.damage = 1000;
                species = side = 0; SetupFixture(); stage = 6; return;
            }
            stage++; return;
        }
        if (stage == 6)
        {
            // Hold J long enough to produce the normal Input System press, then release.
            InputSystem.QueueStateEvent(Keyboard.current, Age < .16f ? new KeyboardState(Key.J) : new KeyboardState());
            flashSeen |= fixture.GetComponent<SpriteRenderer>().sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");
            if (Age < 1.05f) return;
            var hp = fixture.GetComponent<Character>();
            var fill = fixture.GetComponentsInChildren<UnityEngine.UI.Image>().First(i => i.name == "Health");
            results.Add(fixture.species + " J from " + (side == 0 ? "left" : "right") +
                " damage=" + (before - hp.CurrentHp) + " whiteFlash=" + flashSeen + " barChanged=" + (fill.fillAmount < 1f));
            if (side == 0) { side = 1; SetupAttack(); return; }
            hp.invulnerable = false; hp.TakeDamage(lethal);
            results.Add(fixture.species + " lethal disables collider=" + !fixture.GetComponent<Collider2D>().enabled);
            start = Time.time; finalDeathFrame = false; stage = 7; return;
        }
        if (stage == 7)
        {
            if (fixture != null) finalDeathFrame |= fixture.GetComponent<SpriteRenderer>().sprite == fixture.frames[23];
            if (Age < 1.35f) return;
            results.Add("Death last frame=" + finalDeathFrame + " destroyed=" + (fixture == null));
            species++; side = 0;
            if (species < 3) { SetupFixture(); stage = 6; }
            else Finish();
        }
    }
    private static void SetupFixture()
    {
        string[] names = BuildWildernessCaveCreatures.Names;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WildernessCaveMonsters/" + names[species] + ".prefab");
        var go = UnityEngine.Object.Instantiate(prefab);
        fixture = go.GetComponent<WildernessCaveCreature>(); fixture.detectionRadius = 0f;
        go.transform.position = new Vector3(210, species == 1 ? 1.2f : .54f, 0);
        go.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var hp = go.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = 100;
        hp.OnHealthChange.Invoke(hp); SetupAttack();
    }
    private static void SetupAttack()
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        AtPlayer(new Vector3(210 + (side == 0 ? -1.15f : 1.15f), .55f, 0));
        player.GetComponent<SpriteRenderer>().flipX = side == 1;
        player.GetComponent<Animator>().Play("Base Layer.GreenIde",0,0);
        player.GetComponent<Animator>().Update(0);
        before = fixture.GetComponent<Character>().CurrentHp; flashSeen = false; start = Time.time;
    }
    private static void Finish()
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        SessionState.SetString("WildernessCaveCreatureValidation",string.Join("\n",results.ToArray()));
        EditorApplication.update -= Tick;
    }
}

