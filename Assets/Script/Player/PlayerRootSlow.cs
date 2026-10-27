using UnityEngine;
// A timed modifier, rather than changing the player's configured movement speed.
public class PlayerRootSlow : MonoBehaviour
{
    float until;
    public float Multiplier => Time.time < until ? .55f : 1f;
    public void Apply(float seconds) { until = Mathf.Max(until, Time.time + seconds); }
    public void Clear() { until = 0; }
    void OnDisable() { Clear(); }
}
