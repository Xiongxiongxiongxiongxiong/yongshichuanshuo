using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BossLevelEntrance : MonoBehaviour
{
    public GameSceneSO destination;
    public Vector3 spawnPosition = new Vector3(-11, 0.05f, 0);
    private bool requested;
    private void OnTriggerEnter2D(Collider2D other) { TryEnter(other); }
    private void OnTriggerStay2D(Collider2D other) { TryEnter(other); }
    private void TryEnter(Collider2D other)
    {
        if (requested || destination == null) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.isDead) return;
        var loader = FindObjectOfType<SceneLoader>();
        if (loader != null) requested = loader.EnterBossLevel(destination, spawnPosition);
    }
}
