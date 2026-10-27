using UnityEngine;

public class MineOreProjectile : MonoBehaviour
{
    public Attack source;
    public Character owner;
    public Vector2 velocity;
    public LayerMask groundLayer;
    private float age;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[12];

    private void FixedUpdate()
    {
        if (owner == null || owner.CurrentHp <= 0 || source == null) { Destroy(gameObject); return; }
        float delta = Time.fixedDeltaTime;
        velocity += Vector2.down * (5f * delta);
        Vector2 step = velocity * delta;
        int count = Physics2D.CircleCastNonAlloc(transform.position, 0.18f, step.normalized,
            hits, step.magnitude, groundLayer | (1 << 7));
        for (int i = 0; i < count; i++)
        {
            var collider = hits[i].collider;
            if (collider == null || collider.isTrigger) continue;
            if ((groundLayer.value & (1 << collider.gameObject.layer)) != 0) { Destroy(gameObject); return; }
            if (!(collider is CapsuleCollider2D)) continue;
            var player = collider.GetComponentInParent<PlayerController>();
            if (player == null || player.isDead) continue;
            player.GetComponent<Character>().TakeDamage(source);
            Destroy(gameObject); return;
        }
        transform.position += (Vector3)step;
        transform.Rotate(0, 0, 240f * delta);
        age += delta;
        if (age >= 3f) Destroy(gameObject);
    }
}
