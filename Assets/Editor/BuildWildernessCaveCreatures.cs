using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildWildernessCaveCreatures
{
    public const string Folder = "Assets/GeneratedMonsters/WildernessCaves";
    public const string Prefabs = "Assets/Prefabs/WildernessCaveMonsters";
    public static readonly string[] Names = { "CaveMossLizard", "CaveMistMoth", "CaveRockSpitter" };

    [MenuItem("Tools/Monsters/Build Wilderness Cave Creatures")]
    public static void BuildMenu() { Debug.Log(Build()); }
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        EnsureFolder(Prefabs); EnsureFolder(Folder + "/Frames");
        var effects = Slice("CaveSporeEffects", 3, 180, false, true);
        for (int i = 0; i < Names.Length; i++)
        {
            var frames = Slice(Names[i], 6, i == 0 ? 170 : i == 1 ? 185 : 175, i == 1, false);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MineMonsters/PickaxeImp.prefab");
            if (source == null) throw new InvalidOperationException("Existing monster template missing.");
            var go = UnityEngine.Object.Instantiate(source); go.SetActive(false); go.name = Names[i];
            UnityEngine.Object.DestroyImmediate(go.GetComponent<MineCreature>());
            var visual = go.GetComponent<SpriteRenderer>(); visual.sprite = frames[0]; visual.color = Color.white;
            var hp = go.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = i == 0 ? 35 : i == 1 ? 22 : 30;
            var body = go.GetComponent<Rigidbody2D>(); body.gravityScale = i == 1 ? 0 : 3;
            body.mass = i == 2 ? 5 : 1.5f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (i == 2) body.constraints |= RigidbodyConstraints2D.FreezePositionX;
            var box = go.GetComponent<BoxCollider2D>();
            box.size = i == 0 ? new Vector2(1.15f, .65f) : i == 1 ? new Vector2(.72f, .65f) : new Vector2(1f, .9f);
            box.offset = i == 1 ? Vector2.zero : Vector2.up * box.size.y * .5f;
            var damage = go.GetComponent<Attack>(); damage.enabled = false; damage.damage = i == 0 ? 10 : i == 1 ? 8 : 12;
            var ai = go.AddComponent<WildernessCaveCreature>(); ai.species = (WildernessCaveCreature.Species)i;
            ai.frames = frames; ai.effectFrames = effects; ai.damageSource = damage;
            var barSettings = new SerializedObject(go.GetComponent<MonsterHealthBar>());
            barSettings.FindProperty("verticalOffset").floatValue = i == 1 ? .7f : .28f;
            barSettings.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true); PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "/" + Names[i] + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        return "Built 3 cave monster prefabs, 72 creature frames, 12 spore VFX frames; existing flash and health bars reused.";
    }
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
    static Sprite[] Slice(string name, int rows, float ppu, bool flying, bool effect)
    {
        string path = Folder + "/" + name + ".png"; AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = ppu; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var cpu = new Texture2D(2, 2); cpu.LoadImage(System.IO.File.ReadAllBytes(path));
        var result = new Sprite[rows * 4];
        for (int row = 0; row < rows; row++) for (int col = 0; col < 4; col++)
        {
            int x0 = Mathf.RoundToInt(col * texture.width / 4f), x1 = Mathf.RoundToInt((col + 1) * texture.width / 4f);
            int y0 = Mathf.RoundToInt(texture.height * (1 - (row + 1f) / rows));
            int y1 = Mathf.RoundToInt(texture.height * (1 - row / (float)rows));
            Rect rect = new Rect(x0, y0, x1 - x0, y1 - y0);
            int foot = (int)rect.height, count = 0; Vector2 core = Vector2.zero; float weight = 0;
            for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++)
            {
                Color c = cpu.GetPixel(x0 + x, y0 + y); if (c.a < .7f) continue;
                count++;
                if (x > rect.width * .2f && x < rect.width * .8f) foot = Mathf.Min(foot, y);
                if (flying && c.b > .75f && c.g > .65f && c.r < .65f && x > rect.width * .25f)
                { float w = c.b * c.g; core += new Vector2(x, y) * w; weight += w; }
            }
            if (count < 20) throw new InvalidOperationException("Empty animation cell: " + name + " " + row + "/" + col);
            Vector2 pivot = new Vector2(.5f, Mathf.Clamp01((foot + 1) / rect.height));
            if (flying) pivot = weight > 1 ? new Vector2(core.x / weight / rect.width, core.y / weight / rect.height) : new Vector2(.55f, .3f);
            if (effect && row == 0) pivot = new Vector2(.5f, .5f);
            float pixels = effect ? rect.width / (row == 0 ? .5f : row == 1 ? 1.3f : 1.85f) : ppu;
            var sprite = Sprite.Create(texture, rect, pivot, pixels, 0, SpriteMeshType.FullRect);
            sprite.name = name + "_" + row + "_" + col;
            string output = Folder + "/Frames/" + sprite.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(output);
            if (existing == null) { AssetDatabase.CreateAsset(sprite, output); result[row * 4 + col] = sprite; }
            else { EditorUtility.CopySerialized(sprite, existing); EditorUtility.SetDirty(existing); result[row * 4 + col] = existing; UnityEngine.Object.DestroyImmediate(sprite); }
        }
        UnityEngine.Object.DestroyImmediate(cpu); return result;
    }

    [MenuItem("Tools/Monsters/Place Wilderness Cave Creatures In Scenes 15-17")]
    public static void PlaceMenu() { Debug.Log(PlaceAll()); }
    public static string PlaceAll()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Place in Edit Mode.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Preserve unsaved scene work before placing monsters.");
        string result = "";
        try
        {
            for (int n = 15; n <= 17; n++)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_" + n + ".unity");
                if (scene.GetRootGameObjects().Any(g => g.name == "Wilderness Cave Creatures")) { result += n + ": existing group preserved.\n"; continue; }
                var root = new GameObject("Wilderness Cave Creatures"); Physics2D.SyncTransforms();
                Vector2[] pixels = n == 15 ? new[] { new Vector2(430, 294), new Vector2(690, 480), new Vector2(895, 296) } :
                    n == 16 ? new[] { new Vector2(650, 397), new Vector2(550, 320), new Vector2(1125, 326) } :
                    new[] { new Vector2(650, 244), new Vector2(790, 497), new Vector2(780, 435) };
                for (int i = 0; i < 3; i++)
                {
                    Vector2 point = PanoramaLevelAuthoring.Point(pixels[i].x, pixels[i].y);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + Names[i] + ".prefab");
                    if (prefab == null) throw new InvalidOperationException("Build prefab first: " + Names[i]);
                    if (i != 1)
                    {
                        var floor = Physics2D.Raycast(point + Vector2.up, Vector2.down, 2.5f, 1 << 6);
                        if (!floor) throw new InvalidOperationException("No monster landing surface at " + point);
                        point.y = floor.point.y + .04f;
                    }
                    else if (Physics2D.OverlapBox(point, new Vector2(1.3f, 1.3f), 0, 1 << 6))
                        throw new InvalidOperationException("Moth would spawn inside rock: " + point);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    go.transform.SetParent(root.transform, true); go.transform.position = point;
                    go.GetComponent<SpriteRenderer>().color = n == 16 ? new Color(1, .88f, .76f) : n == 17 ? new Color(.8f, .94f, 1) : Color.white;
                    result += n + " " + Names[i] + " " + point + "\n";
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return result;
    }
}
