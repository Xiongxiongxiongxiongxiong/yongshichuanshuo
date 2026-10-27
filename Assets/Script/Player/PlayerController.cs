
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("监听事件")] public SceneLoadEventSO loadEvent;
    public VoidEventSO afterSceneLoadEvent;
    public player inputcontroller;
    private PhysicsCheck physicsCheck;
    public Vector2 inputDirection;
    public float speed;
    private SpriteRenderer _renderer;
    private Rigidbody2D rd;
    public float JumpF;
    [Header("蹬墙跳跃")]
    public float wallJumpHorizontalSpeed = 7f;
    public float wallJumpControlLock = 0.18f;
    public float wallSlideSpeed = 3f;
    private float wallJumpLockUntil;

    public bool isHurt;
    public float hurtForce;
    public bool isDead;
    private PlayerAnimtion playeranim;
    public bool isAttack;
    public bool isCrouching;
    private bool isWeaponUpgrading;
    private RigidbodyConstraints2D constraintsBeforeWeaponUpgrade;
    private bool weaponUpgradePositionLocked;
    public bool IsWeaponUpgrading => isWeaponUpgrading;
    public bool IsUsingSwordSkill => swordSkills != null && swordSkills.IsUsingSkill;
    private PlayerSwordSkills swordSkills;
    private Chest nearbyChest;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    [Header("梯子攀爬")]
    [SerializeField] private float ladderClimbSpeed = 2.5f;
    private Tilemap ladderTilemap;
    private float normalGravityScale;
    private Collider2D[] playerColliders;
    private readonly List<Collider2D> ignoredPlatformColliders = new List<Collider2D>();
    public bool IsClimbingDown { get; private set; }
    public bool IsClimbingLadder => IsClimbingDown;
  //  public int combo;
    //角色材质
    public PhysicsMaterial2D Nomal,Wall;
    private CapsuleCollider2D coll;
    private PlayerSlide slide;
    private bool inputCallbacksRegistered;

    private void Awake()
    {
        EnsureInputController();
        rd = GetComponent<Rigidbody2D>();
        swordSkills = GetComponent<PlayerSwordSkills>();
        _renderer = GetComponent<SpriteRenderer>();
        physicsCheck = GetComponent<PhysicsCheck>();
        playeranim = GetComponent<PlayerAnimtion>();
        coll = GetComponent<CapsuleCollider2D>();
        playerColliders = GetComponents<Collider2D>();
        normalGravityScale = rd.gravityScale;
        slide = GetComponent<PlayerSlide>();
        standingSize = coll.size;
        standingOffset = coll.offset;
    }

    private void EnsureInputController()
    {
        if (inputcontroller == null) inputcontroller = new player();
        if (inputCallbacksRegistered) return;

        inputcontroller.GamePlayer.Jump.started += Jump;
        inputcontroller.GamePlayer.Attack.started += PlayerAttack;
        inputCallbacksRegistered = true;
    }
