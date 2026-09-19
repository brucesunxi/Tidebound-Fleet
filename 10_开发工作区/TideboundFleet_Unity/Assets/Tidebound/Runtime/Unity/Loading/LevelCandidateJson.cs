using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.LevelDesign;

namespace Tidebound.Config
{
    public static class LevelCandidateJson
    {
        public const string Status = "CandidateNeedsPlaytest";

        public static string Write(RecipeGenerationResult candidate)
        {
            if (candidate?.Status != LevelGenerationStatus.Success || candidate.Generation == null)
                throw new ArgumentException("A certified recipe candidate is required.", nameof(candidate));
            var result = candidate.Generation;
            var root = JObject.Parse(LevelProofJson.WriteGenerated(result, candidate.Recipe.Profile, candidate.Seed));
            using (var session = LevelSessionFactory.Create(result.Level,
                new[] { new Tidebound.Ship.ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                new[] { new Tidebound.Boss.BossDefinition("TF_KRAKEN_01") }))
            {
                var board = session.InitialBoard;
                var issues = candidate.Recipe.Check(board);
                if (issues.Count > 0 || !result.ConstructionProof.Replay(result.Level.LevelId, board).IsComplete)
                    throw new ArgumentException("Candidate no longer matches its recipe or construction witness.");
                root["status"] = Status;
                root["recipeVersion"] = Phase5RLevelRecipes.Version;
                root["screeningVersion"] = Phase5RLevelRecipes.ScreeningVersion;
                root["recipe"] = Profile(candidate.Recipe.Profile);
                root["localScreen"] = new JObject
                {
                    ["maximumRunShips"] = candidate.Recipe.MaximumRunShips,
                    ["minimumLocalEntropy"] = candidate.Recipe.MinimumLocalEntropy,
                    ["minimumP10Entropy"] = candidate.Recipe.MinimumP10Entropy,
                    ["maximumEmptyArea"] = candidate.Recipe.MaximumEmptyArea
                };
                root["bootstrapGeneratorVersion"] = ReverseLevelGenerator.Version;
                root["bootstrapProfile"] = Profile(candidate.BootstrapProfile);
                root["refinement"] = new JObject
                {
                    ["maxMutations"] = candidate.Budget.MaxMutations,
                    ["timeLimitMilliseconds"] = candidate.Budget.TimeLimitMilliseconds,
                    ["bootstrapMaxCandidateEvaluations"] = candidate.Budget.Bootstrap.MaxCandidateEvaluations,
                    ["bootstrapMaxBacktracks"] = candidate.Budget.Bootstrap.MaxBacktracks,
                    ["bootstrapBranchWidth"] = candidate.Budget.Bootstrap.BranchWidth,
                    ["bootstrapTimeLimitMilliseconds"] = candidate.Budget.Bootstrap.TimeLimitMilliseconds,
                    ["mutationsAttempted"] = candidate.MutationsAttempted,
                    ["mutationsAccepted"] = candidate.MutationsAccepted,
                    ["finalPenalty"] = candidate.BestPenalty
                };
                var local = LocalLayoutAnalyzer.Analyze(board, new LocalLayoutOptions(candidate.Recipe.Profile.Area));
                root["localMetrics"] = new JObject
                {
                    ["analyzerVersion"] = local.AnalyzerVersion,
                    ["area"] = Area(local.Area), ["windowSize"] = local.WindowSize,
                    ["minimumWindowShips"] = local.MinimumWindowShips, ["allowedGapCells"] = local.AllowedGapCells,
                    ["longestRunShips"] = LevelRecipe.LongestRun(local),
                    ["minimumWindowEntropy"] = local.MinimumWindowEntropy,
                    ["p10WindowEntropy"] = local.P10WindowEntropy,
                    ["eligibleWindows"] = local.Windows.Count, ["totalWindows"] = local.TotalWindowCount,
                    ["largestEmptyRectangle"] = Area(local.LargestEmptyRectangle),
                    ["regionOccupancy"] = new JArray(local.Regions.Select(r => new JObject
                        { ["area"] = Area(r.Area), ["occupancyRatio"] = r.OccupancyRatio }))
                };
                root["releaseTraces"] = new JArray
                {
                    Trace("solver-ascending", board, result.Solver.ShipIds),
                    Trace("construction-descending", board, result.ConstructionProof.Steps.Select(s => s.ShipId)),
                    Trace("seeded-exits", board, SeededExitOrder(board, candidate.Seed))
                };
            }
            return root.ToString(Formatting.Indented) + Environment.NewLine;
        }

        public static string WriteManifest(IEnumerable<RecipeGenerationResult> candidates)
        {
            var all = candidates?.ToArray() ?? throw new ArgumentNullException(nameof(candidates));
            var expected = new HashSet<string>(Phase5RLevelRecipes.All.Select(r => r.LevelId), StringComparer.Ordinal);
            if (all.Length != expected.Count || all.Any(c => c?.Recipe == null || !expected.Remove(c.Recipe.LevelId)))
                throw new ArgumentException("Manifest requires each of the ten recipe IDs exactly once.", nameof(candidates));
            var rows = new JArray();
            foreach (var candidate in all)
            {
                // Revalidate before writing a manifest; status/metrics alone are not acceptance evidence.
                var sidecar = JObject.Parse(Write(candidate));
                rows.Add(new JObject
                {
                    ["levelId"] = candidate.Recipe.LevelId, ["status"] = Status,
                    ["seed"] = candidate.Seed, ["layoutFile"] = candidate.Recipe.LevelId + ".json",
                    ["proofFile"] = candidate.Recipe.LevelId + ".solution.json",
                    ["layoutFingerprint"] = sidecar["layoutFingerprint"].DeepClone(),
                    ["metrics"] = sidecar["metrics"].DeepClone(), ["localMetrics"] = sidecar["localMetrics"].DeepClone()
                });
            }
            return new JObject
            {
                ["manifestVersion"] = 1, ["recipeVersion"] = Phase5RLevelRecipes.Version,
                ["screeningVersion"] = Phase5RLevelRecipes.ScreeningVersion,
                ["rulesVersion"] = LevelRules.Version, ["generatorVersion"] = RecipeLevelGenerator.Version,
                ["geometryIdentityVersion"] = LevelLayoutIdentity.Version,
                ["status"] = Status, ["levels"] = rows
            }.ToString(Formatting.Indented) + Environment.NewLine;
        }

        private static JObject Profile(ReverseGenerationProfile p) => new JObject
        {
            ["id"] = p.Id, ["width"] = p.Width, ["height"] = p.Height,
            ["shipCount"] = p.ShipCount, ["longShipCount"] = p.LongShipCount,
            ["minInitialExits"] = p.MinInitialExits, ["maxInitialExits"] = p.MaxInitialExits,
            ["minDependencyDepth"] = p.MinDependencyDepth, ["maxDependencyDepth"] = p.MaxDependencyDepth,
            ["maxIndependentSeeds"] = p.MaxIndependentSeeds,
            ["minDirectionEntropy"] = p.MinDirectionEntropy, ["maxDirectionClustering"] = p.MaxDirectionClustering,
            ["maxDirectionShare"] = p.MaxDirectionShare, ["minPartialMoves"] = p.MinPartialMoves,
            ["area"] = Area(p.Area)
        };
        private static JObject Area(GenerationArea a) => new JObject
            { ["x"] = a.X, ["y"] = a.Y, ["width"] = a.Width, ["height"] = a.Height };

        private static IEnumerable<string> SeededExitOrder(BoardModel initial, int seed)
        {
            var board = initial; var result = new List<string>();
            var state = unchecked((uint)seed) ^ 0x9E3779B9u; if (state == 0) state = 1;
            while (board.ShipCount > 0)
            {
                var available = board.Ships.Where(s => board.QueryForwardPath(s.Id).CanExit)
                    .Select(s => s.Id).OrderBy(s => s, StringComparer.Ordinal).ToArray();
                if (available.Length == 0) throw new InvalidOperationException("Candidate has no direct-exit continuation.");
                unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; }
                var id = available[state % (uint)available.Length];
                result.Add(id); board = board.ApplyPathResult(board.QueryForwardPath(id));
            }
            return result;
        }

