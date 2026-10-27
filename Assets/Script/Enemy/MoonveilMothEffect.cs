using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MoonveilMothEffect : MonoBehaviour
{
    public enum Kind { Dust, Silk, Warning, Burst }
    public Kind kind;
    public MoonveilMothBoss owner;
    public Sprite[] frames;
    public Vector2 velocity;
    public float lifetime, width = 3, height = 1;
    private float age;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        age += Time.deltaTime;
        if (age >= lifetime || owner == null || !owner.IsFighting) { Destroy(gameObject); return; }
        int row = (int)kind, step = (int)(age * 8);
        if (frames != null && frames.Length >= 16)
            visual.sprite = frames[row * 4 + (kind == Kind.Burst ? Mathf.Min(3, step) : step % 4)];
        if (kind == Kind.Silk)
        {
            if (visual.sprite != null)
            {
                Vector2 size = visual.sprite.bounds.size;
                transform.localScale = new Vector3(width / Mathf.Max(0.01f, size.x), height / Mathf.Max(0.01f, size.y), 1);
            }
            owner.DamagePlayerInBox((Vector2)transform.position + Vector2.up * height * 0.5f, new Vector2(width, height), 12);
        }
        else if (kind == Kind.Dust)
        {
            Vector2 previous = transform.position, delta = velocity * Time.deltaTime;
            var wall = Physics2D.CircleCast(previous, 0.2f, delta.normalized, delta.magnitude, owner.groundLayer);
            if (wall.collider != null) delta = delta.normalized * wall.distance;
            transform.position += (Vector3)delta; visual.flipX = velocity.x < 0;
            owner.DamagePlayerInBox(previous + delta * 0.5f, new Vector2(0.5f + Mathf.Abs(delta.x), 0.5f + Mathf.Abs(delta.y)), 12);
            if (wall.collider != null || transform.position.x < owner.arenaLeft || transform.position.x > owner.arenaRight || transform.position.y < owner.groundY - 1)
                Destroy(gameObject);
        }
    }
}
