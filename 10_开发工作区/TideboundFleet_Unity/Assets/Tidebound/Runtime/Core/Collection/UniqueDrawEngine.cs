using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Tidebound.Collection
{
    public enum DrawPool { Skin, Trail, Showcase, Scene }
    public sealed class UniqueDrawItem
    {
        public string Id,Name,ChineseName; public SkinRarity Rarity; public DrawPool Pool;
    }
    public sealed class UniqueDrawReceipt
    {
        public string Version=UniqueDrawEngine.Version,RequestId,ItemId;
        public DrawPool Pool; public int Ordinal,CoinCost,AtLevel;
        public UniqueDrawReceipt Copy()=>(UniqueDrawReceipt)MemberwiseClone();
    }
    public sealed class UniqueDrawQuote
    {
        public DrawPool Pool; public int PaidCount,Price,EligibilityOrdinal;
        public UniqueDrawItem[] Remaining,Eligible;
        public double Probability(string id)
        {
            var item=Eligible.FirstOrDefault(x=>x.Id==id);if(item==null)return 0;
            var sum=Eligible.Select(x=>x.Rarity).Distinct().Sum(x=>UniqueDrawEngine.Weight(x));
            return (double)UniqueDrawEngine.Weight(item.Rarity)/sum/Eligible.Count(x=>x.Rarity==item.Rarity);
        }
    }
    // Frozen launch pool and receipt algorithm. Additions require a new catalog version, never a counter reset.
    public static class UniqueDrawEngine
    {
        public const string Version="UniqueDrawV1_20260924";
        private static readonly int[][] Prices={new[]{200,300,500,800,1200,1800,2600,3800,5400,7500},new[]{300,600,1200,2200,3600,5000,6500,7500},new[]{500,800,1300,2000,2900,4000,5200,6500,7500}};
        public static readonly UniqueDrawItem[] Items=Build();
        private static UniqueDrawItem[] Build()
        {
            var list=AppearanceCatalog.All.Where(x=>x.Kind==AppearanceKind.Skin&&x.Source==ShowcaseSourceGroup.Draw || x.Kind==AppearanceKind.Trail&&new[]{"04","08","10","11","12"}.Contains(x.Id.Substring(x.Id.Length-2)))
                .Select(x=>new UniqueDrawItem{Id=x.Id,Name=x.Name,ChineseName=x.ChineseName,Rarity=x.Rarity,Pool=x.Kind==AppearanceKind.Skin?DrawPool.Skin:DrawPool.Trail}).ToList();
            foreach(var x in ShowcaseCatalog.All.Where(x=>x.SourceGroup==ShowcaseSourceGroup.Draw))
            {var n=x.VisualIndex+1;list.Add(new UniqueDrawItem{Id=x.Id,Name=x.Name,ChineseName=x.ChineseName,Pool=DrawPool.Showcase,Rarity=(SkinRarity)(n==22?4:new[]{15,17,21}.Contains(n)?3:new[]{13,19,20,24}.Contains(n)?2:1)});}
            return list.ToArray();
        }
        public static int Price(DrawPool pool,int paidCount)
        {if(pool==DrawPool.Scene)return 0;if((int)pool<0||(int)pool>2||paidCount<0)throw new ArgumentException("Invalid draw counter.");var table=Prices[(int)pool];return table[Math.Min(paidCount,table.Length-1)];}
        public static int Weight(SkinRarity rarity)=>new[]{48,30,15,6,1}[(int)rarity];
        public static int OpensAt(DrawPool pool,SkinRarity rarity)=>rarity==SkinRarity.Legendary?(pool==DrawPool.Trail?4:10):rarity==SkinRarity.Epic?(pool==DrawPool.Trail?3:6):1;
        private static HashSet<string> BaseOwned(CollectionData legacy,AppearanceData appearance)=>new HashSet<string>(legacy.OwnedIds.Select(AppearanceCatalog.VisualSkin).Concat(appearance.DrawReceipts.SelectMany(x=>x.SkinIds)));
        public static UniqueDrawQuote Quote(DrawPool pool,CollectionData legacy,AppearanceData appearance)
        {
            var baseline=BaseOwned(legacy,appearance);var paid=appearance.UniqueReceipts.Count(x=>x.Pool==pool);
            // One-time carry-forward of old entitlements affects eligibility only, never the new price counter.
            var carried=pool==DrawPool.Skin?Math.Max(0,Items.Count(x=>x.Pool==pool&&baseline.Contains(x.Id))-(legacy.FirstBlueClaimed||appearance.DrawReceipts.Any(x=>x.Kind=="FirstBlue")?1:0)):0;
            var oldGate=Items.Where(x=>x.Pool==pool&&baseline.Contains(x.Id)).Select(x=>OpensAt(pool,x.Rarity)).DefaultIfEmpty(1).Max();
            var ordinal=Math.Max(paid+carried+1,oldGate);
            var owned=new HashSet<string>(baseline.Concat(appearance.UniqueReceipts.Select(x=>x.ItemId)));
            var remaining=Items.Where(x=>x.Pool==pool&&!owned.Contains(x.Id)).ToArray();
            return new UniqueDrawQuote{Pool=pool,PaidCount=paid,Price=Price(pool,paid),EligibilityOrdinal=ordinal,Remaining=remaining,Eligible=remaining.Where(x=>OpensAt(pool,x.Rarity)<=ordinal).ToArray()};
        }
        public static UniqueDrawReceipt Generate(DrawPool pool,CollectionData legacy,AppearanceData appearance,string request,int level)
        {
            var q=Quote(pool,legacy,appearance);if(q.Eligible.Length==0)throw new ArgumentException("No eligible reward.");
            byte[] hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(Encoding.UTF8.GetBytes(Version+"|"+legacy.ProfileSeed+"|"+request+"|"+pool+"|"+q.PaidCount));
            var roll=((uint)hash[0]<<24|(uint)hash[1]<<16|(uint)hash[2]<<8|hash[3])/4294967296d;
            var item=q.Eligible.Last();foreach(var x in q.Eligible){roll-=q.Probability(x.Id);if(roll<0){item=x;break;}}
            return new UniqueDrawReceipt{RequestId=request,Pool=pool,Ordinal=q.PaidCount+1,CoinCost=q.Price,ItemId=item.Id,AtLevel=level};
        }
        public static void Validate(CollectionData legacy,AppearanceData appearance,int level)
        {
            if(appearance.UniqueReceipts==null)throw new ArgumentException("Missing draw ledger.");
            var replay=new AppearanceData{DrawReceipts=appearance.DrawReceipts};var ids=new HashSet<string>();var lastLevel=3;
            foreach(var receipt in appearance.UniqueReceipts)
            {
                if(receipt==null||receipt.Version!=Version||!Guid.TryParseExact(receipt.RequestId,"N",out _)||!ids.Add(receipt.RequestId)||receipt.AtLevel<lastLevel||receipt.AtLevel>level)throw new ArgumentException("Invalid unique draw receipt.");
                var expected=Generate(receipt.Pool,legacy,replay,receipt.RequestId,receipt.AtLevel);
                if(receipt.Ordinal!=expected.Ordinal||receipt.CoinCost!=expected.CoinCost||receipt.ItemId!=expected.ItemId)throw new ArgumentException("Unique draw replay mismatch.");
                replay.UniqueReceipts=replay.UniqueReceipts.Concat(new[]{receipt}).ToArray();lastLevel=receipt.AtLevel;
            }
        }
    }
}
