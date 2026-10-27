using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildCrystalForeman
{
    private const string Folder="Assets/GeneratedMonsters/Mine/Foreman";
    public const string PrefabPath="Assets/Prefabs/MineMonsters/CrystalPickaxeForeman.prefab";

    public static string Build()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        if(!AssetDatabase.IsValidFolder(Folder+"/Frames")) AssetDatabase.CreateFolder(Folder,"Frames");
        var frames=Slice("CrystalForeman",8,90f,false);
        var effects=Slice("CrystalForemanEffects",3,181f,true);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MineMonsters/PickaxeImp.prefab");
        if(source==null) throw new InvalidOperationException("Existing mine monster prefab missing.");
        var go=UnityEngine.Object.Instantiate(source); go.name="Crystal Pickaxe Foreman"; go.SetActive(false);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<MineCreature>());
        var visual=go.GetComponent<SpriteRenderer>(); visual.sprite=frames[0];
        var hp=go.GetComponent<Character>(); hp.maxHp=hp.CurrentHp=120; hp.invulnerableDuration=.18f;
        var body=go.GetComponent<Rigidbody2D>(); body.mass=3; body.gravityScale=3;
        var box=go.GetComponent<BoxCollider2D>(); box.size=new Vector2(1.45f,2.05f); box.offset=new Vector2(0,1.025f);
        var melee=go.GetComponent<Attack>(); melee.enabled=false; melee.damage=16;
        var slam=go.AddComponent<Attack>(); slam.enabled=false; slam.damage=22;
        var ai=go.AddComponent<CrystalPickaxeForeman>(); ai.frames=frames; ai.effectFrames=effects;
        ai.meleeDamage=melee; ai.slamDamage=slam;
        go.SetActive(true); PrefabUtility.SaveAsPrefabAsset(go,PrefabPath); UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        return "Elite prefab saved: 120 HP, 8 animation rows, ground slam and three-crystal volley, existing flash and overhead bar.";
    }
    private static Sprite[] Slice(string name,int rows,float ppu,bool effect)
    {
        string path=Folder+"/"+name+".png"; AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=ppu; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
        importer.alphaIsTransparency=true; importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var cpu=new Texture2D(2,2); cpu.LoadImage(System.IO.File.ReadAllBytes(path));
        var result=new Sprite[rows*4];
        for(int row=0;row<rows;row++) for(int col=0;col<4;col++)
        {
            int x0=Mathf.RoundToInt(col*texture.width/4f),x1=Mathf.RoundToInt((col+1)*texture.width/4f);
            int y0=Mathf.RoundToInt(texture.height-(row+1)*texture.height/(float)rows);
            int y1=Mathf.RoundToInt(texture.height-row*texture.height/(float)rows);
            Rect rect=new Rect(x0,y0,x1-x0,y1-y0);
            int foot=y1-y0,opaque=0; var anchorPixels=new List<int>();
            for(int y=0;y<y1-y0;y++) for(int x=0;x<x1-x0;x++)
            {
                Color color=cpu.GetPixel(x0+x,y0+y);
                if(color.a<.65f) continue;
                opaque++;
                if(x>(x1-x0)*.2f&&x<(x1-x0)*.8f) foot=Mathf.Min(foot,y);
                if(y<(y1-y0)*.4f&&color.r<.6f&&color.g<.6f&&color.b<.65f&&
                    Mathf.Max(color.r,Mathf.Max(color.g,color.b))-Mathf.Min(color.r,Mathf.Min(color.g,color.b))<.2f)
                    anchorPixels.Add(x);
            }
            Vector2 pivot;
            if(opaque<30)
            {
                // The source holds its final charge pose: preserve that hold without showing an empty cell.
                if(col==0) throw new InvalidOperationException("Empty leading frame: "+name+" row "+row);
                rect=result[row*4+col-1].rect;
                pivot=new Vector2(result[row*4+col-1].pivot.x/rect.width,result[row*4+col-1].pivot.y/rect.height);
            }
            else if(effect) pivot=new Vector2(.5f,row==1?.5f:Mathf.Clamp01((foot+1f)/rect.height));
            else
            {
                anchorPixels.Sort(); float x=anchorPixels.Count>30?anchorPixels[anchorPixels.Count/2]:rect.width*.5f;
                pivot=new Vector2(x/rect.width,Mathf.Clamp01((foot+1f)/rect.height));
            }
            var sprite=Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect); sprite.name=name+"_"+row+"_"+col;
            string output=Folder+"/Frames/"+sprite.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(output);
            if(existing==null){AssetDatabase.CreateAsset(sprite,output);result[row*4+col]=sprite;}
            else{EditorUtility.CopySerialized(sprite,existing);EditorUtility.SetDirty(existing);result[row*4+col]=existing;UnityEngine.Object.DestroyImmediate(sprite);}
        }
        UnityEngine.Object.DestroyImmediate(cpu); return result;
    }
    public static string Place()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Place in Edit Mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene_8.unity") throw new InvalidOperationException("Open SampleScene_8 first.");
        if(UnityEngine.Object.FindObjectOfType<CrystalPickaxeForeman>()!=null) return "Existing elite preserved.";
        Physics2D.SyncTransforms();
        var floor=Physics2D.Raycast(new Vector2(38,22),Vector2.down,12,1<<6);
        if(!floor) throw new InvalidOperationException("Elite landing surface missing.");
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
        instance.transform.position=new Vector3(38,floor.point.y+.04f,0);
        var root=GameObject.Find("Mine Creatures"); if(root!=null) instance.transform.SetParent(root.transform,true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Placed Crystal Pickaxe Foreman on wide crystal-sanctuary approach at "+instance.transform.position;
    }
}
