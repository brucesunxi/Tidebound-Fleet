using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Shared nine-slice graphic promoted from the approved visual sample. Texture crop, fixed corners and stretchable center are explicit.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class HarborSliceGraphic : MaskableGraphic
    {
        public Texture2D Texture;
        public Rect UV = new Rect(0, 0, 1, 1);
        // Left, bottom, right, top as fractions of UV; displayed border sizes are canvas units.
        public Vector4 Cuts;
        public Vector4 Border;
        public override Texture mainTexture => Texture != null ? Texture : s_WhiteTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            var kx = Mathf.Min(1, r.width / Mathf.Max(.001f, Border.x + Border.z));
            var ky = Mathf.Min(1, r.height / Mathf.Max(.001f, Border.y + Border.w));
            var xs = new[] { r.xMin, r.xMin + Border.x * kx, r.xMax - Border.z * kx, r.xMax };
            var ys = new[] { r.yMin, r.yMin + Border.y * ky, r.yMax - Border.w * ky, r.yMax };
            var us = new[] { UV.xMin, UV.xMin + UV.width * Cuts.x, UV.xMax - UV.width * Cuts.z, UV.xMax };
            var vs = new[] { UV.yMin, UV.yMin + UV.height * Cuts.y, UV.yMax - UV.height * Cuts.w, UV.yMax };
            for (var y = 0; y < 4; y++) for (var x = 0; x < 4; x++)
                vh.AddVert(new Vector3(xs[x], ys[y]), color, new Vector2(us[x], vs[y]));
            for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
            { var n = y * 4 + x; vh.AddTriangle(n, n + 4, n + 5); vh.AddTriangle(n, n + 5, n + 1); }
        }
    }
}
