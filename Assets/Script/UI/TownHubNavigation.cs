using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class TownHubNavigation : MonoBehaviour
{
    public SceneLoader loader;
    public GameSceneSO town;
    public GameSceneSO[] destinations;
    public string[] destinationNames;
    public Vector3[] destinationSpawns;
    public Vector3 innPosition;
    public Vector3 guildPosition;
    public bool MapOpen => map != null && map.activeSelf;
    public int LastMapCloseFrame { get; private set; } = -1;
    public bool useExternalHud;
    public InventoryWindows inventoryWindows;
    public void TravelFromHud() => OnTravelButton();
    GameObject canvasObject,map,ownedEvents;
    Text hint;
    Button travelButton;
    Font font;
    PlayerController player;
    float previousTime=1,notificationUntil;
    string notification;

    void Start()
    {
        player=loader.playerTrans.GetComponent<PlayerController>();
        font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},32);
        canvasObject=new GameObject("Town Travel Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=120;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        travelButton=Button(canvasObject.transform,"冒险地图",new Vector2(1130,665),new Vector2(220,44),OnTravelButton);
        travelButton.gameObject.SetActive(!useExternalHud);
        var strip=Panel(canvasObject.transform,"Town Hint",new Vector2(640,47),new Vector2(800,48),new Color(.025f,.04f,.06f,.88f));
        hint=Label(strip.transform,"",Vector2.zero,new Vector2(780,44),20);
        if(useExternalHud)
        {
            var rect=strip.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-18,12);rect.sizeDelta=new Vector2(430,32);
            hint.rectTransform.sizeDelta=new Vector2(416,28);hint.fontSize=13;
            strip.GetComponent<Image>().raycastTarget=false;
        }
        map=Panel(canvasObject.transform,"Adventure Map",new Vector2(640,370),new Vector2(1020,610),new Color(.025f,.035f,.055f,.98f));
        Label(map.transform,"冒险者公会 · 选择目的地",new Vector2(0,260),new Vector2(940,50),30);
        Label(map.transform,"主城已记录为归返点，可随时使用右上角按钮返回。",new Vector2(0,213),new Vector2(940,36),18);
        for(int i=0;i<destinations.Length;i++)
        {
            int index=i;int col=i%3,row=i/3;
            Button(map.transform,destinationNames[i],new Vector2(-320+col*320,148-row*62),new Vector2(300,48),()=>Travel(index));
        }
        Button(map.transform,"关闭",new Vector2(0,-254),new Vector2(200,42),CloseMap);
        map.SetActive(false);canvasObject.SetActive(false);
    }
    void Update()
    {
        bool visible=loader!=null&&player!=null&&player.gameObject.activeInHierarchy&&loader.CurrentScene!=null&&loader.CurrentScene.sceneType!=SceneType.Menu&&!loader.IsLoading;
        if(canvasObject==null)return;
        canvasObject.SetActive(visible);
        if(!visible)return;
        EnsureEvents();
        if (inventoryWindows != null && (inventoryWindows.IsOpen || inventoryWindows.LastCloseFrame == Time.frameCount))
        {
            hint.transform.parent.gameObject.SetActive(false);
            return;
        }
        bool inTown=loader.CurrentScene==town;
        travelButton.GetComponentInChildren<Text>().text=inTown?"冒险地图":"返回主城";
        travelButton.interactable=!player.isDead;
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&MapOpen){CloseMap();return;}
        if(MapOpen||player.isDead)return;
        bool inn=inTown&&Vector2.Distance(player.transform.position,innPosition)<3;
        bool guild=inTown&&Vector2.Distance(player.transform.position,guildPosition)<3;
        hint.transform.parent.gameObject.SetActive(inTown||Time.unscaledTime<notificationUntil);
        hint.text=Time.unscaledTime<notificationUntil?notification:inn?"旅馆  [E] 休息、恢复生命并记录主城归返点":guild?"冒险者公会  [E] 打开冒险地图":"暮光河畔镇  ·  主街 / 河岸 / 城墙均可探索";
        if(inTown&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)
        {
            if(inn)Rest();else if(guild)OpenMap();
        }
    }
    public void OpenMap()
    {
        if(loader.IsLoading||loader.CurrentScene!=town||player==null||player.isDead||MapOpen||Time.timeScale<=0)return;
        previousTime=Time.timeScale;Time.timeScale=0;player.inputcontroller.GamePlayer.Disable();
        map.SetActive(true);EnsureEvents();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(map.GetComponentInChildren<Button>().gameObject);
    }
    public void CloseMap()
    {
        if(!MapOpen)return;
        LastMapCloseFrame=Time.frameCount;
        map.SetActive(false);Time.timeScale=previousTime;
        if(player!=null&&!player.isDead)player.inputcontroller.GamePlayer.Enable();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
    }
    public bool Travel(int index)
    {
        if(index<0||index>=destinations.Length||loader.IsLoading||player==null||player.isDead)return false;
        CloseMap();return loader.TravelTo(destinations[index],destinationSpawns[index]);
    }
    public void Rest()
    {
        if(loader.CurrentScene!=town||player==null||player.isDead)return;
        var health=player.GetComponent<Character>();health.CurrentHp=health.maxHp;health.OnHealthChange?.Invoke(health);
        loader.SaveCheckpoint("TwilightRiverTown",town,loader.mainTownPosition);
        notification="已休息，生命恢复。主城归返点已记录。";notificationUntil=Time.unscaledTime+3;
    }
    void OnTravelButton(){if(loader.CurrentScene==town)OpenMap();else if(!loader.IsLoading&&player!=null&&!player.isDead){CloseMap();loader.ReturnToTown();}}
    void EnsureEvents()
    {
        if(EventSystem.current!=null)return;
        ownedEvents=new GameObject("Town UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        ownedEvents.transform.SetParent(transform,false);ownedEvents.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
    GameObject Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=size;
        rt.anchoredPosition=parent==canvasObject.transform?position-new Vector2(640,360):position;
        go.GetComponent<Image>().color=color;return go;
    }
    Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=position;
        var label=go.GetComponent<Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.96f,.89f,.69f);label.raycastTarget=false;return label;
    }
    Button Button(Transform parent,string text,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var go=Panel(parent,text,position,size,new Color(.14f,.19f,.24f,1));
        var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();button.onClick.AddListener(action);
        Label(go.transform,text,Vector2.zero,size-Vector2.one*8,18);return button;
    }
    void OnDestroy()
    {
        CloseMap();if(font!=null)Destroy(font);
        if(canvasObject!=null)Destroy(canvasObject);if(ownedEvents!=null)Destroy(ownedEvents);
    }
}
