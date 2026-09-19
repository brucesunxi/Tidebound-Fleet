using System;
using System.Linq;
using NUnit.Framework;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class ReverseLevelGeneratorTests
    {
        private const int Seed = 20260919;

        [TestCase(7)]
        [TestCase(80)]
        public void ReverseInsertionProducesRequestedStructureAndIndependentOptimalProofs(int count)
        {
            var profile = count == 7 ? ReverseGenerationProfiles.Tutorial : ReverseGenerationProfiles.FullBoard;
            var result = Generate(profile);
            Assert.That(result.Level.Ships.Length, Is.EqualTo(count));
            Assert.That(result.Level.Width, Is.EqualTo(14));
            Assert.That(result.Level.Height, Is.EqualTo(18));
            Assert.That(result.Analysis.Dependencies.InitialExitCount, Is.InRange(profile.MinInitialExits, profile.MaxInitialExits));
            Assert.That(result.Analysis.Dependencies.CompleteDependencyDepth, Is.InRange(profile.MinDependencyDepth, profile.MaxDependencyDepth));
            Assert.That(result.Analysis.Dependencies.CompleteCycles, Is.Empty);
            Assert.That(result.Analysis.DirectionEntropy, Is.GreaterThanOrEqualTo(profile.MinDirectionEntropy));
            Assert.That(result.Analysis.DirectionClustering, Is.LessThanOrEqualTo(profile.MaxDirectionClustering));
            Assert.That(result.IndependentSeedCount, Is.LessThanOrEqualTo(profile.MaxIndependentSeeds));
            Assert.That(result.Solver.Optimality, Is.EqualTo(SolutionOptimality.Proven));
            var reloaded = LevelJsonReader.Read(LevelJsonWriter.Write(result.Level));
            using (var session = Create(reloaded))
            {
                Assert.That(result.ConstructionProof.Replay("Generated", session.Board).IsComplete, Is.True);
                Assert.That(result.SolverProof.Replay("Generated", session.Board).IsComplete, Is.True);
                var separate = LevelSolver.Solve(reloaded,
                    new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, 10) }, new[] { new BossDefinition("TF_KRAKEN_01") });
                Assert.That(separate.Status, Is.EqualTo(LevelSolverStatus.Solved));
                Assert.That(separate.ShipIds.Count, Is.EqualTo(count));
                var sidecar = LevelProofJson.WriteGenerated(result, profile, Seed);
                Assert.That(LevelProofJson.Read(sidecar).Replay("Generated", session.Board).IsComplete, Is.True);
            }
            // Rebuild every prefix independently, so a generator cannot fake a reversed witness.
            for (var i = 1; i <= count; i++)
            {
                var prefix = new LevelData { SchemaVersion = 2, LevelId = "Prefix", Width = 14, Height = 18,
                    BossId = "TF_KRAKEN_01", Ships = result.Level.Ships.Take(i).ToArray() };
                using (var session = Create(prefix))
                {
                    Assert.That(session.Board.QueryForwardPath(prefix.Ships[i - 1].Id).CanExit, Is.True, "Insertion " + i);
                    foreach (var ship in session.Board.Ships)
                    foreach (var cell in ship.OccupiedCells)
                        Assert.That(profile.Area.Contains(cell.X, cell.Y), Is.True);
                }
            }
        }

        [Test]
        public void SameSeedReproducesLayoutWitnessAndMetadata()
        {
            var profile = ReverseGenerationProfiles.FullBoard;
            var a = Generate(profile);
            var b = Generate(profile);
            Assert.That(LevelJsonWriter.Write(b.Level), Is.EqualTo(LevelJsonWriter.Write(a.Level)));
            Assert.That(LevelProofJson.WriteGenerated(b, profile, Seed), Is.EqualTo(LevelProofJson.WriteGenerated(a, profile, Seed)));
        }

        [TestCase(20260920)]
        [TestCase(20260921)]
        public void AdditionalSeedsEitherReturnCertifiedLevelsOrExplicitFailureWithoutPartialOutput(int seed)
        {
            var profile = ReverseGenerationProfiles.FullBoard;
            var result = ReverseLevelGenerator.Generate("SeedCheck", profile, seed);
            if (result.Status != LevelGenerationStatus.Success)
            {
                Assert.That(result.Status, Is.EqualTo(LevelGenerationStatus.BudgetExceeded).Or.EqualTo(LevelGenerationStatus.GenerationFailed));
                Assert.That(result.Level, Is.Null);
                Assert.That(result.SolverProof, Is.Null);
                Assert.That(result.Reason, Is.Not.Empty);
                return;
            }
            using (var session = Create(result.Level))
            {
                Assert.That(result.SolverProof.Replay("SeedCheck", session.Board).IsComplete, Is.True);
                Assert.That(result.Solver.ShipIds.Count, Is.EqualTo(80));
            }
        }

        [Test]
        public void LengthThreeQuotaIsAuthoredAndNotSilentlyReplacedToMakeItFit()
        {
            var profile = new ReverseGenerationProfile("LongShips", 14, 18, 20, 2, 4, 6, 3, 6, 10,
                0.80, 0.85, 0.50, 0);
            var result = Generate(profile);
            Assert.That(result.Level.Ships.Count(x => x.Length == 3), Is.EqualTo(2));
            Assert.That(result.Level.Ships.All(x => x.Length == 2 || x.Length == 3), Is.True);
            using (var session = Create(result.Level))
                Assert.That(result.SolverProof.Replay("Generated", session.Board).IsComplete, Is.True);
        }

        [Test]
        public void ImpossibleCapacityAndBudgetExhaustionNeverReturnATruncatedLevel()
        {
            var impossible = new ReverseGenerationProfile("TooDense", 3, 3, 7, 0, 1, 3, 2, 4, 4);
            var failed = ReverseLevelGenerator.Generate("Impossible", impossible, Seed);
            Assert.That(failed.Status, Is.EqualTo(LevelGenerationStatus.GenerationFailed));
            Assert.That(failed.Level, Is.Null);
            var limited = ReverseLevelGenerator.Generate("Limited", ReverseGenerationProfiles.FullBoard, Seed,
                new ReverseGenerationBudget(maxCandidateEvaluations: 1));
            Assert.That(limited.Status, Is.EqualTo(LevelGenerationStatus.BudgetExceeded));
            Assert.That(limited.CandidateEvaluations, Is.EqualTo(1));
            Assert.That(limited.Level, Is.Null);
            Assert.That(limited.ConstructionProof, Is.Null);
        }

        [Test]
        public void RejectedStructureUsesBacktrackingBudgetInsteadOfRelaxingTheProfile()
        {
            // Seven ships cannot be equally divided into four directions (normalized entropy 1).
            var impossibleGate = new ReverseGenerationProfile("ExactEntropy", 14, 18, 7, 0, 2, 3, 3, 4, 4,
                1.0, 0.60, 0.43, 1, new GenerationArea(4, 5, 6, 8));
            var result = ReverseLevelGenerator.Generate("NoRelaxation", impossibleGate, Seed,
                new ReverseGenerationBudget(maxBacktracks: 0));
            Assert.That(result.Status, Is.EqualTo(LevelGenerationStatus.BudgetExceeded));
            Assert.That(result.Reason, Does.Contain("Backtrack"));
            Assert.That(result.Level, Is.Null);
        }

        [Test]
        public void InvalidProfileAndNonfiniteThresholdsAreRejectedBeforeAllocation()
        {
            Assert.That(ReverseLevelGenerator.Generate("L", null, Seed).Status, Is.EqualTo(LevelGenerationStatus.InvalidProfile));
            var invalid = new ReverseGenerationProfile("Bad", int.MaxValue, 18, 80, 0, 12, 16, 5, 8, 26);
            Assert.That(ReverseLevelGenerator.Generate("L", invalid, Seed).Status, Is.EqualTo(LevelGenerationStatus.InvalidProfile));
            var nan = new ReverseGenerationProfile("Bad", 14, 18, 80, 0, 12, 16, 5, 8, 26, double.NaN);
            Assert.That(ReverseLevelGenerator.Generate("L", nan, Seed).Status, Is.EqualTo(LevelGenerationStatus.InvalidProfile));
        }

        private static LevelGenerationResult Generate(ReverseGenerationProfile profile)
        {
            var result = ReverseLevelGenerator.Generate("Generated", profile, Seed);
            Assert.That(result.Status, Is.EqualTo(LevelGenerationStatus.Success),
                result.Reason + "; deepest=" + result.DeepestShipCount + "; evaluations=" + result.CandidateEvaluations);
            return result;
        }
        internal static GameSession Create(LevelData level) => LevelSessionFactory.Create(level,
            new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, 10) }, new[] { new BossDefinition("TF_KRAKEN_01") });
    }
}
