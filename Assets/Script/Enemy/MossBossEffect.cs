using UnityEngine;

public class MossBossEffect : MonoBehaviour
{
    public enum Kind { Shockwave, FallingRock, Warning }
    public Kind kind;
    public MossTuskBoss owner;
    public Sprite[] frames;
    public int direction;
    public float delay, lifetime = 1;
    public float impactGroundY;
    private float age;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        age += Time.deltaTime;
        if (age > lifetime || (kind != Kind.Warning && (owner == null || !owner.IsFighting)))
        { Destroy(gameObject); return; }
        int row = kind == Kind.Shockwave ? 0 : kind == Kind.FallingRock ? 1 : 2;
        if (frames != null && frames.Length >= 16) visual.sprite = frames[row * 4 + Mathf.Min(3, (int)(age * 6) % 4)];
        visual.enabled = age >= delay;
        if (age < delay) return;
        Vector2 previous = transform.position;
        if (kind == Kind.Shockwave)
        {
            if (Physics2D.Raycast(previous + Vector2.up * 0.25f, Vector2.right * direction,
                6f * Time.deltaTime + 0.05f, owner.groundLayer))
            { Destroy(gameObject); return; }
            visual.flipX = direction < 0;
            transform.position += Vector3.right * direction * 6f * Time.deltaTime;
            owner.DamagePlayerInBox((Vector2)transform.position + Vector2.up * 0.25f, new Vector2(1.5f, 0.55f), 15);
            if (transform.position.x < owner.arenaLeft || transform.position.x > owner.arenaRight) Destroy(gameObject);
        }
        else if (kind == Kind.FallingRock)
        {
            float speed = 9 + (age - delay) * 10;
            transform.position += Vector3.down * speed * Time.deltaTime;
            Vector2 current = transform.position;
            owner.DamagePlayerInBox((previous + current) * 0.5f,
                new Vector2(1.1f, 1.1f + Mathf.Abs(current.y - previous.y)), 15);
            if (transform.position.y <= impactGroundY + 0.45f) Destroy(gameObject);
        }
    }
}
