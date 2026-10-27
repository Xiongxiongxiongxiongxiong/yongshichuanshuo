using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ValidateLevelForeground
{
    public static string ValidateAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Validate saved scenes in edit mode.");
        var report=new StringBuilder();
        for(int n=9;n<=18;n++)
        {
            var scene=SceneManager.GetSceneByName("SampleScene_"+n);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_"+n+".unity",OpenSceneMode.Additive);
            string folder="Assets/GeneratedForegroundHD/Scene"+n+"/";
            var backup=EditorSceneManager.OpenScene(folder+"BeforeForegroundHD.unity",OpenSceneMode.Additive);
            bool collision=UpgradeLevelForeground.CollisionFingerprint(scene)==UpgradeLevelForeground.CollisionFingerprint(backup);
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            int missing=transforms.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var filters=transforms.Select(t=>t.GetComponent<MeshFilter>()).Where(m=>m!=null).ToArray();
            bool assets=filters.Length==4;
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
                var texture=renderer.sharedMaterial.mainTexture;
                var ends=new List<Vector2>();mesh.GetUVs(2,ends);
                assets&=texture!=null&&Mathf.Abs(texture.width-1086)<=1&&Mathf.Abs(texture.height-1448)<=1&&ends.Count==mesh.vertexCount&&renderer.sharedMaterial.shader.isSupported;
            }
            var originalBg=backup.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite!=null).OrderByDescending(r=>r.bounds.size.x).First();
            var bg=transforms.Select(t=>t.GetComponent<SpriteRenderer>()).Where(r=>r!=null&&r.sprite!=null).OrderByDescending(r=>r.bounds.size.x).First();
            bool background=bg.sprite==originalBg.sprite&&bg.color==new Color(.55f,.60f,.69f,1);
            bool gameplay=transforms.Count(t=>t.GetComponent<Chest>()!=null)==backup.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Chest>(true)).Count();
            EditorSceneManager.CloseScene(backup,true);
            if(!collision||!assets||!background||!gameplay||missing!=0)throw new InvalidOperationException("Scene "+n+" failed: collision="+collision+" assets="+assets+" background="+background+" gameplay="+gameplay+" missing="+missing);
            report.AppendLine("SampleScene_"+n+": PASS — four approximately 1086x1448 foreground textures, supported materials, endpoint UVs, unchanged collision, source background retained/darkened, chest count preserved, no missing scripts.");
        }
        Directory.CreateDirectory("Docs/AI");File.WriteAllText("Docs/AI/Foreground9To18Validation.txt",report.ToString());AssetDatabase.Refresh();return report.ToString();
    }
}

