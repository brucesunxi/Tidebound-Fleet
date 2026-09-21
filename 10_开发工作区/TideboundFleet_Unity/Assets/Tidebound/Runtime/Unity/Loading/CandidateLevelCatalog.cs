using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tidebound.Boss;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Config
{
    /// <summary>Strict development-only catalog. Candidate status never implies production approval.</summary>
    public sealed class CandidateLevelCatalog : IPlayableLevelCatalog
    {
        private readonly string[] layouts;
        public int Count => layouts.Length;
        public string GetLevelId(int index) => Phase5RLevelRecipes.All[index].LevelId;
        public bool IsAvailable(int index) => index >= 0 && index < Count;
        public LevelData Load(int index) => LevelJsonReader.Read(layouts[index]);
        public CandidateLevelCatalog(string manifestJson, IEnumerable<string> layoutJson, IEnumerable<string> proofJson)
        {
            var manifest = JObject.Parse(manifestJson);
            if ((int?)manifest["manifestVersion"] != 1 || (string)manifest["status"] != LevelCandidateJson.Status ||
                (string)manifest["rulesVersion"] != LevelRules.Version ||
                (string)manifest["recipeVersion"] != Phase5RLevelRecipes.Version ||
                (string)manifest["generatorVersion"] != RecipeLevelGenerator.Version ||
                (string)manifest["screeningVersion"] != Phase5RLevelRecipes.ScreeningVersion)
                throw new ArgumentException("Unsupported candidate manifest.");
            var inputs = layoutJson?.ToArray() ?? throw new ArgumentNullException(nameof(layoutJson));
            var proofs = proofJson?.ToArray() ?? throw new ArgumentNullException(nameof(proofJson));
            var rows = manifest["levels"] as JArray;
            if (inputs.Length != 10 || proofs.Length != 10 || rows?.Count != 10)
                throw new ArgumentException("The complete ten-candidate set is required.");
            var byId = inputs.ToDictionary(s => LevelJsonReader.Read(s).LevelId, StringComparer.Ordinal);
            var proofsById = proofs.ToDictionary(s => LevelProofJson.Read(s).LevelId, StringComparer.Ordinal);
            var rowsById = rows.ToDictionary(t => (string)t["levelId"], StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            layouts = new string[10];
            for (var i = 0; i < layouts.Length; i++)
            {
                var recipe = Phase5RLevelRecipes.All[i]; var id = recipe.LevelId;
                if (!byId.TryGetValue(id, out var json) || !proofsById.TryGetValue(id, out var proofText) || !rowsById.TryGetValue(id, out var row))
                    throw new ArgumentException("Missing candidate: " + id);
                var proof = LevelProofJson.Read(proofText); var sidecar = JObject.Parse(proofText);
                if ((string)sidecar["status"] != LevelCandidateJson.Status || (string)row["status"] != LevelCandidateJson.Status ||
                    (string)sidecar["generatorVersion"] != RecipeLevelGenerator.Version ||
                    (string)sidecar["recipeVersion"] != Phase5RLevelRecipes.Version ||
                    (string)sidecar["screeningVersion"] != Phase5RLevelRecipes.ScreeningVersion ||
                    (string)row["layoutFile"] != id + ".json" || (string)row["proofFile"] != id + ".solution.json")
                    throw new ArgumentException("Invalid candidate metadata: " + id);
                using (var session = LevelSessionFactory.Create(LevelJsonReader.Read(json),
                    new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                    new[] { new BossDefinition("TF_KRAKEN_01") }))
                {
                    if (recipe.Check(session.Board).Count != 0 || !proof.Replay(id, session.Board).IsComplete ||
                        (string)row["layoutFingerprint"] != proof.LayoutFingerprint ||
                        !keys.Add(LevelLayoutIdentity.CanonicalKey(session.Board)))
                        throw new ArgumentException("Invalid, stale or repeated candidate: " + id);
                }
                layouts[i] = json;
            }
        }
    }
}
