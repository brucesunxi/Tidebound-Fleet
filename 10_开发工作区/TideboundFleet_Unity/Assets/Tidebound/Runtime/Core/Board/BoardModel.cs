using System.Collections.Generic;
using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>Read-only initial occupancy snapshot. A transactional movement model is deferred to Phase 4.</summary>
    public sealed class BoardModel
    {
        private readonly Dictionary<GridPosition, string> occupied = new Dictionary<GridPosition, string>();
        public int Width { get; }
        public int Height { get; }
        public int OccupiedCellCount => occupied.Count;
        internal BoardModel(int width, int height, IEnumerable<ShipRuntimeData> ships)
        {
            Width = width; Height = height;
            foreach (var ship in ships)
                foreach (var cell in GridFootprint.Cells(ship.Position, ship.Direction, ship.Length)) occupied.Add(cell, ship.Id);
        }
        public string GetShipId(GridPosition cell) => occupied.TryGetValue(cell, out var id) ? id : null;
    }
}
