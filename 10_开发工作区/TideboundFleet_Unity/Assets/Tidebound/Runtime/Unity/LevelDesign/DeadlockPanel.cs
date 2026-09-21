using Tidebound.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Passive notice only: never intercepts input, pauses, spends or opens another panel.</summary>
    public sealed class DeadlockPanel : MonoBehaviour
    {
        private Text message;
        private CanvasGroup visibility;
        public ShipTool RecommendedTool { get; private set; }
        public string Message => message.text;
        public void Initialize(Font font)
        {
            visibility = gameObject.AddComponent<CanvasGroup>();
            visibility.blocksRaycasts = false; visibility.interactable = false;
            var rect = new GameObject("DeadlockMessage", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 4); rect.offsetMax = new Vector2(-12, -4);
            message = rect.gameObject.AddComponent<Text>(); message.font = font; message.fontSize = 17;
            message.color = Color.white; message.alignment = TextAnchor.MiddleCenter; message.raycastTarget = false;
        }
        public void Present(ToolInventory inventory, bool toolsEnabled, int usesLeft)
        {
            RecommendedTool = ShipTool.None;
            if (toolsEnabled && usesLeft > 0 && inventory.IsAvailable)
                foreach (var tool in new[] { ShipTool.Shuffle, ShipTool.Rescue, ShipTool.Reverse })
                    if (inventory.Count(tool) > 0) { RecommendedTool = tool; break; }
            message.text = !toolsEnabled || usesLeft == 0 ? "No ships can move. Use Menu to restart." :
                RecommendedTool != ShipTool.None ? "No ships can move. Try " + RecommendedTool + "." :
                "No ships can move. Visit Supplies on the home screen.";
        }
        public void SetVisibility(bool visible) => visibility.alpha = visible ? 1 : 0;
        public void Layout(Rect safe)
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = safe.position + new Vector2(12, safe.height * .64f);
            rect.sizeDelta = new Vector2(safe.width - 24, 60);
        }
    }
}
