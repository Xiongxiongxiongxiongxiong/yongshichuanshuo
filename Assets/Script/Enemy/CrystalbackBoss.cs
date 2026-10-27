using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class CrystalbackBoss : MonoBehaviour
{
    public enum BossState { Dormant, Intro, Idle, Claw, Burrow, Shock, Rain, Exhausted, Dead, Defeated }
    public Sprite[] frames, effectFrames;
    public Attack damageSource;
    public GameObject entranceGate, exitGate;
    public CinemachineVirtualCamera arenaCamera;
    public LevelCompleteUI completionUI;
    public Transform effectRoot;
    public LayerMask groundLayer = 1 << 6;
    public float arenaLeft = -15.5f, arenaRight = 15.5f, groundY;
    public BossState State { get; private set; }
    public bool Enraged => health != null && health.CurrentHp <= health.maxHp * 0.5f;
    public bool IsFighting => State != BossState.Dormant && State != BossState.Dead && State != BossState.Defeated;
    public string Warning { get; private set; }
    private Character health;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private PlayerController player;
    private float timer, searchAt, recovery, idleDuration, walkTime;
    private int facing = -1, lastMove = -1, repeatedMoves;
    private bool fired, phase, walking;
    private Vector2 burrowStart, burrowEnd;
    private readonly HashSet<Character> hitThisAttack = new HashSet<Character>();
    private readonly List<GameObject> effects = new List<GameObject>();
    private readonly RaycastHit2D[] walkHits = new RaycastHit2D[12];

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        entranceGate.SetActive(false); exitGate.SetActive(false);
        if (arenaCamera != null) arenaCamera.Priority = -10;
        SetState(BossState.Dormant);
    }
    private void OnEnable() { health.OnDead.AddListener(Die); }
    private void OnDisable()
    {
        health.OnDead.RemoveListener(Die); StopAllCoroutines(); ClearEffects();
        if (arenaCamera != null) arenaCamera.Priority = -10;
    }
    private void SetState(BossState state)
    {
        State = state; timer = 0; fired = false; walking = false; walkTime = 0; body.velocity = Vector2.zero;
        if (state == BossState.Idle) idleDuration = Random.Range(1.4f, 2.2f);
        switch (state)
        {
            case BossState.Dormant: Warning = ""; break;
            case BossState.Intro: Warning = "矿脉吞噬者正在苏醒"; break;
            case BossState.Idle: Warning = Enraged ? "矿核过载" : ""; break;
            case BossState.Claw: Warning = "掘岩爪击：后退或滑铲躲避"; break;
            case BossState.Burrow: Warning = "钻地突袭：离开锁定的矿纹圈"; break;
            case BossState.Shock: Warning = "矿震碎岩：跳跃或登上岩台"; break;
            case BossState.Rain: Warning = "矿晶震落：离开金色落点"; break;
            case BossState.Exhausted: Warning = "矿甲松动！趁机攻击"; break;
            case BossState.Dead: Warning = "晶背掘岩兽倒下了"; break;
        }
    }
    public void BeginBattle(PlayerController target)
    {
        if (State != BossState.Dormant || target == null || target.isDead) return;
        player = target; entranceGate.SetActive(true); exitGate.SetActive(true);
        if (arenaCamera != null) arenaCamera.Priority = 50;
        FacePlayer(); SetState(BossState.Intro);
    }
    private void Update()
    {
        timer += Time.deltaTime; Animate();
        if (State == BossState.Dead)
        {
            if (timer >= 1.8f)
            {
                State = BossState.Defeated; entranceGate.SetActive(false); exitGate.SetActive(false);
                StartCoroutine(CompleteLevel());
            }
            return;
        }
        if (State == BossState.Defeated) return;
        if (player == null && Time.time >= searchAt)
        { player = FindObjectOfType<PlayerController>(); searchAt = Time.time + 0.3f; }
        if (State == BossState.Dormant)
        {
            if (player != null && !player.isDead && player.transform.position.x > arenaLeft + 2.2f && player.transform.position.x < arenaRight - 1)
                BeginBattle(player);
            return;
        }
        if (player == null || player.isDead || !player.gameObject.activeInHierarchy)
        { ClearEffects(); body.velocity = Vector2.zero; return; }
        switch (State)
        {
            case BossState.Intro: if (timer >= 1.3f) SetState(BossState.Idle); break;
            case BossState.Idle: FacePlayer(); if (timer >= idleDuration) ChooseAttack(); break;
            case BossState.Claw:
                if (timer >= 0.7f && (!fired || timer < 0.92f))
                { fired = true; DamagePlayerInBox(body.position + new Vector2(facing * 2.3f, 1), new Vector2(3, 2), 15); }
                if (timer >= 1.15f) Exhaust(0.8f);
                break;
            case BossState.Burrow:
                if (!fired && timer >= 1.2f)
                {
                    fired = true; body.position = burrowEnd; visual.enabled = true; hitbox.enabled = true;
                    SpawnEffect("Crystalback Emergence", burrowEnd, CrystalbackEffect.Kind.Burst, 0.55f);
                    DamagePlayerInBox(burrowEnd + Vector2.up * 0.65f, new Vector2(phase ? 4.8f : 4.2f, 1.3f), 20);
                }
                if (timer >= 1.75f) Exhaust(phase ? 2 : 1.7f);
                break;
            case BossState.Shock:
                if (!fired && timer >= 1f)
                { fired = true; SpawnWave(-1); SpawnWave(1); }
                if (timer >= 1.4f) Exhaust(1.4f);
                break;
            case BossState.Rain:
                if (!fired) { fired = true; SpawnRain(); }
                if (timer >= (phase ? 3.4f : 3f)) Exhaust(1.5f);
                break;
            case BossState.Exhausted: if (timer >= recovery) SetState(BossState.Idle); break;
        }
    }
    private void FacePlayer()
    { facing = player.transform.position.x >= body.position.x ? 1 : -1; visual.flipX = facing < 0; }
    private int MoveWeight(int move, float distance)
    {
        if (move == lastMove && (repeatedMoves >= 2 || move == 1)) return 0;
        switch (move)
        {
            case 0: return distance < 4 && Mathf.Abs(player.transform.position.y - groundY) < 2 ? 55 : 0;
            case 1: return distance >= 5 ? 35 : 0;
            case 2: return 25;
            default: return distance < 4 ? 20 : 40;
        }
    }
    private void ChooseAttack()
    {
        ClearEffects(); hitThisAttack.Clear(); FacePlayer(); phase = Enraged;
        float distance = Mathf.Abs(player.transform.position.x - body.position.x);
        int total = 0; for (int i = 0; i < 4; i++) total += MoveWeight(i, distance);
        int roll = Random.Range(0, total), move = 3;
        for (int i = 0; i < 4; i++) { roll -= MoveWeight(i, distance); if (roll < 0) { move = i; break; } }
        repeatedMoves = move == lastMove ? repeatedMoves + 1 : 1; lastMove = move;
        switch (move)
        {
            case 0: SetState(BossState.Claw); break;
            case 1: BeginBurrow(); break;
            case 2: SetState(BossState.Shock); break;
            default: SetState(BossState.Rain); break;
        }
    }
    private void BeginBurrow()
    {
        burrowStart = body.position;
        burrowEnd = new Vector2(Mathf.Clamp(player.transform.position.x, arenaLeft + 3, arenaRight - 3), groundY + 0.02f);
        SetState(BossState.Burrow); hitbox.enabled = false;
        var marker = SpawnEffect("Burrow Target Warning", burrowEnd, CrystalbackEffect.Kind.Warning, 1.2f);
        marker.transform.localScale = new Vector3(phase ? 1.6f : 1.4f, 1, 1);
    }
    private void FixedUpdate()
    {
        walking = false;
        if (body.simulated) body.velocity = Vector2.zero;
        if (!body.simulated || player == null || player.isDead || !player.gameObject.activeInHierarchy) return;
        if (State == BossState.Burrow && timer >= 0.35f && timer < 1.2f)
            body.MovePosition(Vector2.Lerp(burrowStart, burrowEnd, Mathf.Clamp01((timer - 0.35f) / 0.85f)));
        else if (State == BossState.Idle)
        {
            float delta = player.transform.position.x - body.position.x;
            int direction = delta >= 0 ? 1 : -1;
            float halfWidth = hitbox.bounds.extents.x;
            float step = Mathf.Min(Mathf.Max(0, Mathf.Abs(delta) - 3.2f), 1.7f * Time.fixedDeltaTime);
            float targetX = Mathf.Clamp(body.position.x + direction * step, arenaLeft + halfWidth + 0.15f, arenaRight - halfWidth - 0.15f);
            step = Mathf.Abs(targetX - body.position.x);
            int count = Physics2D.BoxCastNonAlloc(hitbox.bounds.center, hitbox.bounds.size, 0, Vector2.right * direction, walkHits, step + 0.05f, groundLayer);
            for (int i = 0; i < count; i++)
            {
                var wall = walkHits[i];
                if (wall.distance <= 0.001f && direction * (hitbox.bounds.center.x - wall.collider.bounds.center.x) >= 0) continue;
                step = Mathf.Min(step, Mathf.Max(0, wall.distance - 0.05f));
            }
            float nextX = body.position.x + direction * step;
            var floor = Physics2D.Raycast(new Vector2(nextX + direction * halfWidth, groundY + 0.3f), Vector2.down, 0.8f, groundLayer);
            if (step > 0.001f && floor.collider != null)
            {
                body.MovePosition(new Vector2(nextX, body.position.y)); walking = true;
                walkTime += Time.fixedDeltaTime * 8; facing = direction; visual.flipX = facing < 0;
            }
        }
        if (!walking) walkTime = 0;
    }
    private void SpawnWave(int direction)
    {
        var wave = SpawnEffect("Mine Rock Wave", body.position + new Vector2(direction * 2.2f, 0), CrystalbackEffect.Kind.Wave, 5);
        wave.velocity = Vector2.right * direction * (phase ? 7 : 5.5f);
    }
    private void SpawnRain()
    {
        int count = phase ? 5 : 3;
        float half = (count - 1) * 1.5f;
        float center = Mathf.Clamp(player.transform.position.x, arenaLeft + 2 + half, arenaRight - 2 - half);
        for (int i = 0; i < count; i++)
        {
            float x = center + i * 3 - half;
            var floor = Physics2D.Raycast(new Vector2(x, groundY + 7), Vector2.down, 8, groundLayer);
            float y = floor.collider != null ? floor.point.y : groundY;
            float delay = 1.1f + i * 0.15f;
            SpawnEffect("Mine Crystal Warning", new Vector2(x, y + 0.03f), CrystalbackEffect.Kind.Warning, delay + 0.95f);
            var crystal = SpawnEffect("Falling Mine Crystal", new Vector2(x, y + 8), CrystalbackEffect.Kind.Crystal, delay + 2.2f);
            crystal.delay = delay; crystal.velocity = Vector2.down * 10;
        }
    }
    private CrystalbackEffect SpawnEffect(string label, Vector2 position, CrystalbackEffect.Kind kind, float lifetime)
    {
        var go = new GameObject(label); go.transform.SetParent(effectRoot, true); go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 2;
        var effect = go.AddComponent<CrystalbackEffect>(); effect.owner = this; effect.frames = effectFrames;
        effect.kind = kind; effect.lifetime = lifetime; effects.Add(go); return effect;
    }
    public void DamagePlayerInBox(Vector2 center, Vector2 size, int damage)
    {
        if (!IsFighting || player == null || player.isDead) return;
        var target = player.GetComponent<Character>();
        if (target.invulnerable || hitThisAttack.Contains(target) || player.GetComponent<PlayerSlide>()?.IsSliding == true) return;
        var capsule = player.GetComponent<CapsuleCollider2D>();
        if (capsule == null || !capsule.enabled || !capsule.bounds.Intersects(new Bounds(center, size))) return;
        float before = target.CurrentHp; damageSource.damage = damage; target.TakeDamage(damageSource);
        if (target.CurrentHp < before) hitThisAttack.Add(target);
    }
    private void Animate()
    {
        if (frames == null || frames.Length < 32) return;
        visual.enabled = !(State == BossState.Burrow && timer >= 0.35f && timer < 1.2f);
        visual.color = State == BossState.Dead || State == BossState.Defeated ? new Color(0.55f, 0.55f, 0.6f) : Color.white;
        int row = 0, frame = (int)(timer * 6) % 4;
        switch (State)
        {
            case BossState.Idle: if (walking) { row = 1; frame = (int)walkTime % 4; } break;
            case BossState.Intro: case BossState.Rain: row = 6; break;
            case BossState.Claw: row = 2; frame = timer < 0.7f ? 0 : timer < 0.8f ? 1 : timer < 0.95f ? 2 : 3; break;
            case BossState.Burrow:
                row = timer < 1.2f ? 3 : 4;
                frame = timer < 1.2f ? Mathf.Min(3, (int)(timer / 0.35f * 4)) : Mathf.Min(3, (int)((timer - 1.2f) / 0.55f * 4));
                break;
            case BossState.Shock: row = 5; frame = timer < 1 ? Mathf.Min(1, (int)(timer * 2)) : timer < 1.2f ? 2 : 3; break;
            case BossState.Dead: case BossState.Defeated: row = 7; frame = Mathf.Min(3, (int)(timer * 3)); break;
        }
        visual.sprite = frames[row * 4 + frame];
    }
    private void Exhaust(float seconds) { recovery = seconds; SetState(BossState.Exhausted); }
    private void Die()
    {
        if (State == BossState.Dead || State == BossState.Defeated) return;
        StopAllCoroutines(); ClearEffects(); SetState(BossState.Dead);
        visual.enabled = true; body.simulated = false; hitbox.enabled = false;
    }
    private IEnumerator CompleteLevel()
    { yield return new WaitForSeconds(0.65f); if (player != null && !player.isDead && completionUI != null) completionUI.Show(); }
    private void ClearEffects()
    { foreach (var go in effects) if (go != null) Destroy(go); effects.Clear(); }
}
