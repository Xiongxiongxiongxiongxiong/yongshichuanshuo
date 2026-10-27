using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UpgradeScene8Foreground
{
    const string Folder="Assets/GeneratedMineV2/ForegroundHD/";
    const float Width=2048f/24f,Height=683f/24f;
    public static string CollisionFingerprint(Scene scene)
    {
        return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true))
            .Where(c=>c is PolygonCollider2D||c is BoxCollider2D)
            .Select(c=>c.name+"|"+c.transform.position.ToString("F4")+"|"+(c is PolygonCollider2D?
                string.Join(";",((PolygonCollider2D)c).points.Select(p=>p.ToString("F4")).ToArray()):((BoxCollider2D)c).size.ToString("F4")))
            .OrderBy(s=>s).ToArray());
    }
    [MenuItem("Tools/Levels/Upgrade Scene 8 Walkable Foreground")]
    public static void BuildMenu(){Debug.Log(Build());}
    public static string Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=SceneManager.GetSceneByName("SampleScene_8");if(!scene.isLoaded)throw new InvalidOperationException("Open SampleScene_8 first.");
        if(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="04 High Density Walkable Foreground"))
            throw new InvalidOperationException("Foreground already upgraded; preserve authored changes.");
        var before=CollisionFingerprint(scene);
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scene8Preview/SampleScene_8_BeforeForegroundHD.unity")==null)
            EditorSceneManager.SaveScene(scene,"Assets/Scene8Preview/SampleScene_8_BeforeForegroundHD.unity",true);
        SceneManager.SetActiveScene(scene);
        var background=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true))
            .First(r=>r.name=="03 Newly Generated Mine Panorama");
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
                float extent=path.name.Contains("Bridge")?.65f:path.name.Contains("Lower Cavern")?2.1f:
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
            var mesh=new Mesh();mesh.name="Scene8 Walkable Quarter "+(k+1);mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,depth);mesh.SetUVs(2,ends);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,Folder+"WalkableQuarter"+(k+1)+".asset");
            var go=new GameObject("Walkable Foreground HD Quarter "+(k+1),typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingLayerID=background.sortingLayerID;r.sortingOrder=-6;
        }
        // Source texture, its dimensions and the animated background material remain unchanged.
        background.color=new Color(.55f,.60f,.69f,1);
        if(before!=CollisionFingerprint(scene))throw new InvalidOperationException("Collision changed unexpectedly.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Saved SampleScene_8: four 1086x1448 walkable foreground textures, original background texture/resolution retained, collision fingerprint identical.";
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
    // Add endpoint distances to existing meshes without moving vertices or colliders.
    public static string ApplyEndpointFeathering()
    {
        var scene=SceneManager.GetSceneByName("SampleScene_8");if(!scene.isLoaded)throw new InvalidOperationException("Open SampleScene_8 first.");
        var paths=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PolygonCollider2D>(true)).Where(p=>!p.isTrigger).ToArray();
        var fingerprint=CollisionFingerprint(scene);int updated=0;
        for(int k=1;k<=4;k++)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"WalkableQuarter"+k+".asset");var vertices=mesh.vertices;var depths=mesh.uv2;
            var ends=new List<Vector2>();
            for(int i=0;i<vertices.Length;i++)
            {
                float x=vertices[i].x,y=vertices[i].y+depths[i].x-.025f,best=float.MaxValue;PolygonCollider2D selected=null;
                foreach(var path in paths)
                {
                    var points=path.points;int count=points.Length/2;
                    for(int j=0;j<count-1;j++)
                    {
                        Vector2 a=path.transform.TransformPoint(points[j]),b=path.transform.TransformPoint(points[j+1]);
                        if(b.x-a.x<.0001f||x<a.x-.001f||x>b.x+.001f)continue;
                        float delta=Mathf.Abs(y-Mathf.Lerp(a.y,b.y,Mathf.Clamp01((x-a.x)/(b.x-a.x))));
                        if(delta<best){best=delta;selected=path;}
                    }
                }
                if(selected==null||best>.01f)throw new InvalidOperationException("Cannot match foreground vertex to terrain.");
                var p=selected.points;Vector2 left=selected.transform.TransformPoint(p[0]),right=selected.transform.TransformPoint(p[p.Length/2-1]);
                ends.Add(new Vector2(Joined(selected,left,paths)?100:Mathf.Max(0,x-left.x),Joined(selected,right,paths)?100:Mathf.Max(0,right.x-x)));
            }
            mesh.SetUVs(2,ends);EditorUtility.SetDirty(mesh);updated++;
        }
        if(fingerprint!=CollisionFingerprint(scene))throw new InvalidOperationException("Collision changed unexpectedly.");
        AssetDatabase.SaveAssets();return "Updated exposed-end feathering on "+updated+" meshes; vertices/collision unchanged.";
    }
}
