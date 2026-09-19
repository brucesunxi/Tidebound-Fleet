using System;
using System.Collections.Generic;
using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>Immutable logical copy of one ship as registered on a board snapshot.</summary>
    public sealed class BoardShipSnapshot
    {
        public string Id { get; }
        public string TypeId { get; }
        public GridPosition Position { get; }
        public ShipDirection Direction { get; }
        public int Length { get; }
        public IReadOnlyList<GridPosition> OccupiedCells { get; }

        internal BoardShipSnapshot(string id, string typeId, GridPosition position,
            ShipDirection direction, int length, IList<GridPosition> occupiedCells)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A ship id is required.", nameof(id));
            if (string.IsNullOrEmpty(typeId)) throw new ArgumentException("A ship type id is required.", nameof(typeId));
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
            if (occupiedCells == null) throw new ArgumentNullException(nameof(occupiedCells));
            if (occupiedCells.Count != length)
                throw new ArgumentException("Occupied cell count must match logical ship length.", nameof(occupiedCells));

            Id = id;
            TypeId = typeId;
            Position = position;
            Direction = direction;
            Length = length;
            var cells = new GridPosition[occupiedCells.Count];
            occupiedCells.CopyTo(cells, 0);
            OccupiedCells = Array.AsReadOnly(cells);
        }

        internal BoardShipSnapshot WithPlacement(GridPosition position,ShipDirection direction) =>
            new BoardShipSnapshot(Id,TypeId,position,direction,Length,new List<GridPosition>(GridFootprint.Cells(position,direction,Length)));

        internal BoardShipSnapshot WithPosition(GridPosition position)
        {
            var cells = new List<GridPosition>(GridFootprint.Cells(position, Direction, Length));
            return new BoardShipSnapshot(Id, TypeId, position, Direction, Length, cells);
        }
    }
}