        private static JObject Trace(string strategy, BoardModel initial, IEnumerable<string> ids)
        {
            var board = initial; var order = new JArray(); var available = new JArray();
            var partial = new JArray(); var unlocked = new JArray();
            foreach (var id in ids)
            {
                var paths = board.Ships.Select(s => board.QueryForwardPath(s.Id)).ToArray();
                var before = new HashSet<string>(board.Ships.Where(s => board.QueryForwardPath(s.Id).CanExit).Select(s => s.Id));
                var path = board.QueryForwardPath(id);
                if (!path.CanExit) throw new InvalidOperationException("Release trace requires a direct-exit step.");
                order.Add(id); available.Add(before.Count);
                partial.Add(paths.Count(p => p.IsBlocked && p.TravelDistance > 0));
                board = board.ApplyPathResult(path);
                unlocked.Add(board.Ships.Count(s => board.QueryForwardPath(s.Id).CanExit && !before.Contains(s.Id)));
            }
            if (board.ShipCount != 0) throw new InvalidOperationException("Incomplete release trace.");
            var prefix = available.Values<int>().Take((int)Math.Ceiling(initial.ShipCount * 0.6)).ToArray();
            var consecutive = 0; var longest = 0;
            foreach (var count in prefix) { consecutive = count == 1 ? consecutive + 1 : 0; longest = Math.Max(longest, consecutive); }
            return new JObject
            {
                ["strategy"] = strategy, ["shipIds"] = order,
                ["availableBefore"] = available, ["partialBefore"] = partial, ["newExits"] = unlocked,
                ["first60PercentMeanExits"] = prefix.Length == 0 ? 0 : prefix.Average(),
                ["first60PercentMaximumExits"] = prefix.Length == 0 ? 0 : prefix.Max(),
                ["first60PercentLongestSingleExitRun"] = longest,
                ["remainingShips"] = board.ShipCount
            };
        }
    }
}
