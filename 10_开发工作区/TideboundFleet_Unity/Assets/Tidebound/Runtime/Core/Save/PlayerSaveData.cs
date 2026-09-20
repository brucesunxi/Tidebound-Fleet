using System;
using System.Linq;
using Tidebound.Board;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Collection;

namespace Tidebound.Save
{
    public sealed class SavedShip
    {
        public string Id,TypeId,SkinId;
        public int X,Y,Length,Damage,CoinCap=1,Coins=1;
        public ShipDirection Direction;
        public SavedShip Copy() => (SavedShip)MemberwiseClone();
        public ShipRuntimeData Runtime() => new ShipRuntimeData(Id,TypeId,SkinId,new GridPosition(X,Y),Direction,Length,Damage);
    }
    public sealed class SavedPlacement
    {
        public string Id; public int X,Y; public ShipDirection Direction;
        public SavedPlacement Copy() => (SavedPlacement)MemberwiseClone();
        public static SavedPlacement From(BoardShipSnapshot s) => new SavedPlacement{Id=s.Id,X=s.Position.X,Y=s.Position.Y,Direction=s.Direction};
    }
    public sealed class SavedDeparture
    {
        public string Id; public int X,Y; public ShipDirection ExitDirection,Orientation; public long Sequence; public double At;
        public SavedDeparture Copy() => (SavedDeparture)MemberwiseClone();
    }
    public sealed class AttemptSaveData
    {
        public string AttemptId,LevelId,BossId,LayoutFingerprint,RewardSeed;
        public string EconomyVersion=BattleCoinRules.Version;
        public int LevelNumber,Width,Height,ToolUses;
        public SavedShip[] Ships=Array.Empty<SavedShip>();
        public SavedPlacement[] Board=Array.Empty<SavedPlacement>();
        public SavedDeparture[] Departures=Array.Empty<SavedDeparture>();
        public string[] HitIds=Array.Empty<string>();
        public string PendingMoveId;
        public bool Paused,Victory;
        public double Elapsed,LaneDuration=1.2,EntranceInterval=.15,FleetEntryDuration=.15,LaunchInterval=.2,FlightDuration=.25;
        public AttemptSaveData Copy()
        {
            var copy=(AttemptSaveData)MemberwiseClone();copy.Ships=Ships.Select(x=>x.Copy()).ToArray();copy.Board=Board.Select(x=>x.Copy()).ToArray();
            copy.Departures=Departures.Select(x=>x.Copy()).ToArray();copy.HitIds=HitIds.ToArray();return copy;
        }
        public int PendingCoins => Ships.Where(s=>HitIds.Contains(s.Id,StringComparer.Ordinal)).Sum(s=>s.Coins);
    }
    public sealed class SettlementRecord
    {
        public string AttemptId,LevelId,Kind,Day,EconomyVersion;
        public int LevelNumber,BattleCoins,FirstClearCoins,RestartOrdinal;
        public SettlementRecord Copy() => (SettlementRecord)MemberwiseClone();
    }
    public sealed class PlayerSaveData
    {
        public const int CurrentVersion=4;
        public int Version=CurrentVersion;
        public CollectionData Collection=new CollectionData();
        public long Revision,Coins;
        public int CurrentLevel=1,HighestClearedLevel,DailyRestartCount;
        public string MaxLocalDay="";
        public bool ImportedToolInventory;
        public ToolInventoryData Tools=new ToolInventoryData();
        public AttemptSaveData Attempt;
        public SettlementRecord[] Settlements=Array.Empty<SettlementRecord>();
        public CoinPurchaseRecord[] Purchases=Array.Empty<CoinPurchaseRecord>();
        public PlayerSaveData Copy()
        {
            var copy=(PlayerSaveData)MemberwiseClone();copy.Collection=Collection?.Copy();copy.Tools=Tools.Copy();copy.Attempt=Attempt?.Copy();copy.Settlements=Settlements.Select(x=>x.Copy()).ToArray();copy.Purchases=Purchases.Select(x=>x.Copy()).ToArray();return copy;
        }
        public void Validate()
        {
            if((Version!=2 && Version!=3 && Version!=CurrentVersion) || Purchases==null || (Version==2 && Purchases.Length!=0) || Revision<0 || Coins<0 || CurrentLevel<1 || CurrentLevel>10001 || HighestClearedLevel!=CurrentLevel-1 || DailyRestartCount<0 ||
                Tools==null || Settlements==null || Settlements.Any(s=>s==null || string.IsNullOrWhiteSpace(s.AttemptId) || s.BattleCoins<0 || s.FirstClearCoins<0) ||
                Settlements.Select(s=>s.AttemptId).Distinct(StringComparer.Ordinal).Count()!=Settlements.Length)
                throw new ArgumentException("Invalid player save.");
            if(MaxLocalDay!="" && !DateTime.TryParseExact(MaxLocalDay,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,out _))throw new ArgumentException("Invalid saved day.");
            if(Purchases.Any(p=>p==null) || Purchases.Select(p=>p.RequestId).Distinct(StringComparer.Ordinal).Count()!=Purchases.Length)
                throw new ArgumentException("Duplicate purchase receipt.");
            foreach(var p in Purchases)p.Validate();
            if(Collection==null)throw new ArgumentException("Missing collection profile.");
            Collection.Validate(CurrentLevel);
            if(Version<CurrentVersion && !Collection.IsPristine)throw new ArgumentException("Legacy save contains unexpected collection transactions.");
            if(Purchases.Select(p=>p.RequestId).Intersect(Collection.Receipts.Select(r=>r.RequestId).Concat(Collection.EquipmentReceipts.Select(r=>r.RequestId)),StringComparer.Ordinal).Any())throw new ArgumentException("Request id reused across transaction types.");
            Tools.Validate();
            if(Purchases.Any(p=>!Tools.Receipts.Contains(p.InventoryReceipt,StringComparer.Ordinal)) ||
                Tools.Receipts.Where(id=>id.StartsWith("coin:",StringComparison.Ordinal)).Any(id=>!Purchases.Any(p=>p.InventoryReceipt==id)))
                throw new ArgumentException("Purchase and inventory receipts disagree.");
            var wins=Settlements.Where(r=>r.Kind=="Victory").OrderBy(r=>r.LevelNumber).ToArray();
            if(wins.Length!=HighestClearedLevel || wins.Where((r,i)=>r.LevelNumber!=i+1).Any() ||
                Coins!=Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins)-Purchases.Sum(p=>(long)p.Price)-Collection.CoinSpent)throw new ArgumentException("Invalid settlement balance or progression.");
            foreach(var r in Settlements)
            {
                if(!Guid.TryParseExact(r.AttemptId,"N",out _) || string.IsNullOrWhiteSpace(r.LevelId) || r.LevelNumber<1 || r.LevelNumber>10000 ||
                    r.EconomyVersion!=BattleCoinRules.Version || !DateTime.TryParseExact(r.Day,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out _) ||
                    (r.Kind!="Victory" && r.Kind!="DeadlockRestart" && r.Kind!="VoluntaryRestart") ||
                    (r.Kind=="Victory" ? r.RestartOrdinal!=0 || r.FirstClearCoins!=BattleCoinRules.FirstClear(r.LevelNumber) : r.RestartOrdinal<1 || r.FirstClearCoins!=0) ||
                    (r.Kind=="VoluntaryRestart" && r.BattleCoins!=0) || (r.RestartOrdinal>10 && r.BattleCoins!=0))throw new ArgumentException("Invalid settlement receipt.");
            }
            if(DailyRestartCount!=Settlements.Count(r=>r.RestartOrdinal>0 && r.Day==MaxLocalDay))throw new ArgumentException("Invalid restart allowance.");
            Tools.Validate();
            if(Attempt!=null)
            {
                SavedGameRuntime.Validate(Attempt);
                var receipt=Settlements.SingleOrDefault(r=>r.AttemptId==Attempt.AttemptId);
                if(receipt!=null && (receipt.Kind!="Victory" || !Attempt.Victory || receipt.LevelNumber!=Attempt.LevelNumber || receipt.BattleCoins!=Attempt.PendingCoins))throw new ArgumentException("Attempt settlement mismatch.");
                if(Attempt.LevelNumber!=(receipt==null ? CurrentLevel : CurrentLevel-1))throw new ArgumentException("Attempt progression mismatch.");
            }
        }
    }
    public interface IPlayerSaveStore { PlayerSaveData Load(); void Save(PlayerSaveData data); }
    public sealed class MemoryPlayerSaveStore : IPlayerSaveStore
    {
        private PlayerSaveData data;
        public PlayerSaveData Load() => data?.Copy();
        public void Save(PlayerSaveData value) { data=value.Copy(); }
    }
}
