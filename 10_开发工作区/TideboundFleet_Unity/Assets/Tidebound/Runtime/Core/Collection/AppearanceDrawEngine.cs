using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Tidebound.Collection
{
    public sealed class AppearanceDrawState
    {
        public HashSet<string> Owned=new HashSet<string>(StringComparer.Ordinal);
        public long Tickets,Draws;public int GoldDry,RedDry,DuplicateDry;public bool FirstBlueClaimed;
        public int Price=>CollectionRules.SinglePrice(Owned.Count(id=>AppearanceDrawEngine.Pool.Any(x=>x.Id==id)));
    }
    /// <summary>New 14-skin acquisition pool, retaining baseline prices and protection. Old receipts stay in their original ledger.</summary>
    public static class AppearanceDrawEngine
    {
        public const string Version="AppearanceSkinPoolV1_BaselinePrices";
        public static AppearanceDefinition[] Pool=>AppearanceCatalog.All.Where(x=>x.Kind==AppearanceKind.Skin&&x.Source==ShowcaseSourceGroup.Draw).ToArray();
        private static readonly int[] Weights={48,30,15,6,1};
        public static AppearanceDrawState Replay(CollectionData legacy,CollectionReceipt[] receipts,int currentLevel)
        {
            if(receipts==null)throw new ArgumentException("Missing appearance receipts.");
            var state=new AppearanceDrawState{Owned=new HashSet<string>(legacy.OwnedIds.Select(AppearanceCatalog.VisualSkin)),Tickets=legacy.Tickets,Draws=legacy.TotalDraws,
                GoldDry=legacy.GoldDry,RedDry=legacy.RedDry,DuplicateDry=legacy.DuplicateDry,FirstBlueClaimed=legacy.FirstBlueClaimed};
            var requests=new HashSet<string>();var lastLevel=3;
            foreach(var r in receipts)
            {
                if(r==null||!Guid.TryParseExact(r.RequestId,"N",out _)||!requests.Add(r.RequestId)||r.RulesVersion!=Version||r.AtLevel<lastLevel||r.AtLevel>currentLevel||r.SkinIds==null||
                    !DateTimeOffset.TryParseExact(r.AtUtc,"O",CultureInfo.InvariantCulture,DateTimeStyles.None,out var at)||at.Offset!=TimeSpan.Zero)throw new ArgumentException("Invalid appearance receipt.");
                lastLevel=r.AtLevel;
                var expected=Generate(state,legacy.ProfileSeed,r.RequestId,r.Kind,r.AtLevel,r.Kind=="Exchange"?r.SkinIds.FirstOrDefault():null);
                if(r.CoinCost!=expected.CoinCost||r.TicketCost!=expected.TicketCost||!r.SkinIds.SequenceEqual(expected.SkinIds))throw new ArgumentException("Appearance receipt does not match deterministic draw.");
            }
            return state;
        }
        public static CollectionReceipt Generate(AppearanceDrawState state,string seed,string request,string kind,int level,string target=null)
        {
            if(level<3||kind=="Ten"&&level<4||kind=="Exchange"&&level<5)throw new ArgumentException("Locked draw.");
            var r=new CollectionReceipt{RequestId=request,RulesVersion=Version,Kind=kind,AtLevel=level,AtUtc=DateTimeOffset.UtcNow.ToString("O",CultureInfo.InvariantCulture)};
            var random=new CollectionRandom(seed+":"+Version+":"+state.Draws.ToString(CultureInfo.InvariantCulture)+":"+kind);var pool=Pool;
            if(kind=="FirstBlue")
            {
                var fresh=pool.Where(x=>x.Rarity==SkinRarity.Uncommon&&!state.Owned.Contains(x.Id)).ToArray();
                if(state.FirstBlueClaimed||fresh.Length==0)throw new ArgumentException("Gift unavailable.");
                r.SkinIds=new[]{fresh[random.Next(fresh.Length)].Id};state.Owned.Add(r.SkinIds[0]);state.FirstBlueClaimed=true;return r;
            }
            if(!state.FirstBlueClaimed)throw new ArgumentException("Claim first blue first.");
            if(kind=="Exchange")
            {
                var item=pool.FirstOrDefault(x=>x.Id==target);if(item==null||state.Owned.Contains(target))throw new ArgumentException("Invalid exchange target.");
                r.TicketCost=CollectionRules.ExchangeTickets(item.Rarity);if(state.Tickets<r.TicketCost)throw new ArgumentException("Insufficient tickets.");
                state.Tickets-=r.TicketCost;state.Owned.Add(target);r.SkinIds=new[]{target};return r;
            }
            if(kind!="Single"&&kind!="Ten")throw new ArgumentException("Unknown draw kind.");
            var count=kind=="Ten"?10:1;r.CoinCost=state.Price*(count==10?9:1);r.SkinIds=new string[count];var high=false;
            for(var i=0;i<count;i++)
            {
                var minimum=CollectionDrawEngine.Required(state.GoldDry,state.RedDry,count==10&&i==9&&!high);
                var roll=random.Next(Weights.Skip((int)minimum).Sum());var rarity=minimum;
                while(roll>=Weights[(int)rarity]){roll-=Weights[(int)rarity];rarity++;}
                var options=pool.Where(x=>x.Rarity==rarity).ToArray();
                if(minimum==SkinRarity.Common&&state.DuplicateDry>=4&&rarity<=SkinRarity.Rare)
                {var fresh=pool.Where(x=>x.Rarity<=SkinRarity.Rare&&!state.Owned.Contains(x.Id)).ToArray();if(fresh.Length>0)options=fresh;}
                var item=options[random.Next(options.Length)];r.SkinIds[i]=item.Id;high|=item.Rarity>=SkinRarity.Rare;state.Draws=checked(state.Draws+1);
                state.GoldDry=item.Rarity>=SkinRarity.Epic?0:state.GoldDry+1;state.RedDry=item.Rarity==SkinRarity.Legendary?0:state.RedDry+1;
                if(state.Owned.Add(item.Id))state.DuplicateDry=0;
                else{state.Tickets=checked(state.Tickets+CollectionRules.DuplicateTickets(item.Rarity));state.DuplicateDry=Math.Min(4,state.DuplicateDry+1);}
            }
            return r;
        }
    }
}
