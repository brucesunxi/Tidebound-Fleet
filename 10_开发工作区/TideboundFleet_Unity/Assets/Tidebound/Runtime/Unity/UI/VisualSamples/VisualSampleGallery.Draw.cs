using System.Linq;
using Tidebound.Collection;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    public sealed partial class VisualSampleGallery
    {
        public RectTransform DrawEntryCoin,DrawPopup,DrawPanel,DrawHeader,DrawTabs,DrawVoucher;
        public Button DrawCloseButton,DrawBackButton,DrawOnceButton;
        public Button[] DrawCategoryButtons;
        public Text DrawTitle,DrawPoolName,DrawCount,DrawInfo,DrawPreviewStamp;
        public int DrawCategory {get;private set;}
        public bool DrawPopupOpen=>CommerceDraw?CommerceDraw.gameObject.activeSelf:DrawPopup&&DrawPopup.gameObject.activeSelf;
        private string[] DrawCategoryNames=>English?new[]{"Skins","Trails","Showcase","Scenes"}:new[]{"皮肤","拖尾","形象","场景"};
        private void BuildDrawPopup()
        {
            DrawPopup=Rect("UnifiedDrawPopup_PreviewOnly",PreviewCanvas.transform);Fill(DrawPopup);
            var shade=DrawPopup.gameObject.AddComponent<Image>();shade.color=new Color(.015f,.055f,.09f,.70f);shade.raycastTarget=true;
            DrawPanel=Art("CreamGoldFrame","Panel_Gold",DrawPopup).rectTransform;
            DrawHeader=Raw("WoodHeader",DrawPopup,"Skins/UI_HarborHeader_v1",new Rect(79f/2172,27f/724,2034f/2172,682f/724)).rectTransform;
            DrawTitle=Label("Title",DrawHeader,"抽奖",29,Color.white);Place(DrawTitle.rectTransform,48,34,162,46);
            var shadow=DrawTitle.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(.25f,.09f,.025f,.9f);shadow.effectDistance=new Vector2(1.4f,-1.4f);
            var close=Rect("Close",DrawPopup);var closeFace=Rect("Visual",close);Fill(closeFace);var closeArt=Art("Coral","Close_Coral",closeFace);Fill(closeArt.rectTransform);DrawCloseButton=Button(close,closeFace,closeArt);Fill(Vector("Cross",closeFace,SampleVectorGraphic.Shape.Cross,Color.white).rectTransform,8);
            DrawCloseButton.onClick.AddListener(CloseDrawPopup);
            DrawTabs=Rect("DrawPoolCategories",DrawPopup);DrawCategoryButtons=new Button[4];
            for(var i=0;i<4;i++)
            {
                var index=i;var r=Rect("Category_"+i,DrawTabs);var face=Rect("Visual",r);Fill(face);var skin=Art("GoldBrownTab","Tab_Brown",face);Fill(skin.rectTransform);DrawCategoryButtons[i]=Button(r,face,skin);Fill(Label("Label",face,"",16,Brown).rectTransform,2);DrawCategoryButtons[i].onClick.AddListener(()=>SelectDrawCategory(index));
            }
            DrawPoolName=Label("PoolName",DrawPopup,"",24,Brown);
            DrawVoucher=Raw("ExistingDrawVoucher",DrawPopup,"Icon_SkinDraw_v1",new Rect(.08f,.08f,.86f,.82f)).rectTransform;
            DrawCount=Label("PoolCount",DrawPopup,"",19,Ink);DrawInfo=Label("PoolDescription",DrawPopup,"",15,Brown);
            var back=Rect("BackToCollection",DrawPopup);var backFace=Rect("Visual",back);Fill(backFace);var backArt=Art("Cream","Capsule_Cream",backFace);Fill(backArt.rectTransform);DrawBackButton=Button(back,backFace,backArt);Fill(Label("Label",backFace,"返回收藏",17,Brown).rectTransform);DrawBackButton.onClick.AddListener(CloseDrawPopup);
            var once=Rect("DrawOnce_NotConnected",DrawPopup);var onceFace=Rect("Visual",once);Fill(onceFace);var onceArt=Art("Gold","CTA_Gold",onceFace);Fill(onceArt.rectTransform);onceArt.Border=new Vector4(25,9,25,16);DrawOnceButton=Button(once,onceFace,onceArt);DrawOnceButton.interactable=false;Fill(Label("Label",onceFace,"抽取一次",20,Brown).rectTransform,3);
            DrawPreviewStamp=Label("PreviewScope",DrawPopup,"抽奖小样 · 不扣费、不发奖",11,Color.white);
            DrawPopup.gameObject.SetActive(false);
        }
        public void OpenDrawPopup()
        {
            if(!PopupOpen)return;
            if(CommerceDraw){Popup.gameObject.SetActive(false);CommerceDraw.Open(SelectedTab);return;}
            SceneViewer.gameObject.SetActive(false);SkinViewer.gameObject.SetActive(false);TrailViewer.gameObject.SetActive(false);Popup.gameObject.SetActive(false);DrawPopup.gameObject.SetActive(true);SelectDrawCategory(SelectedTab);
        }
        private void WireDrawPopup()
        {
            DrawCloseButton.onClick.RemoveAllListeners();DrawCloseButton.onClick.AddListener(CloseDrawPopup);
            DrawBackButton.onClick.RemoveAllListeners();DrawBackButton.onClick.AddListener(CloseDrawPopup);
            for(var i=0;i<4;i++){var index=i;DrawCategoryButtons[i].onClick.RemoveAllListeners();DrawCategoryButtons[i].onClick.AddListener(()=>SelectDrawCategory(index));}
        }
        public void CloseDrawPopup(){if(CommerceDraw)CommerceDraw.gameObject.SetActive(false);DrawPopup.gameObject.SetActive(false);Popup.gameObject.SetActive(true);}
        public void SelectDrawCategory(int index){if(CommerceDraw){CommerceDraw.SelectPool((DrawPool)index);return;}DrawCategory=Mathf.Clamp(index,0,3);RefreshDrawPopup();}
        private void RefreshDrawEntry()
        {
            if(!SceneDraw)return;SceneDraw.gameObject.SetActive(true);
            SceneDrawTitle.text=English?"Go to Draw":"前往抽奖";SceneDrawTitle.fontSize=25;
            SceneDrawSubtitle.text=English?DrawCategoryNames[SelectedTab]+" pool":DrawCategoryNames[SelectedTab]+"奖池";SceneDrawSubtitle.fontSize=14;
            if(DrawPopupOpen)RefreshDrawPopup();
        }
        private void RefreshDrawPopup()
        {
            var names=DrawCategoryNames;DrawTitle.text=English?"Draw":"抽奖";
            for(var i=0;i<4;i++)
            {
                var b=DrawCategoryButtons[i];SetArt(b.GetComponentInChildren<SampleSlicedImage>(),i==DrawCategory?"Tab_Gold":"Tab_Brown");
                var text=b.GetComponentInChildren<Text>();text.text=names[i];text.fontSize=English?14:16;text.color=i==DrawCategory?Brown:new Color(1,.94f,.80f);
            }
            DrawPoolName.text=English?names[DrawCategory]+" Draw":names[DrawCategory]+"抽奖";
            switch(DrawCategory)
            {
                case 0:
                    var skins=SkinRecords.Count(s=>s.group=="Draw");DrawCount.text=English?skins+" ship skins":skins+"款局内皮肤";
                    DrawInfo.text=English?"Level 2 gift: one unowned blue skin.\nPreview pool; drawing is disabled.":"通关第 2 关：免费首抽赠未拥有蓝色\n小样分配奖池 · 暂未开放抽取";break;
                case 1:
                    DrawCount.text=English?"Coming soon":"奖池暂未开放";DrawInfo.text=English?"Trail rewards are not configured yet.":"拖尾奖励尚未配置";break;
                case 2:
                    var ships=ShowcaseCatalog.Groups.First(g=>g.Source==ShowcaseSourceGroup.Draw).CatalogIndices.Count;DrawCount.text=English?ships+" home showcase ships":ships+"款主页形象";
                    DrawInfo.text=English?"Level and share rewards are separate.\nDrawing is not enabled in this preview.":"关卡、分享奖励不进入此奖池\n本小样暂未开放抽取";break;
                default:
                    var scenes=SceneRecords.Count(s=>s.group=="Draw");DrawCount.text=English?scenes+" home scenes":scenes+"款主页场景";
                    DrawInfo.text=English?"Level and share rewards are separate.\nDrawing is not enabled in this preview.":"关卡、分享奖励不进入此奖池\n本小样暂未开放抽取";break;
            }
            DrawBackButton.GetComponentInChildren<Text>().text=English?"Back":"返回收藏";DrawOnceButton.GetComponentInChildren<Text>().text=English?"Draw once":"抽取一次";
            DrawPreviewStamp.text=English?"Draw preview · no spend, no rewards":"抽奖小样 · 不扣费、不发奖";
            RefreshProductionDraw();
        }
        private void LayoutDrawPopup(float h)
        {
            if(!DrawPopup)return;var top=(h-478)/2;
            Place(DrawPanel,18,top,354,478);Place(DrawHeader,66,top-53,258,88);Place((RectTransform)DrawCloseButton.transform,318,top+5,45,45);
            Place(DrawTabs,33,top+56,324,42);for(var i=0;i<4;i++)Place((RectTransform)DrawCategoryButtons[i].transform,i*81,0,80,42);
            Place(DrawPoolName.rectTransform,38,top+108,314,43);Place(DrawVoucher,137,top+164,116,111);
            Place(DrawCount.rectTransform,38,top+279,314,35);Place(DrawInfo.rectTransform,36,top+320,318,56);
            Place((RectTransform)DrawBackButton.transform,37,top+407,143,42);Place((RectTransform)DrawOnceButton.transform,198,top+397,154,59);
            Place(DrawPreviewStamp.rectTransform,20,top+485,350,24);
        }
    }
}
