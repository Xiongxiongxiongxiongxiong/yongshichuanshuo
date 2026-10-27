using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayMineExpansion
{
    const string Pending="MineExpansion.PendingScene";
    static PlayMineExpansion()
    {
        EditorApplication.update+=TryEnter;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)SessionState.SetInt(Pending,0);};
    }
    [MenuItem("Tools/Levels/Play Mine 9 - Blue Quartz")]
    public static void Play9(){Play(9);}
    [MenuItem("Tools/Levels/Play Mine 10 - Minecart Foundry")]
    public static void Play10(){Play(10);}
    [MenuItem("Tools/Levels/Play Mine 11 - Amethyst Ruins")]
    public static void Play11(){Play(11);}
    static void Play(int n)
    {
        if(!EditorApplication.isPlaying)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Single);
        }
        SessionState.SetInt(Pending,n);EditorApplication.isPlaying=true;
    }
    static void TryEnter()
    {
        int n=SessionState.GetInt(Pending,0);
        if(n==0||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
        Application.runInBackground=true;
        if(!SceneManager.GetSceneByName("Menu").isLoaded)return;
        var loader=Object.FindObjectOfType<SceneLoader>();if(loader==null)return;
        var loading=typeof(SceneLoader).GetField("isLoading",BindingFlags.NonPublic|BindingFlags.Instance);
        if((bool)loading.GetValue(loader))return;
        var data=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene"+n+".asset");
        SessionState.SetInt(Pending,0);loader.LoadEventSO.RaiseLoadRequesEvent(data,BuildMineExpansion.Spawn(n),true);
    }
}
