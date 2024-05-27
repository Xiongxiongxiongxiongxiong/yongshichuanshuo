using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SacePoint : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public Sprite lightSprite, darkSprite;
    private bool isDone;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
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
        }
    }
}
