using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class MineCreature : MonoBehaviour
{
    public enum Species { CrystalCrawler, LanternBat, PickaxeImp }
    public enum ActionState { Patrol, Sleep, Chase, Windup, Attack, Recover, Return, Hurt, Dead }
    public Species species;
    [Tooltip("Six rows, four frames per row: idle, move, windup, attack, hurt, death.")]
    public Sprite[] frames;
    public Sprite oreSprite;
    public Attack damageSource;
    public LayerMask groundLayer = 1 << 6;
    public float detectionRadius = 7f;
    public float patrolRadius = 2f;
    public float leashRadius = 9f;
    public ActionState State { get; private set; }
    public bool IsDead => State == ActionState.Dead;

    private Character health;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private PlayerController target;
    private Collider2D targetBody;
    private Vector2 home, aim, diveDirection;
    private float timer, nextAttack, nextScan;
    private int facing = 1;
    private bool rangedAttack, dealtDamage, released;
    private readonly Collider2D[] hits = new Collider2D[16];
    private bool Flying => species == Species.LanternBat;

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        home = body.position;
        body.freezeRotation = true;
        body.gravityScale = Flying ? 0f : 3f;
        SetState(Flying ? ActionState.Sleep : ActionState.Patrol);
    }
    private void OnEnable()
    {
        health.OnTakeDamage.AddListener(Hurt);
        health.OnDead.AddListener(Die);
    }
    private void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(Hurt);
        health.OnDead.RemoveListener(Die);
    }
    private void SetState(ActionState state)
    {
        State = state; timer = 0f; dealtDamage = released = false;
    }
    private void Update()
    {
        timer += Time.deltaTime;
        Animate();
        if (IsDead) return;
        if (State == ActionState.Hurt)
        {
            if (timer >= 0.4f) SetState(Flying ? ActionState.Return : ActionState.Patrol);
            return;
        }
        if (Time.time >= nextScan)
        {
            if (target == null) target = FindObjectOfType<PlayerController>();
            targetBody = target != null ? target.GetComponent<CapsuleCollider2D>() : null;
            nextScan = Time.time + 0.3f;
        }
        bool alive = target != null && target.gameObject.activeInHierarchy && !target.isDead;
        bool visible = alive && targetBody != null &&
            Vector2.Distance(hitbox.bounds.center, targetBody.bounds.center) <= detectionRadius &&
            Vector2.Distance(home, target.transform.position) <= leashRadius &&
            !Physics2D.Linecast(hitbox.bounds.center, targetBody.bounds.center, groundLayer);

        if (State == ActionState.Windup)
        {
            if (!alive) { SetState(Flying ? ActionState.Return : ActionState.Patrol); return; }
            float duration = Flying ? 0.65f : species == Species.CrystalCrawler ? 0.7f : rangedAttack ? 0.85f : 0.65f;
            if (timer >= duration)
            {
                diveDirection = (aim - (Vector2)hitbox.bounds.center).normalized;
                SetState(ActionState.Attack);
            }
            return;
        }
        if (State == ActionState.Attack)
        {
            if (!alive) { FinishAttack(); return; }
            if (species == Species.PickaxeImp)
            {
                if (rangedAttack && !released && timer >= 0.16f) { released = true; ThrowOre(); }
                if (!rangedAttack && timer >= 0.15f && timer <= 0.48f)
                    DamagePlayer((Vector2)hitbox.bounds.center + Vector2.right * facing * 0.7f, new Vector2(1.6f, 1.7f));
                if (timer >= 0.65f) FinishAttack();
            }
            else
            {
                DamagePlayer(hitbox.bounds.center, (Vector2)hitbox.bounds.size + Vector2.one * 0.24f);
                if (timer >= (Flying ? 0.85f : 1.0f)) FinishAttack();
            }
            return;
        }
        if (State == ActionState.Recover)
        {
            if (timer >= (species == Species.CrystalCrawler ? 1.1f : 0.55f))
                SetState(Flying ? ActionState.Return : ActionState.Patrol);
            return;
        }
        if (State == ActionState.Return)
        {
            if (Vector2.Distance(body.position, home) < 0.2f)
                SetState(Flying ? ActionState.Sleep : ActionState.Patrol);
            return;
        }
        if (visible && (Flying || Mathf.Abs(targetBody.bounds.center.y - hitbox.bounds.center.y) < 2.4f))
        {
            facing = target.transform.position.x >= body.position.x ? 1 : -1;
            visual.flipX = facing < 0;
            float distance = Vector2.Distance(hitbox.bounds.center, targetBody.bounds.center);
            bool inRange = Flying || distance < (species == Species.CrystalCrawler ? 4f : 6f);
            if (inRange && Time.time >= nextAttack)
            {
                aim = targetBody.bounds.center;
                rangedAttack = species == Species.PickaxeImp && distance > 1.8f;
                body.velocity = new Vector2(0f, Flying ? 0f : body.velocity.y);
                SetState(ActionState.Windup);
            }
            else if (!Flying && State != ActionState.Chase) SetState(ActionState.Chase);
        }
        else if (State == ActionState.Chase)
            SetState(Vector2.Distance(body.position, home) > patrolRadius ? ActionState.Return : ActionState.Patrol);
    }
    private void FixedUpdate()
    {
        if (IsDead || State == ActionState.Hurt) return;
        if (Flying)
        {
            Vector2 velocity = Vector2.zero;
            if (State == ActionState.Return) velocity = (home - body.position).normalized * 3.2f;
            else if (State == ActionState.Attack)
            {
                velocity = diveDirection * 7f;
                if (Physics2D.Raycast(hitbox.bounds.center, diveDirection,
                    Mathf.Max(hitbox.bounds.extents.x, hitbox.bounds.extents.y) + 0.25f, groundLayer))
                { FinishAttack(); velocity = Vector2.zero; }
            }
            body.velocity = velocity;
            return;
        }
        float speed = 0f;
        if (State == ActionState.Patrol)
        {
            if (Mathf.Abs(body.position.x - home.x) > patrolRadius) facing = body.position.x > home.x ? -1 : 1;
            speed = species == Species.CrystalCrawler ? 0.8f : 0.5f;
        }
        else if (State == ActionState.Chase) speed = species == Species.CrystalCrawler ? 1.5f : 0.9f;
        else if (State == ActionState.Return)
        { facing = home.x >= body.position.x ? 1 : -1; speed = 1f; }
        else if (State == ActionState.Attack && species == Species.CrystalCrawler) speed = 7f;

        if (speed > 0f && !SafeAhead())
        {
            speed = 0f;
            if (State == ActionState.Attack) FinishAttack();
            else facing = -facing;
        }
        body.velocity = new Vector2(facing * speed, body.velocity.y);
        if (speed > 0) visual.flipX = facing < 0;
    }
    private bool SafeAhead()
    {
        float distance = hitbox.bounds.extents.x + 0.22f;
        if (Physics2D.Raycast(hitbox.bounds.center, Vector2.right * facing, distance, groundLayer)) return false;
        Vector2 foot = new Vector2(hitbox.bounds.center.x + facing * distance, hitbox.bounds.min.y + 0.18f);
        return Physics2D.Raycast(foot, Vector2.down, 0.65f, groundLayer);
    }
    private void FinishAttack()
    {
        nextAttack = Time.time + (Flying ? 2.2f : species == Species.PickaxeImp ? 1.6f : 2f);
        body.velocity = new Vector2(0f, Flying ? 0f : body.velocity.y);
        SetState(Flying ? ActionState.Return : ActionState.Recover);
    }
    private void DamagePlayer(Vector2 center, Vector2 size)
    {
        if (dealtDamage || damageSource == null) return;
        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, hits, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            if (!(hits[i] is CapsuleCollider2D) || hits[i].isTrigger) continue;
            var p = hits[i].GetComponentInParent<PlayerController>();
            if (p == null || p.isDead) continue;
            var character = p.GetComponent<Character>();
            float before = character.CurrentHp;
            character.TakeDamage(damageSource);
            if (character.CurrentHp < before) { dealtDamage = true; return; }
        }
    }
    private void ThrowOre()
    {
        if (oreSprite == null) return;
        var go = new GameObject("Imp Thrown Ore");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.position = hitbox.bounds.center + Vector3.right * facing * 0.75f;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = oreSprite;
        renderer.sortingLayerID = visual.sortingLayerID; renderer.sortingOrder = visual.sortingOrder + 1;
        float scale = 0.42f / Mathf.Max(oreSprite.bounds.size.x, oreSprite.bounds.size.y);
        go.transform.localScale = Vector3.one * scale;
        var attack = go.AddComponent<Attack>(); attack.enabled = false; attack.damage = 10;
        var projectile = go.AddComponent<MineOreProjectile>(); projectile.source = attack; projectile.owner = health;
        projectile.groundLayer = groundLayer;
        float flight = 0.8f;
        projectile.velocity = (aim - (Vector2)go.transform.position) / flight + Vector2.up * (5f * flight * 0.5f);
    }
    private void Hurt(Transform attacker)
    {
        if (IsDead) return;
        float direction = attacker != null ? Mathf.Sign(body.position.x - attacker.position.x) : -facing;
        SetState(ActionState.Hurt);
        nextAttack = Time.time + 0.7f;
        body.velocity = new Vector2(direction * 2.5f, Flying ? 1f : 1.4f);
    }
    private void Die()
    {
        if (IsDead) return;
        SetState(ActionState.Dead);
        body.velocity = Vector2.zero; body.simulated = false; hitbox.enabled = false;
        Destroy(gameObject, 1.15f);
    }
    private void Animate()
    {
        if (frames == null || frames.Length < 24) return;
        int row = IsDead ? 5 : State == ActionState.Hurt ? 4 : State == ActionState.Windup ? 2 :
            State == ActionState.Attack ? 3 : State == ActionState.Sleep ? 0 : Flying ? 1 :
            State == ActionState.Patrol || State == ActionState.Chase || State == ActionState.Return ? 1 : 0;
        int frame = Mathf.FloorToInt(timer * (IsDead ? 5f : 8f));
        bool loop = row < 2 || row == 3 && species != Species.PickaxeImp;
        visual.sprite = frames[row * 4 + (loop ? frame % 4 : Mathf.Min(3, frame))];
    }
}
