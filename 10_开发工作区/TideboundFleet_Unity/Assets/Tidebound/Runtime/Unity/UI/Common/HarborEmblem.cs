using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Small original gear/anchor emblems, rendered as layered vector geometry.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HarborEmblem : MaskableGraphic
    {
        public bool Coin;
        public bool Captain;
        public bool AnchorOnly;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var c=r.center;var radius=Mathf.Min(r.width,r.height)*.46f;
            if(Coin)
            {
                Disk(vh,c+Vector2.down*2,radius,HarborDesignTokens.GoldDark);
                Disk(vh,c,radius,HarborDesignTokens.Gold);Disk(vh,c+Vector2.up*.6f,radius*.87f,HarborDesignTokens.PearlWarm);
                Disk(vh,c,radius*.76f,HarborDesignTokens.GoldDark);Disk(vh,c+Vector2.up,radius*.69f,HarborDesignTokens.Gold);
                Anchor(vh,c+Vector2.down,radius*.59f,HarborDesignTokens.GoldDark);Anchor(vh,c+Vector2.up*.5f,radius*.59f,HarborDesignTokens.PearlWarm);
            }
            else if(AnchorOnly)
            {
                Anchor(vh,c+Vector2.down*radius*.04f,radius*.84f,HarborDesignTokens.GoldDark);
                Anchor(vh,c+Vector2.up*radius*.02f,radius*.72f,HarborDesignTokens.PearlWhite);
            }
            else if(Captain)
            {
                Disk(vh,c+Vector2.down*1.5f,radius,new Color(HarborDesignTokens.OceanShadow.r,HarborDesignTokens.OceanShadow.g,HarborDesignTokens.OceanShadow.b,1));
                Disk(vh,c,radius,HarborDesignTokens.Gold);Disk(vh,c,radius*.78f,HarborDesignTokens.PearlWhite);
                Gear(vh,c,radius*.63f,HarborDesignTokens.OceanBlue);Disk(vh,c,radius*.19f,HarborDesignTokens.Gold);
            }
            else
            {
                Gear(vh,c+Vector2.down*2,radius,HarborDesignTokens.OceanShadow);Gear(vh,c,radius,HarborDesignTokens.OceanBlue);
                Disk(vh,c,radius*.66f,HarborDesignTokens.OceanBlue);Disk(vh,c+Vector2.up*.8f,radius*.42f,HarborDesignTokens.OceanShadow);Disk(vh,c,radius*.32f,HarborDesignTokens.PearlWarm);
            }
        }
        private static void Gear(VertexHelper vh,Vector2 c,float radius,Color color)
        {for(var i=0;i<48;i++){var a=i*Mathf.PI*2/48;var b=(i+1)*Mathf.PI*2/48;var r1=i%6<3?radius:radius*.77f;var r2=(i+1)%6<3?radius:radius*.77f;Triangle(vh,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r1,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r2,color);}}
        private static void Anchor(VertexHelper vh,Vector2 c,float r,Color color)
        {
            var t=r*.16f;Line(vh,c+Vector2.up*r*.55f,c-Vector2.up*r*.65f,t,color);Line(vh,c+new Vector2(-r*.5f,r*.18f),c+new Vector2(r*.5f,r*.18f),t,color);
            for(var i=0;i<16;i++){var a=(180+i*180f/16)*Mathf.Deg2Rad;var b=(180+(i+1)*180f/16)*Mathf.Deg2Rad;Line(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r*.72f,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r*.72f,t,color);}
            Disk(vh,c+Vector2.up*r*.65f,r*.25f,color);Disk(vh,c+Vector2.up*r*.65f,r*.12f,HarborDesignTokens.Gold);
        }
        private static void Disk(VertexHelper vh,Vector2 c,float r,Color color)
        {for(var i=0;i<48;i++){var a=i*Mathf.PI*2/48;var b=(i+1)*Mathf.PI*2/48;Triangle(vh,c,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,color);}}
        private static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;Triangle(vh,a+n,b+n,b-n,color);Triangle(vh,a+n,b-n,a-n,color);}
        private static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {var n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
    }
}
