using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class PlayerAnimtion : MonoBehaviour
{
    [Header("梯子攀爬动画")]
    [SerializeField] private Texture2D climbDownSheet;
    [SerializeField] private Sprite[] authoredClimbFrames;
    [SerializeField] private float climbDownFramesPerSecond = 8f;
    [Header("武器升级动画")]
    [SerializeField] private Texture2D weaponUpgradeSheet;
    [SerializeField] private Sprite[] authoredUpgradeFrames;
    [SerializeField] private float weaponUpgradeFramesPerSecond = 8f;
    private Animator anim;
    private Rigidbody2D rd;
    private PhysicsCheck physicsCheck;
    private PlayerController playerController;
    private SpriteRenderer spriteRenderer;
    private Sprite[] climbDownFrames;
    private Sprite[] weaponUpgradeFrames;
    private bool wasPlayingClimbDown;
    private Coroutine weaponUpgradeRoutine;
    private Action weaponUpgradeCompleted;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rd = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        playerController = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateClimbDownFrames();
        CreateWeaponUpgradeFrames();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SetAnimtion();
    }
   //跑步
    public void SetAnimtion()
    {
        if (playerController.IsUsingSwordSkill) return;
        var slide = GetComponent<PlayerSlide>();
        if (slide != null && slide.IsSliding) return;
        if (weaponUpgradeRoutine != null) return;
        if (playerController.IsClimbingLadder)
        {
            PlayClimbDownAnimation();
            return;
        }
        StopClimbDownAnimation();
        if (playerController.isDead) return;
        anim.SetFloat("velocityX", Math.Abs(rd.velocity.x) );
        anim.SetFloat("velocityY",rd.velocity.y);
        anim.SetBool("isGround",physicsCheck.isground);
        anim.SetBool("isDead",playerController.isDead);
        anim.SetBool("isAttack",playerController.isAttack);
        anim.SetBool("isCrouching",playerController.isCrouching);
        if (playerController.isCrouching)
        {
            anim.SetFloat("velocityX", 0);
            anim.SetFloat("velocityY", 0);
            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.GreenCrouch"))
                anim.Play("Base Layer.GreenCrouch", 0, 0);
        }
       // anim.SetInteger("combo",playerController.combo);
    }

    public void PlayerHurt()
    {
        anim.SetTrigger("hurt");
    }
    
    //攻击方法
    public void PlayerAttack()
    {
        anim.SetTrigger("attack");
    }

    private void CreateClimbDownFrames()
    {
        if (authoredClimbFrames != null && authoredClimbFrames.Length > 0 &&
            Array.TrueForAll(authoredClimbFrames, frame => frame != null))
        { climbDownFrames = authoredClimbFrames; return; }
        if (climbDownSheet == null) return;

        // Sixth row, eight cells. The row's cell bounds are y=200..345 in this sheet.
        int[] xPositions = { 0, 168, 335, 503, 670, 838, 1006, 1173 };
        int[] widths = { 168, 167, 168, 167, 168, 167, 168, 168 };
        climbDownFrames = new Sprite[xPositions.Length];
        for (int i = 0; i < xPositions.Length; i++)
        {
            Rect frame = new Rect(xPositions[i], 200, widths[i], 145);
            climbDownFrames[i] = Sprite.Create(climbDownSheet, frame, new Vector2(0.5f, 0.1f), 48f);
            climbDownFrames[i].name = "GreenClimbDown_" + i;
        }
    }

    private void PlayClimbDownAnimation()
    {
        if (!wasPlayingClimbDown)
        {
            wasPlayingClimbDown = true;
            anim.enabled = false;
        }

        if (climbDownFrames == null || climbDownFrames.Length == 0) return;
        int frame = Mathf.FloorToInt(Time.time * climbDownFramesPerSecond) % climbDownFrames.Length;
        spriteRenderer.sprite = climbDownFrames[frame];
    }

    public void StopClimbDownAnimation()
    {
        if (!wasPlayingClimbDown) return;
        wasPlayingClimbDown = false;
        anim.enabled = true;
        if (playerController != null && !playerController.isDead)
        {
            anim.Play("Base Layer.GreenIde", 0, 0f);
            anim.Update(0f);
        }
    }

    public void PlayWeaponUpgradeAnimation(Action onComplete)
    {
        CancelWeaponUpgradeAnimation();
        weaponUpgradeCompleted = onComplete;
        weaponUpgradeRoutine = StartCoroutine(PlayWeaponUpgradeAnimationRoutine());
    }

    public void CancelWeaponUpgradeAnimation()
    {
        if (weaponUpgradeRoutine != null)
        {
            StopCoroutine(weaponUpgradeRoutine);
            weaponUpgradeRoutine = null;
            if (anim != null) anim.enabled = true;
        }
        weaponUpgradeCompleted = null;
    }

    private IEnumerator PlayWeaponUpgradeAnimationRoutine()
    {
        if (weaponUpgradeFrames == null || weaponUpgradeFrames.Length == 0)
        {
            weaponUpgradeRoutine = null;
            weaponUpgradeCompleted?.Invoke();
            weaponUpgradeCompleted = null;
            yield break;
        }

        anim.enabled = false;
        float frameDuration = 1f / Mathf.Max(1f, weaponUpgradeFramesPerSecond);
        foreach (var frame in weaponUpgradeFrames)
        {
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(frameDuration);
        }

        anim.enabled = true;
        if (playerController != null && !playerController.isDead)
        {
            anim.Play("Base Layer.GreenIde", 0, 0f);
            anim.Update(0f);
        }
        weaponUpgradeRoutine = null;
        weaponUpgradeCompleted?.Invoke();
        weaponUpgradeCompleted = null;
    }

    private void CreateWeaponUpgradeFrames()
    {
        if (authoredUpgradeFrames != null && authoredUpgradeFrames.Length > 0 &&
            Array.TrueForAll(authoredUpgradeFrames, frame => frame != null))
        { weaponUpgradeFrames = authoredUpgradeFrames; return; }
        if (weaponUpgradeSheet == null) return;

        // Row nine of char_green_4, using the source sheet's sliced sprite bounds.
        float pixelsPerUnit = spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.pixelsPerUnit
            : 32.571426f;
        float[] xPositions = { 0f, 157.18991f, 313.20676f, 470.39667f, 626.4135f, 783.6034f, 940.79333f, 1096.8102f };
        float[] widths = { 157.18991f, 157.18991f, 157.18991f, 157.18991f, 157.18991f, 157.18991f, 157.18991f, 157.18985f };
        // The body shifts inside the cells as the sword is raised. Anchor the
        // lower body, rather than the center of the cell (which includes the sword).
        float[] bodyAnchors = { 55f, 63f, 69f, 71f, 71f, 68f, 69f, 86f };
        weaponUpgradeFrames = new Sprite[xPositions.Length];
        for (int i = 0; i < xPositions.Length; i++)
        {
            Rect frame = new Rect(xPositions[i], 228.30978f, widths[i], 114.15489f);
            weaponUpgradeFrames[i] = Sprite.Create(weaponUpgradeSheet, frame,
                new Vector2(bodyAnchors[i] / widths[i], 0.03731343f), pixelsPerUnit);
            weaponUpgradeFrames[i].name = "GreenWeaponUpgrade_" + i;
        }
    }

    private void OnDestroy()
    {
        if (climbDownFrames != null && climbDownFrames != authoredClimbFrames)
            foreach (var frame in climbDownFrames)
                if (frame != null) Destroy(frame);
        if (weaponUpgradeFrames != null && weaponUpgradeFrames != authoredUpgradeFrames)
            foreach (var frame in weaponUpgradeFrames)
                if (frame != null) Destroy(frame);
    }
    
    
    
}
