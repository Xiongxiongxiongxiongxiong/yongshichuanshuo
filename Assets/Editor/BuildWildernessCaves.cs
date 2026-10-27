using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildWildernessCaves
{
    public const string Folder="Assets/GeneratedWildernessCaves/";
    public static Vector3 Spawn(int n){return PanoramaLevelAuthoring.Point(96,n==15?357:n==16?259:328)+Vector2.up*.08f;}
    [MenuItem("Tools/Levels/Build Three Wilderness Cave Scenes")]
    public static void BuildMenu(){Debug.Log(BuildAll());}
    public static string BuildAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int n=15;n<=17;n++)if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_"+n+".unity")!=null||
            AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene"+n+".asset")!=null)
            throw new InvalidOperationException("Scene "+n+" already exists; preserve authored content.");
        var source=SceneManager.GetSceneByName("SampleScene_8");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_8.unity",OpenSceneMode.Additive);
        var roots=source.GetRootGameObjects();var chest=roots.SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).First();
        var ui=roots.SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        for(int n=15;n<=17;n++)
        {
            string title=n==15?"Moss Creek Cave":n==16?"Red Rock Canyon Cave":"Frost Mist Mountain Cave";
            string filename=n==15?"MossCreekCave":n==16?"RedRockCanyon":"FrostMistCave";
            var level=new PanoramaLevelAuthoring(n,title,Folder,filename,chest,ui);
            if(n==15)Moss(level);else if(n==16)RedRock(level);else Frost(level);
            level.Finish(Spawn(n),PanoramaLevelAuthoring.Point(1980,n==15?425:n==16?318:331));
        }
        if(opened)EditorSceneManager.CloseScene(source,true);AssetDatabase.SaveAssets();
        return "Saved wilderness cave SampleScene_15, SampleScene_16 and SampleScene_17.";
    }
    static void Moss(PanoramaLevelAuthoring l)
    {
        l.Path("A Sheltered Entry And Steps",0,357,285,357,302,343,329,327,354,311,379,294,530,294);
        l.Path("B Moss Suspension Bridge",530,306,583,324,645,327,700,319,758,296);
        l.Path("C Root Arch Shelf",758,296,1056,296,1092,310);
        l.Path("D First Stream Stone",1066,428,1155,428);l.Path("D Second Stream Stone",1180,449,1258,449);l.Path("D Third Stream Stone",1292,427,1362,427);
        l.Path("E Far Cave Trail",1365,384,1484,384,1493,357,1743,357,1798,377,1853,387,1883,406,1953,425,2048,425);
        l.Path("E Upper Moss Terrace",1479,258,1643,264,1650,308,1743,308);
        l.Path("F High Cave Alcove",1594,154,1746,154);
        l.Path("B Lower Natural Grotto",145,538,853,540);
        l.Path("B Grotto Step",448,496,548,496);l.Path("C Grotto Roof Ledge",532,420,696,420);
        l.Ladder(332,538,357);l.Ladder(620,540,296);l.Ladder(1660,308,154);l.Ladder(1550,357,263);
        l.Treasure("Moss Grotto Cache",230,538);l.Treasure("Root Arch Cache",945,296);l.Treasure("High Moss Cave Cache",1665,154);
    }
    static void RedRock(PanoramaLevelAuthoring l)
    {
        l.Path("A Red Cave And Descending Steps",0,259,235,259,258,284,291,310,326,328,363,355,403,375,438,397,781,397,802,380,822,367);
        l.Path("B Natural Stone Bridge",822,367,870,362,920,354,988,343);
        l.Path("C Sandstone Arch Island",988,326,1232,326);
        l.Path("D First Canyon Island",1247,393,1330,393);l.Path("D Second Canyon Island",1354,404,1447,404);
        l.Path("E Far Canyon And Exit",1469,388,1798,388,1824,367,1858,345,1884,330,1913,318,2048,318);
        l.Path("E Middle Rock Shelter",1544,246,1824,246);l.Path("F High Rock Shelter",1548,104,1990,104);
        l.Path("C Central Arch Lookout",980,145,1091,145,1102,184,1173,184);
        l.Path("B Upper Left Alcove",197,162,471,162);l.Path("B Upper Rope Passage",471,152,522,166,572,156,610,139);l.Path("B Upper Left Lookout",610,129,757,129);
        l.Path("B Middle Cave Shelf",443,246,800,246);
        l.Ladder(1640,388,246);l.Ladder(1755,246,104);l.Ladder(1136,326,184);l.Ladder(485,246,155);l.Ladder(710,397,246);
        l.Treasure("Red Cave Cache",620,397);l.Treasure("Sandstone Lookout Cache",1040,145);l.Treasure("High Red Shelter Cache",1880,104);
    }
    static void Frost(PanoramaLevelAuthoring l)
    {
        l.Path("A Frost Entry And Rock Steps",0,328,283,328,295,312,313,299,340,289,442,289);
        l.Path("B Snow Gallery",451,244,889,244);
        l.Path("C Mist Suspension Bridge",889,265,936,281,984,286,1036,279,1092,270);
        l.Path("C Alpine Rock Island",1092,288,1386,288);
        l.Path("D First Frost Island",1385,338,1478,338);l.Path("D Second Frost Island",1494,299,1578,299);
        l.Path("E Alpine Arch And Exit",1584,241,1928,241,1939,261,1959,283,1981,307,2012,331,2048,331);
        l.Path("C High Alpine Lookout",1180,164,1332,164);
        l.Path("B Explorer Cave Gallery",430,435,1002,435);l.Path("B Deep Frost Cave Floor",200,529,365,549,890,558);
        l.Path("A Lower Cave Access Steps",283,328,305,355,344,379,371,403,401,425,430,435);
        l.Ladder(600,435,244);l.Ladder(540,558,435);l.Ladder(837,558,435);l.Ladder(1238,288,164);
        l.Treasure("Explorer Shelter Cache",700,435);l.Treasure("Snow Gallery Cache",740,244);l.Treasure("Alpine Lookout Cache",1260,164);
    }
}
