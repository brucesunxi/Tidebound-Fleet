using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    [RequireComponent(typeof(Button))]
    public class HarborPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform Face;
        public Graphic Surface;
        public float Travel = 2;
        private Button button;
        private bool held, lastEnabled = true;
        private Vector2 rest;
        private Color restColor = Color.white;
        public bool IsPressed => held;
        protected virtual void Awake() { button = GetComponent<Button>(); if (Face) rest = Face.anchoredPosition; if (Surface) restColor = Surface.color; }
        protected virtual void Update()
        {
            if (!button) button = GetComponent<Button>();
            if (lastEnabled != button.interactable)
            {
                lastEnabled = button.interactable; if (!lastEnabled) SetPressed(false);
                if (Surface) Surface.color = lastEnabled ? restColor : restColor * new Color(.64f, .67f, .68f, 1);
            }
        }
        public void SetPressed(bool value)
        {
            if (!button) button = GetComponent<Button>();
            held = value && button.interactable;
            if (Face) Face.anchoredPosition = rest + (held ? Vector2.down * Travel : Vector2.zero);
        }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) SetPressed(true); }
        public void OnPointerUp(PointerEventData e) => SetPressed(false);
        public void OnPointerExit(PointerEventData e) => SetPressed(false);
        protected virtual void OnDisable() => SetPressed(false);
        protected virtual void OnApplicationFocus(bool focused) { if (!focused) SetPressed(false); }
    }
}
