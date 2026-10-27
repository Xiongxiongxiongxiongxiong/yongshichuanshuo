using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AzureSalamanderEffect : MonoBehaviour
{
    public enum Kind { Tongue, Mist, Warning, Burst }
    public Kind kind;
    public AzureSalamanderBoss owner;
    public Sprite[] frames;
    public float lifetime, width = 3, height = 1;
    public int direction = 1;
    private float age;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime || owner == null || !owner.IsFighting) { Destroy(gameObject); return; }
        int row = (int)kind, step = (int)(age * (kind == Kind.Tongue ? 16 : 8));
        if (frames != null && frames.Length >= 16)
            visual.sprite = frames[row * 4 + (kind == Kind.Burst || kind == Kind.Tongue ? Mathf.Min(3, step) : step % 4)];
        if (kind != Kind.Tongue && kind != Kind.Mist) return;
        if (visual.sprite != null)
        {
            Vector2 size = visual.sprite.bounds.size;
            transform.localScale = new Vector3(width / Mathf.Max(0.01f, size.x), height / Mathf.Max(0.01f, size.y), 1);
            visual.flipX = kind == Kind.Tongue && direction < 0;
        }
        Vector2 center = transform.position;
        if (kind == Kind.Tongue) center += Vector2.right * direction * width * 0.5f;
        else center += Vector2.up * height * 0.5f;
        owner.DamagePlayerInBox(center, new Vector2(width, height), 15);
    }
}
