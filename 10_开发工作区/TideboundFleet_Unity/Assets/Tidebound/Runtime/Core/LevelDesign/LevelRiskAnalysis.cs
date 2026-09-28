using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tidebound.Board;
using Tidebound.Ship;
using Tidebound.Tools;

namespace Tidebound.LevelDesign
{
    public enum BranchSolvability { Solvable, Deadlocked, Unknown }

    public sealed class MoveRiskEvidence
    {
        public string ShipId { get; internal set; }
        public ForwardPathOutcome Outcome { get; internal set; }
        public string AfterFingerprint { get; internal set; }
        public BranchSolvability Status { get; internal set; }
        public string Method { get; internal set; }
        public int VisitedStates { get; internal set; }
        public IReadOnlyList<string> Solution { get; internal set; }
    }

    public sealed class LevelRiskReport
    {
        public string StateFingerprint { get; internal set; }
        public IReadOnlyList<MoveRiskEvidence> Moves { get; internal set; }
        public int ZeroDistanceMoves { get; internal set; }
        public int SafeMoves => Moves.Count(m => m.Status == BranchSolvability.Solvable);
        public int DeadlockMoves => Moves.Count(m => m.Status == BranchSolvability.Deadlocked);
        public int UnknownMoves => Moves.Count(m => m.Status == BranchSolvability.Unknown);
        public bool IsFullyClassified => UnknownMoves == 0;
        // A structural proportion at ONE state, never a prediction of human failure probability.
        public double? ProvenDeadlockShare => Moves.Count == 0 ? (double?)null : (double)DeadlockMoves / Moves.Count;
    }

    public sealed class ToolRecoveryEvidence
    {
        public ShipTool Tool { get; internal set; }
        public IReadOnlyList<string> Targets { get; internal set; }
        public int? Seed { get; internal set; }
        public BranchSolvability Status { get; internal set; }
        public string Method { get; internal set; }
        public string AfterFingerprint { get; internal set; }
        public IReadOnlyList<string> Solution { get; internal set; }
    }

    public sealed class ToolRecoveryReport
    {
        public string StateFingerprint { get; internal set; }
        public IReadOnlyList<ToolRecoveryEvidence> Cases { get; internal set; }
        public int PossibleRescuePairs { get; internal set; }
        public int PossibleReverseTargets { get; internal set; }
        public int RequestedShuffleSeeds { get; internal set; }
        public bool HasVerifiedOneToolRecovery => Cases.Any(c => c.Status == BranchSolvability.Solvable);
        public bool AllRescueOutcomesVerified => PossibleRescuePairs > 0 &&
            Cases.Count(c => c.Tool == ShipTool.Rescue && c.Status == BranchSolvability.Solvable) == PossibleRescuePairs;
    }

    /// <summary>Read-only authoring diagnostics using the actual movement/tool geometry. Budgets yield Unknown, never a claimed deadlock.</summary>
    public static class LevelRiskAnalysis
    {
        public const string Version = "MoveRiskV1";
        public static LevelRiskReport Analyze(BoardModel board, int totalMilliseconds = 2000, int perMoveMilliseconds = 100,
            int maxVisitedStates = 4000, IReadOnlyList<string> verifiedContinuation = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (totalMilliseconds < 1 || perMoveMilliseconds < 1 || maxVisitedStates < 1) throw new ArgumentOutOfRangeException();
            var timer = Stopwatch.StartNew(); var records = new List<MoveRiskEvidence>(); var noops = 0;
            if (verifiedContinuation != null) LevelSolutionProof.Create("RiskContinuation", board, verifiedContinuation);
            foreach (var ship in board.Ships.OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                var path = board.QueryForwardPath(ship.Id);
                if (path.IsBlocked && path.TravelDistance == 0) { noops++; continue; }
                var next = board.ApplyPathResult(path);
                MoveRiskEvidence evidence = null;
                if (verifiedContinuation != null && timer.ElapsedMilliseconds < totalMilliseconds)
                {
                    var suffix = verifiedContinuation.ToList(); suffix.Remove(ship.Id);
                    try
                    {
                        LevelSolutionProof.Create("RiskReorderedContinuation", next, suffix);
                        evidence = new MoveRiskEvidence { AfterFingerprint = LevelStateIdentity.Fingerprint(next), Status = BranchSolvability.Solvable,
                            Method = "Verified reordered continuation", Solution = suffix.AsReadOnly() };
                    }
                    catch (Exception e) when (e is ArgumentException || e is KeyNotFoundException)
                    { /* A failed candidate is not evidence of a deadlock. */ }
                }
                evidence = evidence ?? Classify(next, Math.Min(perMoveMilliseconds, Math.Max(0, totalMilliseconds - (int)timer.ElapsedMilliseconds)), maxVisitedStates);
                evidence.ShipId = ship.Id; evidence.Outcome = path.Outcome; records.Add(evidence);
            }
            return new LevelRiskReport { StateFingerprint = LevelStateIdentity.Fingerprint(board), Moves = records.AsReadOnly(), ZeroDistanceMoves = noops };
        }

