using System;
using Tidebound.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Only presents available actions; stock, spending and restart remain owned by existing services.</summary>
    public sealed class DeadlockPanel : MonoBehaviour
    {
        private Font font;
        private Text title, explanation, allowance, reward;
        private Button rescue, shuffle, reverse, shop, restart, close;
        public void Initialize(Font font, Action<ShipTool> use, Action buy, Action retry, Action dismiss)
        {
            this.font = font;
            title = Label("DeadlockTitle", "No ships can move", 24);
            explanation = Label("Explanation", "Use a tool to change the board,\nor restart this challenge.", 16);
            allowance = Label("Allowance", "", 16); reward = Label("Reward", "", 14);
            rescue = Button("Rescue", () => use(ShipTool.Rescue));
            shuffle = Button("Shuffle", () => use(ShipTool.Shuffle));
            reverse = Button("Reverse", () => use(ShipTool.Reverse));
            shop = Button("Shop", buy); restart = Button("Restart", retry); close = Button("Observe", dismiss);
            close.GetComponentInChildren<Text>().text = "Keep observing";
        }
        public void Present(ToolInventory inventory, bool toolsEnabled, int usesLeft, bool canShop, bool hasAccount, int coins)
        {
            allowance.text = toolsEnabled ? "Tool uses left: " + usesLeft + "/5" : "Tools unlock at level 3.";
            if (toolsEnabled && usesLeft == 0) allowance.text = "All 5 tool uses spent this challenge.";
            var kinds = new[] { ShipTool.Rescue, ShipTool.Shuffle, ShipTool.Reverse };
            var buttons = new[] { rescue, shuffle, reverse };
            for (var i = 0; i < kinds.Length; i++)
            {
                var count = inventory.Count(kinds[i]);
                buttons[i].GetComponentInChildren<Text>().text = kinds[i] + "  x" + count + (count == 0 ? "  (get stock)" : "");
                buttons[i].interactable = toolsEnabled && usesLeft > 0 && inventory.IsAvailable && (count > 0 || canShop);
            }
            shop.interactable = canShop;
            shop.GetComponentInChildren<Text>().text = usesLeft == 0 ? "Buy stock for next challenge" : "Coin tool shop";
            reward.text = hasAccount ? "Restart collects " + coins + " earned battle coins.\nSpent tools are not returned." : "Restart without account rewards.";
            restart.GetComponentInChildren<Text>().text = hasAccount ? "Collect " + coins + " & restart" : "Restart challenge";
        }
        public void Layout(Rect safe)
        {
            Place((RectTransform)transform, safe);
            var w = safe.width; var center = safe.height / 2;
            Place(title.rectTransform, new Rect(16, center + 223, w - 32, 36));
            Place(explanation.rectTransform, new Rect(16, center + 167, w - 32, 48));
            Place(allowance.rectTransform, new Rect(12, center + 127, w - 24, 32));
            var buttons = new[] { rescue, shuffle, reverse, shop };
            for (var i = 0; i < buttons.Length; i++) Place((RectTransform)buttons[i].transform, new Rect(24, center + 70 - i * 56, w - 48, 48));
            Place(reward.rectTransform, new Rect(16, center - 155, w - 32, 48));
            Place((RectTransform)restart.transform, new Rect(24, center - 211, w - 48, 48));
            Place((RectTransform)close.transform, new Rect(24, center - 267, w - 48, 48));
        }
        private RectTransform Rect(string name)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(transform, false); r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; return r;
        }
        private Text Label(string name, string value, int size)
        {
            var t = Rect(name).gameObject.AddComponent<Text>(); t.font = font; t.text = value; t.fontSize = size;
            t.color = Color.white; t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false; return t;
        }
        private Button Button(string name, Action action)
        {
            var r = Rect(name); var bg = r.gameObject.AddComponent<Image>(); bg.color = new Color(.13f, .27f, .35f);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = bg; b.onClick.AddListener(() => action());
            var label = Label(name + "Label", "", 15); label.transform.SetParent(r, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero; return b;
        }
        private static void Place(RectTransform r, Rect rect) { r.anchoredPosition = rect.position; r.sizeDelta = rect.size; }
    }
}
