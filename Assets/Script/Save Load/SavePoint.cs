using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePoint : MonoBehaviour
{
    [Header("广播")] public VoidEventSO LoadGameEvent;
    private SpriteRenderer spriteRenderer;
    public Sprite lightSprite, darkSprite;
    public GameObject lightobj;
    private bool isDone;

    private void Awake()
    {
        spriteRenderer = gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
        lightobj.SetActive(isDone);
        
    }

    private void OnEnable()
    {
        spriteRenderer.sprite = isDone ? lightSprite : darkSprite;
    }

    public void TriggerAction()
    {
        if (!isDone)
        {
            isDone = true;
            spriteRenderer.sprite = lightSprite;
            lightobj.SetActive(true);
            //保存数据
            LoadGameEvent.RaiseEvent();
            this.gameObject.tag = "Untagged";
        }
    }
}
