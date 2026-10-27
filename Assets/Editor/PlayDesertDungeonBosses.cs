using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayDesertDungeonBosses
{
    private const string Key = "DesertDungeonBoss.PlayPending";
    static PlayDesertDungeonBosses()
    {
        EditorApplication.update += Enter;
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.ExitingPlayMode) SessionState.SetInt(Key, -1); };
    }
    [MenuItem("Tools/Boss/Play Desert - Sunburial Scorpion")] public static void Scorpion() { Play(0); }
    [MenuItem("Tools/Boss/Play Desert - Stele Lizard")] public static void Lizard() { Play(1); }
    [MenuItem("Tools/Boss/Play Dungeon - Ironcoffin Jailer")] public static void Jailer() { Play(2); }
    [MenuItem("Tools/Boss/Play Dungeon - Soul Lantern")] public static void Lantern() { Play(3); }
    public static void Play(int index)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildDesertDungeonBosses.ScenePath(index)) == null) throw new InvalidOperationException("Build new encounters first.");
        if (!EditorApplication.isPlaying)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save current scene changes first.");
            EditorSceneManager.OpenScene("Assets/Scenes/persistent.unity", OpenSceneMode.Single);
        }
        SessionState.SetInt(Key, index);
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
    }
    private static void Enter()
    {
        int index = SessionState.GetInt(Key, -1);
        if (index < 0 || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
        var l = UnityEngine.Object.FindObjectOfType<SceneLoader>();
        if (l == null || l.IsLoading || l.CurrentScene == null) return;
        var data = AssetDatabase.LoadAssetAtPath<GameSceneSO>(BuildDesertDungeonBosses.DataPath(index));
        if (l.CurrentScene == data || l.EnterBossLevel(data, new Vector3(-11, .05f, 0))) SessionState.SetInt(Key, -1);
    }
}
