using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Recolors the hair palette without changing sprite geometry or source images.</summary>
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerHairColor : MonoBehaviour
{
    [Serializable]
    public class Atlas
    {
        public Texture2D texture;
        public Rect[] areas;
        public float[] weights;
    }
    [SerializeField] private Atlas[] atlases;
    private SpriteRenderer visual;
    private Sprite previous;
    private MaterialPropertyBlock properties;
    private readonly Dictionary<Sprite, Vector4> rectangles = new Dictionary<Sprite, Vector4>();
    private static readonly int HairRect = Shader.PropertyToID("_HairRect");

    private void OnEnable()
    {
        visual = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
        previous = null;
        rectangles.Clear();
        Refresh();
    }
    private void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if (visual == null || visual.sprite == null || visual.sprite == previous) return;
        previous = visual.sprite;
        Vector4 rect;
        if (!rectangles.TryGetValue(previous,out rect))
        {
            rect = FindHair(previous);
            rectangles.Add(previous,rect);
        }
        visual.GetPropertyBlock(properties);
        properties.SetVector(HairRect,rect);
        visual.SetPropertyBlock(properties);
    }

    private Vector4 FindHair(Sprite sprite)
    {
        if (atlases == null) return Vector4.zero;
        var cell = sprite.rect;
        Rect best = default(Rect);
        float score = 0;
        foreach (var atlas in atlases)
        {
            if (atlas.texture != sprite.texture) continue;
            for (int i=0;i<atlas.areas.Length;i++)
            {
                var area = atlas.areas[i];
                if (!cell.Contains(area.center) || area.width>cell.height*.8f || area.height>cell.height*.75f) continue;
                // Metal shields can contain more neutral pixels than the hair. Prefer
                // compact regions near the top of the frame, where the head is drawn.
                float height = Mathf.InverseLerp(cell.yMin,cell.yMax,area.center.y);
                float weight = atlas.weights[i] * Mathf.Pow(Mathf.Max(.05f,height),3);
                if (weight>score) { score=weight; best=area; }
            }
            if (score>0) return new Vector4(best.xMin/atlas.texture.width,best.yMin/atlas.texture.height,
                best.xMax/atlas.texture.width,best.yMax/atlas.texture.height);
        }
        return Vector4.zero;
    }
    private void OnDisable()
    {
        if (visual == null || properties == null) return;
        visual.GetPropertyBlock(properties); properties.SetVector(HairRect,Vector4.zero); visual.SetPropertyBlock(properties);
    }
}
