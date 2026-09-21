using System;
using System.Linq;
using System.Collections.Generic;
using Tidebound.Collection;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Graybox collection flow: browse, explicit purchase confirmation, durable reveal, next-attempt equipment.</summary>
    public sealed class CollectionPanel : MonoBehaviour
    {
        private RectTransform content, viewport, categoryPanel;
        private ScrollRect scroll;
        private Text categoryTitle, categoryInfo;
        private int category;
        private bool drawMode;
        private Action openShop;
        private Rect lastSafe;
        public int ActiveCategory => category;
        public bool IsDrawPage => drawMode;
        public void ConfigureNavigation(Action supplies) { openShop=supplies; }
        private PlayerSaveService service;private Font font;private CollectionShipPreview preview;
        private Text balance,details,message,filterLabel,transactionText;
        private Button equip,exchange,single,ten,free,confirm;
        private RectTransform transaction;
        private readonly List<Button> cards=new List<Button>(),slots=new List<Button>();
        private readonly Dictionary<string,RectTransform> elements=new Dictionary<string,RectTransform>();
        private string selected,replaceId,request,kind,target;private bool completed;private int page,filter,sourceFilter;
        public CollectionStatus? LastResult {get;private set;}
        public bool HasConfirmation=>transaction!=null && transaction.gameObject.activeSelf;
        public string SelectedSkinId=>selected;
        public string ReplacementSkinId=>replaceId;
        public CollectionReceipt LastReceipt {get;private set;}
        public void Initialize(PlayerSaveService value,Font typeface,Action close)
        {
            service=value;font=HarborUI.Font;
            var surface=GetComponent<Image>();if(surface!=null)surface.color=HarborUI.Cream;
            Label("Title","Fleet collection",21);var header=HarborUI.Surface("TitlePlate",transform,HarborUI.Wood);
            header.raycastTarget=false;header.transform.SetAsFirstSibling();elements["Title"].GetComponent<Text>().color=HarborUI.Cream;
            balance=Label("Balance","",14);
            Label("EquipmentNote","5 slots - used on your NEXT attempt",12);
            for(var i=0;i<5;i++){var n=i;slots.Add(Button("Slot"+i,"",()=>TapSlot(n)));}
            filterLabel=Label("FilterSummary","",12);
            Button("Filter","Rarity >",()=>{filter=(filter+1)%6;page=0;Present();});
            Button("Source","Source >",()=>{sourceFilter=(sourceFilter+1)%3;page=0;Present();});
            for(var i=0;i<8;i++){var n=i;cards.Add(Button("Card"+i,"",()=>{var list=Visible();var index=page*8+n;if(index<list.Length)SelectSkin(list[index].Id);}));}
            foreach(var card in cards)
            {
                var art=HarborUI.Rect("ShipCard",card.transform);art.gameObject.AddComponent<ShipCardGraphic>().raycastTarget=false;
                HarborUI.Place(art,new Rect(25,49,46,64));
                var text=card.GetComponentInChildren<Text>();text.fontSize=14;text.alignment=TextAnchor.LowerCenter;
                text.rectTransform.offsetMin=new Vector2(3,6);text.rectTransform.offsetMax=new Vector2(-3,-70);
            }
            Button("Page","More cards >",()=>{page=(page+1)%Math.Max(1,(Visible().Length+7)/8);Present();});
            var previewRect=Make("Preview",transform);preview=previewRect.gameObject.AddComponent<CollectionShipPreview>();preview.Initialize(true);
            Button("Rotate","Turn",preview.Rotate);Button("Fire","Fire",preview.Fire);
            details=Label("Details","",12);equip=Button("Equip","Equip",EquipSelected);exchange=Button("Exchange","Exchange",()=>SelectTransaction("Exchange",selected));
            single=Button("Single","",()=>SelectTransaction("Single"));ten=Button("Ten","",()=>SelectTransaction("Ten"));
            free=Button("Free","Claim free BLUE",()=>SelectTransaction("FirstBlue"));
            Button("Odds","Rates / protection",ShowOdds);message=Label("Message","",12);Button("Close","×",close);
            transaction=Make("Transaction",transform);var shade=transaction.gameObject.AddComponent<HarborImage>();shade.color=HarborUI.Cream;
            transactionText=Label("TransactionText","",14,transaction);
            confirm=Button("Confirm","Confirm",ConfirmTransaction,transaction);
            Button("Back","Back to collection",()=>{transaction.gameObject.SetActive(false);Present();HarborUI.Focus(elements["Category0"].GetComponent<Button>());},transaction);
            transaction.gameObject.SetActive(false);selected=SkinCatalog.All[0].Id;
            scroll=HarborUI.Scroll("CollectionScroll",transform,out content);viewport=(RectTransform)scroll.transform;
            foreach(var name in new[]{"EquipmentNote","FilterSummary","Filter","Source","Page","Preview","Rotate","Fire","Details","Equip","Exchange","Single","Ten","Free","Odds"})elements[name].SetParent(content,false);
            foreach(var b in slots.Concat(cards))b.transform.SetParent(content,false);
            for(var i=0;i<4;i++){var n=i;Button("Category"+i,new[]{"Skins","Trails","Showcase","Scenes"}[i],()=>SelectCategory(n));}
            Button("DrawTab","Draw",()=>OpenDraw());Button("SuppliesTab","Supplies",()=>{if(!HasConfirmation)openShop?.Invoke();});
            categoryPanel=HarborUI.Rect("CategoryPlaceholder",content);
            categoryTitle=HarborUI.Label("Title",categoryPanel,"",23);
            categoryInfo=HarborUI.Label("Info",categoryPanel,"",16);
            HarborUI.Art("CategoryArt",categoryPanel,"Icon_Collection_v1");
            categoryPanel.gameObject.SetActive(false);
            elements["Close"].GetComponent<Image>().color=new Color(.93f,.29f,.22f);
            var closeLabel=elements["Close"].GetComponentInChildren<Text>();closeLabel.color=Color.white;closeLabel.fontSize=27;
            transaction.SetAsLastSibling();
        }
        public void Open()
        {gameObject.SetActive(true);transaction.gameObject.SetActive(false);replaceId=null;page=0;LastResult=null;drawMode=service?.CanClaimFirstBlue==true;category=0;message.text="New skins are never equipped automatically.";RefreshPage();Present();HarborUI.Focus(elements["Category0"].GetComponent<Button>());}
        private SkinDefinition[] Visible()=>SkinCatalog.All.Where(s=>(filter==0 || (int)s.Rarity==filter-1) && (sourceFilter==0 || sourceFilter==1 && s.IsDefault || sourceFilter==2 && !s.IsDefault)).ToArray();
        public static string ShortName(string id)
        {var s=SkinCatalog.Find(id);return s==null?"Empty":s.IsDefault?"Default":"CBPGR"[(int)s.Rarity]+"-"+id.Substring(id.Length-2);}
        public void SelectSkin(string id)
        {if(SkinCatalog.Find(id)==null || HasConfirmation)return;selected=id;replaceId=null;Present();}
        public void EquipSelected()
        {
            if(!gameObject.activeInHierarchy || HasConfirmation || service?.IsAvailable!=true)return;
            var data=service.Snapshot.Collection;if(!data.OwnedIds.Contains(selected))return;
            var index=Array.IndexOf(data.Equipment,selected);
            if(index>=0){data.Equipment[index]=null;SaveSlots(data.Equipment);return;}
            index=Array.IndexOf(data.Equipment,null);
            if(index<0){replaceId=selected;message.text="Choose one of the 5 slots to replace. No change until selected.";return;}
            data.Equipment[index]=selected;SaveSlots(data.Equipment);
        }
        public void TapSlot(int index)
        {
            if(!gameObject.activeInHierarchy || HasConfirmation || service?.IsAvailable!=true || index<0 || index>=5)return;
            var data=service.Snapshot.Collection;data.Equipment[index]=replaceId;SaveSlots(data.Equipment);
        }
        private void SaveSlots(string[] value)
        {
            var status=service.SetEquipment(Guid.NewGuid().ToString("N"),value);
            if(status==EquipmentStatus.Saved || status==EquipmentStatus.AlreadySaved){replaceId=null;message.text="Equipment saved. Current attempt keeps its original fleet.";}
            else message.text="Equipment not changed: "+status;
            Present();
        }
        public void SelectTransaction(string action,string skin=null)
        {
            if(!gameObject.activeInHierarchy || HasConfirmation || service?.IsAvailable!=true)return;
            kind=action;target=skin;request=Guid.NewGuid().ToString("N");completed=false;LastResult=null;LastReceipt=null;
            transaction.gameObject.SetActive(true);confirm.gameObject.SetActive(true);confirm.interactable=true;
            var c=service.Snapshot.Collection;var price=CollectionRules.SinglePrice(c.OwnedIds.Length-1)*(kind=="Ten"?9:1);
            transactionText.text=kind=="FirstBlue" ? "FREE BLUE GIFT\n\nOne unowned blue skin.\nNo coins charged.\nSeparate gift: normal pity counters unchanged.\n\nEquip it after claiming if you wish." :
                kind=="Exchange" ? "EXCHANGE\n\n"+ShortName(skin)+"\nCost: "+CollectionRules.ExchangeTickets(SkinCatalog.Find(skin).Rarity)+" tickets\n\nNo automatic equipment change." :
                (kind=="Ten"?"TEN DRAWS":"SINGLE DRAW")+"\n\nCost: "+price+" coins\n"+(kind=="Ten"?"Full batch price locked before drawing.\nAt least one purple or higher.":"")+"\n\nDuplicates become tickets.\nResults are saved before they appear.";
            confirm.GetComponentInChildren<Text>().text="Confirm";HarborUI.Focus(confirm);
        }
        public void ConfirmTransaction()
        {
            if(!gameObject.activeInHierarchy || !HasConfirmation || completed || kind==null)return;
            LastResult=service.Collect(request,kind,target);completed=LastResult==CollectionStatus.Saved || LastResult==CollectionStatus.AlreadySaved;
            if(!completed){transactionText.text="Not completed: "+LastResult+"\n\nNo collection change committed.\nRetry or return to collection.";return;}
            LastReceipt=service.CollectionReceiptFor(request);selected=LastReceipt.SkinIds.Last();
            var before=new HashSet<string>(service.Snapshot.Collection.OwnedIds);
            // Reconstruct ownership just before this receipt so repeated results in one ten-draw are labelled correctly.
            before.Clear();before.Add(SkinCatalog.All[0].Id);
            foreach(var r in service.Snapshot.Collection.Receipts.TakeWhile(r=>r.RequestId!=request))foreach(var id in r.SkinIds)before.Add(id);
            var lines=LastReceipt.SkinIds.Select(id=>{var s=SkinCatalog.Find(id);return ShortName(id)+"  "+(before.Add(id)?"NEW":"+"+CollectionRules.DuplicateTickets(s.Rarity)+" tickets");});
            transactionText.text="SAVED TO COLLECTION\n\n"+string.Join("\n",lines)+"\n\nCoins spent: "+LastReceipt.CoinCost+"  /  Tickets spent: "+LastReceipt.TicketCost+"\n\nChoose equipment after returning.";
            confirm.interactable=false;confirm.GetComponentInChildren<Text>().text="Saved";Present();
        }
        public void ShowOdds()
        {
            if(HasConfirmation)return;kind=null;transaction.gameObject.SetActive(true);confirm.gameObject.SetActive(false);
            transactionText.text="BASE RATES\nCommon 48% / Blue 30% / Purple 15%\nGold 6% / Red 1%\n\nPROTECTION\nTen: at least one purple+.\n20th without gold+: gold+.\n60th without red: red.\n4 duplicates: next ordinary low-tier draw\nprioritizes a new common / blue / purple.\nHigher-quality pity takes priority.\n\nPity pools use relative base weights.\nNew-skin protection selects uniformly\nfrom available common / blue / purple.\nFree blue gift and exchange do not count.\n\nBaseline prices: U 0-3: 300 / 4-7: 450\n8-11: 700 / 12-15: 1000. Ten costs 9 singles.";
        }
        public void Present()
        {
            if(service==null)return;var d=service.Snapshot.Collection;var available=service.IsAvailable && service.CurrentLevel>=3;
            balance.text="Owned "+d.OwnedIds.Length+"/16   Coins "+service.Coins+"   Tickets "+d.Tickets;
            for(var i=0;i<5;i++){slots[i].GetComponentInChildren<Text>().text=(i+1)+"\n"+ShortName(d.Equipment[i]);slots[i].interactable=available;}
            var list=Visible();page=Math.Min(page,Math.Max(0,(list.Length-1)/8));
            filterLabel.text=(filter==0?"All rarities":((SkinRarity)(filter-1)).ToString())+" / "+new[]{"All sources","Default","Draw / exchange"}[sourceFilter];
            for(var i=0;i<8;i++)
            {
                var index=page*8+i;cards[i].gameObject.SetActive(category==0 && !drawMode && index<list.Length);if(index>=list.Length)continue;
                var s=list[index];var owns=d.OwnedIds.Contains(s.Id);var slot=Array.IndexOf(d.Equipment,s.Id);
                var tint=new[]{new Color(.58f,.76f,.80f),new Color(.22f,.62f,.90f),new Color(.66f,.45f,.85f),HarborUI.Gold,new Color(.94f,.39f,.32f)}[(int)s.Rarity];
                cards[i].GetComponentInChildren<ShipCardGraphic>().color=owns?tint:new Color(.30f,.42f,.48f);
                cards[i].GetComponentInChildren<Text>().text=ShortName(s.Id)+"\n"+(slot>=0?"Slot "+(slot+1):owns?"Owned":"Locked");
                cards[i].GetComponent<Image>().color=s.Id==selected?HarborUI.Gold:owns?HarborUI.Aqua:new Color(.83f,.87f,.88f);
            }
            var definition=SkinCatalog.Find(selected);var owned=d.OwnedIds.Contains(selected);var equipped=Array.IndexOf(d.Equipment,selected)>=0;
            preview.Present(Math.Max(0,Array.IndexOf(d.Equipment,selected)),owned);
            var acquired=d.Receipts.FirstOrDefault(r=>r.SkinIds.Contains(selected));
            details.text=ShortName(selected)+"  /  "+definition.Rarity+"  /  1 x 2 ship\nCoins per hit: 1-"+CollectionDrawEngine.CoinCap(definition.Rarity)+" (fleet-share chance)\n"+
                (owned ? "Acquired: "+(acquired==null?"Default":acquired.AtUtc.Substring(0,10)+" / "+acquired.Kind) : "Source: draw or "+CollectionRules.ExchangeTickets(definition.Rarity)+" tickets")+"\nMovement / damage unchanged.";
            equip.interactable=available && owned;equip.GetComponentInChildren<Text>().text=equipped?"Unequip":"Equip / replace";
            exchange.interactable=available && !owned && service.CurrentLevel>=5 && !service.CanClaimFirstBlue;
            exchange.GetComponentInChildren<Text>().text=service.CurrentLevel<5?"Exchange at Lv5":"Exchange "+CollectionRules.ExchangeTickets(definition.Rarity);
            var price=CollectionRules.SinglePrice(d.OwnedIds.Length-1);var u=d.OwnedIds.Length-1;var next=u<4?4:u<8?8:u<12?12:15;
            single.GetComponentInChildren<Text>().text="Single  "+price;ten.GetComponentInChildren<Text>().text=service.CurrentLevel<4?"Ten at Lv4":"Ten  "+price*9;
            single.interactable=available && !service.CanClaimFirstBlue;ten.interactable=single.interactable && service.CurrentLevel>=4;
            free.interactable=available && service.CanClaimFirstBlue;free.GetComponentInChildren<Text>().text=service.CanClaimFirstBlue?"Claim FREE blue":"Free blue claimed";
            elements["Page"].GetComponentInChildren<Text>().text="Page "+(page+1)+" / "+Math.Max(1,(list.Length+7)/8)+"  >";
            elements["EquipmentNote"].GetComponent<Text>().text="NEXT attempt only  |  "+u+" collected  |  "+(u<12?(next-u)+" to next price":"Final price tier");
            elements["Odds"].GetComponentInChildren<Text>().text="Rates / pity  G "+d.GoldDry+"/20  R "+d.RedDry+"/60";
        }
        public void OpenDraw()
        {
            if(HasConfirmation)return;
            drawMode=true;category=0;RefreshPage();Present();
        }
        public void SelectCategory(int value)
        {
            if(HasConfirmation || value<0 || value>3)return;
            category=value;drawMode=false;RefreshPage();Present();
        }
        private void RefreshPage()
        {
            foreach(var pair in elements.Where(e=>e.Value.parent==content))pair.Value.gameObject.SetActive(false);
            categoryPanel.gameObject.SetActive(category!=0);
            if(category!=0)
            {
                categoryTitle.text=new[]{"","Sailing trails","Home showcase","Harbor backgrounds"}[category];
                categoryInfo.text="Earn through level clears.\n\nThe unlock catalog is being prepared.\nYour current appearance stays unchanged.";
            }
            else
            {
                var names=drawMode ? new[]{"Preview","Details","Single","Ten","Free","Odds"} :
                    new[]{"EquipmentNote","FilterSummary","Filter","Source","Page","Preview","Rotate","Details","Equip","Exchange","Free"};
                foreach(var name in names)elements[name].gameObject.SetActive(true);
                foreach(var b in slots.Concat(cards))b.gameObject.SetActive(!drawMode);
            }
            elements["Title"].GetComponent<Text>().text=drawMode?"Ship draw":"Fleet collection";
            scroll.verticalNormalizedPosition=1;
            if(lastSafe.width>0)Layout(lastSafe);
        }
        public void Layout(Rect safe)
        {
            lastSafe=safe;safe=new Rect(safe.x+8,safe.y+8,safe.width-16,safe.height-16);Place((RectTransform)transform,safe);var w=safe.width;var h=safe.height;
            Place((RectTransform)transform.Find("TitlePlate"),new Rect(12,h-67,w-84,58));
            At("Title",16,h-61,w-88,48);At("Close",w-66,h-61,52,48);
            At("Balance",12,h-103,w-24,36);
            for(var i=0;i<4;i++)At("Category"+i,12+i*(w-24)/4,h-161,(w-30)/4,50);
            Place(viewport,new Rect(12,132,w-24,h-305));
            var cw=w-24;var ch=category!=0?480:drawMode?580:1044;
            content.sizeDelta=new Vector2(cw,ch);content.anchoredPosition=Vector2.zero;
            if(category!=0)
            {
                Place(categoryPanel,new Rect(0,0,cw,ch));
                Place(categoryTitle.rectTransform,new Rect(12,ch-74,cw-24,60));
                Place((RectTransform)categoryPanel.Find("CategoryArt"),new Rect((cw-150)/2,ch-244,150,150));
                Place(categoryInfo.rectTransform,new Rect(16,ch-420,cw-32,160));
            }
            else if(drawMode)
            {
                At("Preview",(cw-190)/2,ch-200,190,190);At("Details",12,ch-360,cw-24,152);
                At("Single",6,ch-424,(cw-18)/2,54);At("Ten",12+(cw-18)/2,ch-424,(cw-18)/2,54);
                At("Free",6,ch-486,cw-12,52);At("Odds",6,ch-548,cw-12,52);
                if(service?.CanClaimFirstBlue==true)
                {
                    At("Free",6,ch-60,cw-12,52);At("Preview",(cw-190)/2,ch-266,190,190);
                    At("Details",12,ch-430,cw-24,152);At("Single",6,ch-492,(cw-18)/2,54);At("Ten",12+(cw-18)/2,ch-492,(cw-18)/2,54);
                }
            }
            else
            {
                At("EquipmentNote",4,ch-36,cw-8,32);
                for(var i=0;i<5;i++)At("Slot"+i,4+i*(cw-8)/5,ch-96,(cw-18)/5,54);
                At("Filter",4,ch-154,(cw-14)/2,48);At("Source",10+(cw-14)/2,ch-154,(cw-14)/2,48);
                At("FilterSummary",4,ch-190,cw-8,30);
                for(var i=0;i<8;i++)At("Card"+i,4+i%3*(cw-8)/3,ch-320-i/3*130,(cw-20)/3,122);
                At("Page",4,ch-636,cw-8,48);
                At("Preview",4,ch-750,100,100);At("Details",112,ch-800,cw-116,154);
                At("Rotate",4,ch-860,100,48);At("Equip",112,ch-860,cw-116,48);
                At("Exchange",4,ch-918,cw-8,50);At("Free",4,ch-976,cw-8,50);
            }
            At("Message",14,68,w-28,56);At("DrawTab",14,10,(w-34)/2,52);At("SuppliesTab",20+(w-34)/2,10,(w-34)/2,52);
            Place(transaction,new Rect(0,0,w,h));Place(transactionText.rectTransform,new Rect(18,150,w-36,h-172));
            At("Confirm",16,82,w-32,52);At("Back",16,18,w-32,52);
            for(var i=0;i<4;i++)elements["Category"+i].GetComponent<Image>().color=category==i&&!drawMode?HarborUI.Gold:HarborUI.Aqua;
            elements["DrawTab"].GetComponent<Image>().color=drawMode?HarborUI.Gold:HarborUI.Aqua;
        }
        private void At(string name,float x,float y,float w,float h)=>Place(elements[name],new Rect(x,y,w,h));
        private RectTransform Make(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;elements[name]=r;return r;}
        private Text Label(string name,string value,int size,Transform parent=null)
        {var r=Make(name,parent??transform);var t=r.gameObject.AddComponent<HarborText>();t.font=font;t.fontSize=Math.Max(14,size);t.text=value;t.color=HarborUI.Ink;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private Button Button(string name,string value,Action action,Transform parent=null)
        {var r=Make(name,parent??transform);var image=r.gameObject.AddComponent<HarborImage>();image.color=HarborUI.Aqua;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());var t=Label(name+"Label",value,13,r);t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=new Vector2(5,3);t.rectTransform.offsetMax=new Vector2(-5,-3);return b;}
        private static void Place(RectTransform r,Rect rect){r.anchoredPosition=rect.position;r.sizeDelta=rect.size;}
    }
}
