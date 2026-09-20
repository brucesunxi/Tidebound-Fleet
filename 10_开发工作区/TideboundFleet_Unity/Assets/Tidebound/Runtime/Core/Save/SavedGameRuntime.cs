using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Combat;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Tools;

namespace Tidebound.Save
{
    /// <summary>Logical checkpoint. Departure timestamps replay the existing lane/combat systems; animation pixels are not saved.</summary>
    public sealed class SavedGameRuntime : IDisposable
    {
        private readonly AttemptSaveData origin;
        private readonly List<SavedDeparture> departures=new List<SavedDeparture>();
        private readonly List<IDisposable> subscriptions=new List<IDisposable>();
        public GameSession Session { get; }
        public ShipMovementSystem Movement { get; }
        public TransitSystem Transit { get; }
        public FleetCombatSystem Combat { get; }
        public int LevelNumber => origin.LevelNumber;
        public int PendingCoins => origin.Ships.Where(s=>Combat.Attacks.Any(t=>t.Ship.ShipId==s.Id && t.Stage==AttackStage.Hit)).Sum(s=>s.Coins);
        public string PendingMoveId { get; set; }
        public long ChangeVersion { get; private set; }
        public SavedGameRuntime(GameSession session,int levelNumber,LaneTransitTiming laneTiming=null,CombatTiming combatTiming=null,
            string seed=null,IReadOnlyDictionary<string,int> caps=null)
        {
            laneTiming=laneTiming ?? new LaneTransitTiming();combatTiming=combatTiming ?? new CombatTiming();
            Session=session;Movement=new ShipMovementSystem(session);Transit=new TransitSystem(session,laneTiming);Combat=new FleetCombatSystem(session,Transit,combatTiming);
            origin=new AttemptSaveData{AttemptId=session.SessionId,LevelId=session.LevelId,BossId=session.Boss.BossId,LevelNumber=levelNumber,Width=session.Width,Height=session.Height,
                LayoutFingerprint=LevelStateIdentity.Fingerprint(session.InitialBoard),RewardSeed=seed ?? Guid.NewGuid().ToString("N"),
                LaneDuration=laneTiming.LaneDuration,EntranceInterval=laneTiming.EntranceInterval,FleetEntryDuration=laneTiming.FleetEntryDuration,
                LaunchInterval=combatTiming.LaunchInterval,FlightDuration=combatTiming.FlightDuration,
                Ships=session.Ships.Select(s=>new SavedShip{Id=s.Id,TypeId=s.TypeId,SkinId=s.SkinId,X=s.Position.X,Y=s.Position.Y,Direction=s.Direction,Length=s.Length,Damage=s.Damage,
                    CoinCap=s.Length==3?1:caps!=null && caps.TryGetValue(s.SkinId,out var cap)?cap:1}).ToArray()};
            foreach(var ship in origin.Ships)ship.Coins=BattleCoinRules.Calculate(origin.RewardSeed,ship,origin.Ships);
            subscriptions.Add(session.Events.Subscribe<ShipMoveCompleteEvent>(e=>{if(e.Ship.ShipId==PendingMoveId)PendingMoveId=null;ChangeVersion++;}));
            subscriptions.Add(session.Events.Subscribe<ShipExitBoardEvent>(e=>
            {
                if(e.Ship.SessionId!=session.SessionId || departures.Any(x=>x.Id==e.Ship.ShipId))return;
                departures.Add(new SavedDeparture{Id=e.Ship.ShipId,X=e.TailPosition.X,Y=e.TailPosition.Y,ExitDirection=e.ExitDirection,
                    Orientation=session.GetShip(e.Ship.ShipId).Direction,Sequence=e.ExitSequence,At=Transit.ElapsedTime});ChangeVersion++;
            }));
            subscriptions.Add(session.Events.Subscribe<BossDamagedEvent>(_=>ChangeVersion++));
            subscriptions.Add(session.Events.Subscribe<AttemptEndedEvent>(_=>ChangeVersion++));
            Movement.StartPlaying();
        }
        public static SavedGameRuntime Create(LevelData level,int number,LaneTransitTiming lane=null,CombatTiming combat=null) =>
            new SavedGameRuntime(LevelSessionFactory.Create(level,new[]{new ShipDefinition(FoundationLimits.BaseShipTypeId,10)},new[]{new BossDefinition(level.BossId)}),number,lane,combat);
        public AttemptSaveData Capture()
        {
            var saved=origin.Copy();saved.Board=Session.Board.Ships.Select(SavedPlacement.From).ToArray();saved.Departures=departures.Select(x=>x.Copy()).ToArray();
            saved.Elapsed=Transit.ElapsedTime;saved.PendingMoveId=PendingMoveId;saved.Paused=Session.State==GameState.Paused;saved.Victory=Session.State==GameState.Victory;
            saved.HitIds=Combat.Attacks.Where(t=>t.Stage==AttackStage.Hit).Select(t=>t.Ship.ShipId).ToArray();return saved;
        }
        public AttemptSaveData ProjectTool(ToolMutation mutation)
        {
            var saved=Capture();saved.Board=mutation.BoardAfter.Ships.Select(SavedPlacement.From).ToArray();
            if(mutation.RescuePaths!=null)
            {
                var next=saved.Departures.ToList();
                foreach(var path in mutation.RescuePaths)next.Add(new SavedDeparture{Id=path.ShipId,X=path.TargetTail.X,Y=path.TargetTail.Y,ExitDirection=path.Direction,
                    Orientation=Session.GetShip(path.ShipId).Direction,At=saved.Elapsed,Sequence=next.Count+1});
                saved.Departures=next.ToArray();
            }
            return saved;
        }
        public static void Validate(AttemptSaveData s)
        {
            if(s==null || !Guid.TryParseExact(s.AttemptId,"N",out _) || string.IsNullOrWhiteSpace(s.LevelId) || string.IsNullOrWhiteSpace(s.BossId) || string.IsNullOrWhiteSpace(s.RewardSeed) ||
                s.EconomyVersion!=BattleCoinRules.Version || s.LevelNumber<1 || s.LevelNumber>10000 || s.Width<1 || s.Height<1 ||
                s.Width>FoundationLimits.MaxTechnicalBoardWidth || s.Height>FoundationLimits.MaxTechnicalBoardHeight ||
                s.Ships==null || s.Ships.Length<1 || s.Ships.Length>FoundationLimits.MaxTechnicalShipCount || s.Board==null || s.Departures==null || s.HitIds==null ||
                double.IsNaN(s.Elapsed) || double.IsInfinity(s.Elapsed) || s.Elapsed<0)throw new ArgumentException("Invalid attempt checkpoint.");
            if(s.Ships.Any(x=>x==null || string.IsNullOrWhiteSpace(x.Id) || string.IsNullOrWhiteSpace(x.SkinId) || x.TypeId!=FoundationLimits.BaseShipTypeId ||
                x.Length<2 || x.Length>3 || x.Damage!=10 || !Enum.IsDefined(typeof(ShipDirection),x.Direction)) || s.Ships.Select(x=>x.Id).Distinct().Count()!=s.Ships.Length)
                throw new ArgumentException("Invalid saved ship identity.");
            if(s.Ships.Where(x=>x.Length==2).GroupBy(x=>x.SkinId).Any(g=>g.Select(x=>x.CoinCap).Distinct().Count()!=1))throw new ArgumentException("Inconsistent skin rewards.");
            var initial=new BoardModel(s.Width,s.Height,s.Ships.Select(x=>x.Runtime()));
            if(LevelStateIdentity.Fingerprint(initial)!=s.LayoutFingerprint || s.Ships.Any(x=>x.Coins!=BattleCoinRules.Calculate(s.RewardSeed,x,s.Ships)))throw new ArgumentException("Saved layout or reward changed.");
            var ids=s.Ships.Select(x=>x.Id).ToArray();
            if(s.Board.Any(x=>x==null || !ids.Contains(x.Id) || !Enum.IsDefined(typeof(ShipDirection),x.Direction)) || s.Departures.Any(x=>x==null || !ids.Contains(x.Id)) ||
                s.Board.Select(x=>x.Id).Concat(s.Departures.Select(x=>x.Id)).Distinct().Count()!=ids.Length || s.Board.Length+s.Departures.Length!=ids.Length ||
                s.HitIds.Distinct().Count()!=s.HitIds.Length || s.HitIds.Any(x=>!s.Departures.Any(d=>d.Id==x)) ||
                (s.PendingMoveId!=null && !s.Board.Any(x=>x.Id==s.PendingMoveId)))throw new ArgumentException("Saved board partition is invalid.");
            var runtime=s.Board.Select(p=>{var ship=s.Ships.Single(x=>x.Id==p.Id).Runtime();ship.Position=new GridPosition(p.X,p.Y);ship.Direction=p.Direction;return ship;});
            new BoardModel(s.Width,s.Height,runtime);
            var last=0d;
            for(var i=0;i<s.Departures.Length;i++)
            {
                var d=s.Departures[i];if(d.Sequence!=i+1 || double.IsNaN(d.At) || double.IsInfinity(d.At) || d.At<last || d.At>s.Elapsed ||
                    !Enum.IsDefined(typeof(ShipDirection),d.ExitDirection) || !Enum.IsDefined(typeof(ShipDirection),d.Orientation))throw new ArgumentException("Saved departure order is invalid.");last=d.At;
            }
            new LaneTransitTiming(s.LaneDuration,s.EntranceInterval,s.FleetEntryDuration);new CombatTiming(s.LaunchInterval,s.FlightDuration);
            if(s.Victory && (s.Board.Length!=0 || s.HitIds.Length!=s.Ships.Length || s.PendingMoveId!=null))throw new ArgumentException("Invalid saved victory.");
        }
        public static SavedGameRuntime Restore(AttemptSaveData s)
        {
            Validate(s);
            var session=new GameSession(s.LevelId,s.Width,s.Height,s.Ships.Select(x=>x.Runtime()).ToArray(),new BossRuntimeData(s.BossId,s.Ships.Length*10),s.AttemptId);
            var caps=s.Ships.Where(x=>x.Length==2).GroupBy(x=>x.SkinId).ToDictionary(g=>g.Key,g=>g.First().CoinCap);
            var game=new SavedGameRuntime(session,s.LevelNumber,new LaneTransitTiming(s.LaneDuration,s.EntranceInterval,s.FleetEntryDuration),new CombatTiming(s.LaunchInterval,s.FlightDuration),s.RewardSeed,caps);
            try
            {
                foreach(var p in s.Board){var ship=session.GetShip(p.Id);ship.Position=new GridPosition(p.X,p.Y);ship.Direction=p.Direction;}
                session.Board=new BoardModel(s.Width,s.Height,s.Board.Select(p=>session.GetShip(p.Id)));
                foreach(var d in s.Departures)
                {
                    game.Transit.Advance(d.At-game.Transit.ElapsedTime);var ship=session.GetShip(d.Id);
                    ship.Position=new GridPosition(d.X,d.Y);ship.Direction=d.Orientation;ship.State=ShipState.InLane;
                    session.Events.Publish(new ShipExitBoardEvent(new ShipEventContext(session.SessionId,ship.Id,ship.TypeId,ship.SkinId),ship.Position,d.ExitDirection,d.Sequence));
                }
                game.Transit.Advance(s.Elapsed-game.Transit.ElapsedTime);game.Movement.RestoreExitSequence(s.Departures.Length);game.Combat.Advance();
                if(!game.Capture().HitIds.OrderBy(x=>x).SequenceEqual(s.HitIds.OrderBy(x=>x)))throw new ArgumentException("Saved hits disagree with departure timeline.");
                if(s.PendingMoveId!=null)
                {
                    game.PendingMoveId=s.PendingMoveId;var request=game.Movement.TryBeginMove(s.PendingMoveId);
                    if(!request.IsAccepted)throw new ArgumentException("Saved pending move is invalid.");
                    if(request.Operation.Stage==ShipMoveStage.Traveling)game.Movement.CompleteTravel(request.Operation.OperationId);
                    if(game.Movement.IsBusy)game.Movement.CompleteBlockedFeedback(request.Operation.OperationId);
                }
                if(s.Paused)game.Movement.Pause();return game;
            }
            catch {game.Dispose();throw;}
        }
        public bool IsTerminalDeadlock => !Movement.IsBusy && PendingMoveId==null && Session.Board.ShipCount>0 && Session.Board.Ships.All(s=>Session.Board.QueryForwardPath(s.Id).TravelDistance==0);
        public void Dispose() {foreach(var sub in subscriptions)sub.Dispose();Combat.Dispose();Transit.Dispose();Session.Dispose();}
    }
}
