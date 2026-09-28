using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Thin section rules and fixed-size anchor ornaments; the caption is a separate Text.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShowcaseSectionRule : MaskableGraphic
    {
        public float CaptionWidth=100;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var c=r.center;
            var gap=CaptionWidth/2+32;
            Line(vh,new Vector2(r.xMin+3,c.y),new Vector2(c.x-gap,c.y),2,color);
            Line(vh,new Vector2(c.x+gap,c.y),new Vector2(r.xMax-3,c.y),2,color);
            Anchor(vh,c+Vector2.left*(CaptionWidth/2+14),8);
            Anchor(vh,c+Vector2.right*(CaptionWidth/2+14),8);
        }
        private void Anchor(VertexHelper vh,Vector2 c,float radius)
        {
            var top=c+Vector2.up*radius*.7f;
            for(var i=0;i<20;i++)
            {var a=i*Mathf.PI/10;var b=(i+1)*Mathf.PI/10;Line(vh,top+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.2f,top+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.2f,1.4f,color);}
            Line(vh,c+Vector2.up*radius*.48f,c-Vector2.up*radius*.77f,1.8f,color);
            Line(vh,c+new Vector2(-radius*.4f,radius*.2f),c+new Vector2(radius*.4f,radius*.2f),1.6f,color);
            for(var i=0;i<16;i++)
            {var a=(180+i*180f/16)*Mathf.Deg2Rad;var b=(180+(i+1)*180f/16)*Mathf.Deg2Rad;Line(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.7f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.7f,1.8f,color);}
            Triangle(vh,c+new Vector2(-radius*.84f,-radius*.08f),c+new Vector2(-radius*.38f,-radius*.2f),c+new Vector2(-radius*.6f,radius*.2f),color);
            Triangle(vh,c+new Vector2(radius*.84f,-radius*.08f),c+new Vector2(radius*.6f,radius*.2f),c+new Vector2(radius*.38f,-radius*.2f),color);
        }
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Triangle(vh,a+n,b+n,b-n,tint);Triangle(vh,a+n,b-n,a-n,tint);}
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
        {var n=vh.currentVertCount;vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);vh.AddVert(c,tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
    }
}
