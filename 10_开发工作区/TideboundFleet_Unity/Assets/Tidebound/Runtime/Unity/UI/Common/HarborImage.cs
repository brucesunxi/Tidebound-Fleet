using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Scalable rounded UI surface. Geometry only; no generated bitmap or material instance.</summary>
    public sealed class HarborImage : Image
    {
        public float Radius = 18;
        public float Border = 2;
        public Color Edge = new Color(.85f, .67f, .32f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            Draw(vh, r, Radius, Edge);
            var inset = Mathf.Min(Border, Mathf.Min(r.width, r.height) / 4);
            r = new Rect(r.x + inset, r.y + inset, r.width - inset * 2, r.height - inset * 2);
            Draw(vh, r, Mathf.Max(0, Radius - inset), color);
        }
        private static void Draw(VertexHelper vh, Rect r, float radius, Color c)
        {
            var start = vh.currentVertCount;
            vh.AddVert(r.center, c, Vector2.zero);
            radius = Mathf.Clamp(radius, 0, Mathf.Min(r.width, r.height) / 2);
            const int segments = 8;
            for (var corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(corner == 0 || corner == 3 ? r.xMax-radius : r.xMin+radius,
                    corner < 2 ? r.yMax-radius : r.yMin+radius);
                for (var i=0;i<=segments;i++)
                {
                    var angle=(corner*90f+i*90f/segments)*Mathf.Deg2Rad;
                    vh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,c,Vector2.zero);
                }
            }
            var count=4*(segments+1);
            for(var i=0;i<count;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }
}
