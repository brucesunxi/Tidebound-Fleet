using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Tidebound.LevelDesign;
using Tidebound.Tools;

namespace Tidebound.Tests
{
    public sealed class LevelRiskAnalysisTests
    {
        [Test]
        public void RelaxedProjectionNeverInventsDeadlockOnReachableSmallStates()
        {
            var start = LevelDifficultyAnalysisTests.Prototype(true);
            var queue = new List<Tidebound.Board.BoardModel> { start };
            var seen = new HashSet<string> { LevelStateIdentity.CanonicalKey(start) }; var certified = 0;
            for (var i = 0; i < queue.Count && i < 128; i++)
            {
                var state = queue[i];
                foreach (var cycle in BoardDependencyAnalyzer.Analyze(state).CompleteCycles)
                    if (LevelRiskAnalysis.ProvesDeadInRelaxedProjection(state, cycle.ShipIds, 1000, 5000, out _))
                    {
                        certified++;
                        Assert.That(LevelSolver.Solve(state).Status, Is.EqualTo(LevelSolverStatus.Deadlocked));
                    }
                foreach (var ship in state.Ships)
                {
                    var path = state.QueryForwardPath(ship.Id); if (path.IsBlocked && path.TravelDistance == 0) continue;
                    var next = state.ApplyPathResult(path); if (seen.Add(LevelStateIdentity.CanonicalKey(next))) queue.Add(next);
                }
            }
            Assert.That(queue.Count, Is.GreaterThan(20)); Assert.That(certified, Is.GreaterThan(0));
        }

        [Test]
        public void SuppliedContinuationAcceleratesSafeBranchesButDoesNotHideWrongMoves()
        {
            var board = LevelDifficultyAnalysisTests.Prototype(true);
            var proof = LevelSolver.Solve(board);
            var report = LevelRiskAnalysis.Analyze(board, 10000, 1000, verifiedContinuation: proof.ShipIds);
            Assert.That(report.Moves.Single(m => m.ShipId == "H").Status, Is.EqualTo(BranchSolvability.Deadlocked));
            Assert.That(report.Moves.Any(m => m.Method == "Verified reordered continuation"), Is.True);
        }

        [Test]
        public void EightShipWrongMoveIsProvenDeadAndSafeBranchesCarryReplayableEvidence()
        {
            var board = LevelDifficultyAnalysisTests.Prototype(true); var before = LevelStateIdentity.Fingerprint(board);
            var result = LevelRiskAnalysis.Analyze(board, 10000, 1000);
            Assert.That(result.IsFullyClassified, Is.True);
            Assert.That(result.Moves.Single(m => m.ShipId == "H").Status, Is.EqualTo(BranchSolvability.Deadlocked));
            Assert.That(result.SafeMoves, Is.GreaterThan(0));
            foreach (var move in result.Moves.Where(m => m.Status == BranchSolvability.Solvable))
            {
                var next = board.ApplyPathResult(board.QueryForwardPath(move.ShipId));
                Assert.That(LevelSolutionProof.Create("risk", next, move.Solution).Replay("risk", next).IsComplete, Is.True);
            }
            Assert.That(LevelStateIdentity.Fingerprint(board), Is.EqualTo(before));
        }

        [Test]
        public void FourShipDynamicPuzzleDoesNotGetCreditForNonexistentTraps()
        {
            var result = LevelRiskAnalysis.Analyze(LevelDifficultyAnalysisTests.Prototype(), 10000, 1000);
            Assert.That(result.IsFullyClassified, Is.True);
            Assert.That(result.DeadlockMoves, Is.Zero);
            Assert.That(result.SafeMoves, Is.GreaterThan(0));
        }

        [Test]
        public void ExhaustedSearchCannotBeCountedAsDeadlockOrSafe()
        {
            var result = LevelRiskAnalysis.Analyze(LevelDifficultyAnalysisTests.Prototype(true), 10000, 1000, 1);
            Assert.That(result.UnknownMoves, Is.GreaterThan(0));
            Assert.That(result.IsFullyClassified, Is.False);
            Assert.That(result.Moves.Where(m => m.Status == BranchSolvability.Unknown).All(m => m.Solution.Count == 0), Is.True);
            Assert.That(result.ProvenDeadlockShare, Is.EqualTo((double)result.DeadlockMoves / result.Moves.Count));
        }

        [Test]
        public void WrongMoveHasVerifiedOneToolRecoveryWithoutMutatingBoard()
        {
            var initial = LevelDifficultyAnalysisTests.Prototype(true);
            var dead = initial.ApplyPathResult(initial.QueryForwardPath("H")); var before = LevelStateIdentity.Fingerprint(dead);
            var result = LevelRiskAnalysis.AnalyzeRecovery(dead, 10000, 1000, 100, 2);
            Assert.That(result.HasVerifiedOneToolRecovery, Is.True);
            Assert.That(result.Cases.Any(c => c.Tool == ShipTool.Reverse && c.Status == BranchSolvability.Solvable), Is.True);
            Assert.That(LevelStateIdentity.Fingerprint(dead), Is.EqualTo(before));
            Assert.That(result.Cases.Count(c => c.Tool == ShipTool.Rescue), Is.EqualTo(result.PossibleRescuePairs));
            foreach (var c in result.Cases.Where(c => c.Status == BranchSolvability.Solvable && c.Tool != ShipTool.Shuffle))
            {
                var ships = dead.Ships.Where(s => c.Tool != ShipTool.Rescue || !c.Targets.Contains(s.Id)).Select(s =>
                    LevelSolverTests.Ship(s.Id,
                        c.Tool == ShipTool.Reverse && c.Targets.Contains(s.Id) ? s.OccupiedCells.Last().X : s.Position.X,
                        c.Tool == ShipTool.Reverse && c.Targets.Contains(s.Id) ? s.OccupiedCells.Last().Y : s.Position.Y,
                        c.Tool == ShipTool.Reverse && c.Targets.Contains(s.Id) ? RemainingFleetShuffler.Opposite(s.Direction) : s.Direction,
                        s.Length)).ToArray();
                var board = LevelSolverTests.Board(dead.Width, dead.Height, ships);
                Assert.That(LevelStateIdentity.Fingerprint(board), Is.EqualTo(c.AfterFingerprint));
                Assert.That(LevelSolutionProof.Create("recovery", board, c.Solution).Replay("recovery", board).IsComplete, Is.True);
            }
        }
    }
}
