using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character), typeof(Rigidbody2D), typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class CrystalPickaxeForeman : MonoBehaviour
{
    public enum CombatState { Patrol, Chase, MeleeWindup, Melee, SlamWindup, Slam, ThrowWindup, Throw, Recover, Hurt, Dead }
    public Sprite[] frames;
    public Sprite[] effectFrames;
    public Attack meleeDamage, slamDamage;
    public LayerMask groundLayer = 1 << 6;
    public float detectionRadius = 8f;
    public float patrolRadius = 2f;
    public float leashRadius = 12f;
    public CombatState State { get; private set; }
    public bool IsDead => State == CombatState.Dead;
    public bool HasSkillArmor => State == CombatState.SlamWindup || State == CombatState.Slam ||
        State == CombatState.ThrowWindup || State == CombatState.Throw;

    private Character health;
    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private SpriteRenderer visual;
    private PlayerController target;
    private Collider2D targetBody;
    private Vector2 home, lockedAim;
    private int facing = 1, attackIndex, shots;
    private float timer, nextAttack, nextScan;
    private bool struck;
    private readonly Collider2D[] contacts = new Collider2D[16];
    private readonly List<GameObject> effects = new List<GameObject>();

    private void Awake()
    {
        health = GetComponent<Character>(); body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>(); visual = GetComponent<SpriteRenderer>();
        home = body.position; body.freezeRotation = true; SetState(CombatState.Patrol);
    }
    private void OnEnable()
    { health.OnTakeDamage.AddListener(Hurt); health.OnDead.AddListener(Die); }
    private void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(Hurt); health.OnDead.RemoveListener(Die);
        ClearEffects();
    }
    private void SetState(CombatState state)
    { State = state; timer = 0f; struck = false; shots = 0; }
    private void Update()
    {
        timer += Time.deltaTime; Animate();
        if (IsDead) return;
        effects.RemoveAll(effect => effect == null);
        if (State == CombatState.Hurt) { if (timer >= .35f) SetState(CombatState.Patrol); return; }
        if (State == CombatState.Recover) { if (timer >= 1.2f) SetState(CombatState.Patrol); return; }
        if (Time.time >= nextScan)
        {
            if (target == null) target = FindObjectOfType<PlayerController>();
            targetBody = target != null ? target.GetComponent<CapsuleCollider2D>() : null;
            nextScan = Time.time + .3f;
        }
        bool alive = target != null && targetBody != null && target.gameObject.activeInHierarchy && !target.isDead;
        if (State == CombatState.MeleeWindup || State == CombatState.SlamWindup || State == CombatState.ThrowWindup)
        {
            if (!alive) { Recover(); return; }
            float delay = State == CombatState.MeleeWindup ? .45f : State == CombatState.SlamWindup ? .9f : .7f;
            if (timer >= delay) SetState(State == CombatState.MeleeWindup ? CombatState.Melee :
                State == CombatState.SlamWindup ? CombatState.Slam : CombatState.Throw);
            return;
        }
        if (State == CombatState.Melee)
        {
            if (timer >= .24f && timer <= .5f)
                HitPlayer((Vector2)hitbox.bounds.center + Vector2.right * facing * .9f,new Vector2(1.8f,2f),meleeDamage);
            if (timer >= .6f) Recover();
            return;
        }
        if (State == CombatState.Slam)
        {
            if (!struck && timer >= .18f)
            {
                // The ground wave is a separate damage source and has a short range.
                MakeEffect(false); MakeEffect(true); struck = true;
                DamageBox((Vector2)hitbox.bounds.center + Vector2.right * facing * 1.1f,new Vector2(2.2f,2f),slamDamage);
            }
            if (timer >= .7f) Recover();
            return;
        }
        if (State == CombatState.Throw)
        {
            if (!alive) { Recover(); return; }
            if (shots < 3 && timer >= .12f + shots * .32f) { ThrowCrystal(); shots++; }
            if (timer >= 1.15f) Recover();
            return;
        }
        bool seen = alive && Vector2.Distance(hitbox.bounds.center,targetBody.bounds.center) <= detectionRadius &&
            Vector2.Distance(home,target.transform.position) < leashRadius &&
            Mathf.Abs(targetBody.bounds.center.y-hitbox.bounds.center.y)<3f &&
            !Physics2D.Linecast(hitbox.bounds.center,targetBody.bounds.center,groundLayer);
        if (!seen) { if (State != CombatState.Patrol) SetState(CombatState.Patrol); return; }
        facing = target.transform.position.x >= body.position.x ? 1 : -1; visual.flipX = facing < 0;
        float distance = Mathf.Abs(target.transform.position.x-body.position.x);
        if (Time.time >= nextAttack)
        {
            int choice = attackIndex % 3;
            if (distance > 4.5f) choice = 2;
            if (choice == 0 && distance > 2f) { if (State != CombatState.Chase) SetState(CombatState.Chase); return; }
            lockedAim = targetBody.bounds.center; attackIndex++;
            SetState(choice == 0 ? CombatState.MeleeWindup : choice == 1 ? CombatState.SlamWindup : CombatState.ThrowWindup);
        }
        else if (State != CombatState.Chase) SetState(CombatState.Chase);
    }
    private void FixedUpdate()
    {
        if (IsDead || State == CombatState.Hurt) return;
        float speed = 0;
        if (State == CombatState.Patrol)
        {
            if (Mathf.Abs(body.position.x-home.x)>patrolRadius) facing=home.x>body.position.x?1:-1;
            speed=.55f;
        }
        else if (State == CombatState.Chase) speed=1.05f;
        else if (State == CombatState.ThrowWindup && timer < .2f && SafeAhead(-facing)) speed=-1.2f;
        if (speed > 0 && !SafeAhead(facing)) { speed=0; if (State==CombatState.Patrol) facing=-facing; }
        body.velocity=new Vector2(speed*facing,body.velocity.y);
        if (speed>0) visual.flipX=facing<0;
    }
    private bool SafeAhead(int direction)
    {
        float distance=hitbox.bounds.extents.x+.2f;
        return !Physics2D.Raycast(hitbox.bounds.center,Vector2.right*direction,distance,groundLayer) &&
            Physics2D.Raycast(new Vector2(hitbox.bounds.center.x+direction*distance,hitbox.bounds.min.y+.18f),Vector2.down,.7f,groundLayer);
    }
    private void HitPlayer(Vector2 center,Vector2 size,Attack source)
    { if (!struck && DamageBox(center,size,source)) struck=true; }
    private bool DamageBox(Vector2 center,Vector2 size,Attack source)
    {
        int count=Physics2D.OverlapBoxNonAlloc(center,size,0,contacts,1<<7);
        for(int i=0;i<count;i++)
        {
            if(!(contacts[i] is CapsuleCollider2D)||contacts[i].isTrigger) continue;
            var p=contacts[i].GetComponentInParent<PlayerController>();
            if(p==null||p.isDead) continue;
            var hp=p.GetComponent<Character>(); float before=hp.CurrentHp; hp.TakeDamage(source);
            if(hp.CurrentHp<before) return true;
        }
        return false;
    }
    private void Recover()
    { nextAttack=Time.time+1.6f; body.velocity=new Vector2(0,body.velocity.y); SetState(CombatState.Recover); }
    private GameObject EffectObject(string name)
    {
        var go=new GameObject(name); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
        var renderer=go.AddComponent<SpriteRenderer>(); renderer.sortingLayerID=visual.sortingLayerID; renderer.sortingOrder=visual.sortingOrder+1;
        effects.Add(go); return go;
    }
    private void MakeEffect(bool wave)
    {
        var go=EffectObject(wave?"Foreman Ground Shockwave":"Foreman Slam Impact");
        Vector2 origin=new Vector2(body.position.x+facing*1.1f,hitbox.bounds.min.y+.04f);
        var ground=Physics2D.Raycast(origin+Vector2.up,Vector2.down,2,groundLayer);
        if(ground) origin.y=ground.point.y+.04f;
        go.transform.position=origin;
        var effect=go.AddComponent<ForemanCrystalEffect>(); effect.wave=wave; effect.owner=health;
        effect.frames=effectFrames; effect.direction=facing; effect.groundLayer=groundLayer;
        var attack=go.AddComponent<Attack>(); attack.enabled=false; attack.damage=14; effect.damage=attack;
    }
    private void ThrowCrystal()
    {
        if(effectFrames==null||effectFrames.Length<12) return;
        var go=EffectObject("Foreman Thrown Crystal");
        go.transform.position=hitbox.bounds.center+Vector3.right*facing*.95f+Vector3.up*.3f;
        var renderer=go.GetComponent<SpriteRenderer>(); renderer.sprite=effectFrames[4+shots%4];
        go.transform.localScale=Vector3.one*(.55f/Mathf.Max(renderer.sprite.bounds.size.x,renderer.sprite.bounds.size.y));
        var source=go.AddComponent<Attack>(); source.enabled=false; source.damage=10;
        var projectile=go.AddComponent<MineOreProjectile>(); projectile.source=source; projectile.owner=health; projectile.groundLayer=groundLayer;
        float flight=.85f;
        projectile.velocity=(lockedAim-(Vector2)go.transform.position)/flight+Vector2.up*(5f*flight*.5f);
    }
    private void Hurt(Transform attacker)
    {
        if(IsDead||HasSkillArmor) return;
        SetState(CombatState.Hurt); nextAttack=Time.time+.7f;
        body.velocity=new Vector2((attacker==null?-facing:Mathf.Sign(body.position.x-attacker.position.x))*1.8f,1f);
    }
    private void ClearEffects()
    { foreach(var effect in effects) if(effect!=null) Destroy(effect); effects.Clear(); }
    private void Die()
    {
        if(IsDead) return;
        SetState(CombatState.Dead); ClearEffects(); body.velocity=Vector2.zero; body.simulated=false; hitbox.enabled=false;
        Destroy(gameObject,1.3f);
    }
    private void Animate()
    {
        if(frames==null||frames.Length<32) return;
        int row=IsDead?7:State==CombatState.Hurt?6:State==CombatState.SlamWindup?3:State==CombatState.Slam?4:
            State==CombatState.ThrowWindup||State==CombatState.Throw?5:State==CombatState.MeleeWindup||State==CombatState.Melee?2:
            State==CombatState.Patrol||State==CombatState.Chase?1:0;
        int index=Mathf.FloorToInt(timer*(IsDead?4f:7f));
        if(State==CombatState.MeleeWindup) index=0;
        if(State==CombatState.ThrowWindup) index=0;
        else if(State==CombatState.Throw) index=Mathf.Min(3,Mathf.FloorToInt(timer%.32f/.32f*4));
        visual.sprite=frames[row*4+(row<2?index%4:Mathf.Min(3,index))];
    }
}
