using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Ship;
using Tidebound.Unity.Ship;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class ShipMovementControllerTests
    {
        [Test]
        public void ClickWaitsForTravelAndFeedbackCallbacksBeforeUnlocking()
        {
            using (var session = ShipMovementSystemTests.CreateSession(7, 3,
                ShipMovementSystemTests.Ship("A", 0, 0, ShipDirection.Right),
                ShipMovementSystemTests.Ship("B", 4, 0, ShipDirection.Up)))
            {
                var movement = new ShipMovementSystem(session);
                var a = new FakeView("A");
                var b = new FakeView("B");
                using (var controller = Controller(movement, a, b))
                {
                    controller.StartPlaying();
                    a.Click();
                    Assert.That(a.TravelCalls, Is.EqualTo(1));
                    Assert.That(a.TravelTarget, Is.EqualTo(new Vector3(4f, 0f, 0f)));
                    Assert.That(a.TravelDuration, Is.EqualTo(0.18f).Within(0.0001f));
                    Assert.That(session.GetShip("A").Position, Is.EqualTo(new GridPosition(0, 0)));
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Moving));

                    b.Click();
                    Assert.That(b.TravelCalls, Is.Zero);
                    a.CompleteTravel();
                    Assert.That(session.GetShip("A").Position, Is.EqualTo(new GridPosition(2, 0)));
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.BlockedFeedback));
                    Assert.That(a.FeedbackCalls, Is.EqualTo(1));
                    Assert.That(a.FeedbackOffset, Is.EqualTo(new Vector3(0f, 0f, 0.12f)));

                    a.CompleteFeedback();
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Idle));
                    Assert.That(controller.ActiveOperation, Is.Null);
                }
            }
        }

        [Test]
        public void ImmediateBlockStartsFeedbackWithoutTravelAnimation()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 3,
                ShipMovementSystemTests.Ship("A", 0, 0, ShipDirection.Right),
                ShipMovementSystemTests.Ship("B", 2, 0, ShipDirection.Up)))
            {
                var movement = new ShipMovementSystem(session);
                var a = new FakeView("A");
                using (var controller = Controller(movement, a, new FakeView("B")))
                {
                    controller.StartPlaying();
                    var result = controller.RequestMove("A");
                    Assert.That(result.IsAccepted, Is.True);
                    Assert.That(a.TravelCalls, Is.Zero);
                    Assert.That(a.FeedbackCalls, Is.EqualTo(1));
                    a.CompleteFeedback();
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Idle));
                }
            }
        }

        [Test]
        public void CompletionThatRacesWithPauseIsAppliedOnceAfterResume()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            {
                var movement = new ShipMovementSystem(session);
                var a = new FakeView("A");
                using (var controller = Controller(movement, a))
                {
                    controller.StartPlaying();
                    a.Click();
                    Assert.That(controller.Pause(), Is.True);
                    Assert.That(a.Paused, Is.True);
                    a.CompleteTravel();
                    Assert.That(session.Board.ShipCount, Is.EqualTo(1));
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.Exiting));

                    Assert.That(controller.Resume(), Is.True);
                    Assert.That(a.Paused, Is.False);
                    Assert.That(session.Board.ShipCount, Is.Zero);
                    Assert.That(session.GetShip("A").State, Is.EqualTo(ShipState.InLane));
                    Assert.That(controller.ActiveOperation, Is.Null);
                }
            }
        }

        [Test]
        public void ConstructorRejectsMissingAndDuplicateViewsBeforeBindingClicks()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right),
                ShipMovementSystemTests.Ship("B", 0, 0, ShipDirection.Up)))
            {
                var movement = new ShipMovementSystem(session);
                Assert.Throws<ArgumentException>(() =>
                    new ShipMovementController(movement, new FakeMapper(), new[] { new FakeView("A") }));
                Assert.Throws<ArgumentException>(() =>
                    new ShipMovementController(movement, new FakeMapper(),
                        new[] { new FakeView("A"), new FakeView("A"), new FakeView("B") }));
            }
        }

        [Test]
        public void DisposeUnbindsClicksAndRejectsDirectRequests()
        {
            using (var session = ShipMovementSystemTests.CreateSession(5, 4,
                ShipMovementSystemTests.Ship("A", 0, 2, ShipDirection.Right)))
            {
                var view = new FakeView("A");
                var controller = Controller(new ShipMovementSystem(session), view);
                controller.Dispose();
                view.Click();
                Assert.That(view.TravelCalls, Is.Zero);
                Assert.Throws<ObjectDisposedException>(() => controller.RequestMove("A"));
            }
        }

        [Test]
        public void TimingUsesConfiguredSpeedAndDurationBounds()
        {
            var timing = new ShipMovementTiming(12f, 0.18f, 0.60f, 0.12f, 0.06f);
            Assert.That(timing.CalculateTravelDuration(1), Is.EqualTo(0.18f).Within(0.0001f));
            Assert.That(timing.CalculateTravelDuration(6), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(timing.CalculateTravelDuration(20), Is.EqualTo(0.60f).Within(0.0001f));
            Assert.That(timing.CalculateBlockedLateralDistance(2f), Is.EqualTo(0.12f).Within(0.0001f));
            Assert.Throws<ArgumentOutOfRangeException>(() => timing.CalculateTravelDuration(0));
        }

        private static ShipMovementController Controller(ShipMovementSystem movement, params FakeView[] views) =>
            new ShipMovementController(
                movement,
                new FakeMapper(),
                views,
                new ShipMovementTiming(12f, 0.18f, 0.60f, 0.12f, 0.06f));

        private sealed class FakeMapper : IGridWorldMapper
        {
            public float CellSize => 2f;
            public Vector3 TailToWorld(GridPosition tail) => new Vector3(tail.X * 2f, 0f, tail.Y * 2f);
            public Vector3 DirectionToWorld(ShipDirection direction)
            {
                switch (direction)
                {
                    case ShipDirection.Up: return Vector3.forward;
                    case ShipDirection.Down: return Vector3.back;
                    case ShipDirection.Left: return Vector3.left;
                    case ShipDirection.Right: return Vector3.right;
                    default: throw new ArgumentOutOfRangeException(nameof(direction));
                }
            }
        }

        private sealed class FakeView : IShipMovementView
        {
            private Action<string> click;
            private Action travelCompleted;
            private Action feedbackCompleted;

            public string ShipId { get; }
            public int TravelCalls { get; private set; }
            public int FeedbackCalls { get; private set; }
            public Vector3 TravelTarget { get; private set; }
            public float TravelDuration { get; private set; }
            public Vector3 FeedbackOffset { get; private set; }
            public bool Paused { get; private set; }

            public FakeView(string shipId) { ShipId = shipId; }
            public void BindClickHandler(Action<string> handler) { click = handler; }
            public void Click() { click?.Invoke(ShipId); }
            public void PlayTravel(Vector3 targetTailWorld, float duration, Action completed)
            {
                TravelCalls++;
                TravelTarget = targetTailWorld;
                TravelDuration = duration;
                travelCompleted = completed;
            }
            public void PlayBlockedFeedback(Vector3 lateralOffset, float duration, Action completed)
            {
                FeedbackCalls++;
                FeedbackOffset = lateralOffset;
                feedbackCompleted = completed;
            }
            public void SetPaused(bool paused) { Paused = paused; }
            public void CompleteTravel() { var callback = travelCompleted; travelCompleted = null; callback?.Invoke(); }
            public void CompleteFeedback() { var callback = feedbackCompleted; feedbackCompleted = null; callback?.Invoke(); }
        }
    }
}
