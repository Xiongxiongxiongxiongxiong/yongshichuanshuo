using UnityEngine;

/// <summary>Presentation only. The original Animator still owns attack events and gameplay timing.</summary>
[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(PlayerController), typeof(SpriteRenderer))]
public sealed class ModularPlayerVisual : MonoBehaviour
{
    [SerializeField] private Sprite[] bodyFrames;
    [SerializeField] private Vector2[] handOffsets;
    [SerializeField] private SpriteRenderer bodyVisual;
    [SerializeField] private SpriteRenderer weaponVisual;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private float weaponScale = 1f;
    [Header("Locomotion")]
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Vector2[] runHandOffsets;
    [SerializeField, Min(.1f)] private float runStrideLength = 4.2f;

    private PlayerController controller;
    private PlayerSlide slide;
    private PlayerSwordSkills skills;
    private PhysicsCheck checks;
    private Rigidbody2D rigidBody;
    private Animator animator;
    private SpriteRenderer source;
    private bool previousForceRenderingOff;
    private float stateTime;
    private float runPhase;
    private static readonly int[] IdleSequence = { 0, 1, 2, 3, 4, 5, 4, 3, 2, 1 };
    private Pose previousPose;
    private int attackState;
    private TrailRenderer trail;
    private enum Pose { Idle, Run, Attack, Rise, Fall, Crouch, Hurt, Dead, Climb, Slide, Thrust, Spin, Upgrade }

