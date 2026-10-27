using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayDesertDungeonScenes
{
    const string Pending="DesertDungeon.PendingScene";
    static PlayDesertDungeonScenes()
    {
        EditorApplication.update+=TryEnter;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)SessionState.SetInt(Pending,0);};
    }
    [MenuItem("Tools/Levels/Play Desert 19 - Oasis Trail")]public static void Play19(){Play(19);}
    [MenuItem("Tools/Levels/Play Desert 20 - Wind Canyon")]public static void Play20(){Play(20);}
    [MenuItem("Tools/Levels/Play Desert 21 - Lost Sand City")]public static void Play21(){Play(21);}
    [MenuItem("Tools/Levels/Play Dungeon 22 - Ironchain Prison")]public static void Play22(){Play(22);}
    [MenuItem("Tools/Levels/Play Dungeon 23 - Waterworks")]public static void Play23(){Play(23);}
    [MenuItem("Tools/Levels/Play Dungeon 24 - Shadow Royal Crypt")]public static void Play24(){Play(24);}
    public static void Play(int n)
    {
        if(n<19||n>24)throw new System.ArgumentOutOfRangeException("n");
        if(!EditorApplication.isPlaying)
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity",OpenSceneMode.Single);
        }
        SessionState.SetInt(Pending,n);EditorApplication.isPlaying=true;
    }
    static void TryEnter()
    {
        int n=SessionState.GetInt(Pending,0);if(n==0||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
        Application.runInBackground=true;
        bool ready=SceneManager.GetSceneByName("Menu").isLoaded;
        for(int i=19;i<=24;i++)ready|=SceneManager.GetSceneByName("SampleScene_"+i).isLoaded;
        if(!ready)return;
        var loader=Object.FindObjectOfType<SceneLoader>();if(loader==null)return;
        if((bool)typeof(SceneLoader).GetField("isLoading",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(loader))return;
        var data=AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game Scene"+n+".asset");
        SessionState.SetInt(Pending,0);loader.LoadEventSO.RaiseLoadRequesEvent(data,BuildDesertDungeonScenes.Spawn(n),true);
    }
}
