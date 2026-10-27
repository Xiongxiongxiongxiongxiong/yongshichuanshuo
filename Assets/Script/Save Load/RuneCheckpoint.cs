using UnityEngine;

public class RuneCheckpoint : MonoBehaviour
{
    public string checkpointId = "Scene7Rune";
    public GameSceneSO sceneData;
    public Vector3 respawnPosition;
    public float activationRadius = 2.2f;
    public bool IsActivated { get; private set; }
    private SpriteRenderer stone;
    private SceneLoader loader;

    private void Start()
    {
        stone = GetComponent<SpriteRenderer>();
        loader = FindObjectOfType<SceneLoader>();
        IsActivated = loader != null && loader.HasCheckpoint(checkpointId);
        UpdateVisual();
    }

    private void Update()
    {
        if (loader == null || loader.playerTrans == null) return;
        var player = loader.playerTrans.GetComponent<PlayerController>();
        if (!IsActivated && player.gameObject.activeInHierarchy && !player.isDead &&
            Vector2.Distance(player.transform.position, respawnPosition) <= activationRadius)
        {
            loader.SaveCheckpoint(checkpointId, sceneData, respawnPosition);
            IsActivated = true;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        stone.color = IsActivated ? Color.white : new Color(.42f, .42f, .42f, 1);
    }
}
