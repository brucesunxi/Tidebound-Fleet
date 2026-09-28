using System;
using System.Linq;
using Tidebound.Collection;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    /// <summary>Production binding of the accepted gallery layout. Preview progress never feeds this service.</summary>
    public sealed partial class VisualSampleGallery
    {
        private PlayerSaveService productionService;
        public HarborDrawPanel CommerceDraw {get;private set;}
        public void ConfigureCommerce(Action recharge)=>CommerceDraw.SetRecharge(recharge);
        private Button useTrail;
        private RawImage productionTrailArt;
        public bool IsProduction=>productionService!=null;
        public static string SkinIdentity(int i)=>"TF_SKIN_K"+(i+1).ToString("D2");
        public static string SceneIdentity(int i)=>"TF_SCENE_S"+(i+1).ToString("D2");
        public static string TrailIdentity(int i)=>"TF_TRAIL_W"+(i+1).ToString("D2");
        public void BindProduction(PlayerSaveService service,Action close)
        {
            productionService=service??throw new ArgumentNullException(nameof(service));English=!UILanguage.IsChinese;
            HarborUI.Fill((RectTransform)transform);HarborUI.Fill((RectTransform)PreviewCanvas.transform);
            PreviewCanvas.GetComponent<CanvasScaler>().enabled=false;
            PreviewCanvas.overrideSorting=true;PreviewCanvas.sortingOrder=40;Home.gameObject.SetActive(false);Background.gameObject.SetActive(false);
            Toolbar.gameObject.SetActive(false);Stamp.gameObject.SetActive(false);InspectArtwork=false;
            CloseButton.onClick.RemoveAllListeners();CloseButton.onClick.AddListener(()=>close());
            useTrail=TrailTool("UseOwnedTrail",TrailViewer,"");
            useTrail.onClick.AddListener(()=>{SelectProductionAppearance(TrailIdentity(SelectedTrail));});
            TrailVoyage.enabled=false;TrailVoyage.Paused=true;
            foreach(var ribbon in TrailVoyage.Ribbons)ribbon.gameObject.SetActive(false);
            foreach(var boat in TrailVoyage.Boats)boat.gameObject.SetActive(false);
            TrailPause.gameObject.SetActive(false);TrailRestart.gameObject.SetActive(false);TrailPhase.gameObject.SetActive(false);
            TrailStageLabel.gameObject.SetActive(false);TrailMiniLabel.gameObject.SetActive(false);
            productionTrailArt=HarborUI.Rect("StaticWholeTrail",TrailStage).gameObject.AddComponent<RawImage>();productionTrailArt.raycastTarget=false;
            BuildProductionTransactions();
            var commerce=HarborUI.Rect("ProductionSingleDraw",PreviewCanvas.transform);HarborUI.Fill(commerce);
            CommerceDraw=commerce.gameObject.AddComponent<HarborDrawPanel>();
            CommerceDraw.Initialize(service,tab=>{CommerceDraw.gameObject.SetActive(false);Popup.gameObject.SetActive(true);if(SelectedTab!=tab)SelectTab(tab);else RefreshText();},()=>{CommerceDraw.gameObject.SetActive(false);close();});
            commerce.gameObject.SetActive(false);
            DrawOnceButton.onClick.RemoveAllListeners();DrawOnceButton.onClick.AddListener(()=>{if(productionService.CanClaimFirstBlue)ClaimProductionGift();else if(DrawCategory==0)OpenProductionTransaction("Single");});
            RefreshProductionSelection();ShowPopup(true);SelectTab(0);
        }
        public void OpenProduction(int tab=0)
        {
            English=!UILanguage.IsChinese;RefreshProductionSelection();
            if(CommerceDraw)CommerceDraw.gameObject.SetActive(false);
            DrawPopup.gameObject.SetActive(false);SkinViewer.gameObject.SetActive(false);SceneViewer.gameObject.SetActive(false);TrailViewer.gameObject.SetActive(false);
            liveTransaction.gameObject.SetActive(false);
            Popup.gameObject.SetActive(true);SelectTab(tab);
        }
        private void RefreshProductionSelection()
        {
            if(!IsProduction)return;
            SelectedCard=Math.Max(0,ShowcaseCatalog.All.ToList().FindIndex(s=>s.Id==productionService.SelectedShowcaseId));
            SelectedScene=Math.Max(0,Array.FindIndex(SceneRecords,s=>SceneIdentity(Array.IndexOf(SceneRecords,s))==productionService.SelectedSceneId));
        }
        private void SelectProductionShowcase(int index)
        {
            var result=productionService.SelectShowcase(ShowcaseCatalog.All[index].Id);
            RefreshProductionSelection();RefreshText();
            if(result!=ShowcaseSelectionStatus.Saved&&result!=ShowcaseSelectionStatus.AlreadySelected)
            {var item=ShowcaseCatalog.All[index];Hint.text=item.SourceGroup==ShowcaseSourceGroup.Level?(English?"Clear level ":"通关第 ")+item.TargetClearLevel+(English?" to unlock":" 关获取"):(English?"This reward channel is not open yet":"该获取渠道尚未开放");}
        }
        private void SelectProductionAppearance(string id)
        {
            var result=productionService.SelectAppearance(id);RefreshProductionSelection();RefreshText();
            Hint.text=result==AppearanceSelectionStatus.Saved||result==AppearanceSelectionStatus.AlreadySelected?
                (English?"Saved · skins apply to your next attempt":"已保存 · 船皮在下一局生效"):
                result==AppearanceSelectionStatus.SlotsFull?(English?"5 slots full. Unequip one first.":"已装备5款，请先卸下一款"):
                (English?"Selection unchanged: ":"选择未更改：")+result;
        }
        private void RefreshProductionText()
        {
            if(!IsProduction)return;
            Home.gameObject.SetActive(false);Toolbar.gameObject.SetActive(false);Stamp.gameObject.SetActive(false);
            SkinEquipment.text=(English?"Equipped ":"已装备 ")+SkinEquippedCount+"/5";
            if(SelectedTab==0)Hint.text=English?"Tap owned skins to equip · next attempt":"点击已拥有船皮装备 · 下一局生效";
            if(SelectedTab==2)Hint.text=English?"Tap owned ships to change your showcase":"点击已拥有船只切换主页形象";
            if(SelectedTab==3)Hint.text=English?"Tap owned scenes to change your home":"点击已拥有场景切换主页背景";
            if(SelectedTab==1)
            {
                Statistics.text=(English?"Collected ":"已收集 ")+Enumerable.Range(0,TrailCards.Length).Count(IsTrailPreviewOwned)+"/"+TrailCards.Length;
                Hint.text=English?"Tap to preview · equip owned styles inside":"点击预览完整拖尾 · 已拥有款可设为使用";
                TrailHeadingLabels[1].text=English?"More Styles":"更多款式";
            }
            TrailViewerInfo.text=English?"Whole trail artwork · locked styles remain locked":"完整拖尾静态展示 · 查看不会解锁";
            RefreshStaticProductionTrail();
            if(useTrail)
            {
                var h=Screen.height*390f/Mathf.Max(1,Screen.width);Place((RectTransform)useTrail.transform,107,h-207,176,39);
                useTrail.interactable=productionService.OwnsAppearance(TrailIdentity(SelectedTrail));
                useTrail.GetComponentInChildren<Text>().text=productionService.SelectedTrailId==TrailIdentity(SelectedTrail)?(English?"In use":"使用中"):(English?"Use this trail":"使用该拖尾");
            }
            if(DrawPopupOpen)RefreshProductionDraw();
        }
        private void RefreshStaticProductionTrail()
        {
            if(!productionTrailArt)return;
            var record=TrailRecords[SelectedTrail];productionTrailArt.texture=Resources.Load<Texture2D>(record.resourcePath);productionTrailArt.uvRect=HarborAppearanceArt.UV(record.uv);
            HarborCommerceUI.Fit(productionTrailArt,new Rect(22,68,Mathf.Max(1,TrailStage.rect.width-44),Mathf.Max(1,TrailStage.rect.height-90)));
            TrailViewerTitle.text=English?record.name:record.chineseName;
        }
        private void RefreshProductionDraw()
        {
            if(!IsProduction||CommerceDraw)return;
            DrawPreviewStamp.gameObject.SetActive(false);
            var gift=DrawCategory==0&&productionService.CanClaimFirstBlue;
            DrawOnceButton.interactable=DrawCategory==0&&productionService.IsAvailable&&productionService.CurrentLevel>=3;
            var price=productionService.AppearanceDrawState.Price;
            DrawOnceButton.GetComponentInChildren<Text>().text=gift?(English?"Claim free blue":"领取免费蓝皮"):DrawCategory==0?(English?"Draw · ":"单抽 · ")+price:(English?"Not open yet":"暂未开放");
            DrawInfo.text=gift?(English?"Level 2 gift · free blue skin\nReturn to Skins to equip after claiming":"通关第2关赠礼 · 免费蓝色皮肤\n领取后可返回皮肤栏装备"):
                DrawCategory==0?(English?"Coins: ":"金币：")+productionService.Coins+(English?"\nCollection tickets: ":"\n收藏券：")+productionService.AppearanceDrawState.Tickets:
                (English?"This pool is not open yet.\nCoins: ":"该奖池暂未开放\n金币：")+productionService.Coins;
            if(DrawCategory==0)DrawCount.text=English?"Skin pool · 14 styles":"皮肤奖池 · 14款";
            RefreshProductionTransactions();
        }
        private void ClaimProductionGift()
        {
            if(!IsProduction||DrawCategory!=0||!productionService.CanClaimFirstBlue)return;
            var result=productionService.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue");RefreshText();
            DrawInfo.text=result==CollectionStatus.Saved?(English?"Gift saved. Return to Skins to equip.":"赠礼已保存，返回皮肤栏自行装备"):(English?"Gift not claimed: ":"未领取：")+result;
        }
    }
}
