using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closeSprite;
    [SerializeField] private Sprite promptIcon;
    [SerializeField] private int weaponDamageBonus = 1;
    [SerializeField] private Vector2 interactionSize = new Vector2(2.4f, 1.8f);
    [SerializeField] private Vector2 interactionOffset = new Vector2(0f, 0.35f);

    private SpriteRenderer chestRenderer;
    private BoxCollider2D interactionCollider;
    private PlayerController nearbyPlayer;
    private PlayerController scenePlayer;
    private float nextPlayerSearchTime;
    private GameObject promptObject;
    private SpriteRenderer promptRenderer;
    private Sprite promptSprite;
    private bool isOpened;

    private void Awake()
    {
        chestRenderer = GetComponent<SpriteRenderer>();
        interactionCollider = GetComponent<BoxCollider2D>();
        interactionCollider.isTrigger = true;
        Vector3 worldScale = transform.lossyScale;
        float scaleX = Mathf.Max(Mathf.Abs(worldScale.x), 0.001f);
        float scaleY = Mathf.Max(Mathf.Abs(worldScale.y), 0.001f);
        interactionCollider.size = new Vector2(interactionSize.x / scaleX, interactionSize.y / scaleY);
        interactionCollider.offset = new Vector2(interactionOffset.x / scaleX, interactionOffset.y / scaleY);

        if (closeSprite == null) closeSprite = chestRenderer.sprite;
        chestRenderer.sprite = isOpened && openSprite != null ? openSprite : closeSprite;
        CreatePrompt();
    }

    private void OnEnable()
    {
        if (chestRenderer == null) return;
        chestRenderer.sprite = isOpened && openSprite != null ? openSprite : closeSprite;
    }

    private void OnDisable()
    {
        if (nearbyPlayer != null) nearbyPlayer.ClearNearbyChest(this);
        nearbyPlayer = null;
    }

    private void Update()
    {
        RefreshNearbyPlayer();
        bool showPrompt = nearbyPlayer != null && !isOpened;
        if (promptObject == null) return;
        if (promptObject.activeSelf != showPrompt) promptObject.SetActive(showPrompt);
        if (!showPrompt) return;

        SpriteRenderer playerVisual = nearbyPlayer.GetComponent<SpriteRenderer>();
        float promptY = playerVisual != null ? playerVisual.bounds.max.y + 0.25f : nearbyPlayer.transform.position.y + 1.5f;
        promptObject.transform.position = new Vector3(nearbyPlayer.transform.position.x, promptY, -0.1f);

        float pulse = (Mathf.Sin(Time.unscaledTime * 5f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(0.58f, 0.68f, pulse);
        Vector3 parentScale = promptObject.transform.parent.lossyScale;
        promptObject.transform.localScale = new Vector3(
            scale / Mathf.Max(Mathf.Abs(parentScale.x), 0.001f),
            scale / Mathf.Max(Mathf.Abs(parentScale.y), 0.001f),
            1f / Mathf.Max(Mathf.Abs(parentScale.z), 0.001f));
        Color color = Color.white;
        color.a = Mathf.Lerp(0.55f, 1f, pulse);
        promptRenderer.color = color;
    }

    private void OnTriggerEnter2D(Collider2D other) => SetNearbyPlayer(other);
    private void OnTriggerStay2D(Collider2D other) => SetNearbyPlayer(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController exitingPlayer = other.GetComponentInParent<PlayerController>();
        if (exitingPlayer == null || exitingPlayer != nearbyPlayer) return;
        exitingPlayer.ClearNearbyChest(this);
        nearbyPlayer = null;
    }

    private void SetNearbyPlayer(Collider2D other)
    {
        if (isOpened) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        SetNearbyPlayer(player);
    }

    private void SetNearbyPlayer(PlayerController player)
    {
        if (isOpened || player == null || nearbyPlayer == player) return;
        if (nearbyPlayer != null) nearbyPlayer.ClearNearbyChest(this);
        nearbyPlayer = player;
        player.SetNearbyChest(this);
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            promptRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            promptRenderer.sortingOrder = playerRenderer.sortingOrder + 20;
        }
    }

    private void RefreshNearbyPlayer()
    {
        if (isOpened || interactionCollider == null) return;

        if (nearbyPlayer != null && (!nearbyPlayer.isActiveAndEnabled || !IsPlayerInsideInteractionBounds(nearbyPlayer)))
        {
            nearbyPlayer.ClearNearbyChest(this);
            nearbyPlayer = null;
        }

        if (nearbyPlayer != null) return;
        if (scenePlayer == null && Time.unscaledTime >= nextPlayerSearchTime)
        {
            scenePlayer = FindObjectOfType<PlayerController>();
            nextPlayerSearchTime = Time.unscaledTime + 0.5f;
        }

        if (scenePlayer != null && scenePlayer.isActiveAndEnabled && IsPlayerInsideInteractionBounds(scenePlayer))
            SetNearbyPlayer(scenePlayer);
    }

    private bool IsPlayerInsideInteractionBounds(PlayerController player)
    {
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null) return false;

        Bounds chestBounds = interactionCollider.bounds;
        Bounds playerBounds = playerCollider.bounds;
        return Mathf.Abs(chestBounds.center.x - playerBounds.center.x) <= chestBounds.extents.x + playerBounds.extents.x &&
               Mathf.Abs(chestBounds.center.y - playerBounds.center.y) <= chestBounds.extents.y + playerBounds.extents.y;
    }

    public bool TryOpen(PlayerController player)
    {
        if (isOpened || player == null || player != nearbyPlayer) return false;

        isOpened = true;
        if (openSprite != null) chestRenderer.sprite = openSprite;
        gameObject.tag = "Untagged";
        player.ClearNearbyChest(this);
        nearbyPlayer = null;
        player.BeginWeaponUpgrade(weaponDamageBonus);
        return true;
    }

    public void TriggerAction()
    {
        if (nearbyPlayer != null) TryOpen(nearbyPlayer);
    }

    private void CreatePrompt()
    {
        if (promptIcon == null)
        {
            Debug.LogWarning($"Chest '{name}' has no prompt icon assigned.", this);
            return;
        }

        promptSprite = promptIcon;

        promptObject = new GameObject("Attack Prompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = new Vector3(0f, 1.35f, -0.1f);
        promptRenderer = promptObject.AddComponent<SpriteRenderer>();
        promptRenderer.sprite = promptSprite;
        promptRenderer.sortingLayerID = chestRenderer.sortingLayerID;
        promptRenderer.sortingOrder = chestRenderer.sortingOrder + 20;

        promptObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (nearbyPlayer != null) nearbyPlayer.ClearNearbyChest(this);
    }
}
