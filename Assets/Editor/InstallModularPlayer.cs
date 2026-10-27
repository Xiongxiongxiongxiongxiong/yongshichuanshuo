using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallModularPlayer
{
    private const string Folder = "Assets/player/ModularV1/";
    private const string Sheet = Folder + "AdventurerActions-v1.png";
    private const string Concept = Folder + "AdventurerModularConcept-v1.png";
    private const string RunSheet = Folder + "AdventurerRun-v2.png";
    private const float Ppu = 80f;
    // Measured pelvis, foot and hand anchors in the generated source (top-left origin).
    private static readonly float[] Centers = {144,392,635.5f,895,1137.5f,1392, 131,385,639,894,1150,1405,
        127,382,631,881,1150,1405, 126,375,628,879,1153,1406};
    private static readonly float[] Feet = {248,248,248,248,248,248, 506,506,506,506,506,506,
        758,758,758,758,758,758, 969,993,995,994,995,995};
    private static readonly Vector2[] Hands = {new Vector2(171.5f,178.6f),new Vector2(426.8f,177.6f),new Vector2(673.2f,176.6f),
        new Vector2(931.9f,176.6f),new Vector2(1180.2f,176.7f),new Vector2(1435.5f,177.2f),
        new Vector2(202,386),new Vector2(450,383),new Vector2(696,382),new Vector2(952,396),new Vector2(1205,389),new Vector2(1457,385),
        new Vector2(82,558),new Vector2(365,542),new Vector2(695,664),new Vector2(977,648),new Vector2(1104,649),new Vector2(1464,664),
        new Vector2(184,877),new Vector2(430,894),new Vector2(626,986),new Vector2(941,883),new Vector2(1153,960),new Vector2(1456,794)};

    [MenuItem("Tools/Player/Apply Modular Adventurer")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/persistent.unity");
        if (!scene.isLoaded) throw new InvalidOperationException("Open the persistent scene first.");
        var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerController>(true)).Single();
        ImportActions();
        ImportSword();
        var frames = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length != 24) throw new InvalidOperationException("Expected 24 body sprites.");
        var sword = AssetDatabase.LoadAllAssetsAtPath(Concept).OfType<Sprite>().Single();
        var source = player.GetComponent<SpriteRenderer>();
        var existing = player.GetComponent<ModularPlayerVisual>();
        if (existing != null) existing.enabled = false;
        var body = GetRenderer(player.transform, "Modular Body");
        body.sprite = frames[0];
        body.sharedMaterial = source.sharedMaterial;
        body.sortingLayerID = source.sortingLayerID;
        body.sortingOrder = source.sortingOrder;
        var socket = player.transform.Find("WeaponSocket");
        if (socket == null) { socket = new GameObject("WeaponSocket").transform; socket.SetParent(player.transform, false); }
        socket.localPosition = new Vector3((Hands[0].x - Centers[0]) / Ppu, (Feet[0] - Hands[0].y) / Ppu, 0);
        socket.localRotation = Quaternion.Euler(0,0,-12);
        var weapon = GetRenderer(socket, "Equipped Weapon");
        weapon.sprite = sword;
        weapon.sharedMaterial = source.sharedMaterial;
        weapon.sortingLayerID = source.sortingLayerID;
        weapon.sortingOrder = source.sortingOrder + 1;

        var tip = weapon.transform.Find("Blade Trail");
        if (tip == null) { tip = new GameObject("Blade Trail").transform; tip.SetParent(weapon.transform, false); }
        tip.localPosition = new Vector3(0,1.1f,0);
        var trail = tip.GetComponent<TrailRenderer>();
        if (trail == null) trail = tip.gameObject.AddComponent<TrailRenderer>();
        const string materialPath = Folder + "WeaponTrail.mat";
        var trailMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (trailMaterial == null) { trailMaterial = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(trailMaterial, materialPath); }
        trail.sharedMaterial = trailMaterial;
        trail.time = .13f;
        trail.startWidth = .16f;
        trail.endWidth = 0;
        trail.minVertexDistance = .04f;
        trail.endColor = new Color(.3f,1f,.65f,0);
        trail.emitting = false;
        trail.autodestruct = false;
        trail.sortingLayerID = source.sortingLayerID;
        trail.sortingOrder = source.sortingOrder + 2;

        var visual = existing != null ? existing : player.gameObject.AddComponent<ModularPlayerVisual>();
        var so = new SerializedObject(visual);
        var array = so.FindProperty("bodyFrames"); array.arraySize = frames.Length;
        var offsets = so.FindProperty("handOffsets"); offsets.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            offsets.GetArrayElementAtIndex(i).vector2Value = new Vector2((Hands[i].x-Centers[i])/Ppu,(Feet[i]-Hands[i].y)/Ppu);
        }
        so.FindProperty("bodyVisual").objectReferenceValue = body;
        so.FindProperty("weaponVisual").objectReferenceValue = weapon;
        so.FindProperty("weaponSocket").objectReferenceValue = socket;
        so.ApplyModifiedPropertiesWithoutUndo();
        ConfigureRun(visual);
        visual.enabled = true;
        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Modular adventurer applied: 24 body frames + separate replaceable sword. Gameplay components unchanged.");
    }

    [MenuItem("Tools/Player/Fix Run and Idle Weapon")]
    public static void FixLocomotion()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/persistent.unity");
        if (!scene.isLoaded) throw new InvalidOperationException("Open persistent first.");
        var visual = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ModularPlayerVisual>(true)).Single();
        ImportActions();
        var so = new SerializedObject(visual);
        for (int i=0;i<6;i++) so.FindProperty("handOffsets").GetArrayElementAtIndex(i).vector2Value =
            new Vector2((Hands[i].x-Centers[i])/Ppu,(Feet[i]-Hands[i].y)/Ppu);
        so.ApplyModifiedPropertiesWithoutUndo();
        ConfigureRun(visual);
        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureRun(ModularPlayerVisual visual)
    {
        var importer = Prepare(RunSheet);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RunSheet);
        if (tex.width != 1774 || tex.height != 887) throw new InvalidOperationException("Unexpected run sheet dimensions.");
        const float runPpu = 135f;
        float[] hips = {232,662,1096,1529,238,666,1097,1532};
        Vector2[] grips = {new Vector2(357,226),new Vector2(780,221),new Vector2(1210,220),new Vector2(1673,221),
            new Vector2(365,636),new Vector2(786,624),new Vector2(1229,631),new Vector2(1677,632)};
        var pixels = tex.GetPixels32();
        var data = new SpriteMetaData[8];
        for (int i=0;i<8;i++)
        {
            int x0=Mathf.RoundToInt(i%4*tex.width/4f), x1=Mathf.RoundToInt((i%4+1)*tex.width/4f);
            int y0=Mathf.RoundToInt((1-i/4)*tex.height/2f), y1=Mathf.RoundToInt((2-i/4)*tex.height/2f);
            var rect = OpaqueBounds(pixels,tex.width,new RectInt(x0,y0,x1-x0,y1-y0));
            float foot = i<4?430:860;
            data[i] = new SpriteMetaData { name="Adventurer_Run_"+i.ToString("D2"),rect=rect,
                alignment=(int)SpriteAlignment.Custom,pivot=new Vector2((hips[i]-rect.x)/rect.width,(tex.height-foot-rect.y)/rect.height) };
        }
        importer.spritePixelsPerUnit=runPpu;
