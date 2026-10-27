using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BuildCrystalbackBossLevel
{
    public const string ScenePath = "Assets/Scenes/BossScene_3.unity";
    private const string Folder = "Assets/GeneratedBoss/ThirdBoss/";
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return "BossScene_3 exists; preserved.";
        var frames = Slice("CrystalbackBeast", 8);
        var effects = Slice("CrystalbackEffects", 4);
        string bgPath = Folder + "MineArenaBackground.png";
        ConfigureTexture(bgPath);
        var source = SceneManager.GetSceneByName("BossScene_2");
        bool openedSource = !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene("Assets/Scenes/BossScene_2.unity", OpenSceneMode.Additive);
        var room = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (var original in source.GetRootGameObjects())
        {
            var clone = UnityEngine.Object.Instantiate(original); clone.name = original.name;
            SceneManager.MoveGameObjectToScene(clone, room);
        }
        SceneManager.SetActiveScene(room);
        var old = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SporeCrownBoss>(true)).Single();
        var go = old.gameObject; go.SetActive(false);
        var oldHud = go.GetComponent<SporeCrownHud>();
        var boss = go.AddComponent<CrystalbackBoss>(); boss.frames = frames; boss.effectFrames = effects;
        boss.damageSource = old.damageSource; boss.entranceGate = old.entranceGate; boss.exitGate = old.exitGate;
        boss.arenaCamera = old.arenaCamera; boss.effectRoot = old.effectRoot; boss.groundLayer = old.groundLayer;
        boss.completionUI = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelCompleteUI>(true)).Single();
        var hud = go.AddComponent<CrystalbackHud>();
        hud.backgroundSprite = oldHud.backgroundSprite; hud.healthSprite = oldHud.healthSprite; hud.trailSprite = oldHud.trailSprite;
        UnityEngine.Object.DestroyImmediate(oldHud); UnityEngine.Object.DestroyImmediate(old);
        go.name = "Crystalback Burrow Beast"; go.transform.root.name = "Boss Level - Abandoned Amber Mine";
        go.transform.position = new Vector3(3, 0.02f, 0); go.GetComponent<SpriteRenderer>().sprite = frames[0];
        var health = go.GetComponent<Character>(); health.maxHp = health.CurrentHp = 1000;
        var box = go.GetComponent<BoxCollider2D>(); box.size = new Vector2(4.2f, 2.8f); box.offset = new Vector2(0, 1.4f);
        foreach (var map in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)))
        {
            var rock = map.GetTile(new Vector3Int(0, -2, 0));
            if (rock == null) continue;
            foreach (var cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) map.SetTile(cell, rock);
            map.color = new Color(0.62f, 0.58f, 0.65f);
            foreach (var platform in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r => r.name.Contains("Raised Platform")))
            { platform.sprite = map.GetSprite(new Vector3Int(0, -2, 0)); platform.color = new Color(0.62f, 0.58f, 0.65f); }
        }
        ApplyMineBackdrop(room);
        foreach (var gate in new[] { boss.entranceGate, boss.exitGate })
        {
            foreach (var pulse in gate.GetComponentsInChildren<MossBossRuneSeal>(true))
            {
                var sr = pulse.GetComponent<SpriteRenderer>();
                UnityEngine.Object.DestroyImmediate(pulse);
                sr.color = Color.white;
                // A narrow, static amber crystal seal reuses the falling-crystal silhouette.
                sr.sprite = effects[5]; sr.transform.localScale = new Vector3(4, 4.5f, 1);
                sr.transform.position = new Vector3(gate.transform.position.x, 2.3f, -0.1f);
            }
        }
        foreach (var label in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)))
            if (label.name == "Goal Label") label.text = "废弃矿井 · 矿脉巢穴";
        foreach (var goal in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelGoal>(true)))
        { goal.completionUI = boss.completionUI; goal.GetComponent<Collider2D>().enabled = false; }
        go.SetActive(true); EditorSceneManager.SaveScene(room, ScenePath);
        var data = ScriptableObject.CreateInstance<GameSceneSO>(); data.sceneType = SceneType.loaction;
        data.sceneReference = new AssetReference(AssetDatabase.AssetPathToGUID(ScenePath));
        AssetDatabase.CreateAsset(data, "Assets/DataSO/GameScene/Game BossScene3.asset");
        boss.completionUI.retryScene = data; boss.completionUI.retryPosition = new Vector3(-11, 0.05f, 0);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("Scenes");
        settings.CreateOrMoveEntry(data.sceneReference.AssetGUID, group).address = "BossScene_3";
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(group);
        var persistent = SceneManager.GetSceneByName("persistent"); bool openedPersistent = !persistent.isLoaded;
        if (openedPersistent) persistent = EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Additive);
        foreach (var loader in persistent.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneLoader>(true)))
        {
            var serialized = new SerializedObject(loader); serialized.FindProperty("thirdBossLevelScene").objectReferenceValue = data;
            serialized.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
        EditorSceneManager.MarkSceneDirty(room); EditorSceneManager.SaveScene(room); AssetDatabase.SaveAssets();
        if (openedPersistent) EditorSceneManager.CloseScene(persistent, true);
        if (openedSource) EditorSceneManager.CloseScene(source, true);
        return "BossScene_3 created with mine backdrop, Crystalback boss, checkpoint and retry. Future exploration entrance can reference Game BossScene3.";
    }

    public static void ApplyMineBackdrop(Scene room)
    {
        var backgrounds = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
            .Where(r => r.name.Contains("Forest Background")).ToArray();
        var existing = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(r => r.name == "Mine Arena Background");
        if (existing == null)
        {
            var go = new GameObject("Mine Arena Background"); SceneManager.MoveGameObjectToScene(go, room);
            existing = go.AddComponent<SpriteRenderer>();
            if (backgrounds.Length > 0)
            {
                go.transform.SetParent(backgrounds[0].transform.parent, true);
                existing.sortingLayerID = backgrounds[0].sortingLayerID; existing.sortingOrder = backgrounds[0].sortingOrder;
            }
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "MineArenaBackground.png");
        existing.sprite = sprite; existing.color = Color.white;
        existing.transform.position = new Vector3(0, 4.5f, 10);
        existing.transform.localScale = new Vector3(38f / sprite.bounds.size.x, 22f / sprite.bounds.size.y, 1);
        foreach (var background in backgrounds) background.gameObject.SetActive(false);
    }

    private static void ConfigureTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing image: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
    }
    private static Sprite[] Slice(string name, int rows)
    {
        string path = Folder + name + ".png"; ConfigureTexture(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int width = texture.width / 4;
        var result = new Sprite[rows * 4];
        string folder = Folder + name + "Frames";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Folder.TrimEnd('/'), name + "Frames");
        for (int row = 0; row < rows; row++) for (int col = 0; col < 4; col++)
        {
            // Generated character rows are not evenly spaced; use measured transparent gutters.
            int[] edges = rows == 8 ? new[] { 0, 178, 359, 544, 708, 911, 1119, 1327, 1536 } : new[] { 0, 320, 660, 920, 1280 };
            float divisor = rows == 8 ? 1536 : 1280;
            int top = Mathf.RoundToInt(edges[row] * texture.height / divisor);
            int bottom = Mathf.RoundToInt(edges[row + 1] * texture.height / divisor);
            int[][] bossColumns = {
                new[] { 0, 253, 509, 767, 1024 }, new[] { 0, 258, 514, 774, 1024 },
                new[] { 0, 258, 531, 818, 1024 }, new[] { 0, 250, 529, 780, 1024 },
                new[] { 0, 250, 520, 786, 1024 }, new[] { 0, 261, 480, 783, 1024 },
                new[] { 0, 260, 502, 766, 1024 }, new[] { 0, 251, 506, 761, 1024 } };
            int[][] effectColumns = { new[] { 0, 269, 567, 899, 1254 }, new[] { 0, 303, 609, 920, 1254 },
                new[] { 0, 302, 615, 951, 1254 }, new[] { 0, 301, 612, 926, 1254 } };
            var columns = rows == 8 ? bossColumns[row] : effectColumns[row];
            float columnScale = texture.width / (rows == 8 ? 1024f : 1254f);
            int x0 = Mathf.RoundToInt(columns[col] * columnScale);
            width = Mathf.RoundToInt(columns[col + 1] * columnScale) - x0;
            int y0 = texture.height - bottom, h = bottom - top;
            int minX = width, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++) for (int x = 0; x < width; x++)
            {
                if (texture.GetPixel(x0 + x, y0 + y).a < 0.1f) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
            if (maxX < 0) throw new InvalidOperationException("Empty frame: " + row + ":" + col);
            float pivotX = rows == 8 ? ((col + 0.5f) * texture.width / 4 - x0) / width : (minX + maxX + 1) * 0.5f / width;
            float pivotY = rows == 4 && row == 1 ? (minY + maxY + 1) * 0.5f / h : (float)minY / h;
            float ppu = rows == 8 ? 44f * texture.width / 1024f : new[] { 210f, 260f, 105f, 170f }[row] * texture.width / 1280f;
            var sprite = Sprite.Create(texture, new Rect(x0, y0, width, h), new Vector2(pivotX, pivotY), ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name + "_" + row + "_" + col;
            AssetDatabase.CreateAsset(sprite, folder + "/" + sprite.name + ".asset"); result[row * 4 + col] = sprite;
        }
        return result;
    }
}
