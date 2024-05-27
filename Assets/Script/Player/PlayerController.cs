
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("监听事件")] public SceneLoadEventSO loadEvent;
    public VoidEventSO afterSceneLoadEvent;
    public player inputcontroller;
    private PhysicsCheck physicsCheck;
    public Vector2 inputDirection;
    public float speed;
    private SpriteRenderer _renderer;
    private Rigidbody2D rd;
    public float JumpF;

    public bool isHurt;
    public float hurtForce;
    public bool isDead;
    private PlayerAnimtion playeranim;
    public bool isAttack;
  //  public int combo;
    //角色材质
    public PhysicsMaterial2D Nomal,Wall;
    private CapsuleCollider2D coll;
    private void Awake()
    {
        inputcontroller = new player();
        rd = GetComponent<Rigidbody2D>();
        _renderer = GetComponent<SpriteRenderer>();
        inputcontroller.GamePlayer.Jump.started += Jump;
        physicsCheck = GetComponent<PhysicsCheck>();
        playeranim = GetComponent<PlayerAnimtion>();
        coll = GetComponent<CapsuleCollider2D>();
        //攻击
        inputcontroller.GamePlayer.Attack.started += PlayerAttack;

    }
//攻击方法
    private void PlayerAttack(InputAction.CallbackContext obj)
    {
        playeranim.PlayerAttack();
        isAttack = true;
        // combo++;
        // if (combo==3)
        // {
        //     combo = 0;
        // }

    }

    private void Jump(InputAction.CallbackContext obj)
    {
        if (physicsCheck.isground)
        {
            rd.AddForce(transform.up*JumpF,ForceMode2D.Impulse);
            GetComponent<AudioDefination>()?.PlayerAudioClip();
        }
       
    }

    private void OnEnable()
    {
        inputcontroller.Enable();
        loadEvent.LoadRequesEvent += OnloadEvent;
        afterSceneLoadEvent.onEventRaised += OnAfterSceneLoadEvent;
    }




    private void OnDisable()
    {
        inputcontroller.Disable();
        loadEvent.LoadRequesEvent -= OnloadEvent;
        afterSceneLoadEvent.onEventRaised -= OnAfterSceneLoadEvent;
    }

    private void Update()
    {
        inputDirection = inputcontroller.GamePlayer.Move.ReadValue<Vector2>();
        CheckState();
    }

    private void FixedUpdate()
    {
        //不是在受伤状态和攻击状态时可以移动，这里可以按需求更改
        if (!isHurt&&!isAttack)
        {
            Move();
        }
       
    }
//场景加载停止角色控制
    private void OnloadEvent(GameSceneSO arg0, Vector3 arg1, bool arg2)
    {
        inputcontroller.GamePlayer.Disable();
    }
    //场景加载启动角色控制
    private void OnAfterSceneLoadEvent()
    {
        inputcontroller.GamePlayer.Enable();
    }
    //移动
    public void Move()
    {
        rd.velocity = new Vector2(inputDirection.x * speed * Time.deltaTime,rd.velocity.y);
       
        if (inputDirection.x>0)
        {
            _renderer.flipX = false;

        }

        if (inputDirection.x<0)
        {
            _renderer.flipX = true;

        }

    }
//受击反弹
    public void GetHurt(Transform attacker)
    {
        isHurt = true;
        rd.velocity = Vector2.zero;
        Vector2 dir = new Vector2(transform.position.x - attacker.position.x, 0).normalized;
        rd.AddForce(dir*hurtForce,ForceMode2D.Impulse);
    }
    
    //死亡
    public void PlayerDead()
    {
        isDead = true;
        inputcontroller.GamePlayer.Disable();
    }
    
    //切换角色材质
    public void CheckState()
    {
        coll.sharedMaterial = physicsCheck.isground ? Nomal : Wall;
    }

    
    
    
}
