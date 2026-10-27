using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelCompleteUI : MonoBehaviour
{
    public GameObject panel;
    public Button retryButton;
    public GameSceneSO retryScene;
    public Vector3 retryPosition;
    public bool IsOpen { get; private set; }
    private SceneLoader loader;
    private Font runtimeFont;

    private void Awake()
    {
        runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 48);
        foreach (var label in panel.GetComponentsInChildren<Text>(true)) label.font = runtimeFont;
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var label in root.GetComponentsInChildren<TextMesh>(true))
                if (label.gameObject.name == "Goal Label")
                {
                    label.font = runtimeFont;
                    label.GetComponent<MeshRenderer>().sharedMaterial = runtimeFont.material;
                }
    }

    public void Show()
    {
        if (IsOpen) return;
        loader = FindObjectOfType<SceneLoader>();
        if (loader == null) return;
        var player = loader.playerTrans.GetComponent<PlayerController>();
        if (player.isDead) return;
        var slide = player.GetComponent<PlayerSlide>();
        if (slide != null) slide.EndSlide();
        player.inputcontroller.GamePlayer.Disable();
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        IsOpen = true;
        panel.SetActive(true);
        Time.timeScale = 0;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    public void Retry()
    {
        Close();
        if (retryScene != null) loader.EnterBossLevel(retryScene, retryPosition);
        else loader.NewGame();
    }

    public void ReturnToMenu()
    {
        Close();
        loader.ReturnToMainMenu();
    }

    private void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
        Time.timeScale = 1;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDestroy()
    {
        if (IsOpen) Time.timeScale = 1;
        if (runtimeFont != null) Destroy(runtimeFont);
    }
}
