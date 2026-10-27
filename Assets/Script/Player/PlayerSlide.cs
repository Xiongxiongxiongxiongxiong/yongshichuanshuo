using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerSlide : MonoBehaviour
{
    public float duration = 0.5f;
    public float slideSpeed = 10f;
    public float staminaCost = 50f;
    public Sprite[] frames;
    public bool IsSliding { get; private set; }
    private PlayerController controller;
    private PhysicsCheck checks;
    private Rigidbody2D body;
    private CapsuleCollider2D capsule;
    private SpriteRenderer visual;
    private Animator animator;
    private PlayerStamina stamina;
    private Vector2 originalSize, originalOffset;
    private Sprite originalSprite;
    private bool animatorWasEnabled;
    private float elapsed, direction;
    private readonly List<Collider2D> ignored = new List<Collider2D>();

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        checks = GetComponent<PhysicsCheck>();
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        stamina = GetComponent<PlayerStamina>();
    }

    private void Update()
    {
        if (!IsSliding)
        {
            if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
                TryStartSlide();
            return;
        }
        elapsed += Time.deltaTime;
        if (elapsed >= duration || controller.isDead || !controller.inputcontroller.GamePlayer.Move.enabled)
            EndSlide();
    }

    public bool TryStartSlide()
    {
        if (controller.IsUsingSwordSkill || controller.IsWeaponUpgrading) return false;
        if (IsSliding || controller.isDead || controller.isHurt || controller.isAttack ||
            controller.isCrouching || controller.IsClimbingDown || !controller.inputcontroller.GamePlayer.Move.enabled ||
            frames == null || frames.Length == 0) return false;
        checks.RefreshChecks();
        if (!checks.isground) return false;
        if (stamina == null || !stamina.TrySpend(staminaCost)) return false;
        originalSize = capsule.size;
        originalOffset = capsule.offset;
        originalSprite = visual.sprite;
        animatorWasEnabled = animator.enabled;
        direction = visual.flipX ? -1f : 1f;
        elapsed = 0;
        IsSliding = true;
        animator.enabled = false;
        float height = originalSize.y * 0.45f;
        capsule.size = new Vector2(originalSize.x, height);
        capsule.offset = originalOffset - new Vector2(0, (originalSize.y - height) * 0.5f);
        IgnoreEnemies();
        body.velocity = new Vector2(direction * slideSpeed, body.velocity.y);
        return true;
    }

    private void FixedUpdate()
    {
        if (!IsSliding) return;
        IgnoreEnemies();
        body.velocity = new Vector2(direction * slideSpeed, body.velocity.y);
    }

    private void LateUpdate()
    {
        if (IsSliding)
            visual.sprite = frames[Mathf.Min(frames.Length - 1, (int)(elapsed / duration * frames.Length))];
    }

    private void IgnoreEnemies()
    {
        foreach (var enemy in FindObjectsOfType<Character>())
        {
            if (enemy.gameObject == gameObject || enemy.GetComponent<PlayerController>() != null) continue;
            foreach (var other in enemy.GetComponentsInChildren<Collider2D>())
            {
                if (ignored.Contains(other) || Physics2D.GetIgnoreCollision(capsule, other)) continue;
                Physics2D.IgnoreCollision(capsule, other, true);
                ignored.Add(other);
            }
        }
    }

    public void EndSlide()
    {
        if (!IsSliding) return;
        IsSliding = false;
        foreach (var other in ignored)
            if (other != null) Physics2D.IgnoreCollision(capsule, other, false);
        ignored.Clear();
        capsule.size = originalSize;
        capsule.offset = originalOffset;
        visual.sprite = originalSprite;
        animator.enabled = animatorWasEnabled;
        body.velocity = new Vector2(0, body.velocity.y);
    }

    private void OnDisable() { EndSlide(); }
}
