using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ArenaBossEffect : MonoBehaviour
{
    public DesertDungeonBoss owner;
    public Sprite[] frames;
    public int row, attackId, damage;
    public float delay, lifetime, trackFor, stopX;
    public bool mainFloor, projectile, slow, pull, stopAtTarget;
    public Vector2 size = Vector2.one, velocity;
    private float age;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        if (owner == null || !owner.IsFighting || owner.Target == null || owner.Target.isDead) { Destroy(gameObject); return; }
        float previousAge = age; age += Time.deltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }
        if (age < trackFor) transform.position = owner.SurfaceAt(owner.Target.transform.position.x, mainFloor);
        bool warning = age < delay;
        int spriteRow = warning ? 0 : row;
        if (frames != null && frames.Length == 32) visual.sprite = frames[spriteRow * 4 + (int)(age * 8) % 4];
        visual.color = row >= 5 ? new Color(.65f, 1, .95f, warning ? .7f : 1) : Color.white;
        if (visual.sprite != null)
        {
            Vector2 s = visual.sprite.bounds.size;
            // The lantern's tall ring also communicates the flame's damaging height.
            float height = warning && row != 6 ? .2f : size.y;
            transform.localScale = new Vector3(size.x / Mathf.Max(.01f, s.x), height / Mathf.Max(.01f, s.y), 1);
        }
        if (warning) return;
        if (!projectile)
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * size.y * .5f;
            if (slow) owner.ApplySlow(center, size);
            if (damage > 0) owner.HitBox(center, size, damage, attackId);
            return;
        }
        Vector2 previous = transform.position;
        float dt = Mathf.Max(0, age - Mathf.Max(delay, previousAge));
        Vector2 delta = velocity * dt;
        var wall = Physics2D.BoxCast(previous + Vector2.up * size.y * .5f, size * .85f, 0, delta.normalized, delta.magnitude, owner.groundLayer);
        if (wall.collider != null) delta = delta.normalized * wall.distance;
        if (stopAtTarget && Mathf.Sign(stopX - previous.x) != Mathf.Sign(stopX - previous.x - delta.x)) { Destroy(gameObject); return; }
        transform.position += (Vector3)delta; visual.flipX = velocity.x < 0;
        owner.HitBox(previous + delta * .5f + Vector2.up * size.y * .5f, size + new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y)), damage, attackId, pull);
        if (wall.collider != null || transform.position.x < owner.arenaLeft || transform.position.x > owner.arenaRight || transform.position.y < owner.groundY - 1) Destroy(gameObject);
    }
}
