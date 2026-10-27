using UnityEngine;

// Optional defense component; unguarded characters keep their original damage rules.
public class DirectionalDamageGuard : MonoBehaviour
{
    public bool IsGuarding { get; private set; }
    public int Facing { get; private set; } = 1;
    public void SetGuard(bool active, int facing)
    {
        IsGuarding = active;
        Facing = facing < 0 ? -1 : 1;
    }
    public float ModifyDamage(float amount, Transform source)
    {
        if (!isActiveAndEnabled || !IsGuarding || source == null) return amount;
        var attacker = source.GetComponentInParent<Character>();
        float x = attacker != null ? attacker.transform.position.x : source.position.x;
        return (x - transform.position.x) * Facing >= 0 ? amount * .5f : amount;
    }
    void OnDisable() { IsGuarding = false; }
}
