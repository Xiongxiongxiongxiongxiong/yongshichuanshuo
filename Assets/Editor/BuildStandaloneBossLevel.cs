using System;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BuildStandaloneBossLevel
{
    public const string ScenePath = "Assets/Scenes/BossScene_1.unity";
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return "BossScene_1 already exists.";
        var sourceScene = SceneManager.GetSceneByName("SampleScene_7");
        if (!sourceScene.isLoaded) sourceScene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_7.unity", OpenSceneMode.Additive);
        var sourceBoss = sourceScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MossTuskBoss>(true)).First();
        var sourceMap = sourceScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)).First(t => t.name == "map");
        var oldGoal = sourceBoss.exitGoal;
        var roomScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var room = UnityEngine.Object.Instantiate(sourceBoss.transform.root.gameObject);
        room.name = "Boss Level - Ancient Forest"; SceneManager.MoveGameObjectToScene(room, roomScene); room.SetActive(true);
        var boss = room.GetComponentInChildren<MossTuskBoss>(true); boss.gameObject.SetActive(true);
        boss.transform.position = new Vector3(0, 0.02f, 0); boss.transform.localScale = Vector3.one;
        boss.arenaLeft = -15.5f; boss.arenaRight = 15.5f; boss.groundY = 0;
        boss.GetComponent<SpriteRenderer>().flipX = true;
        var floor = Find(room, "Boss Arena Ground"); floor.SetActive(true); floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(36, 1);
        for (int i = 0; i < 2; i++)
        {
            var platform = Find(room, "Arena Raised Platform " + i); platform.SetActive(true);
            platform.transform.position = new Vector3(i == 0 ? -10 : 10, 1.5f, 0);
        }
        var bg = Find(room, "Boss Arena Forest Background"); bg.SetActive(true);
        bg.transform.position = new Vector3(0, 1, 10); bg.transform.localScale = new Vector3(60, 38, 60);
        boss.entranceGate.transform.position = new Vector3(-15.5f, 4, 0);
        boss.exitGate.transform.position = new Vector3(15.5f, 4, 0);
        boss.entranceGate.transform.Find("Rune Seal").position = new Vector3(-15.5f, 0, -0.1f);
        boss.exitGate.transform.Find("Rune Seal").position = new Vector3(15.5f, 0, -0.1f);
        boss.entranceGate.SetActive(false); boss.exitGate.SetActive(false);
        boss.arenaCamera.Follow.position = new Vector3(0, 5, 0);
        boss.arenaCamera.transform.position = new Vector3(0, 5, -10);
        boss.arenaCamera.m_Lens.OrthographicSize = 9.5f; boss.arenaCamera.Priority = -10;

        var grid = new GameObject("Boss Ground Grid", typeof(Grid)); grid.transform.SetParent(room.transform, false);
        var mapGo = new GameObject("Boss Grass And Stone", typeof(Tilemap), typeof(TilemapRenderer)); mapGo.transform.SetParent(grid.transform, false);
        var map = mapGo.GetComponent<Tilemap>(); var renderer = mapGo.GetComponent<TilemapRenderer>();
        renderer.sortingLayerID = sourceMap.GetComponent<TilemapRenderer>().sortingLayerID;
        renderer.sortingOrder = sourceMap.GetComponent<TilemapRenderer>().sortingOrder + 10;
        var grass = sourceMap.GetTile(new Vector3Int(49, 9, 0)); var wall = sourceMap.GetTile(new Vector3Int(49, 8, 0));
        if (grass == null || wall == null) throw new InvalidOperationException("Source terrain tiles missing.");
        for (int x = -18; x < 18; x++) for (int y = -8; y < 0; y++)
            map.SetTile(new Vector3Int(x, y, 0), y == -1 ? grass : wall);

        var uiGo = UnityEngine.Object.Instantiate(sourceBoss.completionUI.gameObject);
        SceneManager.MoveGameObjectToScene(uiGo, roomScene); uiGo.name = "Boss Level Complete Canvas";
        boss.completionUI = uiGo.GetComponent<LevelCompleteUI>(); boss.completionUI.panel.SetActive(false);
        var goalGo = new GameObject("Boss Level Exit", typeof(BoxCollider2D), typeof(LevelGoal));
        goalGo.transform.SetParent(room.transform, false); goalGo.transform.position = new Vector3(13.8f, 1.5f, 0);
        var goalBox = goalGo.GetComponent<BoxCollider2D>(); goalBox.isTrigger = true; goalBox.size = new Vector2(1.5f, 3); goalBox.enabled = false;
        var goal = goalGo.GetComponent<LevelGoal>(); goal.completionUI = boss.completionUI; boss.exitGoal = goal;
        var pedestal = sourceScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Exit Pedestal");
        if (pedestal != null)
        {
            var exit = UnityEngine.Object.Instantiate(pedestal.gameObject, room.transform); exit.name = "Boss Exit Pedestal";
            exit.transform.position = new Vector3(13.8f, 1.1f, 0);
        }
        var labelSource = sourceScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)).FirstOrDefault(t => t.name == "Goal Label");
        if (labelSource != null)
        {
            var label = UnityEngine.Object.Instantiate(labelSource.gameObject, room.transform);
            label.name = "Goal Label"; label.transform.position = new Vector3(-13.2f, 4.5f, 0);
            label.GetComponent<TextMesh>().text = "古林守卫的领地";
        }
        var boundsGo = new GameObject("Boss Camera Bounds", typeof(PolygonCollider2D)); boundsGo.transform.SetParent(room.transform, false);
        boundsGo.tag = "Bounds"; var bounds = boundsGo.GetComponent<PolygonCollider2D>(); bounds.isTrigger = true;
        bounds.points = new[] { new Vector2(-20, -10), new Vector2(-20, 18), new Vector2(20, 18), new Vector2(20, -10) };
        var death = new GameObject("Boss Fall Death Zone", typeof(BoxCollider2D)); death.transform.SetParent(room.transform, false);
        death.tag = "water"; death.transform.position = new Vector3(0, -12, 0);
        death.GetComponent<BoxCollider2D>().isTrigger = true; death.GetComponent<BoxCollider2D>().size = new Vector2(100, 4);
        EditorSceneManager.SaveScene(roomScene, ScenePath);
        string guid = AssetDatabase.AssetPathToGUID(ScenePath);
        var sceneData = ScriptableObject.CreateInstance<GameSceneSO>(); sceneData.sceneType = SceneType.loaction;
        sceneData.sceneReference = new AssetReference(guid); sceneData.name = "Game BossScene1";
        AssetDatabase.CreateAsset(sceneData, "Assets/DataSO/GameScene/Game BossScene1.asset");
        boss.completionUI.retryScene = sceneData;
        boss.completionUI.retryPosition = new Vector3(-11, 0.05f, 0);
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.FindGroup("Scenes"); if (group == null) throw new InvalidOperationException("Scenes Addressables group missing.");
        var entry = settings.CreateOrMoveEntry(guid, group); entry.address = "BossScene_1";
        EditorUtility.SetDirty(settings); EditorUtility.SetDirty(group);

        // The exploration exit now loads this boss room instead of finishing the game.
        var oldEntranceObject = oldGoal.gameObject;
        oldEntranceObject.transform.position = new Vector3(50, 11.5f, 0);
        UnityEngine.Object.DestroyImmediate(oldGoal);
        var entrance = oldEntranceObject.AddComponent<BossLevelEntrance>(); entrance.destination = sceneData;
        entrance.spawnPosition = new Vector3(-11, 0.05f, 0);
        oldEntranceObject.GetComponent<BoxCollider2D>().enabled = true;
        oldEntranceObject.name = "Boss Level Entrance";
        if (labelSource != null)
        { labelSource.text = "进入首领关卡"; labelSource.transform.position = new Vector3(50, 12.5f, 0); }
        sourceBoss.transform.root.gameObject.SetActive(false);
        sourceBoss.transform.root.name = "Archived Boss Arena - moved to BossScene_1";
        var persistent = SceneManager.GetSceneByName("persistent");
        if (!persistent.isLoaded) persistent = EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Additive);
        foreach (var loader in persistent.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneLoader>(true)))
        {
            var so = new SerializedObject(loader); so.FindProperty("bossLevelScene").objectReferenceValue = sceneData; so.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(sourceScene); EditorSceneManager.SaveScene(sourceScene);
        EditorSceneManager.MarkSceneDirty(persistent); EditorSceneManager.SaveScene(persistent);
        EditorSceneManager.MarkSceneDirty(roomScene); EditorSceneManager.SaveScene(roomScene); AssetDatabase.SaveAssets();
        return "BossScene_1 saved and registered; boss centered at(0,0.02); SampleScene_7 exit linked.";
    }
    private static GameObject Find(GameObject root, string name)
    {
        var transform = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        if (transform == null) throw new InvalidOperationException("Missing arena object: " + name);
        return transform.gameObject;
    }
}
