using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    public sealed partial class VisualSampleGallery
    {
        [Serializable] public sealed class TrailRecord
        {public string id,chineseName,name,resourcePath;public bool defaultOwned;public float[] uv;public float aspect;}
        [Serializable] public sealed class TrailManifest {public TrailRecord[] trails;}
        [Serializable] public sealed class TrailCardView
        {public RectTransform Root,Check,Lock;public RawImage Image;public Button Button;public Text Name,State;public SampleSlicedImage Strip;public SampleVectorGraphic Outline;}
        public RectTransform TrailContent,TrailViewer,TrailViewerPanel,TrailStage;
        public TrailCardView[] TrailCards;
        public RectTransform[] TrailHeadings;
        public Text[] TrailHeadingLabels;
        public Text TrailViewerTitle,TrailViewerInfo,TrailPhase,TrailStageLabel,TrailMiniLabel;
        public Button TrailBack,TrailPrevious,TrailNext,TrailPause,TrailRestart;
        public SampleWakeVoyage TrailVoyage;
        public int SelectedTrail;
        public bool TrailViewerOpen=>TrailViewer&&TrailViewer.gameObject.activeSelf;
        private TrailManifest trailManifest;
        public TrailRecord[] TrailRecords=>(trailManifest??(trailManifest=JsonUtility.FromJson<TrailManifest>(Resources.Load<TextAsset>(ArtRoot+"Trails/trail_catalog").text))).trails;
        public bool IsTrailPreviewOwned(int index)=>index>=0&&index<TrailRecords.Length&&(productionService!=null?productionService.OwnsAppearance(TrailIdentity(index)):TrailRecords[index].defaultOwned);
        private static Rect TrailUV(TrailRecord r)=>new Rect(r.uv[0],r.uv[1],r.uv[2],r.uv[3]);
        private void BuildTrails()
        {
            TrailContent=Rect("WholeTrailCards_PreviewOnly",Viewport);TrailCards=new TrailCardView[TrailRecords.Length];
            for(var i=0;i<TrailCards.Length;i++)
            {
                var record=TrailRecords[i];var c=new TrailCardView();TrailCards[i]=c;c.Root=Rect(record.id,TrailContent);
                var face=Rect("Visual",c.Root);Fill(face);var frame=Art("IvoryFrame","Card_Ivory",face);Fill(frame.rectTransform);c.Button=Button(c.Root,face,frame);
                var field=Vector("SeaPreviewField",face,SampleVectorGraphic.Shape.Field,new Color(.10f,.37f,.48f));field.Bottom=new Color(.27f,.60f,.69f);field.Radius=8;Place(field.rectTransform,5,5,72,103);
                c.Image=Raw("WholeTrailArtwork",face,record.resourcePath.Substring("TideboundUI/".Length),TrailUV(record));Fit(c.Image.rectTransform,new Rect(11,10,60,94),record.aspect);
                c.Name=Label("Name",face,"",13,Ink);Place(c.Name.rectTransform,2,109,78,23);
                c.Strip=Art("TrialState","Capsule_Cream",face);Place(c.Strip.rectTransform,5,133,72,20);c.Strip.Border=new Vector4(10,10,10,10);
                c.State=Label("State",c.Strip.transform,"",11,Ink);Fill(c.State.rectTransform);
                c.Lock=Vector("NotOwned",face,SampleVectorGraphic.Shape.Lock,Color.white).rectTransform;Place(c.Lock,59,81,17,20);
                c.Check=Vector("TrialSelection",face,SampleVectorGraphic.Shape.Check,new Color(.5f,1,.25f)).rectTransform;Place(c.Check,56,5,23,23);
                c.Outline=Vector("TrialOutline",face,SampleVectorGraphic.Shape.Outline,new Color(.36f,.79f,.18f));Fill(c.Outline.rectTransform,1.5f);c.Outline.Radius=10;c.Outline.Stroke=2;
            }
            TrailHeadings=new RectTransform[2];TrailHeadingLabels=new Text[2];
            for(var i=0;i<2;i++)
            {var r=Rect("TrailGroup_"+i,TrailContent);TrailHeadings[i]=r;var rule=r.gameObject.AddComponent<ShowcaseSectionRule>();rule.color=Brown;rule.raycastTarget=false;TrailHeadingLabels[i]=Label("Caption",r,"",18,Brown);}
            TrailViewer=Rect("TrailMotionPreview_NoSave",PreviewCanvas.transform);Fill(TrailViewer);
            var shade=TrailViewer.gameObject.AddComponent<Image>();shade.color=new Color(.01f,.055f,.09f,.95f);shade.raycastTarget=true;
            TrailViewerPanel=Art("GoldFrame","Panel_Gold",TrailViewer).rectTransform;
            TrailViewerTitle=Label("WholeStyleName",TrailViewer,"",25,Brown);
            TrailViewerInfo=Label("Scope",TrailViewer,"",12,Brown);
            TrailStage=Rect("WaterPlane_RecordedHistory",TrailViewer);var sea=TrailStage.gameObject.AddComponent<SampleVectorGraphic>();sea.Kind=SampleVectorGraphic.Shape.Field;sea.color=new Color(.035f,.31f,.42f);sea.Bottom=sea.color;sea.Radius=12;sea.raycastTarget=false;
            TrailStage.gameObject.AddComponent<RectMask2D>();TrailVoyage=TrailStage.gameObject.AddComponent<SampleWakeVoyage>();
            var ship=SkinRecords[0];TrailVoyage.Configure(Resources.Load<Texture2D>(ship.resourcePath),new Rect(ship.uv[0],ship.uv[1],ship.uv[2],ship.uv[3]),ship.aspect);
            TrailStageLabel=Label("LargeDemo",TrailStage,"",13,new Color(.80f,.96f,1));
            TrailMiniLabel=Label("SmallDemo",TrailStage,"",12,new Color(.80f,.96f,1));
            TrailPhase=Label("MotionPhase",TrailViewer,"",13,Brown);
            TrailPrevious=TrailTool("Previous",TrailViewer,"上一款");TrailNext=TrailTool("Next",TrailViewer,"下一款");
            TrailPause=TrailTool("Pause",TrailViewer,"暂停");TrailRestart=TrailTool("Restart",TrailViewer,"重播");
            TrailBack=TrailTool("Back",TrailViewer,"返回收藏");
            TrailContent.gameObject.SetActive(false);TrailViewer.gameObject.SetActive(false);
        }
        private Button TrailTool(string name,Transform parent,string text)
        {var root=Rect(name,parent);var face=Rect("Visual",root);Fill(face);var skin=Art("Cream","Capsule_Cream",face);Fill(skin.rectTransform);skin.Border=new Vector4(14,14,14,14);var b=Button(root,face,skin);Fill(Label("Label",face,text,15,Brown).rectTransform,2);return b;}
        private void WireTrails()
        {
            for(var i=0;i<TrailCards.Length;i++){var n=i;TrailCards[i].Button.onClick.RemoveAllListeners();TrailCards[i].Button.onClick.AddListener(()=>TryTrail(n));}
            TrailPrevious.onClick.RemoveAllListeners();TrailPrevious.onClick.AddListener(()=>TryTrail((SelectedTrail+TrailCards.Length-1)%TrailCards.Length));
            TrailNext.onClick.RemoveAllListeners();TrailNext.onClick.AddListener(()=>TryTrail((SelectedTrail+1)%TrailCards.Length));
            TrailPause.onClick.RemoveAllListeners();TrailPause.onClick.AddListener(()=>{TrailVoyage.Paused=!TrailVoyage.Paused;RefreshTrailPhase();});
            TrailRestart.onClick.RemoveAllListeners();TrailRestart.onClick.AddListener(()=>{TrailVoyage.Paused=false;TrailVoyage.Restart();});
            TrailBack.onClick.RemoveAllListeners();TrailBack.onClick.AddListener(CloseTrailPreview);
            var ship=SkinRecords[0];TrailVoyage.Configure(Resources.Load<Texture2D>(ship.resourcePath),new Rect(ship.uv[0],ship.uv[1],ship.uv[2],ship.uv[3]),ship.aspect);
        }
        public void TryTrail(int index)
        {
            if(index<0||index>=TrailCards.Length)return;SelectedTrail=index;
            TrailViewer.gameObject.SetActive(true);TrailVoyage.Paused=false;
            var r=TrailRecords[index];TrailVoyage.SetStyle((Texture2D)TrailCards[index].Image.texture,TrailUV(r),r.aspect);
            RefreshText();ApplyLayout();
        }
        public void CloseTrailPreview(){TrailViewer.gameObject.SetActive(false);RefreshText();}
        private void RefreshTrails()
        {
            if(TrailCards==null)return;
            for(var i=0;i<TrailCards.Length;i++)
            {
                var c=TrailCards[i];var selected=productionService!=null?productionService.SelectedTrailId==TrailIdentity(i):i==SelectedTrail;var owned=IsTrailPreviewOwned(i);
                c.Name.text=English?TrailRecords[i].name:TrailRecords[i].chineseName;c.Name.fontSize=English?11:13;
                c.State.text=selected?(owned?(English?"In use":"使用中"):(English?"Trying":"试用中")):owned?(English?"Owned":"已拥有"):(English?"Locked":"未解锁");
                c.Image.material=!owned&&!InspectArtwork?Resources.Load<Material>(ArtRoot+"Scenes/SceneLocked"):null;
                c.Check.gameObject.SetActive(selected);c.Lock.gameObject.SetActive(!owned&&!selected);c.Outline.gameObject.SetActive(selected);
                c.Strip.color=selected?(owned?new Color(.56f,.90f,.28f):new Color(1,.83f,.35f)):new Color(.70f,.86f,.92f);
            }
            var labels=English?new[]{"Default","Try Every Style"}:new[]{"初始拥有","完整款式试用"};
            for(var i=0;i<2;i++)
            {TrailHeadingLabels[i].text=labels[i];var width=i==0?120:160;Place(TrailHeadingLabels[i].rectTransform,(346-width)/2,0,width,31);var rule=TrailHeadings[i].GetComponent<ShowcaseSectionRule>();rule.CaptionWidth=width;rule.SetAllDirty();}
            var s=TrailRecords[SelectedTrail];TrailViewerTitle.text=(English?s.name:s.chineseName)+"  "+s.id;
            TrailViewerInfo.text=English?"Whole-style motion trial · no unlock or save":"整款动态试用 · 不解锁、不写存档";
            TrailStageLabel.text=English?"Enlarged · sail / turn / stop":"放大观察 · 航行 / 转弯 / 停船";
            TrailMiniLabel.text=English?"Small-scale motion · 3 ships":"小尺度同时航行 · 3 艘预览船";
            TrailPrevious.GetComponentInChildren<Text>().text=English?"Previous":"上一款";TrailNext.GetComponentInChildren<Text>().text=English?"Next":"下一款";
            TrailRestart.GetComponentInChildren<Text>().text=English?"Replay":"重播";TrailBack.GetComponentInChildren<Text>().text=English?"Collection":"返回收藏";
            RefreshTrailPhase();
            if(SelectedTab!=1)return;
            Statistics.transform.parent.gameObject.SetActive(true);Statistics.text=English?"Collected 1 / "+TrailCards.Length:"已收集 1 / "+TrailCards.Length;
            Hint.text=English?"Tap any card to try the whole trail":"点击任意卡片试用完整拖尾";
            SectionTitle.gameObject.SetActive(false);EmptyCategory.gameObject.SetActive(false);
            Stamp.text=InspectArtwork?"Unity 拖尾小样 · 彩色检视，不代表拥有":"Unity 拖尾小样 · 仅默认款拥有，其余可试用";
        }
        private void RefreshTrailPhase()
        {
            if(IsProduction||!TrailViewerOpen||!TrailVoyage)return;
            var phase=TrailVoyage.Paused?(English?"Paused":"已暂停"):TrailVoyage.Phase=="Sailing"?(English?"Sailing · history follows the turn":"航行中 · 尾迹沿经过的路径转弯"):TrailVoyage.Phase=="Stopped"?(English?"Stopped · remaining wake fades":"已停船 · 残留尾迹自然消散"):TrailVoyage.Phase=="Blocked"?(English?"Blocked feedback · no new wake":"受阻轻摆 · 不产生新尾迹"):(English?"Restarting":"重新演示");
            TrailPhase.text=phase;TrailPause.GetComponentInChildren<Text>().text=TrailVoyage.Paused?(English?"Resume":"继续"):(English?"Pause":"暂停");
        }
        private void LayoutTrails(float h)
        {
            if(!TrailContent)return;
            Place(TrailHeadings[0],0,0,346,32);Place(TrailCards[0].Root,0,37,82,158);
            Place(TrailHeadings[1],0,209,346,32);
            for(var i=1;i<TrailCards.Length;i++)Place(TrailCards[i].Root,((i-1)%4)*88,246+(i-1)/4*167,82,158);
            TrailContent.sizeDelta=new Vector2(346,246+Mathf.Ceil((TrailCards.Length-1)/4f)*167);
            Place(TrailViewerPanel,9,51,372,h-64);Place(TrailViewerTitle.rectTransform,22,62,346,44);Place(TrailViewerInfo.rectTransform,20,107,350,24);
            Place(TrailStage,22,141,346,h-301);Place(TrailStageLabel.rectTransform,7,7,332,23);Place(TrailMiniLabel.rectTransform,7,TrailStage.rect.height-170,332,20);
            Place(TrailPhase.rectTransform,22,h-156,346,24);
            Place((RectTransform)TrailPrevious.transform,25,h-124,94,34);Place((RectTransform)TrailPause.transform,125,h-124,64,34);
            Place((RectTransform)TrailRestart.transform,195,h-124,64,34);Place((RectTransform)TrailNext.transform,265,h-124,100,34);
            Place((RectTransform)TrailBack.transform,107,h-77,176,43);
            if(IsProduction)
            {
                Place((RectTransform)TrailPrevious.transform,25,h-124,150,34);Place((RectTransform)TrailNext.transform,215,h-124,150,34);
                RefreshStaticProductionTrail();
            }
        }
    }
}
