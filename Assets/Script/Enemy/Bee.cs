using UnityEngine;

// Flying enemies share Character/Attack with the boar, but do not use its ground checks.
[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bee : MonoBehaviour
{
    public enum FlightState { Patrol, Chase, Attack, Hurt, Return, Dead }
    [Header("动画")]
    public Sprite[] flyFrames;
    public Sprite[] attackFrames;
    public Sprite[] hitFrames;
    public float animationFps = 10f;
    [Header("飞行与发现玩家")]
    public float patrolSpeed = 1.4f;
    public float chaseSpeed = 3.2f;
    public float patrolWidth = 2f;
    public float hoverHeight = 0.25f;
    public float detectionRadius = 5f;
    public float loseRadius = 8f;
    public float leashRadius = 10f;
    public float lostTargetDelay = 1.5f;
    public LayerMask playerLayer = 1 << 7;
    public LayerMask groundLayer = 1 << 6;
    [Header("攻击")]
    public Attack damageSource;
    public float attackRange = 1.2f;
    public float hitRadius = 0.65f;
    public float attackWindup = 0.18f;
    public float attackActiveTime = 0.18f;
    public float attackDuration = 0.6f;
    public float attackCooldown = 1.2f;
    public float attackSpeed = 4.5f;
    [Header("受击与死亡")]
    public float hurtDuration = 0.4f;
    public float knockbackSpeed = 3f;
    public float deathDuration = 1.2f;
    public VoidEventSO cameraShakeEvent;

    public FlightState State { get; private set; }
    public Character Target { get { return target; } }
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private Character health;
    private CircleCollider2D hitbox;
    private Character target;
    private Collider2D targetCollider;
    private Vector2 home;
    private Vector2 attackDirection;
    private Vector2 knockback;
    private float enteredAt;
    private float nextAttackAt;
    private float unseenTime;
    private float nextScanAt;
    private bool dealtDamage;
    private int patrolDirection = 1;
    private readonly Collider2D[] detectionResults = new Collider2D[16];

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        health = GetComponent<Character>();
        hitbox = GetComponent<CircleCollider2D>();
        body.gravityScale = 0;
        body.freezeRotation = true;
        home = body.position;
        health.CurrentHp = health.maxHp;
        ChangeState(FlightState.Patrol);
    }

    private void OnEnable()
    {
        health.OnTakeDamage.AddListener(OnTakeDamage);
        health.OnDead.AddListener(OnDeath);
    }

    private void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(OnTakeDamage);
        health.OnDead.RemoveListener(OnDeath);
    }

    private void ChangeState(FlightState state)
    {
        State = state;
        enteredAt = Time.time;
        body.velocity = Vector2.zero;
    }

    private Vector2 TargetPosition()
    {
        return targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)target.transform.position;
    }

    private bool CanSeeTarget()
    {
        return target != null && target.CurrentHp > 0 && target.gameObject.activeInHierarchy
            && targetCollider != null && !Physics2D.Linecast(body.position, TargetPosition(), groundLayer);
    }

    private void FindTarget()
    {
        if (Time.time < nextScanAt) return;
        nextScanAt = Time.time + 0.15f;
        int count = Physics2D.OverlapCircleNonAlloc(body.position, detectionRadius, detectionResults, playerLayer);
        for (int i = 0; i < count; i++)
        {
            var candidate = detectionResults[i];
            var player = candidate.GetComponent<PlayerController>();
            var character = candidate.GetComponent<Character>();
            if (player == null || character == null || character.CurrentHp <= 0) continue;
            if (Physics2D.Linecast(body.position, candidate.bounds.center, groundLayer)) continue;
            target = character;
            targetCollider = candidate;
            unseenTime = 0;
            ChangeState(FlightState.Chase);
            break;
        }
    }

    private void Update()
    {
        float elapsed = Time.time - enteredAt;
        if (State == FlightState.Dead)
        {
            PlayFrames(hitFrames, elapsed, false);
            var color = visual.color;
            color.a = Mathf.Clamp01(1f - (elapsed - 0.35f) / Mathf.Max(0.1f, deathDuration - 0.35f));
            visual.color = color;
            return;
        }
        if (State == FlightState.Hurt)
        {
            PlayFrames(hitFrames, elapsed, false);
            if (elapsed >= hurtDuration)
                ChangeState(CanSeeTarget() ? FlightState.Chase : FlightState.Return);
            return;
        }
        if (State == FlightState.Attack)
        {
            PlayFrames(attackFrames, elapsed, false);
            if (!dealtDamage && elapsed >= attackWindup && elapsed < attackWindup + attackActiveTime
                && CanSeeTarget() && Vector2.Distance(body.position, targetCollider.ClosestPoint(body.position)) <= hitRadius)
            {
                dealtDamage = true;
                if (damageSource != null) target.TakeDamage(damageSource);
            }
            if (elapsed >= attackDuration)
                ChangeState(CanSeeTarget() ? FlightState.Chase : FlightState.Return);
            return;
        }

        PlayFrames(flyFrames, Time.time, true);
        if (State == FlightState.Patrol) FindTarget();
        if (State == FlightState.Chase)
        {
            if (!CanSeeTarget() || Vector2.Distance(home, body.position) > leashRadius
                || Vector2.Distance(body.position, TargetPosition()) > loseRadius)
                unseenTime += Time.deltaTime;
            else unseenTime = 0;
            if (target == null || target.CurrentHp <= 0 || unseenTime >= lostTargetDelay)
            {
                target = null;
                targetCollider = null;
                ChangeState(FlightState.Return);
            }
            else if (CanSeeTarget() && Time.time >= nextAttackAt
                && Vector2.Distance(body.position, targetCollider.ClosestPoint(body.position)) <= attackRange)
            {
                attackDirection = (TargetPosition() - body.position).normalized;
                dealtDamage = false;
                nextAttackAt = Time.time + attackDuration + attackCooldown;
                ChangeState(FlightState.Attack);
            }
        }
    }

    private void FixedUpdate()
    {
        if (State == FlightState.Dead) return;
        if (State == FlightState.Hurt)
        {
            MoveSafely(knockback * Mathf.Clamp01(1f - (Time.time - enteredAt) / hurtDuration));
            return;
        }
        if (State == FlightState.Attack)
        {
            float elapsed = Time.time - enteredAt;
            MoveSafely(elapsed >= attackWindup && elapsed < attackWindup + attackActiveTime
                ? attackDirection * attackSpeed : Vector2.zero);
            return;
        }
        if (State == FlightState.Chase && target != null)
        {
            Vector2 delta = TargetPosition() - body.position;
            MoveSafely(delta.normalized * (delta.magnitude > hitRadius ? chaseSpeed : 0));
            return;
        }
        if (State == FlightState.Return)
        {
            if (Vector2.Distance(body.position, home) < 0.15f) ChangeState(FlightState.Patrol);
            else MoveSafely((home - body.position).normalized * patrolSpeed);
            return;
        }
        if (Mathf.Abs(body.position.x - home.x) >= patrolWidth)
            patrolDirection = body.position.x > home.x ? -1 : 1;
        float desiredY = home.y + Mathf.Sin(Time.time * 2f) * hoverHeight;
        MoveSafely(new Vector2(patrolDirection * patrolSpeed, Mathf.Clamp((desiredY - body.position.y) * 3f, -1f, 1f)));
    }

    private void MoveSafely(Vector2 velocity)
    {
        if (velocity.sqrMagnitude > 0.001f)
        {
            float radius = hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            var hit = Physics2D.CircleCast(body.position, radius, velocity.normalized,
                velocity.magnitude * Time.fixedDeltaTime + 0.08f, groundLayer);
            if (hit.collider != null)
            {
                velocity = Vector2.zero;
                if (State == FlightState.Patrol) patrolDirection *= -1;
            }
            // Source art faces left.
            if (Mathf.Abs(velocity.x) > 0.05f) visual.flipX = velocity.x > 0;
        }
        body.velocity = velocity;
    }

    private void PlayFrames(Sprite[] frames, float elapsed, bool loop)
    {
        if (frames == null || frames.Length == 0) return;
        int index = Mathf.Max(0, Mathf.FloorToInt(elapsed * animationFps));
        visual.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
    }

    public void OnTakeDamage(Transform attacker)
    {
        if (State == FlightState.Dead) return;
        knockback = ((Vector2)transform.position - (Vector2)attacker.position).normalized * knockbackSpeed;
        ChangeState(FlightState.Hurt);
        nextAttackAt = Time.time + hurtDuration + attackCooldown;
        if (cameraShakeEvent != null) cameraShakeEvent.RaiseEvent();
    }

    public void OnDeath()
    {
        if (State == FlightState.Dead) return;
        ChangeState(FlightState.Dead);
        hitbox.enabled = false;
        body.gravityScale = 0.8f;
        body.velocity = new Vector2(0, -0.5f);
        if (cameraShakeEvent != null) cameraShakeEvent.RaiseEvent();
        Destroy(gameObject, deathDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
