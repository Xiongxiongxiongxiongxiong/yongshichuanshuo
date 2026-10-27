using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Focused Play Mode regression checks; uses the real player, damage and scene references.
public static class ValidateDesertDungeonBosses
{
    private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    private static void Set(object target, string name, object value) { target.GetType().GetField(name, Flags).SetValue(target, value); }
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Place(PlayerController p, float x, float y = .05f)
    {
        p.ResetForNewGame(); p.transform.position = new Vector3(x, y, 0); p.GetComponent<Rigidbody2D>().position = new Vector2(x, y); Physics2D.SyncTransforms();
    }
    private static ArenaBossEffect[] Effects(DesertDungeonBoss b, int row) => Get<List<GameObject>>(b, "effects").Where(g => g != null).Select(g => g.GetComponent<ArenaBossEffect>()).Where(e => e.row == row).ToArray();
    private static void Tick(DesertDungeonBoss b, float time) { Set(b, "timer", time); Call(b, "Update"); }
    public static string CheckCurrent()
    {
        var b = UnityEngine.Object.FindObjectOfType<DesertDungeonBoss>(); Check(b != null, "Encounter not loaded"); Time.timeScale = 0;
        var p = UnityEngine.Object.FindObjectOfType<PlayerController>(); var hp = p.GetComponent<Character>();
        Call(b, "SetState", DesertDungeonBoss.Mode.Dormant); b.BeginBattle(p); Call(b, "ClearEffects");
        b.transform.position = Vector3.zero; b.GetComponent<Rigidbody2D>().position = Vector2.zero; Place(p, -2); Set(b, "facing", -1);
        Check(b.frames.Length == 32 && b.frames.All(s => s != null) && b.effectFrames.Length == 32 && b.effectFrames.All(s => s != null), "Missing sprite frame");
        Call(b, "StartAttack", 0); float windup = b.kind == DesertDungeonBoss.Kind.SunburialScorpion || b.kind == DesertDungeonBoss.Kind.SoulLantern ? .65f : .7f;
        Tick(b, windup - .01f); Check(hp.CurrentHp == hp.maxHp, "Close attack before telegraph");
        Tick(b, windup); Check(hp.CurrentHp == hp.maxHp - 15, "Close attack failed"); hp.invulnerable = false;
        Tick(b, windup + .1f); Check(hp.CurrentHp == hp.maxHp - 15, "Close attack repeated damage");
        var savedRandom = UnityEngine.Random.state; UnityEngine.Random.InitState(26026); int[] counts = new int[4]; int previous = -1;
        try
        {
            for (int i = 0; i < 120; i++)
            {
                Place(p, i < 60 ? -2 : -10); Call(b, "ChooseAttack"); int m = Get<int>(b, "lastMove"); counts[m]++;
                Check(!(m == 2 && previous == 2), "Consecutive mobility");
                if (i < 60 && b.kind != DesertDungeonBoss.Kind.SoulLantern) Check(m != 2, "Forced near mobility");
                previous = m;
            }
        }
        finally { UnityEngine.Random.state = savedRandom; }
        Call(b, "ClearEffects"); Set(b, "phase", false);
        if (b.kind == DesertDungeonBoss.Kind.SunburialScorpion)
        {
            Place(p, 10, 2.05f); Call(b, "StartAttack", 3); var pool = Effects(b, 3).Single();
            Check(pool.transform.position.y < .1f, "Quicksand spawned on platform"); Set(pool, "age", 1.31f); Call(pool, "Update");
            Check(p.GetComponent<PlayerRootSlow>().Multiplier == 1, "Quicksand reached elevated player");
            Place(p, 10); Call(pool, "Update"); Check(p.GetComponent<PlayerRootSlow>().Multiplier < 1 && hp.CurrentHp == hp.maxHp, "Quicksand must slow without ticks");
            Call(b, "ClearEffects"); Set(b, "phase", true); Call(b, "StartAttack", 1); Tick(b, .8f); Check(Effects(b, 2).Length == 2, "Phase tail targets missing");
        }
        else if (b.kind == DesertDungeonBoss.Kind.SteleLizard)
        {
            Place(p, 10, 2.05f); Call(b, "StartAttack", 1); Tick(b, 1.4f); Check(Effects(b, 1).Length == 2, "Slam waves missing");
            var wave = Effects(b, 1)[0]; wave.velocity = Vector2.zero; wave.transform.position = new Vector3(10, .42f, 0); Call(wave, "Update");
            Check(hp.CurrentHp == hp.maxHp, "Sand wave hit platform player");
            Call(b, "ClearEffects"); Set(b, "phase", true); Call(b, "StartAttack", 1); Tick(b, 1.4f); Tick(b, 2.05f); Check(Effects(b, 1).Length == 4, "Phase second wave missing");
            Call(b, "ClearEffects"); Place(p, 7); Call(b, "StartAttack", 2); var fixedPoint = Get<Vector2>(b, "destination"); Place(p, -7); Tick(b, 1.2f);
            Check((b.GetComponent<Rigidbody2D>().position - fixedPoint).sqrMagnitude < .01f && b.GetComponent<BoxCollider2D>().enabled, "Burrow must lock position and restore collider");
        }
        else if (b.kind == DesertDungeonBoss.Kind.IroncoffinJailer)
        {
            Place(p, -3); Set(b, "facing", -1); Call(b, "StartAttack", 3); Tick(b, 1.1f); var chain = Effects(b, 4).Single();
            chain.velocity = Vector2.zero; chain.transform.position = new Vector3(-3, .05f, 0); Call(chain, "Update");
            Check(hp.CurrentHp == hp.maxHp - 16 && p.GetComponent<Rigidbody2D>().position.x > -2, "Chain damage/pull failed");
        }
        else
        {
            Place(p, -8); Call(b, "StartAttack", 1); Tick(b, 1.2f); Check(Effects(b, 5).Length == 3, "Normal souls count");
            Call(b, "ClearEffects"); Set(b, "phase", true); Call(b, "StartAttack", 1); Tick(b, 1.2f); Check(Effects(b, 5).Length == 5, "Phase souls count");
            Call(b, "ClearEffects"); Set(b, "phase", false); Call(b, "StartAttack", 2); var mark = Effects(b, 6).Single();
            Place(p, 7, 2.05f); Set(mark, "age", .6f); Call(mark, "Update"); Check(Mathf.Abs(mark.transform.position.x - 7) < .01f, "Mark tracking");
            Set(mark, "age", .95f); Place(p, -5); Call(mark, "Update"); Check(Mathf.Abs(mark.transform.position.x - 7) < .01f, "Mark did not lock");
            Call(b, "ClearEffects"); Call(b, "StartAttack", 3); Check(Effects(b, 5).Length == 2 && Effects(b, 5).All(e => e.delay == 1.5f), "Recall warning routes missing");
        }
        Call(b, "ClearEffects"); Place(p, -6); b.transform.position = new Vector3(3, .02f, 0); b.GetComponent<Rigidbody2D>().position = new Vector2(3, .02f);
        Call(b, "SetState", DesertDungeonBoss.Mode.Idle); Set(b, "idleDuration", 100f); Physics2D.SyncTransforms(); Time.timeScale = 1;
        return b.kind + ": PASS close telegraph/single hit, random choices=" + string.Join(",", counts) + ", archetype mechanics; live movement started x3";
    }
    public static string FinishMovement()
    {
        Time.timeScale = 0; var b = UnityEngine.Object.FindObjectOfType<DesertDungeonBoss>();
        Check(b.transform.position.x < 2.95f, "No approach movement");
        Check(b.GetComponent<SpriteRenderer>().sprite.name.Contains("_1_"), "Movement animation not active");
        string result = b.kind + ": PASS live approach x=" + b.transform.position.x + " sprite=" + b.GetComponent<SpriteRenderer>().sprite.name;
        var a = new GameObject("QA Boss Finisher").AddComponent<Attack>(); a.damage = 10000;
        b.GetComponent<Character>().invulnerable = false; b.GetComponent<Character>().TakeDamage(a); UnityEngine.Object.Destroy(a.gameObject);
        Check(b.State == DesertDungeonBoss.Mode.Dead, "Death state"); Tick(b, 1.8f);
        Check(!b.entranceGate.activeSelf && !b.exitGate.activeSelf, "Death did not unlock gates"); Time.timeScale = 1;
        return result + "; death/gates PASS";
    }
}
