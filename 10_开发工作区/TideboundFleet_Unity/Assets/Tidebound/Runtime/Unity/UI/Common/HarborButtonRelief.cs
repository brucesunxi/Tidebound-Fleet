using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Move only the painted face/content. The raycast target and navigation rectangle never move.</summary>
    [RequireComponent(typeof(Button),typeof(HarborReliefImage))]
    public sealed class HarborButtonRelief : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform Content;
        public Func<bool> AllowMotion;
        public RectTransform FloatingIcon;
        private Vector2 iconOrigin;
        private float floatTime;
        public void SetFloatingIcon(RectTransform icon){FloatingIcon=icon;iconOrigin=icon.anchoredPosition;}
        public float PressAmount { get; private set; }
        private bool held;
        private Button button;
        private HarborReliefImage surface;
        private void Awake(){button=GetComponent<Button>();surface=GetComponent<HarborReliefImage>();}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&button.IsInteractable())held=true;}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)held=false;}
        public void OnPointerExit(PointerEventData e){held=false;}
        private void OnApplicationFocus(bool focus){if(!focus)ResetFace();}
        private void OnDisable(){ResetFace();}
        private void ResetFace(){held=false;PressAmount=0;floatTime=0;Apply();if(FloatingIcon!=null)FloatingIcon.anchoredPosition=iconOrigin;}
        private void Update()
        {
            if(!button.IsInteractable())held=false;
            var target=held?1f:0f;
            PressAmount=AllowMotion?.Invoke()==false?target:Mathf.MoveTowards(PressAmount,target,Time.unscaledDeltaTime*(held?14:8));
            Apply();
            if(FloatingIcon!=null)
            {
                var animate=AllowMotion?.Invoke()!=false && button.IsInteractable();
                if(animate)floatTime+=Time.unscaledDeltaTime;
                FloatingIcon.anchoredPosition=iconOrigin+Vector2.up*(animate?Mathf.Sin(floatTime*1.55f)*1.25f:0);
            }
        }
        private void Apply()
        {
            if(surface==null)return;surface.Depression=PressAmount;
            if(Content!=null){var y=surface.Depth*.5f-PressAmount*surface.Depth*(surface.FaceTexture!=null?.375f:.75f);Content.offsetMin=new Vector2(0,y);Content.offsetMax=new Vector2(0,y);}
        }
    }
}
