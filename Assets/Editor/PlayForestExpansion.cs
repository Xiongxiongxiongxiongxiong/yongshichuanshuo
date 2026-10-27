using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayForestExpansion
{
    const string Pending="ForestExpansion.PendingScene";
    static PlayForestExpansion()
    {
        EditorApplication.update+=TryEnter;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)SessionState.SetInt(Pending,0);};
    }
    [MenuItem("Tools/Levels/Play Forest 12 - Creek Valley")]
    public static void Play12(){Play(12);}
    [MenuItem("Tools/Levels/Play Forest 13 - Ancient Tree Village")]
    public static void Play13(){Play(13);}
    [MenuItem("Tools/Levels/Play Forest 14 - Moonlit Ruins")]
    public static void Play14(){Play(14);}
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
        SessionState.SetInt(Pending,0);loader.LoadEventSO.RaiseLoadRequesEvent(data,BuildForestExpansion.Spawn(n),true);
    }
}