//攻击方法
    public void AttackFromHud()
    {
        if (inputcontroller != null && inputcontroller.GamePlayer.Attack.enabled)
            PlayerAttack(default(InputAction.CallbackContext));
    }

    private void PlayerAttack(InputAction.CallbackContext obj)
    {
        if (IsUsingSwordSkill) return;
        if (isDead || isHurt || isWeaponUpgrading) return;
        if (slide != null && slide.IsSliding) return;
        if (nearbyChest != null && nearbyChest.TryOpen(this)) return;
        if (isCrouching || IsClimbingDown) return;
        playeranim.PlayerAttack();
        isAttack = true;
        // combo++;
        // if (combo==3)
        // {
        //     combo = 0;
        // }

    }

    private void Jump(InputAction.CallbackContext obj)
    {
        if (IsUsingSwordSkill || isWeaponUpgrading) return;
        if (slide != null && slide.IsSliding) return;
        if (isDead || isCrouching || isHurt || isAttack) return;
        if (IsClimbingDown) StopLadderClimb();
        physicsCheck.RefreshChecks();
        if (physicsCheck.isground)
        {
            rd.AddForce(transform.up*JumpF,ForceMode2D.Impulse);
            GetComponent<AudioDefination>()?.PlayerAudioClip();
        }
        else if (Time.time >= wallJumpLockUntil && (physicsCheck.touchLeftWall || physicsCheck.touchRighWall))
        {
            float direction = physicsCheck.touchRighWall ? -1f : 1f;
            rd.velocity = new Vector2(direction * wallJumpHorizontalSpeed, JumpF / rd.mass);
            _renderer.flipX = direction < 0;
            wallJumpLockUntil = Time.time + wallJumpControlLock;
            GetComponent<AudioDefination>()?.PlayerAudioClip();
        }
       
    }

    private void OnEnable()
    {
        EnsureInputController();
        inputcontroller.Enable();
        if (loadEvent != null) loadEvent.LoadRequesEvent += OnloadEvent;
        if (afterSceneLoadEvent != null) afterSceneLoadEvent.onEventRaised += OnAfterSceneLoadEvent;
    }




    private void OnDisable()
    {
        playeranim?.CancelWeaponUpgradeAnimation();
        CompleteWeaponUpgrade();
        nearbyChest = null;
        StopLadderClimb();
        if (inputcontroller != null) inputcontroller.Disable();
        SetCrouching(false);
        if (loadEvent != null) loadEvent.LoadRequesEvent -= OnloadEvent;
        if (afterSceneLoadEvent != null) afterSceneLoadEvent.onEventRaised -= OnAfterSceneLoadEvent;
    }

    private void OnDestroy()
    {
        if (inputcontroller == null) return;
        inputcontroller.GamePlayer.Jump.started -= Jump;
        inputcontroller.GamePlayer.Attack.started -= PlayerAttack;
        inputcontroller.Dispose();
        inputcontroller = null;
        inputCallbacksRegistered = false;
    }

    private void Update()
    {
        if (isWeaponUpgrading)
        {
            inputDirection = Vector2.zero;
            return;
        }
        if (IsUsingSwordSkill) return;
        if (slide != null && slide.IsSliding) return;
        inputDirection = inputcontroller.GamePlayer.Move.ReadValue<Vector2>();
        bool downPressed = inputcontroller.GamePlayer.Move.enabled && inputDirection.y < -0.5f;
        bool upPressed = inputcontroller.GamePlayer.Move.enabled && inputDirection.y > 0.5f;
        bool canClimb = !isDead && !isHurt && !isAttack &&
                        (slide == null || !slide.IsSliding);

        if (IsClimbingDown)
        {
            if (!canClimb || !TryGetNearbyLadder(out _))
                StopLadderClimb();
        }
        else if ((downPressed || upPressed) && canClimb && TryGetNearbyLadder(out var ladderCenter) &&
                 (downPressed ? HasLadderBelowFeet() : HasLadderAboveHead()))
        {
            StartLadderClimb(ladderCenter);
        }

        SetCrouching(!IsClimbingDown && inputcontroller.GamePlayer.Move.enabled && Keyboard.current != null && Keyboard.current.sKey.isPressed
            && physicsCheck.isground && !isDead && !isHurt && !isAttack);
        CheckState();
    }

    private void FixedUpdate()
    {
        if (isWeaponUpgrading)
        {
            // Entering the animation must also stop momentum from the previous move.
            rd.velocity = new Vector2(0f, rd.velocity.y);
            return;
        }
        if (IsUsingSwordSkill) return;
        //不是在受伤状态和攻击状态时可以移动，这里可以按需求更改
        if (!isDead&&!isHurt&&!isAttack&&!isWeaponUpgrading)
        {
            if (IsClimbingDown)
            {
                if (TryGetNearbyLadder(out var ladderCenter))
                    transform.position = new Vector3(ladderCenter.x, transform.position.y, transform.position.z);
                float verticalInput = Mathf.Abs(inputDirection.y) > 0.5f ? Mathf.Sign(inputDirection.y) : 0f;
                if (verticalInput > 0f && TryFinishLadderAscent()) return;
                if (verticalInput <= 0f && !HasLadderBelowFeet() && TryLandFromLadder()) return;
                rd.velocity = new Vector2(0f, verticalInput * ladderClimbSpeed);
            }
            else
            {
                Move();
            }
        }
       
    }
