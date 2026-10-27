using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CrystalbackEffect : MonoBehaviour
{
    public enum Kind { Wave, Crystal, Warning, Burst }
    public Kind kind;
    public CrystalbackBoss owner;
    public Sprite[] frames;
    public Vector2 velocity;
    public float delay, lifetime;
    private float age;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime || owner == null || !owner.IsFighting) { Destroy(gameObject); return; }
        visual.enabled = age >= delay;
        if (age < delay) return;
        int step = (int)((age - delay) * 8), row = (int)kind;
        if (frames != null && frames.Length >= 16)
            visual.sprite = frames[row * 4 + (kind == Kind.Burst ? Mathf.Min(3, step) : step % 4)];
        if (kind != Kind.Wave && kind != Kind.Crystal) return;
        Vector2 previous = transform.position, delta = velocity * Time.deltaTime;
        Vector2 origin = previous + (kind == Kind.Wave ? Vector2.up * 0.3f : Vector2.zero);
        var wall = Physics2D.CircleCast(origin, 0.15f, delta.normalized, delta.magnitude, owner.groundLayer);
        if (wall.collider != null) delta = delta.normalized * wall.distance;
        transform.position += (Vector3)delta;
        visual.flipX = kind == Kind.Wave && velocity.x < 0;
        Vector2 center = previous + delta * 0.5f + (kind == Kind.Wave ? Vector2.up * 0.3f : Vector2.zero);
        Vector2 size = kind == Kind.Wave ? new Vector2(1.2f + Mathf.Abs(delta.x), 0.6f) : new Vector2(0.8f, 1 + Mathf.Abs(delta.y));
        if (kind == Kind.Crystal && wall.collider != null)
        {
            float top = center.y + size.y * 0.5f;
            float bottom = Mathf.Max(center.y - size.y * 0.5f, wall.point.y + 0.02f);
            size.y = Mathf.Max(0, top - bottom); center.y = (top + bottom) * 0.5f;
        }
        if (size.y > 0) owner.DamagePlayerInBox(center, size, 15);
        if (wall.collider != null || transform.position.x < owner.arenaLeft || transform.position.x > owner.arenaRight || transform.position.y < owner.groundY - 1)
            Destroy(gameObject);
    }
}
