using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DesertDungeonEnemy : MonoBehaviour
{
    public enum Kind { RedtailScorpion, SandJawWorm, ThornballCactus, RustSkeleton, SoulLanternWraith, ChainAmbushBeast, SandPincerGuard, SoulboundJailer }
    public enum ActionState { Idle, Patrol, Chase, Windup, Attack, Recover, Return, Hurt, Stunned, Dead }
    public enum Ability { Basic, Main, Secondary }
    public Kind kind;
    public Sprite[] frames, effectFrames;
    public Attack damageSource;
    public LayerMask groundLayer = 1 << 6;
    public float detectionRadius = 7, patrolRadius = 1.4f, leashRadius = 11;
    public ActionState State { get; private set; }
    public Ability CurrentAbility { get; private set; }
    public bool IsElite => kind == Kind.SandPincerGuard || kind == Kind.SoulboundJailer;
    public bool IsDead => State == ActionState.Dead;
    public bool SkillArmor => IsElite && CurrentAbility != Ability.Basic && (State == ActionState.Windup || State == ActionState.Attack);
    public bool Flying => kind == Kind.SoulLanternWraith;
    public bool Ambusher => kind == Kind.SandJawWorm || kind == Kind.ChainAmbushBeast;
    public int Facing => facing;
    public float MainCooldown => Mathf.Max(0, mainReady - Time.time);
    public float SecondaryCooldown => Mathf.Max(0, secondaryReady - Time.time);
    Character health;
    Rigidbody2D body;
    BoxCollider2D hitbox;
    SpriteRenderer visual;
    PlayerController target;
    CapsuleCollider2D targetBody;
    Color baseColor;
    Vector2 home, lockedAim, attackStart;
    int facing = 1, cycles;
    float timer, nextAttack, nextScan, mainReady, secondaryReady;
    bool struck, released, awakened;
    HashSet<Character> victims;
    readonly Collider2D[] contacts = new Collider2D[16];
    readonly List<GameObject> effects = new List<GameObject>();
    bool Stationary => kind == Kind.ThornballCactus;
    bool Charging => kind == Kind.SandPincerGuard && CurrentAbility == Ability.Main;
    float Windup => kind == Kind.RedtailScorpion || kind == Kind.RustSkeleton ? .5f :
        kind == Kind.ChainAmbushBeast ? .7f : kind == Kind.SandPincerGuard ? (CurrentAbility == Ability.Main ? .7f : CurrentAbility == Ability.Secondary ? .8f : .5f) :
        kind == Kind.SoulboundJailer ? (CurrentAbility == Ability.Main ? .9f : CurrentAbility == Ability.Secondary ? .8f : .5f) :
        kind == Kind.ThornballCactus ? .7f : .8f;

    void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        home = body.position; baseColor = visual.color; body.freezeRotation = true; body.gravityScale = Flying ? 0 : 3;
        State = Ambusher || Stationary || Flying ? ActionState.Idle : ActionState.Patrol;
    }
    void OnEnable() { health.OnTakeDamage.AddListener(Hurt); health.OnDead.AddListener(Die); }
    void OnDisable() { health.OnTakeDamage.RemoveListener(Hurt); health.OnDead.RemoveListener(Die); ClearEffects(); visual.color = baseColor; }
    void SetState(ActionState state) { State = state; timer = 0; struck = released = false; }
    void BeginAttack(Ability ability)
    {
        if (targetBody != null) facing = targetBody.bounds.center.x >= body.position.x ? 1 : -1;
        visual.flipX = facing < 0;
        CurrentAbility = ability; lockedAim = targetBody != null ? (Vector2)targetBody.bounds.center : body.position + Vector2.right * facing * 2;
        victims = new HashSet<Character>(); awakened = true;
        if (ability == Ability.Main) mainReady = Time.time + 6;
        if (ability == Ability.Secondary) secondaryReady = Time.time + 8;
        body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y); SetState(ActionState.Windup);
    }
    void Update()
    {
        timer += Time.deltaTime; Animate(); if (IsDead) return;
        effects.RemoveAll(e => e == null);
        if (Time.time >= nextScan)
        {
            if (target == null) target = FindObjectOfType<PlayerController>();
            targetBody = target != null ? target.GetComponent<CapsuleCollider2D>() : null; nextScan = Time.time + .3f;
        }
        bool alive = target != null && targetBody != null && !target.isDead && target.gameObject.activeInHierarchy;
        if (State == ActionState.Hurt)
        {
            if (timer >= .35f) { if (Ambusher && awakened && alive) BeginAttack(Ability.Basic); else SetState(Flying ? ActionState.Return : Ambusher || Stationary ? ActionState.Idle : ActionState.Patrol); }
            return;
        }
        if (State == ActionState.Stunned) { if (timer >= 1.3f) Recover(1); return; }
        if (State == ActionState.Recover)
        {
            float duration = RecoveryDuration();
            if (timer >= duration) SetState(Flying ? ActionState.Return : Ambusher || Stationary ? ActionState.Idle : ActionState.Patrol);
            return;
        }
        if (State == ActionState.Return) { if (Vector2.Distance(body.position, home) < .2f) SetState(ActionState.Idle); return; }
        if (State == ActionState.Windup)
        {
            if (!alive) { Recover(1); return; }
            if (timer >= Windup) { attackStart = body.position; SetState(ActionState.Attack); }
            return;
        }
        if (State == ActionState.Attack) { PerformAttack(alive); return; }
        bool seen = alive && Vector2.Distance(hitbox.bounds.center, targetBody.bounds.center) < detectionRadius &&
            Vector2.Distance(home, target.transform.position) < leashRadius &&
            (Flying || Mathf.Abs(hitbox.bounds.center.y - targetBody.bounds.center.y) < 2.6f) &&
            !Physics2D.Linecast(hitbox.bounds.center, targetBody.bounds.center, groundLayer);
        if (!seen) { if (State == ActionState.Chase) SetState(ActionState.Patrol); return; }
        facing = target.transform.position.x >= body.position.x ? 1 : -1; visual.flipX = facing < 0;
        if (Time.time < nextAttack) return;
        float distance = Mathf.Abs(target.transform.position.x - body.position.x);
        if (IsElite && health.CurrentHp <= health.maxHp * .5f && Time.time >= secondaryReady)
        { BeginAttack(Ability.Secondary); return; }
        if (IsElite && cycles > 0 && Time.time >= mainReady && distance < 4.5f)
        { BeginAttack(Ability.Main); return; }
        float range = Flying || Stationary ? detectionRadius : Ambusher ? 3.5f : IsElite ? 2.1f : 1.8f;
        if (distance <= range) { cycles++; BeginAttack(Ability.Basic); }
        else if (!Flying && !Stationary && !Ambusher && State != ActionState.Chase) SetState(ActionState.Chase);
    }
    float RecoveryDuration()
    {
        if (kind == Kind.SoulboundJailer) return CurrentAbility == Ability.Main ? 1.4f : CurrentAbility == Ability.Secondary ? 1 : .9f;
        if (kind == Kind.SandPincerGuard) return CurrentAbility == Ability.Secondary ? 1.2f : .9f;
        return kind == Kind.SandJawWorm || Flying ? 1.2f : kind == Kind.ChainAmbushBeast ? 1 : Stationary ? 1.5f : .8f;
    }
    void PerformAttack(bool alive)
    {
        if (!alive) { Recover(1); return; }
        if (kind == Kind.ThornballCactus || Flying)
        {
            if (!released && timer >= .12f)
            {
                released = true;
                if (Flying) lockedAim = targetBody.bounds.center;
                if (Flying) Fire(DesertDungeonProjectile.Style.SoulFire, 9, 5, 3.2f, 0, 0);
                else for (int i = -1; i <= 1; i++) Fire(DesertDungeonProjectile.Style.Thorn, 7, 5, 5, i * 12, 0);
            }
            if (timer > .4f) Recover(1.5f); return;
        }
        if (kind == Kind.SandPincerGuard && CurrentAbility == Ability.Secondary)
        {
            if (!released && timer >= .12f)
            {
                released = true;
                for (int i = 0; i < 3; i++) Fire(DesertDungeonProjectile.Style.SandFan, 10, 3, 4.5f, 0, i);
            }
            if (timer > .45f) Recover(1.5f); return;
        }
        if (kind == Kind.SoulboundJailer && CurrentAbility == Ability.Secondary)
        {
            if (!released && timer >= .12f) { released = true; Fire(DesertDungeonProjectile.Style.SoulChain, 8, 4, 6, 0, 0); }
            if (timer > .45f) Recover(2.2f); return;
        }
        if (kind == Kind.SoulboundJailer && CurrentAbility == Ability.Main)
        {
            if (!released && timer >= .16f)
            {
                released = true; damageSource.damage = 22;
                Hit((Vector2)hitbox.bounds.center + Vector2.right * facing, new Vector2(2, 1.7f));
                Fire(DesertDungeonProjectile.Style.SoulWave, 12, 3, 4.5f, 0, 0);
            }
            if (timer > .6f) Recover(1.8f); return;
        }
        if (Ambusher || Charging)
        {
            damageSource.damage = Charging ? 18 : kind == Kind.SandJawWorm ? 12 : 11;
            if (!struck) struck = Hit(hitbox.bounds.center, hitbox.bounds.size + Vector3.one * .14f);
            float distance = Charging ? 3.5f : kind == Kind.SandJawWorm ? 2 : 2.5f;
            if (Mathf.Abs(body.position.x - attackStart.x) >= distance || timer >= (Charging ? .6f : .55f)) Recover(1.5f);
            return;
        }
        damageSource.damage = kind == Kind.RedtailScorpion ? 10 : kind == Kind.RustSkeleton ? 12 : kind == Kind.SandPincerGuard ? 14 : 15;
        if (!struck && timer >= .14f && timer < .4f) struck = Hit((Vector2)hitbox.bounds.center + Vector2.right * facing * .75f, new Vector2(1.8f, 1.5f));
        if (timer > .5f) Recover(1.3f);
    }
    void FixedUpdate()
    {
        if (IsDead || State == ActionState.Hurt) return;
        if (Flying)
        {
            Vector2 velocity = State == ActionState.Return ? Vector2.ClampMagnitude((home - body.position) * 2, 2) :
                State == ActionState.Idle ? ((home + Vector2.up * Mathf.Sin(Time.time * 1.5f) * .2f) - body.position) * 2 : Vector2.zero;
            if (velocity.sqrMagnitude > .001f && Physics2D.BoxCast(hitbox.bounds.center, hitbox.bounds.size * .9f, 0, velocity.normalized, velocity.magnitude * Time.fixedDeltaTime + .05f, groundLayer)) velocity = Vector2.zero;
            body.velocity = velocity; return;
        }
        float speed = 0;
        if (!Stationary && !Ambusher && State == ActionState.Patrol)
        { if (Mathf.Abs(body.position.x - home.x) > patrolRadius) facing = home.x > body.position.x ? 1 : -1; speed = .55f; }
        else if (State == ActionState.Chase) speed = IsElite ? .9f : 1.1f;
        else if (State == ActionState.Attack && (Ambusher || Charging)) speed = Charging ? 7 : 5;
        if (speed > 0 && !SafeAhead(facing))
        {
            speed = 0;
            if (State == ActionState.Attack)
            {
                if (Charging && WallAhead(facing)) SetState(ActionState.Stunned); else Recover(1.5f);
            }
            else if (State == ActionState.Patrol) facing = -facing;
        }
        body.velocity = new Vector2(facing * speed, body.velocity.y); if (speed > 0) visual.flipX = facing < 0;
    }
    bool WallAhead(int direction) { return Physics2D.Raycast(hitbox.bounds.center, Vector2.right * direction, hitbox.bounds.extents.x + .3f, groundLayer); }
    bool SafeAhead(int direction)
    {
        return !WallAhead(direction) && Physics2D.Raycast(new Vector2(hitbox.bounds.center.x + direction * (hitbox.bounds.extents.x + .3f), hitbox.bounds.min.y + .25f), Vector2.down, .8f, groundLayer);
    }
    bool Hit(Vector2 point, Vector2 size)
    {
        int count = Physics2D.OverlapBoxNonAlloc(point, size, 0, contacts, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            if (!(contacts[i] is CapsuleCollider2D) || contacts[i].isTrigger) continue;
            var p = contacts[i].GetComponentInParent<PlayerController>(); if (p == null || p.isDead) continue;
            var hp = p.GetComponent<Character>(); if (victims != null && victims.Contains(hp)) continue;
            float before = hp.CurrentHp; hp.TakeDamage(damageSource);
            if (hp.CurrentHp < before) { victims?.Add(hp); return true; }
        }
        return false;
    }
    void Fire(DesertDungeonProjectile.Style style, int damage, float range, float speed, float angle, int fan)
    {
        var go = new GameObject(kind + " " + style);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        Vector2 origin = hitbox.bounds.center + Vector3.right * facing * (hitbox.bounds.extents.x + .12f);
        bool ground = style == DesertDungeonProjectile.Style.SoulWave || style == DesertDungeonProjectile.Style.SandFan;
        if (ground)
        {
            origin.y = hitbox.bounds.min.y + .04f;
            var floor = Physics2D.Raycast(origin + Vector2.up * .5f, Vector2.down, 1, groundLayer);
            if (!floor) { Destroy(go); return; } origin.y = floor.point.y + .04f;
        }
        go.transform.position = origin;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 1;
        var source = go.AddComponent<Attack>(); source.enabled = false; source.damage = damage;
        var projectile = go.AddComponent<DesertDungeonProjectile>(); projectile.owner = this; projectile.style = style; projectile.frames = effectFrames;
        projectile.damageSource = source; projectile.maxRange = range; projectile.groundLayer = groundLayer; projectile.victims = victims;
        Vector2 direction = (lockedAim - origin).normalized; if (direction.sqrMagnitude < .1f) direction = Vector2.right * facing;
        projectile.velocity = ground ? new Vector2(facing * speed * (1 - fan * .13f), 0) : (Vector2)(Quaternion.Euler(0, 0, angle) * direction) * speed;
        projectile.fanLane = fan; effects.Add(go);
    }
    void Recover(float cooldown)
    { nextAttack = Time.time + cooldown + RecoveryDuration(); body.velocity = new Vector2(0, Flying ? 0 : body.velocity.y); SetState(ActionState.Recover); }
    void Hurt(Transform source)
    {
        if (IsDead || SkillArmor) return;
        awakened = true; SetState(ActionState.Hurt); nextAttack = Time.time + .7f;
        body.velocity = Stationary || Flying ? Vector2.zero : new Vector2((source != null ? Mathf.Sign(body.position.x - source.position.x) : -facing) * 1.7f, 1);
    }
    void ClearEffects() { foreach (var e in effects) if (e != null) Destroy(e); effects.Clear(); }
    void Die() { if (IsDead) return; SetState(ActionState.Dead); ClearEffects(); body.velocity = Vector2.zero; body.simulated = false; hitbox.enabled = false; Destroy(gameObject, 1.3f); }
    void Animate()
    {
        if (frames == null || frames.Length < (IsElite ? 32 : 24)) return;
        int row;
        if (IsElite) row = IsDead ? 7 : State == ActionState.Hurt || State == ActionState.Stunned ? 6 : CurrentAbility == Ability.Secondary && (State == ActionState.Windup || State == ActionState.Attack) ? 5 :
            CurrentAbility == Ability.Main && State == ActionState.Windup ? 3 : CurrentAbility == Ability.Main && State == ActionState.Attack ? 4 : State == ActionState.Windup || State == ActionState.Attack ? 2 : State == ActionState.Patrol || State == ActionState.Chase ? 1 : 0;
        else row = IsDead ? 5 : State == ActionState.Hurt ? 4 : State == ActionState.Windup ? 2 : State == ActionState.Attack ? 3 : State == ActionState.Patrol || State == ActionState.Chase || State == ActionState.Return ? 1 : 0;
        int index = (int)(timer * (IsDead ? 4 : 8)); if (IsElite && State == ActionState.Windup && CurrentAbility == Ability.Basic) index = 0;
        visual.sprite = frames[row * 4 + (row < 2 ? index % 4 : Mathf.Min(3, index))];
        visual.color = State == ActionState.Windup ? Color.Lerp(baseColor, kind <= Kind.ThornballCactus || kind == Kind.SandPincerGuard ? new Color(1, .6f, .25f) : new Color(.45f, .9f, 1), .2f + timer * .3f) : baseColor;
    }
}
