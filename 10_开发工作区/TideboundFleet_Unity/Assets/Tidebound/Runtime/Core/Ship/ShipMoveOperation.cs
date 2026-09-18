using System;
using Tidebound.Board;

namespace Tidebound.Ship
{
    public enum ShipMoveStage
    {
        Traveling = 0,
        BlockedFeedback = 1,
        Completed = 2
    }

    /// <summary>One accepted click and its immutable logical path plus current completion stage.</summary>
    public sealed class ShipMoveOperation
    {
        internal ForwardPathResult PathResult { get; }

        public long OperationId { get; }
        public string ShipId => PathResult.ShipId;
        public GridPosition OriginTail => PathResult.OriginTail;
        public GridPosition TargetTail => PathResult.TargetTail;
        public ShipDirection Direction => PathResult.Direction;
        public int TravelDistance => PathResult.TravelDistance;
        public bool WasBlocked => PathResult.IsBlocked;
        public bool WillExit => PathResult.CanExit;
        public string BlockerShipId => PathResult.BlockerShipId;
        public ShipMoveStage Stage { get; internal set; }

        internal ShipMoveOperation(long operationId, ForwardPathResult pathResult)
        {
            if (operationId <= 0) throw new ArgumentOutOfRangeException(nameof(operationId));
            PathResult = pathResult ?? throw new ArgumentNullException(nameof(pathResult));
            OperationId = operationId;
            Stage = pathResult.IsBlocked && pathResult.TravelDistance == 0
                ? ShipMoveStage.BlockedFeedback
                : ShipMoveStage.Traveling;
        }
    }

    public enum ShipMoveRequestStatus
    {
        Accepted = 0,
        SessionNotPlaying = 1,
        SessionPaused = 2,
        Busy = 3,
        ShipNotFound = 4,
        ShipNotIdle = 5,
        ShipNotOnBoard = 6
    }

    public readonly struct ShipMoveRequestResult
    {
        public ShipMoveRequestStatus Status { get; }
        public ShipMoveOperation Operation { get; }
        public bool IsAccepted => Status == ShipMoveRequestStatus.Accepted;

        internal ShipMoveRequestResult(ShipMoveRequestStatus status, ShipMoveOperation operation = null)
        {
            Status = status;
            Operation = operation;
        }
    }

    public enum ShipMoveAdvanceStatus
    {
        Applied = 0,
        SessionPaused = 1,
        SessionNotPlaying = 2,
        NoActiveOperation = 3,
        OperationMismatch = 4,
        WrongStage = 5
    }
}
