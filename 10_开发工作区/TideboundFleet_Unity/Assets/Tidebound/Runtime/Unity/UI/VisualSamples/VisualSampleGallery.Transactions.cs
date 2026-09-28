using System;
using System.Linq;
using Tidebound.Collection;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI.VisualSamples
{
    public sealed partial class VisualSampleGallery
    {
        private RectTransform liveTransaction;
        private Text liveTransactionText;
        private Button liveConfirm,liveBack,liveTen,liveOdds,liveExchange;
        private string liveRequest,liveKind,liveTarget;
        private bool liveCompleted;
        private void BuildProductionTransactions()
        {
            liveTen=TrailTool("TenDraw",DrawPopup,"");liveOdds=TrailTool("DrawRules",DrawPopup,"");
            liveTen.onClick.AddListener(()=>OpenProductionTransaction("Ten"));liveOdds.onClick.AddListener(ShowProductionRates);
            liveExchange=TrailTool("TicketExchange",SkinViewer,"");
            liveExchange.onClick.AddListener(()=>OpenProductionTransaction("Exchange",liveTarget));
            liveTransaction=Rect("DurableCollectionTransaction",PreviewCanvas.transform);Fill(liveTransaction);
            var blocker=liveTransaction.gameObject.AddComponent<Image>();blocker.color=new Color(.01f,.05f,.08f,.83f);blocker.raycastTarget=true;
            var frame=Art("GoldFrame","Panel_Gold",liveTransaction);Fill(frame.rectTransform,12);
            liveTransactionText=Label("ActualTransaction",liveTransaction,"",17,Brown);liveTransactionText.verticalOverflow=VerticalWrapMode.Overflow;
            liveConfirm=TrailTool("ConfirmOnce",liveTransaction,"");liveBack=TrailTool("Back",liveTransaction,"");
            liveConfirm.onClick.AddListener(ConfirmProductionTransaction);liveBack.onClick.AddListener(()=>{liveTransaction.gameObject.SetActive(false);RefreshText();});
            liveTransaction.gameObject.SetActive(false);LayoutProductionTransactions();
        }
        private void LayoutProductionTransactions()
        {
            if(!liveTransaction)return;var h=Screen.height*390f/Mathf.Max(1,Screen.width);
            Place(liveTransactionText.rectTransform,32,70,326,h-225);
            Place((RectTransform)liveConfirm.transform,204,h-118,150,48);Place((RectTransform)liveBack.transform,36,h-118,150,48);
            var top=(h-478)/2;Place((RectTransform)liveTen.transform,126,top+369,124,29);Place((RectTransform)liveOdds.transform,253,top+369,95,29);
            Place((RectTransform)liveExchange.transform,95,h-126,200,39);
        }
        private void RefreshProductionTransactions()
        {
            if(!liveTransaction)return;
            var skin=DrawCategory==0;liveTen.gameObject.SetActive(skin);liveOdds.gameObject.SetActive(skin);
            liveTen.interactable=productionService.IsAvailable&&!productionService.CanClaimFirstBlue&&productionService.CurrentLevel>=4;
            liveTen.GetComponentInChildren<Text>().text=English?"Ten draws":"十连抽";liveOdds.GetComponentInChildren<Text>().text=English?"Rules":"规则";
            liveBack.GetComponentInChildren<Text>().text=English?"Back":"返回";liveConfirm.GetComponentInChildren<Text>().text=English?"Confirm":"确认";
            LayoutProductionTransactions();
        }
        private void InspectProductionExchange(int index)
        {
            if(!IsProduction||!liveExchange)return;
            if(CommerceDraw){liveExchange.gameObject.SetActive(false);return;}
            var legacy=AppearanceDrawEngine.Pool.FirstOrDefault(s=>s.Id==SkinIdentity(index));liveTarget=legacy?.Id;
            liveExchange.gameObject.SetActive(legacy!=null&&!IsSkinPreviewOwned(index));
            liveExchange.interactable=productionService.CurrentLevel>=5&&!productionService.CanClaimFirstBlue;
            if(legacy!=null)liveExchange.GetComponentInChildren<Text>().text=(English?"Exchange · ":"兑换 · ")+CollectionRules.ExchangeTickets(legacy.Rarity)+(English?" tickets":"券");
            LayoutProductionTransactions();
        }
        private void OpenProductionTransaction(string kind,string target=null)
        {
            liveKind=kind;liveTarget=target;liveRequest=Guid.NewGuid().ToString("N");liveCompleted=false;
            var c=productionService.AppearanceDrawState;var price=c.Price*(kind=="Ten"?9:1);
            var cost=kind=="Exchange"?CollectionRules.ExchangeTickets(AppearanceCatalog.Find(target).Rarity):price;
            liveTransaction.gameObject.SetActive(true);liveConfirm.gameObject.SetActive(true);liveConfirm.interactable=true;
            liveTransactionText.text=(English?"CONFIRM COLLECTION\n\n":"确认收藏操作\n\n")+
                (kind=="Exchange"?(English?"Exchange costs ":"兑换消耗 "):(English?"Draw costs ":"抽取消耗 "))+cost+(kind=="Exchange"?(English?" tickets":"张收藏券"):(English?" coins":"金币"))+
                (English?"\n\n14 draw skins; level/share rewards excluded.\nDuplicates become tickets.\nChoose equipment after drawing.":"\n\n奖池包含14款皮肤\n关卡与分享专属不在此奖池\n重复获得将转为收藏券\n抽取后可自行选择装备");
            LayoutProductionTransactions();
        }
        private void ConfirmProductionTransaction()
        {
            if(liveCompleted||liveKind==null)return;
            var result=productionService.CollectAppearance(liveRequest,liveKind,liveTarget);
            if(result!=CollectionStatus.Saved&&result!=CollectionStatus.AlreadySaved)
            {liveTransactionText.text=(English?"Not completed: ":"操作未完成：")+result+(English?"\n\nNo committed change. Retry or return.":"\n\n未提交变更，可重试或返回。");return;}
            liveCompleted=true;liveConfirm.interactable=false;var receipt=productionService.AppearanceReceiptFor(liveRequest);
            var names=receipt.SkinIds.Select(id=>AppearanceCatalog.Find(id)).Select(x=>English?x.Name:x.ChineseName);
            liveTransactionText.text=(English?"SAVED TO COLLECTION\n\n":"已保存到收藏\n\n")+string.Join("\n",names)+(English?"\n\nDuplicates convert to tickets.\nChoose equipment after returning.":"\n\n重复内容按原规则转为收藏券\n返回后自行选择装备");RefreshText();
        }
        private void ShowProductionRates()
        {
            liveKind=null;liveTransaction.gameObject.SetActive(true);liveConfirm.gameObject.SetActive(false);
            liveTransactionText.text=English?"SKIN POOL RULES\n\n14 skins · excludes level/share rewards\nWhite 48% · Blue 30% · Purple 15%\nGold 6% · Red 1%\n\nTen draws: purple+ guaranteed\n20 draws without gold+: gold+\n60 without red: red\nFour duplicates: prioritize a new low tier\n\nOwned in this pool: 0–3 / 4–7 / 8–11 / 12+\nSingle: 300 / 450 / 700 / 1000 coins\nTen draws cost nine singles.":"皮肤奖池规则\n\n14款皮肤 · 不含关卡与分享专属\n白48% · 蓝30% · 紫15% · 金6% · 红1%\n\n十连至少紫色；20抽未出金保底金色以上\n60抽未出红保底红色\n连续4次重复后优先未拥有低阶\n\n本奖池已拥有0–3／4–7／8–11／12+款\n单抽300／450／700／1000金币\n十连为当次单抽价格的9倍";
            LayoutProductionTransactions();
        }
    }
}
