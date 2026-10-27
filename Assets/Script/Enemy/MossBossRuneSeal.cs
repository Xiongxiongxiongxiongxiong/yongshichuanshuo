using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class MossBossRuneSeal : MonoBehaviour
{
    public Sprite[] frames;
    private SpriteRenderer visual;
    private void Awake() { visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.FloorToInt(Time.time * 5) % frames.Length];
    }
}
