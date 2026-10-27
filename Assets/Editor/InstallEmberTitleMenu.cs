using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallEmberTitleMenu
{
    const string Folder = "Assets/UI/EmberTitle/";
    static TMP_FontAsset font;
    static readonly Color Ivory = new Color(.98f,.91f,.76f);

    [MenuItem("Tools/UI/Install Ember Walker Title Menu")]
    public static string Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByName("persistent");
        if (!scene.isLoaded) throw new InvalidOperationException("Open persistent first.");
        if (UnityEngine.Object.FindObjectOfType<EmberTitleMenu>(true) != null) throw new InvalidOperationException("Title already installed; edit the existing view.");
        var loader = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SceneLoader>(true)).Single();
        var audio = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AudioManager>(true)).Single();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/RedKnight/Fonts/KnightChinese SDF.asset");
        font.TryAddCharacters("开始冒险继续旅程设置退出游戏声音开关总音量音乐音效全屏模式返回立即生效并自动保存按确认选择静");
        AssetDatabase.ImportAsset(Folder+"TitleBackground.png",ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"TitleBackground.png");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
        importer.SaveAndReimport();
        var root=new GameObject("Ember Walker Title Menu",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(root,scene);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=6;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var view=root.AddComponent<EmberTitleMenu>();view.loader=loader;view.audioManager=audio;view.visibility=root.GetComponent<CanvasGroup>();
        var black=Image(root.transform,"Letterbox",Vector2.zero,new Vector2(1920,1080),new Color(.012f,.02f,.025f,1));
        black.rectTransform.anchorMin=Vector2.zero;black.rectTransform.anchorMax=Vector2.one;black.rectTransform.sizeDelta=Vector2.zero;
        var content=Rect(root.transform,"Title Composition",Vector2.zero,new Vector2(1920,1080));view.composition=content;
        var background=Image(content,"Approved Background",Vector2.zero,new Vector2(1920,1080),Color.white);
        background.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"TitleBackground.png");
        float x=-520;
        view.newGameButton=Button(content,"Start Adventure","开始冒险",new Vector2(x,-6),new Vector2(470,78),view.NewGame);
        view.continueButton=Button(content,"Continue Journey","继续旅程",new Vector2(x,-102),new Vector2(470,78),view.ContinueGame);
        view.settingsButton=Button(content,"Settings","设  置",new Vector2(x,-198),new Vector2(470,78),view.OpenSettings);
        Button(content,"Quit","退出游戏",new Vector2(x,-294),new Vector2(470,78),view.QuitGame);
        Label(content,"按 Enter 确认  ·  ↑↓ 选择",new Vector2(-705,-455),new Vector2(450,42),23);
        var mute=Button(content,"Sound","声音：开",new Vector2(830,-455),new Vector2(150,45),view.ToggleMute);
        view.muteLabel=mute.GetComponentInChildren<TMP_Text>();view.muteLabel.fontSize=20;

        var shade=Image(content,"Settings Modal",Vector2.zero,new Vector2(1920,1080),new Color(0,0,0,.73f));shade.raycastTarget=true;
        view.settingsPanel=shade.gameObject;
        var panel=Rect(shade.transform,"Settings Panel",Vector2.zero,new Vector2(760,650));
        panel.gameObject.AddComponent<EmberMenuFrame>().raycastTarget=true;
        Image(panel,"Panel Backdrop",Vector2.zero,new Vector2(710,600),new Color(.018f,.026f,.029f,.98f));
        Label(panel,"设  置",new Vector2(0,244),new Vector2(640,70),44);
        Label(panel,"立即生效，并自动保存",new Vector2(0,190),new Vector2(640,42),22).color=new Color(.72f,.67f,.55f);
        view.masterSlider=Slider(panel,"Master Volume","总音量",95,view,out view.masterValue);
        view.musicSlider=Slider(panel,"Music Volume","音乐",5,view,out view.musicValue);
        view.effectsSlider=Slider(panel,"Effects Volume","音效",-85,view,out view.effectsValue);
        var fullscreen=Button(panel,"Fullscreen","全屏模式：关",new Vector2(0,-175),new Vector2(440,60),view.ToggleFullscreen);
        view.fullscreenLabel=fullscreen.GetComponentInChildren<TMP_Text>();view.fullscreenLabel.fontSize=26;
        view.closeSettingsButton=Button(panel,"Close Settings","返  回",new Vector2(0,-260),new Vector2(280,60),view.CloseSettings);
        view.settingsPanel.SetActive(false);
        view.FitComposition();

        // Keep the existing addressable Menu scene and travel references; retire only its legacy UI.
        var oldMenu=SceneManager.GetSceneByName("Menu"); bool opened=!oldMenu.isLoaded;
        if(opened)oldMenu=EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity",OpenSceneMode.Additive);
        foreach(var menu in oldMenu.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Memu>(true))) menu.enabled=false;
        foreach(var oldCanvas in oldMenu.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true))) oldCanvas.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(oldMenu);EditorSceneManager.SaveScene(oldMenu);
        if(opened)EditorSceneManager.CloseScene(oldMenu,true);
        SceneManager.SetActiveScene(scene);
        EditorUtility.SetDirty(view);EditorUtility.SetDirty(font);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Ember title installed in persistent; four live menu buttons and saved audio/fullscreen settings. Legacy Menu UI disabled.";
    }

    static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
    static UnityEngine.UI.Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var r=Rect(parent,name,position,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;
    }
    static TMP_Text Label(Transform parent,string text,Vector2 position,Vector2 size,float fontSize)
    {
        var r=Rect(parent,text,position,size);var label=r.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font;label.text=text;label.fontSize=fontSize;label.color=Ivory;label.alignment=TextAlignmentOptions.Center;
        label.raycastTarget=false;label.enableWordWrapping=false;return label;
    }
    static UnityEngine.UI.Button Button(Transform parent,string name,string text,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction callback)
    {
        var rect=Rect(parent,name,position,size);var frame=rect.gameObject.AddComponent<EmberMenuFrame>();frame.raycastTarget=true;
        var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=frame;
        var colors=button.colors;colors.normalColor=colors.highlightedColor=colors.selectedColor=Color.white;
        colors.pressedColor=new Color(.85f,.7f,.45f);colors.disabledColor=new Color(.45f,.45f,.45f,.7f);button.colors=colors;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,callback);
        Label(rect,text,Vector2.zero,new Vector2(size.x-90,size.y-4),34);
        return button;
    }
    static UnityEngine.UI.Slider Slider(Transform parent,string name,string caption,float y,EmberTitleMenu view,out TMP_Text value)
    {
        var row=Rect(parent,name,new Vector2(0,y),new Vector2(610,65));
        Label(row,caption,new Vector2(-230,0),new Vector2(130,50),28);
        value=Label(row,"100%",new Vector2(258,0),new Vector2(100,50),24);
        var track=Image(row,"Slider",new Vector2(15,0),new Vector2(300,38),new Color(.04f,.05f,.05f));track.raycastTarget=true;
        var slider=track.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=0;slider.maxValue=1;slider.value=1;
        var bar=Image(track.transform,"Track",Vector2.zero,new Vector2(300,7),new Color(.29f,.23f,.13f));
        var fillArea=Rect(track.transform,"Fill Area",Vector2.zero,new Vector2(280,9));
        var fill=Image(fillArea,"Fill",Vector2.zero,Vector2.zero,new Color(.87f,.59f,.22f));
        fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;slider.fillRect=fill.rectTransform;
        var handleArea=Rect(track.transform,"Handle Area",Vector2.zero,new Vector2(280,38));
        var handle=Image(handleArea,"Handle",Vector2.zero,new Vector2(18,28),new Color(1,.83f,.48f));
        slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.onValueChanged,view.ChangeAudio);
        return slider;
    }
}
