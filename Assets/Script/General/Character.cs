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
    private void Awake()
    {
        // Enemies loaded after the NewGame event still need their initial health.
        if (CurrentHp <= 0)
        {
            CurrentHp = maxHp;
        }
    }

    private void NewGame()
    {
        CurrentHp = maxHp;
        invulnerable = false;
        invulnerableCounter = 0;
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
        if (CurrentHp > 0 && other.CompareTag("water"))
        {
            CurrentHp = 0;
            OnHealthChange?.Invoke(this);
            OnDead?.Invoke();
            
        }
    }

    public void TakeDamage(Attack attack)
    {
        var slide = GetComponent<PlayerSlide>();
        if (slide != null && slide.IsSliding) return;
        if (invulnerable || CurrentHp <= 0 || attack == null)
        {
            return;
        }

        var guard = GetComponent<DirectionalDamageGuard>();
        float damage = guard != null ? guard.ModifyDamage(attack.damage, attack.transform) : attack.damage;
        if (damage <= 0) return;
        if (CurrentHp - damage>0)
        {
            CurrentHp -= damage;
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
