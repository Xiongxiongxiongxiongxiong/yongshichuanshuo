using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Builds a new content scene without changing the existing New Game progression.
public static class BuildForestCanyonScene8
{
    private const string ScenePath = "Assets/Scenes/SampleScene_8.unity";
    private const string AtlasPath = "Assets/Legacy-Fantasy - High Forest 2.3/Assets/ForestReferenceTiles.png";
    private const string TileFolder = "Assets/Legacy-Fantasy - High Forest 2.3/Assets/Scene7Tiles/";
    private static Transform terrainRoot, props, waterRoot;
    private static Tilemap terrain, ladders;
    private static Dictionary<string, Sprite> sprites;
    private static int sortingLayer;
    private static GameObject chestTemplate;

    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Scene already exists; preserve the authored scene.");
        var source = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_7.unity", OpenSceneMode.Single);
        var sourceObjects = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        var sourceMap = sourceObjects.Select(t => t.GetComponent<Tilemap>()).First(t => t != null && t.name == "map");
        sortingLayer = sourceMap.GetComponent<TilemapRenderer>().sortingLayerID;
        chestTemplate = sourceObjects.First(t => t.GetComponent<Chest>() != null).gameObject;
        var background = sourceObjects.First(t => t.name == "Forest Background - senlinBG").gameObject;
        var uiTemplate = sourceObjects.First(t => t.GetComponent<LevelCompleteUI>() != null).gameObject;
        var labelTemplate = sourceObjects.First(t => t.name == "Goal Label").gameObject;
        sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s => s.name);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root = new GameObject("Forest Canyon - SampleScene 8").transform;
        terrainRoot = Child(root, "01 Terrain And Collision");
        props = Child(root, "02 Forest And Interactive Props");
        waterRoot = Child(root, "03 Waterfalls And Pools");
        var grid = new GameObject("Canyon Grid", typeof(Grid)); grid.transform.SetParent(terrainRoot, false);
        terrain = MakeMap(grid.transform, "Canyon Terrain", 5);
        ladders = MakeMap(grid.transform, "Forest Decorations", 15);

        // Main route: safe start, lower bridge, grotto ledge, stepping canyon,
        // ascent to the summit, upper bridge and descending finish.
        Block("A Start Cliff", 0, 12, -4, 10);
        Block("A Lower Landing", 12, 18, -4, 6);
        Bridge(18, 25, 6, 6, "B Lower Suspension Bridge");
        Block("C Treasure Island", 25, 38, -4, 6);
        Block("C Grotto Roof", 33, 38, 9, 11);
        Block("C Grotto Right Pillar", 37, 38, 6, 9);
        Block("D First Stepping Rock", 40, 43, 0, 9);
        Block("D Second Stepping Rock", 44, 48, 0, 11);
        Block("D Upper Cliff", 49, 54, 0, 14);
        Block("D Forest Terrace", 55, 61, 8, 16);
        Block("E Bridge Approach", 62, 68, 12, 18);
        Bridge(68, 75, 18, 24, "E Ascending Timber Bridge");
        Block("E Summit", 75, 83, 10, 24);
        Bridge(83, 88, 24, 24, "E Waterfall Suspension Bridge");
        Block("E Far Tower", 88, 92, 10, 24);
        Block("F Goal Cliff", 92, 102, -4, 17);

        // Lower optional route with an accessible grotto and return ladders.
        Block("D Lower Refuge", 54, 61, 0, 5);
        Block("D Lower Return Ledge", 61, 65, 5, 8);
        Block("F Cave Floor", 76, 92, -4, 2);
        Block("F Cave Roof", 76, 92, 8, 10);
        Block("F Cave Left Wall", 75, 76, -4, 10);
        Block("F Cave Right Wall", 91, 92, 2, 8);
        Block("F Cave Entry Ledge", 71, 75, 0, 4);
        Bridge(72, 77, 4, 2, "F Lower Grotto Walkway");
        Ladder(12, 6, 10); Ladder(32, 6, 11); Ladder(47, 5, 11);
        Ladder(60, 5, 16); Ladder(90, 2, 10); Ladder(93, 10, 17);

        Pool(18, 25, -4, 2); Pool(38, 75, -4, 0);
        Waterfall(38.1f, 40, 0, 6, "D Grotto Cascade");
        Waterfall(65, 68, 0, 12, "D Central Falls");
        Waterfall(83.1f, 87.9f, 10, 23, "E Great Waterfall");
        Waterfall(69.5f, 71.2f, 0, 6, "F Thin Cascade");

        var bg = UnityEngine.Object.Instantiate(background, root);
        bg.name = "Forest Background - senlinBG";
        bg.transform.position = new Vector3(51, 15, 15);
        bg.transform.localScale = new Vector3(122, 62, 60);
        // Foreground trees frame the traversal while leaving the landing surfaces clear.
        Prop(32, "A Ancient Tree", 15, 6, 11, 22, -8);
        Prop(32, "D Ancient Tree", 56, 5, 14, 27, -8);
        Prop(32, "E Ancient Tree", 79, 10, 13, 28, -8);
        Prop(33, "A Small Tree", 5, 10, 3.5f, 4.5f, 9);
        Prop(33, "C Small Tree", 35, 11, 2.8f, 3.5f, 9);
        Prop(33, "F Small Tree", 99, 17, 3, 4, 9);

        int[] starts = {0,12,25,33,40,44,49,55,62,75,88,92,54,61,71};
        int[] ends = {12,18,33,38,43,48,54,61,68,83,92,102,61,65,75};
        int[] tops = {10,6,6,11,9,11,14,16,18,24,24,17,5,8,4};
        var random = new System.Random(8026);
        for (int i = 0; i < starts.Length; i++)
        {
            for (float x = starts[i] + 0.6f; x < ends[i] - 0.5f; x += 1.7f)
            {
                int id = new[] {34,35,36,37,38,39}[random.Next(6)];
                float height = id == 39 ? 0.8f : 0.65f + (float)random.NextDouble() * 0.4f;
                Prop(id, "Understory " + i, x, tops[i] - 0.04f, height * 1.1f, height, 11);
            }
        }
        foreach (var p in new[] {new Vector2(6,10),new Vector2(16,6),new Vector2(29,6),new Vector2(52,14),
            new Vector2(65,18),new Vector2(81,24),new Vector2(96,17),new Vector2(80,2),new Vector2(89,2)})
            Prop(46, "Canyon Lantern", p.x, p.y, 1.6f, 2.6f, 14);
        Prop(53, "C Grotto Door", 35, 6, 2.8f, 3.3f, 10);
        Prop(53, "F Hidden Cave Door", 84, 2, 5.5f, 6, 10);
        Prop(48, "Summit Supply Crate", 77, 24, 1.1f, 1.1f, 12);
        Prop(49, "Cave Barrel", 87, 2, 0.9f, 1.2f, 12);
        PlaceChest("C Grotto Treasure", 30, 6);
        PlaceChest("E Summit Treasure", 80, 24);
        PlaceChest("F Hidden Cave Treasure", 83, 2);

        var start = new GameObject("Start Point"); start.transform.SetParent(root, false); start.transform.position = new Vector3(3,10.05f,0);
        var finish = new GameObject("Goal Point"); finish.transform.SetParent(root, false); finish.transform.position = new Vector3(99,17.05f,0);
        Prop(55, "Finish Stone Marker", 99, 17, 1.2f, 1.7f, 14);
        var ui = UnityEngine.Object.Instantiate(uiTemplate); SceneManager.MoveGameObjectToScene(ui, scene);
        ui.name = "Level Complete Canvas";
        var completion = ui.GetComponent<LevelCompleteUI>(); completion.panel.SetActive(false);
        var goal = new GameObject("Forest Canyon Level Goal", typeof(BoxCollider2D), typeof(LevelGoal));
        goal.transform.SetParent(root, false); goal.transform.position = new Vector3(99,18,0);
        goal.GetComponent<BoxCollider2D>().isTrigger = true; goal.GetComponent<BoxCollider2D>().size = new Vector2(2,3);
        goal.GetComponent<LevelGoal>().completionUI = completion;
        var label = UnityEngine.Object.Instantiate(labelTemplate, root); label.name = "Goal Label";
        label.transform.position = new Vector3(98,20,0); label.GetComponent<TextMesh>().text = "瀑布峡谷 · 终点";

        var bounds = new GameObject("Camera_Bounds", typeof(PolygonCollider2D)); bounds.transform.SetParent(root, false);
        bounds.tag = "Bounds"; var polygon = bounds.GetComponent<PolygonCollider2D>(); polygon.isTrigger = true;
        polygon.points = new[] {new Vector2(-3,-6),new Vector2(105,-6),new Vector2(105,38),new Vector2(-3,38)};
        var fall = Box("Fall Death Zone", root, new Vector2(51,-7), new Vector2(130,3), true);
        fall.gameObject.tag = "water";
        EditorSceneManager.SaveScene(scene, ScenePath);
        string guid = AssetDatabase.AssetPathToGUID(ScenePath);
        var data = ScriptableObject.CreateInstance<GameSceneSO>(); data.sceneType = SceneType.loaction;
        data.sceneReference = new UnityEngine.AddressableAssets.AssetReference(guid);
        AssetDatabase.CreateAsset(data, "Assets/DataSO/GameScene/Game Scene8.asset");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entry = settings.CreateOrMoveEntry(guid, settings.FindGroup("Scenes")); entry.address = "SampleScene_8";
        EditorUtility.SetDirty(settings); completion.retryScene = data; completion.retryPosition = start.transform.position;
        EditorUtility.SetDirty(completion);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(source, true); SceneManager.SetActiveScene(scene);
        return "Saved SampleScene_8: main route, cave branch, 6 ladders, 3 chests, 4 waterfalls, goal and Addressables.";
    }

    private static Transform Child(Transform parent, string name)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static Tilemap MakeMap(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(parent, false);
        var renderer = go.GetComponent<TilemapRenderer>(); renderer.sortingLayerID = sortingLayer; renderer.sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }
    private static TileBase Tile(string name) { return AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + name + ".asset"); }
    private static void Block(string name, int left, int right, int bottom, int top)
    {
        for (int x = left; x < right; x++) for (int y = bottom; y < top; y++)
        {
            string tile = y == top - 1 ? (x == left ? "Scene7_GrassLeft" : x == right-1 ? "Scene7_GrassRight" : "Scene7_GrassRepeat") :
                x == left ? "Scene7_LeftWall" : x == right-1 ? "Scene7_RightWall" : "Scene7_WallWhole";
            terrain.SetTile(new Vector3Int(x,y,0), Tile(tile));
        }
        Box(name, terrainRoot, new Vector2((left+right)*0.5f,(bottom+top)*0.5f), new Vector2(right-left,top-bottom), false);
    }
    private static BoxCollider2D Box(string name, Transform parent, Vector2 center, Vector2 size, bool trigger)
    {
        var go = new GameObject(name, typeof(BoxCollider2D)); go.transform.SetParent(parent, false); go.transform.position = center;
        go.layer = trigger ? 0 : 6; var box = go.GetComponent<BoxCollider2D>(); box.size = size; box.isTrigger = trigger; return box;
    }
    private static GameObject Place(Sprite sprite, string name, float x, float bottom, float width, float height, int order, Transform parent)
    {
        var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false);
        var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingLayerID = sortingLayer; renderer.sortingOrder = order;
        go.transform.localScale = new Vector3(width/sprite.bounds.size.x,height/sprite.bounds.size.y,1);
        go.transform.position = new Vector3(x,bottom - sprite.bounds.min.y * go.transform.localScale.y,0);
        return go;
    }
    private static GameObject Prop(int id, string name, float x, float y, float width, float height, int order)
    { return Place(sprites["ForestReferenceTiles_"+id],name,x,y,width,height,order,props); }
    private static void Bridge(float left, float right, float y0, float y1, string name)
    {
        var parent = Child(props, name);
        int pieces = Mathf.CeilToInt((right-left)/0.5f);
        for (int i=0; i<pieces; i++)
        {
            float x = Mathf.Lerp(left,right,(i+0.5f)/pieces);
            float sag = y0==y1 ? Mathf.Sin((i+0.5f)/pieces*Mathf.PI)*0.3f : 0f;
            float y = Mathf.Lerp(y0,y1,(i+0.5f)/pieces)-sag;
            Place(sprites["Scene7_BridgeRepeat"],"Timber Deck",x,y-.18f,(right-left)/pieces+.04f,.22f,13,parent);
            Box("Walkable Bridge Deck",parent,new Vector2(x,y-.10f),new Vector2((right-left)/pieces+.04f,.2f),false);
        }
        for (int i=0; i<=3; i++)
        {
            float t=i/3f, x=Mathf.Lerp(left,right,t), y=Mathf.Lerp(y0,y1,t);
            var post = Prop(i==3?41:40,"Bridge Anchor",x,y-.5f,.65f,1.9f,14); post.transform.SetParent(parent,true);
            if(i<3)
            {
                var rope = Prop(42,"Suspension Rope",x+(right-left)/6f,y+.25f,(right-left)/3f+.1f,1.2f,14);
                rope.transform.SetParent(parent,true); rope.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(y1-y0,right-left)*Mathf.Rad2Deg);
            }
        }
    }
    private static void Ladder(int x, int bottom, int top)
    { for(int y=bottom; y<top; y++) ladders.SetTile(new Vector3Int(x,y,0),Tile("ForestReference_Bridge_6")); }
    private static void Pool(float left, float right, float bottom, float top)
    {
        var go=Place(sprites["Scene7_PoolCore"],"Deep Canyon Pool",(left+right)*.5f,bottom,right-left,top-bottom,7,waterRoot);
        Place(sprites["ForestReferenceTiles_31"],"Pool Foam",(left+right)*.5f,top-.15f,right-left,.35f,10,waterRoot);
        var hazard=Box("Deep Water Hazard",waterRoot,new Vector2((left+right)*.5f,bottom+(top-bottom)*.35f),new Vector2(right-left,(top-bottom)*.7f),true);
        hazard.gameObject.tag="water";
    }
    private static void Waterfall(float left,float right,float bottom,float top,string name)
    {
        // Continuous core avoids the dark horizontal gutters in the original full-frame slices.
        Place(sprites["Scene7_WaterCore"],name,(left+right)*.5f,bottom,right-left,top-bottom,3,waterRoot);
        Place(sprites["ForestReferenceTiles_31"],name+" Splash",(left+right)*.5f,bottom-.1f,right-left+.7f,.7f,12,waterRoot);
    }
    private static void PlaceChest(string name,float x,float y)
    {
        var chest=UnityEngine.Object.Instantiate(chestTemplate,props); chest.name=name;
        chest.transform.localScale=new Vector3(1.6f,1.4f,1);
        var r=chest.GetComponent<SpriteRenderer>(); chest.transform.position=new Vector3(x,y-r.sprite.bounds.min.y*chest.transform.localScale.y,0);
        r.sortingOrder=15; chest.SetActive(true);
    }
}

