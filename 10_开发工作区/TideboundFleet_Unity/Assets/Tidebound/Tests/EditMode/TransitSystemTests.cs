using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class TransitSystemTests
    {
        [Test]
        public void RouteResolverMapsFourEdgesAndBottomTieUsesRight()
        {
            Assert.That(LaneRouteResolver.Resolve(ShipDirection.Up, new GridPosition(1, 6), 4),
                Is.EqualTo(LaneRoute.Top));
            Assert.That(LaneRouteResolver.Resolve(ShipDirection.Left, new GridPosition(-2, 2), 4),
                Is.EqualTo(LaneRoute.Left));
            Assert.That(LaneRouteResolver.Resolve(ShipDirection.Right, new GridPosition(4, 2), 4),
                Is.EqualTo(LaneRoute.Right));
            Assert.That(LaneRouteResolver.Resolve(ShipDirection.Down, new GridPosition(0, -2), 4),
                Is.EqualTo(LaneRoute.BottomViaLeft));
            Assert.That(LaneRouteResolver.Resolve(ShipDirection.Down, new GridPosition(3, -2), 4),
                Is.EqualTo(LaneRoute.BottomViaRight));
            Assert.That(LaneRouteResolver.Resolve(ShipDirection.Down, new GridPosition(1, -2), 3),
                Is.EqualTo(LaneRoute.BottomViaRight));
        }

        [Test]
        public void ExitEventAutomaticallyTraversesAndEntersFleetAtDefinedTimes()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            using (var transit = new TransitSystem(session))
            {
                var movement = new ShipMovementSystem(session);
                var entries = new List<ShipEnterFleetEvent>();
                session.Events.Subscribe<ShipEnterFleetEvent>(entries.Add);
                movement.StartPlaying();
                Exit(movement, "A");

                Assert.That(transit.ActiveCount, Is.EqualTo(1));
                Assert.That(transit.TryGetTransit("A", out var operation), Is.True);
                Assert.That(operation.Route, Is.EqualTo(LaneRoute.Right));
                Assert.That(operation.Stage, Is.EqualTo(LaneTransitStage.Traversing));

                transit.Advance(1.19d);
                Assert.That(operation.LaneProgress, Is.EqualTo(1.19f / 1.2f).Within(0.0001f));
                Assert.That(operation.Stage, Is.EqualTo(LaneTransitStage.Traversing));
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InLane));

                transit.Advance(0.01d);
                Assert.That(operation.Stage, Is.EqualTo(LaneTransitStage.EnteringFleet));
                Assert.That(operation.LaneProgress, Is.EqualTo(1f));
                transit.Advance(0.15d);

                Assert.That(transit.IsEmpty, Is.True);
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InFleet));
                Assert.That(entries.Count, Is.EqualTo(1));
                Assert.That(entries[0].ExitSequence, Is.EqualTo(1));
            }
        }

        [Test]
        public void LaterSequenceWaitsEvenWhenItReachedEntranceFirst()
        {
            using (var session = CreateManualSession("A", "B"))
            using (var transit = new TransitSystem(session))
            {
                session.State = GameState.Playing;
                session.GetShip("A").State = ShipState.InLane;
                session.GetShip("B").State = ShipState.InLane;
                var order = new List<string>();
                session.Events.Subscribe<ShipEnterFleetEvent>(x => order.Add(x.Ship.ShipId));

                Assert.That(transit.Enqueue(ExitEvent(session, "B", 2, ShipDirection.Up)).IsAccepted, Is.True);
                transit.Advance(1.20d);
                Assert.That(transit.TryGetTransit("B", out var b), Is.True);
                Assert.That(b.Stage, Is.EqualTo(LaneTransitStage.WaitingAtEntrance));

                Assert.That(transit.Enqueue(ExitEvent(session, "A", 1, ShipDirection.Down)).IsAccepted, Is.True);
                transit.Advance(1.20d);
                Assert.That(transit.TryGetTransit("A", out var a), Is.True);
                Assert.That(a.Stage, Is.EqualTo(LaneTransitStage.EnteringFleet));
                Assert.That(b.Stage, Is.EqualTo(LaneTransitStage.WaitingAtEntrance));

                transit.Advance(0.30d);
                CollectionAssert.AreEqual(new[] { "A", "B" }, order);
                Assert.That(transit.IsEmpty, Is.True);
            }
        }

        [Test]
        public void LargeAdvancePreservesFifoAndEntranceSpacing()
        {
            using (var session = CreateManualSession("A", "B", "C"))
            using (var transit = new TransitSystem(session))
            {
                session.State = GameState.Playing;
                foreach (var ship in session.Ships) ship.State = ShipState.InLane;
                var order = new List<string>();
                var entryTimes = new List<double>();
                session.Events.Subscribe<ShipEnterFleetEvent>(x =>
                {
                    order.Add(x.Ship.ShipId);
                    entryTimes.Add(transit.ElapsedTime);
                });

                transit.Enqueue(ExitEvent(session, "A", 1, ShipDirection.Left));
                transit.Enqueue(ExitEvent(session, "B", 2, ShipDirection.Up));
                transit.Enqueue(ExitEvent(session, "C", 3, ShipDirection.Right));
                transit.Advance(10d);

                CollectionAssert.AreEqual(new[] { "A", "B", "C" }, order);
                Assert.That(entryTimes[0], Is.EqualTo(1.35d).Within(0.000001d));
                Assert.That(entryTimes[1] - entryTimes[0], Is.EqualTo(0.15d).Within(0.000001d));
                Assert.That(entryTimes[2] - entryTimes[1], Is.EqualTo(0.15d).Within(0.000001d));
                Assert.That(transit.NextEntranceSequence, Is.EqualTo(4));
            }
        }

        [Test]
        public void PauseFreezesPathAndEntranceProgress()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            using (var transit = new TransitSystem(session))
            {
                var movement = new ShipMovementSystem(session);
                movement.StartPlaying();
                Exit(movement, "A");
                transit.Advance(0.50d);
                var before = transit.ElapsedTime;
                var beforeProgress = transit.GetActiveTransits()[0].LaneProgress;

                movement.Pause();
                Assert.That(transit.Advance(5d), Is.EqualTo(LaneAdvanceStatus.SessionPaused));
                Assert.That(transit.ElapsedTime, Is.EqualTo(before));
                Assert.That(transit.GetActiveTransits()[0].LaneProgress, Is.EqualTo(beforeProgress));

                movement.Resume();
                transit.Advance(0.85d);
                Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InFleet));
            }
        }

        [Test]
        public void InvalidDuplicateAndCrossSessionEventsCannotCreateExtraEntries()
        {
            using (var session = CreateManualSession("A", "B"))
            using (var other = CreateManualSession("X"))
            using (var transit = new TransitSystem(session))
            {
                session.State = GameState.Playing;
                session.GetShip("A").State = ShipState.InLane;
                session.GetShip("B").State = ShipState.InLane;
                other.GetShip("X").State = ShipState.InLane;

                var first = ExitEvent(session, "A", 1, ShipDirection.Up);
                Assert.That(transit.Enqueue(first).Status, Is.EqualTo(LaneEnqueueStatus.Accepted));
                Assert.That(transit.Enqueue(first).Status, Is.EqualTo(LaneEnqueueStatus.DuplicateShip));
                Assert.That(transit.Enqueue(ExitEvent(session, "B", 1, ShipDirection.Up)).Status,
                    Is.EqualTo(LaneEnqueueStatus.DuplicateSequence));
                Assert.That(transit.Enqueue(ExitEvent(other, "X", 2, ShipDirection.Up)).Status,
                    Is.EqualTo(LaneEnqueueStatus.SessionMismatch));
                Assert.That(transit.ActiveCount, Is.EqualTo(1));

                transit.Advance(2d);
                Assert.That(transit.Enqueue(ExitEvent(session, "B", 1, ShipDirection.Up)).Status,
                    Is.EqualTo(LaneEnqueueStatus.SequenceAlreadyCompleted));
            }
        }

        [Test]
        public void DisposeUnsubscribesFromFutureExitEvents()
        {
            using (var session = CreateManualSession("A"))
            {
                session.State = GameState.Playing;
                session.GetShip("A").State = ShipState.InLane;
                var transit = new TransitSystem(session);
                transit.Dispose();
                session.Events.Publish(ExitEvent(session, "A", 1, ShipDirection.Up));
                Assert.That(transit.ActiveCount, Is.Zero);
                Assert.That(transit.Advance(1d), Is.EqualTo(LaneAdvanceStatus.Disposed));
            }
        }

        [Test]
        public void TestLevelReferenceSequenceMovesAllSevenShipsIntoFleetOnce()
        {
            using (var session = Config.LevelConfigLoader.Load(LevelLoadingTests.Fixture()))
            using (var transit = new TransitSystem(session))
            {
                var movement = new ShipMovementSystem(session);
                var entries = new List<ShipEnterFleetEvent>();
                session.Events.Subscribe<ShipEnterFleetEvent>(entries.Add);
                movement.StartPlaying();
                var order = new[] { "S005", "S002", "S001", "S004", "S006", "S003", "S007" };
                foreach (var shipId in order) Exit(movement, shipId);

                Assert.That(transit.ActiveCount, Is.EqualTo(7));
                transit.Advance(5d);

                Assert.That(transit.IsEmpty, Is.True);
                CollectionAssert.AreEqual(order, entries.ConvertAll(x => x.Ship.ShipId));
                CollectionAssert.AreEqual(new long[] { 1, 2, 3, 4, 5, 6, 7 },
                    entries.ConvertAll(x => x.ExitSequence));
                foreach (var ship in session.Ships) Assert.That(ship.State, Is.EqualTo(ShipState.InFleet));
            }
        }

        private static void Exit(ShipMovementSystem movement, string shipId)
        {
            var request = movement.TryBeginMove(shipId);
            Assert.That(request.IsAccepted, Is.True, shipId);
            Assert.That(request.Operation.WillExit, Is.True, shipId);
            Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                Is.EqualTo(ShipMoveAdvanceStatus.Applied), shipId);
        }

        private static GameSession CreateManualSession(params string[] ids)
        {
            var ships = new ShipPlacementData[ids.Length];
            for (var index = 0; index < ids.Length; index++)
                ships[index] = ShipMovementSystemTests.Ship(ids[index], 0, index, ShipDirection.Right);
            return ShipMovementSystemTests.CreateSession(8, Math.Max(3, ids.Length + 1), ships);
        }

        internal static ShipExitBoardEvent ExitEvent(
            GameSession session, string shipId, long sequence, ShipDirection direction)
        {
            var ship = session.GetShip(shipId);
            return new ShipExitBoardEvent(
                new ShipEventContext(session.SessionId, ship.Id, ship.TypeId, ship.SkinId),
                ship.Position,
                direction,
                sequence);
        }
    }
}
