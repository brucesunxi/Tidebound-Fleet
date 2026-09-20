using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Tidebound.Collection
{
    // Stable across platforms and reopen/retry. Request ids are deliberately excluded from randomness.
    public sealed class CollectionRandom
    {
        private uint state;
        public CollectionRandom(string seed)
        {
            using(var hash=SHA256.Create())
            {var b=hash.ComputeHash(Encoding.UTF8.GetBytes(seed));state=(uint)b[0]|((uint)b[1]<<8)|((uint)b[2]<<16)|((uint)b[3]<<24);}
            if(state==0)state=0x9e3779b9;
        }
        public int Next(int max)
        {if(max<=0)throw new ArgumentOutOfRangeException(nameof(max));state^=state<<13;state^=state>>17;state^=state<<5;return (int)((ulong)state*(uint)max>>32);}
    }
    public static class CollectionDrawEngine
    {
        private static readonly int[] Weights={48,30,15,6,1};
        public static SkinRarity Required(int gold,int red,bool tenthWithoutPurple)=>red>=59 ? SkinRarity.Legendary : gold>=19 ? SkinRarity.Epic : tenthWithoutPurple ? SkinRarity.Rare : SkinRarity.Common;
        public static int CoinCap(SkinRarity rarity)=>new[]{1,2,3,5,8}[(int)rarity];
        public static SkinDefinition Pick(CollectionData data,CollectionRandom random,SkinRarity minimum)
        {
            var total=Weights.Skip((int)minimum).Sum();var roll=random.Next(total);var rarity=minimum;
            while(roll>=Weights[(int)rarity]){roll-=Weights[(int)rarity];rarity++;}
            var pool=SkinCatalog.All.Where(s=>!s.IsDefault && s.Rarity==rarity).ToArray();
            if(minimum==SkinRarity.Common && data.DuplicateDry>=4 && rarity<=SkinRarity.Rare)
            {
                var fresh=SkinCatalog.All.Where(s=>!s.IsDefault && s.Rarity<=SkinRarity.Rare && !data.OwnedIds.Contains(s.Id)).ToArray();
                if(fresh.Length>0)pool=fresh;
            }
            return pool[random.Next(pool.Length)];
        }
        public static void ApplyDraw(CollectionData data,SkinDefinition skin)
        {
            data.TotalDraws=checked(data.TotalDraws+1);
            data.GoldDry=skin.Rarity>=SkinRarity.Epic ? 0 : data.GoldDry+1;
            data.RedDry=skin.Rarity==SkinRarity.Legendary ? 0 : data.RedDry+1;
            if(data.OwnedIds.Contains(skin.Id)){data.Tickets=checked(data.Tickets+CollectionRules.DuplicateTickets(skin.Rarity));data.DuplicateDry=Math.Min(4,data.DuplicateDry+1);}
            else{data.OwnedIds=data.OwnedIds.Concat(new[]{skin.Id}).ToArray();data.DuplicateDry=0;}
        }
        // Mutates only a detached candidate. Callers must commit before revealing this receipt.
        public static CollectionReceipt Generate(CollectionData data,string requestId,string kind,int level,string target=null)
        {
            var r=new CollectionReceipt{RequestId=requestId,Kind=kind,AtLevel=level,AtUtc=DateTimeOffset.UtcNow.ToString("O",System.Globalization.CultureInfo.InvariantCulture)};
            var random=new CollectionRandom(data.ProfileSeed+":"+CollectionRules.Version+":"+data.TotalDraws.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+kind);
            if(kind=="FirstBlue")
            {
                var pool=SkinCatalog.All.Where(s=>s.Rarity==SkinRarity.Uncommon && !data.OwnedIds.Contains(s.Id)).ToArray();
                if(data.FirstBlueClaimed || pool.Length==0)throw new ArgumentException("First blue unavailable.");
                r.SkinIds=new[]{pool[random.Next(pool.Length)].Id};data.OwnedIds=data.OwnedIds.Concat(r.SkinIds).ToArray();data.FirstBlueClaimed=true;
            }
            else if(kind=="Exchange")
            {
                var skin=SkinCatalog.Find(target);
                if(skin==null || skin.IsDefault || data.OwnedIds.Contains(target))throw new ArgumentException("Exchange unavailable.");
                r.TicketCost=CollectionRules.ExchangeTickets(skin.Rarity);
                if(data.Tickets<r.TicketCost)throw new ArgumentException("Insufficient tickets.");
                r.SkinIds=new[]{target};data.Tickets-=r.TicketCost;data.OwnedIds=data.OwnedIds.Concat(r.SkinIds).ToArray();
            }
            else
            {
                if(kind!="Single" && kind!="Ten")throw new ArgumentException("Unknown draw kind.");
                var count=kind=="Ten" ? 10 : 1;r.CoinCost=CollectionRules.SinglePrice(data.OwnedIds.Length-1)*(count==10?9:1);r.SkinIds=new string[count];
                var high=false;
                for(var i=0;i<count;i++)
                {var skin=Pick(data,random,Required(data.GoldDry,data.RedDry,count==10 && i==9 && !high));r.SkinIds[i]=skin.Id;high|=skin.Rarity>=SkinRarity.Rare;ApplyDraw(data,skin);}
            }
            data.Receipts=data.Receipts.Concat(new[]{r}).ToArray();return r;
        }
    }
}
