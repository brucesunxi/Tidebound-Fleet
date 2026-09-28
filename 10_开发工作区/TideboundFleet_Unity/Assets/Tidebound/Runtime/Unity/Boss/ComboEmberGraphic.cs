using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.Boss
{
 [RequireComponent(typeof(CanvasRenderer))]
 public sealed class ComboEmberGraphic : MaskableGraphic
 {
  public float Clock,Opacity,BaseY=55,Rise=58;public int Tier,ActiveSlots;
  protected override void OnPopulateMesh(VertexHelper v)
  {
   v.Clear();if(Opacity<=0||Tier==0)return;var r=GetPixelAdjustedRect();var sw=r.width/5;
   for(var slot=0;slot<5;slot++)
   {
    if((ActiveSlots&(1<<slot))==0)continue;
    for(var j=0;j<Tier*3;j++)
    {
     var age=Mathf.Repeat(Clock*(.65f+j*.03f)+j*.618f+slot*.17f,1);
     var x=r.x+(slot+.5f)*sw+Mathf.Sin(j*2.4f+Clock*1.3f)*sw*.35f;
     var y=r.y+BaseY+age*Rise;
     var radius=(.65f+j%3*.25f)*(1-age*.6f);var alpha=Opacity*Mathf.Sin(age*Mathf.PI);
     var c=new Vector2(x,y);var n=v.currentVertCount;var col=new Color(1,.87f,.24f,alpha);
     v.AddVert(c+Vector2.up*radius*2,col,Vector2.zero);v.AddVert(c+Vector2.right*radius,col,Vector2.zero);
     v.AddVert(c-Vector2.up*radius*2,new Color(1,.4f,.06f,0),Vector2.zero);v.AddVert(c-Vector2.right*radius,col,Vector2.zero);
     v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);
    }
   }
  }
 }
}
