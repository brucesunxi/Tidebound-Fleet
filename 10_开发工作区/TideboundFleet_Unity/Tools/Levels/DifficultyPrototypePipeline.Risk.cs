using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;

internal static partial class DifficultyPrototypePipeline
{
    private static JToken Evidence(object value) => JToken.Parse(JsonConvert.SerializeObject(value, new StringEnumConverter()));
    private static void AuditRisk()
    {
        var campaign = Path.Combine(root, "Assets/Tidebound/Config/Levels/Campaign");
        var legacy = Path.Combine(root, "Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
        var entries = JObject.Parse(File.ReadAllText(Path.Combine(campaign, "manifest-100.json")))["levels"];
        var levels = new JArray();
        foreach (var row in entries)
        {
            var file = (string)row["layoutFile"];
            var folder = file.StartsWith("P5R_Ten_", StringComparison.Ordinal) ? legacy : campaign;
            var level = LevelJsonReader.Read(File.ReadAllText(Path.Combine(folder, file)));
            var board = Board(level); var local = LocalLayoutAnalyzer.Analyze(board); var graph = new DirectGraph(level);
            var edge = new HashSet<int>(level.Ships.Select((s, i) => new { s, i }).Where(v =>
                GridFootprint.Cells(v.s.Position, v.s.Direction, v.s.Length).Any(c => c.X < 2 || c.Y < 2 || c.X >= level.Width - 2 || c.Y >= level.Height - 2)).Select(v => v.i));
            var linked = edge.Count(i => graph.Blockers[i].Concat(graph.Dependents[i]).Any(j => !edge.Contains(j)));
            var curves = new JArray();
            for (var s = 0; s < 5; s++)
            { var order = graph.Order(s, out var curve); curves.Add(new JObject { ["strategy"] = Strategies[s], ["clearedByDirectExits"] = order.Count, ["first60MeanExits"] = curve.Take(Math.Max(1, (int)Math.Ceiling(level.Ships.Length * .6))).DefaultIfEmpty().Average() }); }
            levels.Add(new JObject { ["levelId"] = level.LevelId, ["fingerprint"] = LevelStateIdentity.Fingerprint(board),
                ["ships"] = board.ShipCount, ["pureExitComplete"] = LevelDifficultyAnalysis.Peel(board).IsComplete,
                ["localQuality"] = BridgeQuality(board), ["twoCellBoundaryShips"] = edge.Count,
                ["boundaryShipsLinkedToInterior"] = linked, ["boundaryInteriorLinkShare"] = edge.Count == 0 ? 0 : (double)linked / edge.Count,
                ["strategies"] = curves });
        }
        var reviews = new JArray();
        foreach (var id in new[] { "DV2_P4", "DV2_P8", "DV2_B80_T", "DV2_B80_R" })
        {
            var board = Board(LevelJsonReader.Read(File.ReadAllText(Path.Combine(output, id + ".json"))));
            var proof = LevelProofJson.Read(File.ReadAllText(Path.Combine(output, id + ".solution.json")));
            var checkpoints = new JArray(); var prefix = new List<string>(); JObject certifiedTrap = null;
            for (var step = 0; step < proof.Steps.Count; step++)
            {
                if (certifiedTrap == null)
                    foreach (var ship in board.Ships)
                    {
                        var move = board.QueryForwardPath(ship.Id);
                        if (move.IsBlocked && move.TravelDistance == 0) continue;
                        var after = board.ApplyPathResult(move); var dependencies = BoardDependencyAnalyzer.Analyze(after);
                        if (dependencies.HardLockedCycleCount == 0) continue;
                        var suffix = proof.Steps.Skip(step).Select(s => s.ShipId).ToArray();
                        LevelSolutionProof.Create(id, board, suffix);
                        certifiedTrap = new JObject { ["stepIndex"] = step, ["prefix"] = new JArray(prefix),
                            ["beforeFingerprint"] = LevelStateIdentity.Fingerprint(board), ["safeContinuation"] = new JArray(suffix),
                            ["wrongMove"] = ship.Id, ["afterFingerprint"] = LevelStateIdentity.Fingerprint(after),
                            ["deadlockMethod"] = "ClosedZeroTravelCycle", ["cycles"] = Evidence(dependencies.Cycles.Where(c => c.IsHardLocked)),
                            ["recovery"] = Evidence(LevelRiskAnalysis.AnalyzeRecovery(after, 2000, 100, 100, 4)) };
                        break;
                    }
                if (step == 0 || proof.Steps[step].Outcome == ForwardPathOutcome.Blocked)
                {
                    var report = LevelRiskAnalysis.Analyze(board, board.ShipCount <= 8 ? 10000 : 4000, board.ShipCount <= 8 ? 1000 : 100,
                        verifiedContinuation: proof.Steps.Skip(step).Select(s => s.ShipId).ToArray());
                    var checkpoint = new JObject { ["stepIndex"] = step, ["prefix"] = new JArray(prefix), ["risk"] = Evidence(report) };
                    var dangerous = report.Moves.FirstOrDefault(m => m.Status == BranchSolvability.Deadlocked);
                    if (dangerous != null)
                    {
                        var dead = board.ApplyPathResult(board.QueryForwardPath(dangerous.ShipId));
                        checkpoint["wrongMove"] = dangerous.ShipId;
                        checkpoint["recovery"] = Evidence(LevelRiskAnalysis.AnalyzeRecovery(dead, 1000, 100, 100, 4));
                    }
                    checkpoints.Add(checkpoint);
                }
                prefix.Add(proof.Steps[step].ShipId); board = board.ApplyPathResult(board.QueryForwardPath(proof.Steps[step].ShipId));
            }
            reviews.Add(new JObject { ["levelId"] = id, ["layoutFingerprint"] = proof.LayoutFingerprint, ["checkpoints"] = checkpoints,
                ["certifiedWrongMoveOnSolution"] = certifiedTrap });
            Console.WriteLine(id + " risk checkpoints=" + checkpoints.Count);
        }
        var summary = new JObject { ["campaignCount"] = levels.Count,
            ["pureExitCompleteCount"] = levels.Count(l => (bool)l["pureExitComplete"]),
            ["sameDirectionRunOver2Count"] = levels.Count(l => (int)l["localQuality"]["oneGapRun"] > 2),
            ["sameDirectionRunOver3Count"] = levels.Count(l => (int)l["localQuality"]["oneGapRun"] > 3) };
        var reportPath = Path.GetFullPath(Path.Combine(root, "../..", "40_项目交接文档/验证记录/20260927_错误分支与边缘审查"));
        Directory.CreateDirectory(reportPath);
        File.WriteAllText(Path.Combine(reportPath, "risk-audit.json"), new JObject { ["version"] = LevelRiskAnalysis.Version,
            ["rules"] = LevelRules.Version, ["scope"] = "Full campaign spatial/direct-exit audit; review risk only at listed states, bounded search. Unknown is not deadlock. No estimated human failure rate.",
            ["summary"] = summary, ["campaign"] = levels, ["reviewSamples"] = reviews }.ToString(Formatting.Indented) + "\n");
        Console.WriteLine(summary.ToString(Formatting.None));
    }
}
