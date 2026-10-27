using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class SporeCrownBoss : MonoBehaviour
{
    public enum BossState { Dormant, Intro, Idle, Roots, Spores, Leap, Exhausted, Dead, Defeated, Melee }
    public Sprite[] frames, effectFrames;
    public Sprite[] meleeFrames;
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
    private float timer, searchAt, recovery, idleDuration, walkFrameTime;
    private bool walking;
    private const float WalkSpeed = 1.8f, WalkStopDistance = 3f;
    private int lastMove = -1, repeatedMoves, facing = -1, sporeRounds;
    private bool fired, attackEnraged, retreatLeap;
    private const float SporeWindup = 1.5f;
    private const float RetreatDistance = 4f, AttackLeapDistance = 6f;
    private Vector2 leapStart, leapEnd;
    private readonly HashSet<Character> hitThisAttack = new HashSet<Character>();
    private readonly List<GameObject> effects = new List<GameObject>();
    private readonly RaycastHit2D[] walkHits = new RaycastHit2D[12];

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        if (entranceGate != null) entranceGate.SetActive(false);
        if (exitGate != null) exitGate.SetActive(false);
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
        State = state; timer = 0; fired = false; walking = false; walkFrameTime = 0; body.velocity = Vector2.zero;
        if (state == BossState.Idle)
            idleDuration = player != null && Mathf.Abs(player.transform.position.x - body.position.x) > RetreatDistance
                ? Random.Range(1.8f, 2.6f) : Random.Range(0.9f, 1.4f);
        switch (state)
        {
            case BossState.Dormant: Warning = ""; break;
            case BossState.Intro: Warning = "幽谷菌主已苏醒"; break;
            case BossState.Idle: Warning = Enraged ? "盛放阶段" : ""; break;
            case BossState.Roots: Warning = "藤根突刺：离开金色标记"; break;
            case BossState.Spores: Warning = attackEnraged ? "双重孢子：寻找弹道间隙" : "孢子散射：跳跃或滑铲接近"; break;
            case BossState.Leap: Warning = retreatLeap ? "菌主后撤：跳跃拉开距离" : "菌冠跃砸：离开金色落点"; break;
            case BossState.Melee: Warning = "根臂横扫：后退或滑铲躲避"; break;
            case BossState.Exhausted: Warning = "菌冠低垂！趁机攻击"; break;
            case BossState.Dead: Warning = "幽谷菌主倒下了"; break;
        }
    }
    public void BeginBattle(PlayerController target)
    {
        if (State != BossState.Dormant || target == null || target.isDead) return;
        player = target;
        entranceGate.SetActive(true); exitGate.SetActive(true);
        if (arenaCamera != null) arenaCamera.Priority = 50;
        SetState(BossState.Intro);
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
            case BossState.Idle:
                FacePlayer();
                if (timer >= idleDuration) ChooseAttack();
                break;
            case BossState.Roots:
                if (timer >= 1.65f) Exhaust(1.25f);
                break;
            case BossState.Melee:
                if (timer >= 0.6f && (!fired || timer < 0.82f))
                {
                    fired = true;
                    DamagePlayerInBox(body.position + new Vector2(facing * 2.1f, 1f), new Vector2(2.8f, 2f), 15);
                }
                if (timer >= 1.1f) Exhaust(0.6f);
                break;
            case BossState.Spores:
                if (sporeRounds == 0 && timer >= SporeWindup) { FireSpores(); sporeRounds++; }
                if (attackEnraged && sporeRounds == 1 && timer >= SporeWindup + 0.65f) { FireSpores(); sporeRounds++; }
                if (timer >= SporeWindup + (attackEnraged ? 0.85f : 0.2f)) Exhaust(1.5f);
                break;
            case BossState.Leap:
                if (!fired && timer >= 1.1f)
                {
                    fired = true; body.position = leapEnd;
                    hitbox.enabled = true;
                    if (!retreatLeap)
                    {
                        DamagePlayerInBox(leapEnd + Vector2.up * 0.7f, new Vector2(attackEnraged ? 5 : 4.2f, 1.4f), 20);
                        SpawnEffect("Crown Landing", leapEnd, SporeCrownEffect.Kind.Burst, 0.5f);
                    }
                    Exhaust(retreatLeap ? 1 : attackEnraged ? 2 : 1.8f);
                }
                break;
            case BossState.Exhausted: if (timer >= recovery) SetState(BossState.Idle); break;
        }
    }
    private void FacePlayer()
    {
        facing = player.transform.position.x >= body.position.x ? 1 : -1;
        visual.flipX = facing < 0;
    }
    private void ChooseAttack()
    {
        // Snapshot the phase so half-health cannot change an active warning or hitbox.
        ClearEffects(); hitThisAttack.Clear(); FacePlayer(); attackEnraged = Enraged;
        float distance = Mathf.Abs(player.transform.position.x - body.position.x);
        int move = SelectMove(distance);
        repeatedMoves = move == lastMove ? repeatedMoves + 1 : 1;
        lastMove = move;
        switch (move)
        {
            case 0:
                SetState(BossState.Roots); SpawnRoots(); break;
            case 1:
                sporeRounds = 0; SetState(BossState.Spores); break;
            case 2:
                BeginLeap(false);
                break;
            case 3:
                SetState(BossState.Melee);
                break;
            case 4:
                BeginLeap(true);
                break;
        }
    }
    private int MoveWeight(int move, float distance)
    {
        if (move == lastMove && (repeatedMoves >= 2 || move == 4)) return 0;
        bool close = distance < RetreatDistance;
        switch (move)
        {
            case 0: return close ? 20 : 40;
            case 1: return close ? 15 : 40;
            case 2: return distance >= AttackLeapDistance ? 20 : 0;
            case 3: return close && Mathf.Abs(player.transform.position.y - body.position.y) < 2 ? 50 : 0;
            case 4: return close ? 15 : 0;
            default: return 0;
        }
    }
    private int SelectMove(float distance)
    {
        // Near the boss, a retreat is a low-weight option, never a mandatory response.
        int total = 0;
        for (int move = 0; move < 5; move++) total += MoveWeight(move, distance);
        int roll = Random.Range(0, total);
        for (int move = 0; move < 5; move++)
        {
            roll -= MoveWeight(move, distance);
            if (roll < 0) return move;
        }
        return 0;
    }
    private void BeginLeap(bool retreat)
    {
        retreatLeap = retreat; leapStart = body.position;
        float targetX = player.transform.position.x;
        if (retreat)
        {
            float left = Mathf.Clamp(targetX - 8, arenaLeft + 3, arenaRight - 3);
            float right = Mathf.Clamp(targetX + 8, arenaLeft + 3, arenaRight - 3);
            // Prefer moving away from the player; switch sides if the wall leaves too little room.
            bool goLeft = body.position.x < targetX;
            targetX = goLeft ? left : right;
            if (Mathf.Abs(targetX - player.transform.position.x) < AttackLeapDistance)
                targetX = goLeft ? right : left;
        }
        leapEnd = new Vector2(Mathf.Clamp(targetX, arenaLeft + 3, arenaRight - 3), groundY + 0.02f);
        SetState(BossState.Leap); hitbox.enabled = false;
        if (!retreat)
        {
            var marker = SpawnEffect("Crown Landing Warning", leapEnd, SporeCrownEffect.Kind.Warning, 1.1f);
            marker.transform.localScale = new Vector3(attackEnraged ? 1.65f : 1.4f, 1, 1);
        }
    }
    private void FixedUpdate()
    {
        walking = false;
        if (body.simulated) body.velocity = Vector2.zero;
        if (!body.simulated || player == null || player.isDead || !player.gameObject.activeInHierarchy) return;
        if (State == BossState.Leap)
        {
            float t = Mathf.Clamp01((timer - 0.3f) / 0.8f);
            body.MovePosition(Vector2.Lerp(leapStart, leapEnd, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 4);
        }
        else if (State == BossState.Idle)
        {
            float delta = player.transform.position.x - body.position.x;
            float step = Mathf.Min(Mathf.Max(0, Mathf.Abs(delta) - WalkStopDistance), WalkSpeed * Time.fixedDeltaTime);
            int direction = delta >= 0 ? 1 : -1;
            float halfWidth = hitbox.bounds.extents.x;
            float targetX = Mathf.Clamp(body.position.x + direction * step,
                arenaLeft + halfWidth + 0.15f, arenaRight - halfWidth - 0.15f);
            step = Mathf.Abs(targetX - body.position.x);
            // Kinematic movement must explicitly stop at platform sides and arena terrain.
            int count = Physics2D.BoxCastNonAlloc(hitbox.bounds.center, hitbox.bounds.size, 0,
                Vector2.right * direction, walkHits, step + 0.05f, groundLayer);
            for (int i = 0; i < count; i++)
            {
                var wall = walkHits[i];
                // A leap may end partly under a platform; allow walking out of that overlap.
                if (wall.distance <= 0.001f && direction * (hitbox.bounds.center.x - wall.collider.bounds.center.x) >= 0) continue;
                step = Mathf.Min(step, Mathf.Max(0, wall.distance - 0.05f));
            }
            float nextX = body.position.x + direction * step;
            var floor = Physics2D.Raycast(new Vector2(nextX + direction * halfWidth, groundY + 0.3f),
                Vector2.down, 0.8f, groundLayer);
            if (step > 0.001f && floor.collider != null)
            {
                body.MovePosition(new Vector2(nextX, body.position.y));
                walking = true; walkFrameTime += Time.fixedDeltaTime * 8;
                facing = direction; visual.flipX = facing < 0;
            }
        }
        if (!walking) walkFrameTime = 0;
    }
    private void Exhaust(float seconds) { recovery = seconds; SetState(BossState.Exhausted); }
    private void SpawnRoots()
    {
        int count = attackEnraged ? 5 : 3;
        // Move the entire pattern inside the arena, avoiding overlapping clamped roots at edges.
        float half = (count - 1) * 1.6f;
        float center = Mathf.Clamp(player.transform.position.x, arenaLeft + 2 + half, arenaRight - 2 - half);
        for (int i = 0; i < count; i++)
        {
            float x = center + i * 3.2f - half;
            // Only the arena floor may spawn roots. Elevated platforms are cover, never spawn surfaces.
            var surface = Physics2D.Raycast(new Vector2(x, groundY + 0.25f), Vector2.down, 0.75f, groundLayer);
            if (surface.collider == null) continue;
            float y = surface.point.y;
            var ceiling = Physics2D.BoxCast(new Vector2(x, y + 0.03f), new Vector2(1.3f, 0.02f),
                0, Vector2.up, 1.65f, groundLayer);
            float height = ceiling.collider != null ? Mathf.Max(0, ceiling.point.y - y - 0.08f) : 1.6f;
            if (height <= 0.1f) continue;
            SpawnEffect("Root Warning", new Vector2(x, y + 0.03f), SporeCrownEffect.Kind.Warning, 0.95f);
            var root = SpawnEffect("Crown Root Spike", new Vector2(x, y), SporeCrownEffect.Kind.Root, 1.65f);
            root.delay = 0.95f;
            root.rootHeight = Mathf.Min(1.6f, height);
        }
    }
    private void FireSpores()
    {
        for (int i = -1; i <= 1; i++)
        {
            var spore = SpawnEffect("Crown Spore", body.position + new Vector2(facing * 2, 1.6f), SporeCrownEffect.Kind.Spore, 3);
            spore.velocity = new Vector2(facing * 4.5f, i * 1.25f);
        }
    }
    private SporeCrownEffect SpawnEffect(string label, Vector2 position, SporeCrownEffect.Kind kind, float lifetime)
    {
        var go = new GameObject(label); go.transform.SetParent(effectRoot, true); go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 2;
        var effect = go.AddComponent<SporeCrownEffect>(); effect.owner = this; effect.frames = effectFrames;
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
        if (State == BossState.Melee && meleeFrames != null && meleeFrames.Length >= 8)
        {
            int index = timer < 0.6f ? Mathf.Min(2, (int)(timer / 0.6f * 3)) :
                timer < 0.82f ? 3 + Mathf.Min(2, (int)((timer - 0.6f) / 0.22f * 3)) :
                6 + Mathf.Min(1, (int)((timer - 0.82f) / 0.28f * 2));
            visual.sprite = meleeFrames[index];
            return;
        }
        if (frames == null || frames.Length < 32) return;
        int row = 0, frame = (int)(timer * 6) % 4;
        switch (State)
        {
            case BossState.Idle:
                if (walking) { row = 1; frame = (int)walkFrameTime % 4; }
                break;
            case BossState.Intro: row = 5; break;
            case BossState.Roots: row = 2; frame = Mathf.Min(3, (int)(timer * 3)); break;
            case BossState.Spores:
                row = 3;
                if (timer < SporeWindup) frame = Mathf.Min(1, (int)(timer / SporeWindup * 2));
                else if (attackEnraged && timer >= SporeWindup + 0.2f && timer < SporeWindup + 0.65f) frame = 1;
                else frame = Mathf.Min(3, 2 + (int)((timer - SporeWindup - (sporeRounds >= 2 ? 0.65f : 0)) * 8));
                break;
            case BossState.Leap: row = 4; frame = timer < 0.3f ? 0 : timer < 0.75f ? 1 : 2; break;
            case BossState.Exhausted: row = 6; break;
            case BossState.Dead: case BossState.Defeated: row = 7; frame = Mathf.Min(3, (int)(timer * 3)); break;
        }
        visual.sprite = frames[row * 4 + Mathf.Clamp(frame, 0, 3)];
    }
    private void Die()
    {
        if (State == BossState.Dead || State == BossState.Defeated) return;
        StopAllCoroutines(); ClearEffects(); SetState(BossState.Dead);
        body.simulated = false; hitbox.enabled = false;
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
