using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallTownNPCs
{
    const string Folder = "Assets/GeneratedTown/NPC/";
    [MenuItem("Tools/Levels/Install Town NPCs")]
    public static void Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByName("SampleScene_25");
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_25.unity", OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save existing town edits first.");
        if (scene.GetRootGameObjects().Any(g => g.name == "Town NPCs")) throw new InvalidOperationException("NPCs already installed; preserve scene edits.");
        Directory.CreateDirectory("Docs/AI/TownNPC");
        File.Copy(scene.path, "Docs/AI/TownNPC/SampleScene_25_BeforeNPC.unity.backup", false);
        var root = new GameObject("Town NPCs");
        SceneManager.MoveGameObjectToScene(root, scene);
        Create(root.transform, "Blacksmith", 445, 449, 2.6f);
        Create(root.transform, "Angel", 1170, 474, 2.9f);
        Create(root.transform, "WarehouseKeeper", 1260, 611, 2.5f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        SceneManager.SetActiveScene(scene);
    }

    static void Create(Transform parent, string id, float x, float y, float height)
    {
        string path = Folder + id + "Idle.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var pixels = texture.GetPixels32();
        int w = texture.width / 3, h = texture.height / 2;
        var rects = new Rect[6];
        for (int i = 0; i < 6; i++)
        {
            int ox = i % 3 * w, oy = (1 - i / 3) * h;
            int left = w, right = 0, bottom = h, top = 0;
            for (int py = 0; py < h; py++) for (int px = 0; px < w; px++)
            {
                if (pixels[(oy + py) * texture.width + ox + px].a < 32) continue;
                left = Math.Min(left, px); right = Math.Max(right, px);
                bottom = Math.Min(bottom, py); top = Math.Max(top, py);
            }
            if (right <= left || top <= bottom) throw new InvalidOperationException("Empty frame " + id + i);
            rects[i] = new Rect(ox + left, oy + bottom, right - left + 1, top - bottom + 1);
        }
        float ppu = rects.Average(r => r.height) / height;
        var sprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            sprites[i] = Sprite.Create(texture, rects[i], new Vector2(.5f, 0), ppu, 0, SpriteMeshType.FullRect);
            sprites[i].name = id + "Idle" + i;
            AssetDatabase.CreateAsset(sprites[i], Folder + id + "Idle" + i + ".asset");
        }
        var clip = new AnimationClip { name = id + " Idle", frameRate = 5 };
        int[] sequence = {0,1,2,3,4,5,4,3,2,1,0};
        var keys = sequence.Select((f, i) => new ObjectReferenceKeyframe { time = i * .2f, value = sprites[f] }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, Folder + id + "Idle.anim");
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + id + ".controller");
        var state = controller.layers[0].stateMachine.AddState("Idle");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;
        var go = new GameObject(id);
        go.transform.SetParent(parent, false);
        go.transform.position = PanoramaLevelAuthoring.Point(x, y);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprites[0];
        renderer.sortingLayerName = "player";
        renderer.sortingOrder = -1;
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        PrefabUtility.SaveAsPrefabAsset(go, Folder + id + ".prefab");
    }
}
