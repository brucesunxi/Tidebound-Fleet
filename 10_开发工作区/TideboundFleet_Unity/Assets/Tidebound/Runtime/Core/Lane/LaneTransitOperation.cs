using System;
using Tidebound.Board;
using Tidebound.Events;
using Tidebound.Ship;

namespace Tidebound.Lane
{
    public enum LaneTransitStage
    {
        Traversing = 0,
        WaitingAtEntrance = 1,
        EnteringFleet = 2,
        Completed = 3
    }

    public sealed class LaneTransitOperation
    {
        private readonly double laneDuration;
        private readonly double fleetEntryDuration;

        public ShipEventContext Ship { get; }
        public GridPosition ExitTail { get; }
        public ShipDirection ExitDirection { get; }
        public long ExitSequence { get; }
        public LaneRoute Route { get; }
        public LaneTransitStage Stage { get; internal set; }
        public float LaneProgress { get; internal set; }
        public float FleetEntryProgress { get; internal set; }

        internal double EnqueuedAt { get; }
        internal double LaneReadyAt => EnqueuedAt + laneDuration;
        internal double FleetEntryStartedAt { get; set; }
        internal double FleetEntryCompletedAt => FleetEntryStartedAt + fleetEntryDuration;

        internal LaneTransitOperation(
            ShipExitBoardEvent exitEvent,
            LaneRoute route,
            double enqueuedAt,
            LaneTransitTiming timing)
        {
            Ship = exitEvent.Ship;
            ExitTail = exitEvent.TailPosition;
            ExitDirection = exitEvent.ExitDirection;
            ExitSequence = exitEvent.ExitSequence;
            Route = route;
            EnqueuedAt = enqueuedAt;
            laneDuration = timing.LaneDuration;
            fleetEntryDuration = timing.FleetEntryDuration;
            Stage = LaneTransitStage.Traversing;
        }

        internal void RefreshProgress(double currentTime)
        {
            LaneProgress = Clamp01((currentTime - EnqueuedAt) / laneDuration);
            FleetEntryProgress = Stage == LaneTransitStage.Completed ? 1f :
                Stage == LaneTransitStage.EnteringFleet
                    ? Clamp01((currentTime - FleetEntryStartedAt) / fleetEntryDuration)
                    : 0f;
        }

        private static float Clamp01(double value)
        {
            if (value <= 0d) return 0f;
            if (value >= 1d) return 1f;
            return (float)value;
        }
    }

    public enum LaneEnqueueStatus
    {
        Accepted = 0,
        SessionMismatch = 1,
        InvalidSequence = 2,
        ShipNotFound = 3,
        ShipTypeMismatch = 4,
        ShipNotInLane = 5,
        DuplicateShip = 6,
        DuplicateSequence = 7,
        SequenceAlreadyCompleted = 8,
        Disposed = 9
    }

    public readonly struct LaneEnqueueResult
    {
        public LaneEnqueueStatus Status { get; }
        public LaneTransitOperation Operation { get; }
        public bool IsAccepted => Status == LaneEnqueueStatus.Accepted;

        internal LaneEnqueueResult(LaneEnqueueStatus status, LaneTransitOperation operation = null)
        {
            Status = status;
            Operation = operation;
        }
    }

    public enum LaneAdvanceStatus
    {
        Advanced = 0,
        SessionPaused = 1,
        SessionNotPlaying = 2,
        Disposed = 3
    }
}
