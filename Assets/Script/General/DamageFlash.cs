using UnityEngine;

[RequireComponent(typeof(Character), typeof(SpriteRenderer))]
public class DamageFlash : MonoBehaviour
{
    public Material flashMaterial;
    public float duration = 0.12f;
    private Character health;
    private SpriteRenderer visual;
    private Material normalMaterial;
    private float endsAt;
    private bool flashing;

    private void Awake()
    {
        health = GetComponent<Character>();
        visual = GetComponent<SpriteRenderer>();
        normalMaterial = visual.sharedMaterial;
    }

    private void OnEnable() { health.OnTakeDamage.AddListener(Flash); }
    private void OnDisable()
    {
        health.OnTakeDamage.RemoveListener(Flash);
        Restore();
    }
    private void Flash(Transform attacker)
    {
        if (flashMaterial == null) return;
        flashing = true;
        endsAt = Time.time + duration;
        visual.sharedMaterial = flashMaterial;
    }
    private void LateUpdate()
    {
        if (flashing && Time.time >= endsAt) Restore();
    }
    private void Restore()
    {
        if (flashing && visual != null) visual.sharedMaterial = normalMaterial;
        flashing = false;
    }
}
