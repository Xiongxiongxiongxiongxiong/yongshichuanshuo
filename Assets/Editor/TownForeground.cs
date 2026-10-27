using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Town uses the original uniform lighting, independently of combat foregrounds.
public static class TownForeground
{
    public static string Build(int number)
    {
        if(number!=25)throw new ArgumentOutOfRangeException("number");
        if(Application.isPlaying)throw new InvalidOperationException("Exit play mode first.");
        var scene=SceneManager.GetSceneByName("SampleScene_25");
        if(!scene.isLoaded||scene.isDirty)throw new InvalidOperationException("Open saved town first.");
        string collision=UpgradeScene8Foreground.CollisionFingerprint(scene);
        var background=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true))
            .First(r=>r.sprite!=null&&AssetDatabase.GetAssetPath(r.sprite)=="Assets/GeneratedTown/TwilightRiverTown.png");
        var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(background.sprite));
        importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        background.color=Color.white;
        background.transform.localScale=new Vector3((2048f/24)/background.sprite.bounds.size.x,(683f/24)/background.sprite.bounds.size.y,1);
        foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
            if(t.name=="04 High Density Walkable Foreground")t.gameObject.SetActive(false);
        if(collision!=UpgradeScene8Foreground.CollisionFingerprint(scene))throw new InvalidOperationException("Collision changed.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Town saved: original full-resolution panorama, white tint, walkable brightness overlay disabled; collision unchanged.";
    }
}
