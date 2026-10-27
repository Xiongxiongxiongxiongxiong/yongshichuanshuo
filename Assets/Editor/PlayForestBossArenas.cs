using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayForestBossArenas
{
    private const string Key = "ForestBossArena.PlayPending";
    static PlayForestBossArenas()
    {
        EditorApplication.update += Enter;
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode) SessionState.SetInt(Key, 0); };
    }
    [MenuItem("Tools/Boss/Play First Boss - Forest Sanctuary")] public static void First() { Play(1); }
    [MenuItem("Tools/Boss/Play Second Boss - Spore Grotto")] public static void Second() { Play(2); }
    public static void Play(int index)
    {
        if (!EditorApplication.isPlaying)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene changes first.");
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Single);
        }
        SessionState.SetInt(Key, index);
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
    }
    private static void Enter()
    {
        int index = SessionState.GetInt(Key, 0);
        if (index == 0 || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        var loader = UnityEngine.Object.FindObjectOfType<SceneLoader>();
        if (loader == null || loader.IsLoading || loader.CurrentScene == null) return;
        var data = AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game BossScene" + index + ".asset");
        if (loader.CurrentScene == data || loader.EnterBossLevel(data, new Vector3(-11, .05f, 0))) SessionState.SetInt(Key, 0);
    }
}
