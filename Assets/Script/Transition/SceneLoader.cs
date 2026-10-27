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
    [Header("主城")]
    public GameSceneSO mainTownScene;
    public Vector3 mainTownPosition;
    public bool IsLoading => isLoading;
    public GameSceneSO CurrentScene => currentLoadedScene;
    public bool CanContinue => checkpoint != null && checkpointScene != null;

    public bool TravelTo(GameSceneSO destination, Vector3 spawn, bool restorePlayer = false)
    {
        if (isLoading || destination == null) return false;
        Time.timeScale = 1;
        resetPlayerOnLoad = restorePlayer;
        LoadEventSO.RaiseLoadRequesEvent(destination, spawn, true);
        return true;
    }

    public void ReturnToTown()
    {
        TravelTo(mainTownScene, mainTownPosition, true);
    }
    [SerializeField] private GameSceneSO bossLevelScene;
    [SerializeField] private GameSceneSO secondBossLevelScene;
    [SerializeField] private GameSceneSO thirdBossLevelScene;
    [SerializeField] private GameSceneSO caveBossLevelScene;
    [SerializeField] private GameSceneSO mothBossLevelScene;
    [SerializeField] private GameSceneSO[] additionalBossScenes = new GameSceneSO[0];

    public GameSceneSO menuScene;
    
    private GameSceneSO currentLoadedScene;
    
    private GameSceneSO sceneToLoad;
    private Vector3 positionToGo;
    private bool fadeScreen;
    private bool isLoading;
    private Coroutine deathReturn;
    private bool resetPlayerOnLoad;
    private const string CheckpointKey = "Yongshi.Checkpoint";
    [Serializable]
    private class CheckpointData
    {
        public string id;
        public string sceneGuid;
        public Vector3 position;
    }
    private CheckpointData checkpoint;
    private GameSceneSO checkpointScene;

    public bool HasCheckpoint(string id)
    {
        return checkpoint != null && checkpoint.id == id;
    }

    public void SaveCheckpoint(string id, GameSceneSO scene, Vector3 position)
    {
        checkpoint = new CheckpointData { id = id, sceneGuid = scene.sceneReference.AssetGUID, position = position };
        checkpointScene = scene;
        PlayerPrefs.SetString(CheckpointKey, JsonUtility.ToJson(checkpoint));
        PlayerPrefs.Save();
    }

    public void ContinueGame()
    {
        if (checkpoint == null || checkpointScene == null) { NewGame(); return; }
        resetPlayerOnLoad = true;
        LoadEventSO.RaiseLoadRequesEvent(checkpointScene, checkpoint.position, true);
    }

    public bool EnterBossLevel(GameSceneSO scene, Vector3 spawn)
    {
        if (isLoading || scene == null) return false;
        SaveCheckpoint(Array.IndexOf(additionalBossScenes, scene) >= 0 ? scene.name + "Entrance" : scene == mothBossLevelScene ? "MoonveilMothEntrance" : scene == caveBossLevelScene ? "AzureSalamanderEntrance" : scene == thirdBossLevelScene ? "CrystalbackEntrance" : scene == secondBossLevelScene ? "SporeCrownEntrance" : "MossTuskKingEntrance", scene, spawn);
        resetPlayerOnLoad = true;
        LoadEventSO.RaiseLoadRequesEvent(scene, spawn, true);
        return true;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1;
        LoadEventSO.RaiseLoadRequesEvent(menuScene, menuPosition, true);
    }

    public float fadeDuration;
    private void Awake()
    {
        if (PlayerPrefs.HasKey(CheckpointKey))
        {
            try { checkpoint = JsonUtility.FromJson<CheckpointData>(PlayerPrefs.GetString(CheckpointKey)); }
            catch (ArgumentException) { checkpoint = null; }
            if (checkpoint != null && firstLoadScene != null && checkpoint.sceneGuid == firstLoadScene.sceneReference.AssetGUID)
                checkpointScene = firstLoadScene;
            else if (checkpoint != null && bossLevelScene != null && checkpoint.sceneGuid == bossLevelScene.sceneReference.AssetGUID)
                checkpointScene = bossLevelScene;
            else if (checkpoint != null && secondBossLevelScene != null && checkpoint.sceneGuid == secondBossLevelScene.sceneReference.AssetGUID)
                checkpointScene = secondBossLevelScene;
            else if (checkpoint != null && thirdBossLevelScene != null && checkpoint.sceneGuid == thirdBossLevelScene.sceneReference.AssetGUID)
                checkpointScene = thirdBossLevelScene;
            else if (checkpoint != null && caveBossLevelScene != null && checkpoint.sceneGuid == caveBossLevelScene.sceneReference.AssetGUID)
                checkpointScene = caveBossLevelScene;
            else if (checkpoint != null && mothBossLevelScene != null && checkpoint.sceneGuid == mothBossLevelScene.sceneReference.AssetGUID)
                checkpointScene = mothBossLevelScene;
            else if (checkpoint != null && mainTownScene != null && checkpoint.sceneGuid == mainTownScene.sceneReference.AssetGUID)
                checkpointScene = mainTownScene;
            else if (checkpoint != null && Array.Exists(additionalBossScenes, scene => scene != null && scene.sceneReference.AssetGUID == checkpoint.sceneGuid))
                checkpointScene = Array.Find(additionalBossScenes, scene => scene != null && scene.sceneReference.AssetGUID == checkpoint.sceneGuid);
            else if (checkpoint != null)
            {
                var navigation = GetComponent<TownHubNavigation>();
                if (navigation != null && navigation.destinations != null)
                    checkpointScene = Array.Find(navigation.destinations,
                        destination => destination != null && destination.sceneReference.AssetGUID == checkpoint.sceneGuid);
                if (checkpointScene == null) checkpoint = null;
            }
        }
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
        playerTrans.GetComponent<Character>().OnDead.AddListener(OnPlayerDeath);
    }

    private void OnDisable()
    {
        LoadEventSO.LoadRequesEvent -= OnLoadRequesEvent;
        NewGameEvent.onEventRaised -= NewGame;
        if (playerTrans != null)
            playerTrans.GetComponent<Character>().OnDead.RemoveListener(OnPlayerDeath);
        if (deathReturn != null) StopCoroutine(deathReturn);
        deathReturn = null;
    }

    public void NewGame()
    {
        checkpoint = null;
        checkpointScene = null;
        PlayerPrefs.DeleteKey(CheckpointKey);
        PlayerPrefs.Save();
        resetPlayerOnLoad = true;
      //  sceneToLoad = firstLoadScene;
       // OnLoadRequesEvent(sceneToLoad,firstPosition,true);
     //  LoadEventSO.RaiseLoadRequesEvent(sceneToLoad,firstPosition,true);
     LoadEventSO.RaiseLoadRequesEvent(firstLoadScene,firstPosition,true);
    }

    private void OnPlayerDeath()
    {
        if (deathReturn == null) deathReturn = StartCoroutine(ReturnToMenuAfterDeath());
    }

    private IEnumerator ReturnToMenuAfterDeath()
    {
        Time.timeScale = 1f;
        playerTrans.GetComponent<PlayerController>().PlayerDead();
        var animator = playerTrans.GetComponent<Animator>();
        yield return null;
        while (animator != null && animator.gameObject.activeInHierarchy)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Base Layer.GreenDead") && state.normalizedTime >= 1f)
                break;
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        while (isLoading) yield return null;
        if (checkpoint != null && checkpointScene != null)
        {
            resetPlayerOnLoad = true;
            OnLoadRequesEvent(checkpointScene, checkpoint.position, true);
        }
        else OnLoadRequesEvent(menuScene, menuPosition, true);
        deathReturn = null;
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
    bool isMenu = currentLoadedScene.sceneType == SceneType.Menu;
    playerTrans.gameObject.SetActive(!isMenu);
    if (!isMenu && resetPlayerOnLoad)
    {
        playerTrans.GetComponent<PlayerController>().ResetForNewGame();
        resetPlayerOnLoad = false;
    }
    if (fadeScreen)
    {
        //渐影渐出变透明
        FadeEvent.FadeOut(fadeDuration);
    }

    isLoading = false;

    if (currentLoadedScene.sceneType != SceneType.Menu)
    {
        if (currentLoadedScene == mainTownScene)
            SaveCheckpoint("TwilightRiverTown", mainTownScene, mainTownPosition);
        //场景加载完成后事件
        afterSceneLoadedEvent.RaiseEvent();
    }

}
}
