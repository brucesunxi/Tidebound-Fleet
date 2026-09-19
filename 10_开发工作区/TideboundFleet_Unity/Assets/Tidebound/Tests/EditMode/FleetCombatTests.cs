using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Combat;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Fleet;
using Tidebound.Lane;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class FleetCombatTests
    {
        private static GameSession Single() => ShipMovementSystemTests.CreateSession(4,4,ShipMovementSystemTests.Ship("A",0,0,ShipDirection.Up));
        private static ShipEventContext Context(GameSession s,ShipRuntimeData ship) => new ShipEventContext(s.SessionId,ship.Id,ship.TypeId,ship.SkinId);
        private static void Exit(ShipMovementSystem movement,string id)
        {
            var request=movement.TryBeginMove(id);Assert.That(request.IsAccepted,Is.True);
            Assert.That(request.Operation.WillExit,Is.True);
            Assert.That(movement.CompleteTravel(request.Operation.OperationId),Is.EqualTo(ShipMoveAdvanceStatus.Applied));
        }
        [Test]
        public void ShipCreatesOneTokenButOnlyImpactDamagesBossAndWins()
        {
            using(var s=Single()) using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var combat=new FleetCombatSystem(s,lane))
            {
                var hits=0;var wins=0;var created=0;
                s.Events.Subscribe<BossDamagedEvent>(_=>hits++);s.Events.Subscribe<GameWinEvent>(_=>wins++);s.Events.Subscribe<AttackCreatedEvent>(_=>created++);
                var movement=new ShipMovementSystem(s);movement.StartPlaying();Exit(movement,"A");
                lane.Advance(.109);combat.Advance();Assert.That(combat.Attacks,Is.Empty);
                lane.Advance(.001);combat.Advance();
                Assert.That(created,Is.EqualTo(1));Assert.That(combat.InFlightCount,Is.EqualTo(1));Assert.That(s.Boss.Hp,Is.EqualTo(10));
                Assert.That(s.State,Is.EqualTo(GameState.Playing));Assert.That(lane.IsEmpty,Is.True);Assert.That(s.Board.ShipCount,Is.Zero);
                lane.Advance(.249);combat.Advance();Assert.That(hits,Is.Zero);Assert.That(wins,Is.Zero);
                lane.Advance(.001);combat.Advance();
                Assert.That(s.Boss.Hp,Is.Zero);Assert.That(hits,Is.EqualTo(1));Assert.That(wins,Is.EqualTo(1));Assert.That(combat.IsVictorious,Is.True);
                combat.Advance();Assert.That(hits,Is.EqualTo(1));Assert.That(wins,Is.EqualTo(1));
            }
        }
        [Test]
        public void ForeignPrematureMismatchedAndDuplicateEventsCannotCreateExtraShots()
        {
            using(var s=Single()) using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var combat=new FleetCombatSystem(s,lane))
            {
                var context=Context(s,s.Ships[0]);
                s.Events.Publish(new ShipEnterFleetEvent(context,1));
                var movement=new ShipMovementSystem(s);movement.StartPlaying();Exit(movement,"A");lane.Advance(.11);
                s.Events.Publish(new ShipEnterFleetEvent(context,1));
                s.Events.Publish(new ShipEnterFleetEvent(new ShipEventContext("old", "A",context.TypeId,context.SkinId),2));
                s.Events.Publish(new ShipEnterFleetEvent(new ShipEventContext(s.SessionId,"A","bad",context.SkinId),2));
                s.Events.Publish(new ShipEnterFleetEvent(new ShipEventContext(s.SessionId,"A",context.TypeId,"bad"),2));
                s.Events.Publish(new ShipEnterFleetEvent(context,0));
                Assert.That(combat.Attacks.Count,Is.EqualTo(1));Assert.That(combat.RejectedEvents,Is.EqualTo(6));
                lane.Advance(1);combat.Advance();Assert.That(combat.HitCount,Is.EqualTo(1));
            }
        }
        [Test]
        public void PauseFreezesFlightsAndQueuesThenResumeUsesRemainingTime()
        {
            using(var s=Single()) using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var combat=new FleetCombatSystem(s,lane))
            {
                var movement=new ShipMovementSystem(s);movement.StartPlaying();Exit(movement,"A");lane.Advance(.2);combat.Advance();
                var progress=combat.Attacks[0].FlightProgress(combat.Time);movement.Pause();lane.Advance(100);combat.Advance();
                Assert.That(combat.Attacks[0].FlightProgress(combat.Time),Is.EqualTo(progress));Assert.That(s.Boss.Hp,Is.EqualTo(10));
                movement.Resume();lane.Advance(.16);combat.Advance();Assert.That(combat.IsVictorious,Is.True);
            }
        }
        private static GameSession Skins(params (string skin,int length)[] specs)
        {
            var ships=specs.Select((s,i)=>new ShipRuntimeData("S"+i,FoundationLimits.BaseShipTypeId,s.skin,new GridPosition(i*2,0),ShipDirection.Up,s.length,10)).ToArray();
            return new GameSession("Fleet",24,6,ships,new BossRuntimeData("Boss",ships.Length*10));
        }
        [Test]
        public void SameSkinUsesCadenceWhileDifferentSkinsAndLongSupportRunIndependently()
        {
            using(var s=Skins(("A",2),("A",2),("B",2),(FoundationLimits.DefaultLongSkinId,3)))
            using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var combat=new FleetCombatSystem(s,lane))
            {
                var movement=new ShipMovementSystem(s);movement.StartPlaying();foreach(var ship in s.Ships)Exit(movement,ship.Id);
                lane.Advance(.2);combat.Advance();
                Assert.That(combat.Attacks[1].LaunchAt-combat.Attacks[0].LaunchAt,Is.EqualTo(.2).Within(1e-6));
                Assert.That(combat.Attacks[2].LaunchAt,Is.LessThan(combat.Attacks[1].LaunchAt));
                Assert.That(combat.Attacks[3].LaunchAt,Is.LessThan(combat.Attacks[1].LaunchAt));
                Assert.That(combat.Fleet.StandardGroups.Count,Is.EqualTo(2));Assert.That(combat.Fleet.Support.ArrivedCount,Is.EqualTo(1));
                Assert.That(combat.Fleet.StandardGroups[0].ArrivedCount,Is.EqualTo(2));
                lane.Advance(1);combat.Advance();Assert.That(combat.HitCount,Is.EqualTo(4));Assert.That(combat.IsVictorious,Is.True);
            }
        }
        [Test]
        public void FiveSkinSeatsFollowEquipmentOrderAndLongShipsDoNotTakeASixthSeat()
        {
            using(var s=Skins(("A",2),("B",2),("C",2),("D",2),("E",2),(FoundationLimits.DefaultLongSkinId,3)))
            {
                var roster=new FleetRoster(s.Ships,new[]{"E","D","C","B","A"});
                CollectionAssert.AreEqual(new[]{"E","D","C","B","A"},roster.StandardGroups.Select(g=>g.SkinId));
                Assert.That(roster.Support.IsSupport,Is.True);
                Assert.Throws<ArgumentException>(()=>new FleetRoster(s.Ships,new[]{"A","B","C","D","E","F"}));
                Assert.Throws<ArgumentException>(()=>new FleetRoster(s.Ships,new[]{"A","A"}));
            }
        }
        [Test]
        public void EightyShipBurstHitsExactlyOnceAndWinsOnlyAfterAllQueuedAttacks()
        {
            var placements=Enumerable.Range(0,80).Select(i=>ShipMovementSystemTests.Ship("S"+i,2*(i%12),2*(i/12),ShipDirection.Right)).ToArray();
            using(var s=ShipMovementSystemTests.CreateSession(24,24,placements))
            using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var combat=new FleetCombatSystem(s,lane))
            {
                var hits=new HashSet<string>();var wins=0;
                s.Events.Subscribe<BossDamagedEvent>(e=>Assert.That(hits.Add(e.AttackId),Is.True));s.Events.Subscribe<GameWinEvent>(_=>wins++);
                var movement=new ShipMovementSystem(s);movement.StartPlaying();foreach(var ship in s.Ships.OrderByDescending(x=>x.Position.X))Exit(movement,ship.Id);
                lane.Advance(1);combat.Advance();Assert.That(lane.IsEmpty,Is.True);Assert.That(combat.PendingCount,Is.GreaterThan(0));Assert.That(wins,Is.Zero);
                lane.Advance(20);combat.Advance();Assert.That(hits.Count,Is.EqualTo(80));Assert.That(combat.PendingCount,Is.Zero);
                Assert.That(s.Boss.Hp,Is.Zero);Assert.That(wins,Is.EqualTo(1));Assert.That(s.Boss.InitialHp,Is.EqualTo(800));
            }
        }
        [Test]
        public void ReentrantAdvanceDuringDamageDoesNotDuplicateDamageOrVictory()
        {
            using(var s=Single()) using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var combat=new FleetCombatSystem(s,lane))
            {
                var wins=0;var hits=0;s.Events.Subscribe<GameWinEvent>(_=>wins++);
                s.Events.Subscribe<BossDamagedEvent>(_=>{hits++;combat.Advance();});
                var movement=new ShipMovementSystem(s);movement.StartPlaying();Exit(movement,"A");lane.Advance(1);combat.Advance();
                Assert.That(hits,Is.EqualTo(1));Assert.That(wins,Is.EqualTo(1));
            }
        }
        [Test]
        public void CorruptHpProducesExplicitFaultInsteadOfFalseVictory()
        {
            using(var s=Single()) using(var lane=new TransitSystem(s)) using(var combat=new FleetCombatSystem(s,lane))
            {
                new ShipMovementSystem(s).StartPlaying();s.Boss.Hp=0;combat.Advance();
                Assert.That(combat.FaultReason,Is.EqualTo("BossHpMismatch"));Assert.That(s.State,Is.EqualTo(GameState.Failed));
            }
        }
        [Test]
        public void DisposeUnsubscribesAndLateOldSessionEventsCannotDamageANewSession()
        {
            using(var s=Single()) using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01)))
            using(var other=Single())
            {
                var combat=new FleetCombatSystem(s,lane);combat.Dispose();
                var movement=new ShipMovementSystem(s);movement.StartPlaying();Exit(movement,"A");lane.Advance(2);
                Assert.That(combat.Attacks,Is.Empty);Assert.That(other.Boss.Hp,Is.EqualTo(10));
                Assert.Throws<ObjectDisposedException>(()=>combat.Advance());
            }
        }
        [TestCase(0,.25)][TestCase(.2,0)][TestCase(double.NaN,.25)][TestCase(.2,double.PositiveInfinity)]
        public void InvalidTimingIsRejected(double launch,double flight) => Assert.Throws<ArgumentOutOfRangeException>(()=>new CombatTiming(launch,flight));
    }
}