//场景加载停止角色控制
    private void OnloadEvent(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        StopLadderClimb();
        inputcontroller.GamePlayer.Disable();
    }
    //场景加载启动角色控制
    private void OnAfterSceneLoadEvent()
    {
        inputcontroller.GamePlayer.Enable();
    }
    //移动
    public void Move()
    {
        if (slide != null && slide.IsSliding) return;
        if (IsClimbingDown) return;
        if (Time.time < wallJumpLockUntil) return;
       
        if (inputDirection.x>0)
        {
            _renderer.flipX = false;

        }

        if (inputDirection.x<0)
        {
            _renderer.flipX = true;

        }

        if (isCrouching)
        {
            rd.velocity = new Vector2(0, rd.velocity.y);
            return;
        }
        rd.velocity = new Vector2(inputDirection.x * speed * Time.deltaTime * (GetComponent<PlayerRootSlow>() != null ? GetComponent<PlayerRootSlow>().Multiplier : 1f),rd.velocity.y);
        if (!physicsCheck.isground && rd.velocity.y < -wallSlideSpeed &&
            ((physicsCheck.touchLeftWall && inputDirection.x < 0) ||
             (physicsCheck.touchRighWall && inputDirection.x > 0)))
            rd.velocity = new Vector2(rd.velocity.x, -wallSlideSpeed);

    }
