using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    public LevelCompleteUI completionUI;
    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerController>();
        if (player != null && !player.isDead) completionUI.Show();
    }
}
