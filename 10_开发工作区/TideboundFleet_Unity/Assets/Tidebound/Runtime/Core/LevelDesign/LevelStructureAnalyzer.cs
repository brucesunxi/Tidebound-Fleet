using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tidebound.Board;
using Tidebound.Ship;

namespace Tidebound.LevelDesign
{
    public sealed class LevelAnalysisReport
    {
        public int ShipCount { get; }
        public int OccupiedCellCount { get; }
        public double OccupancyRatio { get; }
        public IReadOnlyDictionary<ShipDirection, int> DirectionCounts { get; }
        public double DirectionEntropy { get; }
        public double DirectionClustering { get; }
        public int AdjacentShipPairCount { get; }
        public double MaximumDirectionShare { get; }
        public int LongShipCount { get; }
        public BoardDependencyGraph Dependencies { get; }

        internal LevelAnalysisReport(BoardModel board, IDictionary<ShipDirection, int> directionCounts,
            double entropy, double clustering, int adjacentPairs, double maximumDirectionShare,
            int longShipCount, BoardDependencyGraph dependencies)
        {
            ShipCount = board.ShipCount;
            OccupiedCellCount = board.OccupiedCellCount;
            OccupancyRatio = board.Width * board.Height == 0 ? 0d :
                (double)board.OccupiedCellCount / (board.Width * board.Height);
            DirectionCounts = new ReadOnlyDictionary<ShipDirection, int>(
                new Dictionary<ShipDirection, int>(directionCounts));
            DirectionEntropy = entropy;
            DirectionClustering = clustering;
            AdjacentShipPairCount = adjacentPairs;
            MaximumDirectionShare = maximumDirectionShare;
            LongShipCount = longShipCount;
            Dependencies = dependencies;
        }
    }

    public static class LevelStructureAnalyzer
    {
        public static LevelAnalysisReport Analyze(BoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var counts = new Dictionary<ShipDirection, int>
            {
                { ShipDirection.Up, 0 }, { ShipDirection.Down, 0 },
                { ShipDirection.Left, 0 }, { ShipDirection.Right, 0 }
            };
            var longShips = 0;
            foreach (var ship in board.Ships)
            {
                counts[ship.Direction]++;
                if (ship.Length == 3) longShips++;
            }

            var entropy = 0d;
            var maxShare = 0d;
            if (board.ShipCount > 0)
            {
                foreach (var count in counts.Values)
                {
                    if (count == 0) continue;
                    var share = (double)count / board.ShipCount;
                    entropy -= share * (Math.Log(share) / Math.Log(2d));
                    maxShare = Math.Max(maxShare, share);
                }
                entropy /= 2d;
            }

            var pairs = new HashSet<ShipPair>();
            foreach (var ship in board.Ships)
            foreach (var cell in ship.OccupiedCells)
            {
                AddPair(board, ship.Id, new GridPosition(cell.X + 1, cell.Y), pairs);
                AddPair(board, ship.Id, new GridPosition(cell.X, cell.Y + 1), pairs);
            }
            var same = 0;
            foreach (var pair in pairs)
                if (board.GetShip(pair.First).Direction == board.GetShip(pair.Second).Direction) same++;
            var clustering = pairs.Count == 0 ? 0d : (double)same / pairs.Count;

            return new LevelAnalysisReport(board, counts, entropy, clustering, pairs.Count, maxShare,
                longShips, BoardDependencyAnalyzer.Analyze(board));
        }

        private static void AddPair(BoardModel board, string source, GridPosition neighbor,
            ISet<ShipPair> pairs)
        {
            if (!board.IsInside(neighbor)) return;
            var other = board.GetShipId(neighbor);
            if (other != null && other != source) pairs.Add(new ShipPair(source, other));
        }

        private readonly struct ShipPair : IEquatable<ShipPair>
        {
            public string First { get; }
            public string Second { get; }

            public ShipPair(string a, string b)
            {
                if (string.CompareOrdinal(a, b) <= 0) { First = a; Second = b; }
                else { First = b; Second = a; }
            }

            public bool Equals(ShipPair other) => First == other.First && Second == other.Second;
            public override bool Equals(object obj) => obj is ShipPair other && Equals(other);
            public override int GetHashCode() => unchecked((First.GetHashCode() * 397) ^ Second.GetHashCode());
        }
    }
}
