using System;
using System.Linq;
using Tidebound.Collection;

namespace Tidebound.Save
{
    public sealed partial class PlayerSaveService
    {
        public AppearanceDrawState AppearanceDrawState=>AppearanceDrawEngine.Replay(data.Collection,data.Appearance.DrawReceipts,data.CurrentLevel);
        public CollectionReceipt AppearanceReceiptFor(string request)=>data.Appearance.DrawReceipts.FirstOrDefault(r=>r.RequestId==request)?.Copy();
        public CollectionStatus CollectAppearance(string requestId,string kind,string target=null)
        {
            if(!IsAvailable)return CollectionStatus.StorageUnavailable;
            if(!Guid.TryParseExact(requestId,"N",out _))return CollectionStatus.InvalidRequest;
            var prior=data.Appearance.DrawReceipts.FirstOrDefault(r=>r.RequestId==requestId);
            if(prior!=null)return prior.Kind==kind&&(kind=="Exchange"?prior.SkinIds[0]==target:target==null)?CollectionStatus.AlreadySaved:CollectionStatus.RequestConflict;
            if(HasUniqueRequest(requestId)||data.Purchases.Any(r=>r.RequestId==requestId)||data.Collection.Receipts.Any(r=>r.RequestId==requestId)||data.Collection.EquipmentReceipts.Any(r=>r.RequestId==requestId))return CollectionStatus.RequestConflict;
            if(data.Appearance.UniqueReceipts.Length>0)return CollectionStatus.Locked;
            if(kind!="FirstBlue"&&kind!="Single"&&kind!="Ten"&&kind!="Exchange"||kind!="Exchange"&&target!=null)return CollectionStatus.InvalidSelection;
            if(data.CurrentLevel<3||kind=="Ten"&&data.CurrentLevel<4||kind=="Exchange"&&data.CurrentLevel<5)return CollectionStatus.Locked;
            if(Runtime!=null&&(Runtime.Movement.IsBusy||Runtime.PendingMoveId!=null||Runtime.Combat.IsVictorious&&!CurrentAttemptSettled))return CollectionStatus.Busy;
            var state=AppearanceDrawState;
            if(kind=="FirstBlue")
            {if(!CanClaimFirstBlue||!AppearanceDrawEngine.Pool.Any(x=>x.Rarity==SkinRarity.Uncommon&&!state.Owned.Contains(x.Id)))return CollectionStatus.InvalidSelection;}
            else if(CanClaimFirstBlue)return CollectionStatus.FirstBluePending;
            if(kind=="Exchange")
            {
                var item=AppearanceDrawEngine.Pool.FirstOrDefault(x=>x.Id==target);if(item==null||state.Owned.Contains(target))return CollectionStatus.InvalidSelection;
                if(state.Tickets<CollectionRules.ExchangeTickets(item.Rarity))return CollectionStatus.InsufficientTickets;
            }
            if((kind=="Single"||kind=="Ten")&&data.Coins<state.Price*(kind=="Ten"?9:1))return CollectionStatus.InsufficientCoins;
            var next=data.Copy();var receipt=AppearanceDrawEngine.Generate(state,data.Collection.ProfileSeed,requestId,kind,data.CurrentLevel,target);
            next.Coins-=receipt.CoinCost;next.Appearance.DrawReceipts=next.Appearance.DrawReceipts.Concat(new[]{receipt}).ToArray();
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            return Commit(next)?CollectionStatus.Saved:CollectionStatus.StorageUnavailable;
        }
    }
}