        private static MoveRiskEvidence Classify(BoardModel board, int milliseconds, int maxVisitedStates)
        {
            var record = new MoveRiskEvidence { AfterFingerprint = LevelStateIdentity.Fingerprint(board), Status = BranchSolvability.Unknown,
                Method = "Not searched: total budget exhausted", Solution = Array.Empty<string>() };
            if (milliseconds <= 0) return record;
            var timer = Stopwatch.StartNew();
            foreach (var cycle in BoardDependencyAnalyzer.Analyze(board).CompleteCycles.Where(c => c.ShipIds.Count <= 10))
            {
                var budget = Math.Min(20, milliseconds - (int)timer.ElapsedMilliseconds);
                if (budget <= 0) break;
                if (!ProvesDeadInRelaxedProjection(board, cycle.ShipIds, budget, maxVisitedStates, out var visited)) continue;
                record.Status = BranchSolvability.Deadlocked; record.VisitedStates = visited;
                record.Method = "Exhausted relaxed projection (all intermediate stops allowed): " + string.Join(",", cycle.ShipIds);
                return record;
            }
            var remaining = milliseconds - (int)timer.ElapsedMilliseconds;
            if (remaining <= 0) return record;
            var result = LevelSolver.Solve(board, new LevelSolverOptions(maxVisitedStates, 400000, remaining));
            record.Method = result.Method + (result.Reason == null ? "" : ": " + result.Reason); record.VisitedStates = result.VisitedStateCount;
            if (result.Status == LevelSolverStatus.Solved)
            {
                // Retain replayable evidence, not just the solver's boolean.
                var proof = LevelSolutionProof.Create("RiskEvidence", board, result.ShipIds);
                if (!proof.Replay("RiskEvidence", board).IsComplete) throw new InvalidOperationException("Risk solution replay failed.");
                record.Status = BranchSolvability.Solvable; record.Solution = result.ShipIds;
            }
            else if (result.Status == LevelSolverStatus.Deadlocked) record.Status = BranchSolvability.Deadlocked;
            return record;
        }

        // Sound negative certificate: remove outsiders AND permit stopping at every free intermediate cell.
        // Every real move of a retained ship maps to an allowed relaxed move; outsider moves map to no-ops.
        // Removing outsiders alone is NOT sound because an outsider can be a necessary stopper.
        internal static bool ProvesDeadInRelaxedProjection(BoardModel original, IEnumerable<string> retained,
            int milliseconds, int maxStates, out int visitedCount)
        {
            var ids = new HashSet<string>(retained, StringComparer.Ordinal); var initial = original;
            foreach (var ship in original.Ships) if (!ids.Contains(ship.Id)) initial = initial.WithoutShip(ship.Id);
            var timer = Stopwatch.StartNew(); var queue = new List<BoardModel> { initial };
            var seen = new HashSet<string>(StringComparer.Ordinal) { LevelStateIdentity.CanonicalKey(initial) }; visitedCount = 1;
            for (var index = 0; index < queue.Count; index++)
            {
                if (timer.ElapsedMilliseconds >= milliseconds) return false;
                var state = queue[index]; if (state.ShipCount == 0) return false;
                foreach (var ship in state.Ships)
                {
                    var path = state.QueryForwardPath(ship.Id);
                    var direction = GridFootprint.DirectionStep(ship.Direction);
                    // Include an exit and every fully-on-board partial stop, even when the real rule forces exit.
                    var maxDistance = path.ClearCells.Count;
                    for (var distance = path.CanExit ? -1 : 1; distance <= maxDistance; distance++)
                    {
                        if (distance == 0) continue;
                        var next = distance == -1 ? state.WithoutShip(ship.Id) : state.WithPlacements(state.Ships.Select(s => s.Id == ship.Id ?
                            s.WithPosition(new GridPosition(s.Position.X + direction.X * distance, s.Position.Y + direction.Y * distance)) : s));
                        if (next.ShipCount == 0) return false;
                        var key = LevelStateIdentity.CanonicalKey(next); if (seen.Contains(key)) continue;
                        if (seen.Count >= maxStates) return false;
                        seen.Add(key); queue.Add(next); visitedCount = seen.Count;
                    }
                }
            }
            return true;
        }

