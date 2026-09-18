using System;
using System.Collections.Generic;
using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>Logical occupancy only. No Transform, Bounds, Collider, sprite, or world-unit input.</summary>
    public static class GridFootprint
    {
        public static GridPosition DirectionStep(ShipDirection direction)
        {
            switch (direction)
            {
                case ShipDirection.Up: return new GridPosition(0, 1);
                case ShipDirection.Down: return new GridPosition(0, -1);
                case ShipDirection.Left: return new GridPosition(-1, 0);
                case ShipDirection.Right: return new GridPosition(1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        public static IEnumerable<GridPosition> Cells(GridPosition tail, ShipDirection direction, int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));
            var step = DirectionStep(direction);
            for (var i = 0; i < length; i++)
                yield return new GridPosition(checked(tail.X + step.X * i), checked(tail.Y + step.Y * i));
        }
    }
}
