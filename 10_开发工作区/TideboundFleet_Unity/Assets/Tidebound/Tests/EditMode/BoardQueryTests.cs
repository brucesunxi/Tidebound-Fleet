using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class BoardQueryTests
    {
        [Test]
        public void BoardIndexesCompleteFootprintsAndExposesCopiedShipSnapshots()
        {
            using (var session = CreateSession(6, 4,
                Ship("A", 1, 1, ShipDirection.Right),
                Ship("B", 5, 2, ShipDirection.Down)))
            {
                var board = session.Board;
                Assert.That(board.Width, Is.EqualTo(6));
                Assert.That(board.Height, Is.EqualTo(4));
                Assert.That(board.OccupiedCellCount, Is.EqualTo(4));
                Assert.That(board.IsInside(new GridPosition(0, 0)), Is.True);
                Assert.That(board.IsInside(new GridPosition(5, 3)), Is.True);
                Assert.That(board.IsInside(new GridPosition(-1, 0)), Is.False);
                Assert.That(board.IsInside(new GridPosition(6, 0)), Is.False);
                Assert.That(board.IsOccupied(new GridPosition(2, 1)), Is.True);
                Assert.That(board.GetShipId(new GridPosition(2, 1)), Is.EqualTo("A"));
                Assert.That(board.GetShipId(new GridPosition(0, 0)), Is.Null);
                Assert.That(board.Ships.Count, Is.EqualTo(2));

                var ship = board.GetShip("A");
                Assert.That(ship.TypeId, Is.EqualTo("TF_SPEEDBOAT"));
                Assert.That(ship.Position, Is.EqualTo(new GridPosition(1, 1)));
                Assert.That(ship.Direction, Is.EqualTo(ShipDirection.Right));
                Assert.That(ship.Length, Is.EqualTo(2));
                CollectionAssert.AreEqual(
                    new[] { new GridPosition(1, 1), new GridPosition(2, 1) }, ship.OccupiedCells);
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<GridPosition>)ship.OccupiedCells).Add(new GridPosition(3, 1)));
            }
        }

        [Test]
        public void ImmediateBlockReturnsZeroTravelAndKeepsTheOriginTail()
        {
            using (var session = CreateSession(6, 3,
                Ship("A", 0, 0, ShipDirection.Right),
                Ship("B", 2, 0, ShipDirection.Up)))
            {
                var result = session.Board.QueryForwardPath("A");
                Assert.That(result.Outcome, Is.EqualTo(ForwardPathOutcome.Blocked));
                Assert.That(result.IsBlocked, Is.True);
                Assert.That(result.CanExit, Is.False);
                Assert.That(result.ClearCellCount, Is.Zero);
                Assert.That(result.TravelDistance, Is.Zero);
                Assert.That(result.OriginTail, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(result.TargetTail, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(result.BlockerShipId, Is.EqualTo("B"));
                Assert.That(result.BlockerCell, Is.EqualTo(new GridPosition(2, 0)));
            }
        }

        [Test]
        public void BlockAfterGapStopsAtTheLastLegalTailPosition()
        {
            using (var session = CreateSession(7, 3,
                Ship("A", 0, 0, ShipDirection.Right),
                Ship("B", 4, 0, ShipDirection.Up)))
            {
                var result = session.Board.QueryForwardPath("A");
                CollectionAssert.AreEqual(
                    new[] { new GridPosition(2, 0), new GridPosition(3, 0) }, result.ClearCells);
                Assert.That(result.TravelDistance, Is.EqualTo(2));
                Assert.That(result.TargetTail, Is.EqualTo(new GridPosition(2, 0)));
                Assert.That(result.BlockerShipId, Is.EqualTo("B"));
                Assert.That(result.BlockerCell, Is.EqualTo(new GridPosition(4, 0)));
            }
        }

        [Test]
        public void BlockedTransactionMovesOccupancyAtomicallyAndPreservesTheSourceBoard()
        {
            using (var session = CreateSession(7, 3,
                Ship("A", 0, 0, ShipDirection.Right),
                Ship("B", 4, 0, ShipDirection.Up)))
            {
                var source = session.Board;
                var query = source.QueryForwardPath("A");
                var moved = source.ApplyPathResult(query);

                Assert.That(source.GetShip("A").Position, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(source.GetShipId(new GridPosition(0, 0)), Is.EqualTo("A"));
                Assert.That(moved.GetShip("A").Position, Is.EqualTo(new GridPosition(2, 0)));
                Assert.That(moved.GetShipId(new GridPosition(0, 0)), Is.Null);
                Assert.That(moved.GetShipId(new GridPosition(2, 0)), Is.EqualTo("A"));
                Assert.That(moved.GetShipId(new GridPosition(3, 0)), Is.EqualTo("A"));
                Assert.That(moved.GetShipId(new GridPosition(4, 0)), Is.EqualTo("B"));
                Assert.That(moved.OccupiedCellCount, Is.EqualTo(source.OccupiedCellCount));
                Assert.Throws<InvalidOperationException>(() => moved.ApplyPathResult(query));
            }
        }

        [Test]
        public void ZeroDistanceTransactionKeepsEveryOccupiedCell()
        {
            using (var session = CreateSession(6, 3,
                Ship("A", 0, 0, ShipDirection.Right),
                Ship("B", 2, 0, ShipDirection.Up)))
            {
                var source = session.Board;
                var unchanged = source.ApplyPathResult(source.QueryForwardPath("A"));
                Assert.That(unchanged, Is.SameAs(source));
                Assert.That(unchanged.OccupiedCellCount, Is.EqualTo(4));
                Assert.That(unchanged.GetShipId(new GridPosition(0, 0)), Is.EqualTo("A"));
                Assert.That(unchanged.GetShipId(new GridPosition(1, 0)), Is.EqualTo("A"));
            }
        }

        [Test]
        public void ExitTransactionRemovesOnlyTheDepartingShip()
        {
            using (var session = CreateSession(6, 4,
                Ship("A", 1, 0, ShipDirection.Right),
                Ship("B", 0, 2, ShipDirection.Up)))
            {
                var source = session.Board;
                var next = source.ApplyPathResult(source.QueryForwardPath("A"));
                BoardShipSnapshot ignored;
                Assert.That(next.ShipCount, Is.EqualTo(1));
                Assert.That(next.OccupiedCellCount, Is.EqualTo(2));
                Assert.That(next.TryGetShip("A", out ignored), Is.False);
                Assert.That(next.GetShipId(new GridPosition(0, 2)), Is.EqualTo("B"));
                Assert.That(source.ShipCount, Is.EqualTo(2));
                Assert.That(source.GetShipId(new GridPosition(1, 0)), Is.EqualTo("A"));
            }
        }

        [TestCase(ShipDirection.Up, 2, 1, 2, 6)]
        [TestCase(ShipDirection.Down, 2, 4, 2, -1)]
        [TestCase(ShipDirection.Left, 4, 2, -1, 2)]
        [TestCase(ShipDirection.Right, 1, 2, 6, 2)]
        public void ClearPathInEveryDirectionTargetsACompletelyExitedTail(
            ShipDirection direction, int tailX, int tailY, int targetX, int targetY)
        {
            using (var session = CreateSession(6, 6, Ship("A", tailX, tailY, direction)))
            {
                var result = session.Board.QueryForwardPath("A");
                Assert.That(result.Outcome, Is.EqualTo(ForwardPathOutcome.Exit));
                Assert.That(result.CanExit, Is.True);
                Assert.That(result.IsBlocked, Is.False);
                Assert.That(result.ClearCellCount, Is.EqualTo(3));
                Assert.That(result.TravelDistance, Is.EqualTo(5));
                Assert.That(result.TargetTail, Is.EqualTo(new GridPosition(targetX, targetY)));
                Assert.That(result.BlockerShipId, Is.Null);
                Assert.That(result.BlockerCell, Is.Null);
            }
        }

        [Test]
        public void TestLevelQueriesMatchTheAuthoredOpeningBlockers()
        {
            using (var session = LevelConfigLoader.Load(LevelLoadingTests.Fixture()))
            {
                var first = session.Board.QueryForwardPath("S001");
                Assert.That(first.IsBlocked, Is.True);
                Assert.That(first.ClearCellCount, Is.EqualTo(1));
                Assert.That(first.TargetTail, Is.EqualTo(new GridPosition(1, 0)));
                Assert.That(first.BlockerShipId, Is.EqualTo("S002"));

                var second = session.Board.QueryForwardPath("S002");
                Assert.That(second.IsBlocked, Is.True);
                Assert.That(second.ClearCellCount, Is.EqualTo(1));
                Assert.That(second.BlockerShipId, Is.EqualTo("S005"));

                var free = session.Board.QueryForwardPath("S005");
                Assert.That(free.CanExit, Is.True);
                Assert.That(free.ClearCellCount, Is.EqualTo(1));
                Assert.That(free.TravelDistance, Is.EqualTo(3));
                Assert.That(free.TargetTail, Is.EqualTo(new GridPosition(3, 6)));
            }
        }

        [Test]
        public void TestLevelReferenceSequenceClearsTheImmutableBoardWithoutEditingTheSession()
        {
            using (var session = LevelConfigLoader.Load(LevelLoadingTests.Fixture()))
            {
                var board = session.Board;
                var sequence = new[] { "S005", "S002", "S001", "S004", "S006", "S003", "S007" };
                foreach (var shipId in sequence)
                {
                    var query = board.QueryForwardPath(shipId);
                    Assert.That(query.CanExit, Is.True, shipId + " should have a clear path in the reference sequence.");
                    board = board.ApplyPathResult(query);
                }

                Assert.That(board.ShipCount, Is.Zero);
                Assert.That(board.OccupiedCellCount, Is.Zero);
                Assert.That(session.Board.ShipCount, Is.EqualTo(7));
                Assert.That(session.Ships.Count, Is.EqualTo(7));
                Assert.That(session.Ships[0].State, Is.EqualTo(ShipState.Idle));
            }
        }

        [Test]
        public void BoardQueriesStayConsistentWhenRuntimeObjectsAreEditedDirectly()
        {
            using (var session = CreateSession(6, 4, Ship("A", 0, 0, ShipDirection.Right)))
            {
                session.Ships[0].Position = new GridPosition(4, 3);
                session.Ships[0].Direction = ShipDirection.Left;

                var registered = session.Board.GetShip("A");
                Assert.That(registered.Position, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(registered.Direction, Is.EqualTo(ShipDirection.Right));
                Assert.That(session.Board.GetShipId(new GridPosition(0, 0)), Is.EqualTo("A"));
                Assert.That(session.Board.QueryForwardPath("A").TargetTail, Is.EqualTo(new GridPosition(6, 0)));
            }
        }

        [Test]
        public void UnknownOrEmptyShipIdsFailWithoutChangingTheBoard()
        {
            using (var session = CreateSession(4, 3, Ship("A", 0, 0, ShipDirection.Right)))
            {
                BoardShipSnapshot ignored;
                Assert.That(session.Board.TryGetShip(null, out ignored), Is.False);
                Assert.That(session.Board.TryGetShip("missing", out ignored), Is.False);
                Assert.Throws<ArgumentException>(() => session.Board.GetShip(""));
                Assert.Throws<KeyNotFoundException>(() => session.Board.QueryForwardPath("missing"));
                Assert.That(session.Board.OccupiedCellCount, Is.EqualTo(2));
            }
        }

        private static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction) =>
            new ShipPlacementData
            {
                Id = id,
                TypeId = "TF_SPEEDBOAT",
                Position = new GridPosition(x, y),
                Direction = direction
            };

        private static GameSession CreateSession(int width, int height, params ShipPlacementData[] ships)
        {
            var level = new LevelData
            {
                SchemaVersion = 1,
                LevelId = "BoardQueryTest",
                Width = width,
                Height = height,
                BossId = "TF_KRAKEN_01",
                Ships = ships
            };
            return LevelSessionFactory.Create(
                level,
                new[] { new ShipDefinition("TF_SPEEDBOAT", 2, 10) },
                new[] { new BossDefinition("TF_KRAKEN_01") });
        }
    }
}
