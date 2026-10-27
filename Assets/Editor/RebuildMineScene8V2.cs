using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class RebuildMineScene8V2
{
    const string Folder="Assets/GeneratedMineV2/";
    const float Pixels=24f, ImageHeight=683f;
    static Transform root, ground, decor;
    static int sorting;
    static Sprite ladder, closed, opened;
    static Tilemap ladderMap;
    static Tile ladderTile;
    static Vector2 Point(float x,float y) { return new Vector2(x/Pixels,(ImageHeight-y)/Pixels); }

    [MenuItem("Tools/Levels/Rebuild Mine SampleScene_8 V2")]
    public static void Build()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene=SceneManager.GetActiveScene();
        if(scene.name!="SampleScene_8") throw new InvalidOperationException("Open SampleScene_8 first.");
        if(scene.GetRootGameObjects().Any(g=>g.name=="Amberfall Mine - V2")) throw new InvalidOperationException("V2 already exists; preserve manual edits.");
        EditorSceneManager.SaveScene(scene,"Assets/Scene8Preview/SampleScene_8_BeforeV2.unity",true);
        var old=scene.GetRootGameObjects();
        var chestTemplate=old.SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).First();
        var ui=old.SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        sorting=chestTemplate.GetComponent<SpriteRenderer>().sortingLayerID;
        Configure(Folder+"MinePanorama.png",72);
        Configure(Folder+"MineProps.png",72);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"MineProps.png");
        // Sprite rectangles are in normalized thirds of the generated sheet.
        ladder=Slice(texture,"Ladder",142f/1536,22f/1024,222f/1536,984f/1024,new Vector2(.5f,0));
        closed=Slice(texture,"ChestClosed",531f/1536,182f/1024,460f/1536,345f/1024,new Vector2(.5f,0));
        opened=Slice(texture,"ChestOpen",1056f/1536,182f/1024,469f/1536,500f/1024,new Vector2(.5f,0));
        root=Child(null,"Amberfall Mine - V2");
        ground=Child(root,"01 Collision Matched To Painted Platforms");
        decor=Child(root,"02 Generated Interactive Sprites");
        var rigid=ground.gameObject.AddComponent<Rigidbody2D>(); rigid.bodyType=RigidbodyType2D.Static;
        var composite=ground.gameObject.AddComponent<CompositeCollider2D>(); composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
        ground.gameObject.layer=6;
        var panorama=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"MinePanorama.png");
        var image=Child(root,"03 Newly Generated Mine Panorama");
        var renderer=image.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite=panorama;
        renderer.sortingLayerID=sorting; renderer.sortingOrder=-10;
        var mat=new Material(Shader.Find("Yongshi/Mine Panorama Water")); AssetDatabase.CreateAsset(mat,Folder+"MineWater.mat"); renderer.sharedMaterial=mat;
        image.position=new Vector3(2048/Pixels/2,ImageHeight/Pixels/2,0);
        image.localScale=new Vector3((2048/Pixels)/panorama.bounds.size.x,(ImageHeight/Pixels)/panorama.bounds.size.y,1);

        // Pixel-space paths trace the actual foreground walkable silhouette.
        Path("A Entrance And Climb",new[]{new Vector2(0,354),new Vector2(210,354),new Vector2(236,350),new Vector2(320,350),new Vector2(345,343),new Vector2(376,343),new Vector2(388,325),new Vector2(422,325),new Vector2(439,315),new Vector2(462,315),new Vector2(476,293),new Vector2(551,293)});
        Path("B Sagging Rope Bridge",new[]{new Vector2(551,296),new Vector2(582,310),new Vector2(620,314),new Vector2(660,307),new Vector2(699,294)});
        Path("C Crystal Sanctuary",new[]{new Vector2(699,294),new Vector2(755,294),new Vector2(790,287),new Vector2(826,294),new Vector2(856,297),new Vector2(902,298),new Vector2(939,312),new Vector2(989,312),new Vector2(1020,304),new Vector2(1107,305)});
        Path("D First Island",new[]{new Vector2(1153,343),new Vector2(1228,343)});
        Path("D Second Island",new[]{new Vector2(1286,383),new Vector2(1363,383)});
        Path("E Lower Approach",new[]{new Vector2(1381,384),new Vector2(1448,384)});
        Path("E Timber Mid Terrace",new[]{new Vector2(1422,297),new Vector2(1535,297)});
        Path("E Summit",new[]{new Vector2(1546,214),new Vector2(1620,210),new Vector2(1718,210)});
        Path("F Exit Gallery",new[]{new Vector2(1718,289),new Vector2(1780,295),new Vector2(1822,298),new Vector2(1880,312),new Vector2(1940,323),new Vector2(2000,335),new Vector2(2048,347)});
        Path("C Hidden Lower Mine",new[]{new Vector2(293,550),new Vector2(360,563),new Vector2(420,573),new Vector2(520,575),new Vector2(680,577),new Vector2(830,578),new Vector2(929,573),new Vector2(993,572),new Vector2(1005,534),new Vector2(1069,534)});
        Path("B Hidden Treasure Alcove",new[]{new Vector2(120,511),new Vector2(224,511),new Vector2(243,525),new Vector2(260,533),new Vector2(293,550)});
        Path("B Lower Timber Bridge",new[]{new Vector2(392,456),new Vector2(520,458),new Vector2(650,457),new Vector2(800,456),new Vector2(951,457)});

        var grid=Child(root,"Ladder Grid"); grid.gameObject.AddComponent<Grid>();
        var map=Child(grid,"Forest Decorations"); ladderMap=map.gameObject.AddComponent<Tilemap>();
        var mapRenderer=map.gameObject.AddComponent<TilemapRenderer>(); mapRenderer.enabled=false;
        ladderTile=ScriptableObject.CreateInstance<Tile>(); ladderTile.name="Amberfall_Ladder"; ladderTile.colliderType=Tile.ColliderType.None;
        AssetDatabase.CreateAsset(ladderTile,Folder+"ClimbLadder.asset");
        Ladder("B Lower Mine Access",433,573,323);
        Ladder("C Secret Return",850,578,298);
        Ladder("E First Ascent",1430,384,297);
        Ladder("E Summit Ascent",1545,297,210);

        ChestAt(chestTemplate,"Lower Mine Treasure",196,511);
        ChestAt(chestTemplate,"Crystal Sanctuary Treasure",1050,305);
        ChestAt(chestTemplate,"Summit Treasure",1630,210);
        var start=Child(root,"Start Point"); start.position=Point(116,354)+Vector2.up*.08f;
        var finish=Child(root,"Goal Point"); finish.position=Point(1984,332);
        var goal=Box("Amberfall Exit",root,Point(1984,306),new Vector2(2,3),true);
        goal.gameObject.AddComponent<LevelGoal>().completionUI=ui;
        ui.retryPosition=start.position;
        ui.retryScene=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene8.asset");
        Box("Water And Fall Hazard",root,new Vector2(43,1.6f),new Vector2(95,2.2f),true).tag="water";
        var bounds=Child(root,"Camera_Bounds"); bounds.tag="Bounds";
        var poly=bounds.gameObject.AddComponent<PolygonCollider2D>(); poly.isTrigger=true;
        poly.points=new[]{new Vector2(0,0),new Vector2(2048/Pixels,0),new Vector2(2048/Pixels,ImageHeight/Pixels),new Vector2(0,ImageHeight/Pixels)};
        var camera=Child(root,"Editor Overview Camera").gameObject.AddComponent<Camera>(); camera.enabled=false;
        camera.tag="EditorOnly"; camera.orthographic=true; camera.orthographicSize=ImageHeight/Pixels/2;
        camera.transform.position=new Vector3(2048/Pixels/2,ImageHeight/Pixels/2,-20);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.015f,.02f,.04f); camera.allowMSAA=false;
        foreach(var go in old) if(go!=ui.gameObject) UnityEngine.Object.DestroyImmediate(go);
        EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.LookAt(new Vector3(43,14,0),Quaternion.identity,45,true,true);
        Debug.Log("Amberfall V2 saved with newly generated art, traced collision, 4 ladders and 3 interactive chests.");
    }
    static void Configure(string path,int ppu)
    {
        var i=(TextureImporter)AssetImporter.GetAtPath(path); i.textureType=TextureImporterType.Sprite;
        i.spriteImportMode=SpriteImportMode.Single; i.spritePixelsPerUnit=ppu; i.filterMode=FilterMode.Point;
        i.textureCompression=TextureImporterCompression.Uncompressed; i.mipmapEnabled=false; i.maxTextureSize=4096; i.alphaIsTransparency=true; i.SaveAndReimport();
    }
    static Sprite Slice(Texture2D t,string name,float x,float y,float w,float h,Vector2 pivot)
    {
        var s=Sprite.Create(t,new Rect(x*t.width,y*t.height,w*t.width,h*t.height),pivot,72,0,SpriteMeshType.FullRect);
        s.name=name; AssetDatabase.CreateAsset(s,Folder+name+".asset"); return s;
    }
    static Transform Child(Transform parent,string name)
    { var g=new GameObject(name); if(parent!=null)g.transform.SetParent(parent,false); return g.transform; }
    static BoxCollider2D Box(string name,Transform parent,Vector2 position,Vector2 size,bool trigger)
    { var t=Child(parent,name);t.position=position;var b=t.gameObject.AddComponent<BoxCollider2D>();b.size=size;b.isTrigger=trigger;return b; }
    static void Path(string name,Vector2[] pixels)
    {
        var t=Child(ground,name);t.gameObject.layer=6;
        // A shallow closed strip permits the painted lower tunnels to remain accessible.
        var points=new Vector2[pixels.Length*2];
        for(int i=0;i<pixels.Length;i++){points[i]=Point(pixels[i].x,pixels[i].y);points[points.Length-1-i]=points[i]+Vector2.down*.35f;}
        var p=t.gameObject.AddComponent<PolygonCollider2D>();p.points=points;p.usedByComposite=true;
    }
    static void Ladder(string name,float px,float bottom,float top)
    {
        var from=Point(px,bottom);var to=Point(px,top);
        int x=Mathf.RoundToInt(from.x-.5f), low=Mathf.FloorToInt(from.y),high=Mathf.CeilToInt(to.y);
        for(int y=low;y<high;y++)ladderMap.SetTile(new Vector3Int(x,y,0),ladderTile);
        var t=Child(decor,name);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=ladder;
        r.sortingLayerID=sorting;r.sortingOrder=5;
        t.position=new Vector3(x+.5f,low,0);
        FixClimbableLadderProportions.Configure(r,.9f,high-low);
    }
    static void ChestAt(Chest template,string name,float px,float py)
    {
        var g=UnityEngine.Object.Instantiate(template.gameObject,decor);g.name=name;
        var r=g.GetComponent<SpriteRenderer>();r.sprite=closed;r.sortingOrder=12;
        g.transform.localScale=Vector3.one*(name=="Lower Mine Treasure"?.32f:.23f);g.transform.position=Point(px,py);
        var so=new SerializedObject(g.GetComponent<Chest>());so.FindProperty("openSprite").objectReferenceValue=opened;
        so.FindProperty("closeSprite").objectReferenceValue=closed;so.ApplyModifiedPropertiesWithoutUndo();
        g.SetActive(true);
    }
}

