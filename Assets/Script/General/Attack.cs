using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    private readonly HashSet<Character> hitThisSwing = new HashSet<Character>();
    private bool playerAttack;
    public int damage;
    public float attackRange;
    public float attcakRate;

    public void AddDamage(int amount)
    {
        if (amount > 0) damage += amount;
    }

    private void OnEnable()
    {
        hitThisSwing.Clear();
        var player = GetComponentInParent<PlayerController>();
        playerAttack = player != null;
        if (!playerAttack || transform == player.transform) return;
        var visual = player.GetComponent<SpriteRenderer>();
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (visual.flipX ? -1f : 1f);
        transform.localScale = scale;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        // Unity can deliver trigger messages to disabled MonoBehaviours too.
        // AI damage sources are disabled and only used during explicit attack windows.
        if (!isActiveAndEnabled) return;
        var owner = GetComponentInParent<Character>();
        if (owner != null && owner.CurrentHp <= 0) return;
        var target = other.GetComponentInParent<Character>();
        if (target == null || target == owner || target.CurrentHp <= 0) return;
        if (playerAttack && hitThisSwing.Contains(target)) return;
        float before = target.CurrentHp;
        target.TakeDamage(this);
        if (playerAttack && target.CurrentHp < before) hitThisSwing.Add(target);
    }
}
