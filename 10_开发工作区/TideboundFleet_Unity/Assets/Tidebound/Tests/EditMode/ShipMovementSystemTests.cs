using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class ShipMovementSystemTests
    {
        [Test]
        public void SessionLifecycleControlsWhetherClicksAndCompletionAreAccepted()
        {
            using (var session = CreateSession(5, 3, Ship("A", 0, 0, ShipDirection.Right)))
            {
                var movement = new ShipMovementSystem(session);
                Assert.That(movement.TryBeginMove("A").Status, Is.EqualTo(ShipMoveRequestStatus.SessionNotPlaying));
                Assert.That(movement.StartPlaying(), Is.True);
                Assert.That(movement.StartPlaying(), Is.False);

                var request = movement.TryBeginMove("A");
                Assert.That(request.IsAccepted, Is.True);
                Assert.That(movement.Pause(), Is.True);
                Assert.That(session.State, Is.EqualTo(GameState.Paused));
                Assert.That(movement.TryBeginMove("A").Status, Is.EqualTo(ShipMoveRequestStatus.SessionPaused));
                Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                    Is.EqualTo(ShipMoveAdvanceStatus.SessionPaused));
                Assert.That(session.Board.ShipCount, Is.EqualTo(1));

                Assert.That(movement.Resume(), Is.True);
                Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                    Is.EqualTo(ShipMoveAdvanceStatus.Applied));
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InLane));
                Assert.That(movement.Resume(), Is.False);
            }
        }

        [Test]
        public void BlockedTravelCommitsAtArrivalThenUnlocksAfterFeedback()
        {
            using (var session = CreateSession(7, 3,
                Ship("A", 0, 0, ShipDirection.Right),
                Ship("B", 4, 0, ShipDirection.Up)))
            {
                var movement = new ShipMovementSystem(session);
                var eventOrder = new List<string>();
                ShipMoveCompleteEvent completion = default;
                session.Events.Subscribe<ShipMoveStartEvent>(_ => eventOrder.Add("start"));
                session.Events.Subscribe<ShipMoveCompleteEvent>(x => { eventOrder.Add("complete"); completion = x; });
                movement.StartPlaying();

                var request = movement.TryBeginMove("A");
                Assert.That(request.Status, Is.EqualTo(ShipMoveRequestStatus.Accepted));
                Assert.That(request.Operation.Stage, Is.EqualTo(ShipMoveStage.Traveling));
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Moving));
                Assert.That(session.GetShip("A").Position, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(session.Board.GetShip("A").Position, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(movement.TryBeginMove("B").Status, Is.EqualTo(ShipMoveRequestStatus.Busy));
                Assert.That(movement.CompleteTravel(request.Operation.OperationId + 1),
                    Is.EqualTo(ShipMoveAdvanceStatus.OperationMismatch));

                Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                    Is.EqualTo(ShipMoveAdvanceStatus.Applied));
                Assert.That(session.GetShip("A").Position, Is.EqualTo(new GridPosition(2, 0)));
                Assert.That(session.Board.GetShip("A").Position, Is.EqualTo(new GridPosition(2, 0)));
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.BlockedFeedback));
                Assert.That(request.Operation.Stage, Is.EqualTo(ShipMoveStage.BlockedFeedback));
                Assert.That(completion.From, Is.EqualTo(new GridPosition(0, 0)));
                Assert.That(completion.To, Is.EqualTo(new GridPosition(2, 0)));
                Assert.That(completion.WasBlocked, Is.True);
                CollectionAssert.AreEqual(new[] { "start", "complete" }, eventOrder);
                Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                    Is.EqualTo(ShipMoveAdvanceStatus.WrongStage));

                Assert.That(movement.CompleteBlockedFeedback(request.Operation.OperationId),
                    Is.EqualTo(ShipMoveAdvanceStatus.Applied));
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Idle));
                Assert.That(request.Operation.Stage, Is.EqualTo(ShipMoveStage.Completed));
                Assert.That(movement.IsBusy, Is.False);
            }
        }

        [Test]
        public void ImmediateBlockPublishesCompletionBeforeFeedbackWithoutLosingOccupancy()
        {
            using (var session = CreateSession(6, 3,
                Ship("A", 0, 0, ShipDirection.Right),
                Ship("B", 2, 0, ShipDirection.Up)))
            {
                var movement = new ShipMovementSystem(session);
                var starts = 0;
                var completes = 0;
                session.Events.Subscribe<ShipMoveStartEvent>(_ => starts++);
                session.Events.Subscribe<ShipMoveCompleteEvent>(_ => completes++);
                movement.StartPlaying();

                var request = movement.TryBeginMove("A");
                Assert.That(request.IsAccepted, Is.True);
                Assert.That(request.Operation.TravelDistance, Is.Zero);
                Assert.That(request.Operation.Stage, Is.EqualTo(ShipMoveStage.BlockedFeedback));
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.BlockedFeedback));
                Assert.That(session.Board.OccupiedCellCount, Is.EqualTo(4));
                Assert.That(session.Board.GetShipId(new GridPosition(0, 0)), Is.EqualTo("A"));
                Assert.That(session.Board.GetShipId(new GridPosition(1, 0)), Is.EqualTo("A"));
                Assert.That(starts, Is.EqualTo(1));
                Assert.That(completes, Is.EqualTo(1));

                movement.CompleteBlockedFeedback(request.Operation.OperationId);
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Idle));
            }
        }

        [Test]
        public void CompleteExitReleasesBoardSetsInLaneAndAllocatesStableSequence()
        {
            using (var session = CreateSession(5, 5,
                Ship("A", 0, 3, ShipDirection.Right),
                Ship("B", 0, 0, ShipDirection.Up)))
            {
                var movement = new ShipMovementSystem(session);
                var exits = new List<ShipExitBoardEvent>();
                var completes = new List<ShipMoveCompleteEvent>();
                session.Events.Subscribe<ShipExitBoardEvent>(exits.Add);
                session.Events.Subscribe<ShipMoveCompleteEvent>(completes.Add);
                movement.StartPlaying();

                Exit(movement, "A");
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InLane));
                Assert.That(session.GetShip("A").Position, Is.EqualTo(new GridPosition(5, 3)));
                Assert.That(session.Board.TryGetShip("A", out _), Is.False);
                Assert.That(session.InitialBoard.TryGetShip("A", out _), Is.True);
                Assert.That(movement.TryBeginMove("A").Status, Is.EqualTo(ShipMoveRequestStatus.ShipNotIdle));

                Exit(movement, "B");
                Assert.That(session.Board.ShipCount, Is.Zero);
                Assert.That(exits.Count, Is.EqualTo(2));
                Assert.That(exits[0].Ship.ShipId, Is.EqualTo("A"));
                Assert.That(exits[0].ExitSequence, Is.EqualTo(1));
                Assert.That(exits[1].Ship.ShipId, Is.EqualTo("B"));
                Assert.That(exits[1].ExitSequence, Is.EqualTo(2));
                Assert.That(completes.Count, Is.EqualTo(2));
                Assert.That(completes.TrueForAll(x => !x.WasBlocked), Is.True);
            }
        }

        [Test]
        public void UnknownAndNonIdleShipsAreRejectedWithoutEvents()
        {
            using (var session = CreateSession(4, 3, Ship("A", 0, 0, ShipDirection.Right)))
            {
                var movement = new ShipMovementSystem(session);
                var starts = 0;
                session.Events.Subscribe<ShipMoveStartEvent>(_ => starts++);
                movement.StartPlaying();
                Assert.That(movement.TryBeginMove("missing").Status, Is.EqualTo(ShipMoveRequestStatus.ShipNotFound));
                session.GetShip("A").State = ShipState.InLane;
                Assert.That(movement.TryBeginMove("A").Status, Is.EqualTo(ShipMoveRequestStatus.ShipNotIdle));
                Assert.That(starts, Is.Zero);
                Assert.That(movement.IsBusy, Is.False);
            }
        }

        [Test]
        public void TestLevelReferenceSequenceProducesSevenOrderedExits()
        {
            using (var session = LevelConfigLoader.Load(LevelLoadingTests.Fixture()))
            {
                var movement = new ShipMovementSystem(session);
                var exitIds = new List<string>();
                var sequences = new List<long>();
                session.Events.Subscribe<ShipExitBoardEvent>(x =>
                {
                    exitIds.Add(x.Ship.ShipId);
                    sequences.Add(x.ExitSequence);
                });
                movement.StartPlaying();
                var order = new[] { "S005", "S002", "S001", "S004", "S006", "S003", "S007" };
                foreach (var shipId in order) Exit(movement, shipId);

                CollectionAssert.AreEqual(order, exitIds);
                CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4, 5, 6, 7 }, sequences);
                Assert.That(session.Board.ShipCount, Is.Zero);
                Assert.That(session.Board.OccupiedCellCount, Is.Zero);
                Assert.That(session.InitialBoard.ShipCount, Is.EqualTo(7));
                foreach (var ship in session.Ships) Assert.That(ship.State, Is.EqualTo(ShipState.InLane));
            }
        }

        private static void Exit(ShipMovementSystem movement, string shipId)
        {
            var request = movement.TryBeginMove(shipId);
            Assert.That(request.Status, Is.EqualTo(ShipMoveRequestStatus.Accepted), shipId);
            Assert.That(request.Operation.WillExit, Is.True, shipId);
            Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                Is.EqualTo(ShipMoveAdvanceStatus.Applied), shipId);
        }

        internal static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction) =>
            new ShipPlacementData
            {
                Id = id,
                TypeId = "TF_BASE_SHIP",
                Length = 2,
                Position = new GridPosition(x, y),
                Direction = direction
            };

        internal static GameSession CreateSession(int width, int height, params ShipPlacementData[] ships)
        {
            var level = new LevelData
            {
                SchemaVersion = 2,
                LevelId = "ShipMovementTest",
                Width = width,
                Height = height,
                BossId = "TF_KRAKEN_01",
                Ships = ships
            };
            return LevelSessionFactory.Create(
                level,
                new[] { new ShipDefinition("TF_BASE_SHIP", 10) },
                new[] { new BossDefinition("TF_KRAKEN_01") });
        }
    }
}
