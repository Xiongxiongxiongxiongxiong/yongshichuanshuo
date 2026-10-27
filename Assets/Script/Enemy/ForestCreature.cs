using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class ForestCreature : MonoBehaviour
{
    public bool ranged;
    public Sprite[] frames;
    public Sprite projectileSprite;
    public Attack damageSource;
    public LayerMask groundLayer = 1 << 6;
    public float patrolRadius = 2f;
    private Character health;
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private BoxCollider2D box;
    private PlayerController target;
    private Vector2 home;
    private float timer, nextAttack, nextSearch;
    private int row, facing = 1;
    private bool fired, dead;

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>(); box = GetComponent<BoxCollider2D>();
        home = body.position;
    }
    private void OnEnable()
    {
        health.OnTakeDamage.AddListener(Hurt); health.OnDead.AddListener(Die);
    }
    private void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(Hurt); health.OnDead.RemoveListener(Die);
    }
    private void SetState(int value)
    {
        row = value; timer = 0; fired = false;
    }
    private void Update()
    {
        timer += Time.deltaTime;
        if (frames != null && frames.Length >= 20)
        {
            int index = Mathf.FloorToInt(timer * 8);
            visual.sprite = frames[row * 4 + (row < 2 ? index % 4 : Mathf.Min(3, index))];
        }
        if (dead) return;
        if (row == 3) { if (timer > 0.4f) SetState(0); return; }
        if (row == 2)
        {
            if (!fired && timer > 0.45f)
            {
                fired = true;
                if (ranged) FireSpore();
                else body.velocity = new Vector2(facing * 5f, 5f);
            }
            if (!ranged && fired) DamageNearbyPlayer();
            if (timer > 1.05f) { nextAttack = Time.time + (ranged ? 2.5f : 2.2f); SetState(0); }
            return;
        }
        if (Time.time >= nextSearch)
        {
            target = FindObjectOfType<PlayerController>(); nextSearch = Time.time + 0.4f;
        }
        bool seen = target != null && !target.isDead && Vector2.Distance(body.position, target.transform.position) < 7 &&
            !Physics2D.Linecast(box.bounds.center, target.GetComponent<Collider2D>().bounds.center, groundLayer);
        if (seen)
        {
            facing = target.transform.position.x >= transform.position.x ? 1 : -1;
            if (Time.time >= nextAttack && Mathf.Abs(target.transform.position.x - transform.position.x) < (ranged ? 5 : 3))
            { SetState(2); body.velocity = new Vector2(0, body.velocity.y); }
            else if (row != 1) SetState(1);
        }
        else
        {
            if (Mathf.Abs(body.position.x - home.x) > patrolRadius) facing = body.position.x > home.x ? -1 : 1;
            if (row != 1) SetState(1);
        }
        visual.flipX = facing < 0;
    }
    private void FixedUpdate()
    {
        if (dead || row == 3 || row == 2) return;
        Vector2 probe = new Vector2(box.bounds.center.x + facing * (box.bounds.extents.x + 0.25f), box.bounds.min.y + 0.2f);
        bool floor = Physics2D.Raycast(probe, Vector2.down, 0.65f, groundLayer);
        bool wall = Physics2D.Raycast(box.bounds.center, Vector2.right * facing, box.bounds.extents.x + 0.2f, groundLayer);
        if (!floor || wall) { facing = -facing; body.velocity = new Vector2(0, body.velocity.y); return; }
        float speed = ranged ? 0.55f : 1.1f;
        body.velocity = new Vector2(facing * speed, body.velocity.y);
    }
    private void DamageNearbyPlayer()
    {
        foreach (var c in Physics2D.OverlapBoxAll(box.bounds.center, box.bounds.size + Vector3.one * 0.25f, 0))
        {
            var p = c.GetComponentInParent<PlayerController>();
            if (p == null || p.isDead) continue;
            p.GetComponent<Character>().TakeDamage(damageSource);
        }
    }
    private void FireSpore()
    {
        var go = new GameObject("Mushroom Spore"); go.transform.position = box.bounds.center + Vector3.right * facing * 0.8f;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = projectileSprite; sr.color = Color.white;
        sr.sortingLayerID = visual.sortingLayerID; sr.sortingOrder = visual.sortingOrder + 1;
        var projectile = go.AddComponent<ForestSpore>(); projectile.direction = facing; projectile.source = damageSource;
        projectile.groundLayer = groundLayer;
    }
    private void Hurt(Transform attacker)
    {
        if (dead) return; SetState(3);
        body.velocity = new Vector2(Mathf.Sign(transform.position.x - attacker.position.x) * 2.5f, 1f);
    }
    private void Die()
    {
        if (dead) return; dead = true; SetState(4); body.velocity = Vector2.zero;
        box.enabled = false; body.simulated = false; Destroy(gameObject, 0.85f);
    }
}

public class ForestSpore : MonoBehaviour
{
    public int direction;
    public Attack source;
    public LayerMask groundLayer;
    private float age;
    private void Update()
    {
        float distance = 4.5f * Time.deltaTime;
        if (Physics2D.Raycast(transform.position, Vector2.right * direction, distance + 0.1f, groundLayer))
        { Destroy(gameObject); return; }
        transform.position += Vector3.right * direction * distance;
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, 0.18f))
        {
            var p = c.GetComponentInParent<PlayerController>();
            if (p == null || source == null) continue;
            p.GetComponent<Character>().TakeDamage(source); Destroy(gameObject); return;
        }
        age += Time.deltaTime; if (age >= 3 || source == null) Destroy(gameObject);
    }
}
