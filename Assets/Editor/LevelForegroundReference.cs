using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelForegroundReference
{
    public static string Export(int number)
    {
        var source=SceneManager.GetSceneByName("SampleScene_"+number);
        var background=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite!=null).OrderByDescending(r=>r.bounds.size.x).First();
        var active=SceneManager.GetActiveScene();
        var hidden=new List<GameObject>();
        for(int i=0;i<SceneManager.sceneCount;i++)foreach(var g in SceneManager.GetSceneAt(i).GetRootGameObjects())if(g.activeSelf){hidden.Add(g);g.SetActive(false);}
        var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(temp);
        var picture=new GameObject("Reference Panorama");var sr=picture.AddComponent<SpriteRenderer>();sr.sprite=background.sprite;
        picture.transform.position=background.transform.position;picture.transform.rotation=background.transform.rotation;picture.transform.localScale=background.transform.lossyScale;
        var material=new Material(Shader.Find("Sprites/Default"));sr.sharedMaterial=material;
        var camera=new GameObject("Reference Camera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=683f/48f;camera.aspect=543f/724;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        string folder="Assets/GeneratedForegroundHD/Scene"+number+"/";Directory.CreateDirectory(folder);
        try
        {
            for(int q=0;q<4;q++)
            {
                camera.transform.position=new Vector3((q+.5f)*2048f/96f,683f/48f,-20);
                var old=RenderTexture.active;var rt=new RenderTexture(543,724,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var texture=new Texture2D(543,724,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,543,724),0,0);texture.Apply();File.WriteAllBytes(folder+"ReferenceQuarter"+(q+1)+".png",texture.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);
            }
        }
        finally{EditorSceneManager.CloseScene(temp,true);UnityEngine.Object.DestroyImmediate(material);foreach(var g in hidden)if(g!=null)g.SetActive(true);SceneManager.SetActiveScene(active);}
        AssetDatabase.Refresh();return folder;
    }
}
