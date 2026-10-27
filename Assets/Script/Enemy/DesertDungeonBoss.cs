using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

// Four authored encounters share lifecycle, hit accounting and arena movement.
[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class DesertDungeonBoss : MonoBehaviour
{
    public enum Kind { SunburialScorpion, SteleLizard, IroncoffinJailer, SoulLantern }
    public enum Mode { Dormant, Intro, Idle, Close, Heavy, Mobility, Special, Exhausted, Dead, Defeated }
    public Kind kind;
    public Sprite[] frames, effectFrames;
    public Attack damageSource;
    public GameObject entranceGate, exitGate;
    public CinemachineVirtualCamera arenaCamera;
    public LevelCompleteUI completionUI;
    public Transform effectRoot;
    public LayerMask groundLayer = 1 << 6;
    public float arenaLeft = -15.5f, arenaRight = 15.5f, groundY;
    public Mode State { get; private set; }
    public bool Enraged => health != null && health.CurrentHp <= health.maxHp * .5f;
    public bool IsFighting => State != Mode.Dormant && State != Mode.Dead && State != Mode.Defeated;
    private static readonly string[] Titles = { "葬日沙蝎 · 赤钳暴君", "负碑沙蜥 · 沉陵守望者", "缚刑狱长 · 铁棺", "无面司灯 · 囚魂者" };
    public string Title => Titles[(int)kind];
    public string Warning { get; private set; }
    public PlayerController Target => player;
    private Character health, playerHealth;
    private Rigidbody2D body, playerBody;
    private BoxCollider2D hitbox;
    private CapsuleCollider2D playerBox;
    private PlayerSlide slide;
    private PlayerRootSlow slow;
    private SpriteRenderer visual;
    private PlayerController player;
    private float timer, searchAt, recovery, idleDuration, walkTime;
    private int facing = -1, lastMove = -1, repeatedMoves, stage, attackId;
    private bool phase, walking, combo;
    private Vector2 origin, destination;
    private readonly HashSet<int> landed = new HashSet<int>();
    private readonly List<GameObject> effects = new List<GameObject>();
    private readonly RaycastHit2D[] moveHits = new RaycastHit2D[16];

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        entranceGate.SetActive(false); exitGate.SetActive(false);
        if (arenaCamera != null) arenaCamera.Priority = -10;
        SetState(Mode.Dormant);
    }
    private void OnEnable() { health.OnDead.AddListener(Die); }
    private void OnDisable()
    {
        health.OnDead.RemoveListener(Die); StopAllCoroutines(); ClearEffects();
        if (arenaCamera != null) arenaCamera.Priority = -10;
    }
    private void BindPlayer(PlayerController target)
    {
        player = target; playerHealth = target.GetComponent<Character>(); playerBody = target.GetComponent<Rigidbody2D>();
        playerBox = target.GetComponent<CapsuleCollider2D>(); slide = target.GetComponent<PlayerSlide>();
        slow = target.GetComponent<PlayerRootSlow>();
        if (slow == null) slow = target.gameObject.AddComponent<PlayerRootSlow>();
    }
    public void BeginBattle(PlayerController target)
    {
        if (State != Mode.Dormant || target == null || target.isDead) return;
        BindPlayer(target); entranceGate.SetActive(true); exitGate.SetActive(true);
        if (arenaCamera != null) arenaCamera.Priority = 50;
        FacePlayer(); SetState(Mode.Intro);
    }
    private void SetState(Mode state)
    {
        State = state; timer = 0; stage = 0; walking = false; walkTime = 0; body.velocity = Vector2.zero;
        if (state != Mode.Mobility) body.position = new Vector2(body.position.x, groundY + .02f);
        hitbox.enabled = state != Mode.Dead && state != Mode.Defeated;
        visual.enabled = true;
        if (state == Mode.Idle) idleDuration = Random.Range(1.3f, 2.15f);
        Warning = state == Mode.Intro ? Title + "苏醒" : state == Mode.Exhausted ? "破绽出现！趁机攻击" : "";
        if (state == Mode.Close) Warning = kind == Kind.SteleLizard ? "裂颚噬咬：离开头部" : kind == Kind.SoulLantern ? "幽爪探身：后退或绕后" : "近身横砸：后退或绕后";
        if (state == Mode.Heavy) Warning = kind == Kind.SunburialScorpion ? "贯沙尾刺：离开落点" : kind == Kind.SoulLantern ? "囚魂巡游：寻找魂火间隙" : "重击蓄势：跳跃或登上石台";
        if (state == Mode.Mobility) Warning = kind == Kind.SteleLizard ? "埋沙突进：离开裂纹" : kind == Kind.SoulLantern ? "点名灯刑：光圈锁定后离开" : "冲锋蓄势：跳跃或登台";
        if (state == Mode.Special) Warning = kind == Kind.SunburialScorpion ? "流沙陷区：离开沙圈或登台" : kind == Kind.SteleLizard ? "扇尾扬砂：远离尾部" : kind == Kind.IroncoffinJailer ? "锁链拖刑：跳起，石台可以挡住锁链" : "引魂归灯：避开横向回收路线";
    }
    private void Update()
    {
        timer += Time.deltaTime; Animate();
        if (State == Mode.Dead)
        {
            if (timer >= 1.8f) { State = Mode.Defeated; entranceGate.SetActive(false); exitGate.SetActive(false); StartCoroutine(Complete()); }
            return;
        }
        if (State == Mode.Defeated) return;
        if (State == Mode.Dormant)
        {
            if (Time.time >= searchAt) { player = FindObjectOfType<PlayerController>(); searchAt = Time.time + .4f; }
            if (player != null && !player.isDead && player.transform.position.x > arenaLeft + 2 && player.transform.position.x < arenaRight - 1) BeginBattle(player);
            return;
        }
        if (player == null || player.isDead || !player.gameObject.activeInHierarchy) { ClearEffects(); return; }
        switch (State)
        {
            case Mode.Intro: if (timer >= 1.3f) SetState(Mode.Idle); break;
            case Mode.Idle: FacePlayer(); if (timer >= idleDuration) ChooseAttack(); break;
            case Mode.Close:
                float windup = kind == Kind.SoulLantern || kind == Kind.SunburialScorpion ? .65f : .7f;
                if (timer >= windup && (stage == 0 || timer < windup + .2f))
                { stage = 1; HitBox(body.position + new Vector2(facing * 2, 1), new Vector2(2.8f, 2), 15, attackId); }
                if (timer >= windup + .45f) Exhaust(.8f);
                break;
            case Mode.Heavy: TickHeavy(); break;
            case Mode.Mobility: TickMobility(); break;
            case Mode.Special: TickSpecial(); break;
            case Mode.Exhausted: if (timer >= recovery) SetState(Mode.Idle); break;
        }
    }
    private void FacePlayer() { facing = player.transform.position.x >= body.position.x ? 1 : -1; visual.flipX = facing < 0; }
    private int Weight(int move, float distance)
    {
        if (move == lastMove && (repeatedMoves >= 2 || move == 2)) return 0;
        if (move == 0) return distance < 4 && player.transform.position.y < groundY + 2 ? 55 : 0;
        if (move == 2 && kind != Kind.SoulLantern) return distance >= 6 && (kind != Kind.SteleLizard || player.transform.position.y < groundY + 1.5f) ? 30 : 0;
        if (move == 3 && kind == Kind.SteleLizard) return facing * (player.transform.position.x - body.position.x) < 0 ? 55 : 20;
        return move == 1 ? 35 : 30;
    }
    private void ChooseAttack()
    {
        // Old effects keep their attack ID, so overlapping casts never share a fresh hit allowance.
        effects.RemoveAll(go => go == null); landed.RemoveWhere(id => id < attackId - 8);
        phase = Enraged; FacePlayer();
        float d = Mathf.Abs(player.transform.position.x - body.position.x);
        int total = 0; for (int i = 0; i < 4; i++) total += Weight(i, d);
        int roll = Random.Range(0, total), move = 1;
        for (int i = 0; i < 4; i++) { roll -= Weight(i, d); if (roll < 0) { move = i; break; } }
        StartAttack(move);
    }
    private void StartAttack(int move)
    {
        repeatedMoves = move == lastMove ? repeatedMoves + 1 : 1; lastMove = move; attackId++;
        SetState(move == 0 ? Mode.Close : move == 1 ? Mode.Heavy : move == 2 ? Mode.Mobility : Mode.Special);
        origin = body.position; combo = false;
        if (State == Mode.Heavy && kind == Kind.SunburialScorpion)
        {
            MarkTarget(1.2f, 0, false, 2.4f, 1.6f, 22, 2);
        }
        if (State == Mode.Mobility && kind == Kind.SoulLantern) MarkTarget(1.4f, .9f, false, 2.4f, 2.6f, 18, 6);
        if (State == Mode.Mobility && kind == Kind.SteleLizard)
        {
            destination = new Vector2(Mathf.Clamp(player.transform.position.x, arenaLeft + 3, arenaRight - 3), groundY + .02f);
            Zone(destination, new Vector2(3.8f, 1.6f), 1.2f, .35f, 20, 7);
        }
        if (State == Mode.Special && kind == Kind.SunburialScorpion) MarkTarget(1.3f, 0, true, phase ? 5 : 3.8f, .35f, 0, 3, 3);
        if (State == Mode.Special && kind == Kind.SoulLantern) SpawnRecall();
    }
    private void TickHeavy()
    {
        if (kind == Kind.SunburialScorpion)
        {
            if (phase && stage == 0 && timer >= .8f) { stage++; MarkTarget(1.2f, 0, false, 2.4f, 1.6f, 22, 2); }
            if (timer >= (phase ? 2.3f : 1.5f)) Exhaust(1.8f);
        }
        else if (kind == Kind.SoulLantern)
        {
            if (stage == 0 && timer >= 1.2f)
            {
                stage++; int n = phase ? 5 : 3;
                for (int i = 0; i < n; i++) Projectile(body.position + new Vector2(facing * 1.7f, .7f + i * .7f), new Vector2(facing * 3.5f, 0), 5, 12, 5);
            }
            if (timer >= 1.6f) Exhaust(1.4f);
        }
        else
        {
            float windup = kind == Kind.SteleLizard ? 1.4f : 1.3f;
            if (stage == 0 && timer >= windup)
            {
                stage++; HitBox(body.position + new Vector2(facing * 2.2f, .65f), new Vector2(3.5f, 1.3f), 24, attackId);
                Burst(body.position + Vector2.right * facing * 2);
                if (kind == Kind.SteleLizard || phase) SpawnWaves();
            }
            if (kind == Kind.SteleLizard && phase && stage == 1 && timer >= windup + .65f) { stage++; SpawnWaves(); }
            if (timer >= windup + (phase ? .95f : .35f)) Exhaust(1.8f);
        }
    }
    private void TickMobility()
    {
        if (kind == Kind.SoulLantern)
        {
            if (phase && stage == 0 && timer >= .85f) { stage++; MarkTarget(1.4f, .9f, false, 2.4f, 2.6f, 18, 6); }
            if (timer >= (phase ? 2.6f : 1.8f)) Exhaust(1.5f);
            return;
        }
        if (kind == Kind.SteleLizard)
        {
            if (timer >= 1.2f && stage == 0)
            {
                stage++; body.position = destination; hitbox.enabled = true; visual.enabled = true; Burst(destination);
                if (phase) for (int i = -1; i <= 1; i += 2) Projectile(destination + Vector2.up * .9f, new Vector2(i * 4, 1), 7, 12, 1.8f);
            }
            if (timer >= 1.65f) Exhaust(2);
            return;
        }
        if (timer < 1) return;
        if (stage == 0) stage = 1;
        if (combo)
        {
            Warning = "横砸将至：后退或绕后";
            if (timer >= 2.65f && stage == 2) { stage++; HitBox(body.position + new Vector2(facing * 2, 1), new Vector2(2.8f, 2), 15, attackId); }
            if (timer >= 2.95f) Exhaust(1.5f);
        }
        else if (timer >= 2)
        {
            if (phase && Random.value < .4f) { combo = true; stage = 2; Warning = "横砸蓄势"; }
            else Exhaust(1.5f);
        }
    }
    private void TickSpecial()
    {
        if (kind == Kind.SunburialScorpion) { if (timer >= 1.65f) Exhaust(1.2f); }
        else if (kind == Kind.SteleLizard)
        {
            if (stage == 0 && timer >= 1)
            {
                stage++; HitBox(body.position + new Vector2(-facing * 2.2f, .9f), new Vector2(3.5f, 1.8f), 18, attackId);
                for (int i = 0; i < 3; i++) Projectile(body.position + new Vector2(-facing * 2, .8f), new Vector2(-facing * (3.5f + i * .4f), i * 1.1f), 7, 12, 1.8f);
            }
            if (timer >= 1.4f) Exhaust(1.1f);
        }
        else if (kind == Kind.IroncoffinJailer)
        {
            if (stage == 0 && timer >= 1.1f) { stage++; var e = Projectile(body.position + new Vector2(facing * 1.7f, .65f), new Vector2(facing * 7, 0), 4, 16, 1.4f); e.pull = true; }
            if (timer >= 2.6f) Exhaust(1.2f);
        }
        else if (timer >= 3.8f) Exhaust(2);
    }
    private void FixedUpdate()
    {
        walking = false;
        if (!body.simulated || !IsFighting || player == null || player.isDead) return;
        body.velocity = Vector2.zero;
        if (State == Mode.Mobility && kind == Kind.SteleLizard && timer >= .25f && timer < 1.2f)
        {
            // A shallow visible sand ridge; target is fixed before burrowing.
            hitbox.enabled = false;
            body.MovePosition(Vector2.Lerp(origin, destination, Mathf.Clamp01((timer - .25f) / .95f)));
            return;
        }
        if (State == Mode.Mobility && kind != Kind.SoulLantern && kind != Kind.SteleLizard && timer >= 1 && timer < 2 && !combo)
        {
            float step = SafeStep(facing, 10 * Time.fixedDeltaTime);
            Vector2 next = body.position + Vector2.right * facing * step;
            HitBox((body.position + next) * .5f + Vector2.up * .55f, new Vector2(hitbox.size.x + step, 1.1f), 20, attackId);
            body.MovePosition(next);
            if (step < .01f) { Burst(next); Exhaust(2); }
            return;
        }
        if (State != Mode.Idle) return;
        float delta = player.transform.position.x - body.position.x;
        int dir = delta >= 0 ? 1 : -1;
        float distance = Mathf.Max(0, Mathf.Abs(delta) - 3.1f);
        float move = SafeStep(dir, Mathf.Min(distance, (phase ? 2 : 1.6f) * Time.fixedDeltaTime));
        if (move <= .001f) return;
        walking = true; walkTime += Time.fixedDeltaTime * 8;
        float y = groundY + (kind == Kind.SoulLantern ? .15f + Mathf.Sin(walkTime) * .08f : .02f);
        body.MovePosition(new Vector2(body.position.x + dir * move, y));
    }
    private float SafeStep(int dir, float requested)
    {
        float x = Mathf.Clamp(body.position.x + dir * requested, arenaLeft + hitbox.size.x * .5f + .15f, arenaRight - hitbox.size.x * .5f - .15f);
        float step = Mathf.Abs(x - body.position.x);
        int n = Physics2D.BoxCastNonAlloc(hitbox.bounds.center, hitbox.bounds.size * .96f, 0, Vector2.right * dir, moveHits, step + .04f, groundLayer);
        for (int i = 0; i < n; i++)
        {
            var h = moveHits[i];
            if (h.distance < .001f && dir * (hitbox.bounds.center.x - h.collider.bounds.center.x) >= 0) continue;
            step = Mathf.Min(step, Mathf.Max(0, h.distance - .04f));
        }
        return step;
    }
    public Vector2 SurfaceAt(float x, bool mainFloor)
    {
        x = Mathf.Clamp(x, arenaLeft + 2, arenaRight - 2);
        var h = Physics2D.Raycast(new Vector2(x, groundY + (mainFloor ? .3f : 8)), Vector2.down, mainFloor ? 1 : 10, groundLayer);
        return new Vector2(x, h.collider != null ? h.point.y + .02f : groundY + .02f);
    }
    private void MarkTarget(float delay, float track, bool floor, float width, float height, int damage, int row, float duration = .4f)
    {
        var e = Zone(SurfaceAt(player.transform.position.x, floor), new Vector2(width, height), delay, duration, damage, row);
        e.trackFor = track; e.mainFloor = floor;
        if (row == 3) e.slow = true;
    }
    private ArenaBossEffect Zone(Vector2 position, Vector2 size, float delay, float duration, int damage, int row)
    {
        var e = Effect(position, row, duration + delay); e.size = size; e.delay = delay; e.damage = damage; return e;
    }
    private ArenaBossEffect Projectile(Vector2 position, Vector2 velocity, int row, int damage, float lifetime)
    {
        var e = Effect(position, row, lifetime); e.velocity = velocity; e.size = row == 1 ? new Vector2(1.5f, .8f) : new Vector2(.8f, .8f); e.damage = damage; e.projectile = true; return e;
    }
    private void SpawnWaves()
    { for (int dir = -1; dir <= 1; dir += 2) Projectile(body.position + new Vector2(dir * 2, .42f), new Vector2(dir * 5, 0), 1, 16, 3); }
    private void SpawnRecall()
    {
        for (int dir = -1; dir <= 1; dir += 2)
        {
            Vector2 start = new Vector2(dir < 0 ? arenaLeft + 1 : arenaRight - 1, groundY + .75f);
            var e = Projectile(start, new Vector2(-dir * 6, 0), 5, 16, 4.4f); e.delay = 1.5f;
            e.stopX = body.position.x; e.stopAtTarget = true;
            // A shallow ring stretched along the actual low return lane.
            var lane = Effect(new Vector2((start.x + body.position.x) * .5f, groundY + .4f), 0, 1.5f);
            lane.size = new Vector2(Mathf.Abs(start.x - body.position.x), .25f); lane.damage = 0;
        }
    }
    private ArenaBossEffect Effect(Vector2 position, int row, float lifetime)
    {
        var go = new GameObject("Boss Effect " + row); go.transform.SetParent(effectRoot, true); go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 3;
        var e = go.AddComponent<ArenaBossEffect>(); e.owner = this; e.frames = effectFrames; e.row = row; e.lifetime = lifetime; e.attackId = attackId;
        effects.Add(go); return e;
    }
    private void Burst(Vector2 point) { var e = Effect(point, 7, .45f); e.size = new Vector2(3, 1.5f); }
    public bool OverlapsPlayer(Vector2 center, Vector2 size)
    { return playerBox != null && playerBox.enabled && playerBox.bounds.Intersects(new Bounds(center, size)); }
    public bool HitBox(Vector2 center, Vector2 size, int damage, int id, bool pull = false)
    {
        if (!IsFighting || player == null || player.isDead || playerHealth.invulnerable || landed.Contains(id) || slide != null && slide.IsSliding || !OverlapsPlayer(center, size)) return false;
        float before = playerHealth.CurrentHp; damageSource.damage = damage; playerHealth.TakeDamage(damageSource);
        if (playerHealth.CurrentHp >= before) return false;
        landed.Add(id);
        if (pull && !player.isDead)
        {
            // Pull only into clear space, never through a pillar or platform.
            Vector2 dir = Vector2.right * Mathf.Sign(body.position.x - playerBody.position.x);
            var wall = Physics2D.BoxCast(playerBox.bounds.center, playerBox.bounds.size * .95f, 0, dir, 2, groundLayer);
            float amount = wall.collider != null ? Mathf.Max(0, wall.distance - .1f) : 2;
            playerBody.position += dir * amount;
        }
        return true;
    }
    public void ApplySlow(Vector2 center, Vector2 size)
    {
        if (player != null && !player.isDead && (slide == null || !slide.IsSliding) && OverlapsPlayer(center, size)) slow.Apply(.15f);
    }
    private void Animate()
    {
        if (frames == null || frames.Length != 32) return;
        int row = State == Mode.Close ? 2 : State == Mode.Heavy ? 3 : State == Mode.Mobility ? 4 : State == Mode.Special ? 5 : State == Mode.Exhausted ? 6 : State == Mode.Dead || State == Mode.Defeated ? 7 : walking ? 1 : 0;
        if (combo && State == Mode.Mobility) row = 2;
        float start = State == Mode.Close ? .65f : State == Mode.Heavy ? 1.2f : State == Mode.Mobility ? 1 : 1.1f;
        float poseTime = timer;
        if (combo && State == Mode.Mobility) { poseTime = timer - 2; start = .65f; }
        int frame = row == 0 || row == 6 ? (int)(timer * 5) % 4 : row == 1 ? (int)walkTime % 4 : row == 7 ? Mathf.Min(3, (int)(timer * 3)) : poseTime < start ? Mathf.Min(1, (int)(poseTime / (start * .5f))) : poseTime < start + .25f ? 2 : 3;
        visual.sprite = frames[row * 4 + frame];
        visual.color = row == 7 ? new Color(.6f, .6f, .65f) : phase ? new Color(1, .85f, .8f) : Color.white;
    }
    private void Exhaust(float seconds) { recovery = seconds; SetState(Mode.Exhausted); }
    private void Die()
    {
        if (State == Mode.Dead || State == Mode.Defeated) return;
        ClearEffects(); SetState(Mode.Dead); body.simulated = false; hitbox.enabled = false; Warning = Title + "倒下了";
    }
    private IEnumerator Complete() { yield return new WaitForSeconds(.65f); if (player != null && !player.isDead) completionUI.Show(); }
    private void ClearEffects() { foreach (var go in effects) if (go != null) Destroy(go); effects.Clear(); if (slow != null) slow.Clear(); }
}
