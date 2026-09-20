using System;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;

namespace Tidebound.Tests
{
    public sealed class BoardAssistanceTests
    {
        private static ShipPlacementData Ship(string id, int x, int y, ShipDirection d) => LevelSolverTests.Ship(id,x,y,d);
        [TestCase(ShipDirection.Up)][TestCase(ShipDirection.Down)][TestCase(ShipDirection.Left)][TestCase(ShipDirection.Right)]
        public void OnlyAfterFiveSecondsHighlightsAnActualDirectExit(ShipDirection direction)
        {
            using(var s=ShipMovementSystemTests.CreateSession(8,8,Ship("A",3,3,direction)))
            {
                var a=new BoardAssistance();a.Advance(s.Board,4.9,true,false,true);Assert.That(a.HighlightedShipId,Is.Null);
                a.Advance(s.Board,.1,true,false,true);Assert.That(a.HighlightedShipId,Is.EqualTo("A"));
                Assert.That(s.Board.QueryForwardPath(a.HighlightedShipId).CanExit,Is.True);
                Assert.That(s.Board.ShipCount,Is.EqualTo(1));Assert.That(s.ToolUses,Is.Zero);
                a.Advance(s.Board,1.4,true,false,true);Assert.That(a.HighlightedShipId,Is.Null);
                a.Advance(s.Board,60,true,false,true);Assert.That(a.HighlightedShipId,Is.Null,"No repeated pulsing in the same idle cycle.");
                a.Advance(s.Board,1,true,true,true);a.Advance(s.Board,5,true,false,true);Assert.That(a.HighlightedShipId,Is.EqualTo("A"));
            }
        }
        [Test]
        public void PartialOnlySolvableBoardGetsNeitherDirectHintNorDeadlockModal()
        {
            using(var s=ShipMovementSystemTests.CreateSession(6,5,Ship("A",0,2,ShipDirection.Right),Ship("B",4,3,ShipDirection.Down),Ship("C",5,0,ShipDirection.Left),Ship("D",1,0,ShipDirection.Up)))
            {
                var a=new BoardAssistance();a.Advance(s.Board,10,true,false,true);
                Assert.That(a.HighlightedShipId,Is.Null);Assert.That(a.NoMoves,Is.False);Assert.That(a.TryAnnounceDeadlock(),Is.False);
            }
        }
        [Test]
        public void CompleteSolvabilityIsNotRequiredWhenOneShipCanExit()
        {
            using(var s=ShipMovementSystemTests.CreateSession(6,5,Ship("A",0,0,ShipDirection.Right),Ship("B",3,0,ShipDirection.Left),Ship("C",3,3,ShipDirection.Up)))
            {
                var a=new BoardAssistance();a.Advance(s.Board,5,true,false,true);
                Assert.That(a.HighlightedShipId,Is.EqualTo("C"));Assert.That(a.NoMoves,Is.False);
            }
        }
        [TestCase(false,false,true)][TestCase(true,true,true)][TestCase(true,false,false)]
        public void IneligibleInputOrDisabledHintsResetTimer(bool eligible,bool input,bool enabled)
        {
            using(var s=ShipMovementSystemTests.CreateSession(8,8,Ship("A",3,3,ShipDirection.Up)))
            {
                var a=new BoardAssistance();a.Advance(s.Board,4.9,true,false,true);
                a.Advance(s.Board,100,eligible,input,enabled);Assert.That(a.HighlightedShipId,Is.Null);
                a.Advance(s.Board,4.9,true,false,true);Assert.That(a.HighlightedShipId,Is.Null);
                a.Advance(s.Board,.1,true,false,true);Assert.That(a.HighlightedShipId,Is.EqualTo("A"));
            }
        }
        [Test]
        public void NewBoardClearsStaleHintAndRearmsDeadlockOnce()
        {
            using(var s=ShipMovementSystemTests.CreateSession(6,3,Ship("A",0,0,ShipDirection.Right),Ship("B",5,0,ShipDirection.Left)))
            {
                var a=new BoardAssistance();a.Observe(s.Board);Assert.That(a.NoMoves,Is.False);
                var m=new ShipMovementSystem(s);m.StartPlaying();var op=m.TryBeginMove("A");
                m.CompleteTravel(op.Operation.OperationId);m.CompleteBlockedFeedback(op.Operation.OperationId);
                a.Observe(s.Board);Assert.That(a.TryAnnounceDeadlock(),Is.True);Assert.That(a.TryAnnounceDeadlock(),Is.False);
                a.ResetIdle();a.Advance(s.Board,20,false,true,false);Assert.That(a.TryAnnounceDeadlock(),Is.False,"Modal eligibility/input changes must not rearm dismissal.");
                a.Observe(new BoardModel(s.Width,s.Height,s.Ships));Assert.That(a.TryAnnounceDeadlock(),Is.True);
            }
        }
        [Test]
        public void EmptyBoardDoesNotBecomeDeadlockOrKeepAnExitedHint()
        {
            using(var s=ShipMovementSystemTests.CreateSession(5,3,Ship("A",0,0,ShipDirection.Right)))
            {
                var a=new BoardAssistance();a.Advance(s.Board,5,true,false,true);Assert.That(a.HighlightedShipId,Is.EqualTo("A"));
                var m=new ShipMovementSystem(s);m.StartPlaying();var op=m.TryBeginMove("A");m.CompleteTravel(op.Operation.OperationId);
                a.Advance(s.Board,5,true,false,true);Assert.That(a.HighlightedShipId,Is.Null);Assert.That(a.NoMoves,Is.False);Assert.That(a.TryAnnounceDeadlock(),Is.False);
            }
        }
        [Test]
        public void TransientModalPauseIsNotPersistedButUserPauseIs()
        {
            using(var s=ShipMovementSystemTests.CreateSession(5,3,Ship("A",0,0,ShipDirection.Right)))
            using(var runtime=new SavedGameRuntime(s,1))
            {
                runtime.Movement.Pause();Assert.That(runtime.Capture().Paused,Is.True);
                runtime.PresentationPause=true;var snapshot=runtime.Capture();Assert.That(snapshot.Paused,Is.False);
                using(var restored=SavedGameRuntime.Restore(snapshot))Assert.That(restored.Capture().Paused,Is.False);
                runtime.PresentationPause=false;Assert.That(runtime.Capture().Paused,Is.True);
            }
        }
        [TestCase(-1)][TestCase(double.NaN)][TestCase(double.PositiveInfinity)]
        public void InvalidDeltaCannotCorruptTimer(double seconds)
        {Assert.Throws<ArgumentOutOfRangeException>(()=>new BoardAssistance().Advance(null,seconds,true,false,true));}
    }
}
