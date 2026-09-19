using System;
using System.IO;
using System.Linq;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.EditorTools
{
    /// <summary>Small reproducible I1 export entry point, not the production level editor.</summary>
    public static class Phase5RAlgorithmExport
    {
        public const int SampleSeed = 20260919;
        public const string Folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R_Rebuild";

        [MenuItem("Tools/Tidebound/Export I1 Algorithm Samples")]
        public static void Export()
        {
            var tutorial = Generate("P5R_Rebuild_001", ReverseGenerationProfiles.Tutorial);
            var full = Generate("P5R_Rebuild_002", ReverseGenerationProfiles.FullBoard);
            // Generate and independently validate both before creating any output.
            Write(tutorial, ReverseGenerationProfiles.Tutorial);
            Write(full, ReverseGenerationProfiles.FullBoard);
            AssetDatabase.Refresh();
            Debug.Log("Tidebound I1: exported 7-ship and 80-ship algorithm prototypes; mobile/playable acceptance remains pending.");
        }

        private static LevelGenerationResult Generate(string id, ReverseGenerationProfile profile)
        {
            var result = ReverseLevelGenerator.Generate(id, profile, SampleSeed);
            if (result.Status != LevelGenerationStatus.Success)
                throw new InvalidOperationException(id + ": " + result.Status + ": " + result.Reason);
            var reloaded = LevelJsonReader.Read(LevelJsonWriter.Write(result.Level));
            using (var session = LevelSessionFactory.Create(reloaded,
                       new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                       new[] { new BossDefinition("TF_KRAKEN_01") }))
            {
                var independent = LevelSolver.Solve(session.InitialBoard);
                if (independent.Status != LevelSolverStatus.Solved || independent.ShipIds.Count != profile.ShipCount ||
                    !result.ConstructionProof.Replay(id, session.InitialBoard).IsComplete ||
                    !LevelProofJson.Read(LevelProofJson.WriteGenerated(result, profile, SampleSeed)).Replay(id, session.InitialBoard).IsComplete)
                    throw new InvalidOperationException(id + ": serialized replay failed.");
                Debug.Log(id + ": ships=" + profile.ShipCount + ", exits=" + result.Analysis.Dependencies.InitialExitCount +
                    ", depth=" + result.Analysis.Dependencies.CompleteDependencyDepth + ", backtracks=" + result.Backtracks);
            }
            return result;
        }

        private static void Write(LevelGenerationResult result, ReverseGenerationProfile profile)
        {
            var levelText = LevelJsonWriter.Write(result.Level);
            var proofText = LevelProofJson.WriteGenerated(result, profile, SampleSeed);
            Directory.CreateDirectory(Folder);
            var path = Path.Combine(Folder, result.Level.LevelId);
            // Never silently overwrite a hand-edited sample. Matching deterministic output is a no-op.
            var outputs = new[] { (path + ".json", levelText), (path + ".solution.json", proofText) };
            foreach (var item in outputs)
                if (File.Exists(item.Item1) && File.ReadAllText(item.Item1) != item.Item2)
                    throw new IOException("Sample already exists with different content: " + item.Item1);
            foreach (var item in outputs)
            {
                if (File.Exists(item.Item1)) continue;
                var temporary = item.Item1 + ".writing";
                try { File.WriteAllText(temporary, item.Item2); File.Move(temporary, item.Item1); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
    }
}
