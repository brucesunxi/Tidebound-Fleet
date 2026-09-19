using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Code-drawn arrow avoids a font or imported-art dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GrayboxArrowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            var points = new[] { new Vector2(-.12f,-.4f),new Vector2(.12f,-.4f),new Vector2(.12f,.05f),
                new Vector2(.36f,.05f),new Vector2(0,.43f),new Vector2(-.36f,.05f),new Vector2(-.12f,.05f) };
            foreach (var p in points) vh.AddVert(new Vector3(r.center.x+p.x*r.width,r.center.y+p.y*r.height),color,Vector2.zero);
            vh.AddTriangle(0,2,1); vh.AddTriangle(0,6,2); vh.AddTriangle(6,4,3); vh.AddTriangle(6,5,4);
        }
    }
}
