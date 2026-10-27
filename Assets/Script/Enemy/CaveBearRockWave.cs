using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CaveBearRockWave : MonoBehaviour
{
    public StonebackCaveBear owner;
    public bool wave;
    public Sprite[] frames;
    public int direction = 1;
    public LayerMask groundLayer = 1 << 6;
    public Attack damageSource;
    // Shared by the slam and its wave, independent of the player's invulnerability timer.
    public HashSet<Character> victims;
    public float DistanceTravelled { get; private set; }
    SpriteRenderer visual;
    float age;
    readonly Collider2D[] contacts = new Collider2D[12];
    void Awake() { visual = GetComponent<SpriteRenderer>(); }
    void FixedUpdate()
    {
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled) { Destroy(gameObject); return; }
        age += Time.fixedDeltaTime;
        if (age > (wave ? .9f : .5f)) { Destroy(gameObject); return; }
        if (frames != null && frames.Length >= 8) visual.sprite = frames[(wave ? 0 : 4) + (wave ? (int)(age * 12) % 4 : Mathf.Min(3, (int)(age * 9)))];
        visual.flipX = direction < 0;
        if (!wave) return;
        float step = Mathf.Min(4.5f * Time.fixedDeltaTime, 3 - DistanceTravelled);
        Vector2 point = transform.position;
        if (step <= 0 || Physics2D.Raycast(point + Vector2.up * .25f, Vector2.right * direction, step + .15f, groundLayer))
        { Destroy(gameObject); return; }
        var floor = Physics2D.Raycast(point + Vector2.right * direction * step + Vector2.up * .6f, Vector2.down, 1.15f, groundLayer);
        if (!floor || Mathf.Abs(floor.point.y - point.y) > .4f) { Destroy(gameObject); return; }
        point = floor.point + Vector2.up * .04f; transform.position = point; DistanceTravelled += step;
        if (victims == null) victims = new HashSet<Character>();
        int count = Physics2D.OverlapBoxNonAlloc(point + Vector2.up * .25f, new Vector2(.95f, .5f), 0, contacts, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            if (!(contacts[i] is CapsuleCollider2D) || contacts[i].isTrigger) continue;
            var p = contacts[i].GetComponentInParent<PlayerController>(); if (p == null || p.isDead) continue;
            var hp = p.GetComponent<Character>(); if (victims.Contains(hp)) continue;
            float before = hp.CurrentHp; hp.TakeDamage(damageSource);
            if (hp.CurrentHp < before) victims.Add(hp);
        }
    }
}
