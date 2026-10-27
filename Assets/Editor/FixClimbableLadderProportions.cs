using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixClimbableLadderProportions
{
    const string SegmentPath="Assets/GeneratedMineV2/LadderRepeat.asset";
    public static void Configure(SpriteRenderer renderer,float width,float height)
    {
        var segment=AssetDatabase.LoadAssetAtPath<Sprite>(SegmentPath);
        if(segment==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedMineV2/Ladder.asset");
            // One complete middle rung, with matching rail edges above and below.
            segment=Sprite.Create(source.texture,new Rect(142,531,222,92),new Vector2(.5f,0),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            segment.name="LadderRepeat";AssetDatabase.CreateAsset(segment,SegmentPath);
        }
        float scale=width/segment.bounds.size.x;
        var parent=renderer.transform.parent==null?Vector3.one:renderer.transform.parent.lossyScale;
        renderer.sprite=segment;renderer.drawMode=SpriteDrawMode.Tiled;
        renderer.tileMode=SpriteTileMode.Continuous;
        renderer.size=new Vector2(segment.bounds.size.x,height/scale);
        // Assign after changing sprite/draw mode; the editor can preserve the old
        // apparent size by adjusting scale during that transition.
        renderer.transform.localScale=new Vector3(scale/parent.x,scale/parent.y,1/parent.z);
    }
    public static string Fix(int number)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit play mode first.");
        var scene=SceneManager.GetSceneByName("SampleScene_"+number);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_"+number+".unity",OpenSceneMode.Additive);
        if(scene.isDirty)throw new InvalidOperationException("Unsaved scene edits: "+scene.name);
        var ladders=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite!=null&&r.sprite.name=="Ladder").ToArray();
        if(ladders.Length==0)return scene.name+": no stretched ladders.";
        string folder="Assets/LadderProportionFix/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        string backup=folder+scene.name+"_Before.unity";
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(backup)==null)EditorSceneManager.SaveScene(scene,backup,true);
        string collision=UpgradeScene8Foreground.CollisionFingerprint(scene);
        var map=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)).First(t=>t.name=="Forest Decorations");
        string climb=EditorJsonUtility.ToJson(map);
        foreach(var ladder in ladders)
        {
            var bounds=ladder.bounds;
            Configure(ladder,bounds.size.x,bounds.size.y);
            if((ladder.bounds.min-bounds.min).sqrMagnitude>.00001f||(ladder.bounds.max-bounds.max).sqrMagnitude>.00001f)
                throw new InvalidOperationException("Ladder endpoints moved: "+ladder.name);
            if(Mathf.Abs(ladder.transform.lossyScale.x-ladder.transform.lossyScale.y)>.00001f)
                throw new InvalidOperationException("Ladder is still stretched.");
        }
        if(collision!=UpgradeScene8Foreground.CollisionFingerprint(scene)||climb!=EditorJsonUtility.ToJson(map))throw new InvalidOperationException("Climb/collision data changed.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return scene.name+": "+ladders.Length+" tiled ladders saved; uniform scale, bounds and climb map unchanged.";
    }
}
