using UnityEngine;

public class ForemanCrystalEffect : MonoBehaviour
{
    public bool wave;
    public Character owner;
    public Sprite[] frames;
    public int direction;
    public LayerMask groundLayer;
    public Attack damage;
    private float age, travelled;
    private bool hitPlayer;
    private readonly Collider2D[] hits=new Collider2D[12];
    private void FixedUpdate()
    {
        age+=Time.fixedDeltaTime;
        if(owner==null||owner.CurrentHp<=0||age>(wave?1.3f:.5f)) { Destroy(gameObject); return; }
        var visual=GetComponent<SpriteRenderer>();
        if(frames!=null&&frames.Length>=12) visual.sprite=frames[(wave?0:8)+(wave?(int)(age*10)%4:Mathf.Min(3,(int)(age*9)))];
        visual.flipX=direction<0;
        if(!wave) return;
        float step=5f*Time.fixedDeltaTime;
        Vector2 position=transform.position;
        if(Physics2D.Raycast(position+Vector2.up*.25f,Vector2.right*direction,step+.12f,groundLayer)) { Destroy(gameObject); return; }
        Vector2 next=position+Vector2.right*direction*step;
        var floor=Physics2D.Raycast(next+Vector2.up*.65f,Vector2.down,1.2f,groundLayer);
        if(!floor||Mathf.Abs(floor.point.y-position.y)>.5f) { Destroy(gameObject); return; }
        next.y=floor.point.y+.04f; transform.position=next;
        travelled+=step; if(travelled>=5.5f) { Destroy(gameObject); return; }
        if(hitPlayer) return;
        int count=Physics2D.OverlapBoxNonAlloc(next+Vector2.up*.25f,new Vector2(1.2f,.55f),0,hits,1<<7);
        for(int i=0;i<count;i++)
        {
            if(!(hits[i] is CapsuleCollider2D)||hits[i].isTrigger) continue;
            var p=hits[i].GetComponentInParent<PlayerController>(); if(p==null||p.isDead) continue;
            var health=p.GetComponent<Character>(); float before=health.CurrentHp; health.TakeDamage(damage);
            if(health.CurrentHp<before) { hitPlayer=true; break; }
        }
    }
}
