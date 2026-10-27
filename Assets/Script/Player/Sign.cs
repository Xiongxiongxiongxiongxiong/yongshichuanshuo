using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
public class Sign : MonoBehaviour
{
    private player playerInput;
    private bool canPress;
    private IInteractable targetItem;
    private void Awake()
    {
        if (playerInput == null) playerInput = new player();
    }


    private void OnEnable()
    {
        if (playerInput == null) playerInput = new player();
        playerInput.GamePlayer.Enable();
        playerInput.GamePlayer.Confirm.started += Onconfirm;
    }

    private void OnDisable()
    {
        canPress = false;
        targetItem = null;
        if (playerInput == null) return;
        playerInput.GamePlayer.Confirm.started -= Onconfirm;
        playerInput.Disable();
    }

    private void Onconfirm(InputAction.CallbackContext obj)
    {
        if (canPress && targetItem != null)
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

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Interactable"))
        {
            targetItem = other.GetComponent<IInteractable>();
            canPress = targetItem != null;

        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        canPress = false;
        targetItem = null;
    }

    private void OnDestroy() { playerInput?.Dispose(); }
}
