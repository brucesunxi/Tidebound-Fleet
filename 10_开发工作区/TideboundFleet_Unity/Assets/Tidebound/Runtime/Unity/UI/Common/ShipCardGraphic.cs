using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Shared illustrated catalog silhouette. Rarity tint is metadata, not a new production skin.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShipCardGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var scale=Mathf.Min(r.width/60,r.height/84);
            var o=r.center-new Vector2(30,42)*scale;
            Hull(vh,o+Vector2.down*2*scale,scale,1.05f,new Color(.03f,.26f,.36f,.16f),new Color(.03f,.26f,.36f,.16f));
            Hull(vh,o,scale,1,new Color(.045f,.22f,.34f),new Color(.02f,.13f,.23f));
            Hull(vh,o+Vector2.up*2*scale,scale,.90f,Color.Lerp(color,Color.white,.36f),color);
            Hull(vh,o+Vector2.up*3*scale,scale,.73f,HarborUI.Cream,Color.white);
            Panel(vh,o,scale,new Rect(16,18,28,39),7,new Color(.11f,.28f,.34f),new Color(.06f,.18f,.25f));
            Panel(vh,o,scale,new Rect(17,22,26,37),6,Color.white,HarborUI.Cream);
            Panel(vh,o,scale,new Rect(20,45,20,13),4,new Color(.2f,.77f,.91f),new Color(.035f,.35f,.58f));
            Panel(vh,o,scale,new Rect(21,28,18,12),3,new Color(1,.39f,.23f),new Color(.82f,.15f,.09f));
            Panel(vh,o,scale,new Rect(27,60,5,12),2,HarborUI.Gold,HarborDesignTokens.GoldDark);
            Panel(vh,o,scale,new Rect(32,65,10,7),1,new Color(1,.35f,.18f),new Color(.90f,.17f,.08f));
            Panel(vh,o,scale,new Rect(21,47,3,8),1,new Color(.85f,.98f,1),new Color(.43f,.81f,.93f));
            foreach(var x in new[]{12,43})foreach(var y in new[]{26,42})
                Panel(vh,o,scale,new Rect(x,y,5,5),2.5f,HarborUI.Gold,new Color(.83f,.52f,.06f));
        }
        private static void Panel(VertexHelper vh,Vector2 o,float s,Rect r,float radius,Color top,Color bottom)
        {HarborReliefImage.Round(vh,new Rect(o+r.position*s,r.size*s),radius*s,top,bottom);}
        private static void Hull(VertexHelper vh,Vector2 o,float s,float size,Color top,Color bottom)
        {
            var start=vh.currentVertCount;var c=new Vector2(30,42);vh.AddVert(o+c*s,Color.Lerp(bottom,top,.5f),Vector2.zero);
            const int count=40;
            for(var i=0;i<count;i++)
            {
                var a=i*Mathf.PI*2/count;var y=Mathf.Cos(a);
                var p=c+new Vector2(Mathf.Sin(a)*24*(1-Mathf.Max(0,y)*.33f),y*38)*size;
                vh.AddVert(o+p*s,Color.Lerp(bottom,top,(p.y-4)/76),Vector2.zero);
            }
            for(var i=0;i<count;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }
}
