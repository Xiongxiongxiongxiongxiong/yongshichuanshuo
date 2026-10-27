using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Uses the approved panorama + traced collision workflow from SampleScene_8.
public static class BuildMineExpansion
{
    const string Folder="Assets/GeneratedMineExpansion/";
    const float PPU=24, Height=683;
    static Transform root, ground, props;
    static Tilemap ladders;
    static Tile climbTile;
    static Chest chestTemplate;
    static int sorting;
    public static Vector3 Spawn(int number)
    {
        if(number==9)return Point(98,327)+Vector2.up*.08f;
        if(number==10)return Point(95,240)+Vector2.up*.08f;
        return Point(95,321)+Vector2.up*.08f;
    }
    static Vector2 Point(float x,float y){return new Vector2(x/PPU,(Height-y)/PPU);}

    [MenuItem("Tools/Levels/Build Three Mine Expansion Scenes")]
    public static void BuildFromMenu(){Debug.Log(BuildAll());}

    public static string BuildAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int n=9;n<=11;n++)
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene_"+n+".unity")!=null)
                throw new InvalidOperationException("SampleScene_"+n+" already exists. Preserve authored scenes.");
        var source=SceneManager.GetSceneByName("SampleScene_8");
        bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_8.unity",OpenSceneMode.Additive);
        var sourceObjects=source.GetRootGameObjects();
        chestTemplate=sourceObjects.SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).First();
        var uiTemplate=sourceObjects.SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        sorting=chestTemplate.GetComponent<SpriteRenderer>().sortingLayerID;
        climbTile=AssetDatabase.LoadAssetAtPath<Tile>("Assets/GeneratedMineV2/ClimbLadder.asset");
        for(int n=9;n<=11;n++)Build(n,uiTemplate);
        if(opened)EditorSceneManager.CloseScene(source,true);
        AssetDatabase.SaveAssets();
        return "Created SampleScene_9, SampleScene_10, SampleScene_11, each with independent Addressables, collision, ladders, treasure and goal.";
    }
    static void Build(int n,LevelCompleteUI uiTemplate)
    {
        string title=n==9?"Blue Quartz Mine":n==10?"Abandoned Minecart Foundry":"Amethyst Ruins Mine";
        string filename=n==9?"BlueQuartz":n==10?"MineFoundry":"AmethystRuins";
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        root=Child(null,title+" - SampleScene "+n);
        ground=Child(root,"01 Traced Ground And Bridges");ground.gameObject.layer=6;
        var body=ground.gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;
        ground.gameObject.AddComponent<CompositeCollider2D>().geometryType=CompositeCollider2D.GeometryType.Polygons;
        props=Child(root,"02 Interactive Treasure");
        var grid=Child(root,"Ladder Grid");grid.gameObject.AddComponent<Grid>();
        var map=Child(grid,"Forest Decorations");ladders=map.gameObject.AddComponent<Tilemap>();
        map.gameObject.AddComponent<TilemapRenderer>().enabled=false;
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+filename+".png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=72;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var panorama=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+filename+".png");
        var image=Child(root,"03 Generated "+title+" Panorama");
        var r=image.gameObject.AddComponent<SpriteRenderer>();r.sprite=panorama;r.sortingLayerID=sorting;r.sortingOrder=-10;
        var material=new Material(Shader.Find("Yongshi/Mine Panorama Water"));AssetDatabase.CreateAsset(material,Folder+filename+".mat");r.sharedMaterial=material;
        image.position=new Vector3(2048/PPU/2,Height/PPU/2,0);
        image.localScale=new Vector3(2048/PPU/panorama.bounds.size.x,Height/PPU/panorama.bounds.size.y,1);
        var ui=UnityEngine.Object.Instantiate(uiTemplate.gameObject).GetComponent<LevelCompleteUI>();
        ui.name="Level Complete Canvas";ui.panel.SetActive(false);
        Vector2 exit;
        if(n==9){BlueQuartz();exit=Point(1980,380);}
        else if(n==10){Foundry();exit=Point(1980,375);}
        else{Amethyst();exit=Point(1980,411);}
        var start=Child(root,"Start Point");start.position=Spawn(n);
        Child(root,"Goal Point").position=exit;
        var goal=Box("Mine Exit Goal",root,exit+Vector2.up*1.1f,new Vector2(2.2f,2.6f));
        goal.AddComponent<LevelGoal>().completionUI=ui;
        var bounds=Child(root,"Camera_Bounds");bounds.tag="Bounds";
        var polygon=bounds.gameObject.AddComponent<PolygonCollider2D>();polygon.isTrigger=true;
        polygon.points=new[]{Vector2.zero,new Vector2(2048/PPU,0),new Vector2(2048/PPU,Height/PPU),new Vector2(0,Height/PPU)};
        Box("Deep Water And Fall Hazard",root,new Vector2(43,.9f),new Vector2(95,1.4f)).tag="water";
        var c=Child(root,"Editor Overview Camera").gameObject.AddComponent<Camera>();c.tag="EditorOnly";c.enabled=false;
        c.orthographic=true;c.orthographicSize=Height/PPU/2;c.transform.position=new Vector3(2048/PPU/2,Height/PPU/2,-20);
        c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.015f,.02f,.04f);c.allowMSAA=false;
        string scenePath="Assets/Scenes/SampleScene_"+n+".unity";
        EditorSceneManager.SaveScene(scene,scenePath);
        var data=ScriptableObject.CreateInstance<GameSceneSO>();data.sceneType=SceneType.loaction;
        data.sceneReference=new UnityEngine.AddressableAssets.AssetReference(AssetDatabase.AssetPathToGUID(scenePath));
        AssetDatabase.CreateAsset(data,"Assets/DataSO/GameScene/Game Scene"+n+".asset");
        ui.retryScene=data;ui.retryPosition=Spawn(n);EditorUtility.SetDirty(ui);
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scenePath),settings.FindGroup("Scenes")).address="SampleScene_"+n;
        EditorUtility.SetDirty(settings);EditorSceneManager.SaveScene(scene);
        Render(c,Folder+filename+"_Overview.png");
        Debug.Log("Saved "+scenePath+" ("+title+")");
    }
    static void BlueQuartz()
    {
        Path("A Entry And Quartz Steps",0,327,360,327,384,313,413,306,438,291,464,276,502,266);
        Path("B Sagging Suspension Bridge",502,266,556,277,620,280,676,276,730,270);
        Path("C Giant Quartz Gallery",730,264,840,264,852,281,914,290,953,306,991,310);
        Path("D First Island",1008,337,1092,337);Path("D Second Island",1113,372,1192,372);Path("D Third Island",1216,344,1290,344);
        Path("E Lower Gantry",1288,330,1450,330,1700,330);
        Path("E Upper Crane Deck",1225,195,1624,195);
        Path("F Exit Steps",1700,328,1740,320,1780,310,1820,310,1840,334,1868,358,1900,380,2048,380);
        Path("B Lower Treasure Mine",94,544,320,546,560,548,807,549);
        Path("C Middle Timber Passage",360,450,677,450,701,430,816,430);
        Path("E Hidden Lower Spur",1528,495,1640,472,1720,440,1770,410,1812,388,1893,380);
        Ladder(340,546,327);Ladder(740,548,430);Ladder(800,430,264);Ladder(1350,330,195);Ladder(1544,495,330);
        Treasure("Lower Quartz Cache",510,548);Treasure("High Crane Cache",1520,195);Treasure("Quartz Gallery Cache",850,281);
    }
    static void Foundry()
    {
        Path("A Entry And Workshop Stairs",0,240,238,240,270,260,288,286,318,308,343,327,370,350,404,375,740,375,776,374,790,361,817,349);
        Path("B Waterwheel Bridge",817,349,986,348);
        Path("C Hoist Island",986,348,1226,348);
        Path("D First Ore Island",1262,367,1345,367);Path("D Second Ore Island",1388,391,1456,391);Path("D Third Ore Island",1472,367,1557,367);
        Path("E Bottom Scaffold And Exit",1550,416,1829,416,1875,401,1920,390,1965,380,2048,374);
        Path("E Middle Workshop",1525,289,1819,289);
        Path("E Upper Workshop",1539,161,1834,161);
        Path("F Stone Stair Branch",1819,289,1840,311,1860,334,1890,350,1920,367,1952,380);
        Path("B High Ore Shelf",195,132,395,139);
        Path("B High Rope Bridge",395,142,480,159,550,155,590,142);
        Path("C Upper Supply Hut",590,142,872,159);
        Path("A Lower Optional Timber Walk",125,440,404,440);
        Ladder(305,240,132);Ladder(385,440,350);Ladder(700,375,150);Ladder(1635,416,289);Ladder(1775,289,161);
        Treasure("High Ore Storage Cache",680,148);Treasure("Hoist Island Cache",1050,348);Treasure("Upper Foundry Cache",1680,161);
    }
    static void Amethyst()
    {
        Path("A Entry And Ruin Steps",0,321,360,321,390,302,414,288,940,288);
        Path("B Broken Stone Bridge",940,305,1091,305);Path("B Bridge Fragment",1116,319,1182,319);
        Path("C Crystal Hollow",1127,425,1396,425);Path("C High Sanctuary Cache",1285,280,1405,280);
        Path("D First Crystal Island",1420,367,1501,367);Path("D Second Crystal Island",1517,340,1587,340);
        Path("E Ancient Gate And Exit",1602,321,1785,321,1810,338,1842,357,1875,384,1900,404,1943,411,2048,411);
        Path("B Hidden Lower Shrine",280,505,900,505);Path("C Lower Crystal Refuge",1378,498,1452,498);
        Ladder(340,505,321);Ladder(770,505,288);Ladder(1343,425,280);Ladder(1388,498,425);
        Treasure("Sanctuary Upper Cache",625,288);Treasure("Hidden Shrine Cache",822,505);Treasure("Crystal Spire Cache",1310,280);
    }
    static Transform Child(Transform parent,string name)
    {var go=new GameObject(name);if(parent!=null)go.transform.SetParent(parent,false);return go.transform;}
    static GameObject Box(string name,Transform parent,Vector2 point,Vector2 size)
    {var t=Child(parent,name);t.position=point;var b=t.gameObject.AddComponent<BoxCollider2D>();b.isTrigger=true;b.size=size;return t.gameObject;}
    static void Path(string name,params float[] xy)
    {
        var t=Child(ground,name);t.gameObject.layer=6;int count=xy.Length/2;var points=new Vector2[count*2];
        for(int i=0;i<count;i++){points[i]=Point(xy[i*2],xy[i*2+1]);points[points.Length-1-i]=points[i]+Vector2.down*.35f;}
        var p=t.gameObject.AddComponent<PolygonCollider2D>();p.points=points;p.usedByComposite=true;
    }
    static void Ladder(float x,float bottom,float top)
    {
        var from=Point(x,bottom);var to=Point(x,top);int cell=Mathf.RoundToInt(from.x-.5f),low=Mathf.FloorToInt(from.y),high=Mathf.CeilToInt(to.y);
        for(int y=low;y<high;y++)ladders.SetTile(new Vector3Int(cell,y,0),climbTile);
        var t=Child(props,"Climbable Ladder "+x);var r=t.gameObject.AddComponent<SpriteRenderer>();
        r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedMineV2/Ladder.asset");r.sortingLayerID=sorting;r.sortingOrder=5;
        t.position=new Vector3(cell+.5f,low,0);FixClimbableLadderProportions.Configure(r,.8f,high-low);
    }
    static void Treasure(string name,float x,float y)
    {var c=UnityEngine.Object.Instantiate(chestTemplate.gameObject,props);c.name=name;c.transform.localScale=Vector3.one*.25f;c.transform.position=Point(x,y);c.SetActive(true);}
    public static void Render(Camera camera,string path)
    {
        var hidden=new List<GameObject>();
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var other=SceneManager.GetSceneAt(i);if(other==camera.gameObject.scene)continue;
            foreach(var go in other.GetRootGameObjects())if(go.activeSelf){hidden.Add(go);go.SetActive(false);}
        }
        var rt=new RenderTexture(2172,724,24);camera.targetTexture=rt;camera.aspect=3;camera.Render();RenderTexture.active=rt;
        var t=new Texture2D(2172,724,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,2172,724),0,0);t.Apply();System.IO.File.WriteAllBytes(path,t.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(t);UnityEngine.Object.DestroyImmediate(rt);
        foreach(var go in hidden)go.SetActive(true);
    }
}

