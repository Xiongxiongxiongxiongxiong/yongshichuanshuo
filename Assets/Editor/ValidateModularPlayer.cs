using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

/// <summary>Runs real input smoke tests in the loaded mine; never saves runtime changes.</summary>
public static class ValidateModularPlayer
{
    private const string Output = "Docs/AI/ModularPlayer/";
    private static PlayerController player;
    private static ModularPlayerVisual visual;
    private static SpriteRenderer source, body, weapon;
    private static float started;
    private static int stage;
    private static bool captured, oldBackground;
    private static Vector3 origin;
    private static Vector2 capsuleSize;
    private static readonly HashSet<int> frames = new HashSet<int>();
    private static readonly List<string> results = new List<string>();
    private static float Age => Time.time - started;
    private static readonly string[] Names = { "Idle", "RunRight", "RunLeft", "Release", "Attack", "Jump", "Crouch", "Release", "Slide", "Thrust", "Spin", "Upgrade", "Climb" };
    private static readonly float[] Durations = { .4f,.45f,.45f,.3f,1.1f,2.1f,.4f,.3f,.8f,.8f,.9f,1.4f,.65f };

    public static string Start()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play the mine scene first.");
        player = UnityEngine.Object.FindObjectOfType<PlayerController>();
        visual = player.GetComponent<ModularPlayerVisual>();
        source = player.GetComponent<SpriteRenderer>();
        body = player.transform.Find("Modular Body").GetComponent<SpriteRenderer>();
        weapon = player.transform.Find("WeaponSocket/Equipped Weapon").GetComponent<SpriteRenderer>();
        origin = player.transform.position;
        capsuleSize = player.GetComponent<CapsuleCollider2D>().size;
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        stage = 0; results.Clear(); frames.Clear(); captured = false; started = Time.time;
        Directory.CreateDirectory(Output);
        SessionState.SetString("ModularPlayerValidation", "Running");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        return "Started mine input and modular art smoke tests";
    }

    private static void Tick()
    {
        try { Run(); }
        catch (Exception ex) { results.Add("FAIL " + ex); Finish(); }
    }

    private static void Run()
    {
        if (!Application.isPlaying) { results.Add("FAIL Play Mode ended during validation"); Finish(); return; }
        var key = Key.None;
        if (stage == 1) key = Key.D;
        if (stage == 2) key = Key.A;
        if (stage == 4 && Age < .12f) key = Key.J;
        if (stage == 5 && Age < .12f) key = Key.Space;
        if (stage == 6) key = Key.S;
        if (stage == 8 && Age < .12f) key = Key.K;
        if (stage == 9 && Age < .12f) key = Key.U;
        if (stage == 10 && Age < .12f) key = Key.I;
        if (stage == 12) key = Key.S;
        InputSystem.QueueStateEvent(Keyboard.current, key == Key.None ? new KeyboardState() : new KeyboardState(key));
        frames.Add(visual.CurrentFrame);
        if (!source.forceRenderingOff) throw new Exception("Original body is visible");
        if (body.sprite == null || !body.sprite.name.StartsWith("Adventurer_")) throw new Exception("Old or missing body frame");
        if (body.flipX != source.flipX) throw new Exception("Body facing disagrees with controller");
        if (Age > .08f && !captured && stage != 3 && stage != 7 &&
            (stage != 4 || player.isAttack) && (stage != 9 && stage != 10 || player.IsUsingSwordSkill))
        {
            Capture(Names[stage]+"Game");
            captured = true;
        }
        if (Age < Durations[stage]) return;
        bool ok = true;
        switch (stage)
        {
            case 0: ok = frames.Any(f=>f<6); break;
            case 1: ok = frames.Any(f=>f>=24&&f<32)&&!source.flipX; break;
            case 2: ok = frames.Any(f=>f>=24&&f<32)&&source.flipX; break;
            case 4: ok = frames.Any(f=>f>=12&&f<18)&&!player.isAttack; break;
            case 5: ok = frames.Contains(18)&&frames.Contains(19)&&player.GetComponent<PhysicsCheck>().isground; break;
            case 6: ok = frames.Contains(20); break;
            case 8: ok = frames.Contains(20)&&!player.GetComponent<PlayerSlide>().IsSliding&&player.GetComponent<CapsuleCollider2D>().size==capsuleSize; break;
            case 9: ok = frames.Contains(15)&&!player.IsUsingSwordSkill; break;
            case 10: ok = frames.Any(f=>f>=12&&f<18)&&!player.IsUsingSwordSkill; break;
            case 11: ok = frames.Contains(13)&&!player.IsWeaponUpgrading; break;
            case 12: ok = frames.Contains(23)&&!weapon.enabled; break;
        }
        results.Add((ok?"PASS ":"FAIL ")+Names[stage]+" frames="+string.Join(",",frames.OrderBy(f=>f).Select(f=>f.ToString()).ToArray()));
        stage++; frames.Clear(); captured = false; started = Time.time;
        // Each displacement test starts on the known-safe spawn platform.
        // Sliding left followed by a thrust can otherwise leave the mine boundary.
        if (stage == 8 || stage == 9 || stage == 10)
        {
            player.transform.position = origin;
            player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            source.flipX = false;
            Physics2D.SyncTransforms();
        }
        if (stage == 9 || stage == 10) player.GetComponent<PlayerStamina>().ResetStamina();
        if (stage == 11) player.BeginWeaponUpgrade(0);
        if (stage == 12)
        {
            var map = UnityEngine.Object.FindObjectsOfType<Tilemap>().First(m=>m.name=="Forest Decorations");
            var ladders = new List<Vector3>();
            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                var tile = map.GetTile(cell);
                if (tile != null && tile.name.IndexOf("Ladder",StringComparison.OrdinalIgnoreCase)>=0) ladders.Add(map.GetCellCenterWorld(cell));
            }
            var spot = ladders.OrderBy(p=>Mathf.Abs(p.x-origin.x)).Skip(2).First();
            player.transform.position = spot;
            player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        if (stage >= Names.Length)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            player.isHurt = true; visual.RefreshVisual(0);
            results.Add((visual.CurrentFrame==21?"PASS ":"FAIL ")+"Hurt presentation"); Capture("HurtGame");
            player.isHurt = false; player.isDead = true; visual.RefreshVisual(0);
            results.Add((visual.CurrentFrame==22&&!weapon.enabled?"PASS ":"FAIL ")+"Death hides weapon"); Capture("DeadGame");
            player.isDead = false;
            var original = visual.EquippedWeapon;
            visual.EquipWeapon(null); visual.RefreshVisual(0);
            bool empty = !weapon.enabled;
            visual.EquipWeapon(original,.85f); visual.RefreshVisual(0);
            results.Add((empty&&visual.EquippedWeapon==original?"PASS ":"FAIL ")+"Unequip and replace weapon independently of body");
            visual.EquipWeapon(original);
            visual.enabled = false;
            bool restored = !source.forceRenderingOff&&!body.enabled&&!weapon.enabled;
            visual.enabled = true;
            results.Add((restored&&source.forceRenderingOff?"PASS ":"FAIL ")+"Disable restores original, re-enable hides original");
            Finish();
        }
    }

    public static void Capture(string name)
    {
        var go = new GameObject("TEMP Modular Validation Camera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 2.5f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.06f,.09f,.12f,1);
        cam.transform.position = player.transform.position+new Vector3(0,.8f,-10); cam.enabled = false;
        var rt = new RenderTexture(960,720,24); var prior = RenderTexture.active;
        var texture = new Texture2D(960,720,TextureFormat.RGBA32,false);
        try
        {
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0,0,960,720),0,0); texture.Apply();
            File.WriteAllBytes(Output+name+".png",texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = prior; cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        Application.runInBackground = oldBackground;
        string report = string.Join("\n",results.ToArray());
        File.WriteAllText(Output+"RuntimeValidation.txt",report);
        SessionState.SetString("ModularPlayerValidation",report);
    }
}
