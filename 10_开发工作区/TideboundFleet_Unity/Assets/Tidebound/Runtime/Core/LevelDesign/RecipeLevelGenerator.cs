using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.LevelDesign
{
    public sealed class RecipeGenerationBudget
    {
        public int MaxMutations { get; }
        public int TimeLimitMilliseconds { get; }
        public ReverseGenerationBudget Bootstrap { get; }
        public RecipeGenerationBudget(int maxMutations = 60000, int timeLimitMilliseconds = 60000,
            ReverseGenerationBudget bootstrap = null)
        {
            MaxMutations = maxMutations; TimeLimitMilliseconds = timeLimitMilliseconds;
            Bootstrap = bootstrap ?? new ReverseGenerationBudget();
        }
        internal bool IsValid => MaxMutations >= 0 && TimeLimitMilliseconds > 0 && Bootstrap.IsValid;
    }

    public sealed class RecipeGenerationResult
    {
        public LevelGenerationStatus Status { get; }
        public string Reason { get; }
        public LevelRecipe Recipe { get; }
        public int Seed { get; }
        public RecipeGenerationBudget Budget { get; }
        public ReverseGenerationProfile BootstrapProfile { get; }
        public LevelGenerationResult Generation { get; }
        public int MutationsAttempted { get; }
        public int MutationsAccepted { get; }
        public double BestPenalty { get; }

        internal RecipeGenerationResult(LevelGenerationStatus status, string reason, LevelRecipe recipe, int seed,
            RecipeGenerationBudget budget, ReverseGenerationProfile bootstrapProfile, LevelGenerationResult generation,
            int attempted, int accepted, double bestPenalty)
        {
            Status = status; Reason = reason; Recipe = recipe; Seed = seed; Budget = budget;
            BootstrapProfile = bootstrapProfile; Generation = generation;
            MutationsAttempted = attempted; MutationsAccepted = accepted; BestPenalty = bestPenalty;
        }
    }

    /// <summary>
    /// Reverse insertion creates a complete solvable scaffold. Bounded local reinsertion changes one ship
    /// while preserving the complete dependency DAG. Final layouts are ordered and checked as reverse
    /// insertion prefixes again, then independently solved and replayed using the actual movement rules.
    /// </summary>
    public static class RecipeLevelGenerator
    {
        public const string Version = "ReverseRefinementV1";

        public static RecipeGenerationResult Generate(LevelRecipe recipe, int seed, RecipeGenerationBudget budget = null)
        {
            budget = budget ?? new RecipeGenerationBudget();
            if (recipe == null || !recipe.IsValid || !budget.IsValid)
                return new RecipeGenerationResult(LevelGenerationStatus.InvalidProfile, "Invalid recipe or budget.",
                    recipe, seed, budget, null, null, 0, 0, double.PositiveInfinity);
            var timer = Stopwatch.StartNew();
            var p = recipe.Profile;
            // This is an explicit construction stage, not a fallback that loosens the final recipe.
            var bootstrapProfile = p.ShipCount < 40 ? p : new ReverseGenerationProfile(
                "I2_Bootstrap", p.Width, p.Height, p.ShipCount, p.LongShipCount, 12, 20, 3, 20,
                p.MaxIndependentSeeds, p.MinDirectionEntropy, 0.60, p.MaxDirectionShare, p.MinPartialMoves, p.Area);
            var bootBudget = new ReverseGenerationBudget(budget.Bootstrap.MaxCandidateEvaluations,
                budget.Bootstrap.MaxBacktracks, budget.Bootstrap.BranchWidth,
                Math.Min(budget.TimeLimitMilliseconds, budget.Bootstrap.TimeLimitMilliseconds));
            var scaffold = ReverseLevelGenerator.Generate(recipe.LevelId, bootstrapProfile, seed, bootBudget);
            if (scaffold.Status != LevelGenerationStatus.Success)
                return new RecipeGenerationResult(scaffold.Status, "Reverse scaffold: " + scaffold.Reason,
                    recipe, seed, budget, bootstrapProfile, null, 0, 0, double.PositiveInfinity);

            var ships = scaffold.Level.Ships.ToArray();
            var board = Build(p, ships);
            var analysis = LevelStructureAnalyzer.Analyze(board);
            var local = LocalLayoutAnalyzer.Analyze(board, new LocalLayoutOptions(p.Area));
            var penalty = recipe.Cost(board, analysis, local);
            var bestPenalty = penalty;
            var random = new StableRandom(seed);
            var attempted = 0; var accepted = 0;
            while (penalty > 0 && attempted < budget.MaxMutations && timer.ElapsedMilliseconds < budget.TimeLimitMilliseconds)
            {
                var iteration = attempted++;
                var index = random.Next(ships.Length);
                var old = ships[index];
                var direction = random.Next(3) == 0 ? old.Direction : (ShipDirection)random.Next(4);
                int x, y;
                if (random.Next(3) < 2)
                { x = old.Position.X + random.Next(-3, 4); y = old.Position.Y + random.Next(-3, 4); }
                else
                { x = random.Next(p.Area.X, p.Area.X + p.Area.Width); y = random.Next(p.Area.Y, p.Area.Y + p.Area.Height); }
                var position = new GridPosition(x, y);
                if (position.Equals(old.Position) && direction == old.Direction) continue;
                var cells = GridFootprint.Cells(position, direction, old.Length);
                if (cells.Any(c => !p.Area.Contains(c.X, c.Y) ||
                    (board.GetShipId(c) != null && board.GetShipId(c) != old.Id))) continue;

                ships[index] = new ShipPlacementData { Id = old.Id, TypeId = old.TypeId,
                    Length = old.Length, Position = position, Direction = direction };
                var next = Build(p, ships);
                var nextAnalysis = LevelStructureAnalyzer.Analyze(next);
                if (nextAnalysis.Dependencies.CompleteCycles.Count != 0)
                { ships[index] = old; continue; }
                var nextLocal = LocalLayoutAnalyzer.Analyze(next, new LocalLayoutOptions(p.Area));
                var nextPenalty = recipe.Cost(next, nextAnalysis, nextLocal);
                // Deterministic annealing escapes local minima; hard recipe gates are still checked on export.
                var temperature = 8 * (1 - (iteration % 10000) / 10000.0) + 0.05;
                if (nextPenalty <= penalty || random.NextDouble() < Math.Exp((penalty - nextPenalty) / temperature))
                {
                    board = next; analysis = nextAnalysis; penalty = nextPenalty; accepted++;
                    bestPenalty = Math.Min(bestPenalty, penalty);
                }
                else ships[index] = old;
            }

            if (timer.ElapsedMilliseconds >= budget.TimeLimitMilliseconds || penalty > 0)
                return new RecipeGenerationResult(LevelGenerationStatus.BudgetExceeded,
                    timer.ElapsedMilliseconds >= budget.TimeLimitMilliseconds ? "Recipe time budget reached." : "Refinement mutation budget reached.",
                    recipe, seed, budget, bootstrapProfile, null, attempted, accepted, bestPenalty);
            var issues = recipe.Check(board);
            if (issues.Count > 0) throw new InvalidOperationException("Zero penalty violated final recipe: " + string.Join(",", issues));

            var constructionOrder = ExitOrderFromGraph(board, analysis.Dependencies);
            var byId = ships.ToDictionary(s => s.Id, StringComparer.Ordinal);
            var insertionOrder = constructionOrder.Reverse().Select(id => byId[id]).ToArray();
            // Validate every reverse prefix, independently of the graph used to propose local changes.
            for (var n = 1; n <= insertionOrder.Length; n++)
            {
                var prefix = Build(p, insertionOrder.Take(n));
                if (!prefix.QueryForwardPath(insertionOrder[n - 1].Id).CanExit)
                    throw new InvalidOperationException("Refined reverse insertion witness is invalid.");
            }
            var solution = LevelSolver.Solve(board);
            if (solution.Status != LevelSolverStatus.Solved || solution.ShipIds.Count != p.ShipCount)
                return new RecipeGenerationResult(LevelGenerationStatus.GenerationFailed, "Independent solver did not certify the full recipe.",
                    recipe, seed, budget, bootstrapProfile, null, attempted, accepted, bestPenalty);
            var construction = LevelSolutionProof.Create(recipe.LevelId, board, constructionOrder);
            var proof = LevelSolutionProof.Create(recipe.LevelId, board, solution.ShipIds);
            if (!construction.Replay(recipe.LevelId, board).IsComplete || !proof.Replay(recipe.LevelId, board).IsComplete)
                throw new InvalidOperationException("Recipe proof replay failed.");
            if (timer.ElapsedMilliseconds >= budget.TimeLimitMilliseconds)
                return new RecipeGenerationResult(LevelGenerationStatus.BudgetExceeded, "Certification time budget reached.",
                    recipe, seed, budget, bootstrapProfile, null, attempted, accepted, bestPenalty);
            var level = new LevelData { SchemaVersion = 2, LevelId = recipe.LevelId,
                Width = p.Width, Height = p.Height, BossId = scaffold.Level.BossId, Ships = insertionOrder };
            var result = new LevelGenerationResult(LevelGenerationStatus.Success, null, scaffold.CandidateEvaluations,
                scaffold.Backtracks, p.ShipCount, LevelRecipe.IndependentSeeds(board, analysis.Dependencies),
                level, construction, proof, solution, analysis, Version);
            return new RecipeGenerationResult(LevelGenerationStatus.Success, null, recipe, seed, budget, bootstrapProfile,
                result, attempted, accepted, bestPenalty);
        }

        private static string[] ExitOrderFromGraph(BoardModel board, BoardDependencyGraph graph)
        {
            var remaining = new HashSet<string>(board.Ships.Select(s => s.Id), StringComparer.Ordinal);
            var order = new List<string>();
            while (remaining.Count > 0)
            {
                // Use descending IDs for the construction witness; Solver independently queries actual paths.
                var id = remaining.Where(s => !graph.GetNode(s).AllBlockerShipIds.Any(remaining.Contains))
                    .OrderByDescending(s => s, StringComparer.Ordinal).FirstOrDefault();
                if (id == null) throw new InvalidOperationException("Acyclic witness expected.");
                order.Add(id); remaining.Remove(id);
            }
            return order.ToArray();
        }

        private static BoardModel Build(ReverseGenerationProfile profile, IEnumerable<ShipPlacementData> ships) =>
            new BoardModel(profile.Width, profile.Height, ships.Select(s => new ShipRuntimeData(s.Id, s.TypeId,
                s.Length == 3 ? FoundationLimits.DefaultLongSkinId : FoundationLimits.DefaultStandardSkinId,
                s.Position, s.Direction, s.Length, FoundationLimits.BaseShipDamage)));

        private sealed class StableRandom
        {
            private uint state;
            public StableRandom(int seed) { state = unchecked((uint)seed) ^ 0x9E3779B9u; if (state == 0) state = 1; }
            private uint NextUInt()
            { unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; } return state; }
            public int Next(int count) => (int)(NextUInt() % (uint)count);
            public int Next(int from, int to) => from + Next(to - from);
            public double NextDouble() => NextUInt() / 4294967296.0;
        }
    }
}
