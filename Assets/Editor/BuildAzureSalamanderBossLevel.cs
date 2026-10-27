using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BuildAzureSalamanderBossLevel
{
    public const string ScenePath = "Assets/Scenes/BossScene_4.unity";
    private const string Folder = "Assets/GeneratedBoss/WildernessCaveBoss/";
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return "BossScene_4 exists; preserved.";
        var frames = Slice("AzureSalamander", 8);
        var effects = Slice("AzureSalamanderEffects", 4);
        ConfigureTexture(Folder + "NaturalCaveBackground.png");
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "NaturalCaveBackground.png");
        var source = SceneManager.GetSceneByName("BossScene_3"); bool openedSource = !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene("Assets/Scenes/BossScene_3.unity", OpenSceneMode.Additive);
        var room = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (var original in source.GetRootGameObjects())
        { var clone = UnityEngine.Object.Instantiate(original); clone.name = original.name; SceneManager.MoveGameObjectToScene(clone, room); }
        SceneManager.SetActiveScene(room);
        var old = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CrystalbackBoss>(true)).Single();
        var go = old.gameObject; go.SetActive(false);
        var oldHud = go.GetComponent<CrystalbackHud>();
        var boss = go.AddComponent<AzureSalamanderBoss>(); boss.frames = frames; boss.effectFrames = effects;
        boss.damageSource = old.damageSource; boss.entranceGate = old.entranceGate; boss.exitGate = old.exitGate;
        boss.arenaCamera = old.arenaCamera; boss.effectRoot = old.effectRoot; boss.groundLayer = old.groundLayer;
        boss.completionUI = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelCompleteUI>(true)).Single();
        var hud = go.AddComponent<AzureSalamanderHud>();
        hud.backgroundSprite = oldHud.backgroundSprite; hud.healthSprite = oldHud.healthSprite; hud.trailSprite = oldHud.trailSprite;
        UnityEngine.Object.DestroyImmediate(oldHud); UnityEngine.Object.DestroyImmediate(old);
        go.name = "Azure Ridge Cave Salamander"; go.transform.root.name = "Boss Level - Wilderness Moonpool Grotto";
        go.transform.position = new Vector3(3, 0.02f, 0); go.GetComponent<SpriteRenderer>().sprite = frames[0];
        var health = go.GetComponent<Character>(); health.maxHp = health.CurrentHp = 1200;
        var box = go.GetComponent<BoxCollider2D>(); box.size = new Vector2(4.2f, 2.2f); box.offset = new Vector2(0, 1.1f);
        Sprite rock = null;
        foreach (var map in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)))
        { map.color = new Color(0.58f, 0.72f, 0.73f); if (rock == null) rock = map.GetSprite(new Vector3Int(0, -2, 0)); }
        foreach (var platform in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r => r.name.Contains("Raised Platform")))
            platform.color = new Color(0.58f, 0.72f, 0.73f);
        var backgrounds = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => r.name.Contains("Background")).ToArray();
        foreach (var renderer in backgrounds) renderer.gameObject.SetActive(false);
        var bgGo = new GameObject("Natural Cave Background"); SceneManager.MoveGameObjectToScene(bgGo, room);
        bgGo.transform.SetParent(go.transform.root, true); var bg = bgGo.AddComponent<SpriteRenderer>(); bg.sprite = bgSprite;
        if (backgrounds.Length > 0) { bg.sortingLayerID = backgrounds[0].sortingLayerID; bg.sortingOrder = backgrounds[0].sortingOrder; }
        bgGo.transform.position = new Vector3(0, 4.5f, 10);
        bgGo.transform.localScale = new Vector3(38f / bgSprite.bounds.size.x, 22f / bgSprite.bounds.size.y, 1);
        foreach (var gate in new[] { boss.entranceGate, boss.exitGate })
        {
            foreach (var sr in gate.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var pulse = sr.GetComponent<MossBossRuneSeal>(); if (pulse != null) UnityEngine.Object.DestroyImmediate(pulse);
                sr.sprite = rock; sr.color = new Color(0.55f, 0.72f, 0.76f); sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(0.85f, 4.5f); sr.transform.localScale = Vector3.one;
                sr.transform.position = new Vector3(gate.transform.position.x, 2.25f, -0.1f);
            }
        }
        foreach (var label in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)))
            if (label.name == "Goal Label") label.text = "天然幽潭 · 岩螈巢穴";
        foreach (var goal in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelGoal>(true)))
        { goal.completionUI = boss.completionUI; goal.GetComponent<Collider2D>().enabled = false; }
        go.SetActive(true); EditorSceneManager.SaveScene(room, ScenePath);
        var data = ScriptableObject.CreateInstance<GameSceneSO>(); data.sceneType = SceneType.loaction;
        data.sceneReference = new AssetReference(AssetDatabase.AssetPathToGUID(ScenePath));
        AssetDatabase.CreateAsset(data, "Assets/DataSO/GameScene/Game BossScene4.asset");
        boss.completionUI.retryScene = data; boss.completionUI.retryPosition = new Vector3(-11, 0.05f, 0);
        var settings = AddressableAssetSettingsDefaultObject.Settings; var group = settings.FindGroup("Scenes");
        settings.CreateOrMoveEntry(data.sceneReference.AssetGUID, group).address = "BossScene_4";
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(group);
        var persistent = SceneManager.GetSceneByName("persistent"); bool openedPersistent = !persistent.isLoaded;
        if (openedPersistent) persistent = EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Additive);
        foreach (var loader in persistent.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneLoader>(true)))
        {
            var serialized = new SerializedObject(loader); serialized.FindProperty("caveBossLevelScene").objectReferenceValue = data;
            serialized.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
        EditorSceneManager.MarkSceneDirty(room); EditorSceneManager.SaveScene(room); AssetDatabase.SaveAssets();
        if (openedPersistent) EditorSceneManager.CloseScene(persistent, true);
        if (openedSource) EditorSceneManager.CloseScene(source, true);
        return "BossScene_4 saved with natural cave backdrop, salamander boss and retry/checkpoint data.";
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
    private static List<Vector2Int> Groups(bool[] occupied, int expected)
    {
        var groups = new List<Vector2Int>(); int start = -1;
        for (int i = 0; i <= occupied.Length; i++)
        {
            bool active = i < occupied.Length && occupied[i];
            if (active && start < 0) start = i;
            if (!active && start >= 0) { if (i - start >= 4) groups.Add(new Vector2Int(start, i)); start = -1; }
        }
        while (groups.Count > expected)
        {
            int nearest = 0, gap = int.MaxValue;
            for (int i = 0; i < groups.Count - 1; i++) if (groups[i + 1].x - groups[i].y < gap) { nearest = i; gap = groups[i + 1].x - groups[i].y; }
            groups[nearest] = new Vector2Int(groups[nearest].x, groups[nearest + 1].y); groups.RemoveAt(nearest + 1);
        }
        if (groups.Count != expected) throw new InvalidOperationException("Animation sheet has " + groups.Count + " separated groups; expected " + expected + ". Inspect source gutters.");
        return groups;
    }
    private static Sprite[] Slice(string name, int rows)
    {
        string path = Folder + name + ".png"; ConfigureTexture(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); var pixels = texture.GetPixels32();
        var occupiedRows = new bool[texture.height];
        for (int y = 0; y < texture.height; y++) { int count = 0; for (int x = 0; x < texture.width; x++) if (pixels[y * texture.width + x].a > 80) count++; occupiedRows[y] = count > 4; }
        var rowGroups = Groups(occupiedRows, rows); rowGroups.Reverse();
        string frameFolder = Folder + name + "Frames";
        if (!AssetDatabase.IsValidFolder(frameFolder)) AssetDatabase.CreateFolder(Folder.TrimEnd('/'), name + "Frames");
        var result = new Sprite[rows * 4]; float actorPpu = 0;
        for (int row = 0; row < rows; row++)
        {
            int bottom = rowGroups[row].x, top = rowGroups[row].y;
            var occupiedColumns = new bool[texture.width];
            for (int x = 0; x < texture.width; x++) { int count = 0; for (int y = bottom; y < top; y++) if (pixels[y * texture.width + x].a > 80) count++; occupiedColumns[x] = count > 2; }
            // Breath wisps bridge two character cells; use their measured frame gutters for that row.
            var columns = rows == 8 && row == 5
                ? new List<Vector2Int> { new Vector2Int(0, Mathf.RoundToInt(texture.width * 245f / 1024)),
                    new Vector2Int(Mathf.RoundToInt(texture.width * 245f / 1024), Mathf.RoundToInt(texture.width * 513f / 1024)),
                    new Vector2Int(Mathf.RoundToInt(texture.width * 513f / 1024), Mathf.RoundToInt(texture.width * 773f / 1024)),
                    new Vector2Int(Mathf.RoundToInt(texture.width * 773f / 1024), texture.width) }
                : Groups(occupiedColumns, 4);
            for (int col = 0; col < 4; col++)
            {
                int left = columns[col].x, right = columns[col].y;
                int minY = top, maxY = bottom, minX = right, maxX = left;
                for (int y = bottom; y < top; y++) for (int x = left; x < right; x++)
                    if (pixels[y * texture.width + x].a > 80) { minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); }
                float w = maxX - minX + 1, h = maxY - minY + 1;
                if (w <= 0 || h <= 0) throw new InvalidOperationException("Empty sprite.");
                if (actorPpu <= 0) actorPpu = h / 2.8f;
                float anchor = 0.5f;
                if (rows == 8)
                {
                    int feetMin = maxX, feetMax = minX;
                    for (int y = minY; y <= Mathf.Min(maxY, minY + Mathf.Max(2, (int)(h * 0.06f))); y++) for (int x = minX; x <= maxX; x++)
                        if (pixels[y * texture.width + x].a > 80) { feetMin = Mathf.Min(feetMin, x); feetMax = Mathf.Max(feetMax, x); }
                    if (feetMax >= feetMin) anchor = ((feetMin + feetMax + 1) * 0.5f - minX) / w;
                }
                var pivot = rows == 4 && row == 0 ? new Vector2(0, 0.5f) : new Vector2(anchor, 0);
                float ppu = rows == 8 ? actorPpu : row == 2 ? w / 3 : w / 2.5f;
                var sprite = Sprite.Create(texture, new Rect(minX, minY, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
                sprite.name = name + "_" + row + "_" + col;
                string spritePath = frameFolder + "/" + sprite.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (existing == null) { AssetDatabase.CreateAsset(sprite, spritePath); result[row * 4 + col] = sprite; }
                else { EditorUtility.CopySerialized(sprite, existing); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(existing); result[row * 4 + col] = existing; }
            }
        }
        return result;
    }
}
