using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Tidebound.Core;
using Tidebound.Tools;

namespace Tidebound.Save
{
    /// <summary>One durable transaction owns progress, tool effects, stock and settlement. Never settles from a visual callback.</summary>
    public sealed partial class PlayerSaveService : IToolInventoryStore,IToolMutationStore
    {
        private readonly IPlayerSaveStore store;
        private readonly Func<DateTime> today;
        private PlayerSaveData data;
        private long savedChangeVersion=-1;
        public bool IsAvailable { get; private set; }=true;
        public string LastError { get; private set; }
        public SavedGameRuntime Runtime { get; private set; }
        public ToolInventory Inventory { get; }
        public long Coins => data.Coins;
        public int CurrentLevel => data.CurrentLevel;
        public int DailyRestartCount => data.DailyRestartCount;
        public PlayerSaveData Snapshot => data.Copy();
        public int RestartReward => Runtime?.IsTerminalDeadlock==true &&
            (string.CompareOrdinal(today().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),data.MaxLocalDay)>0 ? 0 : data.DailyRestartCount)<10 ? Runtime.PendingCoins : 0;
        public bool CurrentAttemptSettled => Runtime!=null && data.Settlements.Any(r=>r.AttemptId==Runtime.Session.SessionId);
        public PlayerSaveService(IPlayerSaveStore store,IToolInventoryStore legacyTools=null,Func<DateTime> today=null)
        {
            this.store=store ?? throw new ArgumentNullException(nameof(store));this.today=today ?? (()=>DateTime.Now.Date);
            try
            {
                data=store.Load();
                if(data==null)
                {
                    data=new PlayerSaveData();var legacy=legacyTools?.Load();
                    if(legacy!=null){legacy.Validate();data.Tools=legacy.Copy();data.ImportedToolInventory=true;data.Revision=1;store.Save(data.Copy());}
                }
                data.Validate();
                if(data.Attempt!=null)using(var verified=SavedGameRuntime.Restore(data.Attempt)){}
                // Upgrade in place, retaining the existing filename and the previous file as backup.
                if(data.Version<PlayerSaveData.CurrentVersion){var upgraded=data.Copy();upgraded.Version=PlayerSaveData.CurrentVersion;upgraded.Revision=checked(data.Revision+1);upgraded.Validate();store.Save(upgraded.Copy());data=upgraded;}
            }
            catch(Exception e){data=new PlayerSaveData();IsAvailable=false;LastError=e.GetType().Name;}
            Inventory=new ToolInventory(this);
        }
        ToolInventoryData IToolInventoryStore.Load()
        {if(!IsAvailable)throw new InvalidDataException("Player save is unavailable.");return data.Tools.Copy();}
        void IToolInventoryStore.Save(ToolInventoryData tools) => SaveTools(tools,null);
        void IToolMutationStore.SaveToolMutation(ToolInventoryData tools,ToolMutation mutation) => SaveTools(tools,mutation);
        private void SaveTools(ToolInventoryData tools,ToolMutation mutation)
        {
            var next=data.Copy();next.Tools=tools.Copy();
            if(Runtime!=null && data.Attempt?.AttemptId==Runtime.Session.SessionId)
                next.Attempt=mutation==null ? Runtime.Capture() : Runtime.ProjectTool(mutation);
            else if(mutation!=null)throw new InvalidOperationException("No current attempt for a tool effect.");
            if(!Commit(next))throw new IOException("Inventory checkpoint was not committed.");
        }
        public CoinPurchaseStatus BuyWithCoins(string requestId,string productId,CoinShopCatalog catalog)
        {
            if(!IsAvailable || !Inventory.IsAvailable)return CoinPurchaseStatus.StorageUnavailable;
            if(!Guid.TryParseExact(requestId,"N",out _))return CoinPurchaseStatus.InvalidRequest;
            var prior=data.Purchases.SingleOrDefault(p=>p.RequestId==requestId);
            if(prior!=null)return prior.ProductId==productId ? CoinPurchaseStatus.AlreadyPurchased : CoinPurchaseStatus.RequestConflict;
            if(data.Collection.Receipts.Any(r=>r.RequestId==requestId) || data.Collection.EquipmentReceipts.Any(r=>r.RequestId==requestId))return CoinPurchaseStatus.RequestConflict;
            var product=catalog?.Find(productId);
            if(product==null)return CoinPurchaseStatus.InvalidProduct;
            if(data.CurrentLevel<catalog.UnlockLevel)return CoinPurchaseStatus.Locked;
            if(Runtime!=null && (data.Attempt?.AttemptId!=Runtime.Session.SessionId || Runtime.Movement.IsBusy || Runtime.PendingMoveId!=null ||
                (Runtime.Session.State!=GameState.Playing && Runtime.Session.State!=GameState.Paused)))return CoinPurchaseStatus.Busy;
            if(data.Coins<product.Price)return CoinPurchaseStatus.InsufficientCoins;
            var next=data.Copy();
            try
            {
                next.Tools.Rescue=checked(next.Tools.Rescue+product.Rescue);next.Tools.Shuffle=checked(next.Tools.Shuffle+product.Shuffle);next.Tools.Reverse=checked(next.Tools.Reverse+product.Reverse);
            }
            catch(OverflowException){return CoinPurchaseStatus.InventoryLimit;}
            var receipt=new CoinPurchaseRecord{RequestId=requestId,ProductId=product.Id,CatalogVersion=catalog.Version,Price=product.Price,
                Rescue=product.Rescue,Shuffle=product.Shuffle,Reverse=product.Reverse,AtLevel=data.CurrentLevel};
            next.Coins-=receipt.Price;next.Purchases=next.Purchases.Concat(new[]{receipt}).ToArray();
            next.Tools.Receipts=next.Tools.Receipts.Concat(new[]{receipt.InventoryReceipt}).ToArray();
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            if(!Commit(next))return CoinPurchaseStatus.StorageUnavailable;
            Inventory.AcceptCommitted(data.Tools);return CoinPurchaseStatus.Purchased;
        }
        public void Suspend(string reason) {IsAvailable=false;LastError=reason;}
        public bool Start(SavedGameRuntime runtime)
        {
            if(!IsAvailable || runtime.LevelNumber!=data.CurrentLevel || (data.Attempt!=null && !data.Attempt.Victory && data.Attempt.AttemptId!=runtime.Session.SessionId))return false;
            var next=data.Copy();next.Attempt=runtime.Capture();
            if(!Commit(next))return false;Runtime=runtime;savedChangeVersion=runtime.ChangeVersion;
            Inventory.ReachLevel(runtime.LevelNumber);return true;
        }
        public void AttachRestored(SavedGameRuntime runtime)
        {
            if(!IsAvailable || data.Attempt?.AttemptId!=runtime.Session.SessionId)throw new InvalidOperationException("Cannot attach a different attempt.");
            Runtime=runtime;savedChangeVersion=-1;
        }
        public bool PrepareMove(string id)
        {
            if(Runtime==null || data.Attempt?.AttemptId!=Runtime.Session.SessionId || Runtime.PendingMoveId!=null || Runtime.Movement.IsBusy || Runtime.Session.State!=GameState.Playing || !Runtime.Session.Board.TryGetShip(id,out _))return false;
            var next=data.Copy();next.Attempt=Runtime.Capture();next.Attempt.PendingMoveId=id;
            if(!Commit(next))return false;Runtime.PendingMoveId=id;savedChangeVersion=Runtime.ChangeVersion;return true;
        }
        public bool Checkpoint(bool force=false)
        {
            if(Runtime==null || !IsAvailable || data.Attempt?.AttemptId!=Runtime.Session.SessionId)return false;
            if(!force && savedChangeVersion==Runtime.ChangeVersion && LastError==null)return true;
            var next=data.Copy();next.Attempt=Runtime.Capture();
            if(next.Attempt.Victory && !next.Settlements.Any(s=>s.AttemptId==next.Attempt.AttemptId))
            {
                if(next.Attempt.LevelNumber!=next.CurrentLevel)throw new InvalidOperationException("Cannot reward an old or skipped level.");
                var receipt=Receipt(next.Attempt,"Victory",next.Attempt.PendingCoins,BattleCoinRules.FirstClear(next.CurrentLevel),0);
                next.Coins=checked(next.Coins+receipt.BattleCoins+receipt.FirstClearCoins);next.Settlements=next.Settlements.Concat(new[]{receipt}).ToArray();
                next.HighestClearedLevel=next.CurrentLevel;next.CurrentLevel++;
            }
            if(!Commit(next))return false;savedChangeVersion=Runtime.ChangeVersion;return true;
        }
        public bool Restart(SavedGameRuntime replacement)
        {
            if(!IsAvailable || Runtime==null || Runtime.Session.State==GameState.Victory || data.Attempt?.AttemptId!=Runtime.Session.SessionId ||
                replacement.Session.SessionId==Runtime.Session.SessionId || replacement.LevelNumber!=data.CurrentLevel || replacement.Session.LevelId!=Runtime.Session.LevelId ||
                data.Settlements.Any(s=>s.AttemptId==Runtime.Session.SessionId))return false;
            var current=Runtime.Capture();var next=data.Copy();var day=today().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            if(string.CompareOrdinal(day,next.MaxLocalDay)>0){next.MaxLocalDay=day;next.DailyRestartCount=0;}
            var ordinal=checked(++next.DailyRestartCount);var deadlock=Runtime.IsTerminalDeadlock;
            var amount=deadlock && ordinal<=10 ? current.PendingCoins : 0;
            var record=Receipt(current,deadlock ? "DeadlockRestart" : "VoluntaryRestart",amount,0,ordinal);record.Day=next.MaxLocalDay;
            next.Coins=checked(next.Coins+amount);next.Settlements=next.Settlements.Concat(new[]{record}).ToArray();next.Attempt=replacement.Capture();
            if(!Commit(next))return false;Runtime=replacement;savedChangeVersion=replacement.ChangeVersion;return true;
        }
        private SettlementRecord Receipt(AttemptSaveData attempt,string kind,int battle,int first,int ordinal) => new SettlementRecord
        {AttemptId=attempt.AttemptId,LevelId=attempt.LevelId,LevelNumber=attempt.LevelNumber,Kind=kind,BattleCoins=battle,FirstClearCoins=first,RestartOrdinal=ordinal,
         EconomyVersion=attempt.EconomyVersion,Day=today().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)};
        private bool Commit(PlayerSaveData next)
        {
            if(!IsAvailable)return false;
            try {next.Revision=checked(data.Revision+1);next.Validate();store.Save(next.Copy());data=next;LastError=null;return true;}
            catch(Exception e){LastError=e.GetType().Name;return false;}
        }
    }
}
