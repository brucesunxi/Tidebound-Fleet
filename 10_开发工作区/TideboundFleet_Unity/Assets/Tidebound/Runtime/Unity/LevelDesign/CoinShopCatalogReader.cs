using System;
using System.Linq;
using Newtonsoft.Json;
using Tidebound.Save;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    public static class CoinShopCatalogReader
    {
        [Serializable] private sealed class Data {public string version;public int unlockLevel;public Product[] products;}
        [Serializable] private sealed class Product {public string id,name;public int price,rescue,shuffle,reverse;}
        public static CoinShopCatalog Read(string json)
        {
            var data=JsonConvert.DeserializeObject<Data>(json);
            if(data?.products==null || data.products.Any(p=>p==null))throw new ArgumentException("Missing coin shop configuration.");
            return new CoinShopCatalog(data.version,data.unlockLevel,data.products.Select(p=>new CoinShopProduct(p.id,p.name,p.price,p.rescue,p.shuffle,p.reverse)));
        }
        public static CoinShopCatalog LoadDefault()
        {
            var asset=Resources.Load<TextAsset>("CoinToolShop");
            if(asset==null)throw new InvalidOperationException("Coin shop configuration not found.");
            return Read(asset.text);
        }
    }
}
