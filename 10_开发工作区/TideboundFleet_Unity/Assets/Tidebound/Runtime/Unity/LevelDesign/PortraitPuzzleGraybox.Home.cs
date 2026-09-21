using System;
using Tidebound.Core;
using Tidebound.Save;
using Tidebound.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private bool homeNavigation, homePauseOwned, homeDispatch;
        private RawImage homeBackdrop;
        private CollectionShipPreview showcase;
        private Canvas homeCanvas;
        private Vector2Int homeScreen;
        private Rect homeSafe;
        private RectTransform homeRoot, homeControls, homeSettings, homeCollectionRoot, homeShopRoot;
        private CollectionPanel homeCollection;
        private CoinShopPanel homeShop;
        private Text homeWallet, homeNotice, homeLevel;
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
            backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;backdrop.GetComponent<Image>().raycastTarget=true;
            homeBackdrop=HarborUI.Art("Harbor",homeRoot,"Harbor_Background_v1");HarborUI.Fill(homeBackdrop.rectTransform);
            homeControls=Rect("HomeControls",homeRoot);
            var settings=HarborUI.RaisedButton("Settings",homeControls,"",()=>{homeSettings.gameObject.SetActive(true);HarborUI.Focus(homeSettings.Find("LanguageAuto").GetComponent<Button>());});
            var gear=HarborUI.Rect("Gear",settings.transform.Find("Face")).gameObject.AddComponent<HarborEmblem>();gear.raycastTarget=false;
            HarborUI.Place(gear.rectTransform,new Rect(12,12,28,28));
            var walletSurface=HarborUI.Rect("WalletSurface",homeControls).gameObject.AddComponent<HarborReliefImage>();walletSurface.Depth=4;walletSurface.Radius=25;walletSurface.raycastTarget=false;
            var coin=HarborUI.Rect("Coin",walletSurface.transform).gameObject.AddComponent<HarborEmblem>();coin.Coin=true;coin.raycastTarget=false;
            HarborUI.Place(coin.rectTransform,new Rect(6,8,36,36));
            homeWallet=HarborUI.Label("Coins",homeControls,"",22);homeWallet.fontStyle=FontStyle.Bold;
            HarborUI.RaisedButton("Collection",homeControls,"Collection",OpenCollection);
            HarborUI.RaisedButton("SkinDraw",homeControls,"Skin Draw",OpenHomeDraw);
            HarborUI.RaisedButton("Supplies",homeControls,"Supplies",OpenShop);
            foreach(var name in new[]{"Daily Gift","Invite","Rankings"})
            {
                HarborUI.RaisedButton(name,homeControls,name,null).interactable=false;
                var badge=HarborUI.Rect(name+"Badge",homeControls).gameObject.AddComponent<HarborReliefImage>();badge.Depth=2;badge.Radius=12;badge.raycastTarget=false;
                var soon=HarborUI.Label("Label",badge.transform,"Soon",12);soon.fontStyle=FontStyle.Bold;HarborUI.Fill(soon.rectTransform,2);
            }
            var names=new[]{"Collection","SkinDraw","Supplies","Daily Gift","Invite","Rankings"};
            var icons=new[]{"Collection","SkinDraw","Supplies","DailyGift","Invite","Rankings"};
            for(var i=0;i<names.Length;i++)
            {
                var button=homeControls.Find(names[i]);var face=button.Find("Face");var icon=HarborUI.Art("Icon",face,"Icon_"+icons[i]+"_v1");
                HarborUI.Place(icon.rectTransform,new Rect(6,22,68,68));
                button.GetComponent<HarborButtonRelief>().SetFloatingIcon(icon.rectTransform);
                var label=button.GetComponentInChildren<Text>();label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=Vector2.zero;
                HarborUI.Place(label.rectTransform,new Rect(1,7,78,24));label.fontSize=14;
            }
            showcase=Rect("ShowcasePreview",homeControls).gameObject.AddComponent<CollectionShipPreview>();showcase.Initialize(true);
            showcase.AllowMotion=()=>!reducedEntryMotion && !homeSettings.gameObject.activeSelf && Application.isFocused;
            // Keep the side controls in front of the transparent preview at narrow aspect ratios.
            showcase.transform.SetSiblingIndex(3);
            homeContinue=HarborUI.RaisedButton("Continue",homeControls,"",ContinueFromHome,true);
            var title=homeContinue.GetComponentInChildren<Text>();title.fontSize=30;
            title.rectTransform.anchorMin=new Vector2(0,.36f);title.rectTransform.anchorMax=new Vector2(1,1);title.rectTransform.offsetMin=new Vector2(10,-3);title.rectTransform.offsetMax=new Vector2(-10,-7);
            homeLevel=HarborUI.Label("Level",homeContinue.transform.Find("Face"),"",16);homeLevel.fontStyle=FontStyle.Bold;
            homeLevel.rectTransform.anchorMin=Vector2.zero;homeLevel.rectTransform.anchorMax=new Vector2(1,.40f);homeLevel.rectTransform.offsetMin=new Vector2(10,12);homeLevel.rectTransform.offsetMax=new Vector2(-10,0);
            foreach(var relief in homeControls.GetComponentsInChildren<HarborButtonRelief>())
                relief.AllowMotion=()=>!reducedEntryMotion && !homeSettings.gameObject.activeSelf && Application.isFocused;
            homeNotice=HarborUI.Label("Notice",homeControls,"",14);
            homeSettings=HarborUI.Surface("HomeSettings",homeRoot,HarborUI.Cream).rectTransform;
            HarborUI.Label("Title",homeSettings,"Settings",26);
            HarborUI.Label("LanguageLabel",homeSettings,"Language",18);
            HarborUI.Button("LanguageAuto",homeSettings,"Automatic",()=>UILanguage.SetPreference(LanguagePreference.Auto));
            HarborUI.Button("LanguageEnglish",homeSettings,"English",()=>UILanguage.SetPreference(LanguagePreference.English));
            HarborUI.Button("LanguageChinese",homeSettings,"Chinese",()=>UILanguage.SetPreference(LanguagePreference.Chinese));
            HarborUI.Button("Hints",homeSettings,"",()=>SetAssistancePreferences(!autoHintsEnabled,reducedHintMotion,true));
            HarborUI.Button("Motion",homeSettings,"",()=>
            {
                var reduced=!reducedEntryMotion;SetReducedEntryMotion(reduced,true);SetReducedResultMotion(reduced,true);
                SetAssistancePreferences(autoHintsEnabled,reduced,true);
            });
            HarborUI.Button("Close",homeSettings,"Back",()=>{homeSettings.gameObject.SetActive(false);HarborUI.Focus(homeControls.Find("Settings").GetComponent<Button>());});
            homeSettings.gameObject.SetActive(false);
            homeCollectionRoot = HarborUI.Surface("HomeCollection",homeRoot,HarborUI.Cream).rectTransform;
            homeCollectionRoot.GetComponent<Image>().raycastTarget = true;
            homeCollection = homeCollectionRoot.gameObject.AddComponent<CollectionPanel>();
            homeCollection.Initialize(saveService, font, CloseCollection);
            homeCollection.ConfigureNavigation(()=>{CloseCollection();OpenHomeShop();}); homeCollectionRoot.gameObject.SetActive(false);
            // Inactive previews must not render into another preview's private layer.
            homeControls.Find("ShowcasePreview").gameObject.SetActive(true);
            homeShopRoot = HarborUI.Surface("HomeSupplies",homeRoot,HarborUI.Cream).rectTransform;
            homeShopRoot.GetComponent<Image>().raycastTarget = true;
            homeShop = homeShopRoot.gameObject.AddComponent<CoinShopPanel>();
            homeShop.Initialize(saveService, toolInventory, CoinShopCatalogReader.LoadDefault(), font, CloseAcquisition);
            homeShop.ConfigureNavigation(()=>{CloseAcquisition();OpenHomeCollection();},()=>{CloseAcquisition();OpenHomeDraw();});
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
            var texture=homeBackdrop.texture;
            if(texture!=null)
            {
                var screenAspect=(float)Screen.width/Mathf.Max(1,Screen.height);var textureAspect=(float)texture.width/texture.height;
                var u=Mathf.Min(1,screenAspect/textureAspect);var v=Mathf.Min(1,textureAspect/screenAspect);
                homeBackdrop.uvRect=new Rect((1-u)/2,(1-v)/2,u,v);
            }
            Place((RectTransform)homeControls.Find("Settings"),new Rect(15,h-79,52,52));
            Place((RectTransform)homeControls.Find("WalletSurface"),new Rect(78,h-79,112,52));
            Place(homeWallet.rectTransform,new Rect(120,h-73,64,40));
            var left=new[]{"Collection","SkinDraw","Supplies"};var right=new[]{"Daily Gift","Invite","Rankings"};
            var continueY=h*.153f;
            var y=Mathf.Max(h*.577f,continueY+316);const float step=102;
            for(var i=0;i<3;i++)
            {
                Place((RectTransform)homeControls.Find(left[i]),new Rect(7,y-i*step,80,87));
                Place((RectTransform)homeControls.Find(right[i]),new Rect(w-87,y-i*step,80,87));
                Place((RectTransform)homeControls.Find(right[i]+"Badge"),new Rect(w-75,y-i*step-12,56,24));
            }
            var size=Mathf.Min(304,w-80);
            Place((RectTransform)homeControls.Find("ShowcasePreview"),new Rect((w-size)/2,h*.325f,size,size));
            Place(homeNotice.rectTransform,new Rect(28,continueY+91,w-56,46));
            Place((RectTransform)homeContinue.transform,new Rect(61,continueY,w-122,94));
            Place(homeSettings,safe);
            Place((RectTransform)homeSettings.Find("Title"),new Rect(24,h-84,w-48,56));
            Place((RectTransform)homeSettings.Find("LanguageLabel"),new Rect(24,h-137,w-48,40));
            var names=new[]{"LanguageAuto","LanguageEnglish","LanguageChinese","Hints","Motion","Close"};
            for(var i=0;i<names.Length;i++)Place((RectTransform)homeSettings.Find(names[i]),new Rect(28,h-202-i*64,w-56,52));
            homeCollection.Layout(safe); homeShop.Layout(safe);
        }
        private void PresentHome()
        {
            if (!IsHomeOpen) return;
            homeWallet.text = saveService?.IsAvailable == true ? saveService.Coins.ToString() : "—";
            var saved = saveService?.Snapshot.Attempt;
            homeContinue.GetComponentInChildren<Text>().text = saveService?.IsAvailable != true ? "Play practice" : saved != null ?
                (saved.Victory ? "View result" : "Continue") : "Start";
            homeLevel.text="Level "+(saved?.LevelNumber ?? saveService?.CurrentLevel ?? 1);
            if(homeSettings.gameObject.activeSelf)
            {
                homeSettings.Find("Hints").GetComponentInChildren<Text>().text="Auto hints: "+(autoHintsEnabled?"On":"Off");
                homeSettings.Find("Motion").GetComponentInChildren<Text>().text="Motion: "+(reducedEntryMotion?"Reduced":"Full");
                var languageButtons=new[]{"LanguageAuto","LanguageEnglish","LanguageChinese"};
                for(var i=0;i<3;i++)homeSettings.Find(languageButtons[i]).GetComponent<Image>().color=(int)UILanguage.Preference==i?HarborUI.Gold:HarborUI.Aqua;
            }
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
        public void OpenHomeDraw()
        {OpenHomeCollection();if(IsHomeCollectionOpen)homeCollection.OpenDraw();}
        private void OpenHomeCollection()
        {
            if (!IsHomeOpen || IsHomeShopOpen || IsHomeCollectionOpen || homeSettings.gameObject.activeSelf) return;
            if (saveService?.IsAvailable != true || saveService.CurrentLevel < 3)
            { homeNotice.text = "Collection unlocks after clearing Level 2."; return; }
            homeControls.gameObject.SetActive(false); homeCollection.Open();
        }
        private void OpenHomeShop()
        {
            if (!IsHomeOpen || IsHomeShopOpen || IsHomeCollectionOpen || homeSettings.gameObject.activeSelf) return;
            homeControls.gameObject.SetActive(false); homeShop.Open();
        }
        private void ClearHome()
        {
            if (homeRoot != null) { homeRoot.gameObject.SetActive(false); Destroy(homeRoot.gameObject); }
            homeRoot = null; homeCanvas = null; homePauseOwned = false;
        }
    }
}
