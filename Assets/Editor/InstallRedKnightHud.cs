using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallRedKnightHud
{
    const string Folder = "Assets/UI/RedKnight/";
    static readonly Color Gold = new Color(.68f,.45f,.19f);
    static readonly Color Ink = new Color(.035f,.033f,.035f,.97f);
    static readonly Color Ivory = new Color(.98f,.9f,.7f);
    static TMP_FontAsset font;
    static Sprite[] art;

    [MenuItem("Tools/Player/Install Red Knight HUD")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>(true);
        var nav = UnityEngine.Object.FindObjectOfType<TownHubNavigation>(true);
        var loader = UnityEngine.Object.FindObjectOfType<SceneLoader>(true);
        if (player == null || nav == null || loader == null) throw new InvalidOperationException("Open persistent scene first.");
        if (UnityEngine.Object.FindObjectOfType<RedKnightHud>(true) != null) throw new InvalidOperationException("HUD already installed; edit the existing hierarchy.");
        art = ImportArt();
        font = CreateFont(nav);
        var root = new GameObject("Red Knight HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasGroup));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, player.gameObject.scene);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=5;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var view=root.AddComponent<RedKnightHud>();view.player=player;view.loader=loader;view.navigation=nav;view.visibility=root.GetComponent<CanvasGroup>();
        var safe=Rect(root.transform,"Safe Area",Vector2.zero,Vector2.zero,Vector2.zero);
        safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;view.safeArea=safe;

        var status=Frame(safe,"Vitals",new Vector2(140,-38),new Vector2(430,100),new Vector2(0,1));
        var portrait=Image(safe,"Knight Portrait",new Vector2(22,-18),new Vector2(150,150),new Vector2(0,1),Color.white,art[0]);
        portrait.preserveAspect=true;
        var name=Frame(safe,"Nameplate",new Vector2(30,-158),new Vector2(140,30),new Vector2(0,1));
        Label(name,"赤焰骑士",Vector2.zero,new Vector2(136,28),19);
        Label(status,"生命",new Vector2(-145,22),new Vector2(52,30),20);
        Label(status,"体力",new Vector2(-145,-20),new Vector2(52,30),20);
        view.damageFill=Bar(status,"Damage Trail",new Vector2(42,22),new Color(.95f,.57f,.13f));
        view.healthFill=Bar(status,"Health",new Vector2(42,22),new Color(.76f,.065f,.055f),false);
        view.staminaFill=Bar(status,"Stamina",new Vector2(42,-20),new Color(.97f,.61f,.075f));
        view.healthText=Label(status,"100 / 100",new Vector2(56,22),new Vector2(245,30),19);
        view.staminaText=Label(status,"100 / 100",new Vector2(56,-20),new Vector2(245,30),19);

        var area=Frame(safe,"Area Plaque",new Vector2(0,-22),new Vector2(310,52),new Vector2(.5f,1));
        view.areaText=Label(area,"暮光河畔镇",Vector2.zero,new Vector2(294,44),24);
        var map=Button(safe,"Adventure Map",new Vector2(-96,-24),new Vector2(224,60),new Vector2(1,1),view.Travel);
        Image(map.transform,"Compass",new Vector2(-77,0),new Vector2(48,48),new Vector2(.5f,.5f),Color.white,art[6]);
        view.mapText=Label(map.transform,"冒险地图  M",new Vector2(27,0),new Vector2(160,46),21);view.mapButton=map;
        var pause=Button(safe,"Pause",new Vector2(-24,-24),new Vector2(58,60),new Vector2(1,1),view.TogglePause);
        view.pauseButton=pause;
        Label(pause.transform,"Ⅱ",Vector2.zero,new Vector2(50,50),30);

        var row=Rect(safe,"Action Bar",new Vector2(0,28),new Vector2(528,142),new Vector2(.5f,0));
        var layout=row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing=12;layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandHeight=false;layout.childForceExpandWidth=false;
        view.actions=new UnityEngine.UI.Button[4];view.cooldownMasks=new UnityEngine.UI.Image[2];view.cooldownTexts=new TMP_Text[2];
        string[] names={"普攻","滑铲","突刺","旋风斩"},keys={"J","K","U","I"};
        for(int i=0;i<4;i++)
        {
            var cell=Rect(row,"Action "+names[i],Vector2.zero,new Vector2(123,142),new Vector2(.5f,.5f));
            var icon=Image(cell,"Icon",new Vector2(0,16),new Vector2(122,122),new Vector2(.5f,.5f),Color.white,art[2+i]);
            var button=icon.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=icon;icon.raycastTarget=true;
            UnityEngine.Events.UnityAction callback=i==0?(UnityEngine.Events.UnityAction)view.Attack:i==1?view.Slide:i==2?view.Thrust:view.Spin;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,callback);
            SetColors(button);view.actions[i]=button;
            if(i>=2)
            {
                var mask=Image(cell,"Cooldown",new Vector2(0,16),new Vector2(96,96),new Vector2(.5f,.5f),new Color(0,0,0,.78f),White());
                mask.type=UnityEngine.UI.Image.Type.Filled;mask.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;mask.fillOrigin=2;mask.fillClockwise=true;mask.fillAmount=0;
                view.cooldownMasks[i-2]=mask;
                view.cooldownTexts[i-2]=Label(cell,"",new Vector2(0,16),new Vector2(94,80),30);
            }
            var caption=Frame(cell,"Caption",new Vector2(0,-54),new Vector2(118,32),new Vector2(.5f,.5f));
            var key=Frame(caption,"Key",new Vector2(-43,0),new Vector2(28,28),new Vector2(.5f,.5f));
            Label(key,keys[i],Vector2.zero,new Vector2(26,26),20);
            Label(caption,names[i],new Vector2(13,0),new Vector2(80,28),19);
        }
        view.utilityButtons=new UnityEngine.UI.Button[2];
        for(int i=0;i<2;i++)
        {
            var utility=Button(safe,i==0?"Character":"Bag",new Vector2(26+i*94,28),new Vector2(82,106),new Vector2(0,0),i==0?(UnityEngine.Events.UnityAction)view.ToggleCharacter:view.ToggleBag);
            view.utilityButtons[i]=utility;
            Image(utility.transform,"Icon",new Vector2(0,17),new Vector2(70,70),new Vector2(.5f,.5f),Color.white,art[7+i]);
            Label(utility.transform,i==0?"角色 C":"背包 B",new Vector2(0,-34),new Vector2(78,26),17);
        }
        var shade=Image(safe,"Modal Shade",Vector2.zero,Vector2.zero,Vector2.zero,new Color(0,0,0,.75f));
        var sr=shade.rectTransform;sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=sr.offsetMax=Vector2.zero;shade.raycastTarget=true;
        var panel=Frame(shade.transform,"Character Window",Vector2.zero,new Vector2(650,610),new Vector2(.5f,.5f));
        view.window=shade.gameObject;
        view.windowTitle=Label(panel,"角色",new Vector2(0,240),new Vector2(570,62),30);
        Image(panel,"Portrait",new Vector2(-218,66),new Vector2(176,176),new Vector2(.5f,.5f),Color.white,art[0]);
        view.windowBody=Label(panel,"",new Vector2(100,5),new Vector2(380,400),23);
        view.windowBody.alignment=TextAlignmentOptions.TopLeft;
        var close=Button(panel,"Resume",new Vector2(0,-246),new Vector2(230,54),new Vector2(.5f,.5f),view.CloseWindow);
        view.resumeButton=close;
        Label(close.transform,"继续冒险  Esc",Vector2.zero,new Vector2(220,48),22);
        shade.gameObject.SetActive(false);
        var legacy=UnityEngine.Object.FindObjectOfType<PlayerStatBar>(true);
        foreach(Transform child in legacy.transform)child.gameObject.SetActive(false);
        legacy.enabled=false;
        nav.useExternalHud=true;
        EditorUtility.SetDirty(nav);EditorUtility.SetDirty(legacy);EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
        Debug.Log("Red Knight HUD installed: persistent uGUI, real health/stamina/cooldowns, travel and character windows.");
    }

    public static Sprite[] ImportArt()
    {
        string path=Folder+"RedKnightAtlas.png";AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);float w=texture.width/3f,h=texture.height/3f;
        var data=new SpriteMetaData[10];
        for(int i=0;i<9;i++)data[i]=new SpriteMetaData{name="KnightUI_"+i,rect=new Rect(Mathf.Round(i%3*w),Mathf.Round((2-i/3)*h),Mathf.Floor(w),Mathf.Floor(h)),alignment=0,pivot=new Vector2(.5f,.5f)};
        data[9]=new SpriteMetaData{name="KnightUI_9",rect=new Rect(Mathf.Round(w*1.11f),Mathf.Round(h*2.08f),Mathf.Floor(w*.80f),Mathf.Floor(h*.81f)),alignment=0,pivot=new Vector2(.5f,.5f),border=new Vector4(w*.13f,h*.13f,w*.13f,h*.13f)};
#pragma warning disable 618
        importer.spritesheet=data.Take(9).ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();
        // Keep the reusable sliced frame as a single sprite: stable file ID on reimport.
        string framePath=Folder+"IronFrame.png";
        var original=new Texture2D(2,2);original.LoadImage(System.IO.File.ReadAllBytes(path));
        var bounds=data[9].rect;
        var cropped=new Texture2D((int)bounds.width,(int)bounds.height,TextureFormat.RGBA32,false);
        cropped.SetPixels(original.GetPixels((int)bounds.x,(int)bounds.y,(int)bounds.width,(int)bounds.height));cropped.Apply();
        System.IO.File.WriteAllBytes(framePath,cropped.EncodeToPNG());UnityEngine.Object.DestroyImmediate(original);UnityEngine.Object.DestroyImmediate(cropped);
        AssetDatabase.ImportAsset(framePath);
        var frameImporter=(TextureImporter)AssetImporter.GetAtPath(framePath);frameImporter.textureType=TextureImporterType.Sprite;
        frameImporter.spriteImportMode=SpriteImportMode.Single;frameImporter.spriteBorder=data[9].border;
        frameImporter.mipmapEnabled=false;frameImporter.textureCompression=TextureImporterCompression.Uncompressed;frameImporter.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Where(x=>x.name!="KnightUI_9").OrderBy(x=>x.name).Concat(new[]{AssetDatabase.LoadAssetAtPath<Sprite>(framePath)}).ToArray();
    }
    static TMP_FontAsset CreateFont(TownHubNavigation nav)
    {
        string path=Folder+"Fonts/KnightChinese SDF.asset";
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null)return existing;
        var source=AssetDatabase.LoadAssetAtPath<Font>(Folder+"Fonts/SimHei.ttf");
        var f=TMP_FontAsset.CreateFontAsset(source,48,6,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
        f.name="KnightChinese SDF";
        string chars="赤焰骑士生命体力冒险地图普攻滑铲突刺旋风斩角色背包暮光河畔镇返回主城暂停旅途歇锋芒未熄继续按或下方钮剑与盾红披斗武器伤害消耗冷却秒行囊尚获得可收纳物品当前装宝箱中的强化会直接应用到在面板查值之境旅馆休息恢复记录归返点0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz /·：，。ⅠⅡ（）【】[]+-_\n"+string.Join("",nav.destinationNames);
        string missing;if(!f.TryAddCharacters(chars,out missing)&&!string.IsNullOrEmpty(missing))Debug.LogWarning("HUD font missing: "+missing);
        AssetDatabase.CreateAsset(f,path);
        foreach(var t in f.atlasTextures){t.name="KnightChinese Atlas";AssetDatabase.AddObjectToAsset(t,f);}
        f.material.name="KnightChinese Material";AssetDatabase.AddObjectToAsset(f.material,f);
        EditorUtility.SetDirty(f);AssetDatabase.SaveAssets();return f;
    }
    static Sprite White()=>AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size,Vector2 anchor)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static UnityEngine.UI.Image Image(Transform parent,string name,Vector2 position,Vector2 size,Vector2 anchor,Color color,Sprite sprite=null)
    {
        var r=Rect(parent,name,position,size,anchor);var im=r.gameObject.AddComponent<UnityEngine.UI.Image>();im.color=color;im.sprite=sprite;im.raycastTarget=false;return im;
    }
    static RectTransform Frame(Transform parent,string name,Vector2 position,Vector2 size,Vector2 anchor)
    {
        if(art!=null&&art.Length>9&&size.y>=38)
        {
            var ornate=Image(parent,name,position,size,anchor,Color.white,art[9]);
            ornate.type=UnityEngine.UI.Image.Type.Sliced;ornate.pixelsPerUnitMultiplier=3;
            return ornate.rectTransform;
        }
        var border=Image(parent,name,position,size,anchor,Gold).rectTransform;
        var inner=Image(border,"Iron",Vector2.zero,size-new Vector2(4,4),new Vector2(.5f,.5f),Ink).rectTransform;
        Image(inner,"Upper Glint",new Vector2(0,size.y/2-5),new Vector2(size.x-10,1),new Vector2(.5f,.5f),new Color(.93f,.72f,.39f,.6f));
        return border;
    }
    static UnityEngine.UI.Image Bar(Transform parent,string name,Vector2 position,Color color,bool background=true)
    {
        if(background)Frame(parent,name+" Track",position,new Vector2(304,30),new Vector2(.5f,.5f));
        var fill=Image(parent,name,position,new Vector2(296,22),new Vector2(.5f,.5f),color,White());
        fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;return fill;
    }
    static TMP_Text Label(Transform parent,string value,Vector2 position,Vector2 size,int fontSize)
    {
        var r=Rect(parent,value,position,size,new Vector2(.5f,.5f));var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;t.text=value;t.fontSize=fontSize;t.color=Ivory;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.enableWordWrapping=true;
        return t;
    }
    static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 position,Vector2 size,Vector2 anchor,UnityEngine.Events.UnityAction onClick)
    {
        var rect=Frame(parent,name,position,size,anchor);var image=rect.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,onClick);SetColors(button);return button;
    }
    static void SetColors(UnityEngine.UI.Button button)
    {
        var colors=button.colors;colors.highlightedColor=new Color(1,.91f,.73f);colors.pressedColor=new Color(.72f,.5f,.3f);colors.disabledColor=new Color(.42f,.4f,.38f,.8f);button.colors=colors;
        var n=button.navigation;n.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=n;
    }
}
