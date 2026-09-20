using System;
using System.Linq;
using Tidebound.Collection;
using Tidebound.Config;
using Tidebound.Lane;
using Tidebound.Combat;

namespace Tidebound.Save
{
    public enum CollectionStatus { Saved, AlreadySaved, Locked, InvalidRequest, InvalidSelection, RequestConflict, Busy, InsufficientCoins, InsufficientTickets, FirstBluePending, StorageUnavailable }
    public sealed partial class PlayerSaveService
    {
        public CollectionReceipt CollectionReceiptFor(string requestId)=>data.Collection.Receipts.FirstOrDefault(r=>r.RequestId==requestId)?.Copy();
        public CollectionStatus Collect(string requestId,string kind,string target=null)
        {
            if(!IsAvailable)return CollectionStatus.StorageUnavailable;
            if(!Guid.TryParseExact(requestId,"N",out _))return CollectionStatus.InvalidRequest;
            var prior=data.Collection.Receipts.FirstOrDefault(r=>r.RequestId==requestId);
            if(prior!=null)return prior.Kind==kind && (kind=="Exchange" ? prior.SkinIds[0]==target : target==null) ? CollectionStatus.AlreadySaved : CollectionStatus.RequestConflict;
            if(data.Purchases.Any(r=>r.RequestId==requestId) || data.Collection.EquipmentReceipts.Any(r=>r.RequestId==requestId))return CollectionStatus.RequestConflict;
            if(kind!="FirstBlue" && kind!="Single" && kind!="Ten" && kind!="Exchange" || kind!="Exchange" && target!=null)return CollectionStatus.InvalidSelection;
            if(data.CurrentLevel<3 || kind=="Ten" && data.CurrentLevel<4 || kind=="Exchange" && data.CurrentLevel<5)return CollectionStatus.Locked;
            if(Runtime!=null && (data.Attempt?.AttemptId!=Runtime.Session.SessionId || Runtime.Movement.IsBusy || Runtime.PendingMoveId!=null || Runtime.Combat.IsVictorious && !CurrentAttemptSettled))return CollectionStatus.Busy;
            if(kind=="FirstBlue")
            {if(!CanClaimFirstBlue || !SkinCatalog.All.Any(s=>s.Rarity==SkinRarity.Uncommon && !data.Collection.OwnedIds.Contains(s.Id)))return CollectionStatus.InvalidSelection;}
            else if(CanClaimFirstBlue)return CollectionStatus.FirstBluePending;
            if(kind=="Exchange")
            {
                var skin=SkinCatalog.Find(target);if(skin==null || skin.IsDefault || data.Collection.OwnedIds.Contains(target))return CollectionStatus.InvalidSelection;
                if(data.Collection.Tickets<CollectionRules.ExchangeTickets(skin.Rarity))return CollectionStatus.InsufficientTickets;
            }
            if((kind=="Single" || kind=="Ten") && data.Coins<CollectionRules.SinglePrice(data.Collection.OwnedIds.Length-1)*(kind=="Ten"?9:1))return CollectionStatus.InsufficientCoins;
            var next=data.Copy();var receipt=CollectionDrawEngine.Generate(next.Collection,requestId,kind,data.CurrentLevel,target);next.Coins-=receipt.CoinCost;
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            return Commit(next) ? CollectionStatus.Saved : CollectionStatus.StorageUnavailable;
        }
        public SavedGameRuntime CreateNextAttempt(LevelData level,LaneTransitTiming lane=null,CombatTiming combat=null,bool restart=false)=>
            SavedGameRuntime.CreateCollected(level,data.CurrentLevel,data.Collection,data.Settlements.Length+(restart?1:0),lane,combat);
    }
}
