using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    public enum SettingsIconKind { Music, Sound, Home, Skip, Play, Share }
    /// <summary>Small code-native icons; no added external asset or font dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HarborSettingsIcon : MaskableGraphic
    {
        public SettingsIconKind Kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            if (Kind == SettingsIconKind.Music)
            { Box(vh,r,.38f,.18f,.13f,.69f); Poly(vh,r,new[]{new Vector2(.38f,.74f),new Vector2(.38f,.91f),new Vector2(.88f,1),new Vector2(.88f,.84f)}); Box(vh,r,.75f,.31f,.13f,.58f); Dot(vh,r,.28f,.18f,.39f,.27f); Dot(vh,r,.65f,.31f,.39f,.27f); }
            else if (Kind == SettingsIconKind.Sound)
            { Box(vh,r,.08f,.36f,.28f,.28f); Poly(vh,r,new[]{new Vector2(.27f,.36f),new Vector2(.56f,.15f),new Vector2(.56f,.85f),new Vector2(.27f,.64f)}); Arc(vh,r,.3f); Arc(vh,r,.45f); }
            else if (Kind == SettingsIconKind.Home)
            { Poly(vh,r,new[]{new Vector2(.07f,.51f),new Vector2(.5f,.9f),new Vector2(.93f,.51f),new Vector2(.81f,.43f),new Vector2(.5f,.72f),new Vector2(.19f,.43f)});Box(vh,r,.21f,.13f,.23f,.43f);Box(vh,r,.57f,.13f,.22f,.43f);Box(vh,r,.35f,.4f,.29f,.22f); }
            else if(Kind==SettingsIconKind.Share)
            {
                // Convex strips keep the curved tail hollow; a fan over the concave outline fills it incorrectly.
                for(var i=0;i<18;i++)
                {
                    var a=i/18f;var b=(i+1)/18f;var pa=ShareCurve(a);var pb=ShareCurve(b);
                    var na=ShareNormal(a)*Mathf.Lerp(.006f,.11f,a);var nb=ShareNormal(b)*Mathf.Lerp(.006f,.11f,b);
                    Poly(vh,r,new[]{pa-na,pb-nb,pb+nb,pa+na});
                }
                Poly(vh,r,new[]{new Vector2(.55f,.23f),new Vector2(.96f,.60f),new Vector2(.55f,.94f)});
            }
            else
            { Poly(vh,r,new[]{new Vector2(.16f,.14f),new Vector2(.16f,.86f),new Vector2(.72f,.5f)});if(Kind==SettingsIconKind.Skip)Box(vh,r,.78f,.14f,.13f,.72f); }
        }
        private static Vector2 ShareCurve(float t)=>Mathf.Pow(1-t,2)*new Vector2(.1f,.16f)+2*(1-t)*t*new Vector2(.08f,.63f)+t*t*new Vector2(.59f,.60f);
        private static Vector2 ShareNormal(float t)
        {var d=2*(1-t)*new Vector2(-.02f,.47f)+2*t*new Vector2(.51f,-.03f);return new Vector2(-d.y,d.x).normalized;}
        private void Box(VertexHelper v,Rect r,float x,float y,float w,float h) => HarborReliefImage.Round(v,new Rect(r.x+x*r.width,r.y+y*r.height,w*r.width,h*r.height),1,color,color);
        private void Dot(VertexHelper v,Rect r,float x,float y,float w,float h)
        {
            var n=v.currentVertCount;var center=new Vector2(r.x+x*r.width,r.y+y*r.height);v.AddVert(center,color,Vector2.zero);
            for(var i=0;i<24;i++){var angle=i*Mathf.PI/12;v.AddVert(center+new Vector2(Mathf.Cos(angle)*w*r.width/2,Mathf.Sin(angle)*h*r.height/2),color,Vector2.zero);}
            for(var i=0;i<24;i++)v.AddTriangle(n,n+1+i,n+1+(i+1)%24);
        }
        private void Poly(VertexHelper v,Rect r,Vector2[] pts)
        {var n=v.currentVertCount;foreach(var p in pts)v.AddVert(new Vector2(r.x+p.x*r.width,r.y+p.y*r.height),color,Vector2.zero);for(var i=1;i<pts.Length-1;i++)v.AddTriangle(n,n+i,n+i+1);}
        private void Arc(VertexHelper v,Rect r,float radius)
        {for(var i=0;i<16;i++){var a=-.75f+i*1.5f/16;var b=a+1.5f/16;var origin=new Vector2(.53f,.5f);Poly(v,r,new[]{origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,origin+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,origin+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-.06f),origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-.06f)});}}
    }
}
