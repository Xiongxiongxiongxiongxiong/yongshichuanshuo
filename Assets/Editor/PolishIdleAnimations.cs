using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Align existing drawings; keep source pixels, gameplay and non-idle clips intact.</summary>
public static class PolishIdleAnimations
{
    const string Output="Assets/player/IdlePolish/";
    const string Evidence="Docs/AI/IdlePolish/";
    [MenuItem("Tools/Player/Polish Player and Angel Idle")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Output);Directory.CreateDirectory(Evidence);
        Backup("Assets/Animations/Player/GreenIde.anim");
        Backup("Assets/GeneratedTown/NPC/AngelIdle.anim");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("persistent");
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Additive);
        Backup(scene.path);
        var swap=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TestWeaponSwap>(true)).Single();
        var texture=Read("Assets/player/png/red/char_red_3.png");
        var source=AssetDatabase.LoadAllAssetsAtPath("Assets/player/png/red/char_red_3.png").OfType<Sprite>().ToArray();
        var idle=new Sprite[6];
        Vector2 firstFeet=Feet(texture,source.Single(s=>s.name=="RedKnight_00"));
        for(int i=0;i<6;i++)
        {
            var original=source.Single(s=>s.name=="RedKnight_"+i.ToString("D2"));
            var shift=Feet(texture,original)-firstFeet;
            var pivot=original.pivot+shift;
            var created=Sprite.Create(original.texture,original.rect,new Vector2(pivot.x/original.rect.width,pivot.y/original.rect.height),original.pixelsPerUnit,0,SpriteMeshType.FullRect);
            created.name=original.name;
            var path=Output+original.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(saved==null){AssetDatabase.CreateAsset(created,path);saved=created;}
            else{EditorUtility.CopySerialized(created,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(created);}
            idle[i]=saved;
            var binding=swap.animatedFrames.Single(f=>f.source.name==original.name);
            // Recompute from the currently bound pivot, making repeated applications idempotent.
            Vector2 delta=pivot-binding.source.pivot;
            binding.bodyPivot+=new Vector2(delta.x/binding.bodyRect.width,delta.y/binding.bodyRect.height);
            binding.hand-=delta/original.pixelsPerUnit;
            binding.source=saved;
            for(int j=0;j<swap.originalFrames.Length;j++)if(swap.originalFrames[j].name==original.name)swap.originalFrames[j]=saved;
        }
        UnityEngine.Object.DestroyImmediate(texture);
        var renderer=swap.GetComponent<SpriteRenderer>();
        if(renderer.sprite!=null&&renderer.sprite.name.StartsWith("RedKnight_")&&int.TryParse(renderer.sprite.name.Substring(10),out int n)&&n<6)
        {renderer.sprite=idle[n];EditorUtility.SetDirty(renderer);}
        SetLoop("Assets/Animations/Player/GreenIde.anim",idle,
            new[]{0,1,2,3,4,5,4,3,2,1,0},new[]{0f,.20f,.36f,.52f,.68f,.88f,1.08f,1.24f,1.40f,1.56f,1.76f});
        EditorUtility.SetDirty(swap);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        PolishAngel();AssetDatabase.SaveAssets();
    }
    static void PolishAngel()
    {
        const string folder="Assets/GeneratedTown/NPC/";
        var pixels=Read(folder+"AngelIdle.png");var frames=new Sprite[6];
        // Face reference does not move when the asymmetric wings expand.
        float firstFace=FaceX(pixels,0);
        var first=AssetDatabase.LoadAssetAtPath<Sprite>(folder+"AngelIdle0.asset");
        float faceOffset=firstFace-first.rect.x-first.pivot.x;
        for(int i=0;i<6;i++)
        {
            string path=folder+"AngelIdle"+i+".asset";Backup(path);
            var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);var r=saved.rect;
            var pivot=new Vector2((FaceX(pixels,i)-faceOffset-r.x)/r.width,0);
            var created=Sprite.Create(saved.texture,r,pivot,saved.pixelsPerUnit,0,SpriteMeshType.FullRect);
            created.name=saved.name;EditorUtility.CopySerialized(created,saved);EditorUtility.SetDirty(saved);
            UnityEngine.Object.DestroyImmediate(created);frames[i]=saved;
        }
        UnityEngine.Object.DestroyImmediate(pixels);
        // Original drawings already depict a complete opening/closing wing cycle.
        // Reversing that cycle produced a second abrupt wing reversal.
        SetLoop(folder+"AngelIdle.anim",frames,new[]{0,1,2,3,4,5,0},new[]{0f,.24f,.46f,.72f,.98f,1.20f,1.44f});
    }
    static void SetLoop(string path,Sprite[] frames,int[] sequence,float[] times)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);clip.frameRate=50;
        var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip,binding,sequence.Select((f,i)=>new ObjectReferenceKeyframe{time=times[i],value=frames[f]}).ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.startTime=0;settings.stopTime=times.Last();
        AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
    }
    static Texture2D Read(string path){var t=new Texture2D(2,2);t.LoadImage(File.ReadAllBytes(path));return t;}
    static Vector2 Feet(Texture2D t,Sprite s)
    {
        int minX=999,maxX=0,minY=999;
        for(int y=0;y<32;y++)for(int x=25;x<105;x++)
        {
            if(t.GetPixel((int)s.rect.x+x,(int)s.rect.y+y).a<.75f)continue;
            minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);
        }
        return new Vector2((minX+maxX)*.5f,minY);
    }
    static float FaceX(Texture2D t,int index)
    {
        int ox=index%3*512,oy=(1-index/3)*512;double sum=0,count=0;
        for(int y=390;y<445;y++)for(int x=180;x<350;x++)
        {
            var p=t.GetPixel(ox+x,oy+y);
            if(p.a<.8f||p.r<.7f||p.g<.4f||p.g>.85f||p.r-p.g<.08f||p.g-p.b<.04f||p.g-p.b>.20f)continue;
            sum+=ox+x;count++;
        }
        if(count==0)throw new InvalidOperationException("Angel face missing: "+index);
        return (float)(sum/count);
    }
    static void Backup(string path){string dest=Evidence+Path.GetFileName(path)+".before";if(!File.Exists(dest))File.Copy(path,dest);}
}
