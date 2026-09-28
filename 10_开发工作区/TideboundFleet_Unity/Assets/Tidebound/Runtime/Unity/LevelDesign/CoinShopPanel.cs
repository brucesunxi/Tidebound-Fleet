using System;
using System.Linq;
using Tidebound.Save;
using Tidebound.Tools;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
using C=Tidebound.Unity.UI.HarborCommerceUI;
namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Real coin inventory transactions. Cash packs are explicitly unavailable until a store adapter exists.</summary>
    public sealed class CoinShopPanel:MonoBehaviour
    {
        private PlayerSaveService service;private ToolInventory inventory;private CoinShopCatalog catalog;
        private Action close,openCollection,openDraw;private Func<int> usesLeft;
        private RectTransform header,viewport,content;private ShowcaseSectionRule footerRule;private HarborSliceGraphic panelArt;private Text balance,note;private Button closeButton,plus,toolsTab,cashTab,bottom;
        private CoinShopProduct selected;private string request;private bool completed;private float w,h;private int tab;
        public CoinPurchaseStatus? LastResult{get;private set;}
        public string SelectedProductId=>selected?.Id;
        public int ActiveTab=>tab;
        public void ConfigureNavigation(Action collection,Action draw){openCollection=collection;openDraw=draw;if(bottom)C.Caption(bottom,C.L("前往抽奖","Go to Draw"));}
        public void Initialize(PlayerSaveService service,ToolInventory inventory,CoinShopCatalog catalog,Font font,Action onClose,Func<int> usesLeft=null)
        {
            this.service=service;this.inventory=inventory;this.catalog=catalog;close=onClose;this.usesLeft=usesLeft;
            var old=GetComponent<Graphic>();if(old)old.color=Color.clear;
            panelArt=C.Plate("ApprovedPanel",transform,"Panel_Gold");panelArt.Border=new Vector4(22,22,22,22);
            header=C.Header(transform,C.L("补给","Supplies"));
            closeButton=C.Button("CloseShop",transform,"",()=>close?.Invoke(),"Close_Coral");
            var wallet=C.Plate("Wallet",transform,"Capsule_Cream");balance=C.Label("Balance",wallet.transform,"",22);HarborUI.Fill(balance.rectTransform);
            var coin=C.Plate("Coin",wallet.transform,"Coin_Anchor");C.P(coin,4,3,34,34);
            plus=C.Button("Recharge",transform,"+",OpenRecharge,"CTA_Gold",24);
            toolsTab=C.Button("ToolsTab",transform,C.L("道具补给","Tools"),()=>SelectTab(0),"Tab_Gold");cashTab=C.Button("CashTab",transform,C.L("金币充值","Coins"),OpenRecharge,"Tab_Brown");
            viewport=C.Scroll("Products",transform,out content);note=C.Label("Notice",transform,"",13);StyleText(note,SeaInk,false);
            footerRule=HarborUI.Rect("SupplyFooterRule",transform).gameObject.AddComponent<ShowcaseSectionRule>();footerRule.CaptionWidth=0;footerRule.color=HarborApprovedArt.Brown;footerRule.raycastTarget=false;
            bottom=C.Button("GoToDraw",transform,C.L("前往抽奖","Go to Draw"),()=>{if(openDraw!=null)openDraw();else close?.Invoke();});C.Caption(bottom,C.L("返回游戏","Back to game"));ShowProducts();
        }
        public void Open(ShipTool preferred=ShipTool.None){gameObject.SetActive(true);tab=0;ShowProducts();}
        public void OpenRecharge(){SelectTab(1);}
        private void SelectTab(int value){tab=value;ShowProducts();}
        public void SelectProduct(string id){tab=0;selected=catalog.Find(id);if(selected==null)return;request=Guid.NewGuid().ToString("N");completed=false;LastResult=null;Render();}
        public void ConfirmPurchase()
        {
            if(selected==null||completed)return;
            LastResult=service==null?CoinPurchaseStatus.StorageUnavailable:service.BuyWithCoins(request,selected.Id,catalog);
            completed=LastResult==CoinPurchaseStatus.Purchased||LastResult==CoinPurchaseStatus.AlreadyPurchased;Render();
        }
        public void ShowProducts(){selected=null;request=null;completed=false;LastResult=null;Render();}
        public void Present(){if(!balance)return;balance.text=service?.IsAvailable==true?service.Coins.ToString("N0"):"—";}
        private string ProductName(CoinShopProduct p)=>p.Id=="reverse_1"?C.L("反转 ×1","Reverse ×1"):p.Id=="rescue_1"?C.L("救援 ×1","Rescue ×1"):p.Id=="shuffle_1"?C.L("洗牌 ×1","Shuffle ×1"):C.L("航行补给包","Supply Bundle");
        private string Description(CoinShopProduct p)=>p.Id=="reverse_1"?C.L("原地转向180°","Turn a ship 180°"):p.Id=="rescue_1"?C.L("最多救出2艘外围船","Rescue up to 2 outer ships"):p.Id=="shuffle_1"?C.L("最多重排5艘船","Rearrange up to 5 ships"):C.L("三种道具各1","One of each tool");
        private int Stock(CoinShopProduct p)=>inventory.Count(p.Reverse>0?ShipTool.Reverse:p.Rescue>0?ShipTool.Rescue:ShipTool.Shuffle);
        private void Render()
        {
            if(!content)return;Present();C.Clear(content);
            HarborApprovedArt.Set(toolsTab.GetComponentInChildren<HarborSliceGraphic>(),tab==0?"Tab_Gold":"Tab_Brown");HarborApprovedArt.Set(cashTab.GetComponentInChildren<HarborSliceGraphic>(),tab==1?"Tab_Gold":"Tab_Brown");
            StyleText(toolsTab.GetComponentInChildren<Text>(),tab==0?new Color(.25f,.12f,.015f):Color.white,false);StyleText(cashTab.GetComponentInChildren<Text>(),tab==1?new Color(.25f,.12f,.015f):Color.white,false);
            var cw=Math.Max(280,w-30);var height=selected!=null?430:tab==0?468:490;content.sizeDelta=new Vector2(cw,height);content.anchoredPosition=Vector2.zero;
            if(selected!=null)RenderConfirmation(cw,height);else if(tab==0)RenderTools(cw,height);else RenderCash(cw,height);
            note.text=tab==1?C.L("充值服务尚未开放 · 不会扣款","Purchases unavailable · no payment"):
                usesLeft?.Invoke()==0?C.L("本局使用已达上限 · 购买留待下局","Attempt limit reached · stock for next run"):
                C.L("购买后存入库存 · 每局最多使用5次","Added to inventory · up to 5 uses per run");
            if(service?.IsAvailable!=true)note.text=C.L("存档暂不可用，购买已禁用","Save unavailable · purchases disabled");
        }
        private static readonly Color SeaInk=new Color(.035f,.15f,.28f);
        private static void StyleText(Text text,Color color,bool emphasis=true)
        {
            // The English display face is already very heavy; use the readable body face
            // before applying bold so small descriptions and prices retain open counters.
            if(!UILanguage.IsChinese){if(text is HarborText localized)localized.UseDisplayFont=false;text.font=HarborUI.Font;}
            text.color=color;text.fontStyle=FontStyle.Bold;
            if(!emphasis)return;
            var outline=text.GetComponent<Outline>()??text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,.97f,.84f,.85f);outline.effectDistance=new Vector2(.45f,-.45f);
        }
        private void RenderTools(float cw,int height)
        {
            var order=new[]{"reverse_1","rescue_1","shuffle_1","tools_bundle_1"};
            for(var i=0;i<order.Length;i++)
            {
                var p=catalog.Find(order[i]);if(p==null)continue;var row=C.Plate("Product_"+p.Id,content);C.P(row,0,-(i+1)*117,cw,109);
                var border=HarborUI.Rect("CardAccent",row.transform).gameObject.AddComponent<Tidebound.Unity.UI.VisualSamples.SampleVectorGraphic>();
                border.Kind=Tidebound.Unity.UI.VisualSamples.SampleVectorGraphic.Shape.Outline;border.Radius=10;border.Stroke=1.1f;
                border.color=i==3?new Color(.83f,.57f,.18f,.75f):new Color(.28f,.61f,.75f,.50f);border.raycastTarget=false;C.P(border,2,2,cw-4,105);
                var field=C.DisplayBoard("IconBackground",row.transform,new Rect(8,18,80,80),C.CommerceBackground("Supply_Icon_Enamel"));
                var art=HarborUI.Art("ProductArt",field,p.Id=="tools_bundle_1"?"Icon_Supplies_v1":"Skins/Icon_"+(p.Reverse>0?"Reverse":p.Rescue>0?"Rescue":"Shuffle")+"_v1");
                if(p.Id=="tools_bundle_1")art.uvRect=new Rect(.1467f,.1148f,.736f,.744f);C.Fit(art,new Rect(5,5,70,70));
                var title=C.Label("ProductName",row.transform,ProductName(p),20);StyleText(title,SeaInk);title.alignment=TextAnchor.MiddleLeft;C.P(title,98,75,cw-107,28);
                var desc=C.Label("Description",row.transform,Description(p),13);StyleText(desc,new Color(.10f,.27f,.42f),false);desc.alignment=TextAnchor.MiddleLeft;C.P(desc,98,45,cw-106,30);
                if(p.Id!="tools_bundle_1")
                {
                    var stockPlate=C.Plate("StockBadge",row.transform,"Capsule_Cream");stockPlate.color=new Color(.61f,.84f,.98f);C.P(stockPlate,98,12,78,24);
                    var stock=C.Label("Stock",stockPlate.transform,C.L("库存 ","Stock ")+Stock(p),13);StyleText(stock,SeaInk,false);HarborUI.Fill(stock.rectTransform);
                }
                var buy=C.Button("Select_"+p.Id,row.transform,p.Price+C.L("金币"," coins"),()=>SelectProduct(p.Id),"CTA_Gold",16);C.P(buy,cw-148,9,139,41);buy.interactable=service?.IsAvailable==true&&service.CurrentLevel>=catalog.UnlockLevel;
                var coin=C.Plate("PriceCoin",buy.transform.Find("Face"),"Coin_Anchor");C.P(coin,9,8,25,25);coin.raycastTarget=false;
                var price=buy.GetComponentInChildren<Text>();price.rectTransform.offsetMin=new Vector2(34,8);price.rectTransform.offsetMax=new Vector2(-5,-4);StyleText(price,new Color(.25f,.12f,.015f));

            }
        }
        private void RenderCash(float cw,int height)
        {
            var intro=C.Label("CashHeading",content,C.L("金币可用于购买道具和抽奖","Coins for tools and draw rewards"),15);C.P(intro,0,-35,cw,28);
            var amounts=new[]{1000,3300,12000,27000};var prices=new[]{"0.99","2.99","9.99","19.99"};var cardW=(cw-8)/2;
            for(var i=0;i<4;i++)
            {
                var card=C.Plate("CoinPack_"+amounts[i],content);C.P(card,i%2*(cardW+8),-48-(i/2+1)*205,cardW,195);
                if(i>=2){var chest=HarborUI.Art("Chest",card.transform,"Icon_Supplies_v1");chest.uvRect=new Rect(.1467f,.1148f,.736f,.744f);C.P(chest,cardW/2-38,108,76,76);}
                for(var n=0;n<(i==0?1:i==1?3:i==2?2:4);n++){var coin=C.Plate("Coins_"+n,card.transform,"Coin_Anchor");var size=i<2?53:34;C.P(coin,cardW/2-size/2+(n-(i==0?0:1))*17,i<2?117+n%2*10:106+n%2*6,size,size);}
                var label=C.Label("Amount",card.transform,amounts[i].ToString("N0")+C.L("金币"," coins"),19);C.P(label,3,76,cardW-6,29);
                var unavailable=C.Button("Unavailable",card.transform,C.L("暂不可购买","Unavailable"),null,"CTA_Gold",15);C.P(unavailable,8,10,cardW-16,39);unavailable.interactable=false;
                var reference=C.Label("ReferencePrice",card.transform,C.L("基准 US$","Reference US$")+prices[i],11);C.P(reference,0,53,cardW,20);
            }
        }
        private void RenderConfirmation(float cw,int height)
        {
            var enough=service?.IsAvailable==true&&service.Coins>=selected.Price;
            var title=C.Label("PurchaseTitle",content,ProductName(selected),24);C.P(title,5,-65,cw-10,45);
            var info=Description(selected)+"\n\n"+C.L("消耗：","Cost: ")+selected.Price+C.L("金币"," coins")+"\n";
            if(service?.IsAvailable==true)info+=enough?C.L("扣后余额：","Balance after: ")+(service.Coins-selected.Price):C.L("还差 ","Need ")+(selected.Price-service.Coins)+C.L("金币"," coins");
            info+="\n"+(selected.Id=="tools_bundle_1"?C.L("反转／救援／洗牌各增加1","Adds 1 Reverse / Rescue / Shuffle"):C.L("库存：","Stock: ")+Stock(selected)+" → "+(Stock(selected)+(completed?0:1)));
            if(completed)info=C.L("已存入库存","Added to inventory");else if(LastResult==CoinPurchaseStatus.StorageUnavailable)info=C.L("保存未完成，请重试原订单","Save incomplete. Retry this purchase.");
            var details=C.Label("Details",content,info,18);C.P(details,14,-265,cw-28,195);
            var confirm=C.Button("ConfirmPurchase",content,completed?C.L("已购买","Purchased"):enough?C.L("确认购买","Confirm purchase"):C.L("前往充值","View coin packs"),()=>{if(enough)ConfirmPurchase();else OpenRecharge();},"CTA_Gold",20);C.P(confirm,20,-337,cw-40,55);confirm.interactable=!completed&&service?.IsAvailable==true;
            var back=C.Button("BackToProducts",content,C.L("返回道具补给","Back to supplies"),ShowProducts);C.P(back,20,-403,cw-40,48);
        }
        public void Layout(Rect safe)
        {
            var rect=new Rect(safe.x+8,safe.y+8,safe.width-16,safe.height-16);HarborUI.Place((RectTransform)transform,rect);var changed=Math.Abs(w-rect.width)>.1f||Math.Abs(h-rect.height)>.1f;w=rect.width;h=rect.height;
            C.P(panelArt,0,0,w,h-62);C.P(header,32,h-101,w-64,100);C.P(closeButton,w-51,h-99,43,43);C.P(balance.transform.parent.GetComponent<RectTransform>(),(w-188)/2,h-145,188,40);C.P(plus,w/2+58,h-143,35,35);
            C.P(toolsTab,16,h-198,(w-34)/2,44);C.P(cashTab,w/2+1,h-198,(w-34)/2,44);
            C.P(viewport,15,124,w-30,h-335);C.P(footerRule,23,109,w-46,18);C.P(note,20,62,w-40,43);C.P(bottom,72,15,w-144,43);
            if(changed)Render();else Present();
        }
    }
}
