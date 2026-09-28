using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Collection;
using Tidebound.Tools;

internal static class CampaignPipeline
{
    private static string root, folder, legacy;
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
    private static string Read(string file) => File.ReadAllText(Path.Combine(file.StartsWith("P5R_Ten_", StringComparison.Ordinal) ? legacy : folder, file));
    public static int Main(string[] args)
    {
        try
        {
            root = Path.GetFullPath(args[0]); var action = args[1]; var count = int.Parse(args[2]);
            if (count != 30 && count != 100) throw new ArgumentException("Only the 30 and 100 level gates are supported.");
            folder = Path.Combine(root, "Assets/Tidebound/Config/Levels/Campaign");
            legacy = Path.Combine(root, "Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            Directory.CreateDirectory(folder);
            if (action == "generate") Generate(count);
            else if (action == "validate") Validate(count);
            else throw new ArgumentException("Unknown action.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
    private static string SourceFingerprint()
    {
        var files = Directory.GetFiles(Path.Combine(root, "Assets/Tidebound/Runtime"), "*.cs", SearchOption.AllDirectories)
            .Where(p => p.Contains("/Core/") || p.Contains("/Data/") || p.Contains("/Loading/"))
            .Concat(new[] { Path.Combine(root, "Tools/Levels/CampaignPipeline.cs") }).OrderBy(p => p, StringComparer.Ordinal);
        return CampaignPackValidator.Sha256(string.Join("\n", files.Select(p => p.Substring(root.Length) + ":" + CampaignPackValidator.Sha256(File.ReadAllText(p)))));
    }
    private static void Atomic(string path, string text)
    {
        var temp = path + ".writing";
        File.WriteAllText(temp, text, Utf8);
        if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
    }
    private static void Generate(int count)
    {
        if (count == 100)
        {
            var gate = JObject.Parse(File.ReadAllText(Path.Combine(folder, "validation-30.json")));
            Require((string)gate["manifestSha256"] == CampaignPackValidator.Sha256(Read("manifest-30.json")) &&
                (bool?)gate["passed"] == true && (string)gate["sourceFingerprint"] == SourceFingerprint(),
                "Validate the saved first 30 levels with current sources before expanding.");
            CampaignPackValidator.Validate(Read("manifest-30.json"), Read);
        }
        var rows = new JArray(); var boards = new List<BoardModel>();
        var auditPath = Path.Combine(root, "Library/Tidebound/CampaignGeneration.jsonl");
        Directory.CreateDirectory(Path.GetDirectoryName(auditPath));
        foreach (var design in CampaignLevelPlan.All.Take(count))
        {
            var id = design.Recipe.LevelId; var layoutFile = id + ".json"; var proofFile = id + ".solution.json";
            var existing = design.Number <= 10 || File.Exists(Path.Combine(folder, layoutFile));
            if (!existing)
            {
                var selected = false;
                for (var attempt = 0; attempt < 16; attempt++)
                {
                    var seed = 24090000 + design.Number * 100 + attempt;
                    var started = DateTime.UtcNow;
                    var result = RecipeLevelGenerator.Generate(design.Recipe, seed,
                        new RecipeGenerationBudget(60000, 120000, new ReverseGenerationBudget(timeLimitMilliseconds: 30000)));
                    string rejection = result.Reason;
                    if (result.Status == LevelGenerationStatus.Success)
                    using (var session = CampaignPackValidator.Create(result.Generation.Level))
                    {
                        var issues = design.CheckStructure(session.Board);
                        rejection = issues.Count > 0 ? string.Join(",", issues) : boards.Any(b =>
                            CampaignPackValidator.GeometryOverlap(b, session.Board) > 0.65) ? "NEAR_DUPLICATE" : null;
                        if (rejection == null)
                        {
                            var proof = JObject.Parse(LevelCandidateJson.Write(result, CampaignLevelPlan.Version, CampaignLevelPlan.ScreeningVersion));
                            proof["campaignStructure"] = design.Structure.ToString(); proof["campaignPace"] = design.Pace.ToString();
                            proof["structureFeatures"] = JObject.FromObject(CampaignStructureFeatures.Measure(session.Board));
                            // Proof first: a stopped write cannot leave a layout without its evidence. Resume revalidates both.
                            Atomic(Path.Combine(folder, proofFile), proof.ToString(Formatting.Indented) + "\n");
                            Atomic(Path.Combine(folder, layoutFile), LevelJsonWriter.Write(result.Generation.Level));
                            selected = true;
                        }
                    }
                    var audit = new JObject { ["id"] = id, ["seed"] = seed, ["status"] = result.Status.ToString(),
                        ["rejection"] = rejection, ["seconds"] = (DateTime.UtcNow - started).TotalSeconds,
                        ["mutations"] = result.MutationsAttempted, ["accepted"] = selected };
                    File.AppendAllText(auditPath, audit.ToString(Formatting.None) + "\n", Utf8);
                    Console.WriteLine(audit.ToString(Formatting.None));
                    if (selected) break;
                }
                Require(selected, "Candidate budget exhausted: " + id + ". Earlier completed candidates are retained; no pack was published.");
            }
            var layoutText = Read(layoutFile); var proofText = Read(proofFile);
            using (var session = CampaignPackValidator.Create(LevelJsonReader.Read(layoutText)))
            {
                Require(design.Recipe.Check(session.Board).Count == 0 && design.CheckStructure(session.Board).Count == 0 &&
                    LevelProofJson.Read(proofText).Replay(id, session.Board).IsComplete, "Invalid existing candidate: " + id);
                Require(!boards.Any(b => CampaignPackValidator.GeometryOverlap(b, session.Board) > 0.65), "Repeated geometry: " + id);
                boards.Add(session.Board);
                rows.Add(new JObject { ["number"] = design.Number, ["levelId"] = id, ["contentRevision"] = 1,
                    ["structure"] = design.Structure.ToString(), ["pace"] = design.Pace.ToString(),
                    ["layoutFile"] = layoutFile, ["proofFile"] = proofFile,
                    ["layoutSha256"] = CampaignPackValidator.Sha256(layoutText), ["proofSha256"] = CampaignPackValidator.Sha256(proofText),
                    ["features"] = JObject.FromObject(CampaignStructureFeatures.Measure(session.Board)),
                    ["rewards"] = new JArray(AppearanceCatalog.All.Where(x => x.Source == ShowcaseSourceGroup.Level && x.Requirement == design.Number).Select(x => x.Id)
                        .Concat(ShowcaseCatalog.All.Where(x => x.SourceGroup == ShowcaseSourceGroup.Level && x.TargetClearLevel == design.Number).Select(x => x.Id))) });
            }
            Console.WriteLine("Ready " + design.Number + "/" + count + " " + id);
        }
        var manifest = new JObject { ["manifestVersion"] = 1, ["rulesVersion"] = LevelRules.Version,
            ["recipeVersion"] = CampaignLevelPlan.Version, ["status"] = CampaignPackValidator.Status,
            ["humanPlaytest"] = "Pending", ["deviceValidation"] = "Pending", ["levels"] = rows };
        var text = manifest.ToString(Formatting.Indented) + "\n";
        CampaignPackValidator.Validate(text, Read);
        Atomic(Path.Combine(folder, "manifest-" + count + ".json"), text);
        Console.WriteLine("Candidate pack exported; run validate " + count + " before scene integration or further expansion.");
    }
    private sealed class Store : IPlayerSaveStore
    {
        private PlayerSaveData data;
        public PlayerSaveData Load() => data?.Copy();
        public void Save(PlayerSaveData value) { value.Validate(); data = value.Copy(); }
    }
    private static void Validate(int count)
    {
        var manifestText = Read("manifest-" + count + ".json");
        CampaignPackValidator.Validate(manifestText, Read);
        var store = new Store(); var save = new PlayerSaveService(store);
        var results = new JArray(); var total = 0;
        foreach (var design in CampaignLevelPlan.All.Take(count))
        {
            var id = design.Recipe.LevelId; var level = LevelJsonReader.Read(Read(id + ".json"));
            var proof = JObject.Parse(Read(id + ".solution.json"));
            using (var session = CampaignPackValidator.Create(level))
            {
                var solved = LevelSolver.Solve(session.Board);
                Require(solved.Status == LevelSolverStatus.Solved, "Independent solver: " + id);
                // All stored strategies must clear through actual path transactions, not just metadata.
                foreach (var trace in proof["releaseTraces"])
                {
                    var board = session.Board; var step = 0;
                    foreach (var shipId in trace["shipIds"].Values<string>())
                    {
                        var before = new HashSet<string>(board.Ships.Where(s => board.QueryForwardPath(s.Id).CanExit).Select(s => s.Id));
                        Require(before.Count == (int)trace["availableBefore"][step], "Trace availability drift.");
                        Require(board.Ships.Count(s => { var p = board.QueryForwardPath(s.Id); return p.IsBlocked && p.TravelDistance > 0; }) ==
                            (int)trace["partialBefore"][step], "Partial move trace drift.");
                        var path = board.QueryForwardPath(shipId); Require(path.CanExit, "Invalid trace path.");
                        board = board.ApplyPathResult(path);
                        Require(board.Ships.Count(s => board.QueryForwardPath(s.Id).CanExit && !before.Contains(s.Id)) ==
                            (int)trace["newExits"][step++], "Unlock trace drift.");
                    }
                    Require(board.ShipCount == 0, "Incomplete trace.");
                }
            }
            if (design.Number == 3)
            {
                var request = Guid.NewGuid().ToString("N");
                Require(save.CollectAppearance(request, "FirstBlue") == CollectionStatus.Saved, "First blue claim failed.");
                Require(save.CollectAppearance(request, "FirstBlue") == CollectionStatus.AlreadySaved, "First blue receipt not idempotent.");
                Require(save.SelectAppearance(save.AppearanceReceiptFor(request).SkinIds[0]) == AppearanceSelectionStatus.Saved, "First blue equip failed.");
            }
            using (var game = save.CreateNextAttempt(level))
            {
                Require(save.Start(game), "Cannot start campaign level " + design.Number);
                save.Inventory.ReachLevel(design.Number);
                var gifts = save.Snapshot.Tools.Receipts.Length;
                save.Inventory.ReachLevel(design.Number);
                Require(save.Snapshot.Tools.Receipts.Length == gifts, "Repeated milestone gift.");
                var steps = proof["solutionSteps"].ToArray();
                for (var i = 0; i < steps.Length; i++)
                {
                    var shipId = (string)steps[i]["shipId"];
                    Require(save.PrepareMove(shipId), "Move intent not saved.");
                    var move = game.Movement.TryBeginMove(shipId); Require(move.IsAccepted && move.Operation.WillExit, "Move rejected.");
                    game.Movement.CompleteTravel(move.Operation.OperationId);
                    game.Transit.Advance(0.07); game.Combat.Advance();
                    if (i == steps.Length / 2)
                    {
                        Require(save.Checkpoint(true), "Mid-level save failed.");
                        using (var restored = SavedGameRuntime.Restore(store.Load().Attempt))
                        {
                            Require(LevelStateIdentity.Fingerprint(restored.Session.Board) == LevelStateIdentity.Fingerprint(game.Session.Board), "Restore changed layout.");
                            Require(restored.Combat.HitCount == game.Combat.HitCount && restored.Session.SessionId == game.Session.SessionId, "Restore changed combat/attempt.");
                            foreach (var remaining in steps.Skip(i + 1))
                            {
                                var request = restored.Movement.TryBeginMove((string)remaining["shipId"]);
                                Require(request.IsAccepted, "Restored continuation rejected.");
                                restored.Movement.CompleteTravel(request.Operation.OperationId);
                            }
                            restored.Transit.Advance(100); restored.Combat.Advance();
                            Require(restored.Combat.IsVictorious && restored.Combat.HitCount == level.Ships.Length, "Restored continuation failed.");
                        }
                    }
                }
                game.Transit.Advance(100); game.Combat.Advance();
                Require(game.Combat.IsVictorious && game.Combat.HitCount == level.Ships.Length, "Combat failed: " + id);
                Require(save.Checkpoint(true), "Victory save failed."); var coins = save.Coins;
                Require(save.Checkpoint(true) && save.Coins == coins && save.CurrentLevel == design.Number + 1, "Duplicate settlement/progress.");
                Require(save.Snapshot.HighestClearedLevel == design.Number, "Highest clear mismatch.");
                foreach (var reward in AppearanceCatalog.All.Where(x => x.Source == ShowcaseSourceGroup.Level))
                    Require(save.OwnsAppearance(reward.Id) == (reward.Requirement <= design.Number), "Appearance entitlement mismatch: " + reward.Id);
                foreach (var reward in ShowcaseCatalog.All.Where(x => x.SourceGroup == ShowcaseSourceGroup.Level))
                    Require(save.OwnsShowcase(reward.Id) == (reward.TargetClearLevel <= design.Number), "Showcase entitlement mismatch: " + reward.Id);
                total += level.Ships.Length;
                results.Add(new JObject { ["level"] = design.Number, ["id"] = id, ["ships"] = level.Ships.Length,
                    ["hits"] = game.Combat.HitCount, ["wallet"] = save.Coins, ["passed"] = true });
            }
            save = new PlayerSaveService(store);
            Require(save.IsAvailable && save.CurrentLevel == design.Number + 1, "Progress reload failed.");
            Console.WriteLine("Validated " + design.Number + "/" + count + " wallet=" + save.Coins);
        }
        var report = new JObject { ["passed"] = true, ["count"] = count, ["manifestSha256"] = CampaignPackValidator.Sha256(manifestText),
            ["sourceFingerprint"] = SourceFingerprint(),
            ["scope"] = "CSharp production model: recipes, structure, geometry, independent solver, 3 traces, movement/transit/combat, collected allocation, save/restore, exactly-once settlement, sequential progression",
            ["humanPlaytest"] = "Pending", ["deviceValidation"] = "Pending", ["totalShips"] = total, ["levels"] = results };
        Atomic(Path.Combine(folder, "validation-" + count + ".json"), report.ToString(Formatting.Indented) + "\n");
        Console.WriteLine("PASS " + count + " levels, " + total + " ships.");
    }
}
