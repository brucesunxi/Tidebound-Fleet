using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class LevelDifficultyAnalysisTests
    {
        internal static BoardModel Prototype(bool eight = false)
        {
            var ships = new[] {
                LevelSolverTests.Ship("A", 1, 1, ShipDirection.Up),
                LevelSolverTests.Ship("B", 0, 4, ShipDirection.Right),
                LevelSolverTests.Ship("C", 4, 5, ShipDirection.Down),
                LevelSolverTests.Ship("D", 5, 1, ShipDirection.Left),
                LevelSolverTests.Ship("E", 3, 1, ShipDirection.Left),
                LevelSolverTests.Ship("F", 2, 0, ShipDirection.Left),
                LevelSolverTests.Ship("G", 2, 4, ShipDirection.Down),
                LevelSolverTests.Ship("H", 5, 2, ShipDirection.Left) };
            return LevelSolverTests.Board(6, 6, ships.Take(eight ? 8 : 4).ToArray());
        }

        [TestCase(false, 1, 5)]
        [TestCase(true, 2, 10)]
        public void ShortestProofGivesExactNecessaryPartialCount(bool eight, int partial, int actions)
        {
            var board = Prototype(eight); var hash = LevelStateIdentity.Fingerprint(board);
            var report = LevelDifficultyAnalysis.Analyze(board);
            Assert.That(report.Kind, Is.EqualTo(PuzzleSolutionKind.RequiresPartial));
            Assert.That(report.Solution.Count, Is.EqualTo(actions));
            Assert.That(report.MinimumPartialLowerBound, Is.EqualTo(partial));
            Assert.That(report.MinimumPartialUpperBound, Is.EqualTo(partial));
            Assert.That(report.IsMinimumExact, Is.True);
            Assert.That(report.Trace.Count(s => s.Outcome == ForwardPathOutcome.Exit), Is.EqualTo(board.ShipCount));
            Assert.That(LevelSolutionProof.Create("P", board, report.Solution).Replay("P", board).IsComplete, Is.True);
            Assert.That(LevelStateIdentity.Fingerprint(board), Is.EqualTo(hash));
        }

        [Test]
        public void WitnessOnlyProvidesBoundsUnlessTheyMeet()
        {
            var p4 = LevelDifficultyAnalysis.FindWitness(Prototype());
            Assert.That(p4.IsMinimumExact, Is.True);
            var p8 = LevelDifficultyAnalysis.FindWitness(Prototype(true));
            Assert.That(p8.HasVerifiedSolution, Is.True);
            Assert.That(p8.MinimumPartialLowerBound, Is.EqualTo(1));
            Assert.That(p8.MinimumPartialUpperBound, Is.EqualTo(2));
            Assert.That(p8.IsMinimumExact, Is.False);
        }

        [Test]
        public void BudgetsAndFailedPeeledSearchNeverClaimOriginalDeadlock()
        {
            var report = LevelDifficultyAnalysis.Analyze(Prototype(), new LevelSolverOptions(maxVisitedStates: 1));
            Assert.That(report.Kind, Is.EqualTo(PuzzleSolutionKind.Unknown));
            Assert.That(report.MinimumPartialUpperBound, Is.Null);
            Assert.That(report.HasVerifiedSolution, Is.False);
            var p8 = Prototype(true); var bad = p8.ApplyPathResult(p8.QueryForwardPath("H"));
            Assert.That(LevelDifficultyAnalysis.Analyze(bad).Kind, Is.EqualTo(PuzzleSolutionKind.Unsolvable));
            Assert.That(LevelDifficultyAnalysis.FindWitness(bad).Kind, Is.EqualTo(PuzzleSolutionKind.Unknown));
        }

        [Test]
        public void SuppliedWitnessCertifiesBWithoutPretendingToRunAnOptimalSearch()
        {
            var board = Prototype(true); var proof = LevelSolutionProof.Create("P8", board, new[] { "A", "E", "D", "F", "G", "B", "A", "H", "C", "B" });
            var report = LevelDifficultyAnalysis.FromProof(board, "P8", proof);
            Assert.That(report.Kind, Is.EqualTo(PuzzleSolutionKind.RequiresPartial));
            Assert.That(report.Solver, Is.Null);
            Assert.That(report.MinimumPartialLowerBound, Is.EqualTo(1));
            Assert.That(report.MinimumPartialUpperBound, Is.EqualTo(2));
            Assert.That(report.IsMinimumExact, Is.False);
            Assert.Throws<System.ArgumentException>(() => LevelDifficultyAnalysis.FromProof(board, "other", proof));
            var next = board.ApplyPathResult(board.QueryForwardPath("A"));
            Assert.Throws<System.ArgumentException>(() => LevelDifficultyAnalysis.FromProof(next, "P8", proof));
        }

        [Test]
        public void DirectExitAndEmptyBoardHaveZeroRequiredPartialMoves()
        {
            var board = LevelSolverTests.Board(4, 4, LevelSolverTests.Ship("A", 0, 0, ShipDirection.Up));
            foreach (var state in new[] { board, board.ApplyPathResult(board.QueryForwardPath("A")) })
            {
                var report = LevelDifficultyAnalysis.Analyze(state);
                Assert.That(report.Kind, Is.EqualTo(PuzzleSolutionKind.DirectExit));
                Assert.That(report.MinimumPartialUpperBound, Is.Zero);
                Assert.That(report.IsMinimumExact, Is.True);
            }
        }

        [Test]
        public void TraceDistinguishesPartialMovementAndLostExits()
        {
            var board = LevelSolverTests.Board(6, 5,
                LevelSolverTests.Ship("A", 0, 2, ShipDirection.Right),
                LevelSolverTests.Ship("B", 4, 1, ShipDirection.Up),
                LevelSolverTests.Ship("C", 2, 0, ShipDirection.Up));
            var step = LevelDifficultyAnalysis.Trace(board, new[] { "A" }).Single();
            Assert.That(step.RemainingBefore, Is.EqualTo(3));
            Assert.That(step.Outcome, Is.EqualTo(ForwardPathOutcome.Blocked));
            Assert.That(step.ExitsBefore, Is.EqualTo(2));
            Assert.That(step.LostExits, Is.EqualTo(1));
            Assert.That(step.ExitShare, Is.EqualTo(2d / 3));
            var next = board.ApplyPathResult(board.QueryForwardPath("A"));
            Assert.Throws<System.ArgumentException>(() => LevelDifficultyAnalysis.Trace(next, new[] { "A" }));
        }
    }
}
