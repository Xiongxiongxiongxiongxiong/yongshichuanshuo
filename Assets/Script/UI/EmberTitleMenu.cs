using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>The persistent title view delegates all travel and saves to SceneLoader.</summary>
public sealed class EmberTitleMenu : MonoBehaviour
{
    public SceneLoader loader;
    public AudioManager audioManager;
    public CanvasGroup visibility;
    public RectTransform composition;
    public GameObject settingsPanel;
    public UnityEngine.UI.Button newGameButton, continueButton, settingsButton, closeSettingsButton;
    public UnityEngine.UI.Slider masterSlider, musicSlider, effectsSlider;
    public TMP_Text masterValue, musicValue, effectsValue, fullscreenLabel, muteLabel;
    const string Prefix = "EmberWalker.Settings.";
    bool shown, initialized, muted, visibilityInitialized;
    float musicBase = 1, effectsBase = 1;

    void Awake()
    {
        settingsPanel.SetActive(false);
        musicBase = audioManager.BGMSource.volume;
        effectsBase = audioManager.FXSource.volume;
        masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(Prefix + "Master", 1));
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(Prefix + "Music", 1));
        effectsSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(Prefix + "Effects", 1));
        muted = PlayerPrefs.GetInt(Prefix + "Muted", 0) == 1;
        if (PlayerPrefs.HasKey(Prefix + "Fullscreen") && !Application.isEditor)
            Screen.fullScreen = PlayerPrefs.GetInt(Prefix + "Fullscreen") == 1;
        initialized = true;
        ApplyAudio();
        RefreshFullscreen();
        FitComposition();
    }

    IEnumerator Start()
    {
        yield return null;
        if (visibility.interactable) Select(newGameButton.gameObject);
    }

    void Update()
    {
        bool visible = loader.CurrentScene == null || loader.CurrentScene.sceneType == SceneType.Menu;
        if (!visibilityInitialized || shown != visible)
        {
            visibilityInitialized = true;
            shown = visible;
            visibility.alpha = visible ? 1 : 0;
            if (!visible) settingsPanel.SetActive(false);
            else Select(newGameButton.gameObject);
        }
        visibility.interactable = visible && !loader.IsLoading;
        visibility.blocksRaycasts = visible;
        continueButton.interactable = loader.CanContinue && !loader.IsLoading;
        if (!visible || loader.IsLoading) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && settingsPanel.activeSelf)
        {
            var openDropdown = settingsPanel.GetComponentInChildren<TMP_Dropdown>();
            foreach(var dropdown in settingsPanel.GetComponentsInChildren<TMP_Dropdown>())
                if(dropdown.IsExpanded){openDropdown=dropdown;break;}
            if(openDropdown==null || !openDropdown.IsExpanded) CloseSettings();
        }
    }

    // CanvasScaler updates in Update; fit afterwards so a resized window uses its final canvas size.
    void LateUpdate() { FitComposition(); }

    public void FitComposition()
    {
        if (composition == null) return;
        var parent = composition.parent as RectTransform;
        if (parent == null) return;
        float scale = Mathf.Min(parent.rect.width / 1920f, parent.rect.height / 1080f);
        scale = Mathf.Max(.01f, scale);
        if (!Mathf.Approximately(composition.localScale.x, scale)) composition.localScale = Vector3.one * scale;
    }

    public void NewGame()
    {
        if (loader.IsLoading || settingsPanel.activeSelf) return;
        SaveSettings();
        loader.NewGame();
    }

    public void ContinueGame()
    {
        if (loader.IsLoading || !loader.CanContinue || settingsPanel.activeSelf) return;
        SaveSettings();
        loader.ContinueGame();
    }

    public void OpenSettings()
    {
        if (loader.IsLoading) return;
        settingsPanel.SetActive(true);
        RefreshFullscreen();
        Select(masterSlider.gameObject);
    }

    public void CloseSettings()
    {
        SaveSettings();
        settingsPanel.SetActive(false);
        Select(settingsButton.gameObject);
    }

    public void ChangeAudio(float value) { if (initialized) ApplyAudio(); }

    void ApplyAudio()
    {
        AudioListener.volume = muted ? 0 : masterSlider.value;
        audioManager.BGMSource.volume = musicBase * musicSlider.value;
        audioManager.FXSource.volume = effectsBase * effectsSlider.value;
        masterValue.text = Mathf.RoundToInt(masterSlider.value * 100) + "%";
        musicValue.text = Mathf.RoundToInt(musicSlider.value * 100) + "%";
        effectsValue.text = Mathf.RoundToInt(effectsSlider.value * 100) + "%";
        muteLabel.text = muted ? "声音：关" : "声音：开";
    }

    public void ToggleMute() { muted = !muted; ApplyAudio(); SaveSettings(); }
    public void ToggleFullscreen()
    {
        bool target = !IsFullscreen();
        PlayerPrefs.SetInt(Prefix + "Fullscreen", target ? 1 : 0);
        if (!Application.isEditor) Screen.fullScreen = target;
        RefreshFullscreen();
        PlayerPrefs.Save();
    }
    bool IsFullscreen() => PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
    void RefreshFullscreen() { fullscreenLabel.text = IsFullscreen() ? "全屏模式：开" : "全屏模式：关"; }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(Prefix + "Master", masterSlider.value);
        PlayerPrefs.SetFloat(Prefix + "Music", musicSlider.value);
        PlayerPrefs.SetFloat(Prefix + "Effects", effectsSlider.value);
        PlayerPrefs.SetInt(Prefix + "Muted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }
    public void QuitGame() { SaveSettings(); Application.Quit(); }
    void OnApplicationQuit() { if (initialized) SaveSettings(); }
    static void Select(GameObject target) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target); }
}
