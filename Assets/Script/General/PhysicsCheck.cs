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
        float probeScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        isground = Physics2D.OverlapCircle(transform.TransformPoint(bottomDffset), checkRaduis * probeScale, groundLayer);
        //墙体判断
        // Probe the torso so floors and ledges do not count as walls.
        var bounds = coll.bounds;
        var probeSize = new Vector2(0.05f, bounds.size.y * 0.7f);
        float distance = bounds.extents.x + 0.12f;
        var leftHit = Physics2D.BoxCast(bounds.center, probeSize, 0, Vector2.left, distance, groundLayer);
        var rightHit = Physics2D.BoxCast(bounds.center, probeSize, 0, Vector2.right, distance, groundLayer);
        touchLeftWall = leftHit.collider != null && leftHit.normal.x > 0.5f;
        touchRighWall = rightHit.collider != null && rightHit.normal.x < -0.5f;
    }

    public void RefreshChecks()
    {
        Check();
    }
//在场景中绘制两个点协作观察检测的位置
   private  void OnDrawGizmosSelected()
    {
        float probeScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Gizmos.DrawWireSphere(transform.TransformPoint(bottomDffset), checkRaduis * probeScale);
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
