using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.Boss
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CombatSparkGraphic : MaskableGraphic
    {
        public bool Hit;
        public int Tier;
        public float Phase;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=GetPixelAdjustedRect();var p=r.center;var radius=Mathf.Min(r.width,r.height)*.5f;
            Disc(v,p,radius,new Color(1,.5f,.02f,color.a*.55f),new Color(1,.4f,0,0));
            if(Hit)
            {
                for(var i=0;i<8;i++)
                {var a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var side=new Vector2(-d.y,d.x);var n=v.currentVertCount;
                    v.AddVert(p+side*radius*.13f,new Color(1,1,.78f,color.a),Vector2.zero);v.AddVert(p+d*radius,new Color(1,.87f,.25f,color.a),Vector2.zero);v.AddVert(p-side*radius*.13f,new Color(1,1,.78f,color.a),Vector2.zero);v.AddTriangle(n,n+1,n+2);}
            }
            else
            {
                var n=v.currentVertCount;v.AddVert(p+Vector2.left*radius*.3f,new Color(1,.87f,.18f,color.a),Vector2.zero);v.AddVert(p+Vector2.right*radius*.3f,new Color(1,.87f,.18f,color.a),Vector2.zero);v.AddVert(p+Vector2.down*r.height*.5f,new Color(1,.42f,0,0),Vector2.zero);v.AddTriangle(n,n+1,n+2);
            }
            if(Tier>0)
            {
                for(var i=0;i<Tier*3;i++)
                {var angle=i*2.399f+Phase*4;var offset=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(radius*.55f);Disc(v,p+offset,radius*.10f,new Color(1,1,.6f,color.a),new Color(1,.65f,.1f,0));}
            }
            Disc(v,p,radius*.35f,Tier>=3?new Color(.7f,1,1,color.a):new Color(1,1,1,color.a),new Color(1,.95f,.55f,color.a));
        }
        private static void Disc(VertexHelper v,Vector2 p,float radius,Color inside,Color outside)
        {var n=v.currentVertCount;v.AddVert(p,inside,Vector2.zero);for(var i=0;i<24;i++){var a=i*Mathf.PI/12;v.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,outside,Vector2.zero);}for(var i=0;i<24;i++)v.AddTriangle(n,n+1+i,n+1+(i+1)%24);}
    }
}
