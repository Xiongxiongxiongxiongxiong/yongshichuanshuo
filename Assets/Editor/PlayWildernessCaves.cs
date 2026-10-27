using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayWildernessCaves
{
    const string Pending="WildernessCaves.PendingScene";
    static PlayWildernessCaves()
    {
        EditorApplication.update+=TryEnter;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)SessionState.SetInt(Pending,0);};
    }
    [MenuItem("Tools/Levels/Play Cave 15 - Moss Creek")]
    public static void Play15(){Play(15);}
    [MenuItem("Tools/Levels/Play Cave 16 - Red Rock Canyon")]
    public static void Play16(){Play(16);}
    [MenuItem("Tools/Levels/Play Cave 17 - Frost Mist")]
    public static void Play17(){Play(17);}
    static void Play(int n)
    {
        if(!EditorApplication.isPlaying){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Single);}
        SessionState.SetInt(Pending,n);EditorApplication.isPlaying=true;
    }
    static void TryEnter()
    {
        int n=SessionState.GetInt(Pending,0);if(n==0||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
        Application.runInBackground=true;if(!SceneManager.GetSceneByName("Menu").isLoaded)return;
        var loader=Object.FindObjectOfType<SceneLoader>();if(loader==null)return;
        if((bool)typeof(SceneLoader).GetField("isLoading",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(loader))return;
        var data=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene"+n+".asset");
        SessionState.SetInt(Pending,0);loader.LoadEventSO.RaiseLoadRequesEvent(data,BuildWildernessCaves.Spawn(n),true);
    }
}
