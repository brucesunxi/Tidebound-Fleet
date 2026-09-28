using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    public sealed partial class VisualSampleGallery
    {
        [Serializable] public sealed class SkinRecord
        { public string id,chineseName,name,group,resourcePath; public int rarity,requirement,rotation; public float aspect; public float[] uv; }
        [Serializable] public sealed class SkinManifest { public SkinRecord[] skins; }
        [Serializable] public sealed class SkinCardView
        {
            public RectTransform Root,Check,Lock; public RawImage Ship; public Button Button,Inspect;
            public Text Name,State,Grade; public SampleSlicedImage Strip; public SampleVectorGraphic Field,Outline;
        }
        public RectTransform SkinContent,SkinViewer,SkinViewerPanel,SkinEquipmentBase;
        public SkinCardView[] SkinCards;
        public RectTransform[] SkinHeadings;
        public Text[] SkinHeadingLabels;
        public Text SkinEquipment,SkinViewerTitle,SkinViewerInfo;
        public RawImage SkinLargeImage;
        public Button SkinBackButton;
        public int SkinPreviewLevel,SkinPreviewShareDays;
        private readonly HashSet<int> equippedSkins=new HashSet<int>{0};
        private SkinManifest skinManifest;
        public SkinRecord[] SkinRecords=>(skinManifest??(skinManifest=JsonUtility.FromJson<SkinManifest>(Resources.Load<TextAsset>(ArtRoot+"ShipSkins/skin_catalog").text))).skins;
        public int SkinEquippedCount=>productionService!=null?productionService.AppearanceEquipment.Count(x=>x!=null):equippedSkins.Count;
        public bool SkinViewerOpen=>SkinViewer&&SkinViewer.gameObject.activeSelf;
        public int[] SkinIndices(int group)=>SkinRecords.Select((s,i)=>new{s,i}).Where(v=>v.s.group==SceneGroups[group]).OrderBy(v=>v.s.requirement).ThenBy(v=>v.s.rarity).Select(v=>v.i).ToArray();
        public bool IsSkinPreviewOwned(int i)=>i>=0&&i<SkinRecords.Length&&(productionService!=null?productionService.OwnsAppearance(SkinIdentity(i)):(SkinRecords[i].group=="Initial"||SkinRecords[i].group=="Level"&&SkinRecords[i].requirement<=SkinPreviewLevel||SkinRecords[i].group=="Share"&&SkinRecords[i].requirement<=SkinPreviewShareDays));
        public bool IsSkinEquipped(int i)=>productionService!=null?productionService.AppearanceEquipment.Contains(SkinIdentity(i)):equippedSkins.Contains(i);
        private static Color GradeTop(int grade)=>new[]{new Color(.97f,.98f,1),new Color(.34f,.72f,.98f),new Color(.66f,.43f,.91f),new Color(1,.77f,.27f),new Color(.97f,.38f,.38f)}[grade];
        private static Color GradeBottom(int grade)=>new[]{new Color(.78f,.84f,.89f),new Color(.75f,.93f,1),new Color(.92f,.80f,1),new Color(1,.94f,.67f),new Color(1,.77f,.72f)}[grade];
        private string GradeName(int grade)=>(English?new[]{"White","Blue","Purple","Gold","Red"}:new[]{"白","蓝","紫","金","红"})[grade];
        public void SetSkinPreviewProgress(int level,int shareDays=0)
        {
            SkinPreviewLevel=Mathf.Max(0,level);SkinPreviewShareDays=Mathf.Max(0,shareDays);
            equippedSkins.RemoveWhere(i=>!IsSkinPreviewOwned(i));if(equippedSkins.Count==0)equippedSkins.Add(0);RefreshText();
        }
        private void BuildSkins()
        {
            SkinContent=Rect("ShipSkins_PreviewDataOnly",Viewport);SkinCards=new SkinCardView[SkinRecords.Length];
            for(var i=0;i<SkinCards.Length;i++)
            {
                var s=SkinRecords[i];var c=new SkinCardView();SkinCards[i]=c;c.Root=Rect(s.id,SkinContent);var face=Rect("Visual",c.Root);Fill(face);
                var frame=Art("IvoryFrame","Card_Ivory",face);Fill(frame.rectTransform);c.Button=Button(c.Root,face,frame);
                c.Field=Vector("RarityBackground",face,SampleVectorGraphic.Shape.Field,GradeTop(s.rarity));c.Field.Bottom=GradeBottom(s.rarity);c.Field.Radius=8;Place(c.Field.rectTransform,5,5,72,111);
                var uv=new Rect(s.uv[0],s.uv[1],s.uv[2],s.uv[3]);c.Ship=Raw("ProportionalShip",face,s.resourcePath.Substring("TideboundUI/".Length),uv);
                Fit(c.Ship.rectTransform,new Rect(10,19,62,93),s.aspect);
                // UV inversion keeps the original bytes intact and normalizes the two downward bows.
                if(s.rotation==180)c.Ship.uvRect=new Rect(uv.xMax,uv.yMax,-uv.width,-uv.height);
                var gradePlate=Vector("GradeLabelPlate",face,SampleVectorGraphic.Shape.Field,new Color(1,1,1,.80f));gradePlate.Bottom=new Color(1,1,1,.8f);gradePlate.Radius=4;Place(gradePlate.rectTransform,7,7,32,17);
                c.Grade=Label("RarityName",gradePlate.transform,"",11,Ink);Fill(c.Grade.rectTransform);
                c.Name=Label("SkinName",face,"",14,Ink);Place(c.Name.rectTransform,3,116,76,23);
                c.Strip=Art("OwnershipStrip","Capsule_Cream",face);Place(c.Strip.rectTransform,5,142,72,21);c.Strip.Border=new Vector4(10,10,10,10);
                c.State=Label("Ownership",c.Strip.transform,"",12,Ink);Fill(c.State.rectTransform);
                c.Lock=Vector("Lock",c.Strip.transform,SampleVectorGraphic.Shape.Lock,Ink).rectTransform;Place(c.Lock,3,3,14,15);
                c.Outline=Vector("EquippedOutline",face,SampleVectorGraphic.Shape.Outline,new Color(.31f,.75f,.16f));c.Outline.Radius=10;c.Outline.Stroke=2;Fill(c.Outline.rectTransform,1.5f);
                c.Check=Rect("EquippedBadge",face);Place(c.Check,57,2,23,23);var disk=Art("GreenBadge","Capsule_Cream",c.Check);Fill(disk.rectTransform);disk.Border=new Vector4(11,11,11,11);disk.color=new Color(.49f,.87f,.16f);Fill(Vector("Check",c.Check,SampleVectorGraphic.Shape.Check,Color.white).rectTransform,2);
                var inspect=Rect("InspectArtwork",face);Place(inspect,53,87,28,28);var inspectFace=Rect("Visual",inspect);Fill(inspectFace);var inspectArt=Art("BlueBadge","Capsule_Cream",inspectFace);Fill(inspectArt.rectTransform);inspectArt.Border=new Vector4(13,13,13,13);inspectArt.color=new Color(.3f,.73f,1);c.Inspect=Button(inspect,inspectFace,inspectArt);Fill(Vector("Magnifier",inspectFace,SampleVectorGraphic.Shape.Magnifier,Color.white).rectTransform,4);
            }
            SkinHeadings=new RectTransform[4];SkinHeadingLabels=new Text[4];
            for(var i=0;i<4;i++){var r=Rect("SkinSource_"+SceneGroups[i],SkinContent);SkinHeadings[i]=r;var rule=r.gameObject.AddComponent<ShowcaseSectionRule>();rule.color=Brown;rule.raycastTarget=false;SkinHeadingLabels[i]=Label("Caption",r,"",20,Brown);}
            SkinEquipmentBase=Art("EquipmentCapsule","Capsule_Cream",Popup).rectTransform;Place(SkinEquipmentBase,26,141,136,31);
            SkinEquipment=Label("PreviewEquipmentCount",SkinEquipmentBase,"",15,Brown);Fill(SkinEquipment.rectTransform);
            SkinViewer=Rect("SkinInspection_OnlyPreview",PreviewCanvas.transform);Fill(SkinViewer);var shade=SkinViewer.gameObject.AddComponent<Image>();shade.color=new Color(.015f,.055f,.09f,.88f);shade.raycastTarget=true;
            SkinViewerPanel=Art("Frame","Panel_Gold",SkinViewer).rectTransform;
            SkinViewerTitle=Label("SkinTitle",SkinViewer,"",23,Brown);
            SkinLargeImage=Rect("OriginalShipArtwork",SkinViewer).gameObject.AddComponent<RawImage>();SkinLargeImage.raycastTarget=false;
            SkinViewerInfo=Label("RarityAndSource",SkinViewer,"",16,Ink);
            var back=Rect("BackToSkins",SkinViewer);var backFace=Rect("Visual",back);Fill(backFace);var backArt=Art("Cream","Capsule_Cream",backFace);Fill(backArt.rectTransform);SkinBackButton=Button(back,backFace,backArt);Fill(Label("Label",backFace,"返回皮肤",18,Brown).rectTransform);
            SkinViewer.gameObject.SetActive(false);SkinContent.gameObject.SetActive(false);
        }
        private void WireSkins()
        {
            for(var i=0;i<SkinCards.Length;i++){var n=i;SkinCards[i].Button.onClick.RemoveAllListeners();SkinCards[i].Button.onClick.AddListener(()=>ToggleSkin(n));SkinCards[i].Inspect.onClick.RemoveAllListeners();SkinCards[i].Inspect.onClick.AddListener(()=>InspectSkin(n));}
            SkinBackButton.onClick.RemoveAllListeners();SkinBackButton.onClick.AddListener(()=>SkinViewer.gameObject.SetActive(false));
        }
        public void ToggleSkin(int index)
        {
            if(index<0||index>=SkinCards.Length)return;
            if(productionService!=null){if(!IsSkinPreviewOwned(index)){InspectSkin(index);return;}SelectProductionAppearance(SkinIdentity(index));return;}
            if(!IsSkinPreviewOwned(index)){InspectSkin(index);return;}
            if(equippedSkins.Contains(index))equippedSkins.Remove(index);
            else{if(equippedSkins.Count==5){Hint.text=English?"5 slots full. Unequip one first.":"已装备 5 款，请先卸下一款";return;}equippedSkins.Add(index);}
            RefreshText();
        }
        public string SkinRequirement(int index)
        {
            var s=SkinRecords[index];if(s.group=="Initial")return English?"Initially owned":"初始拥有";
            if(s.group=="Level")return English?"Clear level "+s.requirement:"通关第 "+s.requirement+" 关获取";
            if(s.group=="Share")return English?"Share task on "+s.requirement+" day(s)":"累计 "+s.requirement+" 天完成有效分享任务";
            return English?"Ship skin draw reward":"船体皮肤抽奖获取";
        }
        public void InspectSkin(int index)
        {
            if(index<0||index>=SkinCards.Length)return;
            var s=SkinRecords[index];SkinViewer.gameObject.SetActive(true);
            SkinLargeImage.texture=SkinCards[index].Ship.texture;SkinLargeImage.uvRect=SkinCards[index].Ship.uvRect;
            SkinViewerTitle.text=English?s.name:s.chineseName;
            SkinViewerInfo.text=GradeName(s.rarity)+(English?" rarity":"色品质")+" · "+SkinRequirement(index)+"\n"+(English?"Preview only · does not unlock":"原图检视 · 不解锁、不消耗");
            LayoutSkins(Screen.height*390f/Screen.width);
            InspectProductionExchange(index);
        }
        public void ScrollSkinGroup(int group)
        {Scroll.StopMovement();var range=Mathf.Max(1,SkinContent.rect.height-Viewport.rect.height);Scroll.verticalNormalizedPosition=1-Mathf.Clamp(-SkinHeadings[group].anchoredPosition.y,0,range)/range;}
        private void RefreshSkins()
        {
            if(SkinCards==null)return;var names=English?new[]{"Initially Owned","Level Rewards","Share Rewards","Draw Rewards"}:new[]{"初始拥有","通关获取","分享获取","抽奖获取"};
            for(var i=0;i<4;i++){var t=SkinHeadingLabels[i];t.text=names[i];t.fontSize=English?18:20;var w=English?126:100;Place(t.rectTransform,(346-w)/2,0,w,32);var rule=SkinHeadings[i].GetComponent<ShowcaseSectionRule>();rule.CaptionWidth=w;rule.SetAllDirty();}
            for(var i=0;i<SkinCards.Length;i++)
            {
                var c=SkinCards[i];var s=SkinRecords[i];var owned=IsSkinPreviewOwned(i);var equipped=IsSkinEquipped(i);
                c.Name.text=English?s.name:s.chineseName;c.Name.fontSize=English?12:14;Place(c.Name.rectTransform,3,116,76,25);
                c.Grade.text=GradeName(s.rarity);c.Grade.fontSize=English?10:11;
                c.State.text=equipped?(English?"Equipped":"已装备"):owned?(English?"Owned":"已拥有"):(English?"Locked":"未解锁");c.State.fontSize=English?11:12;
                Place(c.State.rectTransform,owned?0:14,0,owned?72:57,21);c.Strip.color=equipped?new Color(.58f,.94f,.23f):owned?new Color(.62f,.87f,1):new Color(.78f,.82f,.84f);
                c.Lock.gameObject.SetActive(!owned);c.Check.gameObject.SetActive(equipped);c.Outline.gameObject.SetActive(equipped);
                c.Ship.material=!owned&&!InspectArtwork?sceneLocked:null;
            }
            SkinEquipmentBase.gameObject.SetActive(SelectedTab==0);
            Place((RectTransform)Statistics.transform.parent,SelectedTab==0?202:97,142,SelectedTab==0?162:196,31);if(SelectedTab!=0)return;
            SkinEquipment.text=(English?"Equipped ":"已装备 ")+equippedSkins.Count+"/5";
            Statistics.text=(English?"Collected ":"已收集 ")+Enumerable.Range(0,SkinCards.Length).Count(IsSkinPreviewOwned)+"/"+SkinCards.Length;
            Hint.text=equippedSkins.Count==0?(English?"No skins equipped · default used next game":"装备为空 · 下局自动使用默认白船"):(English?"Up to 5 skins · tap magnifier to inspect":"最多装备 5 款 · 点击放大镜查看");Hint.fontSize=English?13:14;
            Stamp.text=InspectArtwork?"Unity 皮肤素材检视 · 彩色不代表拥有":"Unity 船体皮肤小样 · 仅预览数据";
            SkinBackButton.GetComponentInChildren<Text>().text=English?"Back to Skins":"返回皮肤";
        }
        private void LayoutSkins(float h)
        {
            if(!SkinContent)return;var top=0f;
            for(var group=0;group<4;group++){Place(SkinHeadings[group],0,top,346,32);top+=38;var indices=SkinIndices(group);for(var j=0;j<indices.Length;j++)Place(SkinCards[indices[j]].Root,j%4*88,top+j/4*175,82,168);top+=((indices.Length+3)/4)*175+12;}
            SkinContent.sizeDelta=new Vector2(346,top-7);
            var panelHeight=Mathf.Min(570,h-100);var y=(h-panelHeight)/2;Place(SkinViewerPanel,36,y,318,panelHeight);Place(SkinViewerTitle.rectTransform,55,y+17,280,36);
            if(SkinLargeImage.texture)Fit(SkinLargeImage.rectTransform,new Rect(95,y+62,200,panelHeight-190),Mathf.Abs(SkinLargeImage.texture.width*SkinLargeImage.uvRect.width/(SkinLargeImage.texture.height*SkinLargeImage.uvRect.height)));
            Place(SkinViewerInfo.rectTransform,50,y+panelHeight-119,290,61);Place((RectTransform)SkinBackButton.transform,104,y+panelHeight-53,182,37);
        }
    }
}
