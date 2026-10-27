using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Forest continuation of the approved painted-panorama + interactive geometry workflow.
public static class BuildForestExpansion
{
    public const string Folder="Assets/GeneratedForestExpansion/";
    const float PPU=24,Height=683;
    static Transform root,ground,props;
    static Tilemap ladders;
    static Chest template;
    static int sorting;
    static Vector2 Point(float x,float y){return new Vector2(x/PPU,(Height-y)/PPU);}
    public static Vector3 Spawn(int n){return Point(96,n==12?376:n==13?347:421)+Vector2.up*.08f;}

    [MenuItem("Tools/Levels/Build Three Forest Scenes")]
    public static void BuildMenu(){Debug.Log(BuildAll());}
    public static string BuildAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int n=12;n<=14;n++)if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_"+n+".unity")!=null)
            throw new InvalidOperationException("Scene "+n+" already exists. Preserve authored content.");
        var source=SceneManager.GetSceneByName("SampleScene_8");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_8.unity",OpenSceneMode.Additive);
        template=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).First();
        var ui=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        sorting=template.GetComponent<SpriteRenderer>().sortingLayerID;
        for(int n=12;n<=14;n++)Build(n,ui);
        if(opened)EditorSceneManager.CloseScene(source,true);
        AssetDatabase.SaveAssets();return "Created Forest SampleScene_12, SampleScene_13 and SampleScene_14.";
    }
    static void Build(int n,LevelCompleteUI uiTemplate)
    {
        string title=n==12?"Sunlit Forest Creek":n==13?"Ancient Tree Village":"Moonlit Forest Ruins";
        string filename=n==12?"ForestCreek":n==13?"AncientTreeVillage":"MoonlitRuins";
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        root=Child(null,title+" - SampleScene "+n);
        ground=Child(root,"01 Traced Forest Terrain");ground.gameObject.layer=6;
        ground.gameObject.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
        ground.gameObject.AddComponent<CompositeCollider2D>().geometryType=CompositeCollider2D.GeometryType.Polygons;
        props=Child(root,"02 Ladders And Interactive Treasure");
        var grid=Child(root,"Forest Climbing Grid");grid.gameObject.AddComponent<Grid>();
        var map=Child(grid,"Forest Decorations");ladders=map.gameObject.AddComponent<Tilemap>();map.gameObject.AddComponent<TilemapRenderer>().enabled=false;
        string imagePath=Folder+filename+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=72;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var panorama=AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);var image=Child(root,"03 Generated Forest Panorama");
        var renderer=image.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=panorama;renderer.sortingLayerID=sorting;renderer.sortingOrder=-10;
        image.position=new Vector3(2048/PPU/2,Height/PPU/2,0);image.localScale=new Vector3(2048/PPU/panorama.bounds.size.x,Height/PPU/panorama.bounds.size.y,1);
        var ui=UnityEngine.Object.Instantiate(uiTemplate.gameObject).GetComponent<LevelCompleteUI>();ui.name="Level Complete Canvas";ui.panel.SetActive(false);
        Vector2 exit;
        if(n==12){Creek();exit=Point(1980,392);}
        else if(n==13){Village();exit=Point(1980,420);}
        else{Moon();exit=Point(1980,419);}
        Child(root,"Start Point").position=Spawn(n);Child(root,"Goal Point").position=exit;
        Trigger("Forest Exit",root,exit+Vector2.up*1.1f,new Vector2(2.2f,2.6f)).AddComponent<LevelGoal>().completionUI=ui;
        Trigger("Fall And Deep Water Hazard",root,new Vector2(43,.8f),new Vector2(95,1.3f)).tag="water";
        var bounds=Child(root,"Camera_Bounds");bounds.tag="Bounds";var polygon=bounds.gameObject.AddComponent<PolygonCollider2D>();polygon.isTrigger=true;
        polygon.points=new[]{Vector2.zero,new Vector2(2048/PPU,0),new Vector2(2048/PPU,Height/PPU),new Vector2(0,Height/PPU)};
        var c=Child(root,"Editor Overview Camera").gameObject.AddComponent<Camera>();c.tag="EditorOnly";c.enabled=false;c.orthographic=true;c.orthographicSize=Height/PPU/2;
        c.transform.position=new Vector3(2048/PPU/2,Height/PPU/2,-20);c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.015f,.03f,.025f);c.allowMSAA=false;
        string path="Assets/Scenes/SampleScene_"+n+".unity";EditorSceneManager.SaveScene(scene,path);
        var data=ScriptableObject.CreateInstance<GameSceneSO>();data.sceneType=SceneType.loaction;
        data.sceneReference=new UnityEngine.AddressableAssets.AssetReference(AssetDatabase.AssetPathToGUID(path));
        AssetDatabase.CreateAsset(data,"Assets/DataSO/GameScene/Game Scene"+n+".asset");ui.retryScene=data;ui.retryPosition=Spawn(n);EditorUtility.SetDirty(ui);
        var settings=AddressableAssetSettingsDefaultObject.Settings;settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),settings.FindGroup("Scenes")).address="SampleScene_"+n;
        EditorUtility.SetDirty(settings);EditorSceneManager.SaveScene(scene);BuildMineExpansion.Render(c,Folder+filename+"_Overview.png");
    }
    static void Creek()
    {
        Path("A Forest Arch And Steps",0,376,295,376,320,354,355,354,364,335,397,335,406,316,434,316,443,293,552,293);
        Path("B Creek Suspension Bridge",552,300,599,319,659,323,713,312,770,289);
        Path("C Ancient Tree Meadow",770,289,1020,291,1060,293,1112,307,1164,310);
        Path("D First Creek Stone",1150,427,1210,427);Path("D Second Creek Stone",1237,427,1292,427);Path("D Third Creek Stone",1310,419,1373,419);
        Path("E Far Forest Trail",1404,384,1499,384,1542,378,1615,378,1662,382,1799,382,1852,392,2048,392);
        Path("E Moss Terrace",1488,352,1550,352);Path("E Upper Meadow",1556,306,1647,306);Path("E High Meadow",1651,267,1765,267);
        Path("F Tree Lookout",1549,154,1687,154);
        Path("B Root Grotto",160,548,478,548,482,599,936,599);
        Path("C Root Canopy Walk",475,463,850,463);
        Ladder(434,548,316);Ladder(832,599,293);Ladder(1584,306,154);Ladder(1710,382,267);
        Treasure("Root Grotto Cache",310,548);Treasure("Ancient Tree Cache",975,291);Treasure("Lookout Cache",1610,154);
    }
    static void Village()
    {
        Path("A Entry And Root Ascent",0,347,295,347,313,331,335,318,356,305,374,292,525,292);
        Path("B Village Suspension Bridge",525,305,570,321,623,325,673,319,722,299);
        Path("C Hollow Tree Main Balcony",722,299,1045,299,1142,299);
        Path("D First Root Island",1157,323,1264,323);Path("D Second Root Island",1276,367,1387,367);
        Path("E Far Village And Exit",1422,353,1777,353,1864,370,1906,394,1942,420,2048,420);
        Path("C High Treehouse Balcony",788,112,1101,112);
        Path("E Treehouse Upper Deck",1514,239,1700,239,1725,203,1876,203);
        Path("B Lower Root Garden",345,547,877,547);
        Path("B Lower Garden Bridge",533,489,620,502,680,503,743,494);
        Path("C Lower Hollow Tree Landing",743,493,872,493);
        Path("A Low Root Access",295,347,320,380,350,410,400,438,433,450,519,450);
        Ladder(450,547,450);Ladder(490,450,292);Ladder(828,547,299);Ladder(1050,299,112);Ladder(1530,353,239);
        Treasure("Root Garden Cache",600,547);Treasure("High Treehouse Cache",970,112);Treasure("Far Village Cache",1740,203);
    }
    static void Moon()
    {
        Path("A Moon Arch And Moss Steps",0,421,316,421,335,402,359,384,391,370,413,351,442,336,466,310,925,310);
        Path("B Old Suspension Bridge",925,313,970,327,1020,329,1076,324,1124,324);
        Path("B Hidden Bridge Stone",985,347,1060,347);
        Path("C Moon Tree Upper Landing",1095,334,1218,334);Path("C Moon Tree Lower Landing",1218,388,1365,388);
        Path("C High Moon Shrine",1131,243,1370,243);
        Path("D First Moon Stone",1387,369,1468,369);Path("D Second Moon Stone",1489,343,1563,343);
        Path("E Moon Gate And Exit",1569,312,1780,312,1796,326,1816,343,1840,363,1877,387,1902,405,1940,419,2048,419);
        Path("B Hidden Moon Chamber",460,499,938,499);
        Path("B Lower Ruin Steps",938,499,963,532,986,549,1013,572,1107,611);
        Path("E Lower Root Refuge",1619,475,1731,475,1805,462,1885,450);
        Ladder(616,499,310);Ladder(890,499,310);Ladder(1260,388,243);Ladder(1690,475,312);
        Treasure("Hidden Moon Chamber Cache",690,499);Treasure("Moon Shrine Cache",1200,243);Treasure("Moon Gate Cache",1750,312);
    }
    static Transform Child(Transform parent,string name){var go=new GameObject(name);if(parent!=null)go.transform.SetParent(parent,false);return go.transform;}
    static GameObject Trigger(string name,Transform parent,Vector2 point,Vector2 size){var t=Child(parent,name);t.position=point;var box=t.gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=size;return t.gameObject;}
    static void Path(string name,params float[] xy)
    {
        var t=Child(ground,name);t.gameObject.layer=6;int count=xy.Length/2;var points=new Vector2[count*2];
        for(int i=0;i<count;i++){points[i]=Point(xy[i*2],xy[i*2+1]);points[points.Length-1-i]=points[i]+Vector2.down*.35f;}
        var polygon=t.gameObject.AddComponent<PolygonCollider2D>();polygon.points=points;polygon.usedByComposite=true;
    }
    static void Ladder(float x,float bottom,float top)
    {
        var a=Point(x,bottom);var b=Point(x,top);int cell=Mathf.RoundToInt(a.x-.5f),low=Mathf.FloorToInt(a.y),high=Mathf.CeilToInt(b.y);
        var tile=AssetDatabase.LoadAssetAtPath<Tile>("Assets/GeneratedMineV2/ClimbLadder.asset");for(int y=low;y<high;y++)ladders.SetTile(new Vector3Int(cell,y,0),tile);
        var t=Child(props,"Climbable Ladder "+x);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedMineV2/Ladder.asset");
        r.sortingLayerID=sorting;r.sortingOrder=5;t.position=new Vector3(cell+.5f,low,0);FixClimbableLadderProportions.Configure(r,.8f,high-low);
    }
    static void Treasure(string name,float x,float y){var chest=UnityEngine.Object.Instantiate(template.gameObject,props);chest.name=name;chest.transform.localScale=Vector3.one*.25f;chest.transform.position=Point(x,y);chest.SetActive(true);}
}

