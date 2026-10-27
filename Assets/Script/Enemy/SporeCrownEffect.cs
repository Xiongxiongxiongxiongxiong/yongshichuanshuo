using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SporeCrownEffect : MonoBehaviour
{
    public enum Kind { Root, Spore, Warning, Burst }
    public Kind kind;
    public SporeCrownBoss owner;
    public Sprite[] frames;
    public Vector2 velocity;
    public float delay, lifetime;
    public float rootHeight = 1.6f;
    private float age;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime || owner == null || !owner.IsFighting) { Destroy(gameObject); return; }
        visual.enabled = age >= delay;
        if (age < delay) return;
        int row = (int)kind;
        if (frames != null && frames.Length >= 16)
        {
            int step = (int)((age - delay) * 8);
            int frame = kind == Kind.Spore || kind == Kind.Warning ? step % 4 : Mathf.Min(3, step);
            visual.sprite = frames[row * 4 + frame];
        }
        if (kind == Kind.Root)
        {
            // Cap both the rendered spike and its damage below the platform underside.
            if (visual.sprite != null)
                transform.localScale = new Vector3(1, Mathf.Min(1, rootHeight / Mathf.Max(0.01f, visual.sprite.bounds.max.y)), 1);
            owner.DamagePlayerInBox((Vector2)transform.position + Vector2.up * (rootHeight * 0.5f), new Vector2(1.3f, rootHeight), 15);
        }
        else if (kind == Kind.Spore)
        {
            Vector2 previous = transform.position;
            Vector2 step = velocity * Time.deltaTime;
            if (Physics2D.CircleCast(previous, 0.25f, step.normalized, step.magnitude, owner.groundLayer))
            { Destroy(gameObject); return; }
            transform.position += (Vector3)step; visual.flipX = velocity.x < 0;
            owner.DamagePlayerInBox(previous + step * 0.5f, new Vector2(0.6f + Mathf.Abs(step.x), 0.6f + Mathf.Abs(step.y)), 15);
        }
    }
}
