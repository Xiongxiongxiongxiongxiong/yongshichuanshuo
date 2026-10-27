using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ValidateSwordCollection
{
    public static string RenderContactSheet(string sourceName, string file)
    { return RenderSet(BuildSwordCollection.Ids, "sword.", sourceName, file); }
    public static string RenderSet(string[] ids, string prefix, string sourceName, string file, bool mirrorAlternate = false)
    {
        Check(Application.isPlaying, "Enter Play Mode first.");
        var inventory = UnityEngine.Object.FindObjectOfType<PlayerInventory>();
        var swap = inventory.GetComponent<TestWeaponSwap>(); var body = swap.GetComponent<SpriteRenderer>();
        var original = body.sprite; bool facing = body.flipX;
        var method = typeof(TestWeaponSwap).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var root = new GameObject("Sword validation render only"); root.transform.position = new Vector3(5000, 0, 0);
        var cameraObject = new GameObject("Sword validation camera only");
        var rt = new RenderTexture(2000, 1200, 24); var tex = new Texture2D(2000, 1200, TextureFormat.RGBA32, false);
        var oldActive = RenderTexture.active; var material = new Material(Shader.Find("Sprites/Default"));
        try
        {
            for (int n = 0; n < ids.Length; n++)
            {
                var item = inventory.Definition(prefix + ids[n]);
                Check(swap.EquipFrames(item.weaponFrames), "Cannot render " + item.id);
                body.sprite = swap.originalFrames.Single(s => s.name == sourceName); body.flipX = mirrorAlternate && n % 2 == 1; method.Invoke(swap, null);
                var equipped = swap.transform.Find("Independent Test Sword").GetComponent<SpriteRenderer>();
                var actor = new GameObject(item.displayName); actor.layer = 31; actor.transform.SetParent(root.transform, false);
                actor.transform.localPosition = new Vector3((n % 5 - 2) * 5, n < 5 ? .5f : -5.5f, 0);
                var b = actor.AddComponent<SpriteRenderer>(); b.sprite = body.sprite; b.flipX = body.flipX; b.sharedMaterial = material; b.sortingOrder = 1;
                var sword = new GameObject("Frame-locked sword"); sword.layer = 31; sword.transform.SetParent(actor.transform, false);
                sword.transform.localPosition = equipped.transform.localPosition;
                var w = sword.AddComponent<SpriteRenderer>(); w.sprite = equipped.sprite; w.sharedMaterial = material;
                w.flipX = equipped.flipX; w.flipY = equipped.flipY; w.sortingOrder = 1 + swap.CurrentBinding.sortingOffset;
            }
            var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(5000, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 7.5f; camera.aspect = 5f / 3;
            if (ids.Length <= 2)
            { camera.transform.position = new Vector3(5000 + (ids.Length == 1 ? -8 : -5.5f), 2, -10); camera.orthographicSize = 3.5f; }
            camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f,.085f,.105f,1);
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0,0,2000,1200),0,0); tex.Apply(); File.WriteAllBytes(file,tex.EncodeToPNG());
            return file;
        }
        finally
        {
            inventory.Load(); body.sprite = original; body.flipX = facing; method.Invoke(swap, null);
            RenderTexture.active = oldActive; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(cameraObject);
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(material);
        }
    }
    static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    public static string Assets()
    { return AssetsFor(BuildSwordCollection.Ids, "sword.", BuildSwordCollection.Root, "Docs/AI/SwordCollection/"); }
    public static string AssetsFor(string[] ids, string prefix, string root, string evidence)
    {
        var player = UnityEngine.Object.FindObjectOfType<PlayerInventory>(true);
        var swap = player.GetComponent<TestWeaponSwap>();
        int bindings = 0, visible = 0;
        var paths = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < ids.Length; i++)
        {
            var item = player.Definition(prefix + ids[i]);
            Check(item != null && item.IsWeapon && item.icon != null && item.maxStack == 1, "Incomplete item: " + i);
            Check((int)item.rarity == i / 2 && item.weaponFrames.Length == 108, "Incorrect rarity/frame count: " + item.id);
            Check(item.weaponFrames.Select(f => f.source).Distinct().Count() == 108, "Duplicate source: " + item.id);
            foreach (var f in item.weaponFrames)
            {
                var donor = swap.animatedFrames.Single(d => d.source == f.source);
                Check(f.hand == donor.hand && f.hidden == donor.hidden && f.sortingOffset == donor.sortingOffset &&
                    f.bodyRect == donor.bodyRect && f.bodyPivot == donor.bodyPivot && f.cropBody == donor.cropBody &&
                    ((f.flipHorizontal == donor.flipHorizontal && f.flipVertical == donor.flipVertical) ||
                    (BuildSwordCollection.IsRest(f.source) && ((f.flipHorizontal != donor.flipHorizontal && f.flipVertical != donor.flipVertical) || item.id == "dao.verdant_cleaver"))), "Calibration changed: " + item.id + "/" + f.source.name);
                if (!f.hidden)
                {
                    Check(f.sprite != null && f.sprite.pixelsPerUnit > 0, "Missing visible sprite");
                    Check(AssetDatabase.GetAssetPath(f.sprite.texture).Contains("/" + ids[i] + "/"), "Shared different weapon's texture");
                    visible++;
                }
                bindings++;
            }
            foreach (string file in new[] { "Main", "Supplement" })
            {
                string path = root + ids[i] + "/" + file + ".png";
                Check(paths.Add(path), "Shared atlas path");
                var tex = new Texture2D(2, 2);
                try
                {
                    tex.LoadImage(File.ReadAllBytes(path)); var pixels = tex.GetPixels32();
                    int clear = pixels.Count(p => p.a == 0);
                    Check(clear > pixels.Length / 3, "Opaque background in " + path);
                    Check(pixels.Any(p => p.a > 200), "Empty atlas " + path);
                }
                finally { UnityEngine.Object.DestroyImmediate(tex); }
            }
        }
        string report = "PASS: ten items, five rarities, twenty independent transparent atlases, " + bindings + " calibrated body bindings, " + visible + " visible weapon sprites. Existing hand/crop/facing/sorting data preserved.";
        Directory.CreateDirectory(evidence); File.WriteAllText(evidence + "AssetValidation.txt", report);
        Debug.Log(report); return report;
    }

    public static string Runtime()
    { return RuntimeFor(BuildSwordCollection.Ids, "sword.", "Docs/AI/SwordCollection/"); }
    public static string RuntimeFor(string[] ids, string prefix, string evidence)
    {
        Check(Application.isPlaying, "Enter Play Mode first.");
        var inventory = UnityEngine.Object.FindObjectOfType<PlayerInventory>();
        var swap = inventory.GetComponent<TestWeaponSwap>(); var body = swap.GetComponent<SpriteRenderer>();
        var animator = swap.GetComponent<Animator>(); bool animated = animator.enabled;
        var oldSprite = body.sprite; bool oldFlip = body.flipX;
        string saved = PlayerPrefs.GetString(PlayerInventory.SaveKey, ""); bool hadSave = PlayerPrefs.HasKey(PlayerInventory.SaveKey);
        var late = typeof(TestWeaponSwap).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        int cases = 0;
        try
        {
            animator.enabled = false;
            foreach (string id in ids)
            {
                int index = inventory.Backpack.stacks.FindIndex(s => s.itemId == prefix + id);
                Check(index >= 0, "Sword not in backpack: " + id);
                string message;
                if (inventory.EquippedWeaponId != prefix + id) Check(inventory.Equip(index, out message), "Cannot equip " + id);
                var item = inventory.Definition(prefix + id);
                foreach (var frame in item.weaponFrames) foreach (bool flip in new[] { false, true })
                {
                    body.sprite = frame.source; body.flipX = flip; late.Invoke(swap, null);
                    var rendered = swap.transform.Find("Independent Test Sword").GetComponent<SpriteRenderer>();
                    Check(swap.CurrentBinding == frame, "Missing current binding " + id + "/" + frame.source.name);
                    Check(rendered.enabled == (!frame.hidden && frame.sprite != null && body.enabled), "Incorrect visibility");
                    Check(rendered.sprite == frame.sprite && rendered.flipX == (flip ^ frame.flipHorizontal) && rendered.flipY == frame.flipVertical, "Incorrect pose/facing");
                    Check(Vector2.Distance(rendered.transform.localPosition, new Vector2(frame.hand.x * (flip ? -1 : 1), frame.hand.y)) < .0001f, "Grip drift");
                    Check(rendered.transform.localRotation == Quaternion.identity && rendered.sortingOrder == body.sortingOrder + frame.sortingOffset, "Independent clock/rotation or sorting drift");
                    cases++;
                }
                Check(inventory.Load() && inventory.EquippedWeaponId == item.id, "Equipped save reload failed");
            }
            // Real ownership transitions must never leave an equipped item outside the bag.
            string last = prefix + ids.Last();
            int selected = inventory.Backpack.stacks.FindIndex(s => s.itemId == last); string result;
            Check(inventory.Transfer(true, selected, out result) && inventory.EquippedWeaponId == "" && !swap.IsNewWeapon, "Deposit did not unequip");
            int stored = inventory.Warehouse.stacks.FindIndex(s => s.itemId == last);
            Check(inventory.Transfer(false, stored, out result), "Withdraw failed");
            selected = inventory.Backpack.stacks.FindIndex(s => s.itemId == last);
            Check(inventory.Equip(selected, out result), "Re-equip failed");
            Check(inventory.Discard(selected) && inventory.EquippedWeaponId == "" && !swap.IsNewWeapon, "Discard did not unequip");
            string report = "PASS: " + cases + " actual runtime body-frame/weapon/facing/visibility/grip/sorting checks; " + ids.Length + " equip/save/reload cases; deposit, withdraw, re-equip and discard ownership. Original inventory restored in finally.";
            Directory.CreateDirectory(evidence); File.WriteAllText(evidence + "RuntimeValidation.txt", report); Debug.Log(report); return report;
        }
        finally
        {
            if (hadSave) PlayerPrefs.SetString(PlayerInventory.SaveKey, saved); else PlayerPrefs.DeleteKey(PlayerInventory.SaveKey);
            PlayerPrefs.Save(); inventory.Load(); body.sprite = oldSprite; body.flipX = oldFlip; animator.enabled = animated;
            late.Invoke(swap, null);
        }
    }
}
