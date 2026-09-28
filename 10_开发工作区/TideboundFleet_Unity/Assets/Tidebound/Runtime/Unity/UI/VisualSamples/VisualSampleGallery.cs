using System;
using System.Linq;
using Tidebound.Collection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    /// <summary>Isolated visual review. All amounts, ownership and callbacks below are volatile preview data.</summary>
    public sealed partial class VisualSampleGallery : MonoBehaviour
    {
        [Serializable] public sealed class ArtRecord { public string name; public float[] uv; public float[] cuts; public float[] border; }
        [Serializable] public sealed class ArtManifest { public ArtRecord[] assets; }
        [Serializable] public sealed class CardView
        {
            public RectTransform Root; public Button Button; public RawImage Ship;
            public Text Name, Status; public SampleSlicedImage Strip;
            public SampleVectorGraphic Outline; public RectTransform Check, Lock;
        }
        public Canvas PreviewCanvas;
        public RectTransform Home, Popup, Panel, Header, Coins, Main, Entry, Showcase, Toolbar, Tabs;
        public ScrollRect Scroll;
        public RectTransform Viewport, Content;
        public Button MainButton, EntryButton, CloseButton;
        public Text MainLabel, LevelLabel, EntryLabel, PopupTitle, Statistics, SectionTitle, Hint, Stamp, EmptyCategory;
        public Button[] CategoryButtons;
        public CardView[] Cards;
        public RectTransform[] SourceHeadings;
        public Text[] SourceLabels;
        public bool English;
        public int SelectedTab = 2, SelectedCard;
        public int PreviewClearedLevel;
        public bool InspectArtwork;
        public int PreviewClicks { get; private set; }
        public bool PopupOpen => Popup && Popup.gameObject.activeSelf;
        private ArtManifest manifest;
        private Material silhouette;
        private Vector2 lastSize;
        private static readonly Color Ink = new Color(.035f,.24f,.36f);
        private static readonly Color Brown = new Color(.44f,.23f,.10f);
        private const string ArtRoot="TideboundUI/VisualSamples/";

        private void Awake()
        {
            if (!PreviewCanvas) Build();
            Wire(); RefreshText(); ApplyLayout();
        }
        private void LateUpdate()
        {
            var size = new Vector2(Screen.width,Screen.height);
            if(size!=lastSize) ApplyLayout();
            RefreshTrailPhase();
        }
        public void Build(string artManifestJson = null)
        {
            manifest=JsonUtility.FromJson<ArtManifest>(artManifestJson ?? Resources.Load<TextAsset>(ArtRoot+"asset_manifest").text);
            var canvasObject=new GameObject("VisualSampleCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);PreviewCanvas=canvasObject.GetComponent<Canvas>();PreviewCanvas.renderMode=RenderMode.ScreenSpaceOverlay;PreviewCanvas.pixelPerfect=true;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(390,844);scaler.matchWidthOrHeight=0;
            Background=Raw("HarborContext",canvasObject.transform,"Harbor_Background_v1",new Rect(0,0,1,1));Fill(Background.rectTransform);Background.color=Color.white;
            Home=Rect("SampleA_HomeComponents",canvasObject.transform);Fill(Home);
            Showcase=Raw("ExistingShowcase",Home,"Showcase/Showcase_H01_v1",ShowcaseArt.VisibleUV(ShowcaseCatalog.DefaultId)).rectTransform;
            Coins=Art("Coins","Capsule_Cream",Home).rectTransform;
            var coin=Art("AnchorCoin","Coin_Anchor",Coins);Place(coin.rectTransform,4,2,34,34);
            var amount=Label("PreviewAmount",Coins,"307",21,Ink);Place(amount.rectTransform,40,3,53,31);
            var mainRoot=Rect("PrimaryButton",Home); Main=mainRoot;
            var mainFace=Rect("Visual",mainRoot);Fill(mainFace);
            var mainSurface=Art("GoldSurface","CTA_Gold",mainFace);Fill(mainSurface.rectTransform);
            MainButton=Button(mainRoot,mainFace,mainSurface);
            MainLabel=Label("ActionText",mainFace,"开始航行",29,Ink);Place(MainLabel.rectTransform,16,5,228,49);
            LevelLabel=Label("LevelText",mainFace,"第10关",17,Ink);Place(LevelLabel.rectTransform,20,50,220,26);
            Entry=Rect("CollectionEntry",Home);
            var entryFace=Rect("Visual",Entry);Fill(entryFace);
            var entryBase=Raw("PearlBase",entryFace,"Skins/UI_HomeEntry_Pearl_v2",new Rect(54f/1254,66f/1254,1145f/1254,1139f/1254));Fill(entryBase.rectTransform);
            EntryButton=Button(Entry,entryFace,entryBase);
            var book=Raw("CollectionIcon",entryFace,"Icon_Collection_v1",new Rect(112f/1254,310f/1254,1088f/1254,650f/1254));Fit(book.rectTransform,new Rect(5,9,70,53),1088f/650);
            EntryLabel=Label("Name",entryFace,"收藏",17,Ink);Place(EntryLabel.rectTransform,2,61,76,30);

            Popup=Rect("SampleB_CollectionPopup",canvasObject.transform);Fill(Popup);
            var blocker=Rect("InputBlocker",Popup).gameObject.AddComponent<Image>();Fill(blocker.rectTransform);blocker.color=new Color(.015f,.075f,.12f,.42f);blocker.raycastTarget=true;
            Panel=Art("GoldFrame","Panel_Gold",Popup).rectTransform;
            Header=Raw("ExistingWoodHeader",Popup,"Skins/UI_HarborHeader_v1",new Rect(79f/2172,27f/724,2034f/2172,682f/724)).rectTransform;
            PopupTitle=Label("TitleText",Header,"形象",31,Color.white);Place(PopupTitle.rectTransform,55,38,176,52);
            var titleShadow=PopupTitle.gameObject.AddComponent<Outline>();titleShadow.effectColor=new Color(.25f,.09f,.025f,.9f);titleShadow.effectDistance=new Vector2(1.4f,-1.4f);
            var closeRoot=Rect("Close",Popup);var closeFace=Rect("Visual",closeRoot);Fill(closeFace);
            var closeBase=Art("CoralBase","Close_Coral",closeFace);Fill(closeBase.rectTransform);CloseButton=Button(closeRoot,closeFace,closeBase);
            var cross=Vector("Cross",closeFace,SampleVectorGraphic.Shape.Cross,Color.white);Fill(cross.rectTransform,8);
            var statsBase=Art("StatisticsBase","Capsule_Cream",Popup);statsBase.color=new Color(.97f,.88f,.75f);Place(statsBase.rectTransform,97,142,196,31);
            Statistics=Label("PreviewStatistics",statsBase.transform,"",15,Brown);Fill(Statistics.rectTransform);
            SectionTitle=Label("FixedSectionTitle",Popup,"主页形象",19,Brown);Place(SectionTitle.rectTransform,50,183,290,30);
            Viewport=Rect("ListViewport",Popup);Viewport.gameObject.AddComponent<RectMask2D>();
            var input=Viewport.gameObject.AddComponent<Image>();input.color=new Color(1,1,1,.001f);input.raycastTarget=true;
            Scroll=Viewport.gameObject.AddComponent<ScrollRect>();Scroll.horizontal=false;Scroll.vertical=true;Scroll.movementType=ScrollRect.MovementType.Clamped;Scroll.inertia=true;Scroll.decelerationRate=.11f;Scroll.scrollSensitivity=28;Scroll.viewport=Viewport;
            Content=Rect("PreviewCardsOnly",Viewport);Scroll.content=Content;
            Cards=new CardView[ShowcaseCatalog.All.Count];
            for(var i=0;i<Cards.Length;i++) Cards[i]=BuildCard(i);
            SourceHeadings=new RectTransform[ShowcaseCatalog.Groups.Count];SourceLabels=new Text[SourceHeadings.Length];
            for(var i=0;i<SourceHeadings.Length;i++)
            {
                var heading=Rect("Source_"+ShowcaseCatalog.Groups[i].Source,Content);SourceHeadings[i]=heading;
                var rule=heading.gameObject.AddComponent<ShowcaseSectionRule>();rule.color=Brown;rule.raycastTarget=false;
                SourceLabels[i]=Label("Heading",heading,"",20,Brown);
            }
            EmptyCategory=Label("CategoryScopeNote",Popup,"",17,Ink);EmptyCategory.gameObject.SetActive(false);
            Hint=Label("FixedHint",Popup,"点击切换主页形象",13,Brown);
            Tabs=Rect("FixedCategoryTabs",Popup);CategoryButtons=new Button[4];
            for(var i=0;i<4;i++)
            {
                var root=Rect("Tab_"+i,Tabs);var face=Rect("Visual",root);Fill(face);
                var skin=Art("Skin",i==2?"Tab_Gold":"Tab_Brown",face);Fill(skin.rectTransform);
                CategoryButtons[i]=Button(root,face,skin);
                var text=Label("Text",face,"",17,i==2?Brown:new Color(1,.94f,.80f));Fill(text.rectTransform,2);
            }
            Toolbar=Rect("PreviewControls_NotProductUI",canvasObject.transform);
            Tool("A · 首页",Toolbar,0);Tool("B · 形象",Toolbar,1);Tool("EN / 中文",Toolbar,2);Tool("禁用切换",Toolbar,3);
            Stamp=Label("PreviewStamp",canvasObject.transform,"Unity 视觉小样 · 预览数据",11,Color.white);Place(Stamp.rectTransform,5,6,380,20);
            BuildScenes();
            BuildSkins();
            BuildTrails();
            BuildDrawPopup();
            Popup.gameObject.SetActive(false);
            ApplyLayout(); RefreshText();
        }
        private CardView BuildCard(int i)
        {
            var card=new CardView();card.Root=Rect("Card_"+i,Content);
            var face=Rect("Visual",card.Root);Fill(face);
            var frame=Art("IvoryFrame","Card_Ivory",face);Fill(frame.rectTransform);card.Button=Button(card.Root,face,frame);
            card.Outline=Vector("UsingOutline",face,SampleVectorGraphic.Shape.Outline,new Color(1,.26f,.69f));Fill(card.Outline.rectTransform,1.5f);card.Outline.Radius=10;card.Outline.Stroke=2;
            var field=Vector("ImageField",face,SampleVectorGraphic.Shape.Field,new Color(.23f,.72f,.96f));Place(field.rectTransform,6,7,70,87);field.Radius=7;
            var definition=ShowcaseCatalog.All[i];
            card.Ship=Raw("VisibleBoundsShip",face,definition.ResourcePath.Substring("TideboundUI/".Length),ShowcaseArt.VisibleUV(definition.Id));
            Fit(card.Ship.rectTransform,new Rect(7,11,69.5f,81),ShowcaseArt.Aspect(definition.Id));
            card.Name=Label("RealNameText",face,"",12,Ink);Place(card.Name.rectTransform,3,92,76,30);
            card.Strip=Art("StateStrip","Capsule_Cream",face);Place(card.Strip.rectTransform,6,122,70,19);card.Strip.Border=new Vector4(9,9,9,9);
            card.Status=Label("RealStateText",card.Strip.transform,"",11,Ink);Fill(card.Status.rectTransform);
            card.Check=Rect("UsingBadge",face);Place(card.Check,58,-2,24,24);
            var badge=Art("Badge","Capsule_Cream",card.Check);Fill(badge.rectTransform);badge.Border=new Vector4(12,12,12,12);badge.color=new Color(.49f,.87f,.16f);
            var mark=Vector("Check",card.Check,SampleVectorGraphic.Shape.Check,Color.white);Fill(mark.rectTransform,2);
            card.Lock=Rect("LockedBadge",face);Place(card.Lock,57,73,24,24);
            var disk=Art("Badge","Capsule_Cream",card.Lock);Fill(disk.rectTransform);disk.Border=new Vector4(12,12,12,12);disk.color=new Color(.18f,.51f,.74f);
            var padlock=Vector("Lock",card.Lock,SampleVectorGraphic.Shape.Lock,Color.white);Fill(padlock.rectTransform,1);
            return card;
        }
        private void Wire()
        {
            MainButton.onClick.RemoveAllListeners();MainButton.onClick.AddListener(()=>{PreviewClicks++;LevelLabel.text=English?"Preview · no level launched":"预览点击 · 未进入关卡";LevelLabel.fontSize=12;});
            EntryButton.onClick.RemoveAllListeners();EntryButton.onClick.AddListener(()=>ShowPopup(true));
            CloseButton.onClick.RemoveAllListeners();CloseButton.onClick.AddListener(()=>ShowPopup(false));
            for(var i=0;i<4;i++){var n=i;CategoryButtons[i].onClick.RemoveAllListeners();CategoryButtons[i].onClick.AddListener(()=>SelectTab(n));}
            for(var i=0;i<Cards.Length;i++){var n=i;Cards[i].Button.onClick.RemoveAllListeners();Cards[i].Button.onClick.AddListener(()=>SelectCard(n));}
            var controls=Toolbar.GetComponentsInChildren<Button>();
            foreach(var b in controls)b.onClick.RemoveAllListeners();
            controls[0].onClick.AddListener(()=>ShowPopup(false));controls[1].onClick.AddListener(()=>ShowPopup(true));
            controls[2].onClick.AddListener(()=>SetEnglish(!English));controls[3].onClick.AddListener(()=>{if(PopupOpen)SetArtworkInspection(!InspectArtwork);else MainButton.interactable=!MainButton.interactable;});
            RefreshCards();
            WireScenes();
            WireSkins();
            WireTrails();
            WireDrawPopup();
        }
        public void SetEnglish(bool value) { English=value;RefreshText(); }
        public bool IsPreviewOwned(int index) => index>=0&&index<ShowcaseCatalog.All.Count&&(productionService!=null?productionService.OwnsShowcase(ShowcaseCatalog.All[index].Id):ShowcaseCatalog.IsOwned(ShowcaseCatalog.All[index].Id,PreviewClearedLevel));
        public void SetPreviewProgress(int cleared)
        {PreviewClearedLevel=Mathf.Max(0,cleared);if(!IsPreviewOwned(SelectedCard))SelectedCard=0;RefreshText();}
        public void SetArtworkInspection(bool value) { InspectArtwork=value;RefreshText(); }
        public void ShowPopup(bool value) { Popup.gameObject.SetActive(value); if(value)SelectTab(2);RefreshText(); }
        public void SelectTab(int index)
        {
            SelectedTab=Mathf.Clamp(index,0,3);Viewport.gameObject.SetActive(true);EmptyCategory.gameObject.SetActive(false);
            TrailContent.gameObject.SetActive(SelectedTab==1);
            Content.gameObject.SetActive(SelectedTab==2);SceneContent.gameObject.SetActive(SelectedTab==3);SkinContent.gameObject.SetActive(SelectedTab==0);
            Scroll.content=SelectedTab==0?SkinContent:SelectedTab==1?TrailContent:SelectedTab==3?SceneContent:Content;
            for(var i=0;i<4;i++) { SetArt(CategoryButtons[i].GetComponentInChildren<SampleSlicedImage>(),i==SelectedTab?"Tab_Gold":"Tab_Brown");CategoryButtons[i].GetComponentInChildren<Text>().color=i==SelectedTab?Brown:new Color(1,.94f,.80f); }
            RefreshText();ApplyLayout();Scroll.StopMovement();Scroll.verticalNormalizedPosition=1;
        }
        public void SelectCard(int index)
        {
            if(index<0||index>=Cards.Length)return;
            if(productionService!=null){SelectProductionShowcase(index);return;}
            if(!IsPreviewOwned(index))
            {
                var item=ShowcaseCatalog.All[index];
                Hint.text=item.Acquisition==ShowcaseAcquisition.Level?(English?"Unlock after level "+item.ClearLevel:"通关第 "+item.ClearLevel+" 关获取"):(English?"Acquisition not open yet":"获取方式尚未开放");return;
            }
            SelectedCard=index;RefreshCards();RefreshHomeShip();Hint.text=English?"Preview selection updated":"已切换预览形象";
        }
        public void RefreshText()
        {
            if(!PreviewCanvas)return;
            var font=Resources.Load<Font>(English?"TideboundUI/LilitaOne-Regular":"TideboundUI/ResourceHanRoundedCN-Bold");
            foreach(var t in PreviewCanvas.GetComponentsInChildren<Text>(true))t.font=font;
            // Preview tools remain bilingual and never depend on the product's persisted locale.
            foreach(var t in Toolbar.GetComponentsInChildren<Text>())t.font=Resources.Load<Font>("TideboundUI/ResourceHanRoundedCN-Bold");
            Stamp.font=Resources.Load<Font>("TideboundUI/ResourceHanRoundedCN-Bold");
            MainLabel.text=English?"Set Sail":"开始航行";MainLabel.fontSize=English?32:29;
            LevelLabel.text=English?"Level 10":"第10关";LevelLabel.fontSize=17;
            EntryLabel.text=English?"Collection":"收藏";EntryLabel.fontSize=English?15:17;
            PopupTitle.text=English?"Showcase":"形象";PopupTitle.fontSize=English?27:31;
            var owned=Enumerable.Range(0,Cards.Length).Count(IsPreviewOwned);
            Statistics.text=(English?"Collected ":"已收集 ")+owned+"/"+Cards.Length;
            var names=English?new[]{"Skins","Trails","Showcase","Scenes"}:new[]{"皮肤","拖尾","形象","场景"};
            PopupTitle.text=names[SelectedTab];Statistics.transform.parent.gameObject.SetActive(SelectedTab!=1);
            for(var i=0;i<4;i++){var t=CategoryButtons[i].GetComponentInChildren<Text>();t.text=names[i];t.fontSize=English?15:17;}
            SectionTitle.text=SelectedTab==2?(English?"Home Showcase":"主页形象"):names[SelectedTab];
            SectionTitle.gameObject.SetActive(SelectedTab==1);
            for(var i=0;i<SourceLabels.Length;i++)
            {
                var label=SourceLabels[i];label.text=English?ShowcaseCatalog.Groups[i].Title:ShowcaseCatalog.Groups[i].ChineseTitle;label.fontSize=English?18:20;
                var width=English?126:100;Place(label.rectTransform,(346-width)/2,0,width,32);
                var rule=SourceHeadings[i].GetComponent<ShowcaseSectionRule>();rule.CaptionWidth=width;rule.SetAllDirty();
            }
            EmptyCategory.text=English?"No preview content in this category.\nTap Showcase to return.":"本轮未制作此分类内容\n点击「形象」返回";
            Hint.text=English?"Tap to change preview showcase":"点击切换主页形象";
            Stamp.text=PopupOpen?(InspectArtwork?"Unity 素材检视 · 彩色不代表拥有":"Unity 视觉小样 B · 新玩家 / 预览进度"):"Unity 视觉小样 A · 预览数据";
            Toolbar.GetComponentsInChildren<Button>()[3].GetComponentInChildren<Text>().text=PopupOpen?(InspectArtwork?"返回状态":"素材检视"):"禁用切换";
            RefreshCards();RefreshHomeShip();
            RefreshScenes();
            RefreshSkins();
            RefreshTrails();
            RefreshDrawEntry();
            RefreshProductionText();
        }
        private void RefreshCards()
        {
            if(Cards==null)return;
            if(!silhouette) silhouette=ShowcaseArt.LockedMaterial;
            for(var i=0;i<Cards.Length;i++)
            {
                var c=Cards[i];var locked=!IsPreviewOwned(i);var selected=!locked&&i==SelectedCard;
                var item=ShowcaseCatalog.All[i];c.Name.text=English?item.Name:item.ChineseName;c.Name.fontSize=English?11:13;
                c.Status.text=selected?(English?"In use":"使用中"):locked?(English?"Locked":"未解锁"):(English?"Owned":"已拥有");
                c.Strip.color=selected?new Color(.59f,.94f,.22f):locked?new Color(.64f,.79f,.91f):new Color(.61f,.88f,1);
                c.Outline.gameObject.SetActive(selected);c.Check.gameObject.SetActive(selected);c.Lock.gameObject.SetActive(locked);
                c.Ship.material=locked&&!InspectArtwork?silhouette:null;
            }
        }
        private void RefreshHomeShip()
        {
            if(!Showcase)return;
            var id=ShowcaseCatalog.All[SelectedCard].Id;var raw=Showcase.GetComponent<RawImage>();raw.texture=ShowcaseArt.Load(id);raw.uvRect=ShowcaseArt.VisibleUV(id);
            var h=Application.isPlaying&&Screen.width>0?Screen.height*390f/Screen.width:844;
            Fit(Showcase,new Rect(112,h*.33f,237,245),ShowcaseArt.Aspect(id));
        }
        public void ApplyLayout()
        {
            if(!PreviewCanvas)return;
            lastSize=new Vector2(Screen.width,Screen.height);
            var h=Screen.width>0?Screen.height*390f/Screen.width:844;
            if(!Application.isPlaying)h=844;
            Place(Coins,72,50,98,38);Place(Entry,18,h*.35f,80,94);
            RefreshHomeShip();
            Place(Main,65,h-224,260,94);
            Place(Panel,8,95,374,h-146);Place(Header,52,26,286,96);
            Place((RectTransform)CloseButton.transform,332,94,48,48);
            Place(Viewport,22,181,346,h-320);
            var top=0f;
            for(var group=0;group<ShowcaseCatalog.Groups.Count;group++)
            {
                Place(SourceHeadings[group],0,top,346,32);top+=38;
                var indices=ShowcaseCatalog.Groups[group].CatalogIndices;
                for(var i=0;i<indices.Count;i++)Place(Cards[indices[i]].Root,(i%4)*88,top+(i/4)*153,82,146);
                top+=((indices.Count+3)/4)*153+12;
            }
            Content.sizeDelta=new Vector2(346,top-7);
            Place(EmptyCategory.rectTransform,36,260,318,90);
            Place(Hint.rectTransform,24,h-133,342,22);Place(Tabs,19,h-107,352,49);
            for(var i=0;i<4;i++)Place((RectTransform)CategoryButtons[i].transform,i*88,0,87,49);
            Place(Toolbar,12,h-37,366,30);
            LayoutScenes(h);
            LayoutSkins(h);
            LayoutTrails(h);
            if(IsProduction){LayoutProductionTransactions();RefreshProductionText();}
            Canvas.ForceUpdateCanvases();
        }
        private void Tool(string text,Transform parent,int i)
        {var root=Rect("Control_"+i,parent);Place(root,i*92,0,88,29);var face=Rect("Visual",root);Fill(face);var skin=Art("Skin","Capsule_Cream",face);Fill(skin.rectTransform);skin.Border=new Vector4(13,13,13,13);Button(root,face,skin);var t=Label("Label",face,text,11,Ink);Fill(t.rectTransform);}
        private static Button Button(RectTransform root,RectTransform face,Graphic surface)
        {
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var b=root.gameObject.AddComponent<Button>();b.targetGraphic=hit;b.transition=Selectable.Transition.None;b.navigation=new Navigation{mode=Navigation.Mode.None};
            var feedback=root.gameObject.AddComponent<SampleButtonFeedback>();feedback.Face=face;feedback.Surface=surface;return b;
        }
        private SampleSlicedImage Art(string name,string key,Transform parent)
        {var a=Rect(name,parent).gameObject.AddComponent<SampleSlicedImage>();a.raycastTarget=false;SetArt(a,key);return a;}
        private void SetArt(SampleSlicedImage graphic,string key)
        {
            if(manifest==null)manifest=JsonUtility.FromJson<ArtManifest>(Resources.Load<TextAsset>(ArtRoot+"asset_manifest").text);
            var item=Array.Find(manifest.assets,a=>a.name==key);if(item==null)throw new InvalidOperationException("Missing sample art: "+key);
            graphic.Texture=Resources.Load<Texture2D>(ArtRoot+key);graphic.UV=new Rect(item.uv[0],item.uv[1],item.uv[2],item.uv[3]);
            graphic.Cuts=new Vector4(item.cuts[0],item.cuts[1],item.cuts[2],item.cuts[3]);graphic.Border=new Vector4(item.border[0],item.border[1],item.border[2],item.border[3]);graphic.SetAllDirty();
        }
        private static RawImage Raw(string name,Transform parent,string path,Rect uv)
        {var raw=Rect(name,parent).gameObject.AddComponent<RawImage>();raw.texture=Resources.Load<Texture2D>("TideboundUI/"+path);raw.uvRect=uv;raw.raycastTarget=false;return raw;}
        private static SampleVectorGraphic Vector(string name,Transform parent,SampleVectorGraphic.Shape kind,Color color)
        {var g=Rect(name,parent).gameObject.AddComponent<SampleVectorGraphic>();g.Kind=kind;g.color=color;g.raycastTarget=false;return g;}
        public static RectTransform Rect(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);return r;}
        public static void Place(RectTransform r,float x,float top,float width,float height)
        {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-top);r.sizeDelta=new Vector2(width,height);}
        public static void Fill(RectTransform r,float inset=0)
        {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.one*inset;r.offsetMax=-Vector2.one*inset;}
        private static void Fit(RectTransform r,Rect area,float aspect)
        {var w=Mathf.Min(area.width,area.height*aspect);var h=w/aspect;Place(r,area.x+(area.width-w)/2,area.y+(area.height-h)/2,w,h);}
        private static Text Label(string name,Transform parent,string content,int size,Color color)
        {var t=Rect(name,parent).gameObject.AddComponent<Text>();t.font=Resources.Load<Font>("TideboundUI/ResourceHanRoundedCN-Bold");t.fontSize=size;t.color=color;t.text=content;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.supportRichText=false;t.resizeTextForBestFit=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
    }
}
