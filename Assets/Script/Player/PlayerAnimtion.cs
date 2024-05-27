using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimtion : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rd;
    private PhysicsCheck physicsCheck;
    private PlayerController playerController;
    private void Awake()
    {
        anim = GetComponent<Animator>();
        rd = GetComponent<Rigidbody2D>();
        physicsCheck = GetComponent<PhysicsCheck>();
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SetAnimtion();
    }
   //跑步
    public void SetAnimtion()
    {
        anim.SetFloat("velocityX", Math.Abs(rd.velocity.x) );
        anim.SetFloat("velocityY",rd.velocity.y);
        anim.SetBool("isGround",physicsCheck.isground);
        anim.SetBool("isDead",playerController.isDead);
        anim.SetBool("isAttack",playerController.isAttack);
       // anim.SetInteger("combo",playerController.combo);
    }

    public void PlayerHurt()
    {
        anim.SetTrigger("hurt");
    }
    
    //攻击方法
    public void PlayerAttack()
    {
        anim.SetTrigger("attack");
    }
    
    
    
}
