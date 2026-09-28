using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Independent rounded layers keep the bevel and shadow crisp at every canvas scale.</summary>
    public sealed class HarborReliefImage : Image
    {
        public bool Gold;
        public Texture FaceTexture;
        // Left, bottom, right, top: UV cut lines and fixed canvas-unit corner sizes.
        public Vector4 SliceUV, SliceSize;
        public override Texture mainTexture=>FaceTexture!=null?FaceTexture:base.mainTexture;
        public float Radius=22, Depth=7;
        private float depression;
        public float Depression { get=>depression; set { if(Mathf.Approximately(value,depression))return;depression=value;SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var bounds=GetPixelAdjustedRect();if(bounds.width<12||bounds.height<12)return;
            if(FaceTexture!=null)
            {
                // Compress against the fixed lower edge; the event target never follows the painted face.
                bounds.yMax-=depression*Depth*.75f;
                var sx=Mathf.Min(1,bounds.width/Mathf.Max(1,SliceSize.x+SliceSize.z));
                var sy=Mathf.Min(1,bounds.height/Mathf.Max(1,SliceSize.y+SliceSize.w));
                for(var y=0;y<4;y++)for(var x=0;x<4;x++)
                {
                    var px=x==0?bounds.xMin:x==1?bounds.xMin+SliceSize.x*sx:x==2?bounds.xMax-SliceSize.z*sx:bounds.xMax;
                    var py=y==0?bounds.yMin:y==1?bounds.yMin+SliceSize.y*sy:y==2?bounds.yMax-SliceSize.w*sy:bounds.yMax;
                    var u=x==0?0:x==1?SliceUV.x:x==2?1-SliceUV.z:1;
                    var v=y==0?0:y==1?SliceUV.y:y==2?1-SliceUV.w:1;
                    vh.AddVert(new Vector2(px,py),color,new Vector2(u,v));
                }
                for(var y=0;y<3;y++)for(var x=0;x<3;x++)
                {var i=y*4+x;vh.AddTriangle(i,i+4,i+5);vh.AddTriangle(i,i+5,i+1);}return;
            }
            var face=new Rect(bounds.x+3,bounds.y+Depth+3,bounds.width-6,bounds.height-Depth-6);
            var shadow=HarborDesignTokens.OceanShadow;
            for(var i=5;i>=1;i--)
                Round(vh,Inset(Shift(face,0,-Depth-1),-i*.7f),Radius+i*.7f,new Color(shadow.r,shadow.g,shadow.b,.018f),new Color(shadow.r,shadow.g,shadow.b,.045f));
            var baseRect=Shift(face,0,-Depth);
            Round(vh,baseRect,Radius,T(Gold?HarborDesignTokens.GoldDark:HarborDesignTokens.OceanShadow),T(Gold?HarborDesignTokens.GoldDark:HarborDesignTokens.OceanShadow));
            Round(vh,Inset(Shift(baseRect,0,1.5f),1),Radius-1,T(Gold?HarborDesignTokens.Gold:HarborDesignTokens.PearlWarm),T(Gold?HarborDesignTokens.GoldDark:HarborDesignTokens.OceanBlue));
            face=Shift(face,0,-depression*Depth*.75f);
            Round(vh,face,Radius,T(Gold?HarborDesignTokens.Tint(HarborDesignTokens.Gold,.68f):HarborDesignTokens.PearlWhite),T(Gold?HarborDesignTokens.GoldDark:HarborDesignTokens.OceanSoft));
            Round(vh,Inset(face,1.4f),Radius-1.4f,T(HarborDesignTokens.PearlWhite),T(Gold?HarborDesignTokens.Gold:HarborDesignTokens.PearlWarm));
            Round(vh,Inset(face,3.3f),Radius-3.3f,T(Gold?HarborDesignTokens.Tint(HarborDesignTokens.Gold,.28f):HarborDesignTokens.OceanSoft),T(Gold?HarborDesignTokens.Gold:HarborDesignTokens.PearlWhite));
            Round(vh,Inset(face,5.2f),Radius-5.2f,T(Gold?HarborDesignTokens.Tint(HarborDesignTokens.Gold,.36f):HarborDesignTokens.Tint(HarborDesignTokens.OceanBlue,.88f)),T(Gold?HarborDesignTokens.Gold:HarborDesignTokens.PearlWhite));
            var gloss=Inset(face,6.2f);gloss.yMin=gloss.yMax-gloss.height*.43f;
            Round(vh,gloss,Mathf.Max(5,Radius-6),new Color(1,1,1,.28f),new Color(1,1,1,0));
            // Small off-centre glints convey a curved lacquered surface without covering the title.
            var glint=new Rect(face.x+Radius*.48f,face.yMax-12,Mathf.Min(32,face.width*.18f),2.3f);
            Round(vh,glint,1.1f,new Color(1,1,1,.87f),new Color(1,1,1,.48f));
        }
        private Color T(Color source)=>new Color(source.r*color.r,source.g*color.g,source.b*color.b,source.a*color.a);
        private static Rect Shift(Rect r,float x,float y)=>new Rect(r.x+x,r.y+y,r.width,r.height);
        private static Rect Inset(Rect r,float v)=>new Rect(r.x+v,r.y+v,r.width-v*2,r.height-v*2);
        internal static void Round(VertexHelper vh,Rect r,float radius,Color top,Color bottom)
        {
            if(r.width<=0||r.height<=0)return;
            var start=vh.currentVertCount;vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
            radius=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)/2);const int segments=12;
            for(var corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
                for(var i=0;i<=segments;i++)
                {
                    var angle=(corner*90f+i*90f/segments)*Mathf.Deg2Rad;
                    var point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    vh.AddVert(point,Color.Lerp(bottom,top,(point.y-r.yMin)/r.height),Vector2.zero);
                }
            }
            const int count=4*(segments+1);for(var i=0;i<count;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }
}
