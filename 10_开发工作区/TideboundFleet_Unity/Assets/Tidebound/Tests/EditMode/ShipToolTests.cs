using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Lane;
using Tidebound.Combat;
using Tidebound.Tools;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class ShipToolTests
    {
        private static ShipPlacementData Ship(string id,int x,int y,ShipDirection d,int length=2) => LevelSolverTests.Ship(id,x,y,d,length);
        private static GameSession Single(ShipDirection d=ShipDirection.Up,int length=2) => ShipMovementSystemTests.CreateSession(8,8,Ship("A",3,3,d,length));
        private static ToolInventory Stock()
        { var stock=new ToolInventory();stock.Grant("test:gift",1,1,1);return stock; }
        private static LevelSolverOptions Tiny => new LevelSolverOptions(1,1,100,useExitPeeling:false);

        [TestCase(ShipDirection.Up,2)][TestCase(ShipDirection.Right,2)][TestCase(ShipDirection.Down,2)][TestCase(ShipDirection.Left,2)]
        [TestCase(ShipDirection.Up,3)][TestCase(ShipDirection.Right,3)][TestCase(ShipDirection.Down,3)][TestCase(ShipDirection.Left,3)]
        public void ReversePreservesFootprintIdentityAndDoesNotMove(ShipDirection direction,int length)
        {
            using(var s=Single(direction,length))
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock()))
                {
                    var old=s.Board.GetShip("A");var runtime=s.GetShip("A");
                    Assert.That(t.Select(ShipTool.Reverse),Is.EqualTo(ToolUseStatus.Selected));
                    Assert.That(t.UseSelected("A"),Is.EqualTo(ToolUseStatus.Applied));
                    var next=s.Board.GetShip("A");CollectionAssert.AreEquivalent(old.OccupiedCells,next.OccupiedCells);
                    Assert.That(next.Position,Is.EqualTo(old.OccupiedCells[length-1]));Assert.That(next.Direction,Is.Not.EqualTo(direction));
                    Assert.That(s.GetShip("A"),Is.SameAs(runtime));Assert.That(runtime.State,Is.EqualTo(ShipState.Idle));Assert.That(m.IsBusy,Is.False);
                    Assert.That(t.Remaining(ShipTool.Reverse),Is.Zero);Assert.That(t.Select(ShipTool.Reverse),Is.EqualTo(ToolUseStatus.NoUses));
                }
            }
        }
        [Test]
        public void CancellationAndInvalidTargetsNeverConsumeAndGuardsProtectState()
        {
            using(var s=Single())
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock())) using(var disabled=new ShipToolSystem(s,m,Stock(),false))
                {
                    Assert.That(disabled.Shuffle(),Is.EqualTo(ToolUseStatus.Disabled));
                    t.Select(ShipTool.Reverse);Assert.That(t.UseSelected("missing"),Is.EqualTo(ToolUseStatus.InvalidTarget));
                    Assert.That(t.Select(ShipTool.Reverse),Is.EqualTo(ToolUseStatus.Cancelled));Assert.That(t.Remaining(ShipTool.Rescue),Is.EqualTo(1));
                    m.Pause();Assert.That(t.Shuffle(),Is.EqualTo(ToolUseStatus.Paused));m.Resume();
                    m.TryBeginMove("A");Assert.That(t.Select(ShipTool.Reverse),Is.EqualTo(ToolUseStatus.Busy));
                    new BoardProgressMonitor(s,m).EndForRestart();Assert.That(t.Shuffle(),Is.EqualTo(ToolUseStatus.Terminal));
                }
            }
        }
        [Test]
        public void UserReverseAlwaysTurnsEvenIfResultIsDeadlocked()
        {
            using(var s=ShipMovementSystemTests.CreateSession(4,1,Ship("A",0,0,ShipDirection.Right),Ship("B",2,0,ShipDirection.Right)))
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock(),options:Tiny))
                {
                    t.Select(ShipTool.Reverse);Assert.That(t.UseSelected("B"),Is.EqualTo(ToolUseStatus.Applied));
                    Assert.That(s.GetShip("B").Direction,Is.EqualTo(ShipDirection.Left));Assert.That(t.Remaining(ShipTool.Reverse),Is.Zero);
                    Assert.That(new BoardProgressMonitor(s,m).Refresh(),Is.EqualTo(BoardProgressStatus.NoMoves));
                }
            }
        }
        [TestCase(2)][TestCase(3)]
        public void RescueTwoBlockedShipsImmediatelyCommitsBothForOneItemAndTwoAttacks(int length)
        {
            using(var s=ShipMovementSystemTests.CreateSession(length*2,1,Ship("A",0,0,ShipDirection.Right,length),Ship("B",length*2-1,0,ShipDirection.Left,length)))
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock()))
                using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01))) using(var combat=new FleetCombatSystem(s,lane))
                {
                    var exits=0;long sequence=0;
                    s.Events.Subscribe<ShipExitBoardEvent>(e=>
                    {
                        exits++;Assert.That(e.ExitSequence,Is.EqualTo(++sequence));Assert.That(s.Board.ShipCount,Is.Zero);
                        Assert.That(s.Ships.All(x=>x.State==ShipState.InLane),Is.True);Assert.That(t.Remaining(ShipTool.Rescue),Is.Zero);
                        Assert.That(t.Rescue(),Is.EqualTo(ToolUseStatus.Busy));
                    });
                    Assert.That(t.Rescue(),Is.EqualTo(ToolUseStatus.Applied));Assert.That(exits,Is.EqualTo(2));Assert.That(m.IsBusy,Is.False);
                    Assert.That(t.LastAffectedIds.Count,Is.EqualTo(2));Assert.That(t.Remaining(ShipTool.Rescue),Is.Zero);
                    lane.Advance(2);combat.Advance();Assert.That(combat.HitCount,Is.EqualTo(2));Assert.That(combat.IsVictorious,Is.True);
                }
            }
        }
        [Test]
        public void RescueOnlyOneRemainingShipStillUsesOneItem()
        {
            using(var s=Single())
            { var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock()))
              {Assert.That(t.Rescue(),Is.EqualTo(ToolUseStatus.Applied));Assert.That(t.LastAffectedIds.Count,Is.EqualTo(1));Assert.That(t.Remaining(ShipTool.Rescue),Is.Zero);} }
        }
        [Test]
        public void RandomRescueNeverSelectsAnEnclosedInteriorShip()
        {
            var ring=new List<ShipPlacementData>();
            foreach(var y in new[]{0,5})foreach(var x in new[]{0,2,4})ring.Add(Ship("H"+x+"_"+y,x,y,ShipDirection.Right));
            foreach(var x in new[]{0,5})foreach(var y in new[]{1,3})ring.Add(Ship("V"+x+"_"+y,x,y,ShipDirection.Up));
            ring.Add(Ship("INNER",2,2,ShipDirection.Right));var selected=new HashSet<string>();
            for(var seed=0;seed<12;seed++)using(var s=ShipMovementSystemTests.CreateSession(6,6,ring.ToArray()))
            {
                Assert.That(ShipToolSystem.PeripheralShips(s.Board),Does.Not.Contain("INNER"));
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock(),seed:seed))
                {t.Rescue();Assert.That(t.LastAffectedIds,Does.Not.Contain("INNER"));foreach(var id in t.LastAffectedIds)selected.Add(id);}
            }
            Assert.That(selected.Count,Is.GreaterThan(2));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)]
        public void ShuffleRemainingCandidatePreservesIdentityAndDepartedAttack(int index)
        {
            var path=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates",Phase5RLevelRecipes.All[index].LevelId+".json");
            using(var s=ReverseLevelGeneratorTests.Create(LevelJsonReader.Read(File.ReadAllText(path))))
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock(),options:new LevelSolverOptions(4000,400000,500),seed:20260920+index))
                using(var lane=new TransitSystem(s,new LaneTransitTiming(.1,.01,.01))) using(var combat=new FleetCombatSystem(s,lane))
                {
                    var departing=s.Board.Ships.First(x=>s.Board.QueryForwardPath(x.Id).CanExit);var op=m.TryBeginMove(departing.Id);m.CompleteTravel(op.Operation.OperationId);
                    var departed=s.GetShip(departing.Id);var pos=departed.Position;var board=s.Board;var ships=s.Ships.ToArray();
                    Assert.That(t.Shuffle(),Is.EqualTo(ToolUseStatus.Applied));Assert.That(s.Board,Is.Not.SameAs(board));
                    Assert.That(t.LastAffectedIds.Count,Is.EqualTo(Math.Min(5,board.ShipCount)));
                    foreach(var unchanged in board.Ships.Where(x=>!t.LastAffectedIds.Contains(x.Id)))
                    {Assert.That(s.Board.GetShip(unchanged.Id).Position,Is.EqualTo(unchanged.Position));Assert.That(s.Board.GetShip(unchanged.Id).Direction,Is.EqualTo(unchanged.Direction));}
                    CollectionAssert.AreEquivalent(board.Ships.Select(x=>x.Id),s.Board.Ships.Select(x=>x.Id));
                    foreach(var ship in s.Board.Ships) {Assert.That(ship.Length,Is.EqualTo(board.GetShip(ship.Id).Length));Assert.That(s.GetShip(ship.Id).Position,Is.EqualTo(ship.Position));}
                    CollectionAssert.AreEqual(ships,s.Ships);Assert.That(departed.Position,Is.EqualTo(pos));Assert.That(departed.State,Is.EqualTo(ShipState.InLane));
                    Assert.That(t.Remaining(ShipTool.Shuffle),Is.Zero);Assert.That(t.Shuffle(),Is.EqualTo(ToolUseStatus.NoUses));
                    var solution=LevelSolver.Solve(s.Board);Assert.That(solution.Status,Is.EqualTo(LevelSolverStatus.Solved));
                    foreach(var id in solution.ShipIds) {var request=m.TryBeginMove(id);m.CompleteTravel(request.Operation.OperationId);if(m.IsBusy)m.CompleteBlockedFeedback(request.Operation.OperationId);}
                    lane.Advance(30);combat.Advance();Assert.That(combat.HitCount,Is.EqualTo(ships.Length));Assert.That(combat.IsVictorious,Is.True);
                }
            }
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void ShuffleWithFewerThanFiveChangesEveryRemainingShip(int count)
        {
            using(var s=ShipMovementSystemTests.CreateSession(10,8,Enumerable.Range(0,count).Select(i=>Ship("S"+i,i*2,3,ShipDirection.Up)).ToArray()))
            {var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock(),seed:3))
             {Assert.That(t.Shuffle(),Is.EqualTo(ToolUseStatus.Applied));Assert.That(t.LastAffectedIds.Count,Is.EqualTo(count));}}
        }
        [TestCase(0,false)][TestCase(8,true)]
        public void FailedShuffleIsFreeAndAtomic(int attempts,bool tiny)
        {
            using(var s=Single())
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();using(var t=new ShipToolSystem(s,m,Stock(),options:tiny?Tiny:null,shuffleAttempts:attempts))
                {var board=s.Board;Assert.That(t.Shuffle(),Is.EqualTo(ToolUseStatus.Unproven));Assert.That(s.Board,Is.SameAs(board));Assert.That(t.Remaining(ShipTool.Shuffle),Is.EqualTo(1));}
            }
        }
        [Test]
        public void MovableUnsolvableAndBudgetUnknownAreDifferent()
        {
            using(var s=ShipMovementSystemTests.CreateSession(6,3,Ship("A",0,0,ShipDirection.Right),Ship("B",5,0,ShipDirection.Left)))
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();var unknown=new BoardProgressMonitor(s,m,Tiny);
                Assert.That(unknown.Refresh(),Is.EqualTo(BoardProgressStatus.Unknown));Assert.That(unknown.NeedsRescue,Is.False);
                var monitor=new BoardProgressMonitor(s,m);Assert.That(monitor.Refresh(),Is.EqualTo(BoardProgressStatus.Unsolvable));Assert.That(monitor.NeedsRescue,Is.True);
                Assert.That(s.State,Is.EqualTo(GameState.Playing));var op=m.TryBeginMove("A");Assert.That(monitor.Refresh(),Is.EqualTo(BoardProgressStatus.Busy));
                m.CompleteTravel(op.Operation.OperationId);m.CompleteBlockedFeedback(op.Operation.OperationId);Assert.That(monitor.Refresh(),Is.EqualTo(BoardProgressStatus.NoMoves));
                Assert.That(monitor.EndForRestart(),Is.True);Assert.That(s.EndReason,Is.EqualTo("DeadlockAbandoned"));
            }
        }
        [Test]
        public void NoInitialExitButPartialEscapeIsNotADeadlock()
        {
            using(var s=ShipMovementSystemTests.CreateSession(6,5,Ship("A",0,2,ShipDirection.Right),Ship("B",4,3,ShipDirection.Down),
                Ship("C",5,0,ShipDirection.Left),Ship("D",1,0,ShipDirection.Up)))
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();var monitor=new BoardProgressMonitor(s,m);
                Assert.That(s.Board.Ships.Any(x=>s.Board.QueryForwardPath(x.Id).CanExit),Is.False);
                Assert.That(monitor.Refresh(),Is.EqualTo(BoardProgressStatus.Solvable));Assert.That(monitor.NeedsRescue,Is.False);
            }
        }
        [TestCase(GameState.Victory)][TestCase(GameState.Failed)]
        public void TerminalOutcomeIsOnceOnlyAndRestartCannotOverrideWin(GameState first)
        {
            using(var s=Single())
            {
                var m=new ShipMovementSystem(s);m.StartPlaying();var ended=0;var wins=0;
                s.Events.Subscribe<AttemptEndedEvent>(e=>{ended++;Assert.That(e.Outcome,Is.EqualTo(first));Assert.That(s.TryEnd(GameState.Failed,"late"),Is.False);});
                s.Events.Subscribe<GameWinEvent>(_=>wins++);Assert.That(s.TryEnd(first,"test"),Is.True);
                Assert.That(new BoardProgressMonitor(s,m).EndForRestart(),Is.False);Assert.That(ended,Is.EqualTo(1));Assert.That(wins,Is.EqualTo(first==GameState.Victory?1:0));
            }
        }
        [Test]
        public void EndingSubscriberMayDisposeSessionSafely()
        {
            var s=Single();s.Events.Subscribe<AttemptEndedEvent>(_=>s.Dispose());Assert.DoesNotThrow(()=>s.TryEnd(GameState.Victory,"test"));s.Dispose();
        }
    }
}