//受击反弹
    public void GetHurt(Transform attacker)
    {
        if (isDead) return;
        swordSkills?.CancelSkill();
        if (isWeaponUpgrading)
        {
            playeranim?.CancelWeaponUpgradeAnimation();
            CompleteWeaponUpgrade();
        }
        StopLadderClimb();
        isHurt = true;
        wallJumpLockUntil = 0;
        SetCrouching(false);
        rd.velocity = Vector2.zero;
        Vector2 dir = new Vector2(transform.position.x - attacker.position.x, 0).normalized;
        rd.AddForce(dir*hurtForce,ForceMode2D.Impulse);
    }
    
    //死亡
    public void PlayerDead()
    {
        swordSkills?.CancelSkill();
        if (slide != null) slide.EndSlide();
        if (isDead) return;
        playeranim?.CancelWeaponUpgradeAnimation();
        CompleteWeaponUpgrade();
        StopLadderClimb();
        isDead = true;
        SetCrouching(false);
        isHurt = false;
        isAttack = false;
        inputcontroller.GamePlayer.Disable();
        rd.velocity = new Vector2(0, rd.velocity.y);
        StopAllCoroutines();
        var anim = GetComponent<Animator>();
        anim.ResetTrigger("hurt");
        anim.ResetTrigger("attack");
        anim.SetBool("isDead", true);
        anim.SetBool("isAttack", false);
        anim.SetBool("isCrouching", false);
        for (int layer = 1; layer < anim.layerCount; layer++) anim.SetLayerWeight(layer, 0);
        anim.Play("Base Layer.GreenDead", 0, 0);
        anim.Update(0);
        foreach (var attack in GetComponentsInChildren<Attack>(true))
            if (attack.gameObject != gameObject) attack.gameObject.SetActive(false);
    }

    public void ResetForNewGame()
    {
        GetComponent<PlayerRootSlow>()?.Clear();
        swordSkills?.ResetSkills();
        var stamina = GetComponent<PlayerStamina>();
        if (stamina != null) stamina.ResetStamina();
        if (slide != null) slide.EndSlide();
        playeranim?.CancelWeaponUpgradeAnimation();
        CompleteWeaponUpgrade();
        nearbyChest = null;
        wallJumpLockUntil = 0;
        StopLadderClimb();
        isDead = false;
        SetCrouching(false);
        isHurt = false;
        isAttack = false;
        inputDirection = Vector2.zero;
        rd.velocity = Vector2.zero;
        var health = GetComponent<Character>();
        health.CurrentHp = health.maxHp;
        health.invulnerable = false;
        var anim = GetComponent<Animator>();
        anim.Rebind();
        anim.SetBool("isDead", false);
        anim.SetBool("isAttack", false);
        anim.ResetTrigger("hurt");
        anim.ResetTrigger("attack");
        for (int layer = 1; layer < anim.layerCount; layer++) anim.SetLayerWeight(layer, 1);
        anim.Play("Base Layer.GreenIde", 0, 0);
        anim.Update(0);
        inputcontroller.GamePlayer.Enable();
        health.OnHealthChange?.Invoke(health);
    }
    
    //切换角色材质
    private void SetCrouching(bool crouching)
    {
        if (isCrouching == crouching) return;
        isCrouching = crouching;
        float height = standingSize.y * 0.55f;
        coll.size = crouching ? new Vector2(standingSize.x, height) : standingSize;
        // Keep the collider's bottom at the same position while changing height.
        coll.offset = crouching ? standingOffset - new Vector2(0, (standingSize.y - height) * 0.5f) : standingOffset;
    }

    private bool TryGetNearbyLadder(out Vector3 ladderCenter)
    {
        ladderCenter = Vector3.zero;
        if (ladderTilemap == null || !ladderTilemap.gameObject.scene.isLoaded)
        {
            ladderTilemap = null;
            foreach (var tilemap in FindObjectsOfType<Tilemap>())
            {
                if (tilemap.name == "Forest Decorations")
                {
                    ladderTilemap = tilemap;
                    break;
                }
            }
        }

        if (ladderTilemap == null) return false;

        Vector3 foot = new Vector3(transform.position.x, coll.bounds.min.y + 0.1f, transform.position.z);
        Vector3Int footCell = ladderTilemap.WorldToCell(foot);
        for (int xOffset = -2; xOffset <= 2; xOffset++)
        {
            for (int yOffset = -4; yOffset <= 2; yOffset++)
            {
                Vector3Int cell = new Vector3Int(footCell.x + xOffset, footCell.y + yOffset, 0);
                TileBase tile = ladderTilemap.GetTile(cell);
                if (!IsLadderTile(tile)) continue;

                Vector3 center = ladderTilemap.GetCellCenterWorld(cell);
                if (Mathf.Abs(center.x - transform.position.x) > 1.1f) continue;
                ladderCenter = center;
                return true;
            }
        }

        return false;
    }

    private bool HasLadderBelowFeet()
    {
        if (!TryGetNearbyLadder(out _)) return false;
        Vector3 foot = new Vector3(transform.position.x, coll.bounds.min.y + 0.1f, transform.position.z);
        Vector3Int footCell = ladderTilemap.WorldToCell(foot);
        for (int xOffset = -2; xOffset <= 2; xOffset++)
        {
            for (int yOffset = -1; yOffset >= -5; yOffset--)
            {
                TileBase tile = ladderTilemap.GetTile(new Vector3Int(footCell.x + xOffset, footCell.y + yOffset, 0));
                if (IsLadderTile(tile)) return true;
            }
        }

        return false;
    }

    private bool HasLadderAboveHead()
    {
        if (!TryGetNearbyLadder(out _)) return false;
        Vector3 head = new Vector3(transform.position.x, coll.bounds.max.y - 0.1f, transform.position.z);
        Vector3Int headCell = ladderTilemap.WorldToCell(head);
        for (int xOffset = -2; xOffset <= 2; xOffset++)
        {
            for (int yOffset = 0; yOffset <= 5; yOffset++)
            {
                TileBase tile = ladderTilemap.GetTile(new Vector3Int(headCell.x + xOffset, headCell.y + yOffset, 0));
                if (IsLadderTile(tile)) return true;
            }
        }

        return false;
    }

    private void StartLadderClimb(Vector3 ladderCenter)
    {
        IsClimbingDown = true;
        normalGravityScale = rd.gravityScale;
        rd.gravityScale = 0f;
        rd.velocity = Vector2.zero;
        transform.position = new Vector3(ladderCenter.x, transform.position.y, transform.position.z);
        SetCrouching(false);
        SetLadderPlatformCollisionsIgnored(true);
    }

    private bool TryLandFromLadder()
    {
        // IgnoreCollision affects contacts, so query the temporarily ignored terrain
        // explicitly and restore contacts before the feet cross its upper surface.
        Bounds bounds = coll.bounds;
        float step = ladderClimbSpeed * Time.fixedDeltaTime;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.12f);
        RaycastHit2D nearest = default(RaycastHit2D);
        foreach (var platform in ignoredPlatformColliders)
        {
            if (platform == null || !platform.enabled || platform.isTrigger) continue;
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.14f + step, 1 << platform.gameObject.layer);
            foreach (var hit in hits)
            {
                if (hit.collider != platform || hit.normal.y < 0.5f) continue;
                if (nearest.collider == null || hit.distance < nearest.distance) nearest = hit;
            }
        }
        if (nearest.collider == null) return false;

        rd.position += Vector2.up * (nearest.point.y + 0.015f - bounds.min.y);
        StopLadderClimb();
        return true;
    }

    private bool TryFinishLadderAscent()
    {
        Vector3 ladderCenter;
        if (!TryGetNearbyLadder(out ladderCenter)) return false;
        Vector3Int topCell = ladderTilemap.WorldToCell(ladderCenter);
        // Find the end of this continuous ladder, rather than using the broad
        // nearby-ladder search as an exit boundary above the platform.
        int maxRows = ladderTilemap.cellBounds.size.y;
        for (int row = 0; row < maxRows; row++)
        {
            Vector3Int next = topCell + Vector3Int.up;
            if (!IsLadderTile(ladderTilemap.GetTile(next))) break;
            topCell = next;
        }
        float topY = ladderTilemap.CellToWorld(topCell + Vector3Int.up).y;
        float footY = coll.bounds.min.y;
        if (footY + ladderClimbSpeed * Time.fixedDeltaTime < topY) return false;

        rd.position += Vector2.up * (topY + 0.015f - footY);
        StopLadderClimb();
        return true;
    }

    private void StopLadderClimb()
    {
        if (!IsClimbingDown) return;
        IsClimbingDown = false;
        rd.gravityScale = normalGravityScale;
        rd.velocity = new Vector2(rd.velocity.x, 0f);
        SetLadderPlatformCollisionsIgnored(false);
        playeranim?.StopClimbDownAnimation();
    }

    private static bool IsLadderTile(TileBase tile)
    {
        if (tile == null) return false;
        string name = tile.name;
        return name.IndexOf("Ladder", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Bridge_6", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SetLadderPlatformCollisionsIgnored(bool ignored)
    {
        if (playerColliders == null) return;

        if (ignored)
        {
            ignoredPlatformColliders.Clear();
            foreach (var platform in FindObjectsOfType<Collider2D>())
            {
                if (platform == null || platform.GetComponent<TilemapCollider2D>() == null &&
                    platform.GetComponent<CompositeCollider2D>() == null) continue;
                foreach (var playerCollider in playerColliders)
                {
                    if (playerCollider == null) continue;
                    Physics2D.IgnoreCollision(playerCollider, platform, true);
                }
                ignoredPlatformColliders.Add(platform);
            }
        }
        else
        {
            foreach (var platform in ignoredPlatformColliders)
            {
                if (platform == null) continue;
                foreach (var playerCollider in playerColliders)
                    if (playerCollider != null) Physics2D.IgnoreCollision(playerCollider, platform, false);
            }
            ignoredPlatformColliders.Clear();
        }
    }

    public void SetNearbyChest(Chest chest)
    {
        nearbyChest = chest;
    }

    public void ClearNearbyChest(Chest chest)
    {
        if (nearbyChest == chest) nearbyChest = null;
    }

    public void BeginWeaponUpgrade(int damageBonus)
    {
        if (isDead || isHurt || isWeaponUpgrading) return;
        foreach (var attack in GetComponentsInChildren<Attack>(true))
            if (attack.gameObject != gameObject) attack.AddDamage(damageBonus);

        StopLadderClimb();
        SetCrouching(false);
        inputDirection = Vector2.zero;
        rd.velocity = new Vector2(0f, rd.velocity.y);
        constraintsBeforeWeaponUpgrade = rd.constraints;
        rd.constraints |= RigidbodyConstraints2D.FreezePositionX;
        weaponUpgradePositionLocked = true;
        isWeaponUpgrading = true;
        if (playeranim != null) playeranim.PlayWeaponUpgradeAnimation(CompleteWeaponUpgrade);
        else CompleteWeaponUpgrade();
    }

    private void CompleteWeaponUpgrade()
    {
        if (weaponUpgradePositionLocked && rd != null)
            rd.constraints = constraintsBeforeWeaponUpgrade;
        weaponUpgradePositionLocked = false;
        isWeaponUpgrading = false;
    }

    public void CheckState()
    {
        coll.sharedMaterial = physicsCheck.isground ? Nomal : Wall;
    }

    
    
    
}

