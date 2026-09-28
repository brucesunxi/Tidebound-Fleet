using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    public enum HarborSurfaceKind { Panel, Control, Tab, Card, Inset }

    /// <summary>Shared, maskable pearl surface. Geometry stays inside its fixed hit area.</summary>
    public sealed class HarborImage : Image
    {
        public float Radius=18, Border=2, Depth=4;
        public Color Edge=HarborDesignTokens.PearlWhite;
        public HarborSurfaceKind Kind;
        public bool Selected, Pressed, Focused;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var bounds=GetPixelAdjustedRect();
            if(bounds.width<=0||bounds.height<=0)return;
            if(Border<=0){Round(vh,bounds,Radius,color,color);return;}
            var depth=Mathf.Min(Depth,bounds.height*.12f);
            var face=new Rect(bounds.x+1,bounds.y+depth,bounds.width-2,bounds.height-depth-1);
            var shadow=HarborDesignTokens.Shade(color,.67f);shadow.a=color.a;
            Round(vh,new Rect(face.x,face.y-depth,face.width,face.height),Radius,shadow,shadow);
            if(Pressed){face.y-=depth*.65f;face.height+=depth*.25f;}
            var rim=Selected||Focused?HarborDesignTokens.OceanBlue:Kind==HarborSurfaceKind.Panel?HarborUI.Gold:Edge;rim.a*=color.a;
            var rimTop=HarborDesignTokens.Tint(rim,.22f);rimTop.a=rim.a;
            Round(vh,face,Radius,rimTop,rim);
            var inset=Selected||Focused?2.6f:Kind==HarborSurfaceKind.Panel?Mathf.Max(5,Border):Border;
            face=new Rect(face.x+inset,face.y+inset,face.width-inset*2,face.height-inset*2);
            if(face.width<=0||face.height<=0)return;
            var top=Color.Lerp(color,Color.white,Kind==HarborSurfaceKind.Panel?.28f:.36f);top.a=color.a;
            Round(vh,face,Mathf.Max(1,Radius-inset),top,color);
            if(Kind==HarborSurfaceKind.Card)
            {
                var well=new Rect(face.x+3,face.y+face.height*.36f,face.width-6,face.height*.60f);
                var aqua=Color.Lerp(color,HarborDesignTokens.OceanSoft,.64f);aqua.a=color.a;
                var light=Color.Lerp(aqua,Color.white,.25f);light.a=color.a;
                Round(vh,well,Mathf.Max(3,Radius-5),light,aqua);
            }
            if(Selected)
            {
                var line=new Rect(face.x+face.width*.22f,face.y+2,face.width*.56f,3);
                var blue=HarborDesignTokens.OceanBlue;blue.a=color.a;Round(vh,line,1.5f,blue,blue);
            }
        }
        private static void Round(VertexHelper vh,Rect r,float radius,Color top,Color bottom)
        {HarborReliefImage.Round(vh,r,radius,top,bottom);}
    }
}
