using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class CrumblingPlatform : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float standingDuration = 3f;
    [SerializeField, Min(0.1f)] private float respawnDelay = 5f;
    [SerializeField, Min(0.1f)] private float emptyResetDelay = 3f;

    private BoxCollider2D surface;
    private SpriteRenderer visual;
    private Color originalColor;
    private readonly Collider2D[] occupants = new Collider2D[16];
    private float standingTime;
    private float emptyTime;
    private float respawnTime;
    private bool broken;

    private void Awake()
    {
        surface = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        originalColor = visual.color;
    }

    private void OnEnable()
    {
        if (surface == null) return;
        Restore();
    }

    private void FixedUpdate()
    {
        float delta = Time.fixedDeltaTime;
        if (broken)
        {
            respawnTime -= delta;
            if (respawnTime <= 0f) Restore();
            return;
        }

        if (HasStandingPlayer())
        {
            emptyTime = 0f;
            standingTime += delta;
            if (standingTime >= standingDuration)
            {
                Break();
                return;
            }
        }
        else
        {
            emptyTime += delta;
            if (emptyTime >= emptyResetDelay) standingTime = 0f;
        }

        // Warn during the last part of the remaining standing time.
        float warning = Mathf.InverseLerp(standingDuration - 0.7f, standingDuration, standingTime);
        visual.color = Color.Lerp(originalColor, new Color(1f, 0.45f, 0.25f, originalColor.a),
            warning * (0.35f + 0.35f * Mathf.Sin(Time.time * 24f)));
    }

    private bool HasStandingPlayer()
    {
        Bounds bounds = surface.bounds;
        int count = Physics2D.OverlapBoxNonAlloc(
            new Vector2(bounds.center.x, bounds.max.y + 0.06f),
            new Vector2(bounds.size.x - 0.04f, 0.18f), 0f, occupants, 1 << 7);
        for (int i = 0; i < count; i++)
        {
            Collider2D candidate = occupants[i];
            if (candidate.isTrigger || !candidate.enabled) continue;
            PlayerController player = candidate.GetComponentInParent<PlayerController>();
            if (player == null || player.isDead) continue;
            Rigidbody2D body = candidate.attachedRigidbody;
            // A side touch or an upward jump through the sensor is not standing.
            if (Mathf.Abs(candidate.bounds.min.y - bounds.max.y) <= 0.13f &&
                body != null && body.velocity.y <= 0.2f) return true;
        }
        return false;
    }

    private void Break()
    {
        broken = true;
        respawnTime = respawnDelay;
        SpawnFragments();
        surface.enabled = false;
        visual.enabled = false;
    }

    private void Restore()
    {
        broken = false;
        standingTime = emptyTime = respawnTime = 0f;
        surface.enabled = true;
        visual.enabled = true;
        visual.color = originalColor;
    }

    private void SpawnFragments()
    {
        Bounds bounds = visual.bounds;
        for (int i = 0; i < 6; i++)
        {
            var fragment = new GameObject("Platform Fragment");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fragment, gameObject.scene);
            fragment.transform.position = new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (i + 0.5f) / 6f), bounds.center.y, transform.position.z);
            var renderer = fragment.AddComponent<SpriteRenderer>();
            renderer.sprite = visual.sprite;
            renderer.sortingLayerID = visual.sortingLayerID;
            renderer.sortingOrder = visual.sortingOrder;
            renderer.color = originalColor;
            Vector2 size = visual.sprite.bounds.size;
            fragment.transform.localScale = new Vector3(bounds.size.x / 6f / size.x, bounds.size.y / size.y, 1f);
            var body = fragment.AddComponent<Rigidbody2D>();
            body.gravityScale = 2f;
            body.velocity = new Vector2((i - 2.5f) * 0.4f, 1.5f);
            body.angularVelocity = (i % 2 == 0 ? -1f : 1f) * 100f;
            Destroy(fragment, 0.7f);
        }
    }
}
