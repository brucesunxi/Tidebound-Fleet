using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    /// <summary>Native UI geometry for scalable semantic glyphs and the card's independent blue image field.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SampleVectorGraphic : MaskableGraphic
    {
        public enum Shape { Field, Outline, Check, Lock, Cross, Magnifier }
        public Shape Kind;
        public Color Bottom = new Color(.77f, .94f, 1);
        public float Radius = 8, Stroke = 2;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            if (Kind == Shape.Check) { Line(vh, P(r,.23f,.50f), P(r,.43f,.29f), r.width*.12f, color); Line(vh,P(r,.43f,.29f),P(r,.79f,.73f),r.width*.12f,color); return; }
            if (Kind == Shape.Cross) { Line(vh,P(r,.25f,.25f),P(r,.75f,.75f),r.width*.15f,color);Line(vh,P(r,.25f,.75f),P(r,.75f,.25f),r.width*.15f,color); return; }
            if (Kind == Shape.Lock)
            {
                for(var i=0;i<16;i++) {var a=i*Mathf.PI/16;var b=(i+1)*Mathf.PI/16;Line(vh,P(r,.5f,.58f)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r.width*.20f,P(r,.5f,.58f)+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r.width*.20f,r.width*.09f,color);}
                Quad(vh,P(r,.23f,.21f),P(r,.77f,.60f),color);Disk(vh,P(r,.5f,.41f),r.width*.055f,new Color(.10f,.29f,.44f));return;
            }
            if(Kind==Shape.Magnifier)
            {
                for(var i=0;i<24;i++){var a=i*Mathf.PI/12;var b=(i+1)*Mathf.PI/12;var c=P(r,.43f,.58f);Line(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r.width*.24f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r.width*.24f,r.width*.10f,color);}
                Line(vh,P(r,.60f,.40f),P(r,.84f,.16f),r.width*.13f,color);return;
            }
            const int segments = 12;
            var radius = Mathf.Min(Radius, Mathf.Min(r.width,r.height)/2);
            for (var corner=0;corner<4;corner++) for(var i=0;i<segments;i++)
            {
                var a=(corner*90+i*90f/segments)*Mathf.Deg2Rad; var b=(corner*90+(i+1)*90f/segments)*Mathf.Deg2Rad;
                var c=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                var p=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;var q=c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                if(Kind==Shape.Outline) Line(vh,p,q,Stroke,color); else Triangle(vh,r.center,p,q,r);
                if(i==segments-1)
                {
                    var nc=(corner+1)%4;
                    var c2=new Vector2(nc==0||nc==3?r.xMax-radius:r.xMin+radius,nc<2?r.yMax-radius:r.yMin+radius);
                    var p2=c2+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                    if(Kind==Shape.Outline) Line(vh,q,p2,Stroke,color);else Triangle(vh,r.center,q,p2,r);
                }
            }
        }
        private static Vector2 P(Rect r,float x,float y)=>new Vector2(r.x+x*r.width,r.y+y*r.height);
        private void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Rect r)
        {var n=vh.currentVertCount;foreach(var p in new[]{a,b,c})vh.AddVert(p,Color.Lerp(Bottom,color,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
        private static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Color color)
        {var n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(new Vector2(b.x,a.y),color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(new Vector2(a.x,b.y),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
        private static void Disk(VertexHelper vh,Vector2 c,float radius,Color color)
        {for(var i=0;i<16;i++){var n=vh.currentVertCount;vh.AddVert(c,color,Vector2.zero);vh.AddVert(c+new Vector2(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8))*radius,color,Vector2.zero);vh.AddVert(c+new Vector2(Mathf.Cos((i+1)*Mathf.PI/8),Mathf.Sin((i+1)*Mathf.PI/8))*radius,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {var d=b-a;if(d.sqrMagnitude<.0001f)return;var v=new Vector2(-d.y,d.x).normalized*width/2;var n=vh.currentVertCount;vh.AddVert(a+v,color,Vector2.zero);vh.AddVert(b+v,color,Vector2.zero);vh.AddVert(b-v,color,Vector2.zero);vh.AddVert(a-v,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);Disk(vh,a,width/2,color);Disk(vh,b,width/2,color);}
    }
}
