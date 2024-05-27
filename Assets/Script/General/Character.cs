using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class Character : MonoBehaviour
{
    [Header("事件监听")] 
    public VoidEventSO newGameEvent;
    
    [Header("基本属性")]
    public float maxHp;
    public float CurrentHp;
    

  [Header("无敌")]
  //无敌总时间
    public float invulnerableDuration;
//无敌剩余时间
    private float invulnerableCounter;
    //是否无敌
    public bool invulnerable;

    public UnityEvent<Character> OnHealthChange;



    public UnityEvent<Transform> OnTakeDamage;
    public UnityEvent OnDead;
    private void NewGame()
    {
        CurrentHp = maxHp;
        OnHealthChange?.Invoke(this);
    }

    private void OnEnable()
    {
        newGameEvent.onEventRaised += NewGame;
    }

    private void OnDisable()
    {
        newGameEvent.onEventRaised -= NewGame;
    }

    private void Update()
    {
        if (invulnerable)
        {
            invulnerableCounter -= Time.deltaTime;
            if (invulnerableCounter<=0)
            {
                invulnerable = false;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        //如果落水-死亡
        if (other.CompareTag("water"))
        {
            CurrentHp = 0;
            OnHealthChange?.Invoke(this);
            OnDead?.Invoke();
            
        }
    }

    public void TakeDamage(Attack attack)
    {
        if (invulnerable)
        {
            return;
        }

        if (CurrentHp - attack.damage>0)
        {
            CurrentHp -= attack.damage;
            TriggerInvulnerable();
            
            //受伤
            OnTakeDamage?.Invoke(attack.transform);
            
        }
        else
        {
            CurrentHp = 0;
            OnDead?.Invoke();
        }


        OnHealthChange?.Invoke(this);


    }
//受伤无敌方法
    public void TriggerInvulnerable()
    {
        if (!invulnerable)
        {
            invulnerable = true;
            invulnerableCounter = invulnerableDuration;
        }
    }
}
