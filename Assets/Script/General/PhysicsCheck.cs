using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhysicsCheck : MonoBehaviour
{
    // Start is called before the first frame update
    public CapsuleCollider2D coll;
    [Header("检测参数")] 
    public bool manual;//自动调整左右偏移

//左右位置的偏移
    public Vector2 leftOffset;
    public Vector2 RighOffset;
    
    public float checkRaduis;
    public LayerMask groundLayer;
    public Vector2 bottomDffset;
    [Header("检测参数")]
    public bool isground;
    public bool touchLeftWall;//检测撞左墙
    public bool touchRighWall;//检测撞右墙

    private void Awake()
    {
        coll = GetComponent<CapsuleCollider2D>();
        if (!manual)
        {
            RighOffset = new Vector2((coll.bounds.size.x + coll.offset.x) * 0.5f, coll.bounds.size.y * 0.5f);
            leftOffset = new Vector2(-RighOffset.x, RighOffset.y);
           // leftOffset = new Vector2(-coll.size.x + coll.offset.x*0.5f, RighOffset.y);
        }
    }

    private void Check()
    {
        //检测地面
        isground= Physics2D.OverlapCircle((Vector2)transform.position+bottomDffset, checkRaduis, groundLayer);
        //墙体判断
        touchLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, checkRaduis, groundLayer);
        touchRighWall = Physics2D.OverlapCircle((Vector2)transform.position + RighOffset, checkRaduis, groundLayer);
    }
//在场景中绘制两个点协作观察检测的位置
   private  void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere((Vector2)transform.position+bottomDffset, checkRaduis);
        Gizmos.DrawWireSphere((Vector2)transform.position+leftOffset, checkRaduis);
        Gizmos.DrawWireSphere((Vector2)transform.position+RighOffset, checkRaduis);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Check();
    }
}
