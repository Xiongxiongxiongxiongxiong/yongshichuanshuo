using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DesertDungeonProjectile : MonoBehaviour
{
    public enum Style { Thorn, SoulFire, SandFan, SoulWave, SoulChain }
    public DesertDungeonEnemy owner;
    public Style style;
    public Sprite[] frames;
    public Attack damageSource;
    public Vector2 velocity;
    public float maxRange = 5;
    public int fanLane;
    public LayerMask groundLayer = 1 << 6;
    public HashSet<Character> victims;
    public float Travelled { get; private set; }
    SpriteRenderer visual;
    float age;
    readonly RaycastHit2D[] casts = new RaycastHit2D[16];
    readonly Collider2D[] contacts = new Collider2D[16];
    bool GroundWave => style == Style.SandFan || style == Style.SoulWave;
    void Awake() { visual = GetComponent<SpriteRenderer>(); }
    void FixedUpdate()
    {
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled) { Destroy(gameObject); return; }
        age += Time.fixedDeltaTime;
        if (age > 3 || Travelled >= maxRange) { Destroy(gameObject); return; }
        int row = style == Style.SandFan || style == Style.SoulWave ? 1 : style == Style.SoulChain ? 2 : 0;
        if (frames != null && frames.Length >= 16) visual.sprite = frames[row * 4 + (int)(age * 12) % 4];
        visual.flipX = velocity.x < 0;
        Vector2 step = Vector2.ClampMagnitude(velocity * Time.fixedDeltaTime, maxRange - Travelled);
        Vector2 position = transform.position;
        if (victims == null) victims = new HashSet<Character>();
        if (GroundWave)
        {
            if (Physics2D.Raycast(position + Vector2.up * .22f, step.normalized, step.magnitude + .13f, groundLayer)) { Destroy(gameObject); return; }
            var floor = Physics2D.Raycast(position + step + Vector2.up * .6f, Vector2.down, 1.1f, groundLayer);
            if (!floor || Mathf.Abs(floor.point.y - position.y) > .42f) { Destroy(gameObject); return; }
            position = floor.point + Vector2.up * .04f; transform.position = position; Travelled += step.magnitude;
            // In a side view, the fan separates into three low arcs and different speeds.
            float height = style == Style.SandFan ? Mathf.Sin(Travelled / maxRange * Mathf.PI) * fanLane * .14f : 0;
            visual.transform.localScale = new Vector3(1, 1 + height, 1);
            int n = Physics2D.OverlapBoxNonAlloc(position + Vector2.up * (.22f + height), new Vector2(.9f, .45f), 0, contacts, 1 << 7);
            for (int i = 0; i < n; i++) TryHit(contacts[i]);
            return;
        }
        int count = Physics2D.CircleCastNonAlloc(position, style == Style.SoulChain ? .12f : .1f, step.normalized, casts, step.magnitude, groundLayer | (1 << 7));
        float nearest = float.PositiveInfinity; RaycastHit2D hit = default(RaycastHit2D);
        for (int i = 0; i < count; i++)
        {
            var c = casts[i].collider; if (c == null || c.isTrigger) continue;
            bool ground = (groundLayer.value & 1 << c.gameObject.layer) != 0;
            if (!ground)
            {
                if (!(c is CapsuleCollider2D)) continue;
                var p = c.GetComponentInParent<PlayerController>(); if (p == null || p.isDead || p.GetComponent<PlayerSlide>().IsSliding) continue;
            }
            if (casts[i].distance < nearest) { nearest = casts[i].distance; hit = casts[i]; }
        }
        if (hit.collider != null)
        { if ((groundLayer.value & 1 << hit.collider.gameObject.layer) == 0) TryHit(hit.collider); Destroy(gameObject); return; }
        transform.position = position + step; Travelled += step.magnitude;
    }
    bool TryHit(Collider2D collider)
    {
        if (!(collider is CapsuleCollider2D) || collider.isTrigger) return false;
        var p = collider.GetComponentInParent<PlayerController>(); if (p == null || p.isDead) return false;
        var hp = p.GetComponent<Character>(); if (victims.Contains(hp)) return false;
        float before = hp.CurrentHp; hp.TakeDamage(damageSource);
        if (hp.CurrentHp >= before) return false;
        victims.Add(hp);
        if (style == Style.SoulChain)
        {
            var pull = p.GetComponent<PlayerSoulPull>(); if (pull == null) pull = p.gameObject.AddComponent<PlayerSoulPull>();
            pull.Begin(owner.transform, groundLayer);
        }
        return true;
    }
}
