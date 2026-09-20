using System;
using System.Collections.Generic;
using Tidebound.Save;
using Tidebound.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Explicit product selection and confirmation. Repeated callbacks retain the same request id.</summary>
    public sealed class CoinShopPanel : MonoBehaviour
    {
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
            this.service=service;this.inventory=inventory;this.catalog=catalog;this.font=font;this.usesLeft=usesLeft;
            title=Label("ShopTitle","Tool shop",22);balance=Label("Balance","",17);stock=Label("Stock","",12);
            message=Label("Message","",13);details=Label("Details","",17);
            foreach(var product in catalog.Products)
            {var id=product.Id;products.Add(Button("Product_"+id,product.Name+"  |  "+product.Price+" coins",()=>SelectProduct(id)));}
            confirm=Button("ConfirmPurchase","Buy",ConfirmPurchase);
            back=Button("BackToProducts","Back",ShowProducts);
            ad=Button("Ad","Watch ad - coming later",null);ad.interactable=false;
            google=Button("GooglePlay","Google Play pack - coming later",null);google.interactable=false;
            close=Button("CloseShop","Return to game",onClose);
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
            foreach(var button in products)button.interactable=unlocked;
            if(usesLeft?.Invoke()==0 && !completed)message.text="Tool limit reached. Purchases are for your next attempt.";
            if(selected==null)return;
            confirm.interactable=unlocked && !completed && service.Coins>=selected.Price;
            confirm.GetComponentInChildren<Text>().text=completed ? "Purchased" : "Buy for "+selected.Price+" coins";
            if(unlocked && !completed && service.Coins<selected.Price)message.text="Need "+(selected.Price-service.Coins)+" more coins. Pending coins cannot be spent.";
        }
        public void Layout(Rect safe)
        {
            var root=(RectTransform)transform;Place(root,safe);var w=safe.width;var y=safe.height/2;
            Place(title.rectTransform,new Rect(16,y+242,w-32,32));Place(balance.rectTransform,new Rect(16,y+206,w-32,28));
            Place(stock.rectTransform,new Rect(8,y+178,w-16,24));Place(message.rectTransform,new Rect(16,y+128,w-32,44));
            for(var i=0;i<products.Count;i++)Place((RectTransform)products[i].transform,new Rect(24,y+68-i*56,w-48,48));
            Place(details.rectTransform,new Rect(20,y+12,w-40,104));Place((RectTransform)confirm.transform,new Rect(24,y-54,w-48,48));
            Place((RectTransform)back.transform,new Rect(24,y-110,w-48,48));
            Place((RectTransform)ad.transform,new Rect(24,y-168,w-48,44));Place((RectTransform)google.transform,new Rect(24,y-220,w-48,44));
            Place((RectTransform)close.transform,new Rect(24,y-274,w-48,48));
        }
        private static string Contents(CoinShopProduct p)
        {
            var parts=new List<string>();if(p.Rescue>0)parts.Add("Rescue x"+p.Rescue);if(p.Shuffle>0)parts.Add("Shuffle x"+p.Shuffle);if(p.Reverse>0)parts.Add("Reverse x"+p.Reverse);
            return string.Join(" / ",parts);
        }
        private RectTransform Rect(string name)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(transform,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;return r;}
        private Text Label(string name,string text,int size)
        {var r=Rect(name);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=size;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        private Button Button(string name,string text,Action action)
        {
            var r=Rect(name);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.13f,.27f,.35f);
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;if(action!=null)b.onClick.AddListener(()=>action());
            var t=Label(name+"Label",text,14);t.transform.SetParent(r,false);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;
            t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;return b;
        }
        private static void Place(RectTransform r,Rect value){r.anchoredPosition=value.position;r.sizeDelta=value.size;}
    }
}
