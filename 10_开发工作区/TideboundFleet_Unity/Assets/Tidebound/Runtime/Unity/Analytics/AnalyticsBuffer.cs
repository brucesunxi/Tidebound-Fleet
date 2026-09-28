using System;
using System.Collections.Generic;
using System.Linq;

namespace Tidebound.Unity.Analytics
{
    [Serializable] public sealed class AnalyticsEvent
    {
        public string eventId, name, occurredAt, attemptId, tool;
        public int level, durationMs;
    }
    [Serializable] public sealed class PendingAnalyticsEvent
    {
        public string sessionId, appVersion, platform;
        public AnalyticsEvent item;
    }
    [Serializable] public sealed class AnalyticsState
    {
        public bool consent, ageEligible, pendingDeletion, registered, isTest;
        public string installationId, secret, activeAttempt;
        public float activeSeconds;
        public List<PendingAnalyticsEvent> pending = new List<PendingAnalyticsEvent>();
    }
    [Serializable] public sealed class AnalyticsBatch
    {
        public string sessionId, appVersion, platform;
        public AnalyticsEvent[] events;
    }
    public sealed class AnalyticsBuffer
    {
        public const int Capacity = 256;
        public AnalyticsState State { get; }
        public bool CanCollect => State.consent && State.ageEligible && !State.pendingDeletion;
        public AnalyticsBuffer(AnalyticsState state)
        {
            State = state ?? new AnalyticsState();
            if (State.pending == null) State.pending = new List<PendingAnalyticsEvent>();
            if (!CanCollect) State.pending.Clear();
        }
        public void Withdraw()
        {
            State.consent = false; State.ageEligible = false; State.pending.Clear();
            State.pendingDeletion = !string.IsNullOrEmpty(State.installationId);
        }
        public bool Add(PendingAnalyticsEvent value)
        {
            if (!CanCollect || value?.item == null) return false;
            if (State.pending.Count == Capacity) State.pending.RemoveAt(0);
            State.pending.Add(value); return true;
        }
        public AnalyticsBatch Peek(DateTime utcNow)
        {
            if (!CanCollect) return null;
            State.pending.RemoveAll(p => p?.item == null || !DateTime.TryParse(p.item.occurredAt, out var at) || utcNow - at.ToUniversalTime() > TimeSpan.FromDays(7));
            if (State.pending.Count == 0) return null;
            var first = State.pending[0];
            return new AnalyticsBatch { sessionId = first.sessionId, appVersion = first.appVersion, platform = first.platform,
                events = State.pending.TakeWhile(p => p.sessionId == first.sessionId && p.appVersion == first.appVersion && p.platform == first.platform)
                    .Take(32).Select(p => p.item).ToArray() };
        }
        public void Acknowledge(AnalyticsBatch batch)
        {
            var ids = new HashSet<string>(batch.events.Select(e => e.eventId));
            State.pending.RemoveAll(p => ids.Contains(p.item.eventId));
        }
    }
}
