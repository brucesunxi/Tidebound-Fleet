using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class LevelDesignAnalysisTests
    {
        [Test]
        public void DependencyEdgesComeFromTheRealForwardPathQuery()
        {
            var board = Board(5, 4,
                Ship("A", 0, 1, ShipDirection.Right),
                Ship("B", 3, 0, ShipDirection.Up));

            var graph = BoardDependencyAnalyzer.Analyze(board);
            Assert.That(graph.GetNode("A").BlockerShipId, Is.EqualTo("B"));
            Assert.That(graph.GetNode("A").TravelDistance, Is.EqualTo(1));
            Assert.That(graph.GetNode("B").CanExit, Is.True);
            Assert.That(graph.InitialExitCount, Is.EqualTo(1));
            Assert.That(graph.InitialMoveCount, Is.EqualTo(1));
            Assert.That(graph.DependencyDepth, Is.EqualTo(1));
        }

        [Test]
        public void ClosedZeroTravelCycleIsReportedAsHardLockedAndExactSearchRejectsIt()
        {
            var board = HardLockedCycle();
            var graph = BoardDependencyAnalyzer.Analyze(board);
            Assert.That(graph.Cycles.Count, Is.EqualTo(1));
            Assert.That(graph.Cycles[0].ShipIds, Is.EquivalentTo(new[] { "A", "B", "C", "D" }));
            Assert.That(graph.Cycles[0].IsHardLocked, Is.True);

            var search = BoardSolvabilityAnalyzer.Search(board, 100);
            Assert.That(search.Status, Is.EqualTo(BoardSearchStatus.Deadlocked));
        }

        [Test]
        public void RecorderProofReplaysRealTransactionsAndFingerprintInvalidatesAfterLayoutChange()
        {
            var board = Board(5, 4,
                Ship("A", 0, 1, ShipDirection.Right),
                Ship("B", 3, 0, ShipDirection.Up));
            var recorder = new SolutionProofRecorder(board);
            Assert.That(recorder.TryApply("B", out var first), Is.True);
            Assert.That(first.CanExit, Is.True);
            Assert.That(recorder.TryApply("A", out var second), Is.True);
            Assert.That(second.CanExit, Is.True);

            var proof = recorder.CreateProof("ProofTest");
            var replay = SolutionProofReplay.Replay(board, proof);
            Assert.That(replay.IsComplete, Is.True);
            Assert.That(replay.AppliedStepCount, Is.EqualTo(2));

            var changed = Board(5, 4,
                Ship("A", 0, 2, ShipDirection.Right),
                Ship("B", 3, 0, ShipDirection.Up));
            var invalid = SolutionProofReplay.Replay(changed, proof);
            Assert.That(invalid.IsValid, Is.False);
            StringAssert.Contains("fingerprint", invalid.Error);
        }

        [Test]
        public void ExactSearchFindsTheAcyclicSolution()
        {
            var board = Board(5, 4,
                Ship("A", 0, 1, ShipDirection.Right),
                Ship("B", 3, 0, ShipDirection.Up));
            var result = BoardSolvabilityAnalyzer.Search(board, 100);
            Assert.That(result.Status, Is.EqualTo(BoardSearchStatus.Solved));
            CollectionAssert.AreEqual(new[] { "B", "A" }, result.ShipIds);
        }

        [Test]
        public void ProductionValidationRejectsSpatialDirectionBandsWithoutChangingRuntimeLegality()
        {
            var ships = new ShipPlacementData[16];
            var index = 0;
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
            {
                var direction = row < 2 ? ShipDirection.Right : ShipDirection.Left;
                var tailX = direction == ShipDirection.Right ? column * 2 : column * 2 + 1;
                ships[index] = Ship("S" + index++, tailX, row * 2, direction);
            }
            var board = Board(8, 8, ships);
            var profile = new LevelProductionProfile("StripedTest", 8, 8, 16, 16,
                0d, 0d, 0.40d, 1d, 1);
            var report = LevelStructureAnalyzer.Analyze(board);
            var result = LevelProductionValidator.Validate(board, profile);

            Assert.That(report.DirectionClustering, Is.GreaterThan(0.40d));
            Assert.That(result.Issues.Any(x => x.Code == "DIRECTION_CLUSTERED"), Is.True);
        }

        [Test]
        public void CandidateProfilesAreDataObjectsAndDoNotAffectTutorialRules()
        {
            Assert.That(LevelProductionProfiles.Candidates.Select(x => (x.Width, x.Height)),
                Is.EquivalentTo(new[] { (18, 18), (18, 22), (20, 20), (22, 22) }));
            Assert.That(LevelProductionProfiles.Tutorial.Width, Is.EqualTo(4));
            Assert.That(LevelProductionProfiles.Tutorial.Height, Is.EqualTo(6));
            Assert.That(LevelProductionProfiles.Tutorial.MinShipCount, Is.EqualTo(7));
        }

        private static BoardModel HardLockedCycle() => Board(6, 6,
            Ship("A", 1, 3, ShipDirection.Right),
            Ship("B", 3, 4, ShipDirection.Down),
            Ship("C", 4, 2, ShipDirection.Left),
            Ship("D", 2, 1, ShipDirection.Up));

        private static BoardModel Board(int width, int height, params ShipPlacementData[] ships)
        {
            using (var session = ShipMovementSystemTests.CreateSession(width, height, ships))
                return session.InitialBoard;
        }

        private static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction, int length = 2) =>
            new ShipPlacementData
            {
                Id = id,
                TypeId = "TF_BASE_SHIP",
                Length = length,
                Position = new GridPosition(x, y),
                Direction = direction
            };
    }
}
