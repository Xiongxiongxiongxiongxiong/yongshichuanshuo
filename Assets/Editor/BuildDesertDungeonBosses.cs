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

public static class BuildDesertDungeonBosses
{
    public const string Folder = "Assets/GeneratedBoss/DesertDungeon/";
    public static readonly string[] Names = { "SunburialScorpion", "SteleLizard", "IroncoffinJailer", "SoulLantern" };
    public static string ScenePath(int index) => "Assets/Scenes/BossScene_" + (index + 6) + ".unity";
    public static string DataPath(int index) => "Assets/DataSO/GameScene/Game BossScene" + (index + 6) + ".asset";
    [MenuItem("Tools/Boss/Build Desert and Dungeon Bosses")]
    public static void BuildAll()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save existing scene edits first.");
        var effects = Slice("EncounterEffects", -1);
        for (int i = 0; i < 4; i++) if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath(i)) == null) Build(i, effects);
        var persistent = SceneManager.GetSceneByName("persistent"); bool opened = !persistent.isLoaded;
        if (opened) persistent = EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Additive);
        foreach (var loader in persistent.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneLoader>(true)))
        {
            var serialized = new SerializedObject(loader); var p = serialized.FindProperty("additionalBossScenes");
            var current = Enumerable.Range(0, p.arraySize).Select(x => p.GetArrayElementAtIndex(x).objectReferenceValue).Where(x => x != null).ToList();
            for (int i = 0; i < 4; i++) { var data = AssetDatabase.LoadAssetAtPath<GameSceneSO>(DataPath(i)); if (!current.Contains(data)) current.Add(data); }
            p.arraySize = current.Count;
            for (int i = 0; i < current.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = current[i];
            serialized.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent); AssetDatabase.SaveAssets();
        if (opened) EditorSceneManager.CloseScene(persistent, true);
        Debug.Log("Four independent desert/dungeon bosses saved: BossScene_6–9.");
    }
    private static void Build(int index, Sprite[] effects)
    {
        var frames = Slice(Names[index], index); bool desert = index < 2;
        var bg = Import(desert ? "DesertArena" : "DungeonArena");
        var source = EditorSceneManager.OpenScene("Assets/Scenes/BossScene_5.unity", OpenSceneMode.Additive);
        var room = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (var root in source.GetRootGameObjects()) { var clone = UnityEngine.Object.Instantiate(root); clone.name = root.name; SceneManager.MoveGameObjectToScene(clone, room); }
        SceneManager.SetActiveScene(room);
        var old = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MoonveilMothBoss>(true)).Single();
        var go = old.gameObject; go.SetActive(false);
        var boss = go.AddComponent<DesertDungeonBoss>(); boss.kind = (DesertDungeonBoss.Kind)index; boss.frames = frames; boss.effectFrames = effects;
        boss.damageSource = old.damageSource; boss.entranceGate = old.entranceGate; boss.exitGate = old.exitGate;
        boss.arenaCamera = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Cinemachine.CinemachineVirtualCamera>(true)).Single();
        boss.effectRoot = old.effectRoot;
        boss.completionUI = room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelCompleteUI>(true)).Single();
        var hud = go.AddComponent<DesertDungeonBossHud>(); var oldHud = go.GetComponent<MoonveilMothHud>();
        hud.backgroundSprite = oldHud.backgroundSprite; hud.healthSprite = oldHud.healthSprite; hud.trailSprite = oldHud.trailSprite;
        UnityEngine.Object.DestroyImmediate(oldHud); UnityEngine.Object.DestroyImmediate(old);
        go.name = Names[index]; go.transform.root.name = "Boss Arena - " + Names[index];
        go.transform.position = new Vector3(3, .02f, 0); go.GetComponent<SpriteRenderer>().sprite = frames[0];
        var hp = go.GetComponent<Character>(); hp.maxHp = hp.CurrentHp = new[] { 1200, 1400, 1500, 1100 }[index];
        var box = go.GetComponent<BoxCollider2D>(); box.size = new Vector2(desert ? 3.4f : 2, desert ? 1.9f : 3); box.offset = new Vector2(0, box.size.y * .5f);
        // Keep the proven arena collision/lighting/camera; replace its cave artwork only.
        foreach (var map in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TilemapRenderer>(true))) map.enabled = false;
        foreach (var r in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).Where(r => r.name.Contains("Background"))) r.gameObject.SetActive(false);
        var backdrop = new GameObject("Generated Arena Background"); SceneManager.MoveGameObjectToScene(backdrop, room);
        var sr = backdrop.AddComponent<SpriteRenderer>(); sr.sprite = bg; sr.sortingOrder = -50;
        backdrop.transform.position = new Vector3(0, 4.9f, 10); backdrop.transform.localScale = new Vector3(38 / bg.bounds.size.x, 20 / bg.bounds.size.y, 1);
        var slab = Sprite.Create(bg.texture, new Rect(bg.texture.width * .02f, bg.texture.height * .08f, bg.texture.width * .1f, bg.texture.height * .06f), new Vector2(.5f, 0), 100);
        slab.name = Names[index] + "PlatformStone"; AssetDatabase.CreateAsset(slab, Folder + slab.name + ".asset");
        foreach (var platform in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r => r.name.Contains("Raised Platform")))
        {
            // Match the original collider top at y=2 exactly; surface is visibly solid.
            platform.sprite = slab; platform.color = Color.white; platform.drawMode = SpriteDrawMode.Simple;
            platform.transform.position = new Vector3(platform.transform.position.x, 1.3f, 0);
            platform.transform.localScale = new Vector3(3 / slab.bounds.size.x, .7f / slab.bounds.size.y, 1);
            var c = platform.GetComponent<BoxCollider2D>(); c.size = new Vector2(slab.bounds.size.x, slab.bounds.size.y); c.offset = new Vector2(0, slab.bounds.size.y * .5f);
        }
        foreach (var gate in new[] { boss.entranceGate, boss.exitGate })
            foreach (var r in gate.GetComponentsInChildren<SpriteRenderer>(true)) { r.sprite = slab; r.color = desert ? new Color(.8f, .55f, .35f) : new Color(.4f, .55f, .65f); }
        foreach (var label in room.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)))
            if (label.name == "Goal Label") label.text = boss.Title;
        go.SetActive(true); EditorSceneManager.SaveScene(room, ScenePath(index));
        var data = ScriptableObject.CreateInstance<GameSceneSO>(); data.sceneType = SceneType.loaction;
        data.sceneReference = new AssetReference(AssetDatabase.AssetPathToGUID(ScenePath(index))); AssetDatabase.CreateAsset(data, DataPath(index));
        boss.completionUI.retryScene = data; boss.completionUI.retryPosition = new Vector3(-11, .05f, 0);
        var settings = AddressableAssetSettingsDefaultObject.Settings; var group = settings.FindGroup("Scenes");
        settings.CreateOrMoveEntry(data.sceneReference.AssetGUID, group).address = "BossScene_" + (index + 6);
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(group); EditorSceneManager.MarkSceneDirty(room); EditorSceneManager.SaveScene(room);
        EditorSceneManager.CloseScene(room, true); EditorSceneManager.CloseScene(source, true);
    }
    private static Sprite Import(string name)
    {
        string path = Folder + name + ".png"; var i = (TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType = TextureImporterType.Sprite; i.spriteImportMode = SpriteImportMode.Single; i.isReadable = true; i.alphaIsTransparency = true;
        i.filterMode = FilterMode.Point; i.textureCompression = TextureImporterCompression.Uncompressed; i.mipmapEnabled = false; i.maxTextureSize = 2048; i.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static Sprite[] Slice(string name, int index)
    {
        var texture = Import(name).texture; var pixels = texture.GetPixels32();
        int[][] bands = {
            new[] {0,215,402,591,800,966,1150,1343,1536},
            new[] {0,182,360,549,787,971,1164,1349,1536},
            new[] {0,187,378,573,777,961,1155,1349,1536},
            new[] {0,191,383,577,777,970,1162,1355,1536}
        };
        string folder = Folder + name + "Frames";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Folder.TrimEnd('/'), name + "Frames");
        var result = new Sprite[32]; float actorPpu = 0;
        for (int row = 0; row < 8; row++) for (int col = 0; col < 4; col++)
        {
            int bottom = index < 0 ? texture.height * (7 - row) / 8 : texture.height - bands[index][row + 1];
            int top = index < 0 ? texture.height * (8 - row) / 8 : texture.height - bands[index][row];
            int left = texture.width * col / 4, right = texture.width * (col + 1) / 4;
            int minX = right, maxX = left, minY = top, maxY = bottom;
            for (int y = bottom; y < top; y++) for (int x = left; x < right; x++)
                if (pixels[y * texture.width + x].a > 100) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
            float w = maxX - minX + 1, h = maxY - minY + 1;
            if (w <= 0 || h <= 0) throw new InvalidOperationException("Empty frame " + name + ":" + row + ":" + col);
            if (actorPpu <= 0) actorPpu = index < 2 ? w / 5 : h / 4;
            float ppu = index < 0 ? w / 2 : actorPpu;
            Vector2 pivot = new Vector2(index < 0 ? .5f : ((left + right) * .5f - minX) / w, 0);
            var sprite = Sprite.Create(texture, new Rect(minX, minY, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name + "_" + row + "_" + col; string path = folder + "/" + sprite.name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (old == null) { AssetDatabase.CreateAsset(sprite, path); result[row * 4 + col] = sprite; }
            else { EditorUtility.CopySerialized(sprite, old); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(old); result[row * 4 + col] = old; }
        }
        return result;
    }
}
