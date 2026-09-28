using System;
using NUnit.Framework;
using Tidebound.Unity.Analytics;

namespace Tidebound.Tests
{
    public sealed class AnalyticsBufferTests
    {
        private static PendingAnalyticsEvent Item(string session="session",DateTime? at=null) => new PendingAnalyticsEvent {
            sessionId=session,appVersion="1.0",platform="Editor",item=new AnalyticsEvent {
                eventId=Guid.NewGuid().ToString(),name="session_start",occurredAt=(at??DateTime.UtcNow).ToString("o") } };
        [Test] public void UnknownAgeAndDeclinedConsentCannotQueueAndLoadingPurgesUnconsentedEvents()
        {
            foreach(var state in new[]{new AnalyticsState(),new AnalyticsState{consent=true},new AnalyticsState{ageEligible=true}})
            {
                state.pending.Add(Item());var buffer=new AnalyticsBuffer(state);
                Assert.That(buffer.Add(Item()),Is.False);Assert.That(buffer.Peek(DateTime.UtcNow),Is.Null);Assert.That(state.pending,Is.Empty);
            }
        }
        [Test] public void WithdrawalImmediatelyPurgesQueueAndRetainsCredentialOnlyForDeletion()
        {
            var state=new AnalyticsState{consent=true,ageEligible=true,installationId="reference",secret="private"};
            var buffer=new AnalyticsBuffer(state);buffer.Add(Item());buffer.Withdraw();
            Assert.That(buffer.CanCollect,Is.False);Assert.That(state.pending,Is.Empty);Assert.That(state.pendingDeletion,Is.True);
            Assert.That(state.secret,Is.EqualTo("private"));Assert.That(buffer.Add(Item()),Is.False);
        }
        [Test] public void RetryPreservesEventIdentityAndAcknowledgementDoesNotDiscardNewArrivals()
        {
            var buffer=new AnalyticsBuffer(new AnalyticsState{consent=true,ageEligible=true});
            buffer.Add(Item());var first=buffer.Peek(DateTime.UtcNow);var retry=buffer.Peek(DateTime.UtcNow);
            Assert.That(retry.events[0].eventId,Is.EqualTo(first.events[0].eventId));
            var next=Item();buffer.Add(next);buffer.Acknowledge(first);
            Assert.That(buffer.State.pending.Count,Is.EqualTo(1));Assert.That(buffer.State.pending[0].item.eventId,Is.EqualTo(next.item.eventId));
        }
        [Test] public void BatchesBoundMemorySeparateSessionsAndExpireOldEvents()
        {
            var buffer=new AnalyticsBuffer(new AnalyticsState{consent=true,ageEligible=true});
            for(var i=0;i<300;i++)buffer.Add(Item());
            Assert.That(buffer.State.pending.Count,Is.EqualTo(256));Assert.That(buffer.Peek(DateTime.UtcNow).events.Length,Is.EqualTo(32));
            buffer.State.pending.Clear();buffer.Add(Item("old",DateTime.UtcNow.AddDays(-8)));buffer.Add(Item("a"));buffer.Add(Item("b"));
            Assert.That(buffer.Peek(DateTime.UtcNow).events.Length,Is.EqualTo(1));Assert.That(buffer.Peek(DateTime.UtcNow).sessionId,Is.EqualTo("a"));
        }
    }
}
