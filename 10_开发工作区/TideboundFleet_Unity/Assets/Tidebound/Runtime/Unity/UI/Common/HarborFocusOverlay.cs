using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Focuses the lobby with darker top/side edges while leaving the ship readable.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HarborFocusOverlay : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var blue = HarborDesignTokens.OceanShadow;
            AddQuad(vh, new Rect(r.xMin, r.yMax-r.height*.16f, r.width, r.height*.16f),
                new Color(blue.r,blue.g,blue.b,.28f), new Color(blue.r,blue.g,blue.b,0));
            AddQuad(vh, new Rect(r.xMin,r.yMin,r.width*.29f,r.height),
                new Color(blue.r,blue.g,blue.b,.20f), new Color(blue.r,blue.g,blue.b,0), true);
            AddQuad(vh, new Rect(r.xMax-r.width*.29f,r.yMin,r.width*.29f,r.height),
                new Color(blue.r,blue.g,blue.b,0), new Color(blue.r,blue.g,blue.b,.20f), true);
            AddQuad(vh,new Rect(r.xMin,r.yMin,r.width,r.height*.16f),new Color(blue.r,blue.g,blue.b,0),new Color(blue.r,blue.g,blue.b,.15f));
        }

        private static void AddQuad(VertexHelper vh, Rect r, Color a, Color b, bool horizontal=false)
        {
            var i=vh.currentVertCount;
            vh.AddVert(new Vector2(r.xMin,r.yMin),horizontal?a:b,Vector2.zero);
            vh.AddVert(new Vector2(r.xMin,r.yMax),a,Vector2.up);
            vh.AddVert(new Vector2(r.xMax,r.yMax),horizontal?b:a,Vector2.one);
            vh.AddVert(new Vector2(r.xMax,r.yMin),b,Vector2.right);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
