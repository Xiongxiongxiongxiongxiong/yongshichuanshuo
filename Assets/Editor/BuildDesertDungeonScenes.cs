using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildDesertDungeonScenes
{
    public const string Folder="Assets/GeneratedDesertDungeon/";
    public static readonly string[] Files={"OasisTrail","WindCanyon","LostSandCity","IronchainPrison","UndergroundWaterworks","ShadowRoyalCrypt"};
    public static readonly string[] Titles={"Oasis Caravan Trail","Wind-carved Canyon","Lost Sapphire Sand City","Ironchain Prison","Underground Waterworks","Shadow Royal Crypt"};
    public static Vector3 Spawn(int n){int[] y={284,284,285,257,283,352};return PanoramaLevelAuthoring.Point(70,y[n-19])+Vector2.up*.08f;}
    public static string BuildAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit play mode first.");
        for(int n=19;n<=24;n++)if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_"+n+".unity")!=null||AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene"+n+".asset")!=null)throw new InvalidOperationException("Preserve existing scene "+n);
        var source=SceneManager.GetSceneByName("SampleScene_8");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_8.unity",OpenSceneMode.Additive);
        var chest=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).First();
        var ui=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        for(int n=19;n<=24;n++)
        {
            var l=new PanoramaLevelAuthoring(n,Titles[n-19],Folder,Files[n-19],chest,ui);
            switch(n){case 19:Oasis(l);break;case 20:Canyon(l);break;case 21:City(l);break;case 22:Prison(l);break;case 23:Waterworks(l);break;default:Crypt(l);break;}
            int[] exits={280,284,317,248,298,272};l.Finish(Spawn(n),PanoramaLevelAuthoring.Point(1985,exits[n-19]));
            if(n==22||n==23)
            {
                var room=SceneManager.GetSceneByName("SampleScene_"+n);
                var hazard=new GameObject(n==22?"Prison Spike Pit":"Deep Cistern Water");SceneManager.MoveGameObjectToScene(hazard,room);
                hazard.transform.SetParent(room.GetRootGameObjects().First(r=>r.name.Contains("SampleScene")).transform,false);
                hazard.tag="water";hazard.transform.position=PanoramaLevelAuthoring.Point(n==22?1318:1364,n==22?596:548);
                var box=hazard.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(n==22?16:18,1.5f);
                EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);
            }
        }
        if(opened)EditorSceneManager.CloseScene(source,true);AssetDatabase.SaveAssets();return "Saved SampleScene_19–24 and Addressable scene data.";
    }
    static void Oasis(PanoramaLevelAuthoring l)
    {
        l.Path("A Caravan Entry And Stairs",0,284,260,284,260,300,281,300,281,316,308,316,308,331,335,331,335,340,425,340);
        l.Path("B Oasis Rope Bridge",425,340,500,349,600,354,690,341,740,337);
        l.Path("C Oasis Market",740,330,1107,330,1130,332,1355,332);
        l.Path("D First Oasis Island",1374,357,1443,357);l.Path("D Second Oasis Island",1490,372,1567,372);
        l.Path("E Gate Steps",1600,368,1705,368,1705,352,1730,352,1730,334,1753,334,1753,317,1778,317,1778,300,1802,300,1802,281,2048,281);
        l.Path("F Upper Caravan Terrace",0,151,240,151,263,174,328,174,328,185,590,185);
        l.Path("B Lower Stone Walk",236,340,340,340,340,359,368,359,368,380,400,380,400,402,455,402,455,418,540,444,580,453,865,453);
        l.Path("B Lower Grotto Floor",0,542,180,542,180,564,326,564,326,575,459,575);
        l.Path("C Lower Arcade Floor",555,606,870,606,870,626,994,626,994,607,1100,607,1100,586,1225,586);
        l.Ladder(340,340,185);l.Ladder(300,575,340);l.Ladder(645,606,453);l.Ladder(730,453,330);
        l.Treasure("Caravan Watchtower Cache",490,185);l.Treasure("Shaded Grotto Cache",400,575);l.Treasure("Oasis Market Cache",1000,330);
    }
    static void Canyon(PanoramaLevelAuthoring l)
    {
        l.Path("A Canyon Entry And Descent",0,284,330,284,330,302,350,302,350,323,390,323,390,340,600,340);
        l.Path("B Canyon Rope Bridge",600,340,654,349,700,346,754,334);
        l.Path("C Fossil Mesa",754,329,1304,329);
        l.Path("D First Wind Island",1314,396,1412,396);l.Path("D Second Wind Island",1451,421,1545,421);
        l.Path("E Ruin Gate Approach",1585,390,1705,390,1705,371,1742,371,1742,352,1776,352,1776,332,1810,332,1810,315,1847,315,1847,295,1890,295,1890,284,2048,284);
        l.Path("F Wind Shrine Terrace",0,109,579,109);
        l.Path("B Smuggler Grotto",0,539,183,539,183,565,390,565,390,554,940,554);
        l.Ladder(260,284,109);l.Ladder(513,554,340);l.Ladder(865,554,329);
        l.Treasure("Wind Shrine Cache",390,109);l.Treasure("Smuggler Cache",330,565);l.Treasure("Fossil Mesa Cache",1110,329);
    }
    static void City(PanoramaLevelAuthoring l)
    {
        l.Path("A Palace Entry Steps",0,285,173,285,173,305,204,305,204,325,233,325,233,345,379,345);
        l.Path("B Broken Arcade Bridge",379,345,597,345);
        l.Path("C Blue Mosaic Terrace",597,345,1235,349);
        l.Path("D First Sand Pillar",1285,357,1388,357);l.Path("D Second Sand Pillar",1435,391,1556,391);
        l.Path("E Moon Gate Steps",1590,418,1673,418,1673,399,1700,399,1700,378,1728,378,1728,357,1770,357,1770,337,1808,337,1808,317,2048,317);
        l.Path("F Observatory Terrace",236,164,741,164);
        l.Path("B Middle Arcade Spur",100,418,350,418,350,444,950,444,950,423,1073,423);
        l.Path("B Sunken Crypt Floor",0,601,220,601,220,620,965,620,1090,598,1136,567,1235,567);
        l.Ladder(300,418,345);l.Ladder(650,620,345);l.Ladder(715,345,164);l.Ladder(1000,620,444);
        l.Treasure("Observatory Cache",590,164);l.Treasure("Sunken Palace Cache",390,620);l.Treasure("Mosaic Terrace Cache",1030,347);
    }
    static void Prison(PanoramaLevelAuthoring l)
    {
        l.Path("A Prison Entry",0,257,140,257,140,275,172,275,172,293,550,293);
        l.Path("B Chain Guard Bridge",550,293,720,302,910,304);
        l.Path("C Watch Pillar",910,307,1154,307);
        l.Path("D First Prison Pillar",1206,328,1297,328);l.Path("D Second Prison Pillar",1342,338,1428,338);
        l.Path("E Prison Gate Steps",1496,337,1658,337,1658,319,1692,319,1692,301,1720,301,1720,282,1750,282,1750,264,1782,264,1782,248,2048,248);
        l.Path("F Upper Guard Deck",273,124,716,124);
        l.Path("B Lower Cell Block",0,541,984,541);
        l.Path("E Lower Gate Refuge",1510,623,1635,623,1635,554,2048,554);
        l.Path("E Middle Prison Refuge",1640,470,1780,470);
        l.Ladder(310,541,293);l.Ladder(520,293,124);l.Ladder(865,541,302);l.Ladder(1710,554,301);
        l.Treasure("Guard Deck Cache",590,124);l.Treasure("Cell Block Cache",655,541);l.Treasure("Watch Pillar Cache",1060,307);
    }
    static void Waterworks(PanoramaLevelAuthoring l)
    {
        l.Path("A Cistern Entry Steps",0,283,120,283,120,303,147,303,147,320,665,320);
        l.Path("B Maintenance Timber Walk",665,320,870,326);
        l.Path("C Sluice Gallery",870,326,1130,326);
        l.Path("D First Water Pillar",1204,363,1287,363);l.Path("D Second Water Pillar",1351,364,1434,364);l.Path("D Third Water Pillar",1498,369,1579,369);
        l.Path("E Floodgate Steps",1588,378,1644,378,1644,360,1670,360,1670,341,1701,341,1701,321,1736,321,1736,303,1775,303,1775,298,2048,298);
        l.Path("F Pump House Deck",333,143,750,143);
        l.Path("B Dry Maintenance Chamber",0,568,283,568,283,543,393,543,393,573,487,573,487,549,605,549,605,585,971,585);
        l.Path("C Drain Access Island",677,512,830,512,830,522,910,522);
        l.Ladder(330,568,320);l.Ladder(380,320,143);l.Ladder(676,585,320);
        l.Treasure("Pump House Cache",585,143);l.Treasure("Maintenance Chamber Cache",870,585);l.Treasure("Sluice Gallery Cache",1030,326);
    }
    static void Crypt(PanoramaLevelAuthoring l)
    {
        l.Path("A Crypt Entry",0,352,255,352);
        l.Path("B Left Shrine Landing",255,320,530,320);
        l.Path("C Royal Causeway Steps",530,320,550,340,578,340,578,360,608,360,608,376,1140,376);
        l.Path("D First Grave Pillar",1173,377,1244,377);l.Path("D Second Grave Pillar",1291,379,1359,379);l.Path("D Third Grave Pillar",1404,378,1472,378);
        l.Path("E Rune Door Approach",1510,373,1690,373,1690,350,1720,350,1720,328,1751,328,1751,307,1783,307,1783,288,1810,288,1810,272,2048,272);
        l.Path("F Upper Shrine Terrace",270,165,836,165);
        l.Path("B Mid Crypt Balcony",84,443,631,443);
        l.Path("B Lower Candlelit Crypt",77,582,240,582,240,601,1130,601);
        l.Path("E Lower Royal Refuge",1570,587,2048,587);
        l.Path("E Mid Royal Refuge",1720,438,1874,438,1874,480,2048,480);
        l.Ladder(350,582,320);l.Ladder(414,320,165);l.Ladder(655,601,376);l.Ladder(1650,587,373);
        l.Treasure("Upper Shrine Cache",640,165);l.Treasure("Royal Crypt Cache",515,601);l.Treasure("Causeway Cache",1050,376);
    }
}
