using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class Phase5RCandidatePrototypeTests
    {
        private const string Folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R";

        [Test]
        public void TwelveFixedCandidatesPassRuntimeProductionAndProofValidation()
        {
            var paths = AssetDatabase.FindAssets("t:TextAsset", new[] { Folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => Path.GetFileName(x).StartsWith("P5R_", StringComparison.Ordinal) &&
                            x.EndsWith(".json", StringComparison.Ordinal) &&
                            !x.EndsWith(".solution.json", StringComparison.Ordinal))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.That(paths.Length, Is.EqualTo(12));

            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                Assert.That(asset, Is.Not.Null, path);
                var level = LevelJsonReader.Read(asset.text);
                Assert.That(LevelProductionProfiles.TryGetCandidate(level.Width, level.Height, out var profile),
                    Is.True, path);

                using (var session = LevelSessionFactory.Create(level,
                           new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                           new[] { new BossDefinition("TF_KRAKEN_01") }))
                {
                    var production = LevelProductionValidator.Validate(session.InitialBoard, profile);
                    Assert.That(production.IsValid, Is.True,
                        path + Environment.NewLine + string.Join(Environment.NewLine, production.Issues.Select(x => x.ToString())));

                    var proofPath = path.Substring(0, path.Length - ".json".Length) + ".solution.json";
                    var proofAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(proofPath);
                    Assert.That(proofAsset, Is.Not.Null, proofPath);
                    var json = JObject.Parse(proofAsset.text);
                    var proof = new SolutionProof(
                        (string)json["levelId"],
                        (string)json["layoutFingerprint"],
                        json["shipIds"].Values<string>());
                    Assert.That(proof.LayoutFingerprint, Is.EqualTo(BoardStateFingerprint.Compute(session.InitialBoard)), path);
                    Assert.That(proof.ShipIds.Count, Is.EqualTo(level.Ships.Length), path);
                    var replay = SolutionProofReplay.Replay(session.InitialBoard, proof);
                    Assert.That(replay.IsComplete, Is.True, path + Environment.NewLine + replay.Error);
                }
            }
        }

        [Test]
        public void CandidateSetCoversEachGridAndTargetCountExactlyOnce()
        {
            var manifest = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/Phase5R_PrototypeManifest.json");
            Assert.That(manifest, Is.Not.Null);
            var root = JObject.Parse(manifest.text);
            Assert.That((string)root["status"], Is.EqualTo("PrototypeOnly"));
            var levels = root["levels"].Children<JObject>().ToArray();
            Assert.That(levels.Length, Is.EqualTo(12));
            CollectionAssert.AreEquivalent(
                new[] { "18x18:80", "18x18:85", "18x18:90", "18x22:90", "18x22:95", "18x22:100",
                    "20x20:90", "20x20:95", "20x20:100", "22x22:100", "22x22:105", "22x22:110" },
                levels.Select(x => $"{(int)x["width"]}x{(int)x["height"]}:{(int)x["shipCount"]}").ToArray());
        }
    }
}
