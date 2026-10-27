using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayMoonveilMothBoss
{
    private const string PendingKey = "MoonveilMothBoss.PlayPending";
    static PlayMoonveilMothBoss()
    {
        EditorApplication.update += TryEnter;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode) SessionState.SetBool(PendingKey, false);
        };
    }
    [MenuItem("Tools/Boss/Play Moonveil Moth Boss")]
    public static void Play()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildMoonveilMothBossLevel.ScenePath) == null)
            throw new InvalidOperationException("Build BossScene_5 first.");
        if (!EditorApplication.isPlaying)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save current scene changes before starting the boss preview.");
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Single);
        }
        SessionState.SetBool(PendingKey, true);
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
    }
    private static void TryEnter()
    {
        if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        var existing = UnityEngine.Object.FindObjectOfType<MoonveilMothBoss>();
        if (existing != null) { SessionState.SetBool(PendingKey, false); return; }
        // Wait for SceneLoader's normal startup menu before requesting the encounter.
        var menu = SceneManager.GetSceneByName("Menu");
        if (!menu.isLoaded) return;
        var loader = UnityEngine.Object.FindObjectOfType<SceneLoader>();
        if (loader == null) return;
        var data = AssetDatabase.LoadAssetAtPath<GameSceneSO>("Assets/DataSO/GameScene/Game BossScene5.asset");
        if (loader.EnterBossLevel(data, new Vector3(-11, 0.05f, 0))) SessionState.SetBool(PendingKey, false);
    }
}
