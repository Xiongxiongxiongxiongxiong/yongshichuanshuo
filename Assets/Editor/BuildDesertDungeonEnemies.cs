using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildDesertDungeonEnemies
{
    public const string Folder = "Assets/GeneratedMonsters/DesertDungeon";
    public const string Prefabs = "Assets/Prefabs/DesertDungeonMonsters";
    public static readonly string[] Names = { "RedtailScorpion", "SandJawWorm", "ThornballCactus", "RustSkeleton", "SoulLanternWraith", "ChainAmbushBeast", "SandPincerGuard", "SoulboundJailer" };
    public static readonly int[] Health = { 35, 28, 30, 40, 24, 32, 110, 120 };
    public static readonly Vector2[] Sizes = { new Vector2(1.1f,.7f),new Vector2(1.1f,.7f),new Vector2(.85f,1.05f),new Vector2(.8f,1.4f),new Vector2(.7f,.9f),new Vector2(1.1f,.8f),new Vector2(1.9f,1.4f),new Vector2(1.15f,2f) };
    static void FolderAt(string path)
    { if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');FolderAt(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1)); }
    [MenuItem("Tools/Monsters/Build Desert And Dungeon Enemies")]
    public static void BuildMenu() { Debug.Log(Build()); }
    public static string Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Build in Edit Mode.");
        FolderAt(Prefabs);FolderAt(Folder+"/Frames");
        var desert=Slice("DesertCombatEffects",4,160,true,false);var dungeon=Slice("DungeonCombatEffects",4,160,true,false);
        var template=AssetDatabase.LoadAssetAtPath<GameObject>(BuildWildernessCaveCreatures.Prefabs+"/CaveMossLizard.prefab");
        if(template==null)throw new InvalidOperationException("Existing health/flash monster template missing.");
        for(int i=0;i<Names.Length;i++)
        {
            bool elite=i>=6;var frames=Slice(Names[i],elite?8:6,elite?80:150,false,i==4);
            if(i==6){var charge=Slice("SandPincerCharge",1,80,false,false);for(int c=0;c<4;c++)frames[16+c]=charge[c];}
            var go=UnityEngine.Object.Instantiate(template);go.SetActive(false);go.name=Names[i];
            UnityEngine.Object.DestroyImmediate(go.GetComponent<WildernessCaveCreature>());
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=frames[0];sr.color=Color.white;
            var hp=go.GetComponent<Character>();hp.maxHp=hp.CurrentHp=Health[i];hp.invulnerableDuration=.18f;
            var rb=go.GetComponent<Rigidbody2D>();rb.mass=elite?3:1.5f;rb.gravityScale=i==4?0:3;rb.constraints=RigidbodyConstraints2D.FreezeRotation;
            if(i==2)rb.constraints|=RigidbodyConstraints2D.FreezePositionX;
            var box=go.GetComponent<BoxCollider2D>();box.size=Sizes[i];box.offset=i==4?Vector2.zero:Vector2.up*Sizes[i].y*.5f;
            var attack=go.GetComponent<Attack>();attack.enabled=false;attack.damage=new[]{10,12,7,12,9,11,14,15}[i];
            var ai=go.AddComponent<DesertDungeonEnemy>();ai.kind=(DesertDungeonEnemy.Kind)i;ai.frames=frames;
            ai.effectFrames=i<3||i==6?desert:dungeon;ai.damageSource=attack;
            var bar=new SerializedObject(go.GetComponent<MonsterHealthBar>());bar.FindProperty("verticalOffset").floatValue=i==4?.6f:.35f;bar.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true);PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/"+Names[i]+".prefab");UnityEngine.Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();return "Built eight enemy prefabs with 208 character frames and 32 effect frames.";
    }
    static Sprite[] Slice(string name,int rows,float ppu,bool effects,bool floating)
    {
        string path=Folder+"/"+name+".png";AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=ppu;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var cpu=new Texture2D(2,2);cpu.LoadImage(System.IO.File.ReadAllBytes(path));
        var result=new Sprite[rows*4];
        for(int col=0;col<4;col++)
        {
            int x0=Mathf.RoundToInt(col*texture.width/4f),x1=Mathf.RoundToInt((col+1)*texture.width/4f);
            // Detect complete vertical silhouettes per column, so tall elite windups are not clipped by a nominal grid.
            var spans=new List<Vector2Int>();int begin=-1,last=-1;
            for(int top=0;top<texture.height;top++)
            {
                int n=0;for(int x=x0;x<x1;x++)if(cpu.GetPixel(x,texture.height-1-top).a>.7f)n++;
                if(n>=6){if(begin<0)begin=top;last=top;}
                else if(begin>=0&&top-last>6){if(last-begin>10)spans.Add(new Vector2Int(begin,last+1));begin=-1;}
            }
            if(begin>=0&&last-begin>10)spans.Add(new Vector2Int(begin,last+1));
            while(spans.Count>rows)
            {
                int index=0,gap=int.MaxValue;for(int k=0;k<spans.Count-1;k++){int g=spans[k+1].x-spans[k].y;if(g<gap){gap=g;index=k;}}
                spans[index]=new Vector2Int(spans[index].x,spans[index+1].y);spans.RemoveAt(index+1);
            }
            bool detected=spans.Count==rows;
            for(int row=0;row<rows;row++)
            {
                int top=detected?Mathf.Max(0,spans[row].x-3):Mathf.RoundToInt(row*texture.height/(float)rows);
                int bottom=detected?Mathf.Min(texture.height,spans[row].y+3):Mathf.RoundToInt((row+1)*texture.height/(float)rows);
                Rect rect=new Rect(x0,texture.height-bottom,x1-x0,bottom-top);int foot=(int)rect.height,opaque=0;
                for(int y=0;y<rect.height;y++)for(int x=0;x<rect.width;x++)if(cpu.GetPixel(x0+x,(int)rect.y+y).a>.7f){opaque++;if(x>rect.width*.2f&&x<rect.width*.8f)foot=Mathf.Min(foot,y);}
                if(opaque<20)throw new InvalidOperationException("Empty sprite "+name+" "+row+"/"+col);
                Vector2 pivot=floating?new Vector2(.5f,.5f):new Vector2(.5f,Mathf.Clamp01((foot+1f)/rect.height));
                if(name=="SoulboundJailer"||name=="RustSkeleton")
                {
                    var feet=new List<int>();
                    for(int y=0;y<rect.height*.28f;y++)for(int x=(int)(rect.width*.15f);x<rect.width*.8f;x++)
                    {var color=cpu.GetPixel(x0+x,(int)rect.y+y);if(color.a>.7f&&color.r>=color.b*.9f&&Mathf.Max(color.r,Mathf.Max(color.g,color.b))<.65f)feet.Add(x);}
                    if(feet.Count>30){feet.Sort();pivot.x=feet[feet.Count/2]/rect.width;}
                }
                if(effects&&(row==0||name=="DungeonCombatEffects"&&row==2))pivot=new Vector2(.5f,.5f);
                float scale=effects?rect.width/(row==0?.5f:row==2&&name=="DungeonCombatEffects"?.8f:1.1f):ppu*(texture.width/(rows==8?887f:1024f));
                var sprite=Sprite.Create(texture,rect,pivot,scale,0,SpriteMeshType.FullRect);sprite.name=name+"_"+row+"_"+col;
                string output=Folder+"/Frames/"+sprite.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(output);
                if(existing==null){AssetDatabase.CreateAsset(sprite,output);result[row*4+col]=sprite;}
                else{EditorUtility.CopySerialized(sprite,existing);EditorUtility.SetDirty(existing);result[row*4+col]=existing;UnityEngine.Object.DestroyImmediate(sprite);}
            }
        }
        UnityEngine.Object.DestroyImmediate(cpu);return result;
    }
    [MenuItem("Tools/Monsters/Place Desert And Dungeon Enemies In Scenes19-24")]
    public static void PlaceMenu(){Debug.Log(PlaceAll());}
    public static string PlaceAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Place in Edit Mode.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene work must be preserved.");
        var setup=EditorSceneManager.GetSceneManagerSetup();string report="";
        // Source-art pixel locations, on separate broad routes rather than ladder exits or narrow jumping pillars.
        Vector2[][] locations={
            new[]{new Vector2(370,185),new Vector2(820,453),new Vector2(1200,332),new Vector2(900,330)},
            new[]{new Vector2(450,340),new Vector2(720,554),new Vector2(1180,329),new Vector2(450,109)},
            new[]{new Vector2(480,164),new Vector2(800,444),new Vector2(1120,348),new Vector2(880,345)},
            new[]{new Vector2(380,293),new Vector2(700,493),new Vector2(220,541),new Vector2(400,124)},
            new[]{new Vector2(560,320),new Vector2(820,440),new Vector2(550,549),new Vector2(500,143)},
            new[]{new Vector2(850,376),new Vector2(350,550),new Vector2(900,601),new Vector2(500,165)}
        };
        try{for(int n=19;n<=24;n++)
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_"+n+".unity");
            if(scene.GetRootGameObjects().Any(g=>g.name=="Desert Dungeon Encounters")){report+=n+" existing encounters preserved\n";continue;}
            var root=new GameObject("Desert Dungeon Encounters");Physics2D.SyncTransforms();
            for(int j=0;j<4;j++)
            {
                int index=j==3?(n<=21?6:7):(n<=21?j:j+3);Vector2 point=PanoramaLevelAuthoring.Point(locations[n-19][j].x,locations[n-19][j].y);
                if(index!=4){var floor=Physics2D.Raycast(point+Vector2.up*.7f,Vector2.down,1.5f,1<<6);if(!floor)throw new InvalidOperationException("Missing floor "+n+" "+Names[index]);point.y=floor.point.y+.04f;}
                else if(Physics2D.OverlapBox(point,new Vector2(1,1.2f),0,1<<6))throw new InvalidOperationException("Wraith starts inside terrain "+n);
                if(j==3)foreach(float dx in new[]{-4f,4f}){var edge=Physics2D.Raycast(point+new Vector2(dx,.5f),Vector2.down,1,1<<6);if(!edge||Mathf.Abs(edge.point.y-point.y)>.35f)throw new InvalidOperationException("Elite arena too narrow "+n);}
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+Names[index]+".prefab"),scene);
                go.transform.SetParent(root.transform,true);go.transform.position=point;report+=n+" "+Names[index]+" "+point+"\n";
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}return report;
    }
}


