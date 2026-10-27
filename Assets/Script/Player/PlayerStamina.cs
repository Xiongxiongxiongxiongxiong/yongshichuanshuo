using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerStamina : MonoBehaviour
{
    public float maxStamina = 100f;
    public float recoveryPerSecond = 20f;
    public float Current { get; private set; } = 100f;
    private PlayerController controller;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        ResetStamina();
    }

    private void Update()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
        if (controller != null && controller.inputcontroller != null &&
            !controller.isDead && controller.inputcontroller.GamePlayer.Move.enabled)
            Current = Mathf.Min(maxStamina, Current + recoveryPerSecond * Time.deltaTime);
    }

    public bool TrySpend(float amount)
    {
        if (amount < 0 || Current < amount) return false;
        Current -= amount;
        return true;
    }

    public void ResetStamina() { Current = maxStamina; }
    public void Restore(float amount) { Current = Mathf.Clamp(Current + Mathf.Max(0, amount), 0, maxStamina); }
}
