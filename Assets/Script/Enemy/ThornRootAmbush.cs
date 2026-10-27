using UnityEngine;
public class ThornRootAmbush : MonoBehaviour
{
    public Character owner;
    public Attack damage;
    public Sprite[] frames;
    public float WarningDuration=.9f;
    float age;bool hit;
    readonly Collider2D[] contacts=new Collider2D[12];
    void Update()
    {
        age+=Time.deltaTime;
        if(owner==null||owner.CurrentHp<=0||age>WarningDuration+.55f){Destroy(gameObject);return;}
        var renderer=GetComponent<SpriteRenderer>();
        bool active=age>=WarningDuration;
        if(frames!=null&&frames.Length>=8)renderer.sprite=frames[active?4+Mathf.Min(3,(int)((age-WarningDuration)*8)):(int)(age*8)%4];
        renderer.color=active?Color.white:new Color(1,1,1,.6f+.35f*Mathf.Sin(age*22));
        if(!active||hit)return;
        int n=Physics2D.OverlapBoxNonAlloc((Vector2)transform.position+Vector2.up*.4f,new Vector2(1.5f,.8f),0,contacts,1<<7);
        for(int i=0;i<n;i++){if(!(contacts[i] is CapsuleCollider2D)||contacts[i].isTrigger)continue;var p=contacts[i].GetComponentInParent<PlayerController>();if(p==null||p.isDead)continue;var hp=p.GetComponent<Character>();float before=hp.CurrentHp;hp.TakeDamage(damage);if(hp.CurrentHp<before){var slow=p.GetComponent<PlayerRootSlow>();if(slow==null)slow=p.gameObject.AddComponent<PlayerRootSlow>();slow.Apply(1);hit=true;break;}}
    }
}
