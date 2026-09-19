using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class TenLevelCandidateTests
    {
        private const string Folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates/";
        private static IEnumerable<int> Levels => Enumerable.Range(0, 10);
        private static IEnumerable<TestCaseData> Traces => Levels.SelectMany(i => Enumerable.Range(0, 3)
            .Select(t => new TestCaseData(i, t).SetName("Candidate_" + (i + 1) + "_MovementTrace_" + t)));
        private static string Read(string file) => AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + file).text;
        private static LevelData Load(int index) => LevelJsonReader.Read(Read(Phase5RLevelRecipes.All[index].LevelId + ".json"));

        [TestCaseSource(nameof(Levels))]
        public void SavedCandidateMatchesRecipeAndEveryReverseInsertionPrefix(int index)
        {
            var recipe = Phase5RLevelRecipes.All[index];
            var level = Load(index);
            var sidecar = JObject.Parse(Read(recipe.LevelId + ".solution.json"));
            Assert.That((string)sidecar["status"], Is.EqualTo(LevelCandidateJson.Status));
            Assert.That((string)sidecar["generatorVersion"], Is.EqualTo(RecipeLevelGenerator.Version));
            Assert.That((string)sidecar["recipeVersion"], Is.EqualTo(Phase5RLevelRecipes.Version));
            using (var session = ReverseLevelGeneratorTests.Create(level))
            {
                Assert.That(recipe.Check(session.Board), Is.Empty);
                Assert.That(LevelProofJson.Read(sidecar.ToString()).Replay(level.LevelId, session.Board).IsComplete, Is.True);
                Assert.That(LevelSolutionProof.Create(level.LevelId, session.Board,
                    sidecar["constructionOrder"].Values<string>()).Replay(level.LevelId, session.Board).IsComplete, Is.True);
                var solved = LevelSolver.Solve(session.Board);
                Assert.That(solved.Status, Is.EqualTo(LevelSolverStatus.Solved));
                Assert.That(solved.Optimality, Is.EqualTo(SolutionOptimality.Proven));
                Assert.That(solved.ShipIds.Count, Is.EqualTo(recipe.Profile.ShipCount));
                var local = LocalLayoutAnalyzer.Analyze(session.Board, new LocalLayoutOptions(recipe.Profile.Area));
                Assert.That((int)sidecar["localMetrics"]["longestRunShips"], Is.EqualTo(LevelRecipe.LongestRun(local)));
                Assert.That((double)sidecar["localMetrics"]["minimumWindowEntropy"], Is.EqualTo(local.MinimumWindowEntropy));
            }
            for (var n = 1; n <= level.Ships.Length; n++)
            {
                var prefix = new LevelData { SchemaVersion = 2, LevelId = level.LevelId, Width = level.Width,
                    Height = level.Height, BossId = level.BossId, Ships = level.Ships.Take(n).ToArray() };
                using (var session = ReverseLevelGeneratorTests.Create(prefix))
                    Assert.That(session.Board.QueryForwardPath(prefix.Ships[n - 1].Id).CanExit, Is.True, "prefix " + n);
            }
        }

        [TestCaseSource(nameof(Traces))]
        public void RecordedReleaseTraceMatchesActualMovementAndTransit(int index, int traceIndex)
        {
            var level = Load(index);
            var trace = JObject.Parse(Read(level.LevelId + ".solution.json"))["releaseTraces"][traceIndex];
            using (var session = ReverseLevelGeneratorTests.Create(level))
            using (var transit = new TransitSystem(session))
            {
                var exited = new List<string>(); var entered = new List<string>();
                session.Events.Subscribe<ShipExitBoardEvent>(e => exited.Add(e.Ship.ShipId));
                session.Events.Subscribe<ShipEnterFleetEvent>(e => entered.Add(e.Ship.ShipId));
                var movement = new ShipMovementSystem(session); movement.StartPlaying();
                var step = 0;
                foreach (var id in trace["shipIds"].Values<string>())
                {
                    var before = new HashSet<string>(session.Board.Ships.Where(s => session.Board.QueryForwardPath(s.Id).CanExit).Select(s => s.Id));
                    Assert.That((int)trace["availableBefore"][step], Is.EqualTo(before.Count));
                    Assert.That((int)trace["partialBefore"][step], Is.EqualTo(session.Board.Ships.Count(s =>
                    { var path = session.Board.QueryForwardPath(s.Id); return path.IsBlocked && path.TravelDistance > 0; })));
                    var move = movement.TryBeginMove(id);
                    Assert.That(move.IsAccepted, Is.True); Assert.That(move.Operation.WillExit, Is.True);
                    Assert.That(movement.CompleteTravel(move.Operation.OperationId), Is.EqualTo(ShipMoveAdvanceStatus.Applied));
                    Assert.That((int)trace["newExits"][step], Is.EqualTo(session.Board.Ships.Count(s =>
                        session.Board.QueryForwardPath(s.Id).CanExit && !before.Contains(s.Id))));
                    step++;
                }
                transit.Advance(100);
                Assert.That(session.Board.ShipCount, Is.Zero); Assert.That(transit.IsEmpty, Is.True);
                Assert.That(session.Ships.All(s => s.State == ShipState.InFleet), Is.True);
                Assert.That(exited.Distinct().Count(), Is.EqualTo(level.Ships.Length));
                CollectionAssert.AreEqual(exited, entered);
            }
        }

        [Test]
        public void BatchHasTenDistinctLayoutsAndCompleteManifest()
        {
            var manifest = JObject.Parse(Read("manifest.json"));
            CollectionAssert.AreEquivalent(Phase5RLevelRecipes.All.Select(r => r.LevelId), manifest["levels"].Select(r => (string)r["levelId"]));
            var keys = new HashSet<string>();
            foreach (var i in Levels)
            using (var session = ReverseLevelGeneratorTests.Create(Load(i)))
                Assert.That(keys.Add(LevelLayoutIdentity.CanonicalKey(session.Board)), Is.True);
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 4, 4, 4, 6, 6, 6, 8 }, Phase5RLevelRecipes.All.Select(r => r.Profile.LongShipCount));
        }

        [TestCase(0)]
        [TestCase(9)]
        public void FixedSeedReproducesSavedLayoutAndAllProofMetadata(int index)
        {
            var recipe = Phase5RLevelRecipes.All[index];
            var result = RecipeLevelGenerator.Generate(recipe, 20260919);
            Assert.That(result.Status, Is.EqualTo(LevelGenerationStatus.Success), result.Reason);
            Assert.That(LevelJsonWriter.Write(result.Generation.Level), Is.EqualTo(Read(recipe.LevelId + ".json")));
            Assert.That(LevelCandidateJson.Write(result), Is.EqualTo(Read(recipe.LevelId + ".solution.json")));
        }

        [Test]
        public void ExhaustedBudgetReturnsNoPartialCandidateOrRelaxedRecipe()
        {
            var result = RecipeLevelGenerator.Generate(Phase5RLevelRecipes.All[9], 20260919, new RecipeGenerationBudget(maxMutations: 0));
            Assert.That(result.Status, Is.EqualTo(LevelGenerationStatus.BudgetExceeded));
            Assert.That(result.Generation, Is.Null);
            Assert.Throws<ArgumentException>(() => LevelCandidateJson.Write(result));
            Assert.Throws<ArgumentException>(() => LevelCandidateJson.WriteManifest(Array.Empty<RecipeGenerationResult>()));
        }

        [Test]
        public void InvalidRecipeNeverBeginsGeneration()
        {
            var invalid = new LevelRecipe("invalid", ReverseGenerationProfiles.Tutorial, 3, double.NaN, 0.4, 16);
            Assert.That(RecipeLevelGenerator.Generate(invalid, 1).Status, Is.EqualTo(LevelGenerationStatus.InvalidProfile));
            Assert.That(RecipeLevelGenerator.Generate(null, 1).Generation, Is.Null);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void GeometryIdentityIgnoresIdsOrderAndAxisSymmetry(int mask)
        {
            var level = Load(9);
            using (var original = ReverseLevelGeneratorTests.Create(level))
            {
                var key = LevelLayoutIdentity.CanonicalKey(original.Board);
                foreach (var s in level.Ships)
                {
                    s.Id = "renamed_" + s.Id;
                    s.Position = new GridPosition((mask & 1) != 0 ? level.Width - 1 - s.Position.X : s.Position.X,
                        (mask & 2) != 0 ? level.Height - 1 - s.Position.Y : s.Position.Y);
                    if ((mask & 1) != 0 && s.Direction == ShipDirection.Left) s.Direction = ShipDirection.Right;
                    else if ((mask & 1) != 0 && s.Direction == ShipDirection.Right) s.Direction = ShipDirection.Left;
                    if ((mask & 2) != 0 && s.Direction == ShipDirection.Up) s.Direction = ShipDirection.Down;
                    else if ((mask & 2) != 0 && s.Direction == ShipDirection.Down) s.Direction = ShipDirection.Up;
                }
                level.Ships = level.Ships.Reverse().ToArray();
                using (var changed = ReverseLevelGeneratorTests.Create(level))
                    Assert.That(LevelLayoutIdentity.CanonicalKey(changed.Board), Is.EqualTo(key));
                level.Width++;
                using (var changed = ReverseLevelGeneratorTests.Create(level))
                    Assert.That(LevelLayoutIdentity.CanonicalKey(changed.Board), Is.Not.EqualTo(key));
            }
        }
    }
}
