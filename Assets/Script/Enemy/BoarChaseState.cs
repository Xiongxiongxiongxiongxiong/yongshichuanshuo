using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoarChaseState : BaseState
{
    public override void OnEnter(Enemy enemy)
    {
        currentEnemy = enemy;
        //进入追击更改移动速度
        currentEnemy.currentSpeed = currentEnemy.ChaseSpeed;
        //追击动画
        currentEnemy.animator.SetBool("run",true);
    }

    public override void LogicUpdate()
    {
        if (currentEnemy.lostTimeCounter<=0)
        {
            currentEnemy.SwitchState(NPCState.Patroal);
        }
        if (( !currentEnemy.physicsCheck.isground|| currentEnemy.physicsCheck.touchLeftWall && currentEnemy.faceDir.x <0)|| (currentEnemy.physicsCheck.touchRighWall&& currentEnemy.faceDir.x >0))
        {
            // currentEnemy.wait = true;
            // currentEnemy.animator.SetBool("walk",false);
            currentEnemy.transform.localScale = new Vector3(currentEnemy.faceDir.x , 1, 1);

        }
    }

    public override void PhysicsUpdate()
    {
       
    }

    public override void OnExit()
    {
       currentEnemy. lostTimeCounter = currentEnemy.lostTime;
        currentEnemy.animator.SetBool("run",false);
    }
}
