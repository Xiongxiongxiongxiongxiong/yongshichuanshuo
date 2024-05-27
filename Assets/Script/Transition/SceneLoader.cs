using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;




public class SceneLoader : MonoBehaviour
{


    
    
    public Transform playerTrans;
    public Vector3 firstPosition;
    public Vector3 menuPosition;
    [Header("事件的监听")]


    public SceneLoadEventSO LoadEventSO;

    public VoidEventSO NewGameEvent;

    [Header("广播")] 
    public VoidEventSO afterSceneLoadedEvent;

    public FadeEventSO FadeEvent;
    public SceneLoadEventSO sceneUnloadedEvent;
    [Header("场景")]
    public GameSceneSO firstLoadScene;

    public GameSceneSO menuScene;
    
    private GameSceneSO currentLoadedScene;
    
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;

    public float fadeDuration;
    private void Awake()
    {
       // Addressables.LoadSceneAsync(firstLoadScene.sceneReference, LoadSceneMode.Additive);
       // currentLoadedScene = firstLoadScene;
       // currentLoadedScene.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
       
    }

    private void Start()
    {
      //  b.onClick.AddListener(ggg);
        
        //NewGame();
        LoadEventSO.RaiseLoadRequesEvent(menuScene,menuPosition,true);

    }



    
    
    
    public void OnEnable()
    {
        LoadEventSO.LoadRequesEvent += OnLoadRequesEvent;
        NewGameEvent.onEventRaised += NewGame;
    }

    private void OnDisable()
    {
        LoadEventSO.LoadRequesEvent -= OnLoadRequesEvent;
        NewGameEvent.onEventRaised -= NewGame;
    }

    public void NewGame()
    {
      //  sceneToLoad = firstLoadScene;
       // OnLoadRequesEvent(sceneToLoad,firstPosition,true);
     //  LoadEventSO.RaiseLoadRequesEvent(sceneToLoad,firstPosition,true);
     LoadEventSO.RaiseLoadRequesEvent(firstLoadScene,firstPosition,true);
    }
    
    
    
    
    
/// <summary>
/// 场景加载事件请求
/// </summary>
/// <param name="locationToLoad"></param>
/// <param name="posToGo"></param>
/// <param name="fadeScreen"></param>
    private void OnLoadRequesEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        if (isLoading)
        {
            return;
        }
        isLoading = true;
        sceneToLoad = locationToLoad;
        positionToGo = posToGo;
        this.fadeScreen = fadeScreen;
        if (currentLoadedScene != null)
        {
            StartCoroutine(UnLoadPreviousScene());
        }
        else
        {
            LoadNewScene();
        }
    }

    private IEnumerator UnLoadPreviousScene()
    {
        if (fadeScreen)
        {
            //渐影渐出变黑
            FadeEvent.FadeIn(fadeDuration);
            
            
        }
        yield return new WaitForSeconds(fadeDuration);
        
        //广播事件调整血条显示
        sceneUnloadedEvent.RaiseLoadRequesEvent(sceneToLoad,positionToGo,true);

        yield return  currentLoadedScene.sceneReference.UnLoadScene();

     //关闭人物
     playerTrans.gameObject.SetActive(false);
     //加载新场景
        LoadNewScene();
    }

    private void LoadNewScene()
    {
      var loadingOption =  sceneToLoad.sceneReference.LoadSceneAsync(LoadSceneMode.Additive, true);
      loadingOption.Completed += OnLoadCompleteed;
      
    }
/// <summary>
/// 场景加载之后
/// </summary>
/// <param name="obj"></param>
/// <exception cref="NotImplementedException"></exception>
    private void OnLoadCompleteed(AsyncOperationHandle<SceneInstance> obj)
{
    currentLoadedScene = sceneToLoad;
    playerTrans.position = positionToGo;
    playerTrans.gameObject.SetActive(true);
    if (fadeScreen)
    {
        //渐影渐出变透明
        FadeEvent.FadeOut(fadeDuration);
    }

    isLoading = false;

    if (currentLoadedScene.sceneType != SceneType.Menu)
    {
        //场景加载完成后事件
        afterSceneLoadedEvent.RaiseEvent();
    }

}
}
