using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildStonebackCaveBear
{
    public const string Folder = "Assets/GeneratedMonsters/WildernessCaves/StonebackBear";
    public const string PrefabPath = "Assets/Prefabs/WildernessCaveMonsters/StonebackCaveBear.prefab";
    [MenuItem("Tools/Monsters/Build Stoneback Cave Bear")]
    public static void BuildMenu() { Debug.Log(Build()); }
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        if (!AssetDatabase.IsValidFolder(Folder + "/Frames")) AssetDatabase.CreateFolder(Folder, "Frames");
        var frames = Slice(false); var effects = Slice(true);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(BuildWildernessCaveCreatures.Prefabs + "/CaveMossLizard.prefab");
        if (source == null) throw new InvalidOperationException("Existing cave creature template missing.");
        var go = UnityEngine.Object.Instantiate(source); go.SetActive(false); go.name = "Stoneback Cave Bear";
        UnityEngine.Object.DestroyImmediate(go.GetComponent<WildernessCaveCreature>());
        var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = frames[0]; sr.color = Color.white;
        var hp = go.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = 100; hp.invulnerableDuration = .18f;
        var body = go.GetComponent<Rigidbody2D>(); body.mass = 4; body.gravityScale = 3; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var box = go.GetComponent<BoxCollider2D>(); box.size = new Vector2(2.1f, 1.95f); box.offset = new Vector2(0, .975f);
        var melee = go.GetComponent<Attack>(); melee.enabled = false; melee.damage = 14;
        var slam = go.AddComponent<Attack>(); slam.enabled = false; slam.damage = 20;
        var ai = go.AddComponent<StonebackCaveBear>(); ai.frames = frames; ai.effectFrames = effects; ai.meleeDamage = melee; ai.slamDamage = slam;
        go.SetActive(true); PrefabUtility.SaveAsPrefabAsset(go, PrefabPath); UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets(); return "Built cave bear: 100 HP, 32 creature frames, 8 VFX frames, directional defense, existing flash/bar.";
    }
    static Sprite[] Slice(bool effect)
    {
        string name = effect ? "StonebackBearEffects" : "StonebackBear";
        string path = Folder + "/" + name + ".png"; AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 65; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var cpu = new Texture2D(2, 2); cpu.LoadImage(System.IO.File.ReadAllBytes(path));
        // The generated raised-paw pose is taller: explicit row bounds preserve complete paws.
        float[] tops = { 25, 210, 408, 593, 872, 1105, 1298, 1545 };
        float[] bottoms = { 205, 404, 592, 865, 1095, 1285, 1513, 1740 };
        int rows = effect ? 2 : 8; var result = new Sprite[rows * 4];
        for (int row = 0; row < rows; row++) for (int col = 0; col < 4; col++)
        {
            int x0 = Mathf.RoundToInt(col * texture.width / 4f), x1 = Mathf.RoundToInt((col + 1) * texture.width / 4f);
            int y0 = effect ? Mathf.RoundToInt(texture.height * (1 - (row + 1f) / rows)) : Mathf.RoundToInt(texture.height * (1 - bottoms[row] / 1774f));
            int y1 = effect ? Mathf.RoundToInt(texture.height * (1 - row / (float)rows)) : Mathf.RoundToInt(texture.height * (1 - tops[row] / 1774f));
            Rect rect = new Rect(x0, y0, x1 - x0, y1 - y0);
            int foot = y1 - y0, count = 0;
            for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++)
            {
                if (cpu.GetPixel(x0 + x, y0 + y).a < .7f) continue;
                count++; if (x > rect.width * .2f && x < rect.width * .8f) foot = Mathf.Min(foot, y);
            }
            if (count < 30) throw new InvalidOperationException("Empty bear frame " + row + "/" + col);
            float ppu = effect ? rect.width / (row == 0 ? 1.1f : 1.8f) : 65 * texture.width / 887f;
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, (foot + 1f) / rect.height), ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name + "_" + row + "_" + col;
            string output = Folder + "/Frames/" + sprite.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(output);
            if (existing == null) { AssetDatabase.CreateAsset(sprite, output); result[row * 4 + col] = sprite; }
            else { EditorUtility.CopySerialized(sprite, existing); EditorUtility.SetDirty(existing); result[row * 4 + col] = existing; UnityEngine.Object.DestroyImmediate(sprite); }
        }
        UnityEngine.Object.DestroyImmediate(cpu); return result;
    }
    [MenuItem("Tools/Monsters/Place Stoneback Cave Bear In Caves 15-17")]
    public static void PlaceMenu() { Debug.Log(PlaceAll()); }
    public static string PlaceAll()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Place in Edit Mode.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene changes first.");
        var setup = EditorSceneManager.GetSceneManagerSetup(); string report = "";
        try
        {
            for (int n = 15; n <= 17; n++)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_" + n + ".unity");
                if (UnityEngine.Object.FindObjectOfType<StonebackCaveBear>() != null) { report += n + ": existing bear preserved.\n"; continue; }
                Vector2 expected = n == 15 ? new Vector2(16, 6) : n == 16 ? new Vector2(70, 24.13f) : new Vector2(72, 18.42f);
                Physics2D.SyncTransforms(); var floor = Physics2D.Raycast(expected + Vector2.up, Vector2.down, 2, 1 << 6);
                if (!floor) throw new InvalidOperationException("Bear landing surface missing in " + n);
                // Verify a broad landing area, without altering authored terrain.
                // The lower grotto's safe eight-meter stretch lies mostly left of the bear,
                // keeping the creature clear of the ladder landing on its left.
                foreach (float dx in n == 15 ? new[] { -5.5f, 2.5f } : new[] { -4f, 4f })
                {
                    var flank = Physics2D.Raycast(new Vector2(expected.x + dx, floor.point.y + .5f), Vector2.down, 1, 1 << 6);
                    if (!flank || Mathf.Abs(flank.point.y - floor.point.y) > .3f) throw new InvalidOperationException("Bear arena is too narrow in " + n);
                }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
                go.transform.position = new Vector3(expected.x, floor.point.y + .04f, 0);
                var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Wilderness Cave Creatures");
                if (root != null) go.transform.SetParent(root.transform, true);
                go.GetComponent<SpriteRenderer>().color = n == 16 ? new Color(1, .85f, .72f) : n == 17 ? new Color(.78f, .92f, 1) : Color.white;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                report += "Cave" + n + " bear " + go.transform.position + " wide ground verified\n";
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return report;
    }
}

