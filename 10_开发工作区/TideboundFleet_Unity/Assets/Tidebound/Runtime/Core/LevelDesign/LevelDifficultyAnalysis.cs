using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public enum PuzzleSolutionKind { Unknown, DirectExit, RequiresPartial, Unsolvable }

    public sealed class ExitPeelingResult
    {
        public BoardModel RemainingBoard { get; }
        public IReadOnlyList<string> ShipIds { get; }
        public bool IsComplete => RemainingBoard.ShipCount == 0;
        internal ExitPeelingResult(BoardModel board, IList<string> ids)
        { RemainingBoard = board; ShipIds = Array.AsReadOnly(ids.ToArray()); }
    }

    public sealed class ReleaseTraceStep
    {
        public string ShipId { get; }
        public ForwardPathOutcome Outcome { get; }
        public int RemainingBefore { get; }
        public int ExitsBefore { get; }
        public int PartialBefore { get; }
        public double ExitShare => RemainingBefore == 0 ? 0 : (double)ExitsBefore / RemainingBefore;
        public int NewExits { get; }
        public int LostExits { get; }
        internal ReleaseTraceStep(string id, ForwardPathOutcome outcome, int remaining, int exits,
            int partial, int unlocked, int lost)
        { ShipId = id; Outcome = outcome; RemainingBefore = remaining; ExitsBefore = exits;
            PartialBefore = partial; NewExits = unlocked; LostExits = lost; }
    }

    public sealed class LevelDifficultyReport
    {
        public string StateFingerprint { get; }
        public ExitPeelingResult Peeling { get; }
        public LevelSolverResult Solver { get; }
        public PuzzleSolutionKind Kind { get; }
        public IReadOnlyList<string> Solution { get; }
        public IReadOnlyList<ReleaseTraceStep> Trace { get; }
        public int? MinimumPartialLowerBound { get; }
        public int? MinimumPartialUpperBound { get; }
        public bool IsMinimumExact => MinimumPartialUpperBound.HasValue &&
            MinimumPartialUpperBound == MinimumPartialLowerBound;
        public bool HasVerifiedSolution { get; }
        public string Method { get; }

        internal LevelDifficultyReport(BoardModel initial, ExitPeelingResult peeling, LevelSolverResult solver,
            IList<string> solution, string method, bool originalSearch)
        {
            StateFingerprint = LevelStateIdentity.Fingerprint(initial); Peeling = peeling; Solver = solver;
            Method = method; HasVerifiedSolution = solution != null;
            Solution = Array.AsReadOnly((solution ?? new List<string>()).ToArray());
            Trace = LevelDifficultyAnalysis.Trace(initial, Solution);
            Kind = HasVerifiedSolution ? (peeling.IsComplete ? PuzzleSolutionKind.DirectExit : PuzzleSolutionKind.RequiresPartial)
                : originalSearch && solver?.Status == LevelSolverStatus.Deadlocked ? PuzzleSolutionKind.Unsolvable : PuzzleSolutionKind.Unknown;
            if (Kind != PuzzleSolutionKind.Unsolvable)
                MinimumPartialLowerBound = peeling.IsComplete ? 0 : 1;
            if (HasVerifiedSolution)
            {
                MinimumPartialUpperBound = peeling.IsComplete ? 0 : Trace.Count(s => s.Outcome == ForwardPathOutcome.Blocked);
                if (originalSearch && solver.Status == LevelSolverStatus.Solved && solver.Optimality == SolutionOptimality.Proven)
                    MinimumPartialLowerBound = MinimumPartialUpperBound;
            }
        }
    }

    /// <summary>Production diagnostics. Finding a witness after peeling does not prove shortest distance or deadlock.</summary>
    public static class LevelDifficultyAnalysis
    {
        public const string Version = "DifficultyAnalysisV2";

        public static ExitPeelingResult Peel(BoardModel initial)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            var board = initial; var ids = new List<string>();
            while (board.ShipCount > 0)
            {
                var ship = board.Ships.OrderBy(s => s.Id, StringComparer.Ordinal)
                    .FirstOrDefault(s => board.QueryForwardPath(s.Id).CanExit);
                if (ship == null) break;
                ids.Add(ship.Id); board = board.ApplyPathResult(board.QueryForwardPath(ship.Id));
            }
            return new ExitPeelingResult(board, ids);
        }

        public static LevelDifficultyReport Analyze(BoardModel initial, LevelSolverOptions options = null)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            var peeling = Peel(initial);
            var result = LevelSolver.Solve(initial, options);
            return new LevelDifficultyReport(initial, peeling, result,
                result.Status == LevelSolverStatus.Solved ? result.ShipIds.ToList() : null,
                result.Method, true);
        }

        public static LevelDifficultyReport FromProof(BoardModel initial, string expectedLevelId, LevelSolutionProof proof)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            var replay = proof.Replay(expectedLevelId, initial);
            if (!replay.IsComplete) throw new ArgumentException("Proof is not a verified solution for this board: " + replay.Error, nameof(proof));
            return new LevelDifficultyReport(initial, Peel(initial), null, proof.Steps.Select(s => s.ShipId).ToList(), "VerifiedSuppliedProof", false);
        }

        // Fast feasible route for large dynamic boards. The peeled prefix is replayed, never assumed safe.
        // Failure here is UNKNOWN for the original board: removing a stopper could lose a valid route.
        public static LevelDifficultyReport FindWitness(BoardModel initial, LevelSolverOptions options = null)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            var peeling = Peel(initial);
            var suffix = LevelSolver.Solve(peeling.RemainingBoard, options);
            List<string> ids = null;
            if (suffix.Status == LevelSolverStatus.Solved)
            {
                ids = peeling.ShipIds.Concat(suffix.ShipIds).ToList();
                // Certify all transitions from the ORIGINAL board, including the prefix.
                LevelSolutionProof.Create("Witness", initial, ids);
            }
            return new LevelDifficultyReport(initial, peeling, suffix, ids, "PeelThenSearchWitness", false);
        }

        public static IReadOnlyList<ReleaseTraceStep> Trace(BoardModel initial, IEnumerable<string> order)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (order == null) throw new ArgumentNullException(nameof(order));
            var board = initial; var steps = new List<ReleaseTraceStep>();
            foreach (var id in order)
            {
                var exits = ExitIds(board);
                var partial = board.Ships.Count(s => { var p = board.QueryForwardPath(s.Id); return p.IsBlocked && p.TravelDistance > 0; });
                var path = board.QueryForwardPath(id);
                if (path.IsBlocked && path.TravelDistance == 0) throw new ArgumentException("Trace contains a zero-distance action.");
                var next = board.ApplyPathResult(path); var after = ExitIds(next);
                steps.Add(new ReleaseTraceStep(id, path.Outcome, board.ShipCount, exits.Count, partial,
                    after.Except(exits).Count(), exits.Where(s => s != id && !after.Contains(s)).Count()));
                board = next;
            }
            return Array.AsReadOnly(steps.ToArray());
        }

        private static HashSet<string> ExitIds(BoardModel board) =>
            new HashSet<string>(board.Ships.Where(s => board.QueryForwardPath(s.Id).CanExit).Select(s => s.Id), StringComparer.Ordinal);
    }
}
