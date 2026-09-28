using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    public sealed partial class VisualSampleGallery
    {
        [Serializable] public sealed class SceneRecord
        {public string id,chineseName,name,group,resourcePath;public int requirement;public float thumbnailFocusY;}
        [Serializable] public sealed class SceneManifest {public SceneRecord[] scenes;}
        [Serializable] public sealed class SceneCardView
        {
            public RectTransform Root,Check,Lock;public Button Button;public SampleSceneImage Image;
            public Text Name,State;public SampleSlicedImage Strip;public SampleVectorGraphic Outline;
        }
        public RawImage Background,SceneLargeImage;
        public RectTransform SceneContent,SceneViewer,SceneDraw;
        public SceneCardView[] SceneCards;
        public RectTransform[] SceneHeadings;
        public Text[] SceneHeadingLabels;
        public Text SceneDrawTitle,SceneDrawSubtitle,SceneViewerTitle,SceneViewerInfo;
        public Button SceneDrawButton,SceneBackButton;
        public int SelectedScene,ScenePreviewClearedLevel;
        public int InspectedScene {get;private set;}
        public bool SceneViewerOpen=>SceneViewer&&SceneViewer.gameObject.activeSelf;
        private SceneManifest sceneManifest;
        private Material sceneLocked;
        public SceneRecord[] SceneRecords=> (sceneManifest??(sceneManifest=JsonUtility.FromJson<SceneManifest>(Resources.Load<TextAsset>(ArtRoot+"Scenes/scene_catalog").text))).scenes;
        private static readonly string[] SceneGroups={"Initial","Level","Share","Draw"};
        public int[] SceneIndices(int group)=>SceneRecords.Select((s,i)=>new{s,i}).Where(v=>v.s.group==SceneGroups[group]).OrderBy(v=>v.s.requirement).Select(v=>v.i).ToArray();
        public bool IsScenePreviewOwned(int index)=>index>=0&&index<SceneRecords.Length&&(productionService!=null?productionService.OwnsAppearance(SceneIdentity(index)):(SceneRecords[index].group=="Initial"||SceneRecords[index].group=="Level"&&SceneRecords[index].requirement<=ScenePreviewClearedLevel));
        public void SetScenePreviewProgress(int level)
        {ScenePreviewClearedLevel=Mathf.Max(0,level);if(!IsScenePreviewOwned(SelectedScene))SelectedScene=0;RefreshText();}
        private void BuildScenes()
        {
            SceneContent=Rect("SceneCards_PreviewDataOnly",Viewport);SceneCards=new SceneCardView[SceneRecords.Length];
            for(var i=0;i<SceneCards.Length;i++)
            {
                var c=new SceneCardView();SceneCards[i]=c;c.Root=Rect(SceneRecords[i].id,SceneContent);var face=Rect("Visual",c.Root);Fill(face);
                var frame=Art("IvoryFrame","Card_Ivory",face);Fill(frame.rectTransform);c.Button=Button(c.Root,face,frame);
                c.Image=Rect("SceneThumbnail",face).gameObject.AddComponent<SampleSceneImage>();Place(c.Image.rectTransform,6,6,156,104);c.Image.raycastTarget=false;c.Image.Texture=Resources.Load<Texture2D>(SceneRecords[i].resourcePath);
                var uvHeight=(float)c.Image.Texture.width/c.Image.Texture.height/(156f/104);c.Image.UV=new Rect(0,Mathf.Clamp(SceneRecords[i].thumbnailFocusY-uvHeight/2,0,1-uvHeight),1,uvHeight);
                c.Outline=Vector("SelectedOutline",face,SampleVectorGraphic.Shape.Outline,new Color(.25f,.78f,.16f));Fill(c.Outline.rectTransform,1.5f);c.Outline.Radius=12;c.Outline.Stroke=2;
                c.Name=Label("SceneName",face,"",15,Ink);Place(c.Name.rectTransform,6,112,94,26);c.Name.alignment=TextAnchor.MiddleLeft;
                c.Strip=Art("SceneState","Capsule_Cream",face);Place(c.Strip.rectTransform,101,115,62,21);c.Strip.Border=new Vector4(10,10,10,10);
                c.State=Label("State",c.Strip.transform,"",11,Brown);Place(c.State.rectTransform,14,0,47,21);
                c.Lock=Vector("Lock",c.Strip.transform,SampleVectorGraphic.Shape.Lock,new Color(.36f,.33f,.29f)).rectTransform;Place(c.Lock,2,3,14,15);
                c.Check=Rect("UsingBadge",face);Place(c.Check,141,-1,25,25);var disk=Art("Badge","Capsule_Cream",c.Check);Fill(disk.rectTransform);disk.Border=new Vector4(12,12,12,12);disk.color=new Color(.49f,.87f,.16f);Fill(Vector("Check",c.Check,SampleVectorGraphic.Shape.Check,Color.white).rectTransform,2);
            }
            SceneHeadings=new RectTransform[4];SceneHeadingLabels=new Text[4];
            for(var i=0;i<4;i++)
            {var r=Rect("SceneSource_"+SceneGroups[i],SceneContent);SceneHeadings[i]=r;var line=r.gameObject.AddComponent<ShowcaseSectionRule>();line.color=Brown;line.raycastTarget=false;SceneHeadingLabels[i]=Label("Caption",r,"",20,Brown);}
            SceneDraw=Rect("SharedCollectionDrawEntry",Popup);var drawFace=Rect("Visual",SceneDraw);Fill(drawFace);var drawArt=Art("Gold","CTA_Gold",drawFace);Fill(drawArt.rectTransform);drawArt.Border=new Vector4(42,14,42,29);SceneDrawButton=Button(SceneDraw,drawFace,drawArt);
            DrawEntryCoin=Art("FixedAnchorCoin","Coin_Anchor",drawFace).rectTransform;Place(DrawEntryCoin,24,8,45,46);
            SceneDrawTitle=Label("Action",drawFace,"",25,Brown);Place(SceneDrawTitle.rectTransform,77,0,153,41);
            SceneDrawSubtitle=Label("PoolName",drawFace,"",14,Brown);Place(SceneDrawSubtitle.rectTransform,77,33,153,25);
            SceneViewer=Rect("SceneInspectionOnly",PreviewCanvas.transform);Fill(SceneViewer);
            var overlay=SceneViewer.gameObject.AddComponent<Image>();overlay.color=new Color(.015f,.055f,.08f,.96f);overlay.raycastTarget=true;
            SceneLargeImage=Rect("OriginalAspectScene",SceneViewer).gameObject.AddComponent<RawImage>();SceneLargeImage.raycastTarget=false;
            var top=Rect("FixedTitleBar",SceneViewer).gameObject.AddComponent<Image>();top.color=new Color(.01f,.07f,.12f,.88f);Place(top.rectTransform,0,0,390,65);
            SceneViewerTitle=Label("Name",top.transform,"",24,Color.white);Place(SceneViewerTitle.rectTransform,18,10,354,38);
            SceneViewerInfo=Label("SourceAndPreviewScope",SceneViewer,"",17,Color.white);
            var back=Rect("BackToScenes",SceneViewer);var backFace=Rect("Visual",back);Fill(backFace);var baseArt=Art("Cream","Capsule_Cream",backFace);Fill(baseArt.rectTransform);SceneBackButton=Button(back,backFace,baseArt);Fill(Label("Label",backFace,"返回场景",18,Brown).rectTransform);
            SceneViewer.gameObject.SetActive(false);SceneContent.gameObject.SetActive(false);SceneDraw.gameObject.SetActive(false);
        }
        private void WireScenes()
        {
            for(var i=0;i<SceneCards.Length;i++){var index=i;SceneCards[i].Button.onClick.RemoveAllListeners();SceneCards[i].Button.onClick.AddListener(()=>SelectScene(index));}
            SceneBackButton.onClick.RemoveAllListeners();SceneBackButton.onClick.AddListener(()=>SceneViewer.gameObject.SetActive(false));
            SceneDrawButton.onClick.RemoveAllListeners();SceneDrawButton.onClick.AddListener(OpenDrawPopup);
        }
        public void ScrollSceneGroup(int group)
        {Scroll.StopMovement();var range=Mathf.Max(1,SceneContent.rect.height-Viewport.rect.height);Scroll.verticalNormalizedPosition=1-Mathf.Clamp(-SceneHeadings[group].anchoredPosition.y,0,range)/range;}
        public void SelectScene(int index)
        {
            if(index<0||index>=SceneCards.Length)return;
            if(productionService!=null){if(!IsScenePreviewOwned(index)){InspectScene(index);return;}SelectProductionAppearance(SceneIdentity(index));return;}
            if(IsScenePreviewOwned(index)){SelectedScene=index;RefreshText();Hint.text=English?"Preview background changed":"已切换预览背景";}
            else InspectScene(index);
        }
        public string SceneRequirement(int i)
        {
            var s=SceneRecords[i];
            if(s.group=="Initial")return English?"Initially owned":"初始拥有";
            if(s.group=="Level")return English?"Clear level "+s.requirement:"通关第 "+s.requirement+" 关获取";
            if(s.group=="Share")return s.requirement==1?(English?"Complete your first share task":"首次完成有效分享任务"):(English?"Complete share tasks on 3 days":"累计 3 天完成分享任务");
            return English?"Home scene draw reward":"主页场景抽奖获取";
        }
        public void InspectScene(int index)
        {
            InspectedScene=index;SceneViewer.gameObject.SetActive(true);SceneLargeImage.texture=SceneCards[index].Image.Texture;SceneViewerTitle.text=English?SceneRecords[index].name:SceneRecords[index].chineseName;
            SceneViewerInfo.text=SceneRequirement(index)+"\n"+(English?"Preview only · does not unlock":"仅预览 · 不解锁、不消耗");LayoutScenes(Screen.height*390f/Screen.width);
        }
        private void RefreshScenes()
        {
            if(SceneCards==null)return;
            if(!sceneLocked)sceneLocked=Resources.Load<Material>(ArtRoot+"Scenes/SceneLocked");
            var names=English?new[]{"Initially Owned","Level Rewards","Share Rewards","Draw Rewards"}:new[]{"初始拥有","通关获取","分享获取","抽奖获取"};
            for(var g=0;g<4;g++)
            {var t=SceneHeadingLabels[g];t.text=names[g];t.fontSize=English?18:20;var width=English?126:100;Place(t.rectTransform,(346-width)/2,0,width,32);var rule=SceneHeadings[g].GetComponent<ShowcaseSectionRule>();rule.CaptionWidth=width;rule.SetAllDirty();}
            for(var i=0;i<SceneCards.Length;i++)
            {
                var c=SceneCards[i];var owned=IsScenePreviewOwned(i);var selected=owned&&i==SelectedScene;
                c.Name.text=English?SceneRecords[i].name:SceneRecords[i].chineseName;c.Name.fontSize=English?12:15;
                c.State.text=selected?(English?"In use":"使用中"):owned?(English?"Owned":"已拥有"):(English?"Locked":"未解锁");
                c.State.fontSize=English?10:11;Place(c.State.rectTransform,owned?0:14,0,owned?62:47,21);
                c.State.color=selected?Color.white:Brown;c.Strip.color=selected?new Color(.4f,.84f,.18f):owned?new Color(.66f,.9f,1):new Color(.86f,.84f,.79f);
                c.Lock.gameObject.SetActive(!owned);c.Check.gameObject.SetActive(selected);c.Outline.gameObject.SetActive(selected);
                c.Image.material=!owned&&!InspectArtwork?sceneLocked:null;c.Image.SetMaterialDirty();
            }
            Background.texture=SceneCards[SelectedScene].Image.Texture;RefreshBackgroundCrop();
            if(SelectedTab!=3)return;
            PopupTitle.text=English?"Scenes":"场景";Statistics.text=(English?"Collected ":"已收集 ")+Enumerable.Range(0,SceneCards.Length).Count(IsScenePreviewOwned)+"/"+SceneCards.Length;
            Hint.text=English?"Tap a locked scene to preview":"点击未解锁场景可预览";
            SceneBackButton.GetComponentInChildren<Text>().text=English?"Back to Scenes":"返回场景";
            Stamp.text=InspectArtwork?"Unity 场景素材检视 · 彩色不代表拥有":"Unity 场景小样 · 仅预览数据";
        }
        private void RefreshBackgroundCrop()
        {
            if(!Background||!Background.texture)return;
            var aspect=Screen.width>0?(float)Screen.width/Screen.height:390f/844;var texAspect=(float)Background.texture.width/Background.texture.height;
            if(aspect<texAspect){var w=aspect/texAspect;Background.uvRect=new Rect((1-w)/2,0,w,1);}else{var h=texAspect/aspect;Background.uvRect=new Rect(0,(1-h)/2,1,h);}
        }
        private void LayoutScenes(float h)
        {
            if(!SceneContent)return;RefreshBackgroundCrop();var top=0f;
            for(var g=0;g<4;g++)
            {Place(SceneHeadings[g],0,top,346,32);top+=38;var indices=SceneIndices(g);for(var j=0;j<indices.Length;j++)Place(SceneCards[indices[j]].Root,j%2*178,top+j/2*151,168,144);top+=((indices.Length+1)/2)*151+12;}
            SceneContent.sizeDelta=new Vector2(346,top-7);
            Place(Viewport,22,181,346,h-408);Place(Hint.rectTransform,24,h-220,342,22);Place(Tabs,19,h-194,352,44);
            for(var i=0;i<4;i++)Place((RectTransform)CategoryButtons[i].transform,i*88,0,87,44);
            Place(SceneDraw,67,h-134,256,66);
            if(SceneLargeImage&&SceneLargeImage.texture)Fit(SceneLargeImage.rectTransform,new Rect(0,63,390,h-178),(float)SceneLargeImage.texture.width/SceneLargeImage.texture.height);
            Place(SceneViewerInfo.rectTransform,10,h-114,370,51);Place((RectTransform)SceneBackButton.transform,102,h-54,186,40);
            LayoutDrawPopup(h);
        }
    }
}
