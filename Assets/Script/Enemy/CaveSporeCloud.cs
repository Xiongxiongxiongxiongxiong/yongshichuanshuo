using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CaveSporeCloud : MonoBehaviour
{
    public WildernessCaveCreature owner;
    public Attack damageSource;
    public Sprite[] frames;
    public bool hasGround = true;
    public float Duration = 2f;
    public bool HasHit { get; private set; }
    float age;
    SpriteRenderer visual;
    readonly Collider2D[] contacts = new Collider2D[12];
    void Awake() { visual = GetComponent<SpriteRenderer>(); }
    void Update()
    {
        age += Time.deltaTime;
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled || age > (hasGround ? Duration + .2f : .3f))
        { Destroy(gameObject); return; }
        if (frames != null && frames.Length >= 12)
            visual.sprite = frames[age < .2f ? 4 + Mathf.Min(3, (int)(age * 20)) : 8 + (int)((age - .2f) * 8) % 4];
        float alpha = Mathf.Clamp01((Duration + .2f - age) / .4f);
        visual.color = new Color(1, 1, 1, alpha * .85f);
        if (!hasGround || age < .2f || HasHit) return;
        int count = Physics2D.OverlapBoxNonAlloc((Vector2)transform.position + Vector2.up * .27f,
            new Vector2(1.6f, .55f), 0, contacts, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            if (!(contacts[i] is CapsuleCollider2D) || contacts[i].isTrigger) continue;
            var p = contacts[i].GetComponentInParent<PlayerController>(); if (p == null || p.isDead) continue;
            var hp = p.GetComponent<Character>(); float before = hp.CurrentHp; hp.TakeDamage(damageSource);
            if (hp.CurrentHp < before) { HasHit = true; return; }
        }
    }
}
