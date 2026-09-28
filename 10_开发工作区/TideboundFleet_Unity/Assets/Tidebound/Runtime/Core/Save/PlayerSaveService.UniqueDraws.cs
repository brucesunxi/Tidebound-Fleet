using System;
using System.Linq;
using Tidebound.Collection;
namespace Tidebound.Save
{
    public enum UniqueDrawStatus { Saved, AlreadySaved, InvalidRequest, RequestConflict, Locked, FirstBluePending, Empty, InsufficientCoins, QuoteChanged, Busy, StorageUnavailable }
    public sealed partial class PlayerSaveService
    {
        public UniqueDrawQuote DrawQuote(DrawPool pool)=>UniqueDrawEngine.Quote(pool,data.Collection,data.Appearance);
        public UniqueDrawReceipt UniqueReceiptFor(string id)=>data.Appearance.UniqueReceipts.FirstOrDefault(x=>x.RequestId==id)?.Copy();
        private bool HasUniqueRequest(string id)=>data.Appearance.UniqueReceipts.Any(x=>x.RequestId==id);
        public UniqueDrawStatus DrawUnique(string request,DrawPool pool,int expectedCount,int expectedPrice)
        {
            if(!IsAvailable)return UniqueDrawStatus.StorageUnavailable;
            if(!Guid.TryParseExact(request,"N",out _))return UniqueDrawStatus.InvalidRequest;
            var prior=UniqueReceiptFor(request);
            if(prior!=null)return prior.Pool==pool&&prior.Ordinal==expectedCount+1&&prior.CoinCost==expectedPrice?UniqueDrawStatus.AlreadySaved:UniqueDrawStatus.RequestConflict;
            if(data.Purchases.Any(x=>x.RequestId==request)||data.Collection.Receipts.Any(x=>x.RequestId==request)||data.Collection.EquipmentReceipts.Any(x=>x.RequestId==request)||data.Appearance.DrawReceipts.Any(x=>x.RequestId==request))return UniqueDrawStatus.RequestConflict;
            if(data.CurrentLevel<3||(int)pool<0||(int)pool>2)return UniqueDrawStatus.Locked;
            if(CanClaimFirstBlue)return UniqueDrawStatus.FirstBluePending;
            if(Runtime!=null&&(data.Attempt?.AttemptId!=Runtime.Session.SessionId||Runtime.Movement.IsBusy||Runtime.PendingMoveId!=null||Runtime.Combat.IsVictorious&&!CurrentAttemptSettled))return UniqueDrawStatus.Busy;
            var quote=DrawQuote(pool);if(quote.Price!=expectedPrice||quote.PaidCount!=expectedCount)return UniqueDrawStatus.QuoteChanged;
            if(quote.Eligible.Length==0)return UniqueDrawStatus.Empty;
            if(data.Coins<quote.Price)return UniqueDrawStatus.InsufficientCoins;
            var next=data.Copy();var receipt=UniqueDrawEngine.Generate(pool,data.Collection,data.Appearance,request,data.CurrentLevel);
            next.Appearance.UniqueReceipts=next.Appearance.UniqueReceipts.Concat(new[]{receipt}).ToArray();next.Coins-=receipt.CoinCost;
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            return Commit(next)?UniqueDrawStatus.Saved:UniqueDrawStatus.StorageUnavailable;
        }
    }
}
