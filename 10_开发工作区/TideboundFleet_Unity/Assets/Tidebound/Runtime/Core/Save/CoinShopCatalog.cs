using System;
using System.Collections.Generic;
using System.Linq;

namespace Tidebound.Save
{
    public sealed class CoinShopProduct
    {
        public string Id { get; }
        public string Name { get; }
        public int Price { get; }
        public int Rescue { get; }
        public int Shuffle { get; }
        public int Reverse { get; }
        public CoinShopProduct(string id,string name,int price,int rescue,int shuffle,int reverse)
        {
            if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || price<1 || rescue<0 || shuffle<0 || reverse<0 ||
                rescue>999 || shuffle>999 || reverse>999 || rescue+shuffle+reverse<1)throw new ArgumentException("Invalid coin product.");
            Id=id;Name=name;Price=price;Rescue=rescue;Shuffle=shuffle;Reverse=reverse;
        }
    }
    /// <summary>Trusted, immutable local configuration. Callers submit an id, never a client-supplied price or quantity.</summary>
    public sealed class CoinShopCatalog
    {
        public string Version { get; }
        public int UnlockLevel { get; }
        public IReadOnlyList<CoinShopProduct> Products { get; }
        public CoinShopCatalog(string version,int unlockLevel,IEnumerable<CoinShopProduct> products)
        {
            if(string.IsNullOrWhiteSpace(version) || unlockLevel<3 || unlockLevel>10000 || products==null)throw new ArgumentException("Invalid shop configuration.");
            var items=products.ToArray();
            if(items.Length<1 || items.Length>4 || items.Any(p=>p==null) || items.Select(p=>p.Id).Distinct(StringComparer.Ordinal).Count()!=items.Length)
                throw new ArgumentException("Expected one to four unique coin products.");
            Version=version;UnlockLevel=unlockLevel;Products=Array.AsReadOnly(items);
        }
        public CoinShopProduct Find(string id) => Products.FirstOrDefault(p=>p.Id==id);
    }
    public enum CoinPurchaseStatus { Purchased,AlreadyPurchased,Locked,InsufficientCoins,InvalidRequest,InvalidProduct,RequestConflict,Busy,StorageUnavailable,InventoryLimit }
    public sealed class CoinPurchaseRecord
    {
        public string RequestId,ProductId,CatalogVersion;
        public int Price,Rescue,Shuffle,Reverse,AtLevel;
        public string InventoryReceipt => "coin:"+RequestId;
        public CoinPurchaseRecord Copy() => (CoinPurchaseRecord)MemberwiseClone();
        public void Validate()
        {
            if(!Guid.TryParseExact(RequestId,"N",out _) || string.IsNullOrWhiteSpace(ProductId) || string.IsNullOrWhiteSpace(CatalogVersion) || AtLevel<3 || AtLevel>10001)
                throw new ArgumentException("Invalid purchase receipt.");
            new CoinShopProduct(ProductId,ProductId,Price,Rescue,Shuffle,Reverse);
        }
    }
}
