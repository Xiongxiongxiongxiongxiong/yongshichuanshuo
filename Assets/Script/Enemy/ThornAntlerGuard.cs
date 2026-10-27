using UnityEngine;
[RequireComponent(typeof(Character),typeof(Rigidbody2D),typeof(BoxCollider2D))]
public class ThornAntlerGuard : MonoBehaviour
{
    public enum CombatState { Patrol, Chase, MeleeWindup, Melee, ChargeWindup, Charge, RootWindup, Root, Recover, Stunned, Hurt, Dead }
    public Sprite[] frames;
    public Sprite[] rootFrames;
    public Attack meleeDamage, chargeDamage;
    public LayerMask groundLayer = 1 << 6;
    public float detectionRadius=8, patrolRadius=1.5f;
    public CombatState State { get; private set; }
    public bool HasSkillArmor => State==CombatState.ChargeWindup||State==CombatState.Charge||State==CombatState.RootWindup||State==CombatState.Root;
    Character health; Rigidbody2D body; BoxCollider2D box; SpriteRenderer visual;
    PlayerController target; CapsuleCollider2D targetBox;
    Vector2 home, chargeStart, rootPoint;
    float timer,nextAttack,nextScan; int facing=1,cycle; bool struck,rootPlaced;
    readonly Collider2D[] contacts=new Collider2D[16];
    readonly System.Collections.Generic.List<GameObject> roots=new System.Collections.Generic.List<GameObject>();
    void Awake(){health=GetComponent<Character>();body=GetComponent<Rigidbody2D>();box=GetComponent<BoxCollider2D>();visual=GetComponent<SpriteRenderer>();home=body.position;body.freezeRotation=true;}
    void OnEnable(){health.OnTakeDamage.AddListener(Hurt);health.OnDead.AddListener(Die);}
    void OnDisable(){health.OnTakeDamage.RemoveListener(Hurt);health.OnDead.RemoveListener(Die);ClearRoots();}
    void Set(CombatState state){State=state;timer=0;struck=false;rootPlaced=false;}
    void Update()
    {
        timer+=Time.deltaTime;Animate();if(State==CombatState.Dead)return;
        roots.RemoveAll(r=>r==null);
        if(State==CombatState.Hurt||State==CombatState.Recover||State==CombatState.Stunned){if(timer>=(State==CombatState.Hurt?.35f:State==CombatState.Stunned?1.3f:.85f))Set(CombatState.Patrol);return;}
        if(Time.time>=nextScan){if(target==null)target=FindObjectOfType<PlayerController>();targetBox=target!=null?target.GetComponent<CapsuleCollider2D>():null;nextScan=Time.time+.25f;}
        bool alive=target!=null&&targetBox!=null&&!target.isDead&&target.gameObject.activeInHierarchy;
        if(State==CombatState.MeleeWindup||State==CombatState.ChargeWindup||State==CombatState.RootWindup)
        {
            if(!alive){Recover();return;}
            float delay=State==CombatState.MeleeWindup?.45f:State==CombatState.ChargeWindup?.7f:.35f;
            if(timer>=delay){var next=State==CombatState.MeleeWindup?CombatState.Melee:State==CombatState.ChargeWindup?CombatState.Charge:CombatState.Root;chargeStart=body.position;Set(next);}
            return;
        }
        if(State==CombatState.Melee){if(timer>=.16f&&timer<.4f&&!struck)struck=Damage((Vector2)box.bounds.center+Vector2.right*facing*.8f,new Vector2(1.7f,1.6f),meleeDamage);if(timer>.55f)Recover();return;}
        if(State==CombatState.Charge){if(!struck)struck=Damage((Vector2)box.bounds.center+Vector2.right*facing*.55f,new Vector2(1.7f,1.5f),chargeDamage);if(Mathf.Abs(body.position.x-chargeStart.x)>=4||timer>.8f)Recover();return;}
        if(State==CombatState.Root){if(!rootPlaced){PlaceRoot();rootPlaced=true;}if(timer>.5f)Recover();return;}
        bool seen=alive&&Vector2.Distance(box.bounds.center,targetBox.bounds.center)<detectionRadius&&Mathf.Abs(home.x-target.transform.position.x)<11&&Mathf.Abs(box.bounds.center.y-targetBox.bounds.center.y)<2.8f&&!Physics2D.Linecast(box.bounds.center,targetBox.bounds.center,groundLayer);
        if(!seen){if(State!=CombatState.Patrol)Set(CombatState.Patrol);return;}
        facing=target.transform.position.x>=body.position.x?1:-1;visual.flipX=facing<0;
        if(Time.time>=nextAttack){int choice=cycle%3;if(choice==0&&Mathf.Abs(target.transform.position.x-body.position.x)>2){if(State!=CombatState.Chase)Set(CombatState.Chase);return;}cycle++;rootPoint=targetBox.bounds.center;Set(choice==0?CombatState.MeleeWindup:choice==1?CombatState.ChargeWindup:CombatState.RootWindup);}
        else if(State!=CombatState.Chase)Set(CombatState.Chase);
    }
    bool GroundAhead(int dir){return Physics2D.Raycast(new Vector2(box.bounds.center.x+dir*(box.bounds.extents.x+.2f),box.bounds.min.y+.25f),Vector2.down,.8f,groundLayer);}
    bool WallAhead(int dir,float distance){return Physics2D.Raycast(box.bounds.center,Vector2.right*dir,box.bounds.extents.x+distance,groundLayer);}
    void FixedUpdate()
    {
        if(State==CombatState.Dead||State==CombatState.Hurt)return;
        float speed=0;
        if(State==CombatState.Charge){if(WallAhead(facing,6*Time.fixedDeltaTime+.12f)){body.velocity=new Vector2(0,body.velocity.y);nextAttack=Time.time+2;Set(CombatState.Stunned);return;}if(!GroundAhead(facing)){Recover();return;}speed=6;}
        else if(State==CombatState.Patrol){if(Mathf.Abs(body.position.x-home.x)>patrolRadius)facing=home.x>body.position.x?1:-1;speed=.5f;}
        else if(State==CombatState.Chase)speed=1.1f;
        if(speed>0&&State!=CombatState.Charge&&(!GroundAhead(facing)||WallAhead(facing,.15f))){speed=0;if(State==CombatState.Patrol)facing=-facing;}
        body.velocity=new Vector2(speed*facing,body.velocity.y);if(speed>0)visual.flipX=facing<0;
    }
    bool Damage(Vector2 center,Vector2 size,Attack source)
    {
        int n=Physics2D.OverlapBoxNonAlloc(center,size,0,contacts,1<<7);
        for(int i=0;i<n;i++){if(!(contacts[i] is CapsuleCollider2D)||contacts[i].isTrigger)continue;var p=contacts[i].GetComponentInParent<PlayerController>();if(p==null||p.isDead)continue;var hp=p.GetComponent<Character>();float before=hp.CurrentHp;hp.TakeDamage(source);if(hp.CurrentHp<before)return true;}return false;
    }
    void PlaceRoot()
    {
        var floor=Physics2D.Raycast(rootPoint+Vector2.up*.2f,Vector2.down,5,groundLayer);if(!floor)return;
        var go=new GameObject("Thorn Root Ambush");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);go.transform.position=floor.point+Vector2.up*.03f;
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sortingLayerID=visual.sortingLayerID;renderer.sortingOrder=visual.sortingOrder+2;
        var a=go.AddComponent<Attack>();a.enabled=false;a.damage=12;
        var r=go.AddComponent<ThornRootAmbush>();r.owner=health;r.damage=a;r.frames=rootFrames;roots.Add(go);
    }
    void Recover(){nextAttack=Time.time+1.4f;body.velocity=new Vector2(0,body.velocity.y);Set(CombatState.Recover);}
    void Hurt(Transform source){if(State==CombatState.Dead||HasSkillArmor)return;Set(CombatState.Hurt);nextAttack=Time.time+.6f;body.velocity=new Vector2(source!=null?Mathf.Sign(body.position.x-source.position.x)*1.8f:-facing*1.8f,1);}
    void ClearRoots(){foreach(var r in roots)if(r!=null)Destroy(r);roots.Clear();}
    void Die(){if(State==CombatState.Dead)return;Set(CombatState.Dead);ClearRoots();body.velocity=Vector2.zero;body.simulated=false;box.enabled=false;Destroy(gameObject,1.3f);}
    void Animate(){if(frames==null||frames.Length<32)return;int row=State==CombatState.Dead?7:State==CombatState.Hurt||State==CombatState.Stunned?6:State==CombatState.ChargeWindup?3:State==CombatState.Charge?4:State==CombatState.RootWindup||State==CombatState.Root?5:State==CombatState.MeleeWindup||State==CombatState.Melee?2:State==CombatState.Patrol||State==CombatState.Chase?1:0;int f=Mathf.FloorToInt(timer*7);if(State==CombatState.MeleeWindup)f=0;visual.sprite=frames[row*4+(row<2?f%4:Mathf.Min(3,f))];}
}
