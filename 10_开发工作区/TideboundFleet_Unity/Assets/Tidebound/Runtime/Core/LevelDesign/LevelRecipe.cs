using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    /// <summary>Versioned candidate-screen parameters, not frozen mobile product requirements.</summary>
    public sealed class LevelRecipe
    {
        public string LevelId { get; }
        public ReverseGenerationProfile Profile { get; }
        public int MaximumRunShips { get; }
        public double MinimumLocalEntropy { get; }
        public double MinimumP10Entropy { get; }
        public int MaximumEmptyArea { get; }

        public LevelRecipe(string levelId, ReverseGenerationProfile profile, int maximumRunShips,
            double minimumLocalEntropy, double minimumP10Entropy, int maximumEmptyArea)
        {
            LevelId = levelId; Profile = profile; MaximumRunShips = maximumRunShips;
            MinimumLocalEntropy = minimumLocalEntropy; MinimumP10Entropy = minimumP10Entropy;
            MaximumEmptyArea = maximumEmptyArea;
        }

        internal bool IsValid => !string.IsNullOrWhiteSpace(LevelId) && LevelId == LevelId.Trim() &&
            Profile != null && Profile.IsValid && MaximumRunShips > 0 &&
            MinimumLocalEntropy >= 0 && MinimumLocalEntropy <= 1 &&
            MinimumP10Entropy >= MinimumLocalEntropy && MinimumP10Entropy <= 1 && MaximumEmptyArea >= 0;

        public IReadOnlyList<string> Check(BoardModel board)
        {
            if (!IsValid) throw new InvalidOperationException("Invalid recipe.");
            if (board == null) throw new ArgumentNullException(nameof(board));
            var issues = new List<string>();
            var p = Profile;
            if (board.Width != p.Width || board.Height != p.Height) issues.Add("GRID_SIZE");
            if (board.ShipCount != p.ShipCount) issues.Add("SHIP_COUNT");
            if (board.Ships.Any(s => s.OccupiedCells.Any(c => !p.Area.Contains(c.X, c.Y)))) issues.Add("PLACEMENT_AREA");
            var a = LevelStructureAnalyzer.Analyze(board);
            var g = a.Dependencies;
            if (a.LongShipCount != p.LongShipCount) issues.Add("LONG_SHIP_COUNT");
            if (g.CompleteCycles.Count != 0) issues.Add("COMPLETE_CYCLE");
            if (!g.CompleteDependencyDepth.HasValue ||
                Range(g.CompleteDependencyDepth.Value, p.MinDependencyDepth, p.MaxDependencyDepth) > 0) issues.Add("DEPENDENCY_DEPTH");
            if (Range(g.InitialExitCount, p.MinInitialExits, p.MaxInitialExits) > 0) issues.Add("INITIAL_EXITS");
            if (g.InitialMoveCount < p.MinPartialMoves) issues.Add("PARTIAL_MOVES");
            if (IndependentSeeds(board, g) > p.MaxIndependentSeeds) issues.Add("INDEPENDENT_SEEDS");
            if (a.DirectionEntropy < p.MinDirectionEntropy) issues.Add("GLOBAL_ENTROPY");
            if (a.DirectionClustering > p.MaxDirectionClustering) issues.Add("DIRECTION_CLUSTERING");
            if (a.MaximumDirectionShare > p.MaxDirectionShare) issues.Add("DIRECTION_SHARE");
            if (board.Width != p.Width || board.Height != p.Height) return issues.AsReadOnly();
            var l = LocalLayoutAnalyzer.Analyze(board, new LocalLayoutOptions(p.Area));
            if (LongestRun(l) > MaximumRunShips) issues.Add("LOCAL_RUN");
            if (!l.MinimumWindowEntropy.HasValue || l.MinimumWindowEntropy < MinimumLocalEntropy) issues.Add("LOCAL_ENTROPY");
            if (!l.P10WindowEntropy.HasValue || l.P10WindowEntropy < MinimumP10Entropy) issues.Add("LOCAL_P10");
            if (l.LargestEmptyArea > MaximumEmptyArea) issues.Add("EMPTY_RECTANGLE");
            return issues.AsReadOnly();
        }

        internal double Cost(BoardModel board, LevelAnalysisReport a, LocalLayoutReport l)
        {
            var p = Profile; var g = a.Dependencies;
            if (!g.CompleteDependencyDepth.HasValue) return double.PositiveInfinity;
            return Range(g.InitialExitCount, p.MinInitialExits, p.MaxInitialExits) * 120 +
                Range(g.CompleteDependencyDepth.Value, p.MinDependencyDepth, p.MaxDependencyDepth) * 100 +
                Math.Max(0, LongestRun(l) - MaximumRunShips) * 80 +
                Math.Max(0, MinimumLocalEntropy - (l.MinimumWindowEntropy ?? -1)) * 200 +
                Math.Max(0, MinimumP10Entropy - (l.P10WindowEntropy ?? -1)) * 200 +
                Math.Max(0, l.LargestEmptyArea - MaximumEmptyArea) * 12 +
                Math.Max(0, p.MinDirectionEntropy - a.DirectionEntropy) * 400 +
                Math.Max(0, a.MaximumDirectionShare - p.MaxDirectionShare) * 400 +
                Math.Max(0, a.DirectionClustering - p.MaxDirectionClustering) * 200 +
                Math.Max(0, p.MinPartialMoves - g.InitialMoveCount) * 100 +
                Math.Max(0, IndependentSeeds(board, g) - p.MaxIndependentSeeds) * 100;
        }

        public static int LongestRun(LocalLayoutReport report) => Math.Max(
            report.LongestGappedRow?.ShipCount ?? 0, report.LongestGappedColumn?.ShipCount ?? 0);

        internal static int IndependentSeeds(BoardModel board, BoardDependencyGraph graph)
        {
            var blockers = new HashSet<string>(board.Ships.SelectMany(s => graph.GetNode(s.Id).AllBlockerShipIds));
            return board.Ships.Count(s => !blockers.Contains(s.Id));
        }

        private static double Range(double value, double minimum, double maximum) =>
            Math.Max(minimum - value, Math.Max(value - maximum, 0));
    }

    public static class Phase5RLevelRecipes
    {
        public const string Version = "TenRecipesV1";
        public const string ScreeningVersion = "LocalCandidateScreenV1";
        public static readonly IReadOnlyList<LevelRecipe> All = Array.AsReadOnly(new[]
        {
            new LevelRecipe("P5R_Ten_001", ReverseGenerationProfiles.Tutorial, 3, 0.30, 0.40, 16),
            Full(2, 0, 12, 16, 5, 8), Full(3, 0, 10, 14, 6, 9),
            Full(4, 4, 10, 14, 7, 10), Full(5, 4, 8, 12, 8, 12), Full(6, 4, 12, 16, 6, 9),
            Full(7, 6, 8, 12, 10, 14), Full(8, 6, 6, 10, 11, 15),
            Full(9, 6, 6, 10, 12, 16), Full(10, 8, 6, 10, 12, 18)
        });

        private static LevelRecipe Full(int n, int longs, int minExits, int maxExits, int minDepth, int maxDepth) =>
            new LevelRecipe("P5R_Ten_" + n.ToString("D3", System.Globalization.CultureInfo.InvariantCulture),
                new ReverseGenerationProfile("I2_Level_" + n, 14, 18, 80, longs,
                    minExits, maxExits, minDepth, maxDepth, 26, maxDirectionClustering: 0.55),
                4, 0.30, 0.65, 12);
    }
}
