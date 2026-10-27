using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Character))]
public class MonsterHealthBar : MonoBehaviour
{
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Sprite healthSprite;
    [SerializeField] private Sprite trailSprite;
    [SerializeField] private float verticalOffset = 0.28f;
    private Character health;
    private Collider2D hitbox;
    private RectTransform bar;
    private Image fill, trail;

    private void Awake()
    {
        health = GetComponent<Character>();
        hitbox = GetComponent<Collider2D>();
        var go = new GameObject("Overhead Health Bar", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(transform, false);
        bar = go.GetComponent<RectTransform>();
        bar.sizeDelta = new Vector2(116, 12);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingLayerID = GetComponent<SpriteRenderer>().sortingLayerID;
        canvas.sortingOrder = 100;
        MakeImage("Background", backgroundSprite, new Vector2(116, 12), false);
        trail = MakeImage("Damage Trail", trailSprite, new Vector2(112, 4), true);
        fill = MakeImage("Health", healthSprite, new Vector2(112, 4), true);
        Refresh(health);
        trail.fillAmount = fill.fillAmount;
        PositionBar();
    }

    private Image MakeImage(string name, Sprite sprite, Vector2 size, bool filled)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(bar, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        if (filled)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
        }
        return image;
    }

    private void OnEnable()
    {
        health.OnHealthChange.AddListener(Refresh);
        health.OnDead.AddListener(Hide);
        Refresh(health);
    }
    private void OnDisable()
    {
        health.OnHealthChange.RemoveListener(Refresh);
        health.OnDead.RemoveListener(Hide);
    }
    private void Refresh(Character value)
    {
        if (bar == null) return;
        float ratio = value.maxHp > 0 ? Mathf.Clamp01(value.CurrentHp / value.maxHp) : 0;
        fill.fillAmount = ratio;
        if (trail.fillAmount < ratio) trail.fillAmount = ratio;
        bar.gameObject.SetActive(ratio > 0);
    }
    private void Hide() { if (bar != null) bar.gameObject.SetActive(false); }
    private void LateUpdate()
    {
        if (bar == null || health.CurrentHp <= 0) return;
        PositionBar();
        trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, Time.deltaTime);
    }
    private void PositionBar()
    {
        Bounds bounds = hitbox != null ? hitbox.bounds : GetComponent<SpriteRenderer>().bounds;
        bar.position = new Vector3(bounds.center.x, bounds.max.y + verticalOffset, transform.position.z);
        bar.rotation = Quaternion.identity;
        Vector3 scale = transform.lossyScale;
        bar.localScale = new Vector3(0.015f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            0.015f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1);
    }
}
