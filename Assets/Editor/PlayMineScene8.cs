using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayMineScene8
{
    const string Pending="MineScene8.PlayPending";
    static PlayMineScene8()
    {
        EditorApplication.update+=TryEnter;
        EditorApplication.playModeStateChanged+=s=>
        { if(s==PlayModeStateChange.ExitingPlayMode) SessionState.SetBool(Pending,false); };
    }
    [MenuItem("Tools/Levels/Play Mine SampleScene_8")]
    public static void Play()
    {
        if(!EditorApplication.isPlaying)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Single);
        }
        SessionState.SetBool(Pending,true);
        EditorApplication.isPlaying=true;
    }
    static void TryEnter()
    {
        if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||EditorApplication.isPaused) return;
        if(!SceneManager.GetSceneByName("Menu").isLoaded) return;
        var loader=UnityEngine.Object.FindObjectOfType<SceneLoader>();
        if(loader==null) return;
        var data=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene8.asset");
        SessionState.SetBool(Pending,false);
        loader.LoadEventSO.RaiseLoadRequesEvent(data,new Vector3(116f/24f,(683f-354f)/24f+.08f,0),true);
    }
}
