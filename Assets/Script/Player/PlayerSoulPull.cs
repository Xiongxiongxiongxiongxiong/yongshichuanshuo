using UnityEngine;

// Runs after normal player physics without disabling input or teleporting across geometry.
[DefaultExecutionOrder(200)]
public class PlayerSoulPull : MonoBehaviour
{
    Rigidbody2D body;
    CapsuleCollider2D capsule;
    PlayerController player;
    Transform source;
    LayerMask ground;
    float remaining, until;
    public float DistancePulled { get; private set; }
    public bool IsPulling => remaining > 0 && Time.time < until;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    void Awake() { body = GetComponent<Rigidbody2D>(); capsule = GetComponent<CapsuleCollider2D>(); player = GetComponent<PlayerController>(); }
    public void Begin(Transform target, LayerMask groundLayer)
    { source = target; ground = groundLayer; remaining = 1; DistancePulled = 0; until = Time.time + .4f; }
    void OnDisable() { remaining = 0; source = null; }
    void FixedUpdate()
    {
        if (!IsPulling) return;
        if (source == null || player.isDead || player.IsClimbingDown || !player.inputcontroller.GamePlayer.Move.enabled || GetComponent<PlayerSlide>().IsSliding)
        { remaining = 0; return; }
        float direction = Mathf.Sign(source.position.x - body.position.x);
        float step = Mathf.Min(4 * Time.fixedDeltaTime, remaining);
        // Require a supporting landing surface at the leading foot; never drag a player off a ledge.
        Vector2 foot = new Vector2(capsule.bounds.center.x + direction * (capsule.bounds.extents.x + step), capsule.bounds.min.y + .15f);
        if (!Physics2D.Raycast(foot, Vector2.down, .4f, ground)) { remaining = 0; return; }
        var filter = new ContactFilter2D(); filter.SetLayerMask(ground); filter.useTriggers = false;
        int count = capsule.Cast(Vector2.right * direction, filter, hits, step + .04f);
        for (int i = 0; i < count; i++) if (hits[i].normal.x * direction < -.2f) step = Mathf.Min(step, Mathf.Max(0, hits[i].distance - .04f));
        if (step < .002f || Mathf.Abs(source.position.x - body.position.x) < .9f) { remaining = 0; return; }
        body.velocity = new Vector2(0, body.velocity.y);
        body.MovePosition(body.position + Vector2.right * direction * step);
        remaining -= step; DistancePulled += step;
    }
}
