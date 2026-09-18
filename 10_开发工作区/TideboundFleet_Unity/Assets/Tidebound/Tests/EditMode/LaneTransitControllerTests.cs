using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.Core;
using Tidebound.Lane;
using Tidebound.Ship;
using Tidebound.Unity.Lane;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class LaneTransitControllerTests
    {
        [Test]
        public void ControllerMapsTransitProgressAndCompletesTheMatchingView()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            using (var transit = new TransitSystem(session))
            {
                var movement = new ShipMovementSystem(session);
                var view = new FakeLaneView("A", Vector3.zero);
                using (var controller = new LaneTransitController(
                    transit, new FakePathProvider(), new[] { view }))
                {
                    movement.StartPlaying();
                    Exit(movement, "A");
                    controller.Advance(0.60d);

                    Assert.That(view.ApplyCount, Is.EqualTo(1));
                    Assert.That(view.Position.x, Is.EqualTo(5f).Within(0.001f));
                    Assert.That(view.Scale, Is.EqualTo(0.45f).Within(0.001f));
                    Assert.That(view.Completed, Is.False);

                    controller.Advance(0.60d);
                    Assert.That(view.Position, Is.EqualTo(new Vector3(10f, 0f, 0f)));
                    Assert.That(view.Completed, Is.False);
                    controller.Advance(0.075d);
                    Assert.That(view.Position, Is.EqualTo(new Vector3(10f, 0f, 2.5f)));
                    controller.Advance(0.075d);
                    Assert.That(view.Completed, Is.True);
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InFleet));
                }
            }
        }

        [Test]
        public void PausedControllerLeavesViewAtSamePoseUntilResume()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            using (var transit = new TransitSystem(session))
            {
                var movement = new ShipMovementSystem(session);
                var view = new FakeLaneView("A", Vector3.zero);
                using (var controller = new LaneTransitController(transit, new FakePathProvider(), new[] { view }))
                {
                    movement.StartPlaying();
                    Exit(movement, "A");
                    controller.Advance(0.25d);
                    var pose = view.Position;
                    movement.Pause();
                    Assert.That(controller.Advance(10d), Is.EqualTo(LaneAdvanceStatus.SessionPaused));
                    Assert.That(view.Position, Is.EqualTo(pose));
                    movement.Resume();
                    controller.Advance(0.25d);
                    Assert.That(view.Position, Is.Not.EqualTo(pose));
                }
            }
        }

        [Test]
        public void ConstructorRejectsMissingDuplicateAndInvalidViews()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right),
                ShipMovementSystemTests.Ship("B", 0, 0, ShipDirection.Up)))
            using (var transit = new TransitSystem(session))
            {
                Assert.Throws<ArgumentException>(() => new LaneTransitController(
                    transit, new FakePathProvider(), new[] { new FakeLaneView("A", Vector3.zero) }));
                Assert.Throws<ArgumentException>(() => new LaneTransitController(
                    transit, new FakePathProvider(), new[]
                    {
                        new FakeLaneView("A", Vector3.zero),
                        new FakeLaneView("A", Vector3.zero),
                        new FakeLaneView("B", Vector3.zero)
                    }));
            }
        }

        [Test]
        public void WorldPathKeepsEndpointsAndProvidesForwardTangent()
        {
            var path = new LaneWorldPath(
                Vector3.zero,
                new Vector3(0f, 0f, 5f),
                new Vector3(5f, 0f, 5f));
            Assert.That(path.Sample(0f), Is.EqualTo(Vector3.zero));
            Assert.That(path.Sample(1f), Is.EqualTo(new Vector3(5f, 0f, 5f)));
            Assert.That(path.Tangent(0.5f).sqrMagnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void DisposeStopsPresentationCallsAndRejectsAdvance()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            using (var transit = new TransitSystem(session))
            {
                var view = new FakeLaneView("A", Vector3.zero);
                var controller = new LaneTransitController(transit, new FakePathProvider(), new[] { view });
                controller.Dispose();
                Assert.Throws<ObjectDisposedException>(() => controller.Advance(0.1d));
            }
        }

        private static void Exit(ShipMovementSystem movement, string shipId)
        {
            var request = movement.TryBeginMove(shipId);
            Assert.That(request.IsAccepted, Is.True);
            Assert.That(movement.CompleteTravel(request.Operation.OperationId),
                Is.EqualTo(ShipMoveAdvanceStatus.Applied));
        }

        private sealed class FakePathProvider : ILanePathProvider
        {
            public Vector3 FleetIngressPosition => new Vector3(10f, 0f, 5f);
            public LaneWorldPath CreatePath(LaneRoute route, Vector3 startPosition) =>
                new LaneWorldPath(startPosition, new Vector3(10f, 0f, 0f));
        }

        private sealed class FakeLaneView : ILaneTransitView
        {
            public string ShipId { get; }
            public Vector3 CurrentPosition { get; private set; }
            public Vector3 Position { get; private set; }
            public Vector3 Forward { get; private set; }
            public float Scale { get; private set; }
            public int ApplyCount { get; private set; }
            public bool Completed { get; private set; }

            public FakeLaneView(string shipId, Vector3 position)
            {
                ShipId = shipId;
                CurrentPosition = position;
                Position = position;
            }

            public void ApplyLanePose(Vector3 position, Vector3 forward, float scale)
            {
                Position = position;
                CurrentPosition = position;
                Forward = forward;
                Scale = scale;
                ApplyCount++;
            }

            public void CompleteFleetEntry() => Completed = true;
        }
    }
}
