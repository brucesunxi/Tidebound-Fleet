using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Subtle voyage-call shimmer. It never moves the hit target and obeys reduced motion.</summary>
    public sealed class HarborMainActionPulse : MonoBehaviour
    {
        public Func<bool> AllowMotion;
        public Graphic Glow;
        public RectTransform Emblem;
        private float time;
        private Color baseColor;
        private Quaternion emblemRotation;

        private void Awake(){if(Glow!=null)baseColor=Glow.color;if(Emblem!=null)emblemRotation=Emblem.localRotation;}
        private void OnEnable(){time=0;Apply(false);}
        private void OnDisable(){time=0;Apply(false);}
        private void Update()
        {
            var animate=AllowMotion?.Invoke()!=false;
            if(animate)time+=Time.unscaledDeltaTime;
            Apply(animate);
        }
        private void Apply(bool animate)
        {
            if(Glow==null)return;
            var pulse=animate ? .5f+.5f*Mathf.Sin(time*2.2f) : 0;
            Glow.color=new Color(baseColor.r,baseColor.g,baseColor.b,Mathf.Lerp(.08f,.24f,pulse));
            Glow.rectTransform.localScale=Vector3.one*Mathf.Lerp(1.015f,1.07f,pulse);
            if(Emblem!=null)
            {
                Emblem.localScale=Vector3.one*Mathf.Lerp(1f,1.045f,pulse);
                Emblem.localRotation=emblemRotation*Quaternion.Euler(0,0,animate?Mathf.Sin(time*.95f)*1.8f:0);
            }
        }
    }
}
