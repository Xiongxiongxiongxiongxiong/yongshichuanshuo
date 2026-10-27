using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class WildernessCaveCreature : MonoBehaviour
{
    public enum Species { MossLizard, MistMoth, RockSpitter }
    public enum ActionState { Patrol, Chase, Windup, Attack, Recover, Return, Hurt, Dead }
    public Species species;
    [Tooltip("Six rows of four frames: idle, move, windup, attack, hurt, death.")]
    public Sprite[] frames;
    public Sprite[] effectFrames;
    public Attack damageSource;
    public LayerMask groundLayer = 1 << 6;
    public float detectionRadius = 7f, patrolRadius = 1.5f, leashRadius = 10f;
    public ActionState State { get; private set; }
    public bool IsDead => State == ActionState.Dead;

    Character health;
    Rigidbody2D body;
    BoxCollider2D hitbox;
    SpriteRenderer visual;
    PlayerController target;
    CapsuleCollider2D targetBody;
    Vector2 home, aim, attackStart, diveDirection;
    Color baseColor;
    float timer, nextAttack, nextScan;
    int facing = 1;
    bool dealtDamage, released;
    readonly Collider2D[] hits = new Collider2D[16];
    readonly List<GameObject> effects = new List<GameObject>();
    bool Flying => species == Species.MistMoth;
    bool Stationary => species == Species.RockSpitter;

    void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        home = body.position; baseColor = visual.color;
        body.freezeRotation = true; body.gravityScale = Flying ? 0 : 3;
    }
    void OnEnable() { health.OnTakeDamage.AddListener(Hurt); health.OnDead.AddListener(Die); }
    void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(Hurt); health.OnDead.RemoveListener(Die);
        ClearEffects();
        if (visual != null) visual.color = baseColor;
    }
    void SetState(ActionState state) { State = state; timer = 0; dealtDamage = released = false; }
    void Update()
    {
        timer += Time.deltaTime; Animate();
        if (IsDead) return;
        effects.RemoveAll(effect => effect == null);
        if (State == ActionState.Hurt)
        {
            if (timer >= .35f) SetState(Flying ? ActionState.Return : ActionState.Patrol);
            return;
        }
        if (State == ActionState.Recover)
        {
            if (timer >= (Flying ? 1f : Stationary ? 1.5f : .8f))
                SetState(Flying ? ActionState.Return : ActionState.Patrol);
            return;
        }
        if (Time.time >= nextScan)
        {
            if (target == null) target = FindObjectOfType<PlayerController>();
            targetBody = target != null ? target.GetComponent<CapsuleCollider2D>() : null;
            nextScan = Time.time + .3f;
        }
        bool alive = target != null && targetBody != null && target.gameObject.activeInHierarchy && !target.isDead;
        if (State == ActionState.Windup)
        {
            if (!alive) { FinishAttack(); return; }
            if (timer >= (Flying ? .6f : Stationary ? .8f : .5f))
            {
                diveDirection = (aim - (Vector2)hitbox.bounds.center).normalized;
                attackStart = body.position; SetState(ActionState.Attack);
                if (!Flying && !Stationary && SafeAhead(facing)) body.velocity = new Vector2(facing * 5, 2);
            }
            return;
        }
        if (State == ActionState.Attack)
        {
            if (!alive) { FinishAttack(); return; }
            if (Stationary)
            {
                if (!released && timer >= .12f) { released = true; ThrowSpore(); }
                if (timer >= .45f) FinishAttack();
            }
            else
            {
                DamagePlayer();
                if (timer >= (Flying ? .7f : .4f) || (!Flying && Mathf.Abs(body.position.x - attackStart.x) >= 2)) FinishAttack();
            }
            return;
        }
        if (State == ActionState.Return)
        {
            if (Vector2.Distance(body.position, home) < .18f) SetState(ActionState.Patrol);
            return;
        }
        bool visible = alive && Vector2.Distance(hitbox.bounds.center, targetBody.bounds.center) <= detectionRadius &&
            Vector2.Distance(home, target.transform.position) <= leashRadius &&
            (Flying || Mathf.Abs(targetBody.bounds.center.y - hitbox.bounds.center.y) < 2.5f) &&
            !Physics2D.Linecast(hitbox.bounds.center, targetBody.bounds.center, groundLayer);
        if (visible)
        {
            facing = target.transform.position.x >= body.position.x ? 1 : -1;
            visual.flipX = facing < 0;
            if (Time.time >= nextAttack && (Flying || Stationary || Mathf.Abs(target.transform.position.x - body.position.x) < 3.2f))
            {
                aim = targetBody.bounds.center;
                body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y);
                SetState(ActionState.Windup);
            }
            else if (!Flying && !Stationary && State != ActionState.Chase) SetState(ActionState.Chase);
        }
        else if (State == ActionState.Chase) SetState(ActionState.Patrol);
    }
    void FixedUpdate()
    {
        if (IsDead || State == ActionState.Hurt) return;
        if (Flying)
        {
            Vector2 velocity = Vector2.zero;
            if (State == ActionState.Return) velocity = Vector2.ClampMagnitude((home - body.position) * 3, 3);
            else if (State == ActionState.Patrol)
            {
                Vector2 hover = home + new Vector2(Mathf.Sin(Time.time * .9f) * .45f, Mathf.Sin(Time.time * 1.6f) * .2f);
                velocity = (hover - body.position) * 2;
            }
            else if (State == ActionState.Attack)
            {
                velocity = diveDirection * 5.5f;
                if (Physics2D.BoxCast(hitbox.bounds.center, hitbox.bounds.size * .9f, 0, diveDirection, .22f, groundLayer))
                { FinishAttack(); velocity = Vector2.zero; }
            }
            // Keep flight and return paths out of terrain without teleporting through walls.
            if (velocity.sqrMagnitude > .01f && Physics2D.BoxCast(hitbox.bounds.center, hitbox.bounds.size * .9f, 0,
                    velocity.normalized, velocity.magnitude * Time.fixedDeltaTime + .04f, groundLayer)) velocity = Vector2.zero;
            body.velocity = velocity; return;
        }
        float speed = 0;
        if (!Stationary)
        {
            if (State == ActionState.Patrol)
            {
                if (Mathf.Abs(body.position.x - home.x) > patrolRadius) facing = home.x > body.position.x ? 1 : -1;
                speed = .6f;
            }
            else if (State == ActionState.Chase) speed = 1.2f;
            else if (State == ActionState.Attack) speed = 5;
            if (speed > 0 && !SafeAhead(facing))
            {
                speed = 0;
                if (State == ActionState.Attack) FinishAttack();
                else if (State == ActionState.Patrol) facing = -facing;
            }
        }
        body.velocity = new Vector2(speed * facing, body.velocity.y);
        if (speed > 0) visual.flipX = facing < 0;
    }
    bool SafeAhead(int direction)
    {
        float distance = hitbox.bounds.extents.x + .25f;
        if (Physics2D.Raycast(hitbox.bounds.center, Vector2.right * direction, distance, groundLayer)) return false;
        return Physics2D.Raycast(new Vector2(hitbox.bounds.center.x + direction * distance, hitbox.bounds.min.y + .3f),
            Vector2.down, 1f, groundLayer);
    }
    void FinishAttack()
    {
        nextAttack = Time.time + (Flying ? 2.2f : Stationary ? 1.5f : 1.6f);
        body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y);
        SetState(ActionState.Recover);
    }
    void DamagePlayer()
    {
        if (dealtDamage || damageSource == null) return;
        int count = Physics2D.OverlapBoxNonAlloc(hitbox.bounds.center, hitbox.bounds.size + Vector3.one * .15f, 0, hits, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            if (!(hits[i] is CapsuleCollider2D) || hits[i].isTrigger) continue;
            var p = hits[i].GetComponentInParent<PlayerController>(); if (p == null || p.isDead) continue;
            var hp = p.GetComponent<Character>(); float before = hp.CurrentHp; hp.TakeDamage(damageSource);
            if (hp.CurrentHp < before) { dealtDamage = true; return; }
        }
    }
    void ThrowSpore()
    {
        var go = new GameObject("Cave Spore Stone");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.position = hitbox.bounds.center + Vector3.right * facing * .5f + Vector3.up * .15f;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 1;
        var damage = go.AddComponent<Attack>(); damage.enabled = false; damage.damage = 12;
        var projectile = go.AddComponent<CaveSporeProjectile>(); projectile.owner = this; projectile.damageSource = damage;
        projectile.frames = effectFrames; projectile.groundLayer = groundLayer;
        float flight = .85f;
        projectile.velocity = (aim - (Vector2)go.transform.position) / flight + Vector2.up * (9f * flight * .5f);
        effects.Add(go);
    }
    public void TrackEffect(GameObject effect) { effects.Add(effect); }
    void Hurt(Transform source)
    {
        if (IsDead) return;
        SetState(ActionState.Hurt); nextAttack = Time.time + .7f;
        float direction = source != null ? Mathf.Sign(body.position.x - source.position.x) : -facing;
        body.velocity = Stationary ? Vector2.zero : new Vector2(direction * 2, Flying ? .5f : 1);
    }
    void ClearEffects() { foreach (var effect in effects) if (effect != null) Destroy(effect); effects.Clear(); }
    void Die()
    {
        if (IsDead) return;
        SetState(ActionState.Dead); ClearEffects();
        body.velocity = Vector2.zero; body.simulated = false; hitbox.enabled = false;
        Destroy(gameObject, 1.2f);
    }
    void Animate()
    {
        if (frames == null || frames.Length < 24) return;
        int row = IsDead ? 5 : State == ActionState.Hurt ? 4 : State == ActionState.Windup ? 2 :
            State == ActionState.Attack ? 3 : State == ActionState.Patrol && !Stationary || State == ActionState.Chase || State == ActionState.Return ? 1 : 0;
        int index = Mathf.FloorToInt(timer * (IsDead ? 4 : 8));
        visual.sprite = frames[row * 4 + (row < 2 ? index % 4 : Mathf.Min(3, index))];
        visual.color = State == ActionState.Windup ? Color.Lerp(baseColor, new Color(.55f, 1, .8f), .25f + .2f * Mathf.Sin(timer * 20)) : baseColor;
    }
}
