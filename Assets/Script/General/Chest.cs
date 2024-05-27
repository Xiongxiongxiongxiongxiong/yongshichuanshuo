using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour,IInteractable
{
    private SpriteRenderer _spriteRenderer;
   // private IInteractable _interactableImplementation;
   public Sprite openSprite;
   public Sprite closeSprite;
   private bool isDone;

   private void Awake()
   {
       _spriteRenderer = GetComponent<SpriteRenderer>();
   }

   private void OnEnable()
   {
       _spriteRenderer.sprite = isDone ? openSprite : closeSprite;
   }

   public void TriggerAction()
    {
        if (!isDone)
        {
            OpenChest();
        }
       // _interactableImplementation.TriggerAction();
    }

    private void OpenChest()
    {
        _spriteRenderer.sprite = openSprite;
        isDone = true;
        this.gameObject.tag = "Untagged";
    }
}
