using System;
using System.Linq;
using Tidebound.Save;
using Tidebound.Collection;
using UnityEngine;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Approved borderless celebration, projected exclusively from a durable victory receipt.</summary>
    public sealed class VictoryResultPanel : MonoBehaviour
    {
        private Text title,subtitle,first,battle,total,target,shipName,starAward,starTotal,teaserTitle,shareText;
        private RectTransform flagship,header,teaser,track,fill,starBadge,shareOverlay,shareBody;
        private RawImage coin;
        private HarborVictoryGraphic awardIcon,totalIcon,effects;
        private readonly Button[] rewardButtons=new Button[3];
        private readonly RawImage[] rewardImages=new RawImage[3];
        private readonly Text[] rewardCounts=new Text[3],rewardStars=new Text[3],rewardDistances=new Text[3];
        private readonly RectTransform[] rewardDots=new RectTransform[3];
        private readonly HarborVictoryGraphic[] rewardStarIcons=new HarborVictoryGraphic[3];
        private Button next,back,retry,motion,share,shareClose;
        private CollectionShipPreview preview;
        private VictoryResult result;
        private bool overview,homeNavigation,reducedMotion,readableResult,focused=true,applicationPaused;
        private string showcaseId=ShowcaseCatalog.DefaultId,notice;
        private float phase,scale=1,height=844;
        public bool IsOverview=>overview;
        public bool IsSharePreviewOpen=>shareOverlay!=null&&shareOverlay.gameObject.activeSelf;
        public int DisplayedStars {get;private set;}
        public float CelebrationTime=>phase;
        public int VisibleRewardCount=>result?.NextRewards.Count??0;
        public bool CanAnimate=>readableResult&&!reducedMotion&&focused&&!applicationPaused&&!IsSharePreviewOpen;
        private static string L(string english,string chinese)=>UILanguage.IsChinese?chinese:english;
        public void SetShowcase(string id)
        {
            var definition=ShowcaseCatalog.Find(id);if(definition==null)return;
            showcaseId=id;shipName.text=UILanguage.IsChinese?definition.ChineseName:definition.Name;
            if(preview.DisplayedShowcaseId!=id)preview.PresentShowcase(id);
        }
        public void Initialize(Font value,Action proceed,Action returnOrResult,Action retrySave,Action toggleMotion,bool homeNavigation=false)
        {
            this.homeNavigation=homeNavigation;
            effects=HarborUI.Rect("Celebration",transform).gameObject.AddComponent<HarborVictoryGraphic>();effects.Celebration=true;effects.raycastTarget=false;
            header=HarborUI.Header("ResultHeader",transform);title=Label("ResultTitle",30);title.text="VICTORY!";
            subtitle=Label("ResultSubtitle",14);first=Label("FirstClear",11);battle=Label("BattleCoins",11);battle.gameObject.SetActive(false);
            awardIcon=Star("AwardStar",transform);starAward=Label("StarAward",29);
            coin=GameplayArt.Image("CoinIcon",transform,"Coin_Anchor");total=Label("TotalCoins",27);
            flagship=HarborUI.Rect("Flagship",transform);preview=flagship.gameObject.AddComponent<CollectionShipPreview>();
            preview.IllustrationFill=.96f;preview.IllustrationCenterY=.5f;preview.Initialize(true);preview.UseCelebrationMotion();preview.AllowMotion=()=>CanAnimate;
            shipName=Label("ShipName",16);
            starBadge=Flat("StarCountBadge",transform,new Color(.04f,.2f,.25f,.8f),17);totalIcon=Star("TotalStar",starBadge);starTotal=Label("TotalStars",24,starBadge);
            teaser=HarborUI.Rect("RewardPreview",transform);teaserTitle=Label("Title",14,teaser);
            track=Flat("ProgressTrack",teaser,new Color(.02f,.12f,.18f,.9f),7);fill=Flat("ProgressFill",track,new Color(1,.78f,.14f),5);
            for(var i=0;i<3;i++)
            {
                var index=i;var root=HarborUI.Rect("Reward"+i,teaser);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
                rewardButtons[i]=root.gameObject.AddComponent<Button>();rewardButtons[i].targetGraphic=hit;rewardButtons[i].transition=Selectable.Transition.None;rewardButtons[i].onClick.AddListener(()=>ShowReward(index));
                rewardImages[i]=HarborUI.Art("Art",root,"");rewardCounts[i]=Label("OtherRewards",10,root);
                rewardDots[i]=Flat("Node",root,new Color(1,.87f,.43f),7);rewardStarIcons[i]=Star("Star",root);rewardStars[i]=Label("Threshold",14,root);rewardDistances[i]=Label("Distance",11,root);rewardDistances[i].color=new Color(.69f,.84f,.87f);
            }
            target=Label("NextTarget",12);target.color=new Color(.73f,.86f,.88f);
            next=HarborVictoryButton.Create("NextLevel",transform,"Next level",new Color(1,.78f,.14f),null,()=>{if(!IsSharePreviewOpen)proceed?.Invoke();});
            back=HarborVictoryButton.Create("BackOrResult",transform,"Return home",new Color(.17f,.68f,.88f),SettingsIconKind.Home,()=>{if(!IsSharePreviewOpen)returnOrResult?.Invoke();});
            share=HarborVictoryButton.Create("Share",transform,"Share",new Color(.18f,.74f,.34f),SettingsIconKind.Share,ShowSharePreview);
            retry=HarborVictoryButton.Create("RetryResultSave",transform,"Retry saving rewards",HarborUI.Gold,null,retrySave);retry.gameObject.SetActive(false);
            motion=HarborUI.Control("ResultMotion",transform,"Motion",toggleMotion);motion.gameObject.SetActive(false);
            shareOverlay=HarborUI.Rect("SharePreview",transform);var shade=shareOverlay.gameObject.AddComponent<Image>();shade.color=new Color(.015f,.09f,.13f,.87f);
            shareBody=HarborUI.Popup("Body",shareOverlay);shareText=HarborUI.Label("PreviewText",shareOverlay,"",17);
            shareClose=HarborVictoryButton.Create("ClosePreview",shareOverlay,"Back",HarborUI.Gold,null,CloseSharePreview);shareOverlay.gameObject.SetActive(false);
            UILanguage.Changed+=RefreshLanguage;SetShowcase(showcaseId);
        }
        private void RefreshLanguage(){SetShowcase(showcaseId);PaintLabels();if(result!=null)BindRewards();Layout(new Rect(((RectTransform)transform).anchoredPosition,((RectTransform)transform).rect.size));}
        public void Present(VictoryResult value,bool showOverview,bool readable,float progress,bool reduced)
        {
            if(result?.AttemptId!=value.AttemptId){result=value;phase=0;notice=null;BindRewards();}
            effects.ShadeOnly=false;overview=showOverview;readableResult=readable;reducedMotion=reduced;
            foreach(var item in new GameObject[]{coin.gameObject,total.gameObject,first.gameObject,awardIcon.gameObject,starAward.gameObject,starBadge.gameObject,teaser.gameObject,shipName.gameObject})item.SetActive(true);
            flagship.gameObject.SetActive(true);effects.gameObject.SetActive(true);next.gameObject.SetActive(true);back.gameObject.SetActive(true);share.gameObject.SetActive(true);retry.gameObject.SetActive(false);
            // No invented next level after the installed campaign ends.
            next.interactable=readable&&value.HasNext;back.interactable=share.interactable=readable;
            foreach(var button in rewardButtons)button.interactable=readable;
            if(reduced)phase=Mathf.Max(phase,4);PaintLabels();PaintAnimation();
        }
        private void PaintLabels()
        {
            if(result==null||!readableResult)return;
            title.text="VICTORY!";var unlocked=result.UnlockedRewards?.Rewards.Count??0;
            subtitle.text=notice??(L("Level "+result.LevelNumber+" cleared","第 "+result.LevelNumber+" 关完成")+(unlocked>0?L(" · "+unlocked+" rewards unlocked"," · 解锁 "+unlocked+" 项外观"):""));
            starAward.text="+"+result.AwardedStars;total.text="+"+result.TotalCoins;
            first.text=L("First clear +"+result.FirstClearCoins+" · Battle +"+result.BattleCoins,"首通 +"+result.FirstClearCoins+" · 战斗 +"+result.BattleCoins);
            first.gameObject.SetActive(height>=770);
            teaserTitle.text=result.NextRewards.Count>0?L("Next voyage rewards","下一站 · 通关奖励"):L("All available rewards collected","已收集所有已开放通关奖励");
            target.text=result.HasNext?"":"All available levels complete";target.gameObject.SetActive(!result.HasNext);
            next.GetComponentInChildren<Text>(true).text=result.HasNext?"Next level":"Content complete";
            back.GetComponentInChildren<Text>(true).text=homeNavigation?"Return home":overview?"View result":"Return to voyage";
        }
        private void BindRewards()
        {
            if(result==null)return;track.gameObject.SetActive(result.NextRewards.Count>0);
            for(var i=0;i<3;i++)
            {
                var show=i<result.NextRewards.Count;rewardButtons[i].gameObject.SetActive(show);if(!show)continue;
                var node=result.NextRewards[i];var primary=node.Primary;var art=rewardImages[i];
                if(primary.Kind==ClearRewardKind.Showcase){art.texture=ShowcaseArt.Load(primary.Id);art.uvRect=ShowcaseArt.VisibleUV(primary.Id);}
                else if(primary.Kind==ClearRewardKind.Skin)
                {
                    var code=primary.Id.Substring(primary.Id.Length-3);GameplayArt.Set(art,"Side/"+code);
                    if(art.texture==null){art.texture=Resources.Load<Texture2D>("TideboundUI/VisualSamples/ShipSkins/"+code);art.uvRect=HarborAppearanceArt.UV(HarborAppearanceArt.Skin(primary.Id).uv);}
                }
                else if(primary.Kind==ClearRewardKind.Scene){art.texture=HarborAppearanceArt.SceneTexture(primary.Id);art.uvRect=new Rect(0,0,1,1);}
                else {var trail=HarborAppearanceArt.Trail(primary.Id);art.texture=Resources.Load<Texture2D>(trail.resourcePath);art.uvRect=HarborAppearanceArt.UV(trail.uv);}
                rewardCounts[i].text=node.Rewards.Count>1?"+"+(node.Rewards.Count-1):"";
                rewardStars[i].text=L(node.Stars+" stars",node.Stars+" 星");rewardDistances[i].text=L((node.Stars-result.TotalStars)+" to go","还差 "+(node.Stars-result.TotalStars)+" 星");
            }
            LayoutRewards();
        }
        public void ShowSharePreview()
        {
            if(!readableResult)return;
            shareText.text=L("Level "+result.LevelNumber+" cleared\n"+result.TotalStars+" stars\n\nSystem sharing is coming later. No extra rewards.","第 "+result.LevelNumber+" 关完成\n已收集 "+result.TotalStars+" 颗星星\n\n分享功能接入后开放，暂不附加奖励。");
            shareOverlay.gameObject.SetActive(true);HarborUI.Focus(shareClose);
        }
        private void ShowReward(int index)
        {
            if(!readableResult||index>=result.NextRewards.Count)return;var node=result.NextRewards[index];
            shareText.text=L(node.Stars+" stars · "+(node.Stars-result.TotalStars)+" to go",node.Stars+" 星 · 还差 "+(node.Stars-result.TotalStars)+" 星")+"\n\n"+string.Join("\n",node.Rewards.Select(r=>(UILanguage.IsChinese?r.ChineseName:r.Name)+" · "+L(r.Kind.ToString(),r.Kind==ClearRewardKind.Showcase?"形象":r.Kind==ClearRewardKind.Skin?"船皮":r.Kind==ClearRewardKind.Scene?"场景":"拖尾")));
            shareOverlay.gameObject.SetActive(true);HarborUI.Focus(shareClose);
        }
        public void CloseSharePreview(){shareOverlay.gameObject.SetActive(false);HarborUI.Focus(share);}
        public void Pending(bool failed)
        {
            readableResult=false;overview=false;effects.ShadeOnly=true;effects.SetVerticesDirty();effects.gameObject.SetActive(true);title.text="VICTORY";subtitle.text=failed?"Reward save failed. Please retry.":"Saving your rewards...";
            foreach(var item in new GameObject[]{total.gameObject,first.gameObject,battle.gameObject,coin.gameObject,awardIcon.gameObject,starAward.gameObject,starBadge.gameObject,teaser.gameObject,shareOverlay.gameObject,shipName.gameObject,next.gameObject,back.gameObject,share.gameObject,motion.gameObject,target.gameObject})item.SetActive(false);
            flagship.gameObject.SetActive(true);retry.gameObject.SetActive(failed);
        }
        public void Practice()
        {Pending(false);title.text="Practice complete";subtitle.text="Account unavailable - no rewards saved";next.gameObject.SetActive(true);next.interactable=true;next.GetComponentInChildren<Text>(true).text="Replay practice";back.gameObject.SetActive(homeNavigation);back.interactable=true;}
        public void SetNotice(string value){notice=value;subtitle.text=value;}
        public void FinishCelebration(){phase=Mathf.Max(4,phase);PaintAnimation();}
        private void Update(){if(CanAnimate)phase+=Time.unscaledDeltaTime;PaintAnimation();}
        private void PaintAnimation()
        {
            if(effects==null||result==null)return;
            effects.Phase=reducedMotion?4:phase;effects.FlyStar=!reducedMotion;effects.SetVerticesDirty();
            DisplayedStars=phase>=1.2f||reducedMotion?result.TotalStars:result.PreviousStars;starTotal.text=DisplayedStars.ToString();
            var t=Mathf.Clamp01((phase-1)/.8f);var width=4+64*result.NextRewardProgress*(1-Mathf.Pow(1-t,3));
            HarborUI.Place(fill,new Rect(2*scale,2*scale,width*scale,10*scale));
        }
        private void OnApplicationFocus(bool value){focused=value;}
        private void OnApplicationPause(bool value){applicationPaused=value;}
        private void OnDisable(){if(shareOverlay!=null)shareOverlay.gameObject.SetActive(false);}
        private void OnDestroy(){UILanguage.Changed-=RefreshLanguage;}
        public void Layout(Rect area)
        {
            HarborUI.Place((RectTransform)transform,area);scale=area.width/390f;height=area.height/scale;var compact=height<770;var top=compact?38:78;
            Top(header,new Rect(12,top+4,366,122));Top(title.rectTransform,new Rect(48,top+61,294,47));Top(subtitle.rectTransform,new Rect(12,top+121,366,30));
            var ry=top+171;Top(awardIcon.rectTransform,new Rect(91,ry-20,40,40));Top(starAward.rectTransform,new Rect(129,ry-24,54,48));Top(coin.rectTransform,new Rect(211,ry-20,42,42));Top(total.rectTransform,new Rect(254,ry-24,97,48));Top(first.rectTransform,new Rect(152,ry+16,218,23));Top(battle.rectTransform,new Rect(0,0,0,0));
            var heroTop=top+(compact?193:211);var nameY=height-286;var hero=Mathf.Min(282,nameY-14-heroTop);
            Top(flagship,new Rect((390-hero)/2,heroTop,hero,hero));Top(shipName.rectTransform,new Rect(55,nameY-14,280,28));
            Top(starBadge,new Rect(153,height-268,84,34));Local(totalIcon.rectTransform,new Rect(8,3,29,29));Local(starTotal.rectTransform,new Rect(37,1,44,32));
            Top(teaser,new Rect(0,height-226,390,148));LayoutRewards();Top(target.rectTransform,new Rect(32,height-143,326,42));
            var by=height-73;Top((RectTransform)back.transform,new Rect(23,by,70,53));Top((RectTransform)next.transform,new Rect(105,by,180,53));Top((RectTransform)share.transform,new Rect(297,by,70,53));
            Top((RectTransform)retry.transform,new Rect(44,height-144,302,56));Top((RectTransform)motion.transform,new Rect(42,height-47,306,36));
            Top(effects.rectTransform,new Rect(0,0,390,height));effects.DesignHeight=height;effects.TitleTop=top;effects.CountY=height-251;
            var canvas=GetComponentInParent<Canvas>();var full=canvas==null?area.size:((RectTransform)canvas.rootCanvas.transform).rect.size;
            effects.ShadeBounds=new Rect(-area.position,full);
            Top(shareOverlay,new Rect(0,0,390,height));Top(shareBody,new Rect(20,height/2-165,350,330));Top(shareText.rectTransform,new Rect(42,height/2-142,306,205));Top((RectTransform)shareClose.transform,new Rect(75,height/2+82,240,52));
            FontSize(title,30);FontSize(subtitle,UILanguage.IsChinese?14:12);FontSize(first,11);FontSize(total,27);FontSize(starAward,29);FontSize(shipName,16);FontSize(starTotal,24);FontSize(target,12);
            FontSize(next.GetComponentInChildren<Text>(true),UILanguage.IsChinese?25:22);FontSize(back.GetComponentInChildren<Text>(true),UILanguage.IsChinese?12:10);FontSize(share.GetComponentInChildren<Text>(true),12);
            PaintAnimation();
        }
        private void LayoutRewards()
        {
            if(teaserTitle==null)return;Local(teaserTitle.rectTransform,new Rect(15,124,360,24));FontSize(teaserTitle,UILanguage.IsChinese?14:13);
            Local(track,new Rect(28,44,298,14));
            for(var i=0;i<3;i++)
            {
                var x=96+i*114;Local((RectTransform)rewardButtons[i].transform,new Rect(x-43,0,86,123));
                var art=rewardImages[i];var aspect=art.texture==null?1:art.texture.width*art.uvRect.width/(art.texture.height*art.uvRect.height);
                GameplayArt.Fit(art,new Rect(7*scale,63*scale,72*scale,60*scale),aspect);
                Local(rewardCounts[i].rectTransform,new Rect(63,99,24,20));Local(rewardDots[i],new Rect(36,43,14,14));
                Local(rewardStarIcons[i].rectTransform,new Rect(20,15,17,17));Local(rewardStars[i].rectTransform,new Rect(33,11,55,26));Local(rewardDistances[i].rectTransform,new Rect(-4,-1,94,22));
                FontSize(rewardCounts[i],10);FontSize(rewardStars[i],UILanguage.IsChinese?14:11);FontSize(rewardDistances[i],11);
            }
        }
        private void Top(RectTransform r,Rect area)=>HarborUI.Place(r,new Rect(area.x*scale,(height-area.y-area.height)*scale,area.width*scale,area.height*scale));
        private void Local(RectTransform r,Rect area)=>HarborUI.Place(r,new Rect(area.x*scale,area.y*scale,area.width*scale,area.height*scale));
        private void FontSize(Text t,int size){t.fontSize=Mathf.RoundToInt(size*scale);}
        private Text Label(string name,int size,Transform parent=null)
        {var t=HarborApprovedArt.Text(name,parent??transform,"",size,new Color(1,.96f,.82f));var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.02f,.11f,.15f,.9f);shadow.effectDistance=new Vector2(0,-1);return t;}
        private static HarborVictoryGraphic Star(string name,Transform parent)
        {var s=HarborUI.Rect(name,parent).gameObject.AddComponent<HarborVictoryGraphic>();s.raycastTarget=false;return s;}
        private static RectTransform Flat(string name,Transform parent,Color color,float radius)
        {var image=HarborUI.Surface(name,parent,color);var shape=(HarborImage)image;shape.Radius=radius;shape.Border=0;shape.Depth=0;image.raycastTarget=false;return image.rectTransform;}
    }
}
