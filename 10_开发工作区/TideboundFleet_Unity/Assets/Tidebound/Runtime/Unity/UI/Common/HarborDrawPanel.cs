using System;
using System.Linq;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Unity.UI.VisualSamples;
using UnityEngine;
using UnityEngine.UI;
using C=Tidebound.Unity.UI.HarborCommerceUI;
namespace Tidebound.Unity.UI
{
    /// <summary>Single-draw presentation bound to atomic, replayable save transactions.</summary>
    public sealed class HarborDrawPanel:MonoBehaviour
    {
        private PlayerSaveService service;private Action<int> backToCollection;private Action close,recharge;
        private RectTransform panel,header,viewport,content;private HarborSliceGraphic panelArt;private ShowcaseSectionRule titleRule;private Text wallet,poolTitle,message;private Button closeButton,plus,draw,back,resultUse;private Button[] tabs;
        private float w,h;private string page="main",focusId,request;private UniqueDrawQuote lockedQuote;private UniqueDrawReceipt reward;
        private HarborHeroButton drawPresentation;
        public DrawPool Pool{get;private set;}
        public string Page=>page;
        public UniqueDrawStatus? LastResult{get;private set;}
        public void SetRecharge(Action value){recharge=value;}
        public void Initialize(PlayerSaveService save,Action<int> collection,Action onClose)
        {
            service=save;backToCollection=collection;close=onClose;HarborUI.Fill((RectTransform)transform);
            var shade=gameObject.AddComponent<Image>();shade.color=new Color(.025f,.05f,.08f,.58f);shade.raycastTarget=true;
            panel=HarborUI.Rect("Panel",transform);panelArt=C.Plate("GoldFrame",panel,"Panel_Gold");panelArt.Border=new Vector4(22,22,22,22);
            header=C.Header(panel,C.L("抽奖","Draw"));closeButton=C.Button("Close",panel,"",()=>close?.Invoke(),"Close_Coral");
            var capsule=C.Plate("Wallet",panel,"Capsule_Cream");wallet=C.Label("Balance",capsule.transform,"",22);HarborUI.Fill(wallet.rectTransform);var coin=C.Plate("Coin",capsule.transform,"Coin_Anchor");C.P(coin,4,3,34,34);
            plus=C.Button("Recharge",panel,"+",()=>recharge?.Invoke(),"CTA_Gold",24);
            tabs=new Button[4];for(var i=0;i<4;i++){var n=i;tabs[i]=C.Button("Pool_"+i,panel,"",()=>SelectPool((DrawPool)n),"Tab_Brown",16);}
            titleRule=HarborUI.Rect("PoolHeadingRule",panel).gameObject.AddComponent<ShowcaseSectionRule>();titleRule.color=HarborApprovedArt.Brown;titleRule.raycastTarget=false;poolTitle=C.Label("PoolTitle",panel,"",19);
            viewport=C.Scroll("PoolScroll",panel,out content);message=C.Label("Status",panel,"",13);
            draw=C.Button("DrawOnce",panel,"",BeginDraw,"CTA_Gold",20);resultUse=C.Button("UseReward",panel,"",UseReward,"CTA_Gold",16);resultUse.gameObject.SetActive(false);back=C.Button("BackToCollection",panel,C.L("返回收藏","Back to collection"),()=>backToCollection?.Invoke((int)Pool),"Capsule_Cream",16);
            drawPresentation=HarborHeroButton.Apply(draw,HarborHeroButton.Theme.Draw);
            var owner=GetComponentInParent<Tidebound.Unity.LevelDesign.PortraitPuzzleGraybox>();
            drawPresentation.AllowMotion=()=>!owner||!owner.ReducedUIMotion;
        }
        public void Open(int category){gameObject.SetActive(true);SelectPool((DrawPool)Mathf.Clamp(category,0,3));Layout();}
        public void SelectPool(DrawPool pool){Pool=pool;page="main";focusId=null;reward=null;request=null;LastResult=null;Render();}
        private string PoolName=>(UILanguage.IsChinese?new[]{"船只皮肤奖池","拖尾奖池","主页形象奖池","场景奖池"}:new[]{"Ship Skin Pool","Trail Pool","Showcase Pool","Scene Pool"})[(int)Pool];
        private UniqueDrawQuote Quote=>service.DrawQuote(Pool);
        private string Name(UniqueDrawItem item)=>UILanguage.IsChinese?item.ChineseName:item.Name;
        private bool Owned(UniqueDrawItem item)=>item.Pool==DrawPool.Showcase?service.OwnsShowcase(item.Id):service.OwnsAppearance(item.Id);
        private string Availability(UniqueDrawItem item)
        {
            if(Owned(item))return C.L("已拥有","Owned");
            return UniqueDrawEngine.OpensAt(Pool,item.Rarity)>Quote.EligibilityOrdinal?C.L("第","Draw ")+UniqueDrawEngine.OpensAt(Pool,item.Rarity)+C.L("抽开放"," unlock"):C.L("可抽取","Available");
        }
        public void BeginDraw()
        {
            if(page!="main"||!service.IsAvailable||service.CurrentLevel<3||Pool==DrawPool.Scene)return;
            var gift=service.CanClaimFirstBlue;
            if(!gift&&Quote.Eligible.Length==0)return;
            // Keep an unsuccessful save's request so a retry cannot create a second purchase.
            if(request==null){lockedQuote=Quote;request=Guid.NewGuid().ToString("N");}
            if(!gift&&service.Coins<lockedQuote.Price)
            {LastResult=UniqueDrawStatus.InsufficientCoins;request=null;Render();recharge?.Invoke();return;}
            page="drawing";draw.interactable=false;
            if(gift)
            {
                var result=service.CollectAppearance(request,"FirstBlue");
                if(result==CollectionStatus.Saved||result==CollectionStatus.AlreadySaved)
                {var receipt=service.AppearanceReceiptFor(request);reward=new UniqueDrawReceipt{ItemId=receipt.SkinIds[0],Pool=DrawPool.Skin,RequestId=request};Pool=DrawPool.Skin;page="result";}
                else {page="main";Render();message.text=C.L("赠礼未领取，请重试","Gift not claimed. Please retry.");return;}
            }
            else
            {
                LastResult=service.DrawUnique(request,Pool,lockedQuote.PaidCount,lockedQuote.Price);
                if(LastResult==UniqueDrawStatus.Saved||LastResult==UniqueDrawStatus.AlreadySaved)
                {reward=service.UniqueReceiptFor(request);page="result";}
                else
                {
                    page="main";
                    if(LastResult!=UniqueDrawStatus.StorageUnavailable)request=null;
                }
            }
            Render();
            if(LastResult==UniqueDrawStatus.InsufficientCoins)recharge?.Invoke();
        }
        public void ReturnToPool(){page="main";request=null;reward=null;LastResult=null;Render();}
        public void ShowAll(){page="all";Render();}
        public void UseReward()
        {
            if(reward==null)return;if(reward.Pool==DrawPool.Skin){backToCollection?.Invoke(0);return;}
            var success=false;
            if(reward.Pool==DrawPool.Showcase){var result=service.SelectShowcase(reward.ItemId);success=result==ShowcaseSelectionStatus.Saved||result==ShowcaseSelectionStatus.AlreadySelected;}
            else {var result=service.SelectAppearance(reward.ItemId);success=result==AppearanceSelectionStatus.Saved||result==AppearanceSelectionStatus.AlreadySelected;}
            message.text=success?C.L("已使用","Equipped"):C.L("设置未完成，请重试","Could not save selection. Retry.");
        }
        private void Render()
        {
            if(!content)return;C.Clear(content);content.anchoredPosition=Vector2.zero;wallet.text=service.IsAvailable?service.Coins.ToString("N0"):"—";
            header.GetComponentInChildren<Text>().text=page=="result"?C.L("获得奖励","Reward"):C.L("抽奖","Draw");
            var names=UILanguage.IsChinese?new[]{"皮肤","拖尾","形象","场景"}:new[]{"Skins","Trails","Showcase","Scenes"};
            for(var i=0;i<4;i++){C.Caption(tabs[i],names[i]);HarborApprovedArt.Set(tabs[i].GetComponentInChildren<HarborSliceGraphic>(),i==(int)Pool?"Tab_Gold":"Tab_Brown");tabs[i].GetComponentInChildren<Text>().color=i==(int)Pool?HarborApprovedArt.Brown:Color.white;tabs[i].gameObject.SetActive(page!="result");}
            poolTitle.text=page=="all"?C.L("奖池一览","All rewards"):page=="result"?C.L("已加入收藏","Added to collection"):PoolName;
            wallet.transform.parent.gameObject.SetActive(page!="result");plus.gameObject.SetActive(page!="result");draw.gameObject.SetActive(page=="main"&&Pool!=DrawPool.Scene);resultUse.gameObject.SetActive(page=="result");
            C.Caption(back,page=="main"?C.L("返回收藏","Back to collection"):C.L("返回奖池","Back to pool"));back.onClick.RemoveAllListeners();back.onClick.AddListener(()=>{if(page=="main")backToCollection?.Invoke((int)Pool);else ReturnToPool();});
            var cw=Mathf.Max(290,w-30);message.text="";
            if(page=="main")RenderMain(cw);else if(page=="all")RenderAll(cw);else RenderResult(cw);
            LayoutContent();
        }
        private void RenderMain(float cw)
        {
            content.sizeDelta=new Vector2(cw,425);var quote=Quote;
            var items=UniqueDrawEngine.Items.Where(x=>x.Pool==Pool).ToArray();var desired=Pool==DrawPool.Skin?"TF_SKIN_K11":Pool==DrawPool.Trail?"TF_TRAIL_W04":"TF_SHOWCASE_H20";
            var focus=items.FirstOrDefault(x=>x.Id==focusId)??quote.Eligible.FirstOrDefault(x=>x.Id==desired)??quote.Eligible.FirstOrDefault()??items.FirstOrDefault();
            var scene=Pool==DrawPool.Scene;var stageHeight=scene?Mathf.Clamp(h-484,180,cw*.92f):268;
            var texture=scene?HarborAppearanceArt.SceneTexture("TF_SCENE_S05"):C.CommerceBackground(Pool==DrawPool.Showcase?"Display_Harbor":"Display_Sea");
            var stage=C.DisplayBoard("PrizeStage",content,new Rect(0,-stageHeight-6,cw,stageHeight),texture,scene?1f:.5f);
            // Scene artwork is the full background itself; it is never fitted as a narrow portrait sprite.
            if(!scene&&focus!=null)C.Item(stage,focus.Id,new Rect(22,43,cw-44,stageHeight-67));
            var display=C.Label("DisplayTitle",stage,C.L("奖品展示","Prize display"),14);display.color=Color.white;C.P(display,12,stageHeight-29,cw-24,22);
            var shadow=display.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,.08f,.15f,.65f);shadow.effectDistance=new Vector2(0,-1);
            NamePlaque(stage,focus!=null?Name(focus):C.L("星河浮岛","Astral Isles"),focus?.Rarity,cw,scene);
            if(scene)
            {
                var y=-stageHeight-158;var locked=C.Plate("ClosedScenePool",content);C.P(locked,2,y,cw-4,137);
                var icon=HarborUI.Rect("Lock",locked.transform).gameObject.AddComponent<SampleVectorGraphic>();icon.Kind=SampleVectorGraphic.Shape.Lock;icon.color=HarborApprovedArt.Brown;icon.raycastTarget=false;C.P(icon,cw/2-19,89,34,34);
                var text=C.Label("ClosedPoolLabel",locked.transform,C.L("场景奖池暂未开放","Scene pool coming soon"),18);C.P(text,6,54,cw-16,30);
                var browse=C.Button("BrowseScenes",locked.transform,C.L("查看场景收藏 ›","View scene collection ›"),()=>backToCollection?.Invoke(3),"Capsule_Cream",15);C.P(browse,35,10,cw-74,36);
                content.sizeDelta=new Vector2(cw,stageHeight+167);
            }
            else
            {
                var heading=C.Label("PreviewTitle",content,C.L("精选奖励预览","Reward preview"),16);heading.alignment=TextAnchor.MiddleLeft;C.P(heading,4,-310,cw-108,30);
                var all=C.Button("AllRewards",content,C.L("奖池一览 ›","View all ›"),ShowAll,"Capsule_Cream",13);C.P(all,cw-103,-309,103,29);
                var preview=items.GroupBy(x=>x.Rarity).Select(g=>g.First()).Take(4).ToArray();
                for(var i=0;i<preview.Length;i++)Card(preview[i],content,new Rect(i*(cw+4)/4,-438,(cw-12)/4,120),false);
                content.sizeDelta=new Vector2(cw,444);
            }
            var gift=service.CanClaimFirstBlue;
            var can=service.IsAvailable&&service.CurrentLevel>=3&&Pool!=DrawPool.Scene&&(gift||quote.Eligible.Length>0);
            draw.interactable=can;
            var title=gift?C.L("领取船皮","Claim skin"):C.L("抽取一次","Draw once");
            var subtitle=service.CurrentLevel<3?C.L("通关第2关开放","Clear level 2 to unlock"):gift?C.L("免费赠礼","Free gift"):quote.Remaining.Length==0?C.L("当前奖池已集齐","Pool complete"):quote.Eligible.Length==0?C.L("暂无可抽内容","No eligible rewards"):quote.Price+C.L("金币"," coins");
            drawPresentation.SetContent(title,subtitle,can&&!gift);
            if(LastResult==UniqueDrawStatus.StorageUnavailable)message.text=C.L("保存未完成，未扣款；点击重试","Not saved or charged. Tap to retry.");
            else if(LastResult==UniqueDrawStatus.QuoteChanged)message.text=C.L("价格已更新，请按当前价格抽取","Price updated. Tap to draw at the displayed price.");
            else if(LastResult==UniqueDrawStatus.InsufficientCoins)message.text=C.L("金币不足","Not enough coins");
            if(!service.IsAvailable)message.text=C.L("存档不可用，请重新进入","Save unavailable. Reopen to retry.");
        }
        private void Card(UniqueDrawItem item,Transform parent,Rect rect,bool full)
        {
            var button=C.Button("Reward_"+item.Id,parent,"",()=>{focusId=item.Id;page="main";Render();},"Card_Ivory");C.P(button,rect.x,rect.y,rect.width,rect.height);
            var face=button.transform.Find("Face");var field=C.Plate("Rarity",face,"Capsule_Cream");field.color=C.Rarity(item.Rarity);C.P(field,4,42,rect.width-8,rect.height-47);
            C.Item(face,item.Id,new Rect(8,47,rect.width-16,rect.height-59));var name=C.Label("Name",face,Name(item),UILanguage.IsChinese?13:11);C.P(name,2,20,rect.width-4,26);
            var state=C.Label("Availability",face,Availability(item),10);C.P(state,2,2,rect.width-4,21);
        }
        private void RenderAll(float cw)
        {
            var items=UniqueDrawEngine.Items.Where(x=>x.Pool==Pool).ToArray();var rows=(items.Length+3)/4;content.sizeDelta=new Vector2(cw,Math.Max(160,rows*149));
            if(items.Length==0){var text=C.Label("ClosedPool",content,C.L("场景抽奖尚未开放\n现有场景可在收藏中查看","Scene draws are not open.\nBrowse scenes in your collection."),19);C.P(text,12,-150,cw-24,140);return;}
            for(var i=0;i<items.Length;i++)Card(items[i],content,new Rect(i%4*(cw+4)/4,-(i/4+1)*149,(cw-12)/4,141),true);
        }
        private void RenderResult(float cw)
        {
            var cardHeight=Mathf.Max(280,h-253);content.sizeDelta=new Vector2(cw,cardHeight+6);var item=UniqueDrawEngine.Items.First(x=>x.Id==reward.ItemId);
            var frame=C.DisplayBoard("RewardCard",content,new Rect(0,-cardHeight-6,cw,cardHeight),C.CommerceBackground(C.RewardBackground(item.Pool,item.Rarity)));
            C.Item(frame,item.Id,new Rect(23,96,cw-46,cardHeight-123));
            var namePlate=C.Plate("RewardNamePlate",frame,"Capsule_Cream");C.P(namePlate,35,51,cw-70,35);
            var name=C.Label("RewardName",namePlate.transform,Name(item),UILanguage.IsChinese?23:18);HarborUI.Fill(name.rectTransform);
            var badge=C.Plate("QualityBadge",frame,"Capsule_Cream");badge.color=C.Rarity(item.Rarity);C.P(badge,cw/2-55,15,110,29);
            var quality=C.Label("QualityName",badge.transform,C.RarityName(item.Rarity),17);HarborUI.Fill(quality.rectTransform);
            var ribbon=C.Plate("NewRewardRibbon",frame,"Tab_Brown");ribbon.color=new Color(1,.38f,.35f);C.P(ribbon,3,cardHeight-40,91,30);
            var fresh=C.Label("NewReward",ribbon.transform,C.L("新获得","NEW"),15);fresh.color=Color.white;HarborUI.Fill(fresh.rectTransform);
            C.Caption(resultUse,reward.Pool==DrawPool.Skin?C.L("前往装备","Equip"):reward.Pool==DrawPool.Trail?C.L("使用该拖尾","Use trail"):C.L("设为主页形象","Use on home"));
        }
        private void NamePlaque(Transform parent,string text,SkinRarity? rarity,float width,bool scene)
        {
            var pw=Mathf.Min(width-32,scene?230:266);var plate=C.Plate("PrizeNamePlate",parent,"Capsule_Cream");C.P(plate,(width-pw)/2,10,pw,31);
            var label=C.Label("PrizeName",plate.transform,text,UILanguage.IsChinese?19:14);C.P(label,9,1,pw-(rarity.HasValue?72:18),29);
            if(rarity.HasValue){var badge=C.Plate("RarityBadge",plate.transform,"Capsule_Cream");badge.color=C.Rarity(rarity.Value);C.P(badge,pw-65,4,57,23);var quality=C.Label("QualityName",badge.transform,C.RarityName(rarity.Value),12);HarborUI.Fill(quality.rectTransform);}
        }
        private void OnEnable(){if(service!=null&&content)Render();}
        private void Update(){Layout();}
        public void Layout()
        {
            if(!panel)return;var parent=(RectTransform)transform;var area=parent.rect;var safe=Screen.safeArea;
            // Simulator can retain a device-sized safe area after switching to a fixed Game View.
            if(safe.width<=0||safe.height<=0||safe.xMin<0||safe.yMin<0||safe.xMax>Screen.width+1||safe.yMax>Screen.height+1)safe=new Rect(0,0,Screen.width,Screen.height);
            var scale=area.width/Mathf.Max(1,Screen.width);var width=safe.width*scale-16;var height=safe.height*scale-16;
            var changed=Mathf.Abs(w-width)>.1f||Mathf.Abs(h-height)>.1f;w=width;h=height;
            C.P(panel,safe.x*scale+8,safe.y*scale+8,w,h);C.P(panelArt,0,0,w,h-62);C.P(header,34,h-101,w-68,100);C.P(closeButton,w-51,h-99,43,43);C.P(wallet.transform.parent.GetComponent<RectTransform>(),(w-188)/2,h-144,188,40);C.P(plus,w/2+59,h-142,35,35);
            for(var i=0;i<4;i++)C.P(tabs[i],13+i*(w-26)/4,h-199,(w-30)/4,45);
            var titleY=h-(page=="result"?149:242);C.P(poolTitle,13,titleY,w-26,35);C.P(titleRule,13,titleY,w-26,35);titleRule.CaptionWidth=Mathf.Min(w-112,poolTitle.preferredWidth+8);titleRule.SetVerticesDirty();
            C.P(draw,25,46,w-50,76);if(page=="result"){C.P(back,15,15,(w-38)/2,52);C.P(resultUse,w/2+4,15,(w-38)/2,52);}else C.P(back,80,8,w-160,34);C.P(message,18,page=="main"?128:page=="result"?70:49,w-36,35);
            LayoutContent();if(changed)Render();
        }
        private void LayoutContent()
        {var bottom=page=="main"?(Pool==DrawPool.Scene?60:147):page=="result"?88:90;var top=page=="result"?159:252;C.P(viewport,15,bottom,w-30,Mathf.Max(150,h-top-bottom));}
    }
}
