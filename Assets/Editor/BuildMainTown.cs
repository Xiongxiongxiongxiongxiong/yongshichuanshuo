using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildMainTown
{
    public static Vector3 Spawn => PanoramaLevelAuthoring.Point(850,470)+Vector2.up*.08f;
    public static string Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit play mode first.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_25.unity")!=null)throw new InvalidOperationException("Town already exists; preserve edits.");
        var persistent=SceneManager.GetSceneByName("persistent");if(!persistent.isLoaded)persistent=EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Additive);
        if(persistent.isDirty)throw new InvalidOperationException("Unsaved persistent edits.");
        EditorSceneManager.SaveScene(persistent,"Assets/GeneratedTown/Persistent_BeforeMainTown.unity",true);
        var source=SceneManager.GetSceneByName("SampleScene_8");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_8.unity",OpenSceneMode.Additive);
        var chest=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).First();
        var ui=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        var l=new PanoramaLevelAuthoring(25,"Twilight River Main Town","Assets/GeneratedTown/","TwilightRiverTown",chest,ui);
        l.Path("A Gate And Main Street",0,330,120,330,160,354,200,382,240,408,300,432,350,442,580,459,800,467,850,470,900,474,1235,474,1235,482,1304,482);
        l.Path("B River Arch Bridge",1304,463,1430,463,1460,454,1550,454,1588,437,1640,425,1908,425,2048,425);
        l.Path("C Lower Riverside",0,602,340,602,340,618,352,618);
        l.Path("C Lower Timber Walk",352,602,540,602,540,613,839,613);
        l.Path("C River Arcade",839,611,1420,611,1420,602,1450,602);
        l.Path("D Inn Balcony",968,331,1190,331);
        l.Path("D Guild Balcony",1410,335,1600,335);
        l.Path("E Castle Lower Steps",1808,425,1828,406,1847,387,1866,370,1884,351,1905,337,2048,337);
        l.Path("E Castle Upper Steps",1905,337,1920,319,1938,302,1956,285,1972,267,1992,248,2048,248);
        l.Ladder(300,602,434);l.Ladder(895,611,474);l.Ladder(1090,474,331);l.Ladder(1510,454,335);
        l.Finish(Spawn,PanoramaLevelAuthoring.Point(2020,248));
        var town=SceneManager.GetSceneByName("SampleScene_25");
        foreach(var completion in town.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).ToArray())UnityEngine.Object.DestroyImmediate(completion.gameObject);
        foreach(var goal in town.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelGoal>(true)).ToArray())UnityEngine.Object.DestroyImmediate(goal.gameObject);
        var floor=town.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider2D>(true)).First(c=>c.CompareTag("water"));floor.tag="Untagged";floor.gameObject.name="Town River Safety Return";floor.gameObject.AddComponent<TownSafetyReturn>();
        var root=town.GetRootGameObjects().First();
        foreach(float x in new[]{-.5f,2048f/24f+.5f}){var wall=new GameObject("Town Boundary");SceneManager.MoveGameObjectToScene(wall,town);wall.transform.SetParent(root.transform,false);wall.layer=6;wall.transform.position=new Vector3(x,14,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(1,40);}
        EditorSceneManager.MarkSceneDirty(town);EditorSceneManager.SaveScene(town);
        TownForeground.Build(25);
        var data=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene25.asset");
        var loader=persistent.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SceneLoader>(true)).First();
        loader.firstLoadScene=data;loader.firstPosition=Spawn;loader.mainTownScene=data;loader.mainTownPosition=Spawn;
        var nav=loader.gameObject.AddComponent<TownHubNavigation>();nav.loader=loader;nav.town=data;
        nav.innPosition=PanoramaLevelAuthoring.Point(1110,474);nav.guildPosition=PanoramaLevelAuthoring.Point(1660,425);
        string[] names={"矿洞 · 琥珀矿井","矿洞 · 蓝晶矿脉","矿洞 · 废弃工坊","矿洞 · 紫晶遗迹","森林 · 溪流古道","森林 · 古树村落","森林 · 月光遗迹","山洞 · 苔溪洞窟","山洞 · 红岩峡谷","山洞 · 霜雾山洞","矿洞 · 深层矿道","沙漠 · 绿洲古道","沙漠 · 风蚀峡谷","沙漠 · 失落沙城","地牢 · 铁链监牢","地牢 · 地下水道","地牢 · 暗影王陵"};
        nav.destinationNames=names;nav.destinations=new GameSceneSO[17];nav.destinationSpawns=new Vector3[17];
        for(int n=8;n<=24;n++)
        {
            nav.destinations[n-8]=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene"+n+".asset");
            if(nav.destinations[n-8]==null)
            {
                var missing=UnityEngine.ScriptableObject.CreateInstance<GameSceneSO>();missing.sceneType=SceneType.loaction;
                string guid=AssetDatabase.AssetPathToGUID("Assets/Scenes/SampleScene_"+n+".unity");
                missing.sceneReference=new UnityEngine.AddressableAssets.AssetReference(guid);
                AssetDatabase.CreateAsset(missing,"Assets/DataSO/GameScene/Game Scene"+n+".asset");nav.destinations[n-8]=missing;
                var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                settings.CreateOrMoveEntry(guid,settings.FindGroup("Scenes")).address="SampleScene_"+n;EditorUtility.SetDirty(settings);
            }
            var target=SceneManager.GetSceneByName("SampleScene_"+n);bool openedTarget=!target.isLoaded;
            if(openedTarget)target=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_"+n+".unity",OpenSceneMode.Additive);
            var start=target.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Start Point");
            nav.destinationSpawns[n-8]=start!=null?start.position:new Vector3(116f/24,(683-354)/24f+.08f,0);
            if(openedTarget)EditorSceneManager.CloseScene(target,true);
        }
        EditorUtility.SetDirty(loader);EditorUtility.SetDirty(nav);EditorSceneManager.MarkSceneDirty(persistent);EditorSceneManager.SaveScene(persistent);AssetDatabase.SaveAssets();
        SceneManager.SetActiveScene(town);BuildMineExpansion.Render(town.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(),"Assets/GeneratedTown/Town_Overview.png");
        if(opened)EditorSceneManager.CloseScene(source,true);AssetDatabase.Refresh();return "Town25 saved, New Game enters main town, seventeen destinations and return-to-town/rest navigation installed.";
    }
}
