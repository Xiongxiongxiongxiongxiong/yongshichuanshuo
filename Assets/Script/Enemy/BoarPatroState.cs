using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoarPatroState : BaseState
{
    public override void OnEnter(Enemy enemy)
    {
       //发现玩家切换到chase
       currentEnemy = enemy;
       currentEnemy.currentSpeed = currentEnemy.NormalSpeed;
    }

    public override void LogicUpdate()
    {
        if (currentEnemy.FoundPlayer())
        {
            currentEnemy.SwitchState(NPCState.Chase);
        }

        if (( !currentEnemy.physicsCheck.isground || currentEnemy.physicsCheck.touchLeftWall && currentEnemy.faceDir.x <0)|| (currentEnemy.physicsCheck.touchRighWall&& currentEnemy.faceDir.x >0))
        {
            currentEnemy.wait = true;
            currentEnemy.animator.SetBool("walk",false);
          //  currentEnemy.transform.localScale = new Vector3(currentEnemy.faceDir.x , 1, 1);

        }
        else
        {
            currentEnemy.animator.SetBool("walk",true);
        }
    }

    public override void PhysicsUpdate()  
    {
       
    }

    public override void OnExit()
    {
        currentEnemy.animator.SetBool("walk",false);
    }
}
