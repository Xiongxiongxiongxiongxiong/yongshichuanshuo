using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Editor-only construction helpers for the approved panorama/physical-route workflow.
public sealed class PanoramaLevelAuthoring
{
    const float Pixels=24,Height=683;
    readonly Scene scene;
    readonly Transform root,ground,props;
    readonly Tilemap ladderMap;
    readonly Chest chestTemplate;
    readonly LevelCompleteUI completion;
    readonly Camera overview;
    readonly int number,sorting;
    readonly string folder,filename;
    readonly List<PolygonCollider2D> terrain=new List<PolygonCollider2D>();
    public static Vector2 Point(float x,float y){return new Vector2(x/Pixels,(Height-y)/Pixels);}

    public PanoramaLevelAuthoring(int number,string title,string folder,string filename,Chest template,LevelCompleteUI uiTemplate)
    {
        this.number=number;this.folder=folder;this.filename=filename;chestTemplate=template;
        sorting=template.GetComponent<SpriteRenderer>().sortingLayerID;
        scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        root=Child(null,title+" - SampleScene "+number);
        ground=Child(root,"01 Traced Natural Terrain");ground.gameObject.layer=6;
        ground.gameObject.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
        ground.gameObject.AddComponent<CompositeCollider2D>().geometryType=CompositeCollider2D.GeometryType.Polygons;
        props=Child(root,"02 Climbing And Treasure");
        var grid=Child(root,"Climbing Grid");grid.gameObject.AddComponent<Grid>();
        var map=Child(grid,"Forest Decorations");ladderMap=map.gameObject.AddComponent<Tilemap>();map.gameObject.AddComponent<TilemapRenderer>().enabled=false;
        string path=folder+filename+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=72;
        importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);var image=Child(root,"03 Generated Wilderness Panorama");
        var r=image.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingLayerID=sorting;r.sortingOrder=-10;
        image.position=new Vector3(2048/Pixels/2,Height/Pixels/2,0);
        image.localScale=new Vector3(2048/Pixels/sprite.bounds.size.x,Height/Pixels/sprite.bounds.size.y,1);
        completion=Object.Instantiate(uiTemplate.gameObject).GetComponent<LevelCompleteUI>();completion.name="Level Complete Canvas";completion.panel.SetActive(false);
        overview=Child(root,"Editor Overview Camera").gameObject.AddComponent<Camera>();overview.tag="EditorOnly";overview.enabled=false;
        overview.orthographic=true;overview.orthographicSize=Height/Pixels/2;overview.transform.position=new Vector3(2048/Pixels/2,Height/Pixels/2,-20);
        overview.clearFlags=CameraClearFlags.SolidColor;overview.backgroundColor=new Color(.025f,.03f,.04f);overview.allowMSAA=false;
    }
    public void Path(string name,params float[] xy)
    {
        var t=Child(ground,name);t.gameObject.layer=6;int count=xy.Length/2;var points=new Vector2[count*2];
        for(int i=0;i<count;i++){points[i]=Point(xy[2*i],xy[2*i+1]);points[points.Length-1-i]=points[i]+Vector2.down*.35f;}
        var polygon=t.gameObject.AddComponent<PolygonCollider2D>();polygon.points=points;polygon.usedByComposite=true;terrain.Add(polygon);
    }
    public void Ladder(float x,float bottom,float top)
    {
        var a=Point(x,bottom);var b=Point(x,top);int cell=Mathf.RoundToInt(a.x-.5f),low=Mathf.FloorToInt(a.y),high=Mathf.CeilToInt(b.y);
        var tile=AssetDatabase.LoadAssetAtPath<Tile>("Assets/GeneratedMineV2/ClimbLadder.asset");
        for(int y=low;y<high;y++)ladderMap.SetTile(new Vector3Int(cell,y,0),tile);
        var t=Child(props,"Climbable Ladder "+x);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedMineV2/Ladder.asset");
        r.sortingLayerID=sorting;r.sortingOrder=5;t.position=new Vector3(cell+.5f,low,0);FixClimbableLadderProportions.Configure(r,.8f,high-low);
    }
    public void Treasure(string name,float x,float y)
    {
        var chest=Object.Instantiate(chestTemplate.gameObject,props);chest.name=name;chest.transform.localScale=Vector3.one*.25f;chest.transform.position=Point(x,y);chest.SetActive(true);
    }
    public void Finish(Vector3 start,Vector2 exit)
    {
        Child(root,"Start Point").position=start;Child(root,"Goal Point").position=exit;
        Trigger("Wilderness Cave Exit",exit+Vector2.up*1.1f,new Vector2(2.2f,2.6f)).AddComponent<LevelGoal>().completionUI=completion;
        Trigger("Fall And Deep Water Hazard",new Vector2(43,.8f),new Vector2(95,1.3f)).tag="water";
        var bounds=Child(root,"Camera_Bounds");bounds.tag="Bounds";var polygon=bounds.gameObject.AddComponent<PolygonCollider2D>();polygon.isTrigger=true;
        polygon.points=new[]{Vector2.zero,new Vector2(2048/Pixels,0),new Vector2(2048/Pixels,Height/Pixels),new Vector2(0,Height/Pixels)};
        string path="Assets/Scenes/SampleScene_"+number+".unity";EditorSceneManager.SaveScene(scene,path);
        var data=ScriptableObject.CreateInstance<GameSceneSO>();data.sceneType=SceneType.loaction;
        data.sceneReference=new UnityEngine.AddressableAssets.AssetReference(AssetDatabase.AssetPathToGUID(path));
        AssetDatabase.CreateAsset(data,"Assets/DataSO/GameScene/Game Scene"+number+".asset");completion.retryScene=data;completion.retryPosition=start;EditorUtility.SetDirty(completion);
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),settings.FindGroup("Scenes")).address="SampleScene_"+number;
        EditorUtility.SetDirty(settings);EditorSceneManager.SaveScene(scene);
        BuildMineExpansion.Render(overview,folder+filename+"_Overview.png");
        RenderCollisionReview();EditorSceneManager.SaveScene(scene);
    }
    void RenderCollisionReview()
    {
        var temp=Child(root,"TEMP Collision Review");var material=new Material(Shader.Find("Sprites/Default"));
        try
        {
            foreach(var p in terrain)
            {
                var t=Child(temp,p.name);var line=t.gameObject.AddComponent<LineRenderer>();line.sharedMaterial=material;
                line.startColor=line.endColor=Color.green;line.widthMultiplier=.025f;line.sortingLayerID=sorting;line.sortingOrder=100;line.positionCount=p.points.Length+1;
                for(int i=0;i<=p.points.Length;i++)line.SetPosition(i,(Vector3)p.points[i%p.points.Length]+Vector3.back);
            }
            BuildMineExpansion.Render(overview,folder+filename+"_CollisionReview.png");
        }
        finally{Object.DestroyImmediate(temp.gameObject);Object.DestroyImmediate(material);}
    }
    GameObject Trigger(string name,Vector2 p,Vector2 size){var t=Child(root,name);t.position=p;var b=t.gameObject.AddComponent<BoxCollider2D>();b.size=size;b.isTrigger=true;return t.gameObject;}
    static Transform Child(Transform parent,string name){var go=new GameObject(name);if(parent!=null)go.transform.SetParent(parent,false);return go.transform;}
}

