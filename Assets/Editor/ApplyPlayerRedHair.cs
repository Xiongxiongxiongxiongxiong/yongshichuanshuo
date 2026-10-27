using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyPlayerRedHair
{
    [MenuItem("Tools/Player/Apply Red Hair")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>(true);
        var atlases = new[] {"Assets/player/png/green/char_green_4.png","Assets/player/png/green/char_green_5.png","Assets/player/Skills/GreenSwordSkills.png"}.Select(Inspect).ToArray();
        const string path="Assets/player/PlayerRedHair.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material==null) { material=new Material(Shader.Find("Game/PlayerHairColor"));AssetDatabase.CreateAsset(material,path); }
        var visual=player.GetComponent<SpriteRenderer>();visual.sharedMaterial=material;
        var component=player.GetComponent<PlayerHairColor>();
        if(component==null)component=player.gameObject.AddComponent<PlayerHairColor>();
        var so=new SerializedObject(component);var profiles=so.FindProperty("atlases");profiles.arraySize=atlases.Length;
        for(int i=0;i<atlases.Length;i++)
        {
            var entry=profiles.GetArrayElementAtIndex(i);entry.FindPropertyRelative("texture").objectReferenceValue=atlases[i].texture;
            var areas=entry.FindPropertyRelative("areas");var weights=entry.FindPropertyRelative("weights");areas.arraySize=weights.arraySize=atlases[i].areas.Length;
            for(int j=0;j<areas.arraySize;j++){areas.GetArrayElementAtIndex(j).rectValue=atlases[i].areas[j];weights.GetArrayElementAtIndex(j).floatValue=atlases[i].weights[j];}
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        component.enabled=false;component.enabled=true;
        EditorUtility.SetDirty(visual);EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
    }

    // Inspect the original pixels once in the Editor; keep all source textures untouched.
    private static PlayerHairColor.Atlas Inspect(string path)
    {
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));
        int w=texture.width,h=texture.height;var pixels=texture.GetPixels32();var visited=new bool[pixels.Length];
        var areas=new List<Rect>();var weights=new List<float>();var pending=new Queue<int>();
        for(int index=0;index<pixels.Length;index++)
        {
            if(visited[index]||!Silver(pixels[index]))continue;
            int minX=w,minY=h,maxX=0,maxY=0,count=0;pending.Enqueue(index);visited[index]=true;
            while(pending.Count>0)
            {
                int current=pending.Dequeue(),x=current%w,y=current/w;count++;
                minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=w||ny>=h)continue;int n=ny*w+nx;
                    if(visited[n]||!Silver(pixels[n]))continue;visited[n]=true;pending.Enqueue(n);
                }
            }
            int width=maxX-minX+1,height=maxY-minY+1;
            if(count<70||width<12||height<12||width>height*2.4f||height>width*2)continue;
            areas.Add(new Rect(minX-1,minY-1,width+2,height+2));weights.Add(count);
        }
        UnityEngine.Object.DestroyImmediate(texture);
        return new PlayerHairColor.Atlas {texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path),areas=areas.ToArray(),weights=weights.ToArray()};
    }
    private static bool Silver(Color32 c)
    {
        int hi=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),lo=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
        return c.a>100&&hi>75&&hi-lo<42;
    }
}
