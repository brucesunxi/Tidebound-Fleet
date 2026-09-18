using System;
using System.Collections.Generic;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Ship;

namespace Tidebound.Lane
{
    /// <summary>
    /// Session-scoped lane clock. All ships traverse independently, then enter the fleet strictly by
    /// ExitSequence. It owns no transforms, renderers, combat tokens, or fleet aggregation.
    /// </summary>
    public sealed class TransitSystem : IDisposable
    {
        private const double Epsilon = 0.0000001d;

        private readonly GameSession session;
        private readonly LaneTransitTiming timing;
        private readonly IDisposable exitSubscription;
        private readonly Dictionary<string, LaneTransitOperation> activeByShip =
            new Dictionary<string, LaneTransitOperation>(StringComparer.Ordinal);
        private readonly Dictionary<long, LaneTransitOperation> activeBySequence =
            new Dictionary<long, LaneTransitOperation>();
        private readonly HashSet<string> seenShipIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<long> seenSequences = new HashSet<long>();

        private double elapsedTime;
        private double nextEntranceReleaseAt;
        private long nextEntranceSequence = 1;
        private LaneTransitOperation activeEntrance;
        private bool disposed;

        public string SessionId => session.SessionId;
        public double ElapsedTime => elapsedTime;
        public long NextEntranceSequence => nextEntranceSequence;
        public int ActiveCount => activeByShip.Count;
        public int WaitingCount
        {
            get
            {
                var count = 0;
                foreach (var operation in activeByShip.Values)
                    if (operation.Stage == LaneTransitStage.WaitingAtEntrance) count++;
                return count;
            }
        }
        public bool IsEmpty => activeByShip.Count == 0;
        public IReadOnlyList<ShipRuntimeData> Ships => session.Ships;
        public IEventBus Events => session.Events;

        public TransitSystem(GameSession session, LaneTransitTiming timing = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.timing = timing ?? new LaneTransitTiming();
            exitSubscription = session.Events.Subscribe<ShipExitBoardEvent>(OnShipExitBoard);
        }

        public LaneEnqueueResult Enqueue(ShipExitBoardEvent exitEvent)
        {
            if (disposed) return new LaneEnqueueResult(LaneEnqueueStatus.Disposed);
            if (!string.Equals(exitEvent.Ship.SessionId, session.SessionId, StringComparison.Ordinal))
                return new LaneEnqueueResult(LaneEnqueueStatus.SessionMismatch);
            if (exitEvent.ExitSequence <= 0)
                return new LaneEnqueueResult(LaneEnqueueStatus.InvalidSequence);
            if (!session.TryGetShip(exitEvent.Ship.ShipId, out var ship))
                return new LaneEnqueueResult(LaneEnqueueStatus.ShipNotFound);
            if (!string.Equals(ship.TypeId, exitEvent.Ship.TypeId, StringComparison.Ordinal))
                return new LaneEnqueueResult(LaneEnqueueStatus.ShipTypeMismatch);
            if (ship.State != ShipState.InLane)
                return new LaneEnqueueResult(LaneEnqueueStatus.ShipNotInLane);
            if (exitEvent.ExitSequence < nextEntranceSequence)
                return new LaneEnqueueResult(LaneEnqueueStatus.SequenceAlreadyCompleted);
            if (seenShipIds.Contains(ship.Id))
                return new LaneEnqueueResult(LaneEnqueueStatus.DuplicateShip);
            if (seenSequences.Contains(exitEvent.ExitSequence))
                return new LaneEnqueueResult(LaneEnqueueStatus.DuplicateSequence);

            var route = LaneRouteResolver.Resolve(exitEvent.ExitDirection, exitEvent.TailPosition, session.Width);
            var operation = new LaneTransitOperation(exitEvent, route, elapsedTime, timing);
            activeByShip.Add(ship.Id, operation);
            activeBySequence.Add(operation.ExitSequence, operation);
            seenShipIds.Add(ship.Id);
            seenSequences.Add(operation.ExitSequence);
            return new LaneEnqueueResult(LaneEnqueueStatus.Accepted, operation);
        }

