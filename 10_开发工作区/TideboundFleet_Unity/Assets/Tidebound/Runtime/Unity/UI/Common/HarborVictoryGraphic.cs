using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Code-native gold stars and radial light. Decorative only; never owns input or rewards.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HarborVictoryGraphic : MaskableGraphic
    {
        public bool Celebration;
        public float Phase,TitleTop=78,CountY=593,DesignHeight=844;
        public bool FlyStar=true;
        public Rect ShadeBounds;
        public bool ShadeOnly;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            if(!Celebration){Star(vh,r.center,Mathf.Min(r.width,r.height)*.46f,0,1);return;}
            // Shade may extend into the notch/home-indicator margins; content remains inside the safe area.
            var shade=new Color(.009f,.085f,.12f,.91f);
            vh.AddVert(new Vector2(ShadeBounds.xMin,ShadeBounds.yMin),shade,Vector2.zero);
            vh.AddVert(new Vector2(ShadeBounds.xMin,ShadeBounds.yMax),shade,Vector2.zero);
            vh.AddVert(new Vector2(ShadeBounds.xMax,ShadeBounds.yMax),shade,Vector2.zero);
            vh.AddVert(new Vector2(ShadeBounds.xMax,ShadeBounds.yMin),shade,Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(2,3,0);
            if(ShadeOnly)return;
            var scale=r.width/390f;var center=new Vector2(r.x+195*scale,r.y+(DesignHeight-TitleTop-66)*scale);
            Disc(vh,center,177*scale,new Color(1,.83f,.25f,.26f),new Color(1,.8f,.18f,0));
            for(var i=0;i<12;i++)
            {
                var a=Phase*Mathf.PI*2/28+i*Mathf.PI/6;
                var left=new Vector2(Mathf.Cos(a-.13f),Mathf.Sin(a-.13f));var right=new Vector2(Mathf.Cos(a+.13f),Mathf.Sin(a+.13f));
                var n=vh.currentVertCount;var clear=new Color(1,.83f,.26f,0);var gold=new Color(1,.87f,.39f,.38f);
                vh.AddVert(center,clear,Vector2.zero);vh.AddVert(center+left*100*scale,gold,Vector2.zero);vh.AddVert(center+right*100*scale,gold,Vector2.zero);
                vh.AddVert(center+left*176*scale,clear,Vector2.zero);vh.AddVert(center+right*176*scale,clear,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+1,n+3,n+4);vh.AddTriangle(n+1,n+4,n+2);
            }
            var phase=Phase<2.8f?Phase:(Phase-2.8f)%3.4f;var count=Phase<2.8f?5:3;
            for(var i=0;i<count;i++)
            {
                var start=Phase<2.8f?.12f+i*.125f:.15f+i*.3f;var p=(phase-start)/1.35f;
                if(p<0||p>1)continue;
                var x=169+i%3*25+(i%2==0?-1:1)*(35+i*7)*p;
                var y=TitleTop+75-155*p+85*p*p;
                Star(vh,new Vector2(r.x+x*scale,r.y+(DesignHeight-y)*scale),(9+i%3*3)*scale,p*(i%2==0?-.8f:.9f),Mathf.Clamp01((1-p)*5)*Mathf.Clamp01(p*10));
            }
            if(FlyStar&&Phase>.62f&&Phase<1.24f)
            {
                var p=(Phase-.62f)/.62f;var x=Mathf.Lerp(111,176,p)+Mathf.Sin(p*Mathf.PI)*48;
                var y=Mathf.Lerp(TitleTop+171,CountY,p);
                Star(vh,new Vector2(r.x+x*scale,r.y+(DesignHeight-y)*scale),Mathf.Lerp(18,11,p)*scale,p*.7f,1);
            }
        }
        private static void Disc(VertexHelper v,Vector2 c,float radius,Color middle,Color edge)
        {
            var n=v.currentVertCount;v.AddVert(c,middle,Vector2.zero);
            for(var i=0;i<64;i++){var a=i*Mathf.PI/32;v.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,edge,Vector2.zero);}
            for(var i=0;i<64;i++)v.AddTriangle(n,n+1+i,n+1+(i+1)%64);
        }
        private static void Star(VertexHelper v,Vector2 c,float radius,float rotation,float alpha)
        {
            Fan(v,c+Vector2.down*radius*.1f,radius,rotation,new Color(.72f,.43f,.07f,alpha),new Color(.72f,.43f,.07f,alpha));
            Fan(v,c,radius,rotation,new Color(1,.97f,.65f,alpha),new Color(.98f,.66f,.09f,alpha));
            Fan(v,c+Vector2.up*radius*.06f,radius*.83f,rotation,new Color(1,1,.82f,alpha),new Color(1,.78f,.19f,alpha));
        }
        private static void Fan(VertexHelper v,Vector2 c,float radius,float rotation,Color top,Color bottom)
        {
            var n=v.currentVertCount;v.AddVert(c,Color.Lerp(bottom,top,.60f),Vector2.zero);
            for(var i=0;i<10;i++)
            {var a=Mathf.PI/2+i*Mathf.PI/5+rotation;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(i%2==0?radius:radius*.49f);v.AddVert(c+p,Color.Lerp(bottom,top,(p.y/radius+1)*.5f),Vector2.zero);}
            for(var i=0;i<10;i++)v.AddTriangle(n,n+1+i,n+1+(i+1)%10);
        }
    }
}