    public Sprite EquippedWeapon => weaponVisual != null ? weaponVisual.sprite : null;
    public int CurrentFrame { get; private set; }
    public int CurrentRunFrame { get; private set; } = -1;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        slide = GetComponent<PlayerSlide>();
        skills = GetComponent<PlayerSwordSkills>();
        checks = GetComponent<PhysicsCheck>();
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        source = GetComponent<SpriteRenderer>();
        if (weaponVisual != null) trail = weaponVisual.GetComponentInChildren<TrailRenderer>(true);
    }

    private void OnEnable()
    {
        if (source == null) source = GetComponent<SpriteRenderer>();
        previousForceRenderingOff = source.forceRenderingOff;
        if (bodyFrames == null || bodyFrames.Length != 24 || bodyVisual == null ||
            weaponVisual == null || weaponSocket == null || handOffsets == null || handOffsets.Length != 24)
        {
            if (Application.isPlaying) Debug.LogError("Modular player art is incomplete; retaining the original player visual.", this);
            enabled = false;
            return;
        }
        source.forceRenderingOff = true;
        bodyVisual.enabled = true;
        weaponVisual.enabled = weaponVisual.sprite != null;
        stateTime = 0;
        runPhase = 0;
        if (Application.isPlaying) RefreshVisual(0);
    }

    // A separate child renderer avoids competing with Animator, slide and skill sprite writes.
    private void LateUpdate()
    {
        if (source != null) source.forceRenderingOff = true;
        if (Application.isPlaying) RefreshVisual(Time.deltaTime);
    }

    public void EquipWeapon(Sprite weapon, float scale = 1f)
    {
        if (weaponVisual == null) return;
        weaponVisual.sprite = weapon;
        weaponScale = Mathf.Max(0.01f, scale);
        if (trail != null) trail.Clear();
    }

    public void RefreshVisual(float deltaTime)
    {
        if (controller == null || bodyVisual == null) return;
        Pose pose = ResolvePose();
        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (pose != previousPose || (pose == Pose.Attack && attackState != info.shortNameHash))
        {
            stateTime = 0;
            if (trail != null) trail.Clear();
        }
        else stateTime += deltaTime;
        previousPose = pose;
        attackState = info.shortNameHash;

        int frame = 0;
        int runFrame = -1;
        float angle = -12f;
        bool showWeapon = true;
        switch (pose)
        {
            // Reverse through the breathing poses rather than snapping from the last to the first.
            case Pose.Idle: frame = IdleSequence[(int)(stateTime * 6) % IdleSequence.Length]; break;
            case Pose.Run:
                frame = 6;
                if (runFrames != null && runFrames.Length == 8 && runHandOffsets != null && runHandOffsets.Length == 8)
                {
                    // Advance by distance, so slow movement and speed changes do not cause foot skating.
                    float worldStride = Mathf.Max(.1f, runStrideLength * Mathf.Abs(transform.lossyScale.x));
                    runPhase = Mathf.Repeat(runPhase + Mathf.Abs(rigidBody.velocity.x) * deltaTime / worldStride, 1f);
                    runFrame = Mathf.FloorToInt(runPhase * runFrames.Length);
                }
                else frame += (int)(stateTime * 12) % 6;
                angle = -25;
                break;
            case Pose.Attack:
                frame = 12 + Mathf.Min(5, (int)((info.normalizedTime % 1f) * 6));
                break;
            case Pose.Rise: frame = 18; angle = -25; break;
            case Pose.Fall: frame = 19; angle = -45; break;
            case Pose.Crouch: frame = 20; angle = -70; break;
            case Pose.Hurt: frame = 21; angle = -45; break;
            case Pose.Dead: frame = 22; showWeapon = false; break;
            case Pose.Climb: frame = 23; showWeapon = false; break;
            case Pose.Slide: frame = 20; angle = -90; break;
            case Pose.Thrust: frame = stateTime < .08f ? 13 : stateTime < .29f ? 15 : 17; break;
            case Pose.Spin: frame = 12 + Mathf.Min(5, (int)(stateTime / .6f * 6)); break;
            case Pose.Upgrade: frame = 13; angle = 0; break;
        }
        if (frame >= 12 && frame <= 17)
        {
            // Angles follow the empty-handed swing drawn in the six attack frames.
            switch (frame) { case 12: angle = 35; break; case 13: angle = 10; break;
                case 14: angle = -65; break; case 15: angle = -90; break;
                case 16: angle = 70; break; default: angle = -25; break; }
        }
        if (pose == Pose.Thrust && frame == 15) angle = -90;
        if (pose == Pose.Spin) angle = 30 - stateTime / .6f * 720;
        if (pose == Pose.Upgrade) angle = 0;
        CurrentRunFrame = runFrame;
        CurrentFrame = runFrame >= 0 ? 24 + runFrame : frame;
        bodyVisual.sprite = runFrame >= 0 ? runFrames[runFrame] : bodyFrames[frame];
        bodyVisual.flipX = source.flipX;
        bodyVisual.color = source.color;
        bodyVisual.sharedMaterial = source.sharedMaterial;
        bodyVisual.sortingLayerID = source.sortingLayerID;
        bodyVisual.sortingOrder = source.sortingOrder;
        bodyVisual.enabled = source.enabled;

        float facing = source.flipX ? -1f : 1f;
        Vector2 hand = runFrame >= 0 ? runHandOffsets[runFrame] : handOffsets[frame];
        // The socket is a sibling of the body. Include its complete transform as well as
        // the current sprite's grip, so idle motion and authored body offsets carry the weapon.
        weaponSocket.position = bodyVisual.transform.TransformPoint(new Vector3(hand.x * facing, hand.y, 0));
        weaponSocket.localRotation = bodyVisual.transform.localRotation * Quaternion.Euler(0, 0, angle * facing);
        weaponSocket.localScale = Vector3.Scale(bodyVisual.transform.localScale, new Vector3(facing * weaponScale, weaponScale, 1));
        weaponVisual.sortingLayerID = source.sortingLayerID;
        weaponVisual.sortingOrder = source.sortingOrder + 1;
        weaponVisual.color = source.color;
        weaponVisual.sharedMaterial = source.sharedMaterial;
        weaponVisual.enabled = showWeapon && source.enabled && weaponVisual.sprite != null;
        if (trail != null)
        {
            trail.sortingLayerID = source.sortingLayerID;
            trail.sortingOrder = source.sortingOrder + 2;
            trail.emitting = weaponVisual.enabled && (pose == Pose.Attack || pose == Pose.Thrust || pose == Pose.Spin);
            trail.startColor = pose == Pose.Thrust || pose == Pose.Spin
                ? new Color(.3f, 1f, .65f, .8f) : new Color(.85f, .95f, 1f, .55f);
        }
    }

    private Pose ResolvePose()
    {
        if (controller.isDead) return Pose.Dead;
        if (controller.isHurt) return Pose.Hurt;
        if (controller.IsUsingSwordSkill) return skills != null && skills.IsWhirlwind ? Pose.Spin : Pose.Thrust;
        if (slide != null && slide.IsSliding) return Pose.Slide;
        if (controller.IsClimbingLadder) return Pose.Climb;
        if (controller.IsWeaponUpgrading) return Pose.Upgrade;
        if (controller.isAttack) return Pose.Attack;
        if (controller.isCrouching) return Pose.Crouch;
        if (checks != null && !checks.isground) return rigidBody.velocity.y > .1f ? Pose.Rise : Pose.Fall;
        return Mathf.Abs(rigidBody.velocity.x) > .15f ? Pose.Run : Pose.Idle;
    }

    private void OnDisable()
    {
        if (source != null) source.forceRenderingOff = previousForceRenderingOff;
        if (bodyVisual != null) bodyVisual.enabled = false;
        if (weaponVisual != null) weaponVisual.enabled = false;
        if (trail != null) { trail.emitting = false; trail.Clear(); }
    }
}
