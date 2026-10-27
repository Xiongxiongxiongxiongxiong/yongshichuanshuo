using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildFrostWeaponFrames
{
    const string Folder="Assets/player/EquipmentTest/Animated/";
    static readonly Vector2[] AttackHands={new Vector2(.20f,.50f),new Vector2(.16f,.50f),new Vector2(.22f,.54f),new Vector2(.31f,.22f),new Vector2(.58f,.38f),new Vector2(.61f,.38f),new Vector2(.23f,.22f),new Vector2(.30f,.38f)};
    [MenuItem("Tools/Player/Build Frame Locked Frost Sword")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var swap=UnityEngine.Object.FindObjectOfType<TestWeaponSwap>(true);
        if(swap==null)throw new InvalidOperationException("Open persistent first.");
        var main=Prepare(Folder+"FrostMain.png");var extra=Prepare(Folder+"FrostExtra.png");
        var attack=Prepare(Folder+"FrostAttack.png");var attackPixels=Read(Folder+"FrostAttack.png");
        var mainPixels=Read(Folder+"FrostMain.png");var extraPixels=Read(Folder+"FrostExtra.png");
        var bodyMain=Read(AssetDatabase.GetAssetPath(swap.cleanMain));var bodyExtra=Read(AssetDatabase.GetAssetPath(swap.cleanExtra));
        var result=new TestWeaponSwap.WeaponFrame[swap.originalFrames.Length];
        for(int i=0;i<result.Length;i++)
        {
            var source=swap.originalFrames[i];bool isExtra=source.name.StartsWith("RedKnightSkill_");
            int index=int.Parse(source.name.Substring(isExtra?15:10));
            var texture=isExtra?extra:main;var pixels=isExtra?extraPixels:mainPixels;
            bool hidden=isExtra?index>=30:index>=42&&index<=53||index==36||index==37;
            Rect rect=SheetRect(source,texture);
            bool attackPose=(!isExtra&&index>=6&&index<=13)||(isExtra&&index>=22&&index<=29);
            if(attackPose)
            {
                int pose=isExtra?new[]{2,1,3,4,4,5,6,7}[index-22]:index-6;
                texture=attack;pixels=attackPixels;float w=attack.width/4f,h=attack.height/2f;
                rect=new Rect(Mathf.Round(pose%4*w),Mathf.Round((1-pose/4)*h),Mathf.Floor(w),Mathf.Floor(h));
                if(pose==3){rect.xMin-=w*.06f;}
                if(pose==4){rect.xMax+=w*.10f;}
                if(pose==6){rect.xMin-=w*.10f;}
            }
            Vector2 pivot;int count;
            if(!attackPose)rect=IsolatePose(pixels,rect);
            FindGrip(pixels,rect,out pivot,out count);
            // A few occluded/recovery poses share the adjacent authored sword drawing.
            if(count<4&&!hidden)
            {
                int alternate=index>=65?62+(index-65):index>=54&&index<=61?55+(index-54)%4:0;
                var donor=swap.originalFrames.First(f=>f.name==(isExtra?"RedKnightSkill_":"RedKnight_")+alternate.ToString("D2"));
                rect=IsolatePose(pixels,SheetRect(donor,texture));FindGrip(pixels,rect,out pivot,out count);
            }
            if(count<4&&!hidden)throw new InvalidOperationException("Missing weapon artwork: "+source.name);
            float scale=(float)texture.height/source.texture.height;
            float ppu=attackPose?source.pixelsPerUnit*(texture.width/4f)/(isExtra?167.625f:133.625f)*1.6f:source.pixelsPerUnit*scale*(isExtra?1.4f:1);
            if(!isExtra&&index>=54&&index<=61)
            {
                texture=Prepare(Folder+"FrostUpgrade.png");
                var upgrade=Read(Folder+"FrostUpgrade.png");
                int pose=new[]{0,0,1,2,3,1,2,3}[index-54];
                float cell=texture.width/4f;
                rect=new Rect(Mathf.Round(pose*cell),0,Mathf.Floor(cell),texture.height);
                FindGrip(upgrade,rect,out pivot,out count);
                ppu=source.pixelsPerUnit*cell/133.625f;
                UnityEngine.Object.DestroyImmediate(upgrade);
            }
            var sprite=Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);
            sprite.name="Frost_"+source.name;
            string path=Folder+sprite.name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(saved==null){AssetDatabase.CreateAsset(sprite,path);saved=sprite;}
            else{EditorUtility.CopySerialized(sprite,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(sprite);}
            var hand=Hand(source,index,isExtra,isExtra?bodyExtra:bodyMain);
            var bodyTexture=isExtra?bodyExtra:bodyMain;float bsx=(float)bodyTexture.width/source.texture.width,bsy=(float)bodyTexture.height/source.texture.height;
            Rect full=SheetRect(source,bodyTexture);Rect bodyRect=IsolatePose(bodyTexture,full);
            Vector2 bodyPivot=new Vector2((source.rect.x*bsx+source.pivot.x*bsx-bodyRect.x)/bodyRect.width,
                (source.rect.y*bsy+source.pivot.y*bsy-bodyRect.y)/bodyRect.height);
            bool resting=(!isExtra&&index<6)||(isExtra&&(index<4||index==8||index==9));
            result[i]=new TestWeaponSwap.WeaponFrame{source=source,sprite=saved,hand=hand,hidden=hidden,sortingOffset=attackPose||resting?1:-1,
                bodyRect=bodyRect,bodyPivot=bodyPivot,cropBody=true,
                flipVertical=(!isExtra&&index<6)||(isExtra&&(index<4||index==8||index==9)),
                flipHorizontal=(!isExtra&&index<6)||(isExtra&&(index<4||index==8||index==9))};
        }
        swap.animatedFrames=result;EditorUtility.SetDirty(swap);
        EditorSceneManager.MarkSceneDirty(swap.gameObject.scene);EditorSceneManager.SaveScene(swap.gameObject.scene);AssetDatabase.SaveAssets();
        UnityEngine.Object.DestroyImmediate(mainPixels);UnityEngine.Object.DestroyImmediate(extraPixels);
        UnityEngine.Object.DestroyImmediate(bodyMain);UnityEngine.Object.DestroyImmediate(bodyExtra);
        UnityEngine.Object.DestroyImmediate(attackPixels);
        Debug.Log("Frame-locked frost sword: "+result.Length+" synchronized weapon bindings; no separate clock or rotation tween.");
    }
    [MenuItem("Tools/Player/Repair Frost Upgrade Transparency")]
    public static void RepairUpgradeTransparency()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var swap=UnityEngine.Object.FindObjectOfType<TestWeaponSwap>(true);
        var texture=Prepare(Folder+"FrostUpgrade.png");var pixels=Read(Folder+"FrostUpgrade.png");
        foreach(var frame in swap.animatedFrames)
        {
            if(!frame.source.name.StartsWith("RedKnight_"))continue;
            int index=int.Parse(frame.source.name.Substring(10));if(index<54||index>61)continue;
            int pose=new[]{0,0,1,2,3,1,2,3}[index-54];float cell=texture.width/4f;
            var rect=new Rect(Mathf.Round(pose*cell),0,Mathf.Floor(cell),texture.height);
            Vector2 pivot;int count;FindGrip(pixels,rect,out pivot,out count);
            if(count<4)throw new InvalidOperationException("Upgrade pose has no grip: "+pose);
            var sprite=Sprite.Create(texture,rect,pivot,frame.source.pixelsPerUnit*cell/133.625f,0,SpriteMeshType.FullRect);
            sprite.name=frame.sprite.name;EditorUtility.CopySerialized(sprite,frame.sprite);
            EditorUtility.SetDirty(frame.sprite);UnityEngine.Object.DestroyImmediate(sprite);
        }
        UnityEngine.Object.DestroyImmediate(pixels);AssetDatabase.SaveAssets();
    }
    static Texture2D Prepare(string path)
    {
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D Read(string path){var t=new Texture2D(2,2);t.LoadImage(System.IO.File.ReadAllBytes(path));return t;}
    static Rect SheetRect(Sprite source,Texture2D texture)
    {
        var r=source.rect;float sx=(float)texture.width/source.texture.width,sy=(float)texture.height/source.texture.height;
        return new Rect(Mathf.Round(r.x*sx),Mathf.Round(r.y*sy),Mathf.Min(Mathf.Floor(r.width*sx),texture.width-Mathf.Round(r.x*sx)),Mathf.Min(Mathf.Floor(r.height*sy),texture.height-Mathf.Round(r.y*sy)));
    }
    static void FindGrip(Texture2D tex,Rect rect,out Vector2 pivot,out int count)
    {
        Vector2 total=Vector2.zero;float weight=0;count=0;
        var p=tex.GetPixels((int)rect.x,(int)rect.y,(int)rect.width,(int)rect.height);
        for(int y=0;y<(int)rect.height;y++)for(int x=0;x<(int)rect.width;x++)
        {
            var c=p[y*(int)rect.width+x];
            // Only the leather grip / gold furniture is warm; blue steel and effects are excluded.
            if(c.a<.5f||c.r<.16f||c.r<c.g*1.12f||c.g<c.b*1.15f)continue;
            float w=Mathf.Max(.05f,c.r-c.b);total+=new Vector2(x+.5f,y+.5f)*w;weight+=w;count++;
        }
        pivot=weight>0?new Vector2(total.x/weight/rect.width,total.y/weight/rect.height):new Vector2(.5f,.5f);
    }
    static Rect IsolatePose(Texture2D tex,Rect rect)
    {
        int w=(int)rect.width,h=(int)rect.height;var p=tex.GetPixels((int)rect.x,(int)rect.y,w,h);
        var visited=new bool[p.Length];var queue=new int[p.Length];int best=0;Rect answer=rect;
        for(int n=0;n<p.Length;n++)
        {
            if(visited[n]||p[n].a<.2f)continue;
            int read=0,write=0,warm=0,minX=w,minY=h,maxX=0,maxY=0;queue[write++]=n;visited[n]=true;
            while(read<write)
            {
                int at=queue[read++],x=at%w,y=at/w;var c=p[at];
                if(c.r>.16f&&c.r>c.g*1.12f&&c.g>c.b*1.15f)warm++;
                minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=w||yy>=h)continue;int j=yy*w+xx;if(visited[j]||p[j].a<.2f)continue;visited[j]=true;queue[write++]=j;}
            }
            int score=warm*1000+write;if(warm<4||score<=best)continue;best=score;
            minX=Mathf.Max(0,minX-2);minY=Mathf.Max(0,minY-2);maxX=Mathf.Min(w-1,maxX+2);maxY=Mathf.Min(h-1,maxY+2);
            answer=new Rect(rect.x+minX,rect.y+minY,maxX-minX+1,maxY-minY+1);
        }
        return answer;
    }
    static Vector2 Hand(Sprite source,int index,bool extra,Texture2D body)
    {
        Vector2 grip=new Vector2(.32f,.29f);
        if(!extra&&index<6)grip=new Vector2(.31f,.30f);
        else if(!extra&&index>=6&&index<=13)grip=AttackHands[index-6];
        else if(!extra&&index>=14&&index<=21)
        {Vector2[] run={new Vector2(.33f,.30f),new Vector2(.37f,.29f),new Vector2(.65f,.46f),new Vector2(.65f,.44f),new Vector2(.69f,.41f),new Vector2(.63f,.41f),new Vector2(.39f,.26f),new Vector2(.38f,.28f)};grip=run[index-14];}
        else if(!extra&&index>=54&&index<=61)grip=new Vector2(.24f,index==54||index==61?.30f:.55f);
        else if(!extra&&index>=62&&index<=64)grip=new Vector2(.32f,.46f);
        else if(extra&&index>=22&&index<30)grip=new Vector2(index==22?.24f:index==23?.32f:.48f,index<=23?.66f:.30f);
        else if(extra&&index>=18&&index<22)grip=new Vector2(.65f,.55f);
        else if(extra&&index>=10&&index<18)grip=new Vector2(.36f,.22f);
        else if(extra&&index>=4&&index<8)grip=new Vector2(.40f,.33f);
        Rect r=source.rect;float sx=(float)body.width/source.texture.width,sy=(float)body.height/source.texture.height;
        Vector2 point=new Vector2((r.x+r.width*grip.x)*sx,(r.y+r.height*grip.y)*sy);
        // Refine within the authored hand neighborhood; avoid anchoring to blue sword FX.
        Vector2 sum=Vector2.zero;float weight=0;
        for(int y=Mathf.Max(0,(int)point.y-5);y<Mathf.Min(body.height,(int)point.y+6);y++)
        for(int x=Mathf.Max(0,(int)point.x-5);x<Mathf.Min(body.width,(int)point.x+6);x++)
        {var c=body.GetPixel(x,y);if(c.a<.5f||c.r<c.g*1.15f||c.g<c.b*1.12f)continue;float w=1/(1+Vector2.SqrMagnitude(new Vector2(x,y)-point));sum+=new Vector2(x,y)*w;weight+=w;}
        if(weight>0)point=sum/weight;
        return new Vector2((point.x/sx-r.x-source.pivot.x)/source.pixelsPerUnit,(point.y/sy-r.y-source.pivot.y)/source.pixelsPerUnit);
    }
}
