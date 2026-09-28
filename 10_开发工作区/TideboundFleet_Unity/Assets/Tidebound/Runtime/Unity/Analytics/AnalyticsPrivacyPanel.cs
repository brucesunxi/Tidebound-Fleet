using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.Analytics
{
    public sealed class AnalyticsPrivacyPanel : MonoBehaviour
    {
        private TideboundAnalytics service;
        private RectTransform frame;
        private Text title, body;
        private Button first, second, third, back;
        private bool adultSelected;
        private Vector2 previousSize;
        private static string L(string zh, string en) => UILanguage.IsChinese ? zh : en;
        public void Initialize(TideboundAnalytics analytics)
        {
            service = analytics;
            var shade = gameObject.AddComponent<Image>(); shade.color = new Color(.01f,.08f,.13f,.92f);
            frame = HarborUI.Rect("Content", transform);
            frame.gameObject.AddComponent<Image>().color = HarborUI.Cream;
            title = HarborUI.Label("Title", frame, "", 23);
            body = HarborUI.Label("Explanation", frame, "", 16); body.alignment = TextAnchor.UpperLeft;
            first = HarborUI.Button("Primary", frame, "", Primary, true);
            second = HarborUI.Button("Decline", frame, "", Decline);
            third = HarborUI.Button("UnknownAge", frame, "", Decline);
            back = HarborUI.Button("Back", frame, L("返回", "Back"), () => gameObject.SetActive(false));
            Present();
        }
        public void Open() { adultSelected = false; gameObject.SetActive(true); transform.SetAsLastSibling(); Present(); }
        private void Primary()
        {
            if (service.IsEnabled) { service.SetConsent(false, false); adultSelected = false; Present(); return; }
            if (!adultSelected) { adultSelected = true; Present(); return; }
            service.SetConsent(true, true); Present();
        }
        private void Decline() { service.SetConsent(false, false); adultSelected = false; Present(); }
        private void Present()
        {
            if (service == null || first == null) return;
            var enabled = service.IsEnabled;
            title.text = L("玩法统计选择", "Gameplay analytics");
            body.text = L("可自愿分享关卡进度、游戏时长和道具使用，帮助我们改进游戏。使用随机安装标识，行为明细最多保存90天，不包含姓名、邮箱或支付凭证。\n\n拒绝不影响游戏。可在此关闭统计并删除已上传数据。",
                "Optionally share level progress, play time and tool use to improve the game. We use a random installation ID and keep detailed events for up to 90 days. No name, email or payment credentials are included.\n\nDeclining does not affect gameplay. You can turn this off and delete uploaded analytics here.");
            body.text += enabled ? L("\n\n当前：已启用", "\n\nStatus: enabled") : service.IsDeletionPending ? L("\n\n已关闭，等待联网删除数据。", "\n\nDisabled. Deletion will finish when connected.") :
                adultSelected ? L("\n\n是否愿意分享上述统计？", "\n\nWould you like to share this analytics data?") : L("\n\n请选择年龄范围；未满18岁或年龄未知不启用统计。", "\n\nSelect your age range. Analytics is disabled for under-18s and unknown ages.");
            if (!service.IsAvailable) body.text += L("\n统计服务暂不可用。", "\nAnalytics is currently unavailable.");
            first.GetComponentInChildren<Text>().text = enabled ? L("关闭统计并删除数据", "Disable and delete data") : adultSelected ? L("同意分享统计", "Enable analytics") : L("已满18岁", "18 or older");
            second.GetComponentInChildren<Text>().text = adultSelected ? L("不分享", "Do not share") : L("未满18岁", "Under 18");
            third.GetComponentInChildren<Text>().text = L("不愿说明", "Prefer not to say");
            first.interactable = service.IsAvailable && !service.IsDeletionPending;
            second.gameObject.SetActive(!enabled && !service.IsDeletionPending);
            third.gameObject.SetActive(!enabled && !adultSelected && !service.IsDeletionPending);
            Layout();
        }
        private void Layout()
        {
            var size = ((RectTransform)transform).rect.size; previousSize = size;
            var width = Mathf.Min(size.x - 36, 420); var height = Mathf.Min(size.y - 32, 620);
            HarborUI.Place(frame, new Rect((size.x-width)/2,(size.y-height)/2,width,height));
            HarborUI.Place(title.rectTransform,new Rect(0,height-45,width,40));
            HarborUI.Place(body.rectTransform,new Rect(8,222,width-16,height-280));
            body.resizeTextForBestFit=true;body.resizeTextMinSize=12;body.resizeTextMaxSize=16;
            HarborUI.Place((RectTransform)first.transform,new Rect(0,163,width,48));
            HarborUI.Place((RectTransform)second.transform,new Rect(0,109,width,48));
            HarborUI.Place((RectTransform)third.transform,new Rect(0,55,width,48));
            HarborUI.Place((RectTransform)back.transform,new Rect(0,0,width,44));
        }
        private void Update()
        {
            if (previousSize != ((RectTransform)transform).rect.size) Layout();
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) gameObject.SetActive(false);
        }
    }
}
