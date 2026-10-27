using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// All scene/asset serialization is performed by Unity, never by editing YAML.
public static class BuildMineScene8
{
    const string Folder = "Assets/Scene8Resources/Mine";
    const string Art = "Assets/Legacy-Fantasy - High Forest 2.3/Assets/";
    static Transform root, terrain, props, water;
    static Material cutout;
    static int layer;
    static Dictionary<string, Sprite> art;
    static Tilemap ladders;

    [MenuItem("Tools/Levels/Build Mine SampleScene_8")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene_8") throw new InvalidOperationException("Open SampleScene_8 first.");
        if (scene.GetRootGameObjects().Any(g=>g.name=="Crystal Mine - SampleScene 8"))
            throw new InvalidOperationException("Mine already built; preserve authored changes.");
        System.IO.Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene,"Assets/Scene8Preview/SampleScene_8_BeforeMine.unity",true);
        var oldRoots=scene.GetRootGameObjects();
        var oldChests=oldRoots.SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).ToArray();
        var ui=oldRoots.SelectMany(g=>g.GetComponentsInChildren<LevelCompleteUI>(true)).First();
        var oldLabel=oldRoots.SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)).First(t=>t.name=="Goal Label");
        layer=oldChests[0].GetComponent<SpriteRenderer>().sortingLayerID;
        cutout=new Material(Shader.Find("Yongshi/Mine Atlas Cutout"));
        AssetDatabase.CreateAsset(cutout,Folder+"/MineAtlas.mat");
        art=new Dictionary<string, Sprite>();
        Slice("rock",1,12,14,126,126); Slice("fill",1,25,45,100,80);
        Slice("lip",1,340,165,132,25); Slice("ledge",1,625,14,111,140);
        Slice("ceiling",1,755,14,190,164);
        Slice("crystal",1,835,596,100,104); Slice("crystal2",1,1125,595,116,102);
        Slice("stalagmite",1,858,385,124,188); Slice("ore",1,839,709,123,79);
        Slice("beam",2,451,346,270,160); Slice("beam2",2,794,346,243,160);
        Slice("lamp",2,1052,345,116,162); Slice("rail",2,1041,744,134,61);
        Slice("cart",2,1151,641,132,85); Slice("crate",2,1291,650,68,64);
        Slice("barrel",2,1469,642,60,76); Slice("ladder",2,1473,727,46,219);
        Slice("entrance",2,1173,810,350,202);
        root=Child(null,"Crystal Mine - SampleScene 8");
        terrain=Child(root,"01 Rock Platforms And Collision");
        props=Child(root,"02 Mine Timber Crystals And Treasure");
        water=Child(root,"03 Cascades And Deep Water");
        var grid=new GameObject("Mine Grid",typeof(Grid)); grid.transform.SetParent(root,false);
        var ladderObject=new GameObject("Forest Decorations",typeof(Tilemap),typeof(TilemapRenderer));
        ladderObject.transform.SetParent(grid.transform,false); ladders=ladderObject.GetComponent<Tilemap>();
        var lr=ladderObject.GetComponent<TilemapRenderer>(); lr.sortingLayerID=layer; lr.sortingOrder=22; lr.sharedMaterial=cutout;
        var tile=ScriptableObject.CreateInstance<Tile>(); tile.name="Mine_Ladder"; tile.sprite=art["ladder"];
        tile.colliderType=Tile.ColliderType.None;
        // Tilemap cells use a short repeated section of the same ladder artwork.
        var ladderTexture=art["ladder"].texture;
        var rung=Sprite.Create(ladderTexture,new Rect(1473,ladderTexture.height-830,46,46),new Vector2(.5f,.5f),46);
        AssetDatabase.CreateAsset(rung,Folder+"/LadderRung.asset"); tile.sprite=rung;
        AssetDatabase.CreateAsset(tile,Folder+"/MineClimbTile.asset");

        // Six connected areas, with rises <= 2 units and short bridge gaps.
        Block("A Entrance",0,11,-4,8);
        Block("A Lower Landing",11,16,-4,6); Bridge(16,20,6);
        Block("B Minecart Shelf",20,29,-4,6); Block("B Roof",23,29,10,12);
        Block("C Ascent 1",29,33,-4,8); Block("C Ascent 2",33,37,-4,10);
        Block("C High Gallery",37,44,4,12); Bridge(44,49,12);
        Block("D Falls Lip",49,54,4,12); Block("D Cascade Tower",56,61,0,10);
        Bridge(54,56,10); Block("D Far Landing",63,68,0,10); Bridge(61,63,10);
        Block("E Ascent 1",68,72,0,12); Block("E Ascent 2",72,76,0,14);
        Block("E Summit",76,84,6,16); Bridge(84,88,16);
        Block("F Summit Landing",88,92,5,16); Block("F Exit Gallery",92,104,-4,12);
        // An accessible branch under the high galleries, reached by ladders.
        Block("C Lower Cavern",34,48,-4,0); Bridge(29,34,0);
        Block("D Underground Refuge",48,55,-4,0); Bridge(55,63,0);
        Block("E Secret Floor",63,76,-4,0);
        Block("B Upper Cache",15,21,12,13.1f);
        Ladder(12,6,14,tile); Bridge(12,15,13);
        Ladder(27,6,13,tile); Ladder(35,0,11,tile);
        Ladder(65,0,11,tile); Ladder(74,0,15,tile);
        Ladder(94,12,17,tile);

        var sourceSprites=AssetDatabase.LoadAllAssetsAtPath(Art+"ForestReferenceTiles.png").OfType<Sprite>().ToDictionary(s=>s.name);
        Pool(16,20,-4,0,sourceSprites); Pool(54,63,-4,-1,sourceSprites);
        Falls(54.1f,55.8f,0,11.8f,sourceSprites); Falls(61.15f,62.85f,0,9.8f,sourceSprites);
        Falls(84.2f,87.8f,-3,15.8f,sourceSprites); Pool(80,92,-4,-2.5f,sourceSprites);
        // Background remains behind all traversal geometry, at native proportions.
        var bg=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"leve2_BG.png");
        Place(bg,"leve2_BG - Distant Crystal Caverns",52,-8,132,49.5f,-50,root,false);
        for(int i=0;i<11;i++) Place(art["ceiling"],"Hanging Cave Roof "+i,i*10+2,28,11,7,-4,props,true);
        var surfaces=new[]{new Vector3(3,8,0),new Vector3(9,8,0),new Vector3(21,6,0),new Vector3(28,12,0),
            new Vector3(32,8,0),new Vector3(39,12,0),new Vector3(51,12,0),new Vector3(58,10,0),
            new Vector3(66,10,0),new Vector3(73,14,0),new Vector3(79,16,0),new Vector3(90,16,0),new Vector3(99,12,0),
            new Vector3(38,0,0),new Vector3(52,0,0),new Vector3(69,0,0)};
        for(int i=0;i<surfaces.Length;i++)
        {
            var p=surfaces[i]; Prop(i%2==0?"crystal":"crystal2","Amber Crystal Cluster",p.x,p.y,1.25f,1.35f,15);
            if(i%2==0) Prop("lamp","Hanging Mine Lantern",p.x+1.8f,p.y,1.7f,3.1f,14);
            if(i%3==0) Prop("ore","Loose Ore",p.x-1.5f,p.y,1.3f,.75f,16);
        }
        foreach(var p in new[]{new Vector2(5,8),new Vector2(24,6),new Vector2(40,0),new Vector2(51,0),new Vector2(79,16),new Vector2(99,12)})
            Prop("beam","Timber Mine Support",p.x,p.y,5.5f,4.2f,8);
        Prop("entrance","Starting Mine Entrance",3,8,5.5f,3.2f,9);
        Prop("entrance","Exit Mine Entrance",100,12,5.5f,3.2f,9);
        Prop("cart","Ore Minecart",24,6,2,1.3f,19);
        Prop("cart","Abandoned Cavern Minecart",46,0,2,1.3f,19);
        Prop("crate","Summit Supplies",77,16,1,1,18); Prop("barrel","Summit Barrel",82,16,.8f,1.1f,18);
        for(int i=0;i<3;i++)
        {
            var chest=UnityEngine.Object.Instantiate(oldChests[i%oldChests.Length].gameObject,props);
            chest.name=new[]{"B Upper Cache Treasure","C Cavern Treasure","E Summit Treasure"}[i];
            var p=new[]{new Vector2(18,13.1f),new Vector2(42,0),new Vector2(80,16)}[i];
            var r=chest.GetComponent<SpriteRenderer>(); chest.transform.position=new Vector3(p.x,p.y-r.sprite.bounds.min.y*chest.transform.lossyScale.y,0);
            r.sortingOrder=25; chest.SetActive(true);
        }
        var start=Child(root,"Start Point"); start.position=new Vector3(3,8.08f,0);
        var finish=Child(root,"Goal Point"); finish.position=new Vector3(100,12.08f,0);
        var goal=Box("Mine Exit Goal",root,new Vector2(100,13.2f),new Vector2(2,2.4f),true);
        goal.gameObject.AddComponent<LevelGoal>().completionUI=ui;
        var label=UnityEngine.Object.Instantiate(oldLabel,root); label.name="Goal Label";
        label.text="矿洞出口"; label.transform.position=new Vector3(98.5f,16,0);
        ui.retryPosition=start.position;
        var bounds=Child(root,"Camera_Bounds"); bounds.tag="Bounds";
        var poly=bounds.gameObject.AddComponent<PolygonCollider2D>(); poly.isTrigger=true;
        poly.points=new[]{new Vector2(-3,-6),new Vector2(107,-6),new Vector2(107,30),new Vector2(-3,30)};
        Box("Fall Death Zone",root,new Vector2(52,-7),new Vector2(125,2),true).tag="water";
        // Content scenes use the player/camera from persistent; this camera is for saved overview renders only.
        var preview=Child(root,"Editor Overview Camera"); preview.tag="EditorOnly";
        var camera=preview.gameObject.AddComponent<Camera>(); camera.enabled=false; camera.orthographic=true;
        camera.orthographicSize=20; camera.transform.position=new Vector3(52,12,-30);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.025f,.035f,.07f); camera.allowMSAA=false;
        foreach(var old in oldRoots) if(old!=ui.gameObject) UnityEngine.Object.DestroyImmediate(old);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/SampleScene_8.unity"); AssetDatabase.SaveAssets();
        var settings=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID("Assets/Scenes/SampleScene_8.unity"),settings.FindGroup("Scenes"));
        entry.address="SampleScene_8"; EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.LookAt(new Vector3(52,10,0),Quaternion.identity,58,true,true);
        Debug.Log("Mine SampleScene_8 saved: 6 zones, 6 ladders, 3 treasure chests, 3 waterfalls, start and finish.");
    }
    static void Slice(string name,int sheet,int x,int y,int w,int h)
    {
        string path=Art+"leve2_"+sheet+".png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer.filterMode!=FilterMode.Point||importer.textureCompression!=TextureImporterCompression.Uncompressed)
        { importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.mipmapEnabled=false; importer.SaveAndReimport(); }
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var sprite=Sprite.Create(tex,new Rect(x,tex.height-y-h,w,h),new Vector2(.5f,0),48,0,SpriteMeshType.FullRect);
        sprite.name="Mine_"+name; AssetDatabase.CreateAsset(sprite,Folder+"/"+sprite.name+".asset"); art[name]=sprite;
    }
    static Transform Child(Transform parent,string name)
    { var go=new GameObject(name); if(parent!=null) go.transform.SetParent(parent,false); return go.transform; }
    static GameObject Place(Sprite sprite,string name,float x,float y,float w,float h,int order,Transform parent,bool key)
    {
        var t=Child(parent,name); var r=t.gameObject.AddComponent<SpriteRenderer>(); r.sprite=sprite;
        r.sortingLayerID=layer; r.sortingOrder=order; if(key) r.sharedMaterial=cutout;
        t.localScale=new Vector3(w/sprite.bounds.size.x,h/sprite.bounds.size.y,1);
        t.position=new Vector3(x,y-sprite.bounds.min.y*t.localScale.y,0); return t.gameObject;
    }
    static GameObject Prop(string id,string name,float x,float y,float w,float h,int order)
    { return Place(art[id],name,x,y,w,h,order,props,true); }
    static BoxCollider2D Box(string name,Transform parent,Vector2 center,Vector2 size,bool trigger)
    {
        var t=Child(parent,name); t.position=center; t.gameObject.layer=trigger?0:6;
        var b=t.gameObject.AddComponent<BoxCollider2D>(); b.size=size; b.isTrigger=trigger; return b;
    }
    static void Block(string name,float left,float right,float bottom,float top)
    {
        var group=Child(terrain,name);
        for(float x=left;x<right-.01f;x+=2)
        {
            float w=Mathf.Min(2,right-x);
            for(float y=bottom;y<top-.01f;y+=2)
                Place(art["fill"],"Rock Fill",x+w/2,y,w,Mathf.Min(2,top-y),4,group,true);
            Place(art["lip"],"Walkable Rock Rim",x+w/2,top-.32f,w,.35f,6,group,true);
        }
        Box(name+" Ground",group,new Vector2((left+right)/2,(bottom+top)/2),new Vector2(right-left,top-bottom),false);
    }
    static void Bridge(float left,float right,float y)
    {
        var group=Child(props,"Timber Bridge");
        for(float x=left;x<right;x+=1.5f)
            Place(art["rail"],"Timber And Rail",x+Mathf.Min(1.5f,right-x)/2,y-.2f,Mathf.Min(1.5f,right-x),.62f,18,group,true);
        Box("Bridge Deck",group,new Vector2((left+right)/2,y-.13f),new Vector2(right-left,.26f),false);
    }
    static void Ladder(int x,int bottom,int top,Tile tile)
    { for(int y=bottom;y<top;y++) ladders.SetTile(new Vector3Int(x,y,0),tile); }
    static void Pool(float left,float right,float bottom,float top,Dictionary<string,Sprite> sprites)
    {
        Place(sprites["Scene7_PoolCore"],"Deep Mine Pool",(left+right)/2,bottom,right-left,top-bottom,11,water,false);
        Place(sprites["ForestReferenceTiles_31"],"Pool Foam",(left+right)/2,top-.12f,right-left,.28f,20,water,false);
        Box("Deep Water Hazard",water,new Vector2((left+right)/2,bottom+.5f),new Vector2(right-left,.9f),true).tag="water";
    }
    static void Falls(float left,float right,float bottom,float top,Dictionary<string,Sprite> sprites)
    {
        Place(sprites["Scene7_WaterCore"],"Blue Mine Cascade",(left+right)/2,bottom,right-left,top-bottom,10,water,false);
        Place(sprites["ForestReferenceTiles_31"],"Cascade Splash",(left+right)/2,bottom-.1f,right-left+.6f,.6f,21,water,false);
    }
}
