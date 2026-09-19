using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>
    /// Immutable logical board snapshot. It owns copied ship placement data and never reads presentation state.
    /// Occupancy mutations and movement transactions are deferred to Phase 3.
    /// </summary>
    public sealed class BoardModel
    {
        private readonly Dictionary<GridPosition, string> occupied = new Dictionary<GridPosition, string>();
        private readonly Dictionary<string, BoardShipSnapshot> shipsById =
            new Dictionary<string, BoardShipSnapshot>(StringComparer.Ordinal);
        private readonly IReadOnlyList<BoardShipSnapshot> ships;

        public int Width { get; }
        public int Height { get; }
        public int ShipCount => ships.Count;
        public int OccupiedCellCount => occupied.Count;
        public IReadOnlyList<BoardShipSnapshot> Ships => ships;

        internal BoardModel(int width, int height, IEnumerable<ShipRuntimeData> ships)
            : this(width, height, CopyRuntimeShips(ships))
        {
        }

        private BoardModel(int width, int height, IEnumerable<BoardShipSnapshot> ships)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (ships == null) throw new ArgumentNullException(nameof(ships));

            Width = width;
            Height = height;
            var snapshots = new List<BoardShipSnapshot>();
            foreach (var ship in ships)
            {
                if (ship == null) throw new ArgumentException("A board ship cannot be null.", nameof(ships));
                if (shipsById.ContainsKey(ship.Id))
                    throw new ArgumentException($"Duplicate board ship id '{ship.Id}'.", nameof(ships));
                shipsById.Add(ship.Id, ship);

                foreach (var cell in ship.OccupiedCells)
                {
                    if (!IsInside(cell))
                        throw new ArgumentException($"Ship '{ship.Id}' occupies out-of-board cell {cell}.", nameof(ships));
                    if (occupied.ContainsKey(cell))
                        throw new ArgumentException($"Ship '{ship.Id}' overlaps cell {cell}.", nameof(ships));
                    occupied.Add(cell, ship.Id);
                }
                snapshots.Add(ship);
            }
            this.ships = snapshots.AsReadOnly();
        }

        public bool IsInside(GridPosition cell) =>
            cell.X >= 0 && cell.X < Width && cell.Y >= 0 && cell.Y < Height;

        public bool IsOccupied(GridPosition cell) => occupied.ContainsKey(cell);

        public string GetShipId(GridPosition cell) => occupied.TryGetValue(cell, out var id) ? id : null;

        public bool TryGetShip(string shipId, out BoardShipSnapshot ship)
        {
            if (shipId == null)
            {
                ship = null;
                return false;
            }
            return shipsById.TryGetValue(shipId, out ship);
        }

        public BoardShipSnapshot GetShip(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) throw new ArgumentException("A ship id is required.", nameof(shipId));
            if (!shipsById.TryGetValue(shipId, out var ship))
                throw new KeyNotFoundException($"Ship '{shipId}' does not exist on this board.");
            return ship;
        }

        /// <summary>
        /// Scans from the cell directly in front of the ship's head. The result is a pure calculation:
        /// it never changes occupancy, runtime position, ship state, or events.
        /// </summary>
        public ForwardPathResult QueryForwardPath(string shipId)
        {
            var ship = GetShip(shipId);
            var step = GridFootprint.DirectionStep(ship.Direction);
            var head = ship.OccupiedCells[ship.OccupiedCells.Count - 1];
            var cursor = Offset(head, step, 1);
            var clearCells = new List<GridPosition>();

            while (IsInside(cursor))
            {
                if (occupied.TryGetValue(cursor, out var blockerId))
                {
                    var target = Offset(ship.Position, step, clearCells.Count);
                    return ForwardPathResult.Blocked(
                        ship.Id, ship.Position, ship.Direction, target, clearCells, blockerId, cursor);
                }

                clearCells.Add(cursor);
                cursor = Offset(cursor, step, 1);
            }

            // Clear cells only cover the water between the head and the edge. The tail must then travel
            // another full ship length before it is completely outside the board.
            var exitDistance = checked(clearCells.Count + ship.Length);
            var exitTail = Offset(ship.Position, step, exitDistance);
            return ForwardPathResult.Exit(
                ship.Id, ship.Position, ship.Direction, exitTail, clearCells, exitDistance);
        }

        /// <summary>
        /// Atomically applies a current query result to a new immutable board. The source board remains
        /// unchanged. Exit removes the ship; a blocked result relocates it to the last legal tail cell.
        /// RuntimeData, states, animations, and events are deliberately outside this transaction.
        /// </summary>
        public BoardModel ApplyPathResult(ForwardPathResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var ship = GetShip(result.ShipId);
            var expected = QueryForwardPath(result.ShipId);
            if (!Matches(expected, result))
                throw new InvalidOperationException("The path result is stale or does not belong to this board state.");

            if (result.IsBlocked && result.TravelDistance == 0) return this;

            var next = new List<BoardShipSnapshot>(ships.Count);
            foreach (var item in ships)
            {
                if (item.Id != ship.Id) next.Add(item);
                else if (result.IsBlocked) next.Add(item.WithPosition(result.TargetTail));
            }
            return new BoardModel(Width, Height, next);
        }

        internal BoardModel WithoutShip(string id)
        {
            GetShip(id);
            return new BoardModel(Width,Height,ships.Where(s=>s.Id!=id));
        }

        internal BoardModel WithPlacements(IEnumerable<BoardShipSnapshot> replacements)
        {
            var next=replacements.ToArray();
            if(next.Length!=ShipCount || next.Any(s=>!shipsById.TryGetValue(s.Id,out var old) || old.TypeId!=s.TypeId || old.Length!=s.Length))
                throw new ArgumentException("Rearranging must preserve every remaining ship identity and length.");
            return new BoardModel(Width,Height,next);
        }

        private static IEnumerable<BoardShipSnapshot> CopyRuntimeShips(IEnumerable<ShipRuntimeData> runtimeShips)
        {
            if (runtimeShips == null) throw new ArgumentNullException(nameof(runtimeShips));
            foreach (var ship in runtimeShips)
            {
                if (ship == null) throw new ArgumentException("A board ship cannot be null.", nameof(runtimeShips));
                var cells = new List<GridPosition>(GridFootprint.Cells(ship.Position, ship.Direction, ship.Length));
                yield return new BoardShipSnapshot(
                    ship.Id, ship.TypeId, ship.Position, ship.Direction, ship.Length, cells);
            }
        }

        private static bool Matches(ForwardPathResult expected, ForwardPathResult actual) =>
            expected.ShipId == actual.ShipId &&
            expected.OriginTail.Equals(actual.OriginTail) &&
            expected.Direction == actual.Direction &&
            expected.Outcome == actual.Outcome &&
            expected.TargetTail.Equals(actual.TargetTail) &&
            expected.TravelDistance == actual.TravelDistance &&
            expected.BlockerShipId == actual.BlockerShipId &&
            Nullable.Equals(expected.BlockerCell, actual.BlockerCell);

        private static GridPosition Offset(GridPosition origin, GridPosition step, int distance) =>
            new GridPosition(
                checked(origin.X + step.X * distance),
                checked(origin.Y + step.Y * distance));
    }
}
