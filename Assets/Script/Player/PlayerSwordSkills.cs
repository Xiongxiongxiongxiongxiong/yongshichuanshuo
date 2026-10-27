using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController), typeof(PlayerStamina))]
public class PlayerSwordSkills : MonoBehaviour
{
    [SerializeField] private Texture2D animationSheet;
    [SerializeField] private Sprite[] authoredFrames;
    public bool IsUsingSkill { get; private set; }
    public bool IsWhirlwind => IsUsingSkill && spin;
    public float ThrustCooldownRemaining => Mathf.Max(0, thrustReady - Time.time);
    public float SpinCooldownRemaining => Mathf.Max(0, spinReady - Time.time);
    private PlayerController controller;
    private PlayerStamina stamina;
    private Rigidbody2D body;
    private CapsuleCollider2D capsule;
    private SpriteRenderer visual;
    private Animator animator;
    private Sprite originalSprite;
    private Sprite[] frames;
    private Attack damageSource;
    private readonly HashSet<Character> hitTargets = new HashSet<Character>();
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[12];
    private bool spin, animatorWasEnabled;
    private float elapsed, direction, thrustReady, spinReady;
    private int pulses;
    private const float ThrustDuration = 0.35f, SpinDuration = 0.6f;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        stamina = GetComponent<PlayerStamina>();
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        var source = new GameObject("Sword Skill Damage");
        source.transform.SetParent(transform, false);
        damageSource = source.AddComponent<Attack>();
        CreateFrames();
    }

    private void CreateFrames()
    {
        if (authoredFrames != null && authoredFrames.Length == 8 &&
            System.Array.TrueForAll(authoredFrames, frame => frame != null))
        {
            frames = authoredFrames;
            return;
        }
        if (animationSheet == null) return;
        // Source is 1774x887. Separate variable-width effects, anchor on body/feet.
        float sx = animationSheet.width / 1774f, sy = animationSheet.height / 887f;
        float[] left = { 25, 418, 900, 1458 };
        float[] right = { 340, 900, 1458, 1774 };
        float[] anchors = { 180, 640, 1080, 1600 };
        float ppu = 250f * sy / (visual.sprite.bounds.size.y * 0.85f);
        frames = new Sprite[8];
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 4; col++)
        {
            float top = row == 0 ? 145 : 460;
            float bottom = row == 0 ? 445 : 825;
            float foot = row == 0 ? 418 : 798;
            Rect rect = new Rect(left[col] * sx, (887 - bottom) * sy,
                (right[col] - left[col]) * sx, (bottom - top) * sy);
            frames[row * 4 + col] = Sprite.Create(animationSheet, rect,
                new Vector2((anchors[col] - left[col]) / (right[col] - left[col]),
                (bottom - foot) / (bottom - top)), ppu, 0, SpriteMeshType.FullRect);
            frames[row * 4 + col].name = (row == 0 ? "WindThrust_" : "Whirlwind_") + col;
        }
    }

    private void Update()
    {
        if (IsUsingSkill)
        {
            if (controller.isDead || controller.isHurt || !controller.inputcontroller.GamePlayer.Move.enabled)
            { CancelSkill(); return; }
            elapsed += Time.deltaTime;
            if (spin)
            {
                if (pulses == 0 && elapsed >= 0.12f) { HitEnemies(); pulses++; }
                if (pulses == 1 && elapsed >= 0.46f) { hitTargets.Clear(); HitEnemies(); pulses++; }
            }
            else if (elapsed >= 0.09f && elapsed < 0.29f) HitEnemies();
            if (elapsed >= (spin ? SpinDuration : ThrustDuration)) CancelSkill();
            return;
        }
        if (Keyboard.current == null) return;
        if (Keyboard.current.uKey.wasPressedThisFrame) TryUseSkill(false);
        else if (Keyboard.current.iKey.wasPressedThisFrame) TryUseSkill(true);
    }

    public bool TryUseSkill(bool whirlwind)
    {
        var slide = GetComponent<PlayerSlide>();
        if (IsUsingSkill || frames == null || controller.isDead || controller.isHurt ||
            controller.isAttack || controller.isCrouching || controller.IsClimbingLadder ||
            controller.IsWeaponUpgrading || !controller.inputcontroller.GamePlayer.Move.enabled ||
            (slide != null && slide.IsSliding) || Time.time < (whirlwind ? spinReady : thrustReady)) return false;
        if (!stamina.TrySpend(whirlwind ? 40 : 30)) return false;
        spin = whirlwind;
        if (spin) spinReady = Time.time + 5f; else thrustReady = Time.time + 3f;
        elapsed = 0; pulses = 0; direction = visual.flipX ? -1 : 1;
        hitTargets.Clear();
        originalSprite = visual.sprite;
        animatorWasEnabled = animator.enabled;
        animator.enabled = false;
        IsUsingSkill = true;
        body.velocity = new Vector2(0, body.velocity.y);
        return true;
    }

    private void FixedUpdate()
    {
        if (!IsUsingSkill) return;
        float velocity = !spin && elapsed >= 0.08f && elapsed < 0.28f ? direction * 13f : 0;
        if (velocity != 0)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(GetComponent<PhysicsCheck>().groundLayer);
            filter.useTriggers = false;
            int count = capsule.Cast(Vector2.right * direction, filter, wallHits, Mathf.Abs(velocity) * Time.fixedDeltaTime + 0.05f);
            for (int i = 0; i < count; i++)
                if (wallHits[i].normal.x * direction < -0.5f) { velocity = 0; break; }
        }
        body.velocity = new Vector2(velocity, body.velocity.y);
    }

    private void LateUpdate()
    {
        if (!IsUsingSkill) return;
        int frame = Mathf.Min(3, Mathf.FloorToInt(elapsed / (spin ? SpinDuration : ThrustDuration) * 4));
        visual.sprite = frames[(spin ? 4 : 0) + frame];
    }

    private void HitEnemies()
    {
        int baseDamage = 1;
        foreach (var a in GetComponentsInChildren<Attack>(true))
            if (a != damageSource) baseDamage = Mathf.Max(baseDamage, a.damage);
        damageSource.damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * (spin ? 0.8f : 2f)));
        Vector2 center = capsule.bounds.center;
        if (!spin) center += Vector2.right * direction * 0.9f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, spin ? new Vector2(4.2f, 3.6f) : new Vector2(2.4f, 2f), 0);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<Character>();
            if (enemy == null || enemy.gameObject == gameObject || enemy.GetComponent<PlayerController>() != null ||
                enemy.CurrentHp <= 0 || hitTargets.Contains(enemy)) continue;
            hitTargets.Add(enemy);
            enemy.TakeDamage(damageSource);
        }
    }

    public void CancelSkill()
    {
        if (!IsUsingSkill) return;
        IsUsingSkill = false;
        body.velocity = new Vector2(0, body.velocity.y);
        visual.sprite = originalSprite;
        animator.enabled = animatorWasEnabled;
        if (animator.enabled && !controller.isDead && !controller.isHurt)
        { animator.Play("Base Layer.GreenIde", 0, 0); animator.Update(0); }
    }

    public void ResetSkills() { CancelSkill(); thrustReady = spinReady = 0; }
    private void OnDisable() { CancelSkill(); }
    private void OnDestroy()
    {
        if (frames != null && frames != authoredFrames)
            foreach (var frame in frames) if (frame != null) Destroy(frame);
    }
}
