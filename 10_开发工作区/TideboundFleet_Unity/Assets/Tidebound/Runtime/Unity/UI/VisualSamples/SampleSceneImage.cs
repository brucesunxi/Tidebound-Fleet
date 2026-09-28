using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    // Rounded texture geometry keeps thumbnails cropped, never stretched or baked with text.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SampleSceneImage : MaskableGraphic
    {
        public Texture2D Texture;
        public Rect UV=new Rect(0,0,1,1);
        public float Radius=10;
        public override Texture mainTexture=>Texture?Texture:s_WhiteTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var radius=Mathf.Min(Radius,Mathf.Min(r.width,r.height)/2);
            Add(vh,r.center,r);
            for(var corner=0;corner<4;corner++)
            {
                var c=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(var step=0;step<=8;step++)
                {var a=(corner*90+step*90f/8)*Mathf.Deg2Rad;Add(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,r);}
            }
            for(var n=1;n<vh.currentVertCount;n++)vh.AddTriangle(0,n,n==vh.currentVertCount-1?1:n+1);
        }
        private void Add(VertexHelper vh,Vector2 p,Rect r)
        {vh.AddVert(p,color,new Vector2(UV.x+(p.x-r.xMin)/r.width*UV.width,UV.y+(p.y-r.yMin)/r.height*UV.height));}
    }
}
