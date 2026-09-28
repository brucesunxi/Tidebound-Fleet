using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Instant press/focus feedback, without moving the touch rectangle.</summary>
    [RequireComponent(typeof(Button),typeof(HarborImage))]
    public sealed class HarborControlState : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        private HarborImage surface;private Button control;
        private void Awake(){surface=GetComponent<HarborImage>();control=GetComponent<Button>();}
        private void Press(bool value){if(surface==null)return;surface.Pressed=value;surface.SetVerticesDirty();}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&control.IsInteractable())Press(true);}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Press(false);}
        public void OnPointerExit(PointerEventData e){Press(false);}
        public void OnSelect(BaseEventData e){surface.Focused=true;surface.SetVerticesDirty();}
        public void OnDeselect(BaseEventData e){surface.Focused=false;surface.SetVerticesDirty();}
        private void OnDisable(){Press(false);if(surface!=null){surface.Focused=false;surface.SetVerticesDirty();}}
        private void OnApplicationFocus(bool focused){if(!focused)Press(false);}
    }
}
