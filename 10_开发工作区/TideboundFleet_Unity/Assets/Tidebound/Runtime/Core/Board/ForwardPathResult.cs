using System;
using System.Collections.Generic;
using Tidebound.Ship;

namespace Tidebound.Board
{
    public enum ForwardPathOutcome
    {
        Blocked = 0,
        Exit = 1
    }

    /// <summary>Immutable answer to one forward-path query. It does not apply a move.</summary>
    public sealed class ForwardPathResult
    {
        public string ShipId { get; }
        public GridPosition OriginTail { get; }
        public ShipDirection Direction { get; }
        public ForwardPathOutcome Outcome { get; }
        public GridPosition TargetTail { get; }
        public IReadOnlyList<GridPosition> ClearCells { get; }
        public int ClearCellCount => ClearCells.Count;
        public int TravelDistance { get; }
        public string BlockerShipId { get; }
        public GridPosition? BlockerCell { get; }
        public bool IsBlocked => Outcome == ForwardPathOutcome.Blocked;
        public bool CanExit => Outcome == ForwardPathOutcome.Exit;

        private ForwardPathResult(string shipId, GridPosition originTail, ShipDirection direction,
            ForwardPathOutcome outcome, GridPosition targetTail, IList<GridPosition> clearCells,
            int travelDistance, string blockerShipId, GridPosition? blockerCell)
        {
            if (string.IsNullOrEmpty(shipId)) throw new ArgumentException("A ship id is required.", nameof(shipId));
            if (clearCells == null) throw new ArgumentNullException(nameof(clearCells));
            if (travelDistance < 0) throw new ArgumentOutOfRangeException(nameof(travelDistance));

            ShipId = shipId;
            OriginTail = originTail;
            Direction = direction;
            Outcome = outcome;
            TargetTail = targetTail;
            var cells = new GridPosition[clearCells.Count];
            clearCells.CopyTo(cells, 0);
            ClearCells = Array.AsReadOnly(cells);
            TravelDistance = travelDistance;
            BlockerShipId = blockerShipId;
            BlockerCell = blockerCell;
        }

        internal static ForwardPathResult Blocked(string shipId, GridPosition originTail,
            ShipDirection direction, GridPosition targetTail, IList<GridPosition> clearCells,
            string blockerShipId, GridPosition blockerCell) =>
            new ForwardPathResult(shipId, originTail, direction, ForwardPathOutcome.Blocked,
                targetTail, clearCells, clearCells.Count, blockerShipId, blockerCell);

        internal static ForwardPathResult Exit(string shipId, GridPosition originTail,
            ShipDirection direction, GridPosition targetTail, IList<GridPosition> clearCells,
            int exitDistance) =>
            new ForwardPathResult(shipId, originTail, direction, ForwardPathOutcome.Exit,
                targetTail, clearCells, exitDistance, null, null);
    }
}
