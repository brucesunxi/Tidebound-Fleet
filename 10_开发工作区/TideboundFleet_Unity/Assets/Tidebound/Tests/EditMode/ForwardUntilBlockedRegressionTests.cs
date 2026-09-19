using NUnit.Framework;
using Tidebound.Board;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class ForwardUntilBlockedRegressionTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ForwardStopAdjacentStopThenForwardAgainAndExitInEveryDirection(int turns)
        {
            using (var session = ShipMovementSystemTests.CreateSession(7, 7,
                       Rotate(LevelSolverTests.Ship("A", 0, 2, ShipDirection.Right), turns),
                       Rotate(LevelSolverTests.Ship("B", 4, 1, ShipDirection.Up), turns),
                       Rotate(LevelSolverTests.Ship("C", 6, 1, ShipDirection.Up), turns)))
            {
                var movement = new ShipMovementSystem(session);
                movement.StartPlaying();
                var start = session.Board.GetShip("A").Position;
                var first = movement.TryBeginMove("A");
                Assert.That(first.Operation.TravelDistance, Is.EqualTo(2));
                Assert.That(session.Board.GetShip("A").Position, Is.EqualTo(start));
                movement.CompleteTravel(first.Operation.OperationId);
                var stop = Rotate(new GridPosition(2, 2), turns);
                Assert.That(session.Board.GetShip("A").Position, Is.EqualTo(stop));
                Assert.That(session.GetShip("A").Position, Is.EqualTo(stop));
                movement.CompleteBlockedFeedback(first.Operation.OperationId);
                var adjacent = movement.TryBeginMove("A");
                Assert.That(adjacent.Operation.TravelDistance, Is.Zero);
                Assert.That(adjacent.Operation.Stage, Is.EqualTo(ShipMoveStage.BlockedFeedback));
                movement.CompleteBlockedFeedback(adjacent.Operation.OperationId);
                Assert.That(session.Board.GetShip("A").Position, Is.EqualTo(stop));
                Assert.That(session.Board.OccupiedCellCount, Is.EqualTo(6));
                Exit(movement, "B");
                Assert.That(BoardDependencyAnalyzer.Analyze(session.Board).GetNode("A").BlockerShipId, Is.EqualTo("C"));
                var again = movement.TryBeginMove("A");
                Assert.That(again.Operation.TravelDistance, Is.EqualTo(2));
                movement.CompleteTravel(again.Operation.OperationId);
                movement.CompleteBlockedFeedback(again.Operation.OperationId);
                Assert.That(session.Board.GetShip("A").Position, Is.EqualTo(Rotate(new GridPosition(4, 2), turns)));
                Exit(movement, "C");
                Exit(movement, "A");
                Assert.That(session.Board.ShipCount, Is.Zero);
            }
        }
        private static void Exit(ShipMovementSystem movement, string id)
        {
            var move = movement.TryBeginMove(id);
            Assert.That(move.IsAccepted, Is.True);
            Assert.That(move.Operation.Stage, Is.EqualTo(ShipMoveStage.Traveling));
            Assert.That(movement.CompleteTravel(move.Operation.OperationId), Is.EqualTo(ShipMoveAdvanceStatus.Applied));
        }
        private static ShipPlacementData Rotate(ShipPlacementData ship, int turns)
        {
            var directions = new[] { ShipDirection.Right, ShipDirection.Up, ShipDirection.Left, ShipDirection.Down };
            var index = System.Array.IndexOf(directions, ship.Direction);
            return new ShipPlacementData { Id = ship.Id, TypeId = ship.TypeId, Length = ship.Length,
                Position = Rotate(ship.Position, turns), Direction = directions[(index + turns) % 4] };
        }
        private static GridPosition Rotate(GridPosition p, int turns)
        {
            for (var i = 0; i < turns; i++) p = new GridPosition(6 - p.Y, p.X);
            return p;
        }
    }
}
