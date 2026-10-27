using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class RefreshForestBossArenas
{
    public const string Folder = "Assets/GeneratedBoss/ForestArenaV2/";
    [MenuItem("Tools/Boss/Refresh Forest Arenas 1 and 2")]
    public static void Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits first.");
        var props = Import("ForestArenaProps").texture;
        for (int index = 0; index < 2; index++)
        {
            string path = "Assets/Scenes/BossScene_" + (index + 1) + ".unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var oldColliders = CollisionSnapshot(scene);
            var roots = scene.GetRootGameObjects();
            var boss = roots.SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true)).First(c => index == 0 ? c is MossTuskBoss : c is SporeCrownBoss);
            string bossBefore = EditorJsonUtility.ToJson(boss);
            foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                if (r is TilemapRenderer || r.name.Contains("Background") || r.name == "Goal Label" || r.name == "Rune Seal" || r.name.Contains("Raised Platform")) r.enabled = false;
            foreach (var pulse in roots.SelectMany(g => g.GetComponentsInChildren<MossBossRuneSeal>(true))) pulse.enabled = false;
            var existing = roots.FirstOrDefault(g => g.name == "Forest Arena Artwork V2");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var art = new GameObject("Forest Arena Artwork V2"); SceneManager.MoveGameObjectToScene(art, scene);
            roots = scene.GetRootGameObjects();
            var panorama = Import(index == 0 ? "AncientForestSanctuary" : "SporeCrownGrotto");
            var bg = Make("Layered Forest Sanctuary Background", art.transform, panorama, "BG", -50);
            // Calibrated visible foot plane in each generated panorama, not its lower foundation edge.
            float baselineFromTop = index == 0 ? 665 : 665;
            bg.transform.position = new Vector3(0, (baselineFromTop / panorama.texture.height - .5f) * 22, 10);
            bg.transform.localScale = new Vector3(38 / panorama.bounds.size.x, 22 / panorama.bounds.size.y, 1);
            var platform = Prop(props, index, true);
            foreach (var c in roots.SelectMany(g => g.GetComponentsInChildren<BoxCollider2D>(true)).Where(c => c.name.Contains("Raised Platform")))
            {
                var r = Make(c.name + " Artwork V2", art.transform, platform, "build", 18);
                r.transform.position = new Vector3(c.bounds.center.x, 2, 0);
                float scale = 3.3f / platform.bounds.size.x; r.transform.localScale = new Vector3(scale, scale, 1);
            }
            GameObject[] gates = index == 0 ? new[] { ((MossTuskBoss)boss).entranceGate, ((MossTuskBoss)boss).exitGate } : new[] { ((SporeCrownBoss)boss).entranceGate, ((SporeCrownBoss)boss).exitGate };
            var gateSprite = Prop(props, index, false);
            foreach (var gate in gates)
            {
                var previous = gate.transform.Find("Root Barricade Artwork V2");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var r = Make("Root Barricade Artwork V2", gate.transform, gateSprite, "build", 18);
                r.transform.position = new Vector3(gate.transform.position.x, 0, -.1f);
                r.transform.localScale = new Vector3(1.4f / gateSprite.bounds.size.x, 8 / gateSprite.bounds.size.y, 1);
            }
            if (index == 0) boss.GetComponent<MossBossHud>().useTopLayout = true;
            else boss.GetComponent<SporeCrownHud>().useTopLayout = true;
            if (oldColliders != CollisionSnapshot(scene)) throw new InvalidOperationException("Unexpected physics change in " + path);
            if (bossBefore != EditorJsonUtility.ToJson(boss)) throw new InvalidOperationException("Unexpected boss configuration change");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("BossScene_1 and BossScene_2 artwork overwritten; physics and boss configurations unchanged.");
    }
    public static string CollisionSnapshot(Scene scene)
    {
        return string.Join("\n", scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider2D>(true)).Select(c =>
            c.name + "|" + c.GetType().Name + "|" + c.transform.position.ToString("R") + "|" + c.transform.lossyScale.ToString("R") + "|" + c.offset.ToString("R") + "|" + c.enabled + "|" + c.isTrigger + "|" + (c is BoxCollider2D ? ((BoxCollider2D)c).size.ToString("R") : EditorJsonUtility.ToJson(c))).ToArray());
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
        int left = index * texture.width / 2, right = (index + 1) * texture.width / 2;
        int bottom = platform ? Mathf.RoundToInt(texture.height * (1 - 330 / 1024f)) : 0;
        int top = platform ? texture.height : Mathf.RoundToInt(texture.height * (1 - 330 / 1024f));
        var pixels = texture.GetPixels32(); int minX = right, minY = top, maxX = left, maxY = bottom;
        for (int y = bottom; y < top; y++) for (int x = left; x < right; x++)
            if (pixels[y * texture.width + x].a > 150) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
        float w = maxX - minX + 1, h = maxY - minY + 1;
        float foot = platform ? texture.height * (1 - 142 / 1024f) : minY;
        var sprite = Sprite.Create(texture, new Rect(minX, minY, w, h), new Vector2(.5f, (foot - minY) / h), 100, 0, SpriteMeshType.FullRect);
        sprite.name = (index == 0 ? "Forest" : "Spore") + (platform ? "PlatformV2" : "RootGateV2");
        string path = Folder + sprite.name + ".asset"; var old = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (old == null) { AssetDatabase.CreateAsset(sprite, path); return sprite; }
        EditorUtility.CopySerialized(sprite, old); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(old); return old;
    }
}
