using System;
using UnityEngine;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Only the artwork moves. Grid root, lane pose and logical occupancy remain authoritative.</summary>
    public sealed class ShipFloatPresentation : MonoBehaviour
    {
        private RectTransform visual;
        private RawImage artwork,shadow,shuffleOutline;
        public float ShuffleHighlightRemaining {get;private set;}
        private Func<bool> paused;
        private ShipFeedbackGraphic feedback;
        private float time,impactAge=10,phase;
        private Color tint=Color.white;
        private Material highlightMaterial;
        public void SetTint(Color value){tint=value;}
        public int ImpactCount {get;private set;}
        public float ImpactAge=>impactAge;
        public void Initialize(RectTransform visual,RawImage artwork,Func<bool> paused)
        {
            this.visual=visual;this.artwork=artwork;this.paused=paused;
            phase=Mathf.Abs(transform.parent.name.GetHashCode()%997)/997f*Mathf.PI*2;
            shadow=GameplayArt.Texture("HullContactShadow",visual.parent,"");shadow.texture=artwork.texture;shadow.uvRect=artwork.uvRect;
            shadow.rectTransform.anchorMin=shadow.rectTransform.anchorMax=shadow.rectTransform.pivot=Vector2.one*.5f;
            shadow.rectTransform.sizeDelta=visual.sizeDelta*1.035f;shadow.color=new Color(.015f,.15f,.22f,.40f);
            shadow.transform.SetSiblingIndex(visual.GetSiblingIndex());
            var r=HarborUI.Rect("ImpactAndDizzy",visual.parent);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=Vector2.one;
            feedback=r.gameObject.AddComponent<ShipFeedbackGraphic>();feedback.raycastTarget=false;
        }
        public void HighlightShuffle()
        {
            if(!shuffleOutline)
            {
                shuffleOutline=GameplayArt.Texture("ShuffleRedOutline",visual.parent,"");
                shuffleOutline.texture=artwork.texture;shuffleOutline.uvRect=artwork.uvRect;
                shuffleOutline.rectTransform.anchorMin=shuffleOutline.rectTransform.anchorMax=shuffleOutline.rectTransform.pivot=Vector2.one*.5f;
                shuffleOutline.rectTransform.sizeDelta=visual.sizeDelta;
                shuffleOutline.color=new Color(1,.025f,.06f,1);
                highlightMaterial=new Material(Resources.Load<Shader>("TideboundUI/ShipHighlight"));shuffleOutline.material=highlightMaterial;
                var outline=shuffleOutline.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.035f,.055f,1);outline.effectDistance=new Vector2(.09f,-.09f);
                shuffleOutline.transform.SetSiblingIndex(visual.GetSiblingIndex());
            }
            ShuffleHighlightRemaining=3;
            shuffleOutline.gameObject.SetActive(true);SyncShuffleOutline();
        }
        private void OnDestroy(){if(highlightMaterial)Destroy(highlightMaterial);}
        private void SyncShuffleOutline()
        {
            if(!shuffleOutline)return;
            shuffleOutline.rectTransform.anchoredPosition=visual.anchoredPosition;
            shuffleOutline.rectTransform.localRotation=visual.localRotation;
            shuffleOutline.rectTransform.localScale=visual.localScale;
        }
        public void Impact(){ImpactCount++;impactAge=0;}
        public void ClearImpact(){impactAge=10;feedback.Age=10;feedback.SetVerticesDirty();}
        private void Update()
        {
            if(visual==null||paused?.Invoke()==true)return;
            var dt=Time.unscaledDeltaTime;time+=dt;impactAge+=dt;
            if(ShuffleHighlightRemaining>0)
            {ShuffleHighlightRemaining=Mathf.Max(0,ShuffleHighlightRemaining-dt);shuffleOutline.gameObject.SetActive(ShuffleHighlightRemaining>0);}
            var t=time*1.55f+phase;
            var impulse=impactAge<.26f?Mathf.Sin(impactAge/.26f*Mathf.PI*3)*(1-impactAge/.26f):0;
            visual.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t)*1.35f+impulse*4);
            visual.anchoredPosition=new Vector2(Mathf.Sin(t*.83f)*.012f,Mathf.Sin(t)*.025f-impulse*.045f);
            visual.localScale=new Vector3(1+Mathf.Abs(impulse)*.065f,1-Mathf.Abs(impulse)*.055f,1);
            var localOffset=visual.parent.InverseTransformVector(new Vector3(.075f,-.095f,0));
            shadow.rectTransform.anchoredPosition=(Vector2)localOffset+visual.anchoredPosition*.5f;
            shadow.rectTransform.localRotation=visual.localRotation;
            artwork.color=impactAge<.10f?Color.Lerp(tint,new Color(1,.79f,.63f),.6f):tint;
            SyncShuffleOutline();
            feedback.Age=impactAge;feedback.Bow=visual.sizeDelta.y*.42f;feedback.SetVerticesDirty();
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ShipFeedbackGraphic : MaskableGraphic
    {
        public float Age=10,Bow;
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();if(Age>.95f)return;
            var alpha=Mathf.Clamp01((.95f-Age)/.25f);
            for(var i=0;i<3;i++)
            {
                var a=Age*7+i*Mathf.PI*2/3;
                var p=new Vector2(Mathf.Cos(a)*.38f,Bow+Mathf.Sin(a)*.12f+.05f);
                Star(v,p,.10f,new Color(1,.77f,.12f,alpha));
            }
            if(Age<.27f)for(var i=0;i<7;i++)
            {
                var a=i*Mathf.PI/6;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(.13f+Age*1.7f)+Vector2.up*Bow;
                Star(v,p,.04f,new Color(.85f,1,1,1-Age/.27f));
            }
        }
        private static void Star(VertexHelper v,Vector2 p,float radius,Color c)
        {
            var n=v.currentVertCount;v.AddVert(p,c,Vector2.zero);
            for(var i=0;i<10;i++){var a=Mathf.PI/2+i*Mathf.PI/5;v.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*(i%2==0?1:.43f),c,Vector2.zero);}
            for(var i=0;i<10;i++)v.AddTriangle(n,n+i+1,n+1+(i+1)%10);
        }
    }
}
