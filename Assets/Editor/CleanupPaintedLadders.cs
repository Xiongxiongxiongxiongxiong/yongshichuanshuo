using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CleanupPaintedLadders
{
    const string Folder="Assets/LadderArtworkCleanup/";
    [Serializable] public class Region
    {
        public float x,y,width,height;
        public float xMin=>x;public float xMax=>x+width;public float yMin=>y;public float yMax=>y+height;
    }
    [Serializable] public class Entry{public int number;public string source;public Region[] regions;}
    [Serializable] public class Manifest{public Entry[] entries;}
    static string Fingerprint(Scene scene)
    {
        var roots=scene.GetRootGameObjects();
        var maps=roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true));
        var ladders=roots.SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.sprite!=null&&r.sprite.name=="LadderRepeat");
        return UpgradeScene8Foreground.CollisionFingerprint(scene)+"\nMAPS="+string.Join("\n",maps.Select(m=>m.name+EditorJsonUtility.ToJson(m)).OrderBy(t=>t).ToArray())+
            "\nLADDERS="+string.Join("\n",ladders.Select(r=>r.name+"|"+r.transform.position.ToString("F5")+"|"+r.transform.lossyScale.ToString("F5")+"|"+r.size.ToString("F5")+"|"+r.drawMode+"|"+AssetDatabase.GetAssetPath(r.sprite)).OrderBy(t=>t).ToArray());
    }
    public static string ApplyAll()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"Manifest.json"));
        var shader=Shader.Find("Yongshi/Walkable Foreground Ladder Repair");
        if(shader==null||!shader.isSupported)throw new InvalidOperationException("Repair shader unavailable.");
        foreach(var e in manifest.entries)
        {
            string clean=Folder+"Scene"+e.number+"/Clean.png";
            if(!File.Exists(clean)||!File.Exists(e.source))throw new InvalidOperationException("Missing image for "+e.number);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(clean);
            if(texture==null)throw new InvalidOperationException("Import clean images before applying.");
            if(e.number<=17&&(e.regions==null||e.regions.Length==0||e.regions.Length>8))throw new InvalidOperationException("Missing ladder regions.");
        }
        var report=new StringBuilder();
        foreach(var e in manifest.entries)
        {
            string folder=Folder+"Scene"+e.number+"/",clean=folder+"Clean.png",backup=folder+"Before.png";
            if(!File.Exists(backup))File.Copy(e.source,backup);
            File.Copy(clean,e.source,true);AssetDatabase.ImportAsset(e.source,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(clean);importer.textureType=TextureImporterType.Default;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var cleaned=AssetDatabase.LoadAssetAtPath<Texture2D>(clean);
            if(cleaned.width!=2172||cleaned.height!=724)throw new InvalidOperationException("Unexpected clean image dimensions for "+e.number);
            foreach(int n in e.number==8?new[]{8,18}:new[]{e.number})
            {
                var scene=SceneManager.GetSceneByName("SampleScene_"+n);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_"+n+".unity",OpenSceneMode.Additive);
                string fingerprint=Fingerprint(scene);
                var renderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name.StartsWith("Yongshi/Walkable Foreground")).OrderBy(r=>r.name).ToArray();
                for(int q=0;q<renderers.Length;q++)
                {
                    var material=renderers[q].sharedMaterial;string path=AssetDatabase.GetAssetPath(material),copy=folder+"Scene"+n+"_Material"+q+"_Before.mat";
                    if(AssetDatabase.LoadAssetAtPath<Material>(copy)==null)AssetDatabase.CopyAsset(path,copy);
                    if(n>=19)
                    {
                        string texturePath=AssetDatabase.GetAssetPath(material.mainTexture);
                        if(!File.Exists(folder+"ForegroundBefore.png"))File.Copy(texturePath,folder+"ForegroundBefore.png");
                        File.Copy(clean,texturePath,true);AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceUpdate);
                    }
                    else
                    {
                        float brightness=material.GetFloat("_Brightness"),rim=material.GetFloat("_RimStrength"),end=material.GetFloat("_EndFeather");
                        material.shader=shader;material.SetFloat("_Brightness",brightness);material.SetFloat("_RimStrength",rim);material.SetFloat("_EndFeather",end);
                        material.SetTexture("_CleanTex",cleaned);material.SetVector("_CleanUV",new Vector4(.25f,q*.25f,0,0));
                        for(int i=0;i<8;i++)
                        {
                            Vector4 rect=Vector4.zero;
                            if(i<e.regions.Length){var r=e.regions[i];rect=new Vector4(r.xMin/2048,1-r.yMax/683,r.xMax/2048,1-r.yMin/683);}
                            material.SetVector("_Repair"+i,rect);
                        }
                        EditorUtility.SetDirty(material);
                    }
                }
                if(fingerprint!=Fingerprint(scene))throw new InvalidOperationException("Climbing/collision changed in "+n);
                int count=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Count(r=>r.sprite!=null&&r.sprite.name=="LadderRepeat");
                var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();BuildMineExpansion.Render(camera,folder+"Scene"+n+"_After.png");
                report.AppendLine("SampleScene_"+n+": PASS; "+count+" independent ladders unchanged; collision/climbing Tilemaps unchanged; "+renderers.Length+" foreground materials repaired; panorama ladder cleanup installed.");
                if(opened)EditorSceneManager.CloseScene(scene,true);
            }
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();File.WriteAllText(Folder+"Validation.txt",report.ToString());return report.ToString();
    }
}
