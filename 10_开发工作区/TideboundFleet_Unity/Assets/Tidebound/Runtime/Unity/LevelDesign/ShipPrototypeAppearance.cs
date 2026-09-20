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
        public float EntryAlpha { get; private set; }=1;
        public float HintStrength { get; private set; }
        public bool IsThreeDimensional { get; private set; }
        public int PaletteSlot { get; private set; }
        public MeshRenderer HullRenderer=>hull;
        public MeshRenderer ShadowRenderer=>shadow;
        public void Initialize(ShipPrototypeResources resources,int length,int slot,bool volume)
        {
            flat=GetComponent<Image>();flatColor=flat.color;arrow=transform.Find("Direction").gameObject;
            group=gameObject.AddComponent<CanvasGroup>();PaletteSlot=length==3 ? -1 : slot;
            var root=new GameObject("MeshRoot");root.transform.SetParent(transform,false);
            root.transform.localPosition=((RectTransform)transform).rect.center;
            hull=resources.AddHull(root.transform,length,slot);shadow=resources.AddShadow(root.transform,length);
            properties=new MaterialPropertyBlock();SetMode(volume);SetEntry(1,1);
        }
        public void SetMode(bool volume)
        {IsThreeDimensional=volume;flat.enabled=!volume;arrow.SetActive(!volume);Paint();}
        public void SetEntry(float alpha,float scale)
        {EntryAlpha=Mathf.Clamp01(alpha);group.alpha=EntryAlpha;transform.localScale=Vector3.one*scale;Paint();}
        public void SetHint(float strength){HintStrength=Mathf.Clamp01(strength);Paint();}
        private void Paint()
        {
            if(hull==null)return;
            hull.enabled=shadow.enabled=IsThreeDimensional && EntryAlpha>0;
            flat.color=Color.Lerp(flatColor,new Color(.98f,.78f,.22f),HintStrength);
            properties.SetFloat("_Reveal",EntryAlpha);properties.SetFloat("_Hint",HintStrength);
            hull.SetPropertyBlock(properties);shadow.SetPropertyBlock(properties);
        }
    }
}
