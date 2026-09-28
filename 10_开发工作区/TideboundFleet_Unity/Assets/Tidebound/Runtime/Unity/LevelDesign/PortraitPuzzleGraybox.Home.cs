using Tidebound.Collection;
using Tidebound.Config;
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
        private HarborSettingsPanel homeSettingsView, pauseSettingsView;
        private HarborAudio harborAudio;
        public HarborSettingsPanel HomeSettingsView => homeSettingsView;
        public HarborSettingsPanel PauseSettingsView => pauseSettingsView;
        public HarborAudio AudioSettings => harborAudio;
        private RawImage homeBackdrop;
        private CollectionShipPreview showcase;
        private Canvas homeCanvas;
        private Vector2Int homeScreen;
        private Rect homeSafe;
        private RectTransform homeRoot, homeTopHud, homeShipView, homeLeftMenu, homeRightMenu, homeActionArea, homeNoticeArea,
            homeSettings, homeCollectionRoot, homeShopRoot;
        private CollectionPanel homeCollection;
        private CoinShopPanel homeShop;
        private bool homeReturnToDraw;
        private Text homeWallet, homeShipName, homeNotice, homeLevel;
        private Button homeContinue, homeSettingsButton, homeCollectionButton, homeDrawButton, homeShopButton;
        public bool IsHomeOpen => homeRoot != null && homeRoot.gameObject.activeSelf;
        public bool ReducedUIMotion => reducedEntryMotion;
        public string HomeNotice => homeNotice?.text;
        private bool IsHomeCollectionOpen => IsHomeOpen && homeCollectionRoot.gameObject.activeSelf;
        private bool IsHomeShopOpen => IsHomeOpen && homeShopRoot.gameObject.activeSelf;

        private void BuildHome()
        {
            homeRoot=HarborUI.Prefab("UI_MainMenu",transform,"UI_MainMenu");
            if(homeRoot==null)throw new InvalidOperationException("Missing UI Design System prefab UI_MainMenu. Run Tidebound/UI/Rebuild Design System Prefabs.");
            // Overlay UI still needs a screen camera: render-texture showcase cameras do not count.
            // Parenting it under Home automatically disables it while the gameplay camera is active.
            var screenCamera=new GameObject("HomeScreenCamera",typeof(Camera)).GetComponent<Camera>();
            screenCamera.transform.SetParent(homeRoot,false);screenCamera.cullingMask=0;screenCamera.depth=-100;
            screenCamera.clearFlags=CameraClearFlags.SolidColor;screenCamera.backgroundColor=HarborUI.Aqua;
            homeCanvas=homeRoot.GetComponent<Canvas>();
            homeBackdrop=homeRoot.Find("Background").GetComponent<RawImage>();homeBackdrop.texture=Resources.Load<Texture2D>("TideboundUI/Harbor_Background_v1");
            // The accepted target keeps the harbor light and clear. Old global tint/blur is not part of this skin.
            var treatment=homeBackdrop.GetComponent<HarborBackgroundTreatment>();if(treatment)treatment.enabled=false;
            var focus=homeRoot.Find("BackgroundFocusOverlay");if(focus)focus.gameObject.SetActive(false);
            homeTopHud=(RectTransform)homeRoot.Find("TopHUD");homeShipView=(RectTransform)homeRoot.Find("PlayerShipView");
            homeLeftMenu=(RectTransform)homeRoot.Find("LeftMenu");homeRightMenu=(RectTransform)homeRoot.Find("RightMenu");
            homeActionArea=(RectTransform)homeRoot.Find("MainActionButton");homeNoticeArea=(RectTransform)homeRoot.Find("Notice");

            homeSettingsButton=HarborUI.Prefab("UI_HomeApprovedEntry",homeTopHud,"Settings").GetComponent<Button>();
            homeSettingsButton.onClick.AddListener(OpenHomeSettings);
            homeSettingsButton.transform.Find("Face/Label").gameObject.SetActive(false);
            var settingsIcon=homeSettingsButton.transform.Find("Face/IconSlot");if(settingsIcon!=null)settingsIcon.gameObject.SetActive(false);
            foreach(var image in homeSettingsButton.GetComponentsInChildren<Graphic>(true))image.enabled=false;
            var gear=GameplayArt.Image("ApprovedSettingsButton",homeSettingsButton.transform,"Settings_Button");
            GameplayArt.Stretch(gear.rectTransform);gear.raycastTarget=true;homeSettingsButton.targetGraphic=gear;

            var wallet=HarborUI.Prefab("UI_HomeApprovedCoins",homeTopHud,"Coins");
            homeWallet=wallet.Find("Value").GetComponent<Text>();

            homeCollectionButton=CreateHomeEntry("Collection",homeLeftMenu,"Collection","Collection",OpenCollection);
            homeDrawButton=CreateHomeEntry("SkinDraw",homeLeftMenu,"Skin Draw","SkinDraw",OpenHomeDraw);
            homeShopButton=CreateHomeEntry("Supplies",homeLeftMenu,"Supplies","Supplies",OpenShop);
            CreateHomeEntry("DailyGift",homeRightMenu,"Daily Gift","DailyGift",null).interactable=false;
            CreateHomeEntry("Events",homeRightMenu,"Invite","Invite",null).interactable=false;
            CreateHomeEntry("Rankings",homeRightMenu,"Rankings","Rankings",null).interactable=false;

            var display=HarborUI.Prefab("UI_Ship_Display",homeShipView,"UI_Ship_Display");
            showcase=display.GetComponent<CollectionShipPreview>();showcase.Initialize(true);
            showcase.UseCelebrationMotion();
            // The name belongs to the displayed cosmetic, never to CurrentLevel or player rank.
            // D1 will supply this label from the owned showcase catalog together with the model.
            homeShipName=HarborUI.Label("ShowcaseName",homeShipView,"Sea Breeze",20);
            ((HarborText)homeShipName).UseDisplayFont=true;homeShipName.font=HarborUI.DisplayFont;
            homeShipName.verticalOverflow=VerticalWrapMode.Overflow;
            var nameOutline=homeShipName.gameObject.AddComponent<Outline>();
            nameOutline.effectColor=new Color(1,.99f,.9f,.95f);nameOutline.effectDistance=new Vector2(1.3f,-1.3f);
            showcase.AllowMotion=()=>!reducedEntryMotion && !homeSettings.gameObject.activeSelf && Application.isFocused;
            homeContinue=HarborUI.Prefab("UI_HomeApprovedMain",homeActionArea,"UI_Button_Main").GetComponent<Button>();
            homeContinue.onClick.AddListener(ContinueFromHome);
            var hero=HarborHeroButton.Apply(homeContinue,HarborHeroButton.Theme.Voyage);
            hero.AllowMotion=()=>!reducedEntryMotion && homeSettings && !homeSettings.gameObject.activeSelf;
            homeLevel=homeContinue.transform.Find("Face/SubLabel").GetComponent<Text>();
            foreach(var relief in homeRoot.GetComponentsInChildren<HarborButtonRelief>())
                relief.AllowMotion=()=>!reducedEntryMotion && !homeSettings.gameObject.activeSelf && Application.isFocused;
            homeNotice=HarborUI.Label("Message",homeNoticeArea,"",14);HarborUI.Fill(homeNotice.rectTransform);
            homeSettings=HarborUI.Popup("HomeSettings",homeRoot);
            homeSettingsView=homeSettings.gameObject.AddComponent<HarborSettingsPanel>();
            homeSettingsView.Initialize(false,harborAudio,()=>
            {homeSettings.gameObject.SetActive(false);SetHomeChrome(true);HarborUI.Focus(homeSettingsButton);});
            homeSettings.gameObject.SetActive(false);
            homeCollectionRoot = HarborUI.Popup("HomeCollection",homeRoot);
            homeCollectionRoot.GetComponent<Image>().raycastTarget = true;
            homeCollection = homeCollectionRoot.gameObject.AddComponent<CollectionPanel>();
            homeCollection.Initialize(saveService, font, CloseCollection,approved:true);
            homeCollection.ConfigureNavigation(()=>{var returnToDraw=homeCollection.IsDrawPage;CloseCollection();OpenHomeShop();homeShop.OpenRecharge();homeReturnToDraw=returnToDraw;}); homeCollectionRoot.gameObject.SetActive(false);
            homeShopRoot = HarborUI.Popup("HomeSupplies",homeRoot);
            homeShopRoot.GetComponent<Image>().raycastTarget = true;
            homeShop = homeShopRoot.gameObject.AddComponent<CoinShopPanel>();
            homeShop.Initialize(saveService, toolInventory, CoinShopCatalogReader.LoadDefault(), font, CloseAcquisition);
            homeShop.ConfigureNavigation(()=>{CloseAcquisition();OpenHomeCollection();},()=>{CloseAcquisition();OpenHomeDraw();});
            homeShopRoot.gameObject.SetActive(false);
            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("NavigationEventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            LayoutHome(); PresentHome();
        }

        private Button CreateHomeEntry(string name,Transform parent,string label,string icon,Action action)
        {
            var button=HarborUI.Prefab("UI_HomeApprovedEntry",parent,name).GetComponent<Button>();
            button.transform.Find("Face/Label").GetComponent<Text>().text=label;
            if(action!=null)button.onClick.AddListener(()=>action());
            var art=HarborUI.SetButtonArt(button,"Icon_"+icon+"_v1");
            // Authored visible bounds (top-left pixels, 1254px source). Preserve source alpha;
            // remove the large transparent margins through UV framing, without stretching art.
            Rect crop;
            switch(icon)
            {
                case "Collection":crop=new Rect(97,278,1118,682);break;
                case "SkinDraw":crop=new Rect(109,123,1068,977);break;
                case "Supplies":crop=new Rect(184,177,923,933);break;
                case "DailyGift":crop=new Rect(163,166,971,950);break;
                case "Invite":crop=new Rect(157,164,952,928);break;
                default:crop=new Rect(146,198,962,896);break;
            }
            art.uvRect=new Rect(crop.x/1254f,1-crop.yMax/1254f,crop.width/1254f,crop.height/1254f);
            var scale=Mathf.Min(68/crop.width,53/crop.height);
            var size=crop.size*scale;
            HarborUI.Place(art.rectTransform,new Rect(new Vector2(40,59)-size/2,size));
            return button;
        }

        public void OpenHomeSettings()
        {
            if(!IsHomeOpen || IsHomeCollectionOpen || IsHomeShopOpen)return;
            harborAudio.Click();SetHomeChrome(false);homeSettings.gameObject.SetActive(true);
            HarborUI.Focus(UILanguage.IsChinese?homeSettingsView.LanguageChinese:homeSettingsView.LanguageEnglish);
        }

        private void SetHomeChrome(bool visible)
        {
            foreach(var item in new[]{homeTopHud,homeShipView,homeLeftMenu,homeRightMenu,homeActionArea,homeNoticeArea})
                if(item!=null)item.gameObject.SetActive(visible);
        }

        private void LayoutHome()
        {
            if (homeCanvas == null) return;
            homeScreen = new Vector2Int(Screen.width, Screen.height); homeSafe = Screen.safeArea;
            homeCanvas.scaleFactor = Mathf.Max(1, Screen.width) / 390f;
            // Device Simulator may leave a stale device safe area when switching to a fixed Game View.
            // Keep valid device insets; reject rectangles outside the actual rendering viewport.
            var pixels=Screen.safeArea;
            if(pixels.width<=0||pixels.height<=0||pixels.xMin<0||pixels.yMin<0||pixels.xMax>Screen.width+1||pixels.yMax>Screen.height+1)
                pixels=new Rect(0,0,Screen.width,Screen.height);
            var safe = new Rect(pixels.position / homeCanvas.scaleFactor, pixels.size / homeCanvas.scaleFactor);
            var w = safe.width; var h = safe.height;
            var texture=homeBackdrop.texture;
            if(texture!=null)
            {
                var screenAspect=(float)Screen.width/Mathf.Max(1,Screen.height);var textureAspect=(float)texture.width/texture.height;
                var u=Mathf.Min(1,screenAspect/textureAspect);var v=Mathf.Min(1,textureAspect/screenAspect);
                homeBackdrop.uvRect=new Rect((1-u)/2,(1-v)/2,u,v);
            }
            foreach(var module in new[]{homeTopHud,homeShipView,homeLeftMenu,homeRightMenu,homeActionArea,homeNoticeArea})Place(module,safe);
            Place((RectTransform)homeSettingsButton.transform,new Rect(14,h-79,50,54));
            Place((RectTransform)homeTopHud.Find("Coins"),new Rect(73,h-71,106,40));
            var continueY=Mathf.Max(82,h*.135f);var menuTop=Mathf.Max(h*.57f,continueY+282);var step=Mathf.Min(104,h*.121f);
            var left=new[]{"Collection","SkinDraw","Supplies"};var right=new[]{"DailyGift","Events","Rankings"};
            for(var i=0;i<3;i++)
            {
                Place((RectTransform)homeLeftMenu.Find(left[i]),new Rect(9,menuTop-i*step,80,88));
                Place((RectTransform)homeRightMenu.Find(right[i]),new Rect(w-89,menuTop-i*step,80,88));
            }
            var size=Mathf.Min(350,w-34);
            Place((RectTransform)homeShipView.Find("UI_Ship_Display"),new Rect((w-size)/2,h*.293f,size,size));
            Place(homeShipName.rectTransform,new Rect(95,h*.293f+7,w-190,32));
            Place(homeNoticeArea,new Rect(26,continueY-40,w-52,36));
            Place((RectTransform)homeContinue.transform,new Rect(34,continueY,w-68,84));
            homeSettingsView.Layout(safe);
            homeCollection.Layout(safe); homeShop.Layout(safe);
        }
        private void PresentHome()
        {
            if (!IsHomeOpen) return;
            var sceneTexture=HarborAppearanceArt.SceneTexture(saveService?.SelectedSceneId);
            if(sceneTexture!=null&&homeBackdrop.texture!=sceneTexture){homeBackdrop.texture=sceneTexture;LayoutHome();}
            homeWallet.text = saveService?.IsAvailable == true ? saveService.Coins.ToString() : "—";
            var selected=saveService?.IsAvailable==true?saveService.SelectedShowcaseId:ShowcaseCatalog.DefaultId;
            showcase.PresentShowcase(selected);homeShipName.text=ShowcaseCatalog.Find(selected).Name;
            var nextLevel=saveService?.CurrentLevel ?? 1;
            var available=saveService?.IsAvailable!=true || catalog.IsAvailable(nextLevel-1);
            homeContinue.interactable=available;
            homeContinue.transform.Find("Face/Label").GetComponent<Text>().text=saveService?.IsAvailable!=true ? "Play practice" : "Start Game";
            foreach(var menu in new[]{homeLeftMenu,homeRightMenu})foreach(var text in menu.GetComponentsInChildren<Text>())text.fontSize=UILanguage.IsChinese?17:14;
            homeLevel.text=available ? "Level "+nextLevel : (UILanguage.IsChinese?"已完成全部关卡":"All levels cleared");
            if(homeSettings.gameObject.activeSelf) homeSettingsView.Present();
            if (IsHomeShopOpen) homeShop.Present();
        }
        public void ContinueFromHome()
        {
            if (!IsHomeOpen || IsHomeCollectionOpen || IsHomeShopOpen || homeSettings.gameObject.activeSelf || homeDispatch) return;
            homeDispatch = true;
            try
            {
                homeNotice.text = "";
                if(saveService?.IsAvailable!=true)
                {
                    if(!catalog.IsAvailable(0)){homeNotice.text="Practice content is unavailable.";return;}
                    ClearSession();BindWorld(SavedGameRuntime.Create(catalog.Load(0),1,transitTiming,combatTiming),0);
                    BeginEntry(false);homeRoot.gameObject.SetActive(false);return;
                }
                var index=saveService.CurrentLevel-1;
                if(!catalog.IsAvailable(index)){homeNotice.text="This level is not available yet. Please check back later.";return;}
                // Load first, then atomically replace the saved attempt before touching the visible world.
                // First-blue collection remains claimable from Collection and no longer hijacks this entrance.
                var next=saveService.CreateNextAttempt(catalog.Load(index),transitTiming,combatTiming,
                    saveService.Snapshot.Attempt!=null && !saveService.Snapshot.Attempt.Victory);
                if(!saveService.StartFromHome(next,(prior,replacement)=>PlayableLevelCatalog.CanReplaceAttempt(catalog,prior,replacement)))
                {next.Dispose();homeNotice.text="Unable to save. Please retry.";return;}
                if(world!=null && !IsCleared)analytics?.TrackRestart();
                progress?.EndForRestart();ClearSession();BindWorld(next,index);
                homePauseOwned=false;BeginEntry(false);
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
            if (saveService?.IsAvailable != true)
            { homeNotice.text = "Save unavailable. Please retry."; return; }
            SetHomeChrome(false); homeCollection.Open();
        }
        private void OpenHomeShop()
        {
            if (!IsHomeOpen || IsHomeShopOpen || IsHomeCollectionOpen || homeSettings.gameObject.activeSelf) return;
            SetHomeChrome(false); homeShop.Open();
        }
        private void ClearHome()
        {
            if (homeRoot != null) { homeRoot.gameObject.SetActive(false); Destroy(homeRoot.gameObject); }
            homeRoot = null; homeCanvas = null; homeSettingsView = null; homePauseOwned = false;
        }
    }
}
