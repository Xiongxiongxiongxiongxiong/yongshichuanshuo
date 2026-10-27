using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class MossTuskBoss : MonoBehaviour
{
    public enum BossState { Dormant, Intro, Idle, ChargeWindup, Charge, StompWindup, Stomp, Summon, Stunned, Dead, Defeated }
    public Sprite[] frames;
    public Sprite[] effectFrames;
    public Attack damageSource;
    public GameObject entranceGate, exitGate;
    public CinemachineVirtualCamera arenaCamera;
    public LevelGoal exitGoal;
    public LevelCompleteUI completionUI;
    public Transform effectRoot;
    public LayerMask groundLayer = 1 << 6;
    public float arenaLeft = 54, arenaRight = 85, groundY = 10;
    public BossState State { get; private set; }
    public bool Enraged => health != null && health.CurrentHp <= health.maxHp * 0.5f;
    public bool IsFighting => State != BossState.Dormant && State != BossState.Dead && State != BossState.Defeated;
    public string Warning { get; private set; }
    private Character health;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private PlayerController player;
    private float timer, searchAt;
    private int facing = -1, moveIndex, chargesLeft;
    private bool fired;
    private readonly HashSet<Character> hitThisAttack = new HashSet<Character>();
    private readonly List<GameObject> effects = new List<GameObject>();

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        if (entranceGate != null) entranceGate.SetActive(false);
        if (exitGate != null) exitGate.SetActive(false);
        if (exitGoal != null) exitGoal.GetComponent<Collider2D>().enabled = false;
        if (arenaCamera != null) arenaCamera.Priority = -10;
        SetState(BossState.Dormant);
    }
    private void OnEnable()
    {
        health.OnDead.AddListener(Die);
    }
    private void OnDisable()
    {
        health.OnDead.RemoveListener(Die);
        StopAllCoroutines(); ClearEffects();
        if (arenaCamera != null) arenaCamera.Priority = -10;
    }
    private void SetState(BossState state)
    {
        State = state; timer = 0; fired = false;
        body.velocity = new Vector2(0, body.velocity.y);
        switch (state)
        {
            case BossState.Dormant: Warning = ""; break;
            case BossState.Intro: Warning = "古林守卫已苏醒"; break;
            case BossState.Idle: Warning = Enraged ? "狂暴阶段" : ""; break;
            case BossState.ChargeWindup: Warning = Enraged ? "连续冲锋：跳跃或滑铲躲避" : "獠牙冲锋：跳跃或滑铲躲避"; break;
            case BossState.StompWindup: Warning = "裂地重踏：跳跃躲避冲击波"; break;
            case BossState.Summon: Warning = "苔岩震落：离开金色落点"; break;
            case BossState.Stunned: Warning = "撞墙眩晕！趁机攻击"; break;
            case BossState.Dead: Warning = "古林守卫倒下了"; break;
        }
    }
    public void BeginBattle(PlayerController target)
    {
        if (State != BossState.Dormant || target == null || target.isDead) return;
        player = target;
        if (entranceGate != null) entranceGate.SetActive(true);
        if (exitGate != null) exitGate.SetActive(true);
        if (arenaCamera != null) arenaCamera.Priority = 50;
        FacePlayer(); SetState(BossState.Intro);
    }
    private void FacePlayer()
    {
        if (player != null) facing = player.transform.position.x >= body.position.x ? 1 : -1;
        visual.flipX = facing < 0;
    }
    private void Update()
    {
        timer += Time.deltaTime;
        Animate();
        if (State == BossState.Dead)
        {
            if (timer >= 1.8f)
            {
                State = BossState.Defeated;
                if (entranceGate != null) entranceGate.SetActive(false);
                if (exitGate != null) exitGate.SetActive(false);
                if (exitGoal != null) exitGoal.GetComponent<Collider2D>().enabled = true;
                StartCoroutine(CompleteLevel());
            }
            return;
        }
        if (State == BossState.Defeated) return;
        if (player == null && Time.time >= searchAt)
        { player = FindObjectOfType<PlayerController>(); searchAt = Time.time + 0.3f; }
        if (State == BossState.Dormant)
        {
            if (player != null && !player.isDead && player.transform.position.x > arenaLeft + 2.2f &&
                player.transform.position.x < arenaRight - 1f && player.transform.position.y > groundY - 0.5f)
                BeginBattle(player);
            return;
        }
        if (player == null || player.isDead || !player.gameObject.activeInHierarchy)
        { body.velocity = new Vector2(0, body.velocity.y); ClearEffects(); return; }
        switch (State)
        {
            case BossState.Intro: if (timer >= 1.3f) SetState(BossState.Idle); break;
            case BossState.Idle:
                FacePlayer();
                if (timer >= (Enraged ? 0.85f : 1.25f)) ChooseAttack();
                break;
            case BossState.ChargeWindup:
                if (timer >= (Enraged ? 0.6f : 0.8f)) SetState(BossState.Charge);
                break;
            case BossState.Charge:
                DamagePlayerInBox(hitbox.bounds.center, hitbox.bounds.size + new Vector3(0.15f, 0, 0), 20);
                if (AtWall() || timer >= 4.5f) FinishCharge();
                break;
            case BossState.StompWindup:
                if (timer >= 0.85f) SetState(BossState.Stomp);
                break;
            case BossState.Stomp:
                if (!fired && timer >= 0.18f)
                { fired = true; SpawnWave(-1); SpawnWave(1); }
                if (timer >= 1.05f) SetState(BossState.Idle);
                break;
            case BossState.Summon:
                if (!fired && timer >= 0.45f) { fired = true; StartCoroutine(RainRocks()); }
                if (timer >= 3f) SetState(BossState.Idle);
                break;
            case BossState.Stunned:
                if (timer >= (Enraged ? 1.9f : 1.5f)) SetState(BossState.Idle);
                break;
        }
    }
    private void ChooseAttack()
    {
        hitThisAttack.Clear(); FacePlayer();
        switch (moveIndex++ % 3)
        {
            case 0: chargesLeft = Enraged ? 2 : 1; SetState(BossState.ChargeWindup); break;
            case 1: SetState(BossState.StompWindup); break;
            default: SetState(BossState.Summon); break;
        }
    }
    private void FixedUpdate()
    {
        if (!body.simulated) return;
        float velocity = 0;
        if (player != null && !player.isDead)
        {
            if (State == BossState.Charge) velocity = facing * (Enraged ? 12 : 10);
            else if (State == BossState.Idle && Mathf.Abs(player.transform.position.x - body.position.x) > 8)
                velocity = facing * 1.2f;
        }
        body.velocity = new Vector2(velocity, body.velocity.y);
    }
    private bool AtWall()
    {
        float edge = body.position.x + facing * hitbox.bounds.extents.x;
        if (edge <= arenaLeft + 0.65f || edge >= arenaRight - 0.65f) return true;
        RaycastHit2D hit = Physics2D.Raycast(hitbox.bounds.center, Vector2.right * facing,
            hitbox.bounds.extents.x + 0.25f, groundLayer);
        return hit.collider != null && hit.normal.x * facing < -0.5f;
    }
    private void FinishCharge()
    {
        if (--chargesLeft > 0) { FacePlayer(); SetState(BossState.ChargeWindup); }
        else SetState(BossState.Stunned);
    }
    public void DamagePlayerInBox(Vector2 center, Vector2 size, int damage)
    {
        if (!IsFighting) return;
        foreach (var col in Physics2D.OverlapBoxAll(center, size, 0, 1 << 7))
        {
            var p = col.GetComponentInParent<PlayerController>();
            if (p == null || p.isDead || p.GetComponent<PlayerSlide>()?.IsSliding == true) continue;
            if (col != p.GetComponent<CapsuleCollider2D>()) continue;
            var target = p.GetComponent<Character>();
            if (hitThisAttack.Contains(target) || target.invulnerable) continue;
            float before = target.CurrentHp;
            damageSource.damage = damage; target.TakeDamage(damageSource);
            if (target.CurrentHp < before) hitThisAttack.Add(target);
        }
    }
    private GameObject MakeEffect(string label, Vector2 position)
    {
        var go = new GameObject(label); go.transform.SetParent(effectRoot, true); go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 2;
        var effect = go.AddComponent<MossBossEffect>(); effect.frames = effectFrames;
        effects.Add(go); return go;
    }
    private void SpawnWave(int direction)
    {
        var go = MakeEffect("Boss Ground Shockwave", new Vector2(body.position.x + direction * 1.9f, groundY + 0.08f));
        var effect = go.GetComponent<MossBossEffect>(); effect.owner = this; effect.kind = MossBossEffect.Kind.Shockwave;
        effect.direction = direction; effect.lifetime = 4.5f;
    }
    private IEnumerator RainRocks()
    {
        if (player == null) yield break;
        float targetX = player.transform.position.x;
        for (int i = 0; i < 3; i++)
        {
            float x = Mathf.Clamp(targetX + (i - 1) * 3.2f, arenaLeft + 1.5f, arenaRight - 1.5f);
            var surface = Physics2D.Raycast(new Vector2(x, groundY + 20), Vector2.down, 40, groundLayer);
            float surfaceY = surface.collider != null ? surface.point.y : groundY;
            var warning = MakeEffect("Boss Rock Warning", new Vector2(x, surfaceY + 0.06f)).GetComponent<MossBossEffect>();
            warning.kind = MossBossEffect.Kind.Warning; warning.lifetime = 1.8f;
            var rock = MakeEffect("Boss Falling Rock", new Vector2(x, surfaceY + 10)).GetComponent<MossBossEffect>();
            rock.kind = MossBossEffect.Kind.FallingRock; rock.owner = this; rock.delay = 0.95f; rock.lifetime = 4;
            rock.impactGroundY = surfaceY;
            yield return new WaitForSeconds(0.22f);
        }
    }
    private void Animate()
    {
        if (frames == null || frames.Length < 32) return;
        int row = 0; bool loop = true;
        switch (State)
        {
            case BossState.Idle: if (Mathf.Abs(body.velocity.x) > 0.1f) row = 1; break;
            case BossState.ChargeWindup: row = 2; loop = false; break;
            case BossState.Charge: row = 3; break;
            case BossState.StompWindup: row = 4; loop = false; break;
            case BossState.Stomp: row = 4; loop = false; break;
            case BossState.Intro: case BossState.Summon: row = 5; break;
            case BossState.Stunned: row = 6; break;
            case BossState.Dead: case BossState.Defeated: row = 7; loop = false; break;
        }
        int frame;
        if (State == BossState.StompWindup) frame = Mathf.Min(1, Mathf.FloorToInt(timer * 3));
        else if (State == BossState.Stomp) frame = Mathf.Min(3, 2 + Mathf.FloorToInt(timer * 3));
        else { int step = Mathf.FloorToInt(timer * (row == 7 ? 3 : 6)); frame = loop ? step % 4 : Mathf.Min(3, step); }
        visual.sprite = frames[row * 4 + frame];
    }
    private void Die()
    {
        if (State == BossState.Dead || State == BossState.Defeated) return;
        StopAllCoroutines(); ClearEffects(); SetState(BossState.Dead);
        body.velocity = Vector2.zero; body.simulated = false; hitbox.enabled = false;
    }
    private IEnumerator CompleteLevel()
    {
        yield return new WaitForSeconds(0.65f);
        if (player != null && !player.isDead && completionUI != null) completionUI.Show();
    }
    private void ClearEffects()
    {
        foreach (var go in effects) if (go != null) Destroy(go);
        effects.Clear();
    }
}