        public LaneAdvanceStatus Advance(double deltaTime)
        {
            if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (disposed) return LaneAdvanceStatus.Disposed;
            if (session.State == GameState.Paused) return LaneAdvanceStatus.SessionPaused;
            if (session.State != GameState.Playing) return LaneAdvanceStatus.SessionNotPlaying;

            var targetTime = checked(elapsedTime + deltaTime);
            while (TryGetNextEventTime(targetTime, out var eventTime))
            {
                elapsedTime = eventTime;
                MarkArrivals();

                if (activeEntrance != null && activeEntrance.FleetEntryCompletedAt <= elapsedTime + Epsilon)
                    CompleteFleetEntry(activeEntrance);

                TryStartEntrance();
            }

            elapsedTime = targetTime;
            MarkArrivals();
            TryStartEntrance();
            RefreshProgress();
            return LaneAdvanceStatus.Advanced;
        }

        public bool TryGetTransit(string shipId, out LaneTransitOperation operation)
        {
            if (shipId == null)
            {
                operation = null;
                return false;
            }
            return activeByShip.TryGetValue(shipId, out operation);
        }

        public IReadOnlyList<LaneTransitOperation> GetActiveTransits()
        {
            var items = new List<LaneTransitOperation>(activeByShip.Values);
            items.Sort((left, right) => left.ExitSequence.CompareTo(right.ExitSequence));
            return items.AsReadOnly();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            exitSubscription.Dispose();
            activeByShip.Clear();
            activeBySequence.Clear();
            activeEntrance = null;
        }

        private void OnShipExitBoard(ShipExitBoardEvent exitEvent) => Enqueue(exitEvent);

        private bool TryGetNextEventTime(double targetTime, out double eventTime)
        {
            eventTime = double.PositiveInfinity;

            foreach (var operation in activeByShip.Values)
                if (operation.Stage == LaneTransitStage.Traversing)
                    eventTime = Math.Min(eventTime, operation.LaneReadyAt);

            if (activeEntrance != null)
                eventTime = Math.Min(eventTime, activeEntrance.FleetEntryCompletedAt);
            else if (activeBySequence.TryGetValue(nextEntranceSequence, out var next))
                eventTime = Math.Min(eventTime, Math.Max(next.LaneReadyAt, nextEntranceReleaseAt));

            if (double.IsPositiveInfinity(eventTime) || eventTime > targetTime + Epsilon)
                return false;
            if (eventTime < elapsedTime) eventTime = elapsedTime;
            return true;
        }

        private void MarkArrivals()
        {
            foreach (var operation in activeByShip.Values)
                if (operation.Stage == LaneTransitStage.Traversing &&
                    operation.LaneReadyAt <= elapsedTime + Epsilon)
                    operation.Stage = LaneTransitStage.WaitingAtEntrance;
        }

        private void TryStartEntrance()
        {
            if (activeEntrance != null || elapsedTime + Epsilon < nextEntranceReleaseAt) return;
            if (!activeBySequence.TryGetValue(nextEntranceSequence, out var next)) return;
            if (next.Stage != LaneTransitStage.WaitingAtEntrance) return;

            next.Stage = LaneTransitStage.EnteringFleet;
            next.FleetEntryStartedAt = elapsedTime;
            activeEntrance = next;
            nextEntranceReleaseAt = elapsedTime + timing.EntranceInterval;
        }

        private void CompleteFleetEntry(LaneTransitOperation operation)
        {
            operation.Stage = LaneTransitStage.Completed;
            operation.LaneProgress = 1f;
            operation.FleetEntryProgress = 1f;
            session.GetShip(operation.Ship.ShipId).State = ShipState.InFleet;
            activeByShip.Remove(operation.Ship.ShipId);
            activeBySequence.Remove(operation.ExitSequence);
            activeEntrance = null;
            nextEntranceSequence = checked(operation.ExitSequence + 1);

            session.Events.Publish(new ShipEnterFleetEvent(operation.Ship, operation.ExitSequence));
        }

        private void RefreshProgress()
        {
            foreach (var operation in activeByShip.Values) operation.RefreshProgress(elapsedTime);
        }
    }
}
