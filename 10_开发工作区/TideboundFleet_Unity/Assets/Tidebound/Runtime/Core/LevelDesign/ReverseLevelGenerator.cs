using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.LevelDesign
{
    public enum LevelGenerationStatus { Success, InvalidProfile, GenerationFailed, BudgetExceeded }

    public sealed class LevelGenerationResult
    {
        public LevelGenerationStatus Status { get; }
        public string Reason { get; }
        public LevelData Level { get; }
        public LevelSolutionProof ConstructionProof { get; }
        public LevelSolutionProof SolverProof { get; }
        public LevelSolverResult Solver { get; }
        public LevelAnalysisReport Analysis { get; }
        public int CandidateEvaluations { get; }
        public int Backtracks { get; }
        public int DeepestShipCount { get; }
        public int IndependentSeedCount { get; }

        internal LevelGenerationResult(LevelGenerationStatus status, string reason, int evaluations,
            int backtracks, int deepest, int independentSeeds, LevelData level = null,
            LevelSolutionProof construction = null, LevelSolutionProof solverProof = null,
            LevelSolverResult solver = null, LevelAnalysisReport analysis = null)
        {
            Status = status; Reason = reason; CandidateEvaluations = evaluations; Backtracks = backtracks;
            DeepestShipCount = deepest; IndependentSeedCount = independentSeeds; Level = level;
            ConstructionProof = construction; SolverProof = solverProof; Solver = solver; Analysis = analysis;
        }
    }

    /// <summary>
    /// Inserts ships in reverse solution order. Every insertion has its own entire exit ray clear;
    /// it may block existing ships. Randomness only ranks already legal placements, never fills a board first.
    /// </summary>
    public static class ReverseLevelGenerator
    {
        public const string Version = "ReverseInsertionV1";

        public static LevelGenerationResult Generate(string levelId, ReverseGenerationProfile profile,
            int seed, ReverseGenerationBudget budget = null)
        {
            budget = budget ?? new ReverseGenerationBudget();
            if (string.IsNullOrWhiteSpace(levelId) || levelId != levelId.Trim() || profile == null ||
                !profile.IsValid || !budget.IsValid)
                return new LevelGenerationResult(LevelGenerationStatus.InvalidProfile, "Invalid id, profile or budget.", 0, 0, 0, 0);
            if (profile.ShipCount * 2 + profile.LongShipCount > profile.Area.Width * profile.Area.Height)
                return new LevelGenerationResult(LevelGenerationStatus.GenerationFailed, "Requested footprints exceed the placement area.", 0, 0, 0, 0);
            return new Search(levelId, profile, seed, budget).Run();
        }

        private sealed class Search
        {
            private readonly string levelId;
            private readonly ReverseGenerationProfile profile;
            private readonly ReverseGenerationBudget budget;
            private readonly StableRandom random;
            private readonly Stopwatch timer = Stopwatch.StartNew();
            private readonly List<Candidate> inserted = new List<Candidate>();
            private readonly List<Frame> frames = new List<Frame>();
            private readonly int[,] occupied;
            private int evaluations, backtracks, deepest;
            private string budgetReason;
            private string lastFailure = "No legal candidate in the bounded branch set.";

            public Search(string levelId, ReverseGenerationProfile profile, int seed, ReverseGenerationBudget budget)
            {
                this.levelId = levelId; this.profile = profile; this.budget = budget;
                random = new StableRandom(seed);
                occupied = new int[profile.Width, profile.Height];
                for (var y = 0; y < profile.Height; y++)
                for (var x = 0; x < profile.Width; x++) occupied[x, y] = -1;
            }

            public LevelGenerationResult Run()
            {
                while (true)
                {
                    if (TimedOut() || budgetReason != null) return Failure(LevelGenerationStatus.BudgetExceeded, budgetReason);
                    if (inserted.Count == profile.ShipCount)
                    {
                        var complete = TryFinish();
                        if (TimedOut()) return Failure(LevelGenerationStatus.BudgetExceeded, budgetReason);
                        if (complete != null) return complete;
                        if (!Backtrack()) return Failure(LevelGenerationStatus.BudgetExceeded, budgetReason);
                        continue;
                    }
                    if (frames.Count == inserted.Count)
                    {
                        var candidates = EnumerateCandidates();
                        if (budgetReason != null) return Failure(LevelGenerationStatus.BudgetExceeded, budgetReason);
                        frames.Add(new Frame(candidates));
                    }
                    var frame = frames[inserted.Count];
                    if (frame.Next >= frame.Candidates.Count)
                    {
                        frames.RemoveAt(frames.Count - 1);
                        if (inserted.Count == 0) return Failure(LevelGenerationStatus.GenerationFailed, lastFailure);
                        if (!Backtrack()) return Failure(LevelGenerationStatus.BudgetExceeded, budgetReason);
                        continue;
                    }
                    var candidate = frame.Candidates[frame.Next++];
                    var index = inserted.Count;
                    inserted.Add(candidate);
                    foreach (var cell in Cells(candidate.Ship)) occupied[cell.X, cell.Y] = index;
                    // The cheap geometric filter above is checked against the actual runtime query at commit.
                    var board = BuildBoard();
                    if (!board.QueryForwardPath(candidate.Ship.Id).CanExit)
                        throw new InvalidOperationException("Reverse insertion violated the runtime exit invariant.");
                    deepest = Math.Max(deepest, inserted.Count);
                }
            }

            private bool Backtrack()
            {
                if (backtracks >= budget.MaxBacktracks)
                { budgetReason = "Backtrack budget reached. " + lastFailure; return false; }
                backtracks++;
                var last = inserted[inserted.Count - 1];
                foreach (var cell in Cells(last.Ship)) occupied[cell.X, cell.Y] = -1;
                inserted.RemoveAt(inserted.Count - 1);
                while (frames.Count > inserted.Count + 1) frames.RemoveAt(frames.Count - 1);
                return true;
            }

            private List<Candidate> EnumerateCandidates()
            {
                var count = inserted.Count;
                var watchers = new List<int>[profile.Width, profile.Height];
                var outDegree = new int[count];
                var directionCounts = new int[4];
                var independentCount = 0;
                foreach (var item in inserted)
                {
                    directionCounts[(int)item.Ship.Direction]++;
                    if (item.Blocked.Length == 0) independentCount++;
                    foreach (var blocked in item.Blocked) outDegree[blocked]++;
                }
                for (var i = 0; i < count; i++)
                {
                    var ship = inserted[i].Ship;
                    var delta = GridFootprint.DirectionStep(ship.Direction);
                    var x = ship.Position.X + ship.Length * delta.X;
                    var y = ship.Position.Y + ship.Length * delta.Y;
                    // Full rays intentionally continue through existing blockers.
                    while (Inside(x, y))
                    {
                        if (watchers[x, y] == null) watchers[x, y] = new List<int>();
                        watchers[x, y].Add(i);
                        x += delta.X; y += delta.Y;
                    }
                }
                var rootCount = outDegree.Count(x => x == 0);
                var length = (count + 1) * profile.LongShipCount / profile.ShipCount >
                             count * profile.LongShipCount / profile.ShipCount ? 3 : 2;
                var candidates = new List<Candidate>();
                for (var direction = 0; direction < 4; direction++)
                {
                    if (directionCounts[direction] + 1 > Math.Floor(profile.ShipCount * profile.MaxDirectionShare)) continue;
                    var step = GridFootprint.DirectionStep((ShipDirection)direction);
                    for (var y = profile.Area.Y; y < profile.Area.Y + profile.Area.Height; y++)
                    for (var x = profile.Area.X; x < profile.Area.X + profile.Area.Width; x++)
                    {
                        if (evaluations >= budget.MaxCandidateEvaluations)
                        { budgetReason = "Candidate evaluation budget reached."; return candidates; }
                        evaluations++;
                        if ((evaluations & 255) == 0 && TimedOut()) return candidates;
                        var hx = x + (length - 1) * step.X;
                        var hy = y + (length - 1) * step.Y;
                        if (!profile.Area.Contains(hx, hy)) continue;
                        var clear = true;
                        for (var k = 0; k < length; k++)
                            if (occupied[x + k * step.X, y + k * step.Y] >= 0) { clear = false; break; }
                        if (!clear) continue;
                        var px = hx + step.X; var py = hy + step.Y; var clearance = 0;
                        while (Inside(px, py) && occupied[px, py] < 0)
                        { clearance++; px += step.X; py += step.Y; }
                        if (Inside(px, py)) continue;

                        var blockedSet = new HashSet<int>();
                        var sameNeighbors = new HashSet<int>();
                        for (var k = 0; k < length; k++)
                        {
                            var cx = x + k * step.X; var cy = y + k * step.Y;
                            if (watchers[cx, cy] != null) blockedSet.UnionWith(watchers[cx, cy]);
                            AddSameNeighbor(cx + 1, cy, direction, sameNeighbors);
                            AddSameNeighbor(cx - 1, cy, direction, sameNeighbors);
                            AddSameNeighbor(cx, cy + 1, direction, sameNeighbors);
                            AddSameNeighbor(cx, cy - 1, direction, sameNeighbors);
                        }
                        var blocked = blockedSet.OrderBy(i => i).ToArray();
                        if (blocked.Length == 0 && independentCount >= profile.MaxIndependentSeeds) continue;
                        var depth = blocked.Length == 0 ? 1 : 1 + blocked.Max(i => inserted[i].IncomingDepth);
                        if (depth > profile.MaxDependencyDepth) continue;
                        var rootsAfter = rootCount + 1 - blocked.Count(i => outDegree[i] == 0);
                        if (rootsAfter > profile.MaxInitialExits) continue;
                        var desiredRoots = (int)Math.Round(2 +
                            (profile.MinInitialExits + profile.MaxInitialExits - 4) * 0.5 * (count + 1) / profile.ShipCount);
                        var central = Math.Min(Math.Min(x - profile.Area.X, profile.Area.X + profile.Area.Width - 1 - x),
                            Math.Min(y - profile.Area.Y, profile.Area.Y + profile.Area.Height - 1 - y));
                        var score = -Math.Abs(rootsAfter - desiredRoots) * 150 + Math.Min(blocked.Length, 3) * 6 -
                            directionCounts[direction] * 20 - sameNeighbors.Count * 10 + clearance * 40 - depth * 60;
                        if (count < 8) score += central * 8;
                        score += (int)(random.Next() % 35);
                        candidates.Add(new Candidate(new ShipPlacementData
                        {
                            Id = "S" + (count + 1).ToString("D3", CultureInfo.InvariantCulture),
                            TypeId = FoundationLimits.BaseShipTypeId, Length = length,
                            Position = new GridPosition(x, y), Direction = (ShipDirection)direction
                        }, blocked, depth, score, random.Next()));
                    }
                }
                // Stable ordering, fixed PRNG and fixed operation budget make successful runs reproducible.
                return candidates.OrderByDescending(x => x.Score).ThenBy(x => x.TieBreak)
                    .Take(budget.BranchWidth).ToList();
            }

            private LevelGenerationResult TryFinish()
            {
                var board = BuildBoard();
                var analysis = LevelStructureAnalyzer.Analyze(board);
                var graph = analysis.Dependencies;
                if (graph.CompleteCycles.Count != 0 || graph.CompleteDependencyDepth < profile.MinDependencyDepth ||
                    graph.CompleteDependencyDepth > profile.MaxDependencyDepth ||
                    graph.InitialExitCount < profile.MinInitialExits || graph.InitialExitCount > profile.MaxInitialExits ||
                    graph.InitialMoveCount < profile.MinPartialMoves || analysis.LongShipCount != profile.LongShipCount ||
                    analysis.DirectionEntropy < profile.MinDirectionEntropy ||
                    analysis.DirectionClustering > profile.MaxDirectionClustering ||
                    analysis.MaximumDirectionShare > profile.MaxDirectionShare)
                { lastFailure = "Completed geometry failed the requested structure gates."; return null; }

                var solution = LevelSolver.Solve(board);
                if (solution.Status != LevelSolverStatus.Solved || solution.ShipIds.Count != profile.ShipCount)
                { lastFailure = "Independent runtime solver did not certify the generated layout."; return null; }
                var construction = LevelSolutionProof.Create(levelId, board, inserted.AsEnumerable().Reverse().Select(x => x.Ship.Id));
                var proof = LevelSolutionProof.Create(levelId, board, solution.ShipIds);
                if (!construction.Replay(levelId, board).IsComplete || !proof.Replay(levelId, board).IsComplete)
                    throw new InvalidOperationException("Generated proof replay failed.");
                var level = new LevelData
                {
                    SchemaVersion = FoundationLimits.LevelSchemaVersion, LevelId = levelId,
                    Width = profile.Width, Height = profile.Height, BossId = "TF_KRAKEN_01",
                    Ships = inserted.Select(x => x.Ship).ToArray()
                };
                return new LevelGenerationResult(LevelGenerationStatus.Success, null, evaluations, backtracks, deepest,
                    inserted.Count(x => x.Blocked.Length == 0), level, construction, proof, solution, analysis);
            }

            private void AddSameNeighbor(int x, int y, int direction, ISet<int> same)
            {
                if (Inside(x, y) && occupied[x, y] >= 0 && (int)inserted[occupied[x, y]].Ship.Direction == direction)
                    same.Add(occupied[x, y]);
            }

            private BoardModel BuildBoard() => new BoardModel(profile.Width, profile.Height,
                inserted.Select(x => new ShipRuntimeData(x.Ship.Id, x.Ship.TypeId,
                    x.Ship.Length == 3 ? FoundationLimits.DefaultLongSkinId : FoundationLimits.DefaultStandardSkinId,
                    x.Ship.Position, x.Ship.Direction, x.Ship.Length, FoundationLimits.BaseShipDamage)));

            private static IEnumerable<GridPosition> Cells(ShipPlacementData ship) =>
                GridFootprint.Cells(ship.Position, ship.Direction, ship.Length);
            private bool Inside(int x, int y) => x >= 0 && y >= 0 && x < profile.Width && y < profile.Height;
            private bool TimedOut()
            {
                if (timer.ElapsedMilliseconds < budget.TimeLimitMilliseconds) return false;
                budgetReason = "Generation time budget reached.";
                return true;
            }
            private LevelGenerationResult Failure(LevelGenerationStatus status, string reason) =>
                new LevelGenerationResult(status, reason, evaluations, backtracks, deepest,
                    inserted.Count(x => x.Blocked.Length == 0));
        }

        private sealed class Candidate
        {
            public ShipPlacementData Ship { get; }
            public int[] Blocked { get; }
            public int IncomingDepth { get; }
            public int Score { get; }
            public uint TieBreak { get; }
            public Candidate(ShipPlacementData ship, int[] blocked, int depth, int score, uint tieBreak)
            { Ship = ship; Blocked = blocked; IncomingDepth = depth; Score = score; TieBreak = tieBreak; }
        }
        private sealed class Frame
        {
            public List<Candidate> Candidates { get; }
            public int Next { get; set; }
            public Frame(List<Candidate> candidates) { Candidates = candidates; }
        }
        private sealed class StableRandom
        {
            private uint state;
            public StableRandom(int seed) { state = unchecked((uint)seed) ^ 0x9E3779B9u; if (state == 0) state = 1; }
            public uint Next()
            {
                unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; }
                return state;
            }
        }
    }
}
