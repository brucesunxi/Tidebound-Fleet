using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Independent rounded layers keep the bevel and shadow crisp at every canvas scale.</summary>
    public sealed class HarborReliefImage : Image
    {
        public bool Gold;
        public Texture FaceTexture;
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
                vh.AddVert(new Vector2(bounds.xMin,bounds.yMin),Color.white,Vector2.zero);
                vh.AddVert(new Vector2(bounds.xMin,bounds.yMax),Color.white,Vector2.up);
                vh.AddVert(new Vector2(bounds.xMax,bounds.yMax),Color.white,Vector2.one);
                vh.AddVert(new Vector2(bounds.xMax,bounds.yMin),Color.white,Vector2.right);
                vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
            }
            var face=new Rect(bounds.x+3,bounds.y+Depth+3,bounds.width-6,bounds.height-Depth-6);
            for(var i=5;i>=1;i--)
                Round(vh,Inset(Shift(face,0,-Depth-1),-i*.7f),Radius+i*.7f,new Color(.015f,.12f,.15f,.018f),new Color(.01f,.09f,.12f,.045f));
            var baseRect=Shift(face,0,-Depth);
            Round(vh,baseRect,Radius,Gold?C(211,119,8):C(166,160,132),Gold?C(160,76,4):C(134,141,123));
            Round(vh,Inset(Shift(baseRect,0,1.5f),1),Radius-1,Gold?C(255,191,39):C(247,233,194),Gold?C(224,132,13):C(194,184,148));
            face=Shift(face,0,-depression*Depth*.75f);
            Round(vh,face,Radius,Gold?C(255,239,135):C(255,255,242),Gold?C(237,155,15):C(231,219,181));
            Round(vh,Inset(face,1.4f),Radius-1.4f,Gold?C(255,255,199):Color.white,Gold?C(255,211,58):C(255,247,218));
            Round(vh,Inset(face,3.3f),Radius-3.3f,Gold?C(255,228,65):C(210,247,242),Gold?C(255,189,24):C(255,250,226));
            Round(vh,Inset(face,5.2f),Radius-5.2f,Gold?C(255,237,87):C(218,248,243),Gold?C(255,197,30):C(255,252,232));
            var gloss=Inset(face,6.2f);gloss.yMin=gloss.yMax-gloss.height*.43f;
            Round(vh,gloss,Mathf.Max(5,Radius-6),new Color(1,1,1,.28f),new Color(1,1,1,0));
            // Small off-centre glints convey a curved lacquered surface without covering the title.
            var glint=new Rect(face.x+Radius*.48f,face.yMax-12,Mathf.Min(32,face.width*.18f),2.3f);
            Round(vh,glint,1.1f,new Color(1,1,1,.87f),new Color(1,1,1,.48f));
        }
        private static Color C(int r,int g,int b)=>new Color(r/255f,g/255f,b/255f);
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
