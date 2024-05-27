using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
public class Sign : MonoBehaviour
{
    private player playerInput;
    public GameObject signSprite;
    private bool canPress;
    private Animator animater;
    public Transform palyerTransform;
    private IInteractable targetItem;
    private void Awake()
    {
        animater = signSprite.GetComponent<Animator>();
        playerInput = new player();
        playerInput.Enable();
    }


    private void OnEnable()
    {
        InputSystem.onActionChange += OnActionChange;
        playerInput.GamePlayer.Confirm.started += Onconfirm;
    }

    private void OnDisable()
    {
        canPress = false;
    }

    private void Onconfirm(InputAction.CallbackContext obj)
    {
        if (canPress)
        {
            targetItem.TriggerAction();
            
            GetComponent<AudioDefination>()?.PlayerAudioClip();
        }
    }
/// <summary>
/// 切换设备同时切换动画
/// </summary>
/// <param name="obj"></param>
/// <param name="actionChange"></param>
    private void OnActionChange(object obj, InputActionChange actionChange)
    {
        if (actionChange == InputActionChange.ActionStarted)
        {
           
            var d = ((InputAction)obj).activeControl.device;
            switch (d.device)
            {
                case Keyboard :
                    animater.Play("Sign");
                    break;
                case  DualShockGamepad:
                    animater.Play("PS_Sign");
                    break;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Interactable"))
        {
            canPress = true;
            targetItem = other.GetComponent<IInteractable>();

        }
    }

    private void Update()
    {
        signSprite.GetComponent<SpriteRenderer>().enabled = canPress;
        signSprite.transform.localScale = palyerTransform.localScale;
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        canPress = false;
    }
}
