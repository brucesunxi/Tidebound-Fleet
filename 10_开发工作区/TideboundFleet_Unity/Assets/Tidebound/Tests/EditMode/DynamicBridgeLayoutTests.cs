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
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class DynamicBridgeLayoutTests
    {
        [TestCase("DV2_B80_T", 81)]
        [TestCase("DV2_B80_R", 82)]
        public void ReviewBridgeMixesLocalDirectionsAndKeepsVerifiedDynamicSolution(string id, int expectedSteps)
        {
            var folder = Path.Combine(Application.dataPath, "Tidebound/Config/LevelPrototypes/DifficultyV2");
            var level = LevelJsonReader.Read(File.ReadAllText(Path.Combine(folder, id + ".json")));
            using (var session = LevelSessionFactory.Create(level,
                new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                new[] { new BossDefinition("TF_KRAKEN_01") }))
            {
                var board = session.InitialBoard; var local = LocalLayoutAnalyzer.Analyze(board);
                Assert.That(board.ShipCount, Is.EqualTo(80));
                Assert.That(board.Ships.Count(s => s.Length == 3), Is.EqualTo(6));
                Assert.That(Math.Max(local.LongestGappedRow.ShipCount, local.LongestGappedColumn.ShipCount), Is.LessThanOrEqualTo(2),
                    "Three same-direction ships in a row/column, even with one-cell gaps, are rejected.");
                Assert.That(local.LargestEmptyArea, Is.LessThanOrEqualTo(6));
                Assert.That(local.MinimumWindowEntropy, Is.GreaterThanOrEqualTo(.65));
                Assert.That(local.P10WindowEntropy, Is.GreaterThanOrEqualTo(.75));
                Assert.That(board.Ships.Count(s => board.QueryForwardPath(s.Id).CanExit), Is.InRange(4, 8));
                Assert.That(LevelDifficultyAnalysis.Peel(board).IsComplete, Is.False, "A visual improvement cannot discard the necessary partial advance.");
                var proof = LevelProofJson.Read(File.ReadAllText(Path.Combine(folder, id + ".solution.json")));
                Assert.That(proof.Steps.Count, Is.EqualTo(expectedSteps));
                Assert.That(proof.Replay(id, board).IsComplete, Is.True);
                var risk = LevelRiskAnalysis.Analyze(board, 1000, 25, verifiedContinuation: proof.Steps.Select(s => s.ShipId).ToArray());
                Assert.That(risk.Moves.Count, Is.GreaterThan(0), "An early exit that invalidates a repeated-ID witness must fall back to search, not throw.");
                Assert.That(risk.StateFingerprint, Is.EqualTo(proof.LayoutFingerprint));
                var report = JObject.Parse(File.ReadAllText(Path.Combine(folder, id + ".analysis.json")));
                Assert.That((string)report["bridgeRevision"], Is.EqualTo("MixedBridgeV2"));
                Assert.That((string)report["fingerprint"], Is.EqualTo(LevelStateIdentity.Fingerprint(board)));
                Assert.That((int)report["localQuality"]["largestEmptyRectangle"], Is.EqualTo(local.LargestEmptyArea));
            }
        }
    }
}
