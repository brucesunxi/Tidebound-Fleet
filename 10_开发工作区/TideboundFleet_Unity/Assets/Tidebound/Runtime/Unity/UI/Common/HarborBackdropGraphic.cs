using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Rounded texture geometry; does not require a stencil buffer in the initial Game View.</summary>
    public sealed class HarborBackdropGraphic : RawImage
    {
        public float Radius=11;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var radius=Mathf.Min(Radius,Mathf.Min(r.width,r.height)*.5f);
            const int segments=12;
            for(var corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(var i=0;i<segments;i++)
                {
                    var a=(corner*90+i*90f/segments)*Mathf.Deg2Rad;var b=(corner*90+(i+1)*90f/segments)*Mathf.Deg2Rad;
                    var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;var q=center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                    Triangle(vh,r.center,p,q,r);
                    if(i!=segments-1)continue;
                    var next=(corner+1)%4;var c2=new Vector2(next==0||next==3?r.xMax-radius:r.xMin+radius,next<2?r.yMax-radius:r.yMin+radius);
                    Triangle(vh,r.center,q,c2+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,r);
                }
            }
        }
        private void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Rect r)
        {
            var index=vh.currentVertCount;
            foreach(var p in new[]{a,b,c})vh.AddVert(p,color,new Vector2(uvRect.x+uvRect.width*(p.x-r.xMin)/r.width,uvRect.y+uvRect.height*(p.y-r.yMin)/r.height));
            vh.AddTriangle(index,index+1,index+2);
        }
    }
}