        public static ToolRecoveryReport AnalyzeRecovery(BoardModel board, int perToolMilliseconds = 600,
            int perCaseMilliseconds = 50, int maxRescuePairs = 64, int shuffleSeeds = 8)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (perToolMilliseconds < 1 || perCaseMilliseconds < 1 || maxRescuePairs < 1 || shuffleSeeds < 1) throw new ArgumentOutOfRangeException();
            var cases = new List<ToolRecoveryEvidence>(); var timer = Stopwatch.StartNew();
            foreach (var ship in board.Ships.OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                if (timer.ElapsedMilliseconds >= perToolMilliseconds) break;
                var next = board.WithPlacements(board.Ships.Select(s => s.Id == ship.Id ?
                    s.WithPlacement(s.OccupiedCells[s.Length - 1], RemainingFleetShuffler.Opposite(s.Direction)) : s));
                cases.Add(Recovery(next, ShipTool.Reverse, new[] { ship.Id }, null,
                    Math.Min(perCaseMilliseconds, perToolMilliseconds - (int)timer.ElapsedMilliseconds)));
            }
            var peripheral = ShipToolSystem.PeripheralShips(board).ToArray();
            var possiblePairs = peripheral.Length == 1 ? 1 : peripheral.Length * (peripheral.Length - 1) / 2;
            timer.Restart(); var checkedPairs = 0;
            for (var i = 0; i < peripheral.Length; i++)
            for (var j = peripheral.Length == 1 ? i : i + 1; j < peripheral.Length; j++)
            {
                if (checkedPairs >= maxRescuePairs || timer.ElapsedMilliseconds >= perToolMilliseconds) break;
                var ids = i == j ? new[] { peripheral[i] } : new[] { peripheral[i], peripheral[j] };
                var next = board; foreach (var id in ids) next = next.WithoutShip(id);
                cases.Add(Recovery(next, ShipTool.Rescue, ids, null, Math.Min(perCaseMilliseconds, perToolMilliseconds - (int)timer.ElapsedMilliseconds)));
                checkedPairs++;
            }
            timer.Restart();
            for (var seed = 0; seed < shuffleSeeds && timer.ElapsedMilliseconds < perToolMilliseconds; seed++)
            {
                // Use the gameplay proposal algorithm with a smaller diagnostic budget (32 bounded attempts).
                var budget = Math.Min(perCaseMilliseconds, perToolMilliseconds - (int)timer.ElapsedMilliseconds);
                var next = RemainingFleetShuffler.Propose(board, seed, new LevelSolverOptions(4000, 400000, Math.Max(1, budget / 32)), 32);
                if (next == null)
                    cases.Add(new ToolRecoveryEvidence { Tool = ShipTool.Shuffle, Targets = Array.Empty<string>(), Seed = seed,
                        Status = BranchSolvability.Unknown, Method = "No certified shuffle proposal; no tool would be consumed", Solution = Array.Empty<string>() });
                else cases.Add(Recovery(next, ShipTool.Shuffle, board.Ships.Where(s => !next.GetShip(s.Id).Position.Equals(s.Position) || next.GetShip(s.Id).Direction != s.Direction).Select(s => s.Id).ToArray(),
                    seed, perCaseMilliseconds));
            }
            return new ToolRecoveryReport { StateFingerprint = LevelStateIdentity.Fingerprint(board), Cases = cases.AsReadOnly(),
                PossibleRescuePairs = possiblePairs, PossibleReverseTargets = board.ShipCount, RequestedShuffleSeeds = shuffleSeeds };
        }
        private static ToolRecoveryEvidence Recovery(BoardModel next, ShipTool tool, string[] ids, int? seed, int milliseconds)
        {
            var result = Classify(next, Math.Max(0, milliseconds), 4000);
            return new ToolRecoveryEvidence { Tool = tool, Targets = Array.AsReadOnly(ids), Seed = seed, Status = result.Status,
                Method = result.Method, AfterFingerprint = result.AfterFingerprint, Solution = result.Solution };
        }
    }
}
