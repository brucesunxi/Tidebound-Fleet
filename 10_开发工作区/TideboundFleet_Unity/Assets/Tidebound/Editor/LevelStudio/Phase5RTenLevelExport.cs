using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static class Phase5RTenLevelExport
    {
        public const int BaseSeed = 20260919;
        public const string Folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates";
        private const int SeedsPerRecipe = 8;

        [MenuItem("Tools/Tidebound/Generate Ten Recipe Candidates")]
        public static void Export()
        {
            var outputs = new Dictionary<string, string>(StringComparer.Ordinal);
            var candidates = new List<RecipeGenerationResult>();
            var geometry = new HashSet<string>(StringComparer.Ordinal);
            var attempts = new StringBuilder("levelId,seed,status,mutationsAttempted,mutationsAccepted,bestPenalty,reason\n");
            var reportPath = Path.Combine(Path.GetTempPath(), "TideboundI2b_GenerationAttempts.csv");
            try
            {
                foreach (var recipe in Phase5RLevelRecipes.All)
                {
                    RecipeGenerationResult selected = null;
                    for (var attempt = 0; attempt < SeedsPerRecipe; attempt++)
                    {
                        if (!Application.isBatchMode && EditorUtility.DisplayCancelableProgressBar("Tidebound candidates",
                            recipe.LevelId + " seed " + (BaseSeed + attempt), candidates.Count / 10f))
                            throw new OperationCanceledException("Candidate export cancelled; existing assets remain unchanged.");
                        var result = RecipeLevelGenerator.Generate(recipe, BaseSeed + attempt);
                        attempts.AppendLine(string.Join(",", recipe.LevelId, result.Seed.ToString(CultureInfo.InvariantCulture),
                            result.Status.ToString(), result.MutationsAttempted.ToString(CultureInfo.InvariantCulture),
                            result.MutationsAccepted.ToString(CultureInfo.InvariantCulture),
                            result.BestPenalty.ToString("R", CultureInfo.InvariantCulture), Csv(result.Reason)));
                        if (result.Status != LevelGenerationStatus.Success) continue;
                        // Verify serialized data and current sidecar, rather than trusting in-memory metadata.
                        var layout = LevelJsonWriter.Write(result.Generation.Level);
                        var proof = LevelCandidateJson.Write(result);
                        using (var session = LevelSessionFactory.Create(LevelJsonReader.Read(layout),
                            new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                            new[] { new BossDefinition("TF_KRAKEN_01") }))
                        {
                            var board = session.InitialBoard;
                            if (!LevelProofJson.Read(proof).Replay(recipe.LevelId, board).IsComplete ||
                                LevelSolver.Solve(board).Status != LevelSolverStatus.Solved || recipe.Check(board).Count != 0)
                                throw new InvalidOperationException("Serialized candidate validation failed: " + recipe.LevelId);
                            if (!geometry.Add(LevelLayoutIdentity.CanonicalKey(board)))
                            {
                                attempts.AppendLine(recipe.LevelId + "," + result.Seed + ",DuplicateGeometry,0,0,0,\"Exact or symmetric repeat\"");
                                continue;
                            }
                        }
                        selected = result;
                        outputs.Add(recipe.LevelId + ".json", layout);
                        outputs.Add(recipe.LevelId + ".solution.json", proof);
                        break;
                    }
                    if (selected == null)
                        throw new InvalidOperationException("No candidate for " + recipe.LevelId + "; no partial set is exported. See " + reportPath);
                    candidates.Add(selected);
                    Debug.Log(recipe.LevelId + ": certified candidate; mutations=" + selected.MutationsAttempted);
                }
                outputs.Add("manifest.json", LevelCandidateJson.WriteManifest(candidates));
                SaveCompleteSet(outputs);
                AssetDatabase.Refresh();
                Debug.Log("Tidebound: 10/10 recipe candidates exported. Human/device and animated-scene acceptance remain pending.");
            }
            finally
            {
                File.WriteAllText(reportPath, attempts.ToString(), new UTF8Encoding(false));
                if (!Application.isBatchMode) EditorUtility.ClearProgressBar();
                Debug.Log("Tidebound generation attempts: " + reportPath);
            }
        }

        private static void SaveCompleteSet(IReadOnlyDictionary<string, string> outputs)
        {
            var destination = Path.GetFullPath(Path.Combine(Application.dataPath, "..", Folder));
            if (Directory.Exists(destination))
            {
                foreach (var output in outputs)
                {
                    var path = Path.Combine(destination, output.Key);
                    if (!File.Exists(path) || File.ReadAllText(path) != output.Value)
                        throw new IOException("Existing candidate set differs; preserve it and export a new revision: " + path);
                }
                if (Directory.GetFiles(destination).Any(p => !p.EndsWith(".meta", StringComparison.Ordinal) && !outputs.ContainsKey(Path.GetFileName(p))) ||
                    Directory.GetDirectories(destination).Length != 0)
                    throw new IOException("Existing candidate directory contains unexpected files or subdirectories.");
                return;
            }
            var staging = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/TideboundTen-" + Guid.NewGuid().ToString("N")));
            try
            {
                Directory.CreateDirectory(staging);
                foreach (var output in outputs) File.WriteAllText(Path.Combine(staging, output.Key), output.Value, new UTF8Encoding(false));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                // Project-local Temp and Assets share the project volume: publish the fully written directory at once.
                Directory.Move(staging, destination);
            }
            finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        }

        private static string Csv(string text) => "\"" + (text ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }
}
