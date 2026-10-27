using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UpgradeLevelForeground
{
    static string Folder;
    const float Width=2048f/24f,Height=683f/24f;
    public static string CollisionFingerprint(Scene scene)
    {
        return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true))
            .Where(c=>c is PolygonCollider2D||c is BoxCollider2D)
            .Select(c=>c.name+"|"+c.transform.position.ToString("F4")+"|"+(c is PolygonCollider2D?
                string.Join(";",((PolygonCollider2D)c).points.Select(p=>p.ToString("F4")).ToArray()):((BoxCollider2D)c).size.ToString("F4")))
            .OrderBy(s=>s).ToArray());
    }
    public static string Build(int number)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=SceneManager.GetSceneByName("SampleScene_"+number);
        if(!scene.isLoaded)throw new InvalidOperationException("Open target scene first.");
        if(scene.isDirty)throw new InvalidOperationException("Target has unsaved edits.");
        if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="04 High Density Walkable Foreground"))
            throw new InvalidOperationException("Foreground already upgraded.");
        Folder="Assets/GeneratedForegroundHD/Scene"+number+"/";        var before=CollisionFingerprint(scene);
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Folder+"BeforeForegroundHD.unity")==null)
            EditorSceneManager.SaveScene(scene,Folder+"BeforeForegroundHD.unity",true);
        SceneManager.SetActiveScene(scene);
        var background=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true))
            .OrderByDescending(r=>r.bounds.size.x).First(r=>r.sprite!=null);
        var root=new GameObject("04 High Density Walkable Foreground");root.transform.SetParent(background.transform.parent,false);
        var paths=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PolygonCollider2D>(true)).Where(p=>!p.isTrigger).ToArray();
        for(int k=0;k<4;k++)
        {
            string image=Folder+"WalkableQuarter"+(k+1)+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(image);importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(image);
            var material=new Material(Shader.Find("Yongshi/Walkable Foreground HD"));material.mainTexture=texture;
            AssetDatabase.CreateAsset(material,Folder+"WalkableQuarter"+(k+1)+".mat");
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var depth=new List<Vector2>();var ends=new List<Vector2>();var triangles=new List<int>();
            float left=k*Width/4,right=(k+1)*Width/4;
            foreach(var path in paths)
            {
                var points=path.points;int count=points.Length/2;
                var pathLeft=(Vector2)path.transform.TransformPoint(points[0]);var pathRight=(Vector2)path.transform.TransformPoint(points[count-1]);
                bool leftJoined=Joined(path,pathLeft,paths),rightJoined=Joined(path,pathRight,paths);
                float extent=(path.name.Contains("Suspension")||path.name.Contains("Rope")||path.name.Contains("Timber Walk")||path.name.Contains("Balcony")||path.name.Contains("Deck"))?.65f:path.name.Contains("Bridge")?1.2f:path.name.Contains("Lower")?2.1f:
                    path.name.Contains("Alcove")?1.6f:path.name.Contains("Island")?3.4f:3.8f;
                for(int i=0;i<count-1;i++)
                {
                    Vector2 a=path.transform.TransformPoint(points[i]),b=path.transform.TransformPoint(points[i+1]);
                    if(b.x<=a.x+.0001f)continue;
                    float lo=Mathf.Max(a.x,left),hi=Mathf.Min(b.x,right);if(hi<=lo)continue;
                    float ya=Mathf.Lerp(a.y,b.y,(lo-a.x)/(b.x-a.x)),yb=Mathf.Lerp(a.y,b.y,(hi-a.x)/(b.x-a.x));
                    int start=vertices.Count;float[] rows={-.65f,-.2f,0,.12f,extent*.6f,extent};
                    foreach(float d in rows)
                    {
                        vertices.Add(new Vector3(lo,ya+.025f-d,0));vertices.Add(new Vector3(hi,yb+.025f-d,0));
                        uv.Add(new Vector2((lo-left)/(right-left),(ya+.025f-d)/Height));uv.Add(new Vector2((hi-left)/(right-left),(yb+.025f-d)/Height));
                        depth.Add(new Vector2(d,d/extent));depth.Add(new Vector2(d,d/extent));
                        ends.Add(new Vector2(leftJoined?100:lo-pathLeft.x,rightJoined?100:pathRight.x-lo));
                        ends.Add(new Vector2(leftJoined?100:hi-pathLeft.x,rightJoined?100:pathRight.x-hi));
                    }
                    for(int row=0;row<rows.Length-1;row++){int v=start+row*2;triangles.AddRange(new[]{v,v+2,v+1,v+1,v+2,v+3});}
                }
            }
            var mesh=new Mesh();mesh.name="Scene"+number+" Walkable Quarter "+(k+1);mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,depth);mesh.SetUVs(2,ends);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Folder+"WalkableQuarter"+(k+1)+".asset");
            var go=new GameObject("Walkable Foreground HD Quarter "+(k+1),typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingLayerID=background.sortingLayerID;r.sortingOrder=-6;
        }
        // Source texture, its dimensions and the animated background material remain unchanged.
        background.color=new Color(.55f,.60f,.69f,1);
        if(before!=CollisionFingerprint(scene))throw new InvalidOperationException("Collision changed unexpectedly.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved SampleScene_"+number+": four 1086x1448 walkable foreground textures, original background texture/resolution retained, collision fingerprint identical.";
    }
    static bool Joined(PolygonCollider2D path,Vector2 endpoint,PolygonCollider2D[] paths)
    {
        foreach(var other in paths)
        {
            if(other==path)continue;var p=other.points;int count=p.Length/2;
            foreach(var end in new[]{(Vector2)other.transform.TransformPoint(p[0]),(Vector2)other.transform.TransformPoint(p[count-1])})
                if(Mathf.Abs(endpoint.x-end.x)<.06f&&Mathf.Abs(endpoint.y-end.y)<.35f)return true;
        }
        return false;
    }
}

