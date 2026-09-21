using System;
using System.Collections.Generic;
using Tidebound.Save;
using Tidebound.Tools;
using UnityEngine;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Explicit product selection and confirmation. Repeated callbacks retain the same request id.</summary>
    public sealed class CoinShopPanel : MonoBehaviour
    {
        private RectTransform productContent,productViewport;
        private Button collectionTab,drawTab;
        private Action openCollection,openDraw;
        public void ConfigureNavigation(Action collection,Action draw) {openCollection=collection;openDraw=draw;}
        private PlayerSaveService service;
        private ToolInventory inventory;
        private CoinShopCatalog catalog;
        private Font font;
        private Func<int> usesLeft;
        private Text title,balance,stock,message,details;
        private Button confirm,back,close,ad,google;
        private readonly List<Button> products=new List<Button>();
        private CoinShopProduct selected;
        private string requestId;
        private bool completed;
        public CoinPurchaseStatus? LastResult {get;private set;}
        public string SelectedProductId=>selected?.Id;
        public void Initialize(PlayerSaveService service,ToolInventory inventory,CoinShopCatalog catalog,Font font,Action onClose,Func<int> usesLeft=null)
        {
            this.service=service;this.inventory=inventory;this.catalog=catalog;this.font=HarborUI.Font;
            if(GetComponent<Image>()!=null)GetComponent<Image>().color=HarborUI.Cream;this.usesLeft=usesLeft;
            title=Label("ShopTitle","Tool supplies",24);var header=HarborUI.Surface("TitlePlate",transform,HarborUI.Wood);header.raycastTarget=false;header.transform.SetAsFirstSibling();title.color=HarborUI.Cream;balance=Label("Balance","",17);stock=Label("Stock","",12);
            message=Label("Message","",13);details=Label("Details","",17);
            productViewport=(RectTransform)HarborUI.Scroll("ProductScroll",transform,out productContent).transform;
            foreach(var product in catalog.Products)
            {
                var id=product.Id;var b=Button("Product_"+id,"",()=>SelectProduct(id));b.transform.SetParent(productContent,false);products.Add(b);
                b.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;
            }
            confirm=Button("ConfirmPurchase","Buy",ConfirmPurchase);
            back=Button("BackToProducts","Back",ShowProducts);
            ad=Button("Ad","Watch ad - coming later",null);ad.interactable=false;
            google=Button("GooglePlay","Google Play pack - coming later",null);google.interactable=false;
            close=Button("CloseShop","×",onClose);
            collectionTab=Button("CollectionTab","Collection",()=>openCollection?.Invoke());drawTab=Button("DrawTab","Draw",()=>openDraw?.Invoke());
            close.GetComponent<Image>().color=new Color(.93f,.29f,.22f);
            var closeLabel=close.GetComponentInChildren<Text>();closeLabel.fontSize=27;closeLabel.color=Color.white;
            closeLabel.rectTransform.offsetMin=new Vector2(4,2);closeLabel.rectTransform.offsetMax=new Vector2(-4,-2);
            ShowProducts();
        }
        public void Open(ShipTool preferred=ShipTool.None)
        {
            gameObject.SetActive(true);ShowProducts();
            message.text=service==null ? "Review mode. Purchases unavailable." : !service.IsAvailable ? "Save unavailable. Purchases disabled." :
                service.CurrentLevel<catalog.UnlockLevel ? "Unlocks at level "+catalog.UnlockLevel+"." : "Only settled coins can be spent.";
            if(preferred!=ShipTool.None && service?.IsAvailable==true)
                message.text="Out of "+preferred+". Purchase adds stock; use it when ready.";
            Present();
        }
        public void SelectProduct(string id)
        {
            var product=catalog.Find(id);if(product==null)return;
            selected=product;requestId=Guid.NewGuid().ToString("N");completed=false;LastResult=null;
            foreach(var button in products)button.gameObject.SetActive(false);
            details.gameObject.SetActive(true);confirm.gameObject.SetActive(true);back.gameObject.SetActive(true);
            details.text=product.Name+"\n"+Contents(product)+"\nCost: "+product.Price+" coins";
            message.text="Added to inventory. Does not use the tool.";Present();
        }
        public void ConfirmPurchase()
        {
            if(selected==null || completed)return;
            LastResult=service==null ? CoinPurchaseStatus.StorageUnavailable : service.BuyWithCoins(requestId,selected.Id,catalog);
            completed=LastResult==CoinPurchaseStatus.Purchased || LastResult==CoinPurchaseStatus.AlreadyPurchased;
            message.text=completed ? "Purchased. Inventory saved." : LastResult==CoinPurchaseStatus.InsufficientCoins ? "Not enough settled coins." :
                LastResult==CoinPurchaseStatus.StorageUnavailable ? "Save failed. Nothing charged. Retry or return." : "Purchase unavailable: "+LastResult;
            Present();
        }
        public void ShowProducts()
        {
            selected=null;requestId=null;completed=false;LastResult=null;
            foreach(var button in products)button.gameObject.SetActive(true);
            details.gameObject.SetActive(false);confirm.gameObject.SetActive(false);back.gameObject.SetActive(false);
            message.text="Only settled coins can be spent.";Present();
        }
        public void Present()
        {
            balance.text=service?.IsAvailable==true ? "Coins: "+service.Coins : "Coins unavailable";
            if(usesLeft!=null)balance.text+="  |  Uses left: "+usesLeft()+"/"+ShipToolSystem.MaxUsesPerAttempt;
            stock.text="Stock  Rescue "+inventory.Count(ShipTool.Rescue)+"  |  Shuffle "+inventory.Count(ShipTool.Shuffle)+"  |  Reverse "+inventory.Count(ShipTool.Reverse);
            var unlocked=service?.IsAvailable==true && service.CurrentLevel>=catalog.UnlockLevel;
            for(var i=0;i<products.Count;i++)
            {
                var p=catalog.Products[i];products[i].interactable=unlocked;
                var description=p.Id=="rescue_1"?"Random outer 2 ships":p.Id=="shuffle_1"?"Shuffle up to 5 ships":p.Id=="reverse_1"?"Select a ship; reverse 180 degrees":"One of each tool";
                var count=p.Id=="rescue_1"?inventory.Count(ShipTool.Rescue):p.Id=="shuffle_1"?inventory.Count(ShipTool.Shuffle):p.Id=="reverse_1"?inventory.Count(ShipTool.Reverse):-1;
                products[i].GetComponentInChildren<Text>().text=p.Name+"\n"+description+"\n"+(count<0?"Use when ready":"In stock: "+count)+"     |     "+p.Price+" coins";
            }
            if(usesLeft?.Invoke()==0 && !completed)message.text="Tool limit reached. Purchases are for your next attempt.";
            if(selected==null)return;
            confirm.interactable=unlocked && !completed && service.Coins>=selected.Price;
            confirm.GetComponentInChildren<Text>().text=completed ? "Purchased" : "Buy for "+selected.Price+" coins";
            if(unlocked && !completed && service.Coins<selected.Price)message.text="Need "+(selected.Price-service.Coins)+" more coins. Pending coins cannot be spent.";
        }
        public void Layout(Rect safe)
        {
            safe=new Rect(safe.x+8,safe.y+8,safe.width-16,safe.height-16);var root=(RectTransform)transform;Place(root,safe);var w=safe.width;var h=safe.height;
            Place((RectTransform)transform.Find("TitlePlate"),new Rect(12,h-68,w-84,58));Place(title.rectTransform,new Rect(16,h-62,w-90,48));Place((RectTransform)close.transform,new Rect(w-66,h-62,52,48));
            Place(balance.rectTransform,new Rect(16,h-104,w-32,32));Place(stock.rectTransform,new Rect(12,h-138,w-24,28));
            Place(message.rectTransform,new Rect(18,142,w-36,54));
            Place(productViewport,new Rect(14,210,w-28,h-362));productContent.sizeDelta=new Vector2(w-28,products.Count*126);
            for(var i=0;i<products.Count;i++)Place((RectTransform)products[i].transform,new Rect(2,products.Count*126-(i+1)*126+6,w-32,118));
            Place(details.rectTransform,new Rect(24,h/2-10,w-48,160));Place((RectTransform)confirm.transform,new Rect(24,h/2-76,w-48,54));
            Place((RectTransform)back.transform,new Rect(24,h/2-138,w-48,50));
            Place((RectTransform)ad.transform,new Rect(16,80,(w-38)/2,52));Place((RectTransform)google.transform,new Rect(22+(w-38)/2,80,(w-38)/2,52));
            Place((RectTransform)collectionTab.transform,new Rect(16,14,(w-38)/2,54));Place((RectTransform)drawTab.transform,new Rect(22+(w-38)/2,14,(w-38)/2,54));
        }

        private static string Contents(CoinShopProduct p)
        {
            var parts=new List<string>();if(p.Rescue>0)parts.Add("Rescue x"+p.Rescue);if(p.Shuffle>0)parts.Add("Shuffle x"+p.Shuffle);if(p.Reverse>0)parts.Add("Reverse x"+p.Reverse);
            return string.Join(" / ",parts);
        }
        private RectTransform Rect(string name)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(transform,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;return r;}
        private Text Label(string name,string text,int size)
        {var r=Rect(name);var t=r.gameObject.AddComponent<HarborText>();t.font=font;t.text=text;t.fontSize=Math.Max(14,size);t.color=HarborUI.Ink;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private Button Button(string name,string text,Action action)
        {
            var r=Rect(name);var image=r.gameObject.AddComponent<HarborImage>();image.color=HarborUI.Aqua;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;if(action!=null)b.onClick.AddListener(()=>action());
            var t=Label(name+"Label",text,14);t.transform.SetParent(r,false);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;
            t.rectTransform.offsetMin=new Vector2(14,6);t.rectTransform.offsetMax=new Vector2(-12,-6);return b;
        }
        private static void Place(RectTransform r,Rect value){r.anchoredPosition=value.position;r.sizeDelta=value.size;}
    }
}
