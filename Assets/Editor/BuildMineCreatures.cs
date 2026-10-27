using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildMineCreatures
{
    private const string Folder = "Assets/GeneratedMonsters/Mine";
    private const string Prefabs = "Assets/Prefabs/MineMonsters";
    private static readonly string[] Names = { "CrystalCrawler", "LanternBat", "PickaxeImp" };

    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
        if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder("Assets/Prefabs", "MineMonsters");
        if (!AssetDatabase.IsValidFolder(Folder + "/Frames")) AssetDatabase.CreateFolder(Folder, "Frames");
        var hud = AssetDatabase.LoadAllAssetsAtPath("Assets/Legacy-Fantasy - High Forest 2.3/HUD/Base-01.png").OfType<Sprite>().ToArray();
        int enemyLayer = LayerMask.NameToLayer("Enetmy");
        if (enemyLayer < 0) throw new InvalidOperationException("Existing enemy layer missing.");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");
        foreach (string name in Names)
        {
            string path = Folder + "/" + name + ".png";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = name == "CrystalCrawler" ? 120f : 130f;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var cpu = new Texture2D(2,2); cpu.LoadImage(System.IO.File.ReadAllBytes(path));
            int cellW = texture.width / 4, cellH = texture.height / 6;
            var frames = new Sprite[24];
            for (int row = 0; row < 6; row++) for (int col = 0; col < 4; col++)
            {
                Rect rect = new Rect(col*cellW, texture.height-(row+1)*cellH, cellW, cellH);
                Vector2 pivot;
                if (name == "LanternBat")
                {
                    float[] x = {140,177,177,190,170,150};
                    float[] y = {90,90,90,95,100,65};
                    pivot = new Vector2(x[row]/cellW,y[row]/cellH);
                }
                else
                {
                    int foot = cellH;
                    for(int y=0;y<cellH;y++) for(int x=cellW/4;x<cellW*3/4;x++)
                        if(cpu.GetPixel((int)rect.x+x,(int)rect.y+y).a>.7f) foot=Mathf.Min(foot,y);
                    if(foot==cellH) throw new InvalidOperationException("Empty frame: "+name);
                    pivot=new Vector2(.5f, (foot+1f)/cellH);
                }
                var sprite=Sprite.Create(texture,rect,pivot,importer.spritePixelsPerUnit,0,SpriteMeshType.FullRect);
                sprite.name=name+"_"+row+"_"+col;
                frames[row*4+col]=SaveSprite(sprite,Folder+"/Frames/"+sprite.name+".asset");
            }
            UnityEngine.Object.DestroyImmediate(cpu);
            var go=new GameObject(name); go.SetActive(false); go.layer=enemyLayer;
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=frames[0]; sr.sortingOrder=25;
            // Use the same sorting layer as the existing forest enemies.
            var bee=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bee.prefab");
            if(bee!=null) sr.sortingLayerID=bee.GetComponent<SpriteRenderer>().sortingLayerID;
            var body=go.AddComponent<Rigidbody2D>(); body.mass=1.5f; body.freezeRotation=true;
            body.gravityScale=name=="LanternBat"?0:3;
            body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;
            var box=go.AddComponent<BoxCollider2D>();
            box.size=name=="CrystalCrawler"?new Vector2(1.65f,1.15f):name=="LanternBat"?new Vector2(1.1f,1.2f):new Vector2(1.05f,1.55f);
            box.offset=name=="LanternBat"?Vector2.zero:new Vector2(0,box.size.y*.5f);
            var hp=go.AddComponent<Character>(); hp.maxHp=hp.CurrentHp=name=="CrystalCrawler"?30:name=="LanternBat"?18:40;
            hp.newGameEvent=bee.GetComponent<Character>().newGameEvent;
            hp.invulnerableDuration=.18f;
            hp.OnDead=new UnityEngine.Events.UnityEvent();
            hp.OnTakeDamage=new UnityEngine.Events.UnityEvent<Transform>();
            hp.OnHealthChange=new UnityEngine.Events.UnityEvent<Character>();
            var damage=go.AddComponent<Attack>(); damage.enabled=false;
            damage.damage=name=="CrystalCrawler"?12:name=="LanternBat"?8:14;
            var ai=go.AddComponent<MineCreature>(); ai.species=(MineCreature.Species)Array.IndexOf(Names,name);
            ai.frames=frames; ai.damageSource=damage;
            var flash=go.AddComponent<DamageFlash>(); flash.flashMaterial=material;
            var bar=go.AddComponent<MonsterHealthBar>(); var serial=new SerializedObject(bar);
            serial.FindProperty("backgroundSprite").objectReferenceValue=hud.First(s=>s.name=="Base-01_7");
            serial.FindProperty("healthSprite").objectReferenceValue=hud.First(s=>s.name=="Base-01_29");
            serial.FindProperty("trailSprite").objectReferenceValue=hud.First(s=>s.name=="Base-01_14");
            serial.ApplyModifiedPropertiesWithoutUndo();
            if(name=="PickaxeImp")
            {
                var ore=Sprite.Create(texture,new Rect(211,texture.height-189,30,38),new Vector2(.5f,.5f),130,0,SpriteMeshType.FullRect);
                ore.name="ThrownOre"; ai.oreSprite=SaveSprite(ore,Folder+"/ThrownOre.asset");
            }
            go.SetActive(true); PrefabUtility.SaveAsPrefabAsset(go,Prefabs+"/"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        return "Created 3 enemy prefabs, 72 animation frames, ore projectile sprite, flash and health bars.";
    }
    private static Sprite SaveSprite(Sprite generated,string path)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(existing==null){AssetDatabase.CreateAsset(generated,path);return generated;}
        EditorUtility.CopySerialized(generated,existing); EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(generated); return existing;
    }

    public static string PlaceInMine()
    {
        if(Application.isPlaying) throw new InvalidOperationException("Place in Edit Mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/SampleScene_8.unity") throw new InvalidOperationException("Open SampleScene_8 first.");
        if(GameObject.Find("Mine Creatures")!=null) return "Mine creatures already placed; preserving existing instances.";
        var root=new GameObject("Mine Creatures"); Physics2D.SyncTransforms();
        float[] xs={30,14,6};
        for(int i=0;i<3;i++)
        {
            Vector3 position;
            if(i==1)
            {
                var ceiling=Physics2D.Raycast(new Vector2(xs[i],8.5f),Vector2.up,8,1<<6);
                if(!ceiling) throw new InvalidOperationException("Bat ceiling missing.");
                position=new Vector3(xs[i],ceiling.point.y-1.45f,0);
            }
            else
            {
                var floor=Physics2D.Raycast(new Vector2(xs[i],i==0?11:10),Vector2.down,12,1<<6);
                if(!floor) throw new InvalidOperationException("Enemy floor missing.");
                position=new Vector3(xs[i],floor.point.y+.04f,0);
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+Names[i]+".prefab");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            instance.transform.SetParent(root.transform,true); instance.transform.position=position;
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Placed one of each in SampleScene_8: lower treasure guard, cave ceiling bat, timber bridge crawler.";
    }
}
