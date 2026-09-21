using System;
using Tidebound.Core;
using Tidebound.Save;
using Tidebound.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private bool homeNavigation, homePauseOwned, homeDispatch;
        private Canvas homeCanvas;
        private Vector2Int homeScreen;
        private Rect homeSafe;
        private RectTransform homeRoot, homeControls, homeSettings, homeCollectionRoot, homeShopRoot;
        private CollectionPanel homeCollection;
        private CoinShopPanel homeShop;
        private Text homeWallet, homeNotice;
        private Button homeContinue;
        public bool IsHomeOpen => homeRoot != null && homeRoot.gameObject.activeSelf;
        public string HomeNotice => homeNotice?.text;
        private bool IsHomeCollectionOpen => IsHomeOpen && homeCollectionRoot.gameObject.activeSelf;
        private bool IsHomeShopOpen => IsHomeOpen && homeShopRoot.gameObject.activeSelf;

        private void BuildHome()
        {
            homeRoot = Rect("HomeNavigation", transform);
            homeCanvas = homeRoot.gameObject.AddComponent<Canvas>();
            homeCanvas.renderMode = RenderMode.ScreenSpaceOverlay; homeCanvas.sortingOrder = 30;
            homeRoot.gameObject.AddComponent<GraphicRaycaster>();
            var backdrop = Panel("Sea", homeRoot, new Rect(), new Color(.08f, .39f, .48f));
            backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().raycastTarget = true;
            homeControls = Rect("HomeControls", homeRoot);
            Button("Settings", homeControls, "Settings", () => homeSettings.gameObject.SetActive(true));
            homeWallet = Label("Coins", homeControls, "", 20);
            Button("Collection", homeControls, "Collection", OpenCollection);
            Button("SkinDraw", homeControls, "Skin Draw", OpenCollection);
            Button("Supplies", homeControls, "Supplies", OpenShop);
            foreach (var label in new[] { "Daily Gift", "Invite", "Rankings" })
                Button(label, homeControls, label + "\nSoon", null).interactable = false;
            // A reused preview keeps navigation playable; final showcase art and categories arrive in C2/D1.
            Rect("ShowcasePreview", homeControls).gameObject.AddComponent<CollectionShipPreview>().Initialize();
            homeContinue = Button("Continue", homeControls, "", ContinueFromHome);
            homeNotice = Label("Notice", homeControls, "", 15);
            homeSettings = Panel("HomeSettings", homeRoot, new Rect(), new Color(.025f, .055f, .08f, 1));
            homeSettings.GetComponent<Image>().raycastTarget = true;
            Label("Title", homeSettings, "Settings", 24);
            Button("Hints", homeSettings, "Toggle auto hints", () => SetAssistancePreferences(!autoHintsEnabled, reducedHintMotion, true));
            Button("Motion", homeSettings, "Toggle reduced motion", () =>
            {
                var reduced = !reducedEntryMotion;
                SetReducedEntryMotion(reduced, true); SetReducedResultMotion(reduced, true);
                SetAssistancePreferences(autoHintsEnabled, reduced, true);
            });
            Button("Close", homeSettings, "Back", () => homeSettings.gameObject.SetActive(false));
            homeSettings.gameObject.SetActive(false);
            homeCollectionRoot = Panel("HomeCollection", homeRoot, new Rect(), new Color(.025f, .055f, .08f, 1));
            homeCollectionRoot.GetComponent<Image>().raycastTarget = true;
            homeCollection = homeCollectionRoot.gameObject.AddComponent<CollectionPanel>();
            homeCollection.Initialize(saveService, font, CloseCollection); homeCollectionRoot.gameObject.SetActive(false);
            // Inactive previews must not render into another preview's private layer.
            homeControls.Find("ShowcasePreview").gameObject.SetActive(true);
            homeShopRoot = Panel("HomeSupplies", homeRoot, new Rect(), new Color(.025f, .055f, .08f, 1));
            homeShopRoot.GetComponent<Image>().raycastTarget = true;
            homeShop = homeShopRoot.gameObject.AddComponent<CoinShopPanel>();
            homeShop.Initialize(saveService, toolInventory, CoinShopCatalogReader.LoadDefault(), font, CloseAcquisition);
            homeShopRoot.Find("CloseShop").GetComponentInChildren<Text>().text = "Back to home";
            homeShopRoot.gameObject.SetActive(false);
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("NavigationEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            LayoutHome(); PresentHome();
        }

        private void LayoutHome()
        {
            if (homeCanvas == null) return;
            homeScreen = new Vector2Int(Screen.width, Screen.height); homeSafe = Screen.safeArea;
            homeCanvas.scaleFactor = Mathf.Max(1, Screen.width) / 390f;
            var safe = new Rect(Screen.safeArea.position / homeCanvas.scaleFactor, Screen.safeArea.size / homeCanvas.scaleFactor);
            Place(homeControls, safe); var w = safe.width; var h = safe.height;
            Place((RectTransform)homeControls.Find("Settings"), new Rect(12, h - 64, 82, 48));
            Place(homeWallet.rectTransform, new Rect(106, h - 64, w - 118, 48));
            var left = new[] { "Collection", "SkinDraw", "Supplies" };
            var right = new[] { "Daily Gift", "Invite", "Rankings" };
            for (var i = 0; i < 3; i++)
            {
                Place((RectTransform)homeControls.Find(left[i]), new Rect(12, h * .62f - i * 84, 86, 68));
                Place((RectTransform)homeControls.Find(right[i]), new Rect(w - 98, h * .62f - i * 84, 86, 68));
            }
            var size = Mathf.Min(180, w - 204);
            Place((RectTransform)homeControls.Find("ShowcasePreview"), new Rect((w - size) / 2, h * .44f, size, size));
            Place(homeNotice.rectTransform, new Rect(20, 142, w - 40, 72));
            Place((RectTransform)homeContinue.transform, new Rect(32, 52, w - 64, 76));
            Place(homeSettings, safe);
            Place((RectTransform)homeSettings.Find("Title"), new Rect(20, h / 2 + 110, w - 40, 44));
            var names = new[] { "Hints", "Motion", "Close" };
            for (var i = 0; i < names.Length; i++)
                Place((RectTransform)homeSettings.Find(names[i]), new Rect(24, h / 2 + 40 - 64 * i, w - 48, 48));
            homeCollection.Layout(safe); homeShop.Layout(safe);
        }
        private void PresentHome()
        {
            if (!IsHomeOpen) return;
            homeWallet.text = saveService?.IsAvailable == true ? "Coins: " + saveService.Coins : "Coins unavailable";
            var saved = saveService?.Snapshot.Attempt;
            homeContinue.GetComponentInChildren<Text>().text = saveService?.IsAvailable != true ? "Play practice" : saved != null ?
                (saved.Victory ? "View result" : "Continue") + "\nLevel " + saved.LevelNumber :
                "Start\nLevel " + (saveService?.CurrentLevel ?? 1);
            if (IsHomeShopOpen) homeShop.Present();
        }
        public void ContinueFromHome()
        {
            if (!IsHomeOpen || IsHomeCollectionOpen || IsHomeShopOpen || homeSettings.gameObject.activeSelf || homeDispatch) return;
            homeDispatch = true;
            try
            {
                homeNotice.text = "";
                if (saveService?.IsAvailable != true && world == null)
                {
                    if (!catalog.IsAvailable(0)) { homeNotice.text = "Practice content is unavailable."; return; }
                    BindWorld(SavedGameRuntime.Create(catalog.Load(0), 1, transitTiming, combatTiming), 0);
                    BeginEntry(false); homeRoot.gameObject.SetActive(false); return;
                }
                if (world != null)
                {
                    if (saveService?.CurrentAttemptSettled != true && !SaveCheckpoint(true)) { homeNotice.text = "Unable to save. Please retry."; return; }
                    if (homePauseOwned && IsPaused) movement.Resume();
                    homePauseOwned = false; world.PresentationPause = false;
                    homeRoot.gameObject.SetActive(false); presentation.SetActive(true);
                    NotifyUserActivity(); RefreshViewport(); return;
                }
                var saved = saveService.Snapshot.Attempt;
                var index = saved != null ? saved.LevelNumber - 1 : saveService.CurrentLevel - 1;
                if (!catalog.IsAvailable(index)) { homeNotice.text = "This level is not available yet. Please check back later."; return; }
                var level = catalog.Load(index); // Load and validate before creating or replacing an attempt.
                if (saved != null)
                {
                    if (saved.LevelId != catalog.GetLevelId(index)) throw new InvalidOperationException("Saved level identity changed.");
                    using (var reference = SavedGameRuntime.Create(level, saved.LevelNumber))
                        if (reference.Capture().LayoutFingerprint != saved.LayoutFingerprint)
                            throw new InvalidOperationException("Saved level layout changed.");
                    var restored = SavedGameRuntime.Restore(saved);
                    saveService.AttachRestored(restored); BindWorld(restored, index);
                    BeginEntry(true); RestoreResultIfComplete();
                }
                else
                {
                    if (saveService.CanClaimFirstBlue) { OpenHomeCollection(); return; }
                    CommitEntry(saveService.CreateNextAttempt(level, transitTiming, combatTiming), index, false);
                }
                homeRoot.gameObject.SetActive(false);
            }
            catch (Exception)
            { homeNotice.text = "Unable to load this level. Your saved progress has been kept."; }
            finally { homeDispatch = false; }
        }
        public void ReturnHome()
        {
            if (!homeNavigation || IsHomeOpen || world == null || IsBusy || world.PendingMoveId != null ||
                !IsEntryReady || IsEntrySaveBlocked || IsCollectionOpen || IsAcquisitionOpen || (ResultOwnsInput && !IsResultReadable && !PracticeResult)) return;
            var owned = session.State == GameState.Playing || IsMenuOpen && menuPauseOwned;
            var prior = world.PresentationPause; world.PresentationPause = owned;
            if (saveService?.CurrentAttemptSettled != true && !SaveCheckpoint(true)) { world.PresentationPause = prior; return; }
            homePauseOwned = owned;
            if (session.State == GameState.Playing) movement.Pause();
            demo = null; tools.CancelSelection(); input.CancelSelection(); NotifyUserActivity();
            menuPanel.gameObject.SetActive(false); menuPauseOwned = false;
            presentation.SetActive(false); homeRoot.gameObject.SetActive(true);
            homeNotice.text = ""; LayoutHome(); PresentHome();
        }
        private void OpenHomeCollection()
        {
            if (!IsHomeOpen || IsHomeShopOpen || IsHomeCollectionOpen) return;
            if (saveService?.IsAvailable != true || saveService.CurrentLevel < 3)
            { homeNotice.text = "Collection unlocks after clearing Level 2."; return; }
            homeControls.gameObject.SetActive(false); homeCollection.Open();
        }
        private void OpenHomeShop()
        {
            if (!IsHomeOpen || IsHomeShopOpen || IsHomeCollectionOpen) return;
            homeControls.gameObject.SetActive(false); homeShop.Open();
        }
        private void ClearHome()
        {
            if (homeRoot != null) { homeRoot.gameObject.SetActive(false); Destroy(homeRoot.gameObject); }
            homeRoot = null; homeCanvas = null; homePauseOwned = false;
        }
    }
}