#pragma warning disable 618
        importer.spritesheet=data;
#pragma warning restore 618
        importer.isReadable=false;
        importer.SaveAndReimport();
        var frames=AssetDatabase.LoadAllAssetsAtPath(RunSheet).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        if (frames.Length!=8) throw new InvalidOperationException("Expected eight run frames.");
        var so=new SerializedObject(visual);
        var sprites=so.FindProperty("runFrames"); sprites.arraySize=8;
        var hands=so.FindProperty("runHandOffsets"); hands.arraySize=8;
        for(int i=0;i<8;i++)
        {
            sprites.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];
            hands.GetArrayElementAtIndex(i).vector2Value=new Vector2((grips[i].x-hips[i])/runPpu,((i<4?430:860)-grips[i].y)/runPpu);
        }
        so.FindProperty("runStrideLength").floatValue=4.2f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static SpriteRenderer GetRenderer(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(parent,false); }
        var renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        return renderer;
    }

    private static TextureImporter Prepare(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.isReadable = true;
        importer.SaveAndReimport();
        return importer;
    }

    private static void ImportActions()
    {
        var importer = Prepare(Sheet);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Sheet);
        if (texture.width != 1536 || texture.height != 1024) throw new InvalidOperationException("Unexpected action sheet dimensions.");
        var pixels = texture.GetPixels32();
        var data = new SpriteMetaData[24];
        for (int i = 0; i < data.Length; i++)
        {
            var rect = OpaqueBounds(pixels, texture.width, new RectInt(i % 6 * 256, (3-i/6)*256,256,256));
            data[i] = new SpriteMetaData { name = "Adventurer_"+i.ToString("D2"), rect = rect, alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2((Centers[i]-rect.x)/rect.width,(texture.height-Feet[i]-rect.y)/rect.height) };
        }
        importer.spritePixelsPerUnit = Ppu;
#pragma warning disable 618
        importer.spritesheet = data;
#pragma warning restore 618
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static void ImportSword()
    {
        var importer = Prepare(Concept);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Concept);
        var rect = OpaqueBounds(texture.GetPixels32(), texture.width, new RectInt(texture.width/2,0,texture.width/2,texture.height));
        importer.spritePixelsPerUnit = 280;
#pragma warning disable 618
        importer.spritesheet = new[] { new SpriteMetaData { name = "ShortSword", rect = rect,
            alignment = (int)SpriteAlignment.Custom, pivot = new Vector2((1380-rect.x)/rect.width,(texture.height-730-rect.y)/rect.height) } };
#pragma warning restore 618
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static Rect OpaqueBounds(Color32[] pixels, int width, RectInt cell)
    {
        int minX=cell.xMax, minY=cell.yMax, maxX=cell.xMin, maxY=cell.yMin;
        for (int y=cell.yMin; y<cell.yMax; y++) for (int x=cell.xMin; x<cell.xMax; x++)
        {
            if (pixels[y*width+x].a < 32) continue;
            minX=Mathf.Min(minX,x); maxX=Mathf.Max(maxX,x); minY=Mathf.Min(minY,y); maxY=Mathf.Max(maxY,y);
        }
        if (maxX<=minX || maxY<=minY) throw new InvalidOperationException("An animation cell has no visible pixels.");
        minX=Mathf.Max(cell.xMin,minX-2); minY=Mathf.Max(cell.yMin,minY-2);
        maxX=Mathf.Min(cell.xMax-1,maxX+2); maxY=Mathf.Min(cell.yMax-1,maxY+2);
        return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
    }
}
