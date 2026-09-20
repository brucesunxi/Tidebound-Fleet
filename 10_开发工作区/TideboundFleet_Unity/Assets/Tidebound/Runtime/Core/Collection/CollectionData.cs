using System;
using System.Linq;
using System.Collections.Generic;
using Tidebound.Core;

namespace Tidebound.Collection
{
    public sealed class CollectionReceipt
    {
        public string RequestId,Kind,AtUtc;
        public string RulesVersion=CollectionRules.Version;
        public int AtLevel,CoinCost,TicketCost;
        public string[] SkinIds=Array.Empty<string>();
        public CollectionReceipt Copy(){var c=(CollectionReceipt)MemberwiseClone();c.SkinIds=SkinIds?.ToArray();return c;}
    }
    public sealed class EquipmentReceipt
    {
        public string RequestId;
        public int AtLevel;
        public string[] Slots;
        public EquipmentReceipt Copy(){var c=(EquipmentReceipt)MemberwiseClone();c.Slots=Slots?.ToArray();return c;}
    }
    public sealed class CollectionData
    {
        public int Version=1;
        public string CatalogVersion=SkinCatalog.Version,RulesVersion=CollectionRules.Version;
        public string ProfileSeed=Guid.NewGuid().ToString("N");
        public string[] OwnedIds={FoundationLimits.DefaultStandardSkinId};
        // Null is an empty slot. Preserve all five positions, including holes.
        public string[] Equipment={FoundationLimits.DefaultStandardSkinId,null,null,null,null};
        public long Tickets,TotalDraws;
        public int GoldDry,RedDry,DuplicateDry;
        public bool FirstBlueClaimed;
        public CollectionReceipt[] Receipts=Array.Empty<CollectionReceipt>();
        public EquipmentReceipt[] EquipmentReceipts=Array.Empty<EquipmentReceipt>();
        public CollectionData Copy()
        {
            var c=(CollectionData)MemberwiseClone();c.OwnedIds=OwnedIds?.ToArray();c.Equipment=Equipment?.ToArray();
            c.Receipts=Receipts?.Select(r=>r?.Copy()).ToArray();c.EquipmentReceipts=EquipmentReceipts?.Select(r=>r?.Copy()).ToArray();return c;
        }
        public bool IsPristine => OwnedIds!=null && OwnedIds.SequenceEqual(new[]{FoundationLimits.DefaultStandardSkinId}) &&
            Equipment!=null && Equipment.SequenceEqual(new string[]{FoundationLimits.DefaultStandardSkinId,null,null,null,null}) &&
            Tickets==0 && TotalDraws==0 && GoldDry==0 && RedDry==0 && DuplicateDry==0 && !FirstBlueClaimed &&
            Receipts?.Length==0 && EquipmentReceipts?.Length==0;
        public bool CanClaimFirstBlue(int highestCleared)=>highestCleared>=2 && !FirstBlueClaimed;
        public string[] EffectiveEquipment()=>Equipment.Where(s=>s!=null).DefaultIfEmpty(FoundationLimits.DefaultStandardSkinId).ToArray();
        public long CoinSpent=>Receipts.Sum(r=>(long)r.CoinCost);
        public void Validate(int currentLevel)
        {
            if(Version!=1 || CatalogVersion!=SkinCatalog.Version || RulesVersion!=CollectionRules.Version ||
                !Guid.TryParseExact(ProfileSeed,"N",out _) || OwnedIds==null || Receipts==null || EquipmentReceipts==null ||
                OwnedIds.Any(id=>SkinCatalog.Find(id)==null) || OwnedIds.Distinct(StringComparer.Ordinal).Count()!=OwnedIds.Length ||
                Receipts.Any(r=>r==null) || EquipmentReceipts.Any(r=>r==null))throw new ArgumentException("Invalid collection profile.");
            var ids=Receipts.Select(r=>r.RequestId).Concat(EquipmentReceipts.Select(r=>r.RequestId)).ToArray();
            if(ids.Any(id=>!Guid.TryParseExact(id,"N",out _)) || ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length)
                throw new ArgumentException("Invalid or duplicate collection request.");
            var owned=new HashSet<string>(StringComparer.Ordinal){FoundationLimits.DefaultStandardSkinId};
            long tickets=0,draws=0;int gold=0,red=0,duplicates=0;bool free=false;
            foreach(var r in Receipts)
            {
                if(r.RulesVersion!=CollectionRules.Version || r.AtLevel<3 || r.AtLevel>currentLevel || r.SkinIds==null ||
                    !DateTimeOffset.TryParseExact(r.AtUtc,"O",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var at) || at.Offset!=TimeSpan.Zero ||
                    r.SkinIds.Any(id=>SkinCatalog.Find(id)==null || SkinCatalog.Find(id).IsDefault))throw new ArgumentException("Invalid collection receipt.");
                var count=r.Kind=="Ten" ? 10 : 1;
                if(r.SkinIds.Length!=count)throw new ArgumentException("Invalid collection result count.");
                if(r.Kind=="FirstBlue")
                {
                    if(free || r.CoinCost!=0 || r.TicketCost!=0 || owned.Contains(r.SkinIds[0]) || SkinCatalog.Find(r.SkinIds[0]).Rarity!=SkinRarity.Uncommon)
                        throw new ArgumentException("Invalid first blue receipt.");
                    owned.Add(r.SkinIds[0]);free=true;continue; // Introductory gift is separate from normal draws.
                }
                if(r.Kind=="Exchange")
                {
                    var cost=CollectionRules.ExchangeTickets(SkinCatalog.Find(r.SkinIds[0]).Rarity);
                    if(r.AtLevel<5 || r.CoinCost!=0 || r.TicketCost!=cost || tickets<cost || !owned.Add(r.SkinIds[0]))throw new ArgumentException("Invalid collection exchange.");
                    tickets-=cost;continue;
                }
                if((r.Kind!="Single" && r.Kind!="Ten") || (r.Kind=="Ten" && r.AtLevel<4) || r.TicketCost!=0 ||
                    r.CoinCost!=CollectionRules.SinglePrice(owned.Count-1)*(count==10 ? 9 : 1))throw new ArgumentException("Invalid collection draw price.");
                var highInBatch=false;
                for(var i=0;i<count;i++)
                {
                    var skin=SkinCatalog.Find(r.SkinIds[i]);var rarity=skin.Rarity;
                    var required=CollectionDrawEngine.Required(gold,red,count==10 && i==9 && !highInBatch);
                    if(rarity<required || (required==SkinRarity.Common && duplicates>=4 && rarity<=SkinRarity.Rare && owned.Contains(skin.Id) &&
                        SkinCatalog.All.Any(s=>!s.IsDefault && s.Rarity<=SkinRarity.Rare && !owned.Contains(s.Id))))throw new ArgumentException("Collection result breaks protection rules.");
                    highInBatch|=rarity>=SkinRarity.Rare;draws=checked(draws+1);
                    gold=rarity>=SkinRarity.Epic ? 0 : gold+1;red=rarity==SkinRarity.Legendary ? 0 : red+1;
                    if(owned.Add(skin.Id))duplicates=0;
                    else{tickets=checked(tickets+CollectionRules.DuplicateTickets(rarity));duplicates=Math.Min(4,duplicates+1);}
                }
            }
            if(!owned.SetEquals(OwnedIds) || Tickets!=tickets || TotalDraws!=draws || GoldDry!=gold || RedDry!=red || DuplicateDry!=duplicates || FirstBlueClaimed!=free)
                throw new ArgumentException("Collection totals disagree with receipts.");
            ValidateSlots(Equipment,owned);
            foreach(var r in EquipmentReceipts){if(r.AtLevel<3 || r.AtLevel>currentLevel)throw new ArgumentException("Invalid equipment unlock.");ValidateSlots(r.Slots,owned);}
            var expected=EquipmentReceipts.Length==0 ? new string[]{FoundationLimits.DefaultStandardSkinId,null,null,null,null} : EquipmentReceipts.Last().Slots;
            if(!Equipment.SequenceEqual(expected))throw new ArgumentException("Equipment receipt mismatch.");
        }
        internal static void ValidateSlots(string[] slots,ICollection<string> owned)
        {
            if(slots==null || slots.Length!=5 || slots.Where(s=>s!=null).Any(s=>!owned.Contains(s)) ||
                slots.Where(s=>s!=null).Distinct(StringComparer.Ordinal).Count()!=slots.Count(s=>s!=null))throw new ArgumentException("Invalid equipment slots.");
        }
    }
}
