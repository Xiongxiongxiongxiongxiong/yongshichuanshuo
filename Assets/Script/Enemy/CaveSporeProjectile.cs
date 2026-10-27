using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CaveSporeProjectile : MonoBehaviour
{
    public WildernessCaveCreature owner;
    public Attack damageSource;
    public Sprite[] frames;
    public LayerMask groundLayer = 1 << 6;
    public Vector2 velocity;
    SpriteRenderer visual;
    float age;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    void Awake() { visual = GetComponent<SpriteRenderer>(); }
    void FixedUpdate()
    {
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled) { Destroy(gameObject); return; }
        age += Time.fixedDeltaTime;
        if (age > 3) { Destroy(gameObject); return; }
        if (frames != null && frames.Length >= 12) visual.sprite = frames[(int)(age * 12) % 4];
        velocity += Vector2.down * (9 * Time.fixedDeltaTime);
        Vector2 step = velocity * Time.fixedDeltaTime;
        int count = Physics2D.CircleCastNonAlloc(transform.position, .14f, step.normalized, hits,
            step.magnitude, groundLayer | (1 << 7));
        RaycastHit2D nearest = default(RaycastHit2D); float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var collider = hits[i].collider; if (collider == null || collider.isTrigger) continue;
            if ((groundLayer.value & (1 << collider.gameObject.layer)) == 0)
            {
                if (!(collider is CapsuleCollider2D)) continue;
                var player = collider.GetComponentInParent<PlayerController>();
                if (player == null || player.isDead || player.GetComponent<PlayerSlide>().IsSliding) continue;
            }
            if (hits[i].distance < distance) { distance = hits[i].distance; nearest = hits[i]; }
        }
        if (nearest.collider != null)
        {
            if ((groundLayer.value & (1 << nearest.collider.gameObject.layer)) == 0)
                nearest.collider.GetComponentInParent<PlayerController>().GetComponent<Character>().TakeDamage(damageSource);
            Burst(nearest.point); Destroy(gameObject); return;
        }
        transform.position += (Vector3)step;
    }
    void Burst(Vector2 position)
    {
        // Only place a persistent ground cloud on an actual landing surface.
        var floor = Physics2D.Raycast(position + Vector2.up * .35f, Vector2.down, 2, groundLayer);
        var go = new GameObject("Cave Spore Impact");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.position = floor ? floor.point + Vector2.up * .03f : position;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder;
        var damage = go.AddComponent<Attack>(); damage.enabled = false; damage.damage = 5;
        var effect = go.AddComponent<CaveSporeCloud>(); effect.owner = owner; effect.damageSource = damage;
        effect.frames = frames; effect.hasGround = floor;
        owner.TrackEffect(go);
    }
}
