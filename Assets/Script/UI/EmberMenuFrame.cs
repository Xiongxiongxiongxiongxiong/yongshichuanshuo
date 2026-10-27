using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Resolution-independent brass frame matching the approved title artwork.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class EmberMenuFrame : UnityEngine.UI.MaskableGraphic, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    bool hovered, selected;
    public void OnPointerEnter(PointerEventData data) { hovered = true; SetVerticesDirty(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; SetVerticesDirty(); }
    public void OnSelect(BaseEventData data) { selected = true; SetVerticesDirty(); }
    public void OnDeselect(BaseEventData data) { selected = false; SetVerticesDirty(); }
    protected override void OnDisable() { hovered = selected = false; base.OnDisable(); }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        var rect = GetPixelAdjustedRect();
        float w = rect.width * .5f, h = rect.height * .5f;
        bool active = hovered || selected;
        var outline = new Color(1, .77f, .38f, 1);
        var points = new[] { new Vector2(-w,0), new Vector2(-w+16,12), new Vector2(-w+25,h-7),
            new Vector2(-w+38,h), new Vector2(w-38,h), new Vector2(w-25,h-7), new Vector2(w-16,12),
            new Vector2(w,0), new Vector2(w-16,-12), new Vector2(w-25,-h+7), new Vector2(w-38,-h),
            new Vector2(-w+38,-h), new Vector2(-w+25,-h+7), new Vector2(-w+16,-12) };
        for (int i = 0; i < points.Length; i++) points[i] += rect.center;
        Fill(vh, points, active ? new Color(.15f,.095f,.035f,.96f) : new Color(.025f,.032f,.035f,.94f));
        for (int i = 0; i < points.Length; i++)
        {
            var a = points[i]; var b = points[(i+1)%points.Length];
            if (active) Line(vh,a,b,7,new Color(1,.55f,.12f,.12f));
            Line(vh,a,b,active ? 2.5f : 1.7f,active ? outline : new Color(.69f,.49f,.26f,1));
            var insetA = rect.center+(a-rect.center)*new Vector2(.965f,.82f);
            var insetB = rect.center+(b-rect.center)*new Vector2(.965f,.82f);
            Line(vh,insetA,insetB,.8f,new Color(.65f,.45f,.22f,.75f));
        }
        if (active)
        {
            var p = rect.center + new Vector2(-w+68,0);
            Fill(vh,new[]{p+Vector2.up*7,p+Vector2.right*7,p-Vector2.up*7,p-Vector2.right*7},outline);
        }
    }

    static void Fill(UnityEngine.UI.VertexHelper vh, Vector2[] points, Color color)
    {
        int index = vh.currentVertCount;
        foreach (var p in points) vh.AddVert(p,color,Vector2.zero);
        for (int i=1;i<points.Length-1;i++) vh.AddTriangle(index,index+i,index+i+1);
    }
    static void Line(UnityEngine.UI.VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 delta=(b-a).normalized, n=new Vector2(-delta.y,delta.x)*width*.5f;
        int index=vh.currentVertCount;
        vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
        vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
        vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
    }
}
