using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Temporary Play Mode fixtures exercise actual damage and collision behavior.
public static class ValidateCaveAbilities
{
    static PlayerController player;
    static WildernessCaveCreature creature;
    static CaveSporeCloud cloud;
    static GameObject projectile;
    static int stage;
    static float start;
    static readonly List<string> results = new List<string>();
    static readonly HashSet<string> states = new HashSet<string>();
    static float Age => Time.time - start;
    public static string Start()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play persistent first.");
        Application.runInBackground = true; stage = 0; results.Clear(); states.Clear();
        SessionState.SetString("CaveAbilityValidation", "Running");
        EditorApplication.update -= Tick; EditorApplication.update += Tick; return "Cave ability tests scheduled.";
    }
    static void Tick() { try { Run(); } catch (Exception e) { results.Add("FAILED " + e); Finish(); } }
    static void Run()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }
        if (stage == 0)
        {
            if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName("SampleScene_15").isLoaded) return;
            player = UnityEngine.Object.FindObjectOfType<SceneLoader>().playerTrans.GetComponent<PlayerController>();
            var floor = new GameObject("TEMP Cave Ability Floor", typeof(BoxCollider2D)); floor.layer = 6;
            floor.transform.position = new Vector3(300, 0, 0); floor.GetComponent<BoxCollider2D>().size = new Vector2(60, 1);
            Spawn(0, new Vector2(300, .54f)); Put(301.7f); BeginAttack(); stage = 1; return;
        }
        if (stage == 1)
        {
            if (Age < .4f) return;
            results.Add("Lizard windup damage=" + PlayerDamage()); stage = 2; return;
        }
        if (stage == 2)
        {
            states.Add(creature.State.ToString()); if (Age < 1.25f) return;
            results.Add("Pounce damage=" + PlayerDamage() + " recovery=" + states.Contains("Recover"));
            UnityEngine.Object.Destroy(creature.gameObject);
            var edge = new GameObject("TEMP Cliff", typeof(BoxCollider2D)); edge.layer = 6; edge.transform.position = new Vector3(360, 0, 0); edge.GetComponent<BoxCollider2D>().size = new Vector2(6, 1);
            Spawn(0, new Vector2(362, .54f)); Put(366); player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            BeginAttack(); stage = 3; return;
        }
        if (stage == 3)
        {
            if (Age < 1.3f) return;
            results.Add("Lizard cliff safety=" + (creature.transform.position.x < 362.6f && creature.transform.position.y > .3f));
            UnityEngine.Object.Destroy(creature.gameObject); Spawn(1, new Vector2(300, 2.2f)); Put(301.2f); BeginAttack(); stage = 4; return;
        }
        if (stage == 4)
        {
            if (Age < .5f) return; results.Add("Moth windup damage=" + PlayerDamage()); stage = 5; return;
        }
        if (stage == 5)
        {
            states.Add(creature.State.ToString()); if (Age < 1.7f) return;
            results.Add("Moth swoop damage=" + PlayerDamage() + " low recovery=" + states.Contains("Recover"));
            UnityEngine.Object.Destroy(creature.gameObject); Spawn(2, new Vector2(310, .54f)); Put(300); ThrowAtPlayer(); stage = 6; return;
        }
        if (stage == 6)
        {
            if (Age < .5f) return; results.Add("Spore projectile direct damage=" + PlayerDamage());
            foreach (var c in UnityEngine.Object.FindObjectsOfType<CaveSporeCloud>()) UnityEngine.Object.Destroy(c.gameObject);
            Put(300); MakeCloud(300); start = Time.time; stage = 7; return;
        }
        if (stage == 7)
        {
            if (Age < 1.4f) return;
            results.Add("Cloud deals once=" + (PlayerDamage() == 5) + " total=" + PlayerDamage()); stage = 8; return;
        }
        if (stage == 8)
        {
            if (Age < 2.4f) return; results.Add("Cloud expired=" + (cloud == null));
            Put(300); start = Time.time; stage = 9; return;
        }
        if (stage == 9)
        {
            if (Age < .2f) return;
            player.inputcontroller.GamePlayer.Enable(); player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            player.GetComponent<SpriteRenderer>().flipX = false;
            results.Add("Slide starts=" + player.GetComponent<PlayerSlide>().TryStartSlide());
            MakeCloud(300.8f); start = Time.time; stage = 10; return;
        }
        if (stage == 10)
        {
            if (Age < .7f) return; results.Add("Slide crosses cloud unharmed=" + (PlayerDamage() == 0));
            if (cloud != null) UnityEngine.Object.Destroy(cloud.gameObject);
            Put(300); start = Time.time; stage = 11; return;
        }
        if (stage == 11)
        {
            if (Age < .2f) return;
            player.inputcontroller.GamePlayer.Enable(); player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            player.GetComponent<SpriteRenderer>().flipX = true;
            results.Add("Projectile slide starts=" + player.GetComponent<PlayerSlide>().TryStartSlide());
            ThrowAtPlayer(); start = Time.time; stage = 12; return;
        }
        if (stage == 12)
        {
            if (Age < .55f) return; results.Add("Slide passes projectile unharmed=" + (PlayerDamage() == 0));
            if (projectile != null) UnityEngine.Object.Destroy(projectile);
            foreach (var c in UnityEngine.Object.FindObjectsOfType<CaveSporeCloud>()) UnityEngine.Object.Destroy(c.gameObject);
            Put(309); var hp = creature.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = 100; hp.invulnerable = false; hp.OnHealthChange.Invoke(hp);
            player.inputcontroller.GamePlayer.Enable(); player.GetComponent<SpriteRenderer>().flipX = false;
            results.Add("Thrust started=" + player.GetComponent<PlayerSwordSkills>().TryUseSkill(false)); start = Time.time; stage = 13; return;
        }
        if (stage == 13)
        {
            if (Age < .6f) return; results.Add("Thrust enemy damage=" + (100 - creature.GetComponent<Character>().CurrentHp));
            Put(309.2f); var hp = creature.GetComponent<Character>(); hp.CurrentHp = 100; hp.invulnerable = false; hp.OnHealthChange.Invoke(hp);
            player.inputcontroller.GamePlayer.Enable(); results.Add("Spin started=" + player.GetComponent<PlayerSwordSkills>().TryUseSkill(true)); start = Time.time; stage = 14; return;
        }
        if (stage == 14)
        {
            if (Age < .8f) return; results.Add("Spin enemy damage=" + (100 - creature.GetComponent<Character>().CurrentHp));
            Put(300); MakeCloud(300); creature.TrackEffect(cloud.gameObject);
            var hit = new GameObject("TEMP kill").AddComponent<Attack>(); hit.enabled = false; hit.damage = 1000;
            creature.GetComponent<Character>().invulnerable = false; creature.GetComponent<Character>().TakeDamage(hit);
            start = Time.time; stage = 15; return;
        }
        if (stage == 15)
        {
            if (Age < .1f) return; results.Add("Owner death clears cloud=" + (cloud == null)); Finish();
        }
    }
    static float PlayerDamage() { return 10000 - player.GetComponent<Character>().CurrentHp; }
    static void Put(float x)
    {
        player.ResetForNewGame(); var hp = player.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = 10000; hp.invulnerable = false;
        player.transform.position = new Vector3(x, .55f, 0); var rb = player.GetComponent<Rigidbody2D>(); rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        player.inputcontroller.GamePlayer.Disable(); Physics2D.SyncTransforms();
    }
    static void Spawn(int index, Vector2 position)
    {
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildWildernessCaveCreatures.Prefabs + "/" + BuildWildernessCaveCreatures.Names[index] + ".prefab"), position, Quaternion.identity);
        creature = go.GetComponent<WildernessCaveCreature>(); creature.detectionRadius = 0; creature.patrolRadius = 0;
        Physics2D.SyncTransforms();
    }
    static void BeginAttack()
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(WildernessCaveCreature).GetField("aim", flags).SetValue(creature, (Vector2)player.GetComponent<CapsuleCollider2D>().bounds.center);
        typeof(WildernessCaveCreature).GetField("facing", flags).SetValue(creature, 1);
        typeof(WildernessCaveCreature).GetMethod("SetState", flags).Invoke(creature, new object[] { WildernessCaveCreature.ActionState.Windup });
        states.Clear(); start = Time.time;
    }
    static void ThrowAtPlayer()
    {
        projectile = new GameObject("TEMP Spore Projectile", typeof(SpriteRenderer)); projectile.transform.position = new Vector3(298.8f, 1.15f, 0);
        var damage = projectile.AddComponent<Attack>(); damage.enabled = false; damage.damage = 12;
        var p = projectile.AddComponent<CaveSporeProjectile>(); p.owner = creature; p.damageSource = damage; p.frames = creature.effectFrames; p.velocity = new Vector2(6, 0);
        creature.TrackEffect(projectile); start = Time.time;
    }
    static void MakeCloud(float x)
    {
        var go = new GameObject("TEMP Spore Cloud", typeof(SpriteRenderer)); go.transform.position = new Vector3(x, .53f, 0);
        var damage = go.AddComponent<Attack>(); damage.enabled = false; damage.damage = 5;
        cloud = go.AddComponent<CaveSporeCloud>(); cloud.owner = creature; cloud.damageSource = damage; cloud.frames = creature.effectFrames;
    }
    static void Finish() { EditorApplication.update -= Tick; SessionState.SetString("CaveAbilityValidation", string.Join("\n", results.ToArray())); }
}
