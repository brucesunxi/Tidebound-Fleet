using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Adapts E1/E2 presentation states to meshes or the retained flat comparison view.</summary>
    public sealed class ShipPrototypeAppearance : MonoBehaviour
    {
        private Image flat;
        private GameObject arrow;
        private CanvasGroup group;
        private MeshRenderer hull, shadow;
        private MaterialPropertyBlock properties;
        private Color flatColor;
        private RawImage artwork;
        public float EntryAlpha { get; private set; }=1;
        public float HintStrength { get; private set; }
        public bool IsThreeDimensional { get; private set; }
        public int PaletteSlot { get; private set; }
        public MeshRenderer HullRenderer=>hull;
        public MeshRenderer ShadowRenderer=>shadow;
        public void Initialize(ShipPrototypeResources resources,int length,int slot,bool volume,string skinId=null,bool gameplay=false,System.Func<bool> paused=null)
        {
            flat=GetComponent<Image>();flatColor=flat.color;arrow=transform.Find("Direction").gameObject;
            group=gameObject.AddComponent<CanvasGroup>();PaletteSlot=length==3 ? -1 : slot;
            var root=new GameObject("MeshRoot");root.transform.SetParent(transform,false);
            root.transform.localPosition=((RectTransform)transform).rect.center;
            hull=resources.AddHull(root.transform,length,slot);shadow=resources.AddShadow(root.transform,length);
            if(skinId!=null && (length==2 || gameplay))
            {
                var art=Tidebound.Unity.UI.HarborAppearanceArt.Skin(length==3?Tidebound.Core.FoundationLimits.DefaultStandardSkinId:skinId);
                var rt=Tidebound.Unity.UI.HarborUI.Rect("ActualShipSkin",transform);
                rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.anchoredPosition=Vector2.zero;
                var height=gameplay?1.95f:Mathf.Min(1.82f,.72f/art.aspect);
                rt.sizeDelta=gameplay?new Vector2(.95f,height*(length==3?1.5f:1)):new Vector2(height*art.aspect,height);
                artwork=rt.gameObject.AddComponent<RawImage>();artwork.texture=Resources.Load<Texture2D>(art.resourcePath);
                var uv=Tidebound.Unity.UI.HarborAppearanceArt.UV(art.uv);artwork.uvRect=art.rotation==180?new Rect(uv.xMax,uv.yMax,-uv.width,-uv.height):uv;artwork.raycastTarget=false;
                if(gameplay)gameObject.AddComponent<ShipFloatPresentation>().Initialize(rt,artwork,paused);
            }
            properties=new MaterialPropertyBlock();SetMode(volume);SetEntry(1,1);
        }
        public void SetMode(bool volume)
        {IsThreeDimensional=volume&&artwork==null;flat.enabled=!volume&&artwork==null;arrow.SetActive(!volume&&artwork==null);Paint();}
        public void SetEntry(float alpha,float scale)
        {EntryAlpha=Mathf.Clamp01(alpha);group.alpha=EntryAlpha;transform.localScale=Vector3.one*scale;Paint();}
        public void SetHint(float strength){HintStrength=Mathf.Clamp01(strength);Paint();}
        private void Paint()
        {
            if(hull==null)return;
            hull.enabled=shadow.enabled=IsThreeDimensional && EntryAlpha>0;
            flat.color=Color.Lerp(flatColor,new Color(.98f,.78f,.22f),HintStrength);
            if(artwork)
            {
                var tint=Color.Lerp(Color.white,new Color(1,1,.55f),HintStrength*.55f);
                var floating=GetComponent<ShipFloatPresentation>();if(floating!=null)floating.SetTint(tint);else artwork.color=tint;
            }
            properties.SetFloat("_Reveal",EntryAlpha);properties.SetFloat("_Hint",HintStrength);
            hull.SetPropertyBlock(properties);shadow.SetPropertyBlock(properties);
        }
    }
}
