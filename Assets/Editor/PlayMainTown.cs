using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayMainTown
{
    const string Pending="MainTown.Pending";
    static PlayMainTown(){EditorApplication.update+=TryEnter;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)SessionState.SetBool(Pending,false);};}
    [MenuItem("Tools/Levels/Play Main Town - Twilight River")]
    public static void Play()
    {
        if(!EditorApplication.isPlaying)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Single);
        }
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void TryEnter()
    {
        if(!SessionState.GetBool(Pending,false)||!Application.isPlaying||!SceneManager.GetSceneByName("Menu").isLoaded)return;
        var loader=Object.FindObjectOfType<SceneLoader>();if(loader==null||loader.IsLoading)return;
        SessionState.SetBool(Pending,false);loader.ReturnToTown();
    }
}
