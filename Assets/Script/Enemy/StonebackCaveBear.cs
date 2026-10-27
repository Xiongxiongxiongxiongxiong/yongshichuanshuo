using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer), typeof(DirectionalDamageGuard))]
public class StonebackCaveBear : MonoBehaviour
{
    public enum CombatState { Patrol, Chase, MeleeWindup, Melee, SlamWindup, Slam, Guard, GuardRise, Recover, Hurt, Dead }
    public Sprite[] frames, effectFrames;
    public Attack meleeDamage, slamDamage;
    public LayerMask groundLayer = 1 << 6;
    public float detectionRadius = 8, patrolRadius = 1.2f, leashRadius = 11;
    public CombatState State { get; private set; }
    public bool IsDead => State == CombatState.Dead;
    public bool HasSkillArmor => State == CombatState.SlamWindup || State == CombatState.Slam ||
        State == CombatState.Guard || State == CombatState.GuardRise;
    public int Facing => facing;
    public float GuardCooldownRemaining => Mathf.Max(0, guardReady - Time.time);

    Character health;
    Rigidbody2D body;
    BoxCollider2D hitbox;
    SpriteRenderer visual;
    DirectionalDamageGuard guard;
    PlayerController target;
    CapsuleCollider2D targetBody;
    Vector2 home;
    Color baseColor;
    int facing = 1, attackCycle;
    float timer, nextAttack, nextScan, guardReady;
    bool struck, released;
    HashSet<Character> slamVictims;
    readonly Collider2D[] contacts = new Collider2D[16];
    readonly List<GameObject> effects = new List<GameObject>();

