using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Events;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class EventBusTests
    {
        [Test]
        public void AllSevenContractsDeliverTheirImmutablePayload()
        {
            var ship = new ShipEventContext("session", "S001", "TF_SPEEDBOAT");
            var position = new GridPosition(1, 2);
            RoundTrip(new ShipMoveStartEvent(ship, position, ShipDirection.Right));
            RoundTrip(new ShipMoveCompleteEvent(ship, position, new GridPosition(2, 2), true));
            RoundTrip(new ShipExitBoardEvent(ship, new GridPosition(4, 2), ShipDirection.Right, 7));
            RoundTrip(new ShipEnterFleetEvent(ship, 7));
            RoundTrip(new AttackCreatedEvent(ship, "attack-7", 10));
            RoundTrip(new BossDamagedEvent("session", "TF_KRAKEN_01", "attack-7", 10, 60));
            RoundTrip(new GameWinEvent("session", "TestLevel_001"));
        }

        private static void RoundTrip<T>(T message) where T : struct, ITideboundEvent
        {
            using (var bus = new SessionEventBus())
            {
                var calls = 0; T received = default;
                using (bus.Subscribe<T>(x => { calls++; received = x; })) bus.Publish(message);
                Assert.That(calls, Is.EqualTo(1)); Assert.That(received, Is.EqualTo(message));
            }
        }

        [Test]
        public void TypesAndSessionsAreIsolatedAndSubscriptionsAreDisposable()
        {
            using (var a = LevelConfigLoader.Load(LevelLoadingTests.Fixture()))
            using (var b = LevelConfigLoader.Load(LevelLoadingTests.Fixture()))
            {
                var aCalls = 0; var bCalls = 0; var otherCalls = 0;
                var token = a.Events.Subscribe<GameWinEvent>(_ => aCalls++);
                b.Events.Subscribe<GameWinEvent>(_ => bCalls++);
                a.Events.Subscribe<BossDamagedEvent>(_ => otherCalls++);
                a.Events.Publish(new GameWinEvent(a.SessionId, a.LevelId));
                token.Dispose(); token.Dispose();
                a.Events.Publish(new GameWinEvent(a.SessionId, a.LevelId));
                Assert.That(aCalls, Is.EqualTo(1)); Assert.That(bCalls, Is.Zero); Assert.That(otherCalls, Is.Zero);
            }
        }

        [Test]
        public void RemovingOneDuplicateSubscriptionDoesNotRemoveTheOther()
        {
            using (var bus = new SessionEventBus())
            {
                var calls = 0; Action<GameWinEvent> handler = _ => calls++;
                var first = bus.Subscribe(handler); bus.Subscribe(handler); first.Dispose();
                bus.Publish(new GameWinEvent("s", "l")); Assert.That(calls, Is.EqualTo(1));
            }
        }

        [Test]
        public void SubscriptionChangesApplyToTheNextPublicationAndPreserveOrder()
        {
            using (var bus = new SessionEventBus())
            {
                var calls = new List<int>(); IDisposable second = null; IDisposable added = null;
                bus.Subscribe<GameWinEvent>(_ =>
                {
                    calls.Add(1); second.Dispose();
                    if (added == null) added = bus.Subscribe<GameWinEvent>(x => calls.Add(3));
                });
                second = bus.Subscribe<GameWinEvent>(_ => calls.Add(2));
                bus.Publish(new GameWinEvent("s", "l")); CollectionAssert.AreEqual(new[] { 1, 2 }, calls);
                calls.Clear(); bus.Publish(new GameWinEvent("s", "l")); CollectionAssert.AreEqual(new[] { 1, 3 }, calls);
            }
        }

        [Test]
        public void DisposedSessionRejectsFurtherPublicationAndSubscription()
        {
            var session = LevelConfigLoader.Load(LevelLoadingTests.Fixture());
            var token = session.Events.Subscribe<GameWinEvent>(_ => { });
            session.Dispose(); session.Dispose(); token.Dispose();
            Assert.Throws<ObjectDisposedException>(() => session.Events.Publish(new GameWinEvent("s", "l")));
            Assert.Throws<ObjectDisposedException>(() => session.Events.Subscribe<GameWinEvent>(_ => { }));
        }

        [Test]
        public void SubscriberErrorsAreVisibleAndStopTheCurrentDispatch()
        {
            using (var bus = new SessionEventBus())
            {
                var laterCalls = 0;
                bus.Subscribe<GameWinEvent>(_ => throw new InvalidOperationException("test failure"));
                bus.Subscribe<GameWinEvent>(_ => laterCalls++);
                Assert.Throws<InvalidOperationException>(() => bus.Publish(new GameWinEvent("s", "l")));
                Assert.That(laterCalls, Is.Zero);
            }
        }
    }
}
