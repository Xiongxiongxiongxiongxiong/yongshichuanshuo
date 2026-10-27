using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(DesertDungeonBoss), typeof(Character))]
public class DesertDungeonBossHud : MonoBehaviour
{
    public Sprite backgroundSprite, healthSprite, trailSprite;
    private DesertDungeonBoss boss;
    private Character health;
    private GameObject root;
    private Image fill, trail;
    private Text title, warning;
    private Font font;

    private void Awake()
    {
        boss = GetComponent<DesertDungeonBoss>(); health = GetComponent<Character>();
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 28);
        root = new GameObject("Desert Dungeon Boss HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        Image background = MakeImage("Boss Health Frame", backgroundSprite, new Vector2(540, 24));
        trail = MakeImage("Boss Damage Trail", trailSprite, new Vector2(520, 10));
        fill = MakeImage("Boss Health", healthSprite, new Vector2(520, 10));
        title = MakeText("Boss Name", 24, new Vector2(0, -70));
        warning = MakeText("Attack Warning", 20, new Vector2(0, -135));
        title.text = boss.Title;
        warning.color = new Color(1, 0.83f, 0.4f);
        Refresh(health); trail.fillAmount = fill.fillAmount;
        root.SetActive(false);
    }
    private Image MakeImage(string name, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(root.transform, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -100); rect.sizeDelta = size;
        var image = go.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = false;
        if (name != "Boss Health Frame")
        { image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; }
        return image;
    }
    private Text MakeText(string name, int size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(root.transform, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(1000, 40);
        var label = go.GetComponent<Text>(); label.font = font; label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
        var shadow = go.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, 0.85f);
        shadow.effectDistance = new Vector2(1, -1);
        return label;
    }
    private void OnEnable() { health.OnHealthChange.AddListener(Refresh); }
    private void OnDisable() { health.OnHealthChange.RemoveListener(Refresh); }
    private void Refresh(Character value)
    { if (fill != null) fill.fillAmount = value.maxHp > 0 ? Mathf.Clamp01(value.CurrentHp / value.maxHp) : 0; }
    private void LateUpdate()
    {
        bool visible = boss.State != DesertDungeonBoss.Mode.Dormant;
        if (root.activeSelf != visible) root.SetActive(visible);
        if (!visible) return;
        trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, Time.deltaTime * 0.5f);
        string text = boss.Enraged && boss.IsFighting ? boss.Title + "【狂怒】" : boss.Title;
        if (title.text != text) title.text = text;
        if (warning.text != boss.Warning) warning.text = boss.Warning;
    }
    private void OnDestroy() { if (font != null) Destroy(font); }
}