    void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>(); guard = GetComponent<DirectionalDamageGuard>();
        home = body.position; baseColor = visual.color; body.freezeRotation = true;
    }
    void OnEnable() { health.OnTakeDamage.AddListener(Hurt); health.OnDead.AddListener(Die); }
    void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(Hurt); health.OnDead.RemoveListener(Die);
        guard.SetGuard(false, facing); ClearEffects(); visual.color = baseColor;
    }
    void SetState(CombatState state)
    {
        State = state; timer = 0; struck = released = false;
        guard.SetGuard(state == CombatState.Guard, facing);
        if (state == CombatState.Guard) guardReady = Time.time + 8;
        if (state == CombatState.SlamWindup) slamVictims = new HashSet<Character>();
    }
    void Update()
    {
        timer += Time.deltaTime; Animate(); if (IsDead) return;
        effects.RemoveAll(effect => effect == null);
        if (State == CombatState.Hurt) { if (timer >= .35f) SetState(CombatState.Patrol); return; }
        if (State == CombatState.Recover) { if (timer >= 1.2f) SetState(CombatState.Patrol); return; }
        if (State == CombatState.Guard) { if (timer >= 1.5f) SetState(CombatState.GuardRise); return; }
        if (State == CombatState.GuardRise) { if (timer >= .6f) Recover(); return; }
        if (Time.time >= nextScan)
        {
            if (target == null) target = FindObjectOfType<PlayerController>();
            targetBody = target != null ? target.GetComponent<CapsuleCollider2D>() : null;
            nextScan = Time.time + .3f;
        }
        bool alive = target != null && targetBody != null && !target.isDead && target.gameObject.activeInHierarchy;
        if (State == CombatState.MeleeWindup || State == CombatState.SlamWindup)
        {
            if (!alive) { Recover(); return; }
            if (timer >= (State == CombatState.SlamWindup ? .8f : .45f))
                SetState(State == CombatState.SlamWindup ? CombatState.Slam : CombatState.Melee);
            return;
        }
        if (State == CombatState.Melee)
        {
            if (!struck && timer >= .15f && timer < .4f)
                struck = DamageBox((Vector2)hitbox.bounds.center + Vector2.right * facing * .8f, new Vector2(1.8f, 1.7f), meleeDamage, null);
            if (timer >= .55f) Recover(); return;
        }
        if (State == CombatState.Slam)
        {
            if (!released && timer >= .15f)
            {
                released = true;
                if (slamVictims == null) slamVictims = new HashSet<Character>();
                DamageBox(new Vector2(hitbox.bounds.center.x + facing * 1.1f, hitbox.bounds.min.y + .65f), new Vector2(2.1f, 1.3f), slamDamage, slamVictims);
                MakeEffect(false); MakeEffect(true);
            }
            if (timer >= .6f) Recover(); return;
        }
        bool visible = alive && Vector2.Distance(hitbox.bounds.center, targetBody.bounds.center) <= detectionRadius &&
            Vector2.Distance(home, target.transform.position) < leashRadius &&
            Mathf.Abs(hitbox.bounds.center.y - targetBody.bounds.center.y) < 2.8f &&
            !Physics2D.Linecast(hitbox.bounds.center, targetBody.bounds.center, groundLayer);
        if (!visible) { if (State != CombatState.Patrol) SetState(CombatState.Patrol); return; }
        facing = target.transform.position.x >= body.position.x ? 1 : -1; visual.flipX = facing < 0;
        if (health.CurrentHp <= health.maxHp * .5f && Time.time >= guardReady)
        { SetState(CombatState.Guard); return; }
        float distance = Mathf.Abs(target.transform.position.x - body.position.x);
        if (Time.time >= nextAttack && distance <= (attackCycle % 2 == 0 ? 2.1f : 4.5f))
        { SetState(attackCycle++ % 2 == 0 ? CombatState.MeleeWindup : CombatState.SlamWindup); }
        else if (State != CombatState.Chase) SetState(CombatState.Chase);
    }
    void FixedUpdate()
    {
        if (IsDead || State == CombatState.Hurt) return;
        float speed = 0;
        if (State == CombatState.Patrol)
        {
            if (Mathf.Abs(body.position.x - home.x) > patrolRadius) facing = home.x > body.position.x ? 1 : -1;
            speed = .45f;
        }
        else if (State == CombatState.Chase) speed = .9f;
        if (speed > 0 && !SafeAhead()) { speed = 0; if (State == CombatState.Patrol) facing = -facing; }
        body.velocity = new Vector2(speed * facing, body.velocity.y);
        if (speed > 0) visual.flipX = facing < 0;
    }
    bool SafeAhead()
    {
        float distance = hitbox.bounds.extents.x + .25f;
        return !Physics2D.Raycast(hitbox.bounds.center, Vector2.right * facing, distance, groundLayer) &&
            Physics2D.Raycast(new Vector2(hitbox.bounds.center.x + facing * distance, hitbox.bounds.min.y + .25f), Vector2.down, .8f, groundLayer);
    }
    bool DamageBox(Vector2 center, Vector2 size, Attack source, HashSet<Character> victims)
    {
        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0, contacts, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            if (!(contacts[i] is CapsuleCollider2D) || contacts[i].isTrigger) continue;
            var p = contacts[i].GetComponentInParent<PlayerController>(); if (p == null || p.isDead) continue;
            var hp = p.GetComponent<Character>(); if (victims != null && victims.Contains(hp)) continue;
            float before = hp.CurrentHp; hp.TakeDamage(source);
            if (hp.CurrentHp < before) { if (victims != null) victims.Add(hp); return true; }
        }
        return false;
    }
    void MakeEffect(bool wave)
    {
        var go = new GameObject(wave ? "Cave Bear Rock Wave" : "Cave Bear Slam Impact");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        Vector2 point = new Vector2(hitbox.bounds.center.x + facing * 1.1f, hitbox.bounds.min.y + .04f);
        var floor = Physics2D.Raycast(point + Vector2.up * .6f, Vector2.down, 1.2f, groundLayer);
        if (!floor) { Destroy(go); return; }
        go.transform.position = floor.point + Vector2.up * .04f;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 1;
        var damage = go.AddComponent<Attack>(); damage.enabled = false; damage.damage = 12;
        var effect = go.AddComponent<CaveBearRockWave>(); effect.owner = this; effect.wave = wave; effect.frames = effectFrames;
        effect.direction = facing; effect.groundLayer = groundLayer; effect.damageSource = damage; effect.victims = slamVictims;
        effects.Add(go);
    }
    void Recover() { nextAttack = Time.time + 1.6f; body.velocity = new Vector2(0, body.velocity.y); SetState(CombatState.Recover); }
    void Hurt(Transform source)
    {
        if (IsDead || HasSkillArmor) return;
        SetState(CombatState.Hurt); nextAttack = Time.time + .7f;
        body.velocity = new Vector2((source != null ? Mathf.Sign(body.position.x - source.position.x) : -facing) * 1.7f, 1);
    }
    void ClearEffects() { foreach (var effect in effects) if (effect != null) Destroy(effect); effects.Clear(); }
    void Die()
    {
        if (IsDead) return;
        SetState(CombatState.Dead); ClearEffects(); body.velocity = Vector2.zero; body.simulated = false; hitbox.enabled = false;
        Destroy(gameObject, 1.3f);
    }
    void Animate()
    {
        if (frames == null || frames.Length < 32) return;
        int row = IsDead ? 7 : State == CombatState.Hurt ? 6 : State == CombatState.Guard || State == CombatState.GuardRise ? 5 :
            State == CombatState.SlamWindup ? 3 : State == CombatState.Slam ? 4 : State == CombatState.Melee || State == CombatState.MeleeWindup ? 2 :
            State == CombatState.Patrol || State == CombatState.Chase ? 1 : 0;
        int frame = Mathf.FloorToInt(timer * (IsDead ? 4 : 7));
        if (State == CombatState.MeleeWindup) frame = 0;
        if (State == CombatState.Guard) frame = Mathf.Min(2, frame);
        if (State == CombatState.GuardRise) frame = 3;
        visual.sprite = frames[row * 4 + (row < 2 ? frame % 4 : Mathf.Min(3, frame))];
        float glow = State == CombatState.SlamWindup ? .2f + timer * .35f : State == CombatState.Guard && timer > 1.1f ? .4f : 0;
        visual.color = Color.Lerp(baseColor, new Color(.5f, 1, .85f), glow);
    }
}
