using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class RefreshCaveBossArenas
{
    public const string Folder = "Assets/GeneratedBoss/CaveArenasV2/";
    private static readonly string[] Backgrounds = { "AmberCoreMine", "MoonfallGrotto", "MoonveilSilkNest" };
    [MenuItem("Tools/Boss/Refresh Cave Arenas 3 to 5")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits first.");
        var props = Import("CaveArenaProps").texture;
        for (int index = 0; index < 3; index++)
        {
            string path = "Assets/Scenes/BossScene_" + (index + 3) + ".unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            string collisionBefore = RefreshForestBossArenas.CollisionSnapshot(scene);
            var roots = scene.GetRootGameObjects();
            var boss = roots.SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).First(c => index == 0 ? c is CrystalbackBoss : index == 1 ? c is AzureSalamanderBoss : c is MoonveilMothBoss);
            string bossBefore = EditorJsonUtility.ToJson(boss);
            foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                if (r is TilemapRenderer || r.name.Contains("Background") || r.name == "Goal Label" || r.name.Contains("Raised Platform")) r.enabled = false;
            var previous = roots.FirstOrDefault(g => g.name == "Cave Arena Artwork V2");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            var art = new GameObject("Cave Arena Artwork V2"); SceneManager.MoveGameObjectToScene(art, scene);
            roots = scene.GetRootGameObjects();
            var panorama = Import(Backgrounds[index]);
            var background = Make(Backgrounds[index] + " Background V2", art.transform, panorama, "BG", -50);
            float baseline = new[] { 644f, 678f, 705f }[index] / 941f;
            background.transform.position = new Vector3(0, (baseline - .5f) * 22, 10);
            background.transform.localScale = new Vector3(38 / panorama.bounds.size.x, 22 / panorama.bounds.size.y, 1);
            var platform = Prop(props, index, true);
            foreach (var c in roots.SelectMany(g => g.GetComponentsInChildren<BoxCollider2D>(true)).Where(c => c.name.Contains("Raised Platform")))
            {
                var r = Make(c.name + " Artwork V2", art.transform, platform, "build", 18);
                r.transform.position = new Vector3(c.bounds.center.x, c.bounds.max.y, 0);
                float scale = 3.3f / platform.bounds.size.x; r.transform.localScale = new Vector3(scale, scale, 1);
            }
            GameObject[] gates = index == 0 ? new[] { ((CrystalbackBoss)boss).entranceGate, ((CrystalbackBoss)boss).exitGate } : index == 1 ? new[] { ((AzureSalamanderBoss)boss).entranceGate, ((AzureSalamanderBoss)boss).exitGate } : new[] { ((MoonveilMothBoss)boss).entranceGate, ((MoonveilMothBoss)boss).exitGate };
            var gateSprite = Prop(props, index, false);
            foreach (var gate in gates)
            {
                foreach (var r in gate.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                foreach (var pulse in gate.GetComponentsInChildren<MossBossRuneSeal>(true)) pulse.enabled = false;
                var old = gate.transform.Find("Cave Barricade Artwork V2");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var rNew = Make("Cave Barricade Artwork V2", gate.transform, gateSprite, "build", 18);
                rNew.transform.position = new Vector3(gate.transform.position.x, 0, -.1f);
                rNew.transform.localScale = new Vector3(1.4f / gateSprite.bounds.size.x, 8 / gateSprite.bounds.size.y, 1);
            }
            if (index == 0) boss.GetComponent<CrystalbackHud>().useTopLayout = true;
            else if (index == 1) boss.GetComponent<AzureSalamanderHud>().useTopLayout = true;
            else boss.GetComponent<MoonveilMothHud>().useTopLayout = true;
            if (collisionBefore != RefreshForestBossArenas.CollisionSnapshot(scene)) throw new InvalidOperationException("Physics changed in " + path);
            if (bossBefore != EditorJsonUtility.ToJson(boss)) throw new InvalidOperationException("Boss configuration changed in " + path);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("BossScene_3,4,5 artwork overwritten; physics and boss configuration preserved.");
    }
    private static SpriteRenderer Make(string name, Transform parent, Sprite sprite, string layer, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.sortingLayerName = layer; r.sortingOrder = order; return r;
    }
    private static Sprite Import(string name)
    {
        string path = Folder + name + ".png"; var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static Sprite Prop(Texture2D texture, int index, bool platform)
    {
        int left = index * texture.width / 3, right = (index + 1) * texture.width / 3;
        float split = 280 / 1024f;
        int bottom = platform ? Mathf.RoundToInt(texture.height * (1 - split)) : 0;
        int top = platform ? texture.height : Mathf.RoundToInt(texture.height * (1 - split));
        var pixels = texture.GetPixels32(); int minX = right, minY = top, maxX = left, maxY = bottom;
        for (int y = bottom; y < top; y++) for (int x = left; x < right; x++)
            if (pixels[y * texture.width + x].a > 150) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        float w = maxX - minX + 1, h = maxY - minY + 1;
        float foot = platform ? texture.height * (1 - 126 / 1024f) : minY;
        var sprite = Sprite.Create(texture, new Rect(minX, minY, w, h), new Vector2(.5f, (foot - minY) / h), 100, 0, SpriteMeshType.FullRect);
        sprite.name = Backgrounds[index] + (platform ? "PlatformV2" : "GateV2");
        string path = Folder + sprite.name + ".asset"; var old = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (old == null) { AssetDatabase.CreateAsset(sprite, path); return sprite; }
        EditorUtility.CopySerialized(sprite, old); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(old); return old;
    }
}
