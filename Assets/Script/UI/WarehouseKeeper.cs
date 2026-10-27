using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Interaction belongs to the authored town NPC, not a global warehouse hotkey.</summary>
public sealed class WarehouseKeeper : MonoBehaviour
{
    [Min(.5f)] public float interactionRadius = 2.8f;
    private InventoryWindows windows;
    private PlayerController player;
    private GameObject prompt;
    private TMPro.TMP_Text label;

    public bool InRange(PlayerController target)
    {
        return isActiveAndEnabled && target != null && target.gameObject.scene.IsValid() &&
            Vector2.Distance(target.transform.position, transform.position) <= interactionRadius;
    }

    private void Start()
    {
        windows = FindObjectOfType<InventoryWindows>();
        if (windows == null) return;
        player = windows.player;
        prompt = new GameObject("Warehouse Keeper Prompt", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        prompt.transform.SetParent(transform, false);
        var canvas = prompt.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 130;
        var scaler = prompt.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var go = new GameObject("Talk Hint", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(prompt.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 0); rt.pivot = new Vector2(.5f, 0);
        rt.sizeDelta = new Vector2(460, 52); rt.anchoredPosition = new Vector2(0, 205);
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(.04f, .035f, .03f, .9f); image.raycastTarget = false;
        var text = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        text.transform.SetParent(go.transform, false);
        var tr = text.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        label = text.GetComponent<TMPro.TMP_Text>();
        label.font = windows.font; label.fontSize = 26;
        label.color = new Color(.98f, .9f, .7f); label.alignment = TMPro.TextAlignmentOptions.Center;
        label.text = "仓库管理员   [E] 对话"; label.raycastTarget = false;
        prompt.SetActive(false);
    }

    private void Update()
    {
        if (windows == null || player == null) return;
        bool nearby = InRange(player) && !player.isDead && player.gameObject.activeInHierarchy;
        bool available = nearby && !windows.IsOpen && !windows.hud.WindowOpen &&
            !windows.navigation.MapOpen && !windows.loader.IsLoading && Time.timeScale > 0;
        if (prompt != null) prompt.SetActive(available);
        if (available && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Interact();
    }

    public bool Interact() => windows != null && windows.Talk(this);
    private void OnDisable() { if (prompt != null) prompt.SetActive(false); }
}
