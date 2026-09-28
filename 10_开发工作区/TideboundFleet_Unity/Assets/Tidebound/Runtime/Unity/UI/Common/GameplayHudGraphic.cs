using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Reference HUD geometry, independent of the shared gold popup surface.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GameplayHudGraphic : MaskableGraphic
    {
        public enum Style { Pause, Plaque, Wallet, Health, Combo, Tool, Badge }
        public Style Kind;
        public float Fraction=1;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();var r=GetPixelAdjustedRect();var cream=new Color(1,.96f,.79f);
            if(Kind==Style.Badge)
            {
                Round(v,r,r.height/2,new Color(1,.98f,.82f)*color,new Color(1,.95f,.72f)*color);
                Round(v,Inset(r,2),r.height/2,new Color(1,.42f,.19f)*color,new Color(.91f,.20f,.09f)*color);return;
            }
            if(Kind==Style.Tool)
            {
                Round(v,new Rect(r.x,r.y,r.width,r.height-6),17,new Color(.57f,.31f,.12f)*color,new Color(.48f,.25f,.09f)*color);
                r.yMin+=7;Round(v,r,16,new Color(.73f,.49f,.22f)*color,new Color(.73f,.49f,.22f)*color);
                Round(v,Inset(r,1.5f),14.5f,cream*color,cream*color);
                Round(v,Inset(r,4.5f),12,new Color(1,.94f,.45f)*color,new Color(1,.69f,.10f)*color);
                Round(v,Inset(r,6.5f),10,new Color(1,.98f,.68f)*color,new Color(1,.88f,.47f)*color);
                Round(v,Inset(r,7.5f),9,new Color(1,.91f,.31f)*color,new Color(1,.73f,.13f)*color);
                var gloss=Inset(r,8.5f);gloss.yMin=gloss.yMax-gloss.height*.32f;Round(v,gloss,8,new Color(1,1,.85f,.5f)*color,new Color(1,1,.7f,0));return;
            }
            if(Kind==Style.Health)
            {
                Round(v,r,r.height/2,cream,cream);
                var inside=Inset(r,3);Round(v,inside,inside.height/2,new Color(.39f,.23f,.18f),new Color(.39f,.23f,.18f));
                inside.width*=Mathf.Clamp01(Fraction);
                Round(v,inside,inside.height/2,new Color(1,.33f,.45f),new Color(.93f,.13f,.29f));return;
            }
            if(Kind==Style.Pause)
            {
                Round(v,r,r.width/2,new Color(.68f,.43f,.2f),new Color(.68f,.43f,.2f));r.yMin+=2;
                Round(v,r,r.width/2,Color.white,cream);var face=Inset(r,2);
                Round(v,face,face.width/2,new Color(1,.97f,.84f),new Color(.96f,.82f,.57f));
                for(var i=0;i<2;i++){var bar=new Rect(r.x+r.width*(.34f+i*.24f),r.y+r.height*.29f,r.width*.08f,r.height*.42f);Round(v,bar,bar.width/2,new Color(.48f,.31f,.15f),new Color(.48f,.31f,.15f));}return;
            }
            if(Kind==Style.Plaque)
            {
                Round(v,r,13,new Color(.97f,.76f,.45f),new Color(.83f,.59f,.3f));
                Round(v,Inset(r,2),11,new Color(.71f,.45f,.22f),new Color(.52f,.31f,.14f));
                Round(v,Inset(r,5),8,new Color(.84f,.59f,.32f),new Color(.67f,.42f,.20f));
                Round(v,Inset(r,6),7,new Color(.7f,.43f,.20f),new Color(.55f,.32f,.14f));return;
            }
            if(Kind==Style.Combo)
            {
                Round(v,new Rect(r.x,r.y-2,r.width,r.height),r.height/2,new Color(.59f,.24f,.05f),new Color(.59f,.24f,.05f));
                Round(v,r,r.height/2,cream,cream);
                Round(v,Inset(r,2),r.height/2,new Color(1,.81f,.21f),new Color(1,.4f,.055f));return;
            }
            Round(v,r,r.height/2,new Color(1,.98f,.85f),new Color(1,.88f,.63f));
        }
        private static Rect Inset(Rect r,float d)=>new Rect(r.x+d,r.y+d,r.width-2*d,r.height-2*d);
        private static void Round(VertexHelper v,Rect r,float radius,Color a,Color b)=>HarborReliefImage.Round(v,r,radius,a,b);
    }
}
