using System;
using System.Linq;
using System.Collections.Generic;
using Tidebound.Collection;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Graybox collection flow: browse, explicit purchase confirmation, durable reveal, next-attempt equipment.</summary>
    public sealed class CollectionPanel : MonoBehaviour
    {
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
            service=value;font=typeface;
            Label("Title","FLEET COLLECTION  /  GRAYBOX",19);balance=Label("Balance","",14);
            Label("EquipmentNote","5 slots - saved now, used on your NEXT attempt",12);
            for(var i=0;i<5;i++){var n=i;slots.Add(Button("Slot"+i,"",()=>TapSlot(n)));}
            filterLabel=Label("FilterSummary","",12);
            Button("Filter","Rarity >",()=>{filter=(filter+1)%6;page=0;Present();});
            Button("Source","Source >",()=>{sourceFilter=(sourceFilter+1)%3;page=0;Present();});
            for(var i=0;i<8;i++){var n=i;cards.Add(Button("Card"+i,"",()=>{var list=Visible();var index=page*8+n;if(index<list.Length)SelectSkin(list[index].Id);}));}
            Button("Page","More cards >",()=>{page=(page+1)%Math.Max(1,(Visible().Length+7)/8);Present();});
            var previewRect=Make("Preview",transform);preview=previewRect.gameObject.AddComponent<CollectionShipPreview>();preview.Initialize();
            Button("Rotate","Turn",preview.Rotate);Button("Fire","Fire",preview.Fire);
            details=Label("Details","",12);equip=Button("Equip","Equip",EquipSelected);exchange=Button("Exchange","Exchange",()=>SelectTransaction("Exchange",selected));
            single=Button("Single","",()=>SelectTransaction("Single"));ten=Button("Ten","",()=>SelectTransaction("Ten"));
            free=Button("Free","Claim free BLUE",()=>SelectTransaction("FirstBlue"));
            Button("Odds","Rates / protection",ShowOdds);message=Label("Message","",12);Button("Close","Return",close);
            transaction=Make("Transaction",transform);var shade=transaction.gameObject.AddComponent<Image>();shade.color=new Color(.025f,.055f,.08f,1);
            transactionText=Label("TransactionText","",16,transaction);
            confirm=Button("Confirm","Confirm",ConfirmTransaction,transaction);
            Button("Back","Back to collection",()=>{transaction.gameObject.SetActive(false);Present();},transaction);
            transaction.gameObject.SetActive(false);selected=SkinCatalog.All[0].Id;
        }
        public void Open()
        {gameObject.SetActive(true);transaction.gameObject.SetActive(false);replaceId=null;page=0;LastResult=null;message.text="New skins are never equipped automatically.";Present();}
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
            confirm.GetComponentInChildren<Text>().text="Confirm";
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
                var index=page*8+i;cards[i].gameObject.SetActive(index<list.Length);if(index>=list.Length)continue;
                var s=list[index];var owns=d.OwnedIds.Contains(s.Id);var slot=Array.IndexOf(d.Equipment,s.Id);
                cards[i].GetComponentInChildren<Text>().text=ShortName(s.Id)+"\n"+(slot>=0?"Slot "+(slot+1):owns?"Owned":"Locked");
                cards[i].GetComponent<Image>().color=s.Id==selected?new Color(.18f,.45f,.53f):owns?new Color(.13f,.27f,.35f):new Color(.08f,.13f,.18f);
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
            elements["EquipmentNote"].GetComponent<Text>().text="NEXT attempt only  |  U "+u+"  |  "+(u<12?(next-u)+" to next price":"Final price tier");
            elements["Odds"].GetComponentInChildren<Text>().text="Rates / pity  G "+d.GoldDry+"/20  R "+d.RedDry+"/60";
        }
        public void Layout(Rect safe)
        {
            Place((RectTransform)transform,safe);var w=safe.width;var top=safe.height;
            At("Title",12,top-42,w-24,32);At("Balance",8,top-73,w-16,28);At("EquipmentNote",8,top-97,w-16,22);
            for(var i=0;i<5;i++)At("Slot"+i,12+i*(w-24)/5,top-151,(w-34)/5,48);
            At("Filter",12,top-195,92,38);At("Source",w-104,top-195,92,38);At("FilterSummary",106,top-195,w-212,38);
            for(var i=0;i<8;i++)At("Card"+i,12+i%4*(w-24)/4,top-259-i/4*60,(w-36)/4,54);
            At("Page",12,top-355,w-24,32);At("Preview",12,top-429,76,76);At("Rotate",12,top-454,36,23);At("Fire",52,top-454,36,23);At("Details",98,top-451,w-110,90);
            At("Equip",12,top-499,(w-30)/2,42);At("Exchange",18+(w-30)/2,top-499,(w-30)/2,42);
            At("Single",12,top-547,(w-30)/2,42);At("Ten",18+(w-30)/2,top-547,(w-30)/2,42);
            At("Free",12,top-591,w-24,38);At("Odds",12,top-631,w-24,36);
            At("Message",12,48,w-24,28);At("Close",12,6,w-24,38);
            var scale=Mathf.Min(1,(top-84)/631);
            foreach(var item in elements.Where(e=>e.Value.parent==transform && e.Key!="Message" && e.Key!="Close" && e.Key!="Transaction"))
            {var r=item.Value;Place(r,new Rect(r.anchoredPosition.x,top+(r.anchoredPosition.y-top)*scale,r.sizeDelta.x,r.sizeDelta.y*scale));}
            Place(transaction,new Rect(0,0,w,top));Place((RectTransform)transactionText.transform,new Rect(16,132,w-32,top-154));
            At("Confirm",16,74,w-32,46);At("Back",16,18,w-32,46);
        }
        private void At(string name,float x,float y,float w,float h)=>Place(elements[name],new Rect(x,y,w,h));
        private RectTransform Make(string name,Transform parent)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;elements[name]=r;return r;}
        private Text Label(string name,string value,int size,Transform parent=null)
        {var r=Make(name,parent??transform);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private Button Button(string name,string value,Action action,Transform parent=null)
        {var r=Make(name,parent??transform);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.13f,.27f,.35f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());var t=Label(name+"Label",value,13,r);t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;return b;}
        private static void Place(RectTransform r,Rect rect){r.anchoredPosition=rect.position;r.sizeDelta=rect.size;}
    }
}
