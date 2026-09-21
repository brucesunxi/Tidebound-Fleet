using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Shared catalog silhouette; rarity tint is metadata, never a new production skin model.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShipCardGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var scale=Mathf.Min(r.width/60,r.height/84);
            var origin=r.center-new Vector2(30,42)*scale;
            Polygon(vh,origin,scale,new[]{new Vector2(30,82),new Vector2(53,61),new Vector2(55,17),new Vector2(43,3),new Vector2(17,3),new Vector2(5,17),new Vector2(7,61)},new Color(.045f,.22f,.34f));
            Polygon(vh,origin,scale,new[]{new Vector2(30,75),new Vector2(47,58),new Vector2(48,20),new Vector2(39,10),new Vector2(21,10),new Vector2(12,20),new Vector2(13,58)},color);
            Quad(vh,origin,scale,16,22,28,33,HarborUI.Cream);Quad(vh,origin,scale,20,46,20,18,Color.white);
            Quad(vh,origin,scale,20,46,20,8,new Color(.14f,.67f,.83f));Quad(vh,origin,scale,21,26,18,11,new Color(.91f,.32f,.21f));
            Quad(vh,origin,scale,28,59,4,14,HarborUI.Gold);Quad(vh,origin,scale,32,66,10,7,new Color(.95f,.35f,.21f));
        }
        private static void Quad(VertexHelper vh,Vector2 origin,float scale,float x,float y,float w,float h,Color c)
        {Polygon(vh,origin,scale,new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)},c);}
        private static void Polygon(VertexHelper vh,Vector2 origin,float scale,Vector2[] points,Color c)
        {
            var start=vh.currentVertCount;var center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
            vh.AddVert(origin+center*scale,c,Vector2.zero);foreach(var p in points)vh.AddVert(origin+p*scale,c,Vector2.zero);
            for(var i=0;i<points.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
        }
    }
}
