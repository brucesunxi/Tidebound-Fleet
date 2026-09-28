using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Small transparent light geometry; no texture allocation, raycasts or external materials.</summary>
    public sealed class HarborHeroLight : MaskableGraphic
    {
        public enum Shape { Rays, Star }
        public Shape Kind;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();var center=r.center;var radius=Mathf.Min(r.width,r.height)*.5f;
            if(Kind==Shape.Star)
            {
                mesh.AddVert(center,color,Vector2.zero);
                for(var i=0;i<8;i++){var a=i*Mathf.PI/4;var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*(i%2==0?1:.16f);mesh.AddVert(p,color,Vector2.zero);}
                for(var i=0;i<8;i++)mesh.AddTriangle(0,i+1,(i+1)%8+1);return;
            }
            for(var i=0;i<10;i++)
            {
                var a=i*Mathf.PI/5;var first=mesh.currentVertCount;var clear=new Color(color.r,color.g,color.b,0);
                mesh.AddVert(center,color,Vector2.zero);
                mesh.AddVert(center+new Vector2(Mathf.Cos(a-.14f),Mathf.Sin(a-.14f))*radius,clear,Vector2.zero);
                mesh.AddVert(center+new Vector2(Mathf.Cos(a+.14f),Mathf.Sin(a+.14f))*radius,clear,Vector2.zero);
                mesh.AddTriangle(first,first+1,first+2);
            }
        }
    }
}
