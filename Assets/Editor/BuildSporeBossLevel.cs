using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public static class BuildSporeBossLevel
{
    public const string ScenePath = "Assets/Scenes/BossScene_2.unity";
    private const string Folder = "Assets/GeneratedBoss/SecondBoss/";

    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return "BossScene_2 already exists; preserved.";
        var frames = Slice("SporeCrownElder", 8, 48);
        var effects = Slice("SporeCrownEffects", 4, 64);
        var source = SceneManager.GetSceneByName("BossScene_1");
        bool openedSource = !source.isLoaded;
        if (openedSource) source = EditorSceneManager.OpenScene("Assets/Scenes/BossScene_1.unity", OpenSceneMode.Additive);
        var room = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (var original in source.GetRootGameObjects())
        {
            var clone = UnityEngine.Object.Instantiate(original);
            clone.name = original.name; SceneManager.MoveGameObjectToScene(clone, room);
        }
        SceneManager.SetActiveScene(room);
        var old = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MossTuskBoss>(true)).Single();
        var go = old.gameObject; go.SetActive(false);
        var oldHud = go.GetComponent<MossBossHud>();
        var boss = go.AddComponent<SporeCrownBoss>();
        boss.frames = frames; boss.effectFrames = effects; boss.damageSource = old.damageSource;
        boss.meleeFrames = ImportSporeMelee.LoadFrames();
        boss.entranceGate = old.entranceGate; boss.exitGate = old.exitGate;
        boss.arenaCamera = old.arenaCamera; boss.effectRoot = old.effectRoot;
        boss.completionUI = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelCompleteUI>(true)).Single();
        boss.groundLayer = old.groundLayer;
        var hud = go.AddComponent<SporeCrownHud>();
        hud.backgroundSprite = oldHud.backgroundSprite; hud.healthSprite = oldHud.healthSprite; hud.trailSprite = oldHud.trailSprite;
        UnityEngine.Object.DestroyImmediate(oldHud); UnityEngine.Object.DestroyImmediate(old);
        go.name = "Spore Crown Elder"; go.transform.position = new Vector3(3, 0.02f, 0);
        var body = go.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
        var box = go.GetComponent<BoxCollider2D>(); box.size = new Vector2(3.5f, 3.2f); box.offset = new Vector2(0, 1.6f);
        var health = go.GetComponent<Character>(); health.maxHp = health.CurrentHp = 800;
        go.GetComponent<SpriteRenderer>().sprite = frames[0];
        foreach (var renderer in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true)))
            if (renderer.name.Contains("Forest Background")) renderer.color = new Color(0.7f, 0.78f, 0.64f, 1);
        foreach (var label in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)))
            if (label.name == "Goal Label") label.text = "幽谷菌主的领地";
        foreach (var goal in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelGoal>(true)))
        {
            goal.completionUI = boss.completionUI;
            goal.GetComponent<Collider2D>().enabled = false;
        }
        go.transform.root.name = "Boss Level - Spore Crown Hollow";
        go.SetActive(true);
        EditorSceneManager.SaveScene(room, ScenePath);
        var data = ScriptableObject.CreateInstance<GameSceneSO>(); data.sceneType = SceneType.loaction;
        data.sceneReference = new AssetReference(AssetDatabase.AssetPathToGUID(ScenePath));
        AssetDatabase.CreateAsset(data, "Assets/DataSO/GameScene/Game BossScene2.asset");
        boss.completionUI.retryScene = data; boss.completionUI.retryPosition = new Vector3(-11, 0.05f, 0);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("Scenes");
        settings.CreateOrMoveEntry(data.sceneReference.AssetGUID, group).address = "BossScene_2";
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(group);
        var exploration = SceneManager.GetSceneByName("SampleScene_8");
        if (!exploration.isLoaded) exploration = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_8.unity", OpenSceneMode.Additive);
        var exit = exploration.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelGoal>(true)).Single();
        var exitGo = exit.gameObject; UnityEngine.Object.DestroyImmediate(exit);
        var entrance = exitGo.AddComponent<BossLevelEntrance>(); entrance.destination = data;
        entrance.spawnPosition = new Vector3(-11, 0.05f, 0); exitGo.name = "Second Boss Entrance";
        foreach (var label in exploration.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)))
            if (label.name.Contains("Goal")) label.text = "进入幽谷菌主领地";
        var persistent = SceneManager.GetSceneByName("persistent");
        bool openedPersistent = !persistent.isLoaded;
        if (openedPersistent) persistent = EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Additive);
        foreach (var loader in persistent.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneLoader>(true)))
        {
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("secondBossLevelScene").objectReferenceValue = data;
            serialized.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
        EditorSceneManager.MarkSceneDirty(exploration); EditorSceneManager.SaveScene(exploration);
        EditorSceneManager.MarkSceneDirty(room); EditorSceneManager.SaveScene(room);
        AssetDatabase.SaveAssets();
        if (openedPersistent) EditorSceneManager.CloseScene(persistent, true);
        if (openedSource) EditorSceneManager.CloseScene(source, true);
        return "BossScene_2 built; 32 boss frames, 16 effect frames, canyon entrance and retry connected.";
    }

    private static Sprite[] Slice(string name, int rows, float ppu)
    {
        string path = Folder + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing texture: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int width = texture.width / 4, height = texture.height / rows;
        string frameFolder = Folder + name + "Frames";
        if (!AssetDatabase.IsValidFolder(frameFolder)) AssetDatabase.CreateFolder(Folder.TrimEnd('/'), name + "Frames");
        var result = new Sprite[rows * 4];
        for (int row = 0; row < rows; row++) for (int col = 0; col < 4; col++)
        {
            int x0 = col * width;
            if (rows == 4)
            {
                // Generated VFX uses unequal row heights; preserve all four full animations.
                int[] edges = { 0, 520, 750, 950, 1280 };
                height = Mathf.RoundToInt((edges[row + 1] - edges[row]) * texture.height / 1280f);
            }
            int y0 = rows == 8 ? texture.height - (row + 1) * height :
                texture.height - Mathf.RoundToInt(new[] { 520, 750, 950, 1280 }[row] * texture.height / 1280f);
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                if (texture.GetPixel(x0 + x, y0 + y).a < 0.1f) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
            if (maxX < 0) throw new InvalidOperationException("Empty frame " + row + ":" + col);
            // Preserve the cell gutters, calibrating the pivot to the visible foot baseline.
            float center = (minX + maxX + 1) * 0.5f;
            var rect = new Rect(x0, y0, width, height);
            var pivot = new Vector2(center / width, (float)minY / height);
            if (rows == 4 && row == 1) pivot.y = (minY + maxY + 1) * 0.5f / height;
            float actualPpu = rows == 4 ? new[] { 280f, 400f, 100f, 200f }[row] * texture.width / 1280f : ppu;
            var sprite = Sprite.Create(texture, rect, pivot, actualPpu, 0, SpriteMeshType.FullRect);
            sprite.name = name + "_" + row + "_" + col;
            string spritePath = frameFolder + "/" + sprite.name + ".asset";
            AssetDatabase.CreateAsset(sprite, spritePath); result[row * 4 + col] = sprite;
        }
        return result;
    }
}
