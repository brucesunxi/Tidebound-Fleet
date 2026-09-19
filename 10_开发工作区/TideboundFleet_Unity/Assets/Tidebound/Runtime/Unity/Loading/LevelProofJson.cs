using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.LevelDesign;

namespace Tidebound.Config
{
    /// <summary>Versioned proof sidecar. Metadata is diagnostic; only real replay certifies solvability.</summary>
    public static class LevelProofJson
    {
        public static string Write(LevelSolutionProof proof)
        {
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            var steps = new JArray();
            foreach (var step in proof.Steps)
                steps.Add(new JObject
                {
                    ["shipId"] = step.ShipId, ["from"] = Position(step.From), ["to"] = Position(step.To),
                    ["outcome"] = step.Outcome.ToString(), ["beforeHash"] = step.BeforeHash, ["afterHash"] = step.AfterHash
                });
            return new JObject
            {
                ["proofVersion"] = proof.ProofVersion, ["rulesVersion"] = proof.RulesVersion,
                ["exitMode"] = proof.ExitMode, ["levelId"] = proof.LevelId,
                ["layoutFingerprint"] = proof.LayoutFingerprint, ["solutionSteps"] = steps
            }.ToString(Formatting.Indented) + Environment.NewLine;
        }

        public static string WriteGenerated(LevelGenerationResult result, ReverseGenerationProfile profile, int seed)
        {
            if (result == null || result.Status != LevelGenerationStatus.Success || profile == null)
                throw new ArgumentException("A successful generation and its profile are required.");
            var root = JObject.Parse(Write(result.SolverProof));
            root["status"] = "AlgorithmPrototypeOnly";
            root["generatorVersion"] = result.GeneratorVersion;
            root["solverVersion"] = LevelSolver.Version;
            root["seed"] = seed; root["profileId"] = profile.Id;
            root["optimality"] = result.Solver.Optimality.ToString();
            root["constructionOrder"] = new JArray(result.ConstructionProof.Steps.Select(x => x.ShipId));
            var available = new JArray();
            var unlocked = new JArray();
            var board = CreateBoard(result.Level);
            if (!result.SolverProof.Replay(result.Level.LevelId, board).IsComplete)
                throw new ArgumentException("Generated layout was changed after proof creation.");
            foreach (var step in result.SolverProof.Steps)
            {
                var before = new HashSet<string>(board.Ships.Where(x => board.QueryForwardPath(x.Id).CanExit).Select(x => x.Id));
                available.Add(before.Count);
                board = board.ApplyPathResult(board.QueryForwardPath(step.ShipId));
                unlocked.Add(board.Ships.Count(x => board.QueryForwardPath(x.Id).CanExit && !before.Contains(x.Id)));
            }
            var report = result.Analysis;
            root["metrics"] = new JObject
            {
                ["shipCount"] = report.ShipCount, ["longShipCount"] = report.LongShipCount,
                ["initialExitCount"] = report.Dependencies.InitialExitCount,
                ["initialPartialMoveCount"] = report.Dependencies.InitialMoveCount,
                ["completeDependencyDepthNodes"] = report.Dependencies.CompleteDependencyDepth,
                ["directionEntropy"] = report.DirectionEntropy, ["directionClustering"] = report.DirectionClustering,
                ["maximumDirectionShare"] = report.MaximumDirectionShare, ["occupancyRatio"] = report.OccupancyRatio,
                ["independentSeeds"] = result.IndependentSeedCount, ["candidateEvaluations"] = result.CandidateEvaluations,
                ["backtracks"] = result.Backtracks, ["availableByStep"] = available, ["unlockedByStep"] = unlocked
            };
            return root.ToString(Formatting.Indented) + Environment.NewLine;
        }

        public static LevelSolutionProof Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new LevelFormatException("Proof JSON is empty.");
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 16, DateParseHandling = DateParseHandling.None })
                {
                    var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new LevelFormatException("Unexpected trailing proof content.");
                    if (!(root["solutionSteps"] is JArray array) || array.Count > 10000)
                        throw new LevelFormatException("solutionSteps must be an array with at most 10000 entries.");
                    var steps = new List<LevelSolutionStep>();
                    foreach (var item in array)
                    {
                        if (!(item is JObject step)) throw new LevelFormatException("A proof step must be an object.");
                        var outcomeText = String(step, "outcome");
                        if (!Enum.TryParse<ForwardPathOutcome>(outcomeText, false, out var outcome) ||
                            outcome.ToString() != outcomeText || !Enum.IsDefined(typeof(ForwardPathOutcome), outcome))
                            throw new LevelFormatException("Proof outcome must be Blocked or Exit.");
                        steps.Add(new LevelSolutionStep(String(step, "shipId"), ReadPosition(step, "from"), ReadPosition(step, "to"),
                            outcome, String(step, "beforeHash"), String(step, "afterHash")));
                    }
                    return new LevelSolutionProof(Integer(root, "proofVersion"), String(root, "rulesVersion"),
                        String(root, "exitMode"), String(root, "levelId"), String(root, "layoutFingerprint"), steps);
                }
            }
            catch (Exception e) when (e is JsonException || e is OverflowException || e is FormatException)
            { throw new LevelFormatException("Invalid proof JSON: " + e.Message, e); }
        }

        private static BoardModel CreateBoard(LevelData level)
        {
            using (var session = Tidebound.Core.LevelSessionFactory.Create(level,
                       new[] { new Tidebound.Ship.ShipDefinition(Tidebound.Core.FoundationLimits.BaseShipTypeId, 10) },
                       new[] { new Tidebound.Boss.BossDefinition(level.BossId) })) return session.InitialBoard;
        }
        private static JObject Position(GridPosition p) => new JObject { ["x"] = p.X, ["y"] = p.Y };
        private static GridPosition ReadPosition(JObject parent, string name)
        {
            if (!(parent[name] is JObject value)) throw new LevelFormatException(name + " must be a position object.");
            return new GridPosition(Integer(value, "x"), Integer(value, "y"));
        }
        private static int Integer(JObject value, string name)
        {
            if (value[name]?.Type != JTokenType.Integer) throw new LevelFormatException(name + " must be an Int32.");
            return value[name].Value<int>();
        }
        private static string String(JObject value, string name)
        {
            if (value[name]?.Type != JTokenType.String) throw new LevelFormatException(name + " must be a string.");
            return value[name].Value<string>();
        }
    }
}
