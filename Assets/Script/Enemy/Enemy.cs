using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Rigidbody2D rd;
    public  Animator animator;
    public PhysicsCheck physicsCheck;
    [Header("基本参数")]
    public float NormalSpeed;//移动速度
    public float ChaseSpeed;//追击速度
    public float currentSpeed;//当前移动速度
    public Vector3 faceDir;//怪物的方向信息

    public float hurtForce;
    public Transform attacker;
    //计时器
    [Header("计时器")]
    public float waitTime;
    public float waitTimeCounter;
    public bool wait;
    
    public  float lostTime;
    public float lostTimeCounter;
    
    
    
    
    //状态
    [Header("状态")]
    public bool isHurt;
    public bool isDead;
    
    
    

    private SpriteRenderer spriteRenderer;

    protected BaseState currentState;
    protected BaseState patrolState;
    protected BaseState chaseState;

    [Header("检测")]
    public Vector2 centerOffset;
    public Vector2 checkSize;
    public float checkDistance;
    public LayerMask attackLayer;
    
    
    protected virtual void Awake()
    {
        rd = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        physicsCheck=GetComponent<PhysicsCheck>();
        currentSpeed = NormalSpeed;
      //  faceDir = new Vector3(-1, 0, 0);
      //  spriteRenderer = GetComponent<SpriteRenderer>();

      //  waitTimeCounter = waitTime;
    }

    private void OnEnable()
    {
        currentState = patrolState;
        currentState.OnEnter(this);
    }


    void Start()
    {
        
    }




    // Update is called once per frame
    void Update()
    {

        faceDir = new Vector3(-transform.localScale.x, 0, 0);

       
         currentState.LogicUpdate();
         TimeCounter();
    }
    private void FixedUpdate()
    {
        if (!isHurt& !isDead &!wait)
        {
            EnemyMove();
        }
        currentState.PhysicsUpdate();
        
    }

    private void OnDisable()
    {
        currentState.OnExit();
    }

    public virtual void EnemyMove()
    {
        rd.velocity = new Vector2(currentSpeed * faceDir.x * Time.deltaTime, rd.velocity.y);
        
    }
  //计时器
  public void TimeCounter()
  {
      if (wait)
      {
          waitTimeCounter -= Time.deltaTime;
          if (waitTimeCounter<=0)
          {
              wait = false;
              waitTimeCounter = waitTime;
              transform.localScale = new Vector3(faceDir.x, 1, 1);

          }
      }

      if (!FoundPlayer()& lostTimeCounter>0)
      {
          lostTimeCounter -= Time.deltaTime;
      }

      
      
  }
//发现玩家追击方法
    public bool FoundPlayer()
    {
        return Physics2D.BoxCast(transform.position+(Vector3)centerOffset,checkSize,0,faceDir,checkDistance,attackLayer);
    }


    public void SwitchState(NPCState npc)
    {
        var newState = npc switch
        {
            NPCState.Patroal => patrolState,
            NPCState.Chase => chaseState,
            _ => null
        };
        currentState.OnExit();
        currentState = newState;
        currentState.OnEnter(this);
    }
    
  #region 事件执行方法
  public void OnTakeDamage(Transform attacktrans)
  {
      attacker = attacktrans;
      if (attacktrans.transform.position.x - transform.position.x >0 )
      {
          transform.localScale = new Vector3(-1, 1, 1);
      }
      if (attacktrans.transform.position.x - transform.position.x <0 )
      {
          transform.localScale = new Vector3(1, 1, 1);
      }
      
      //受伤击退
      isHurt = true;
      animator.SetTrigger("hurt");
      //计算受击方向
      Vector2 dir = new Vector2(transform.position.x - attacktrans.position.x,0).normalized;
      rd.velocity = new Vector2(0, rd.velocity.y);
      StartCoroutine(onHurt(dir));

  }
  IEnumerator onHurt(Vector2 dir)
  {

      rd.AddForce(dir*hurtForce,ForceMode2D.Impulse);
      yield return new WaitForSeconds(0.5f);
      isHurt = false;

  }
//死亡
  public void onDie()
  {
      gameObject.layer = 2;
      animator.SetBool("dead",true);
      isDead = true;
  }
  //销毁
  public void DestroyAfterAnimation()
  {
      Destroy(this.gameObject);
  }
  #endregion


  private void OnDrawGizmosSelected()
  {
      Gizmos.DrawWireCube(transform.position+(Vector3)centerOffset+new Vector3(checkDistance*-transform.localScale.x,0),new Vector3(0.2f,0.2f,0.2f));
  }
}
