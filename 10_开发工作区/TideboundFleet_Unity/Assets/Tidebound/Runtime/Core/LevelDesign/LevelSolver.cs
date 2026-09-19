using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.LevelDesign
{
    public enum LevelSolverStatus { Invalid, Solved, Deadlocked, LimitReached }
    public enum SolutionOptimality { Unproven, Proven }

    public sealed class LevelSolverOptions
    {
        public int MaxVisitedStates { get; }
        public int MaxStoredFootprintCells { get; }
        public int TimeLimitMilliseconds { get; }
        public bool UseExitPeeling { get; }
        public string RulesVersion { get; }
        public string ExitMode { get; }

        public LevelSolverOptions(int maxVisitedStates = 20000, int maxStoredFootprintCells = 2000000,
            int timeLimitMilliseconds = 5000, bool useExitPeeling = true,
            string rulesVersion = LevelRules.Version, string exitMode = LevelRules.ExitMode)
        {
            MaxVisitedStates = maxVisitedStates; MaxStoredFootprintCells = maxStoredFootprintCells;
            TimeLimitMilliseconds = timeLimitMilliseconds; UseExitPeeling = useExitPeeling;
            RulesVersion = rulesVersion; ExitMode = exitMode;
        }
    }

    public sealed class LevelSolverResult
    {
        public LevelSolverStatus Status { get; }
        public SolutionOptimality Optimality { get; }
        public IReadOnlyList<string> ShipIds { get; }
        public int VisitedStateCount { get; }
        public long StoredFootprintCells { get; }
        public string Method { get; }
        public string Reason { get; }

        internal LevelSolverResult(LevelSolverStatus status, IList<string> shipIds, int visited, long cells,
            string method, string reason = null)
        {
            Status = status;
            Optimality = status == LevelSolverStatus.Solved ? SolutionOptimality.Proven : SolutionOptimality.Unproven;
            ShipIds = Array.AsReadOnly((shipIds ?? Array.Empty<string>()).ToArray());
            VisitedStateCount = visited; StoredFootprintCells = cells; Method = method; Reason = reason;
        }
    }

    /// <summary>Uses the runtime transaction, independently of generator edges or a supplied witness.</summary>
    public static class LevelSolver
    {
        public const string Version = "LevelSolverV1";

        public static LevelSolverResult Solve(LevelData level, IReadOnlyList<ShipDefinition> ships,
            IReadOnlyList<BossDefinition> bosses, LevelSolverOptions options = null)
        {
            var validation = BoardValidator.Validate(level, ships, bosses);
            if (!validation.IsValid)
                return Result(LevelSolverStatus.Invalid, "Validation", string.Join("; ", validation.Issues.Select(x => x.Code)));
            using (var session = LevelSessionFactory.Create(level, ships, bosses))
                return Solve(session.InitialBoard, options);
        }

        public static LevelSolverResult Solve(BoardModel initialBoard, LevelSolverOptions options = null)
        {
            options = options ?? new LevelSolverOptions();
            if (initialBoard == null || options.MaxVisitedStates < 1 || options.MaxStoredFootprintCells < 1 ||
                options.TimeLimitMilliseconds < 1 || options.RulesVersion != LevelRules.Version ||
                options.ExitMode != LevelRules.ExitMode)
                return Result(LevelSolverStatus.Invalid, "Validation", "Invalid board, budget, rules or exit mode.");
            if (initialBoard.Width > FoundationLimits.MaxTechnicalBoardWidth ||
                initialBoard.Height > FoundationLimits.MaxTechnicalBoardHeight ||
                initialBoard.ShipCount > FoundationLimits.MaxTechnicalShipCount ||
                initialBoard.Ships.Any(x => x.Length < 2 || x.Length > 3 ||
                    x.TypeId != FoundationLimits.BaseShipTypeId || !Enum.IsDefined(typeof(ShipDirection), x.Direction)))
                return Result(LevelSolverStatus.Invalid, "Validation", "Board exceeds the supported rules.");
            if (initialBoard.ShipCount == 0)
                return new LevelSolverResult(LevelSolverStatus.Solved, Array.Empty<string>(), 1, 0, "EmptyBoard");

            var timer = Stopwatch.StartNew();
            if (options.UseExitPeeling)
            {
                var board = initialBoard;
                var order = new List<string>();
                while (board.ShipCount > 0)
                {
                    if (timer.ElapsedMilliseconds >= options.TimeLimitMilliseconds)
                        return Result(LevelSolverStatus.LimitReached, "ExitPeeling", "Time budget reached.");
                    var available = board.Ships.OrderBy(x => x.Id, StringComparer.Ordinal)
                        .FirstOrDefault(x => board.QueryForwardPath(x.Id).CanExit);
                    if (available == null) break;
                    board = board.ApplyPathResult(board.QueryForwardPath(available.Id));
                    order.Add(available.Id);
                }
                if (board.ShipCount == 0)
                    return new LevelSolverResult(LevelSolverStatus.Solved, order, 1, 0, "DirectExitLowerBound");
                // Start search at the ORIGINAL state. A greedy prefix must not constrain shortest search.
            }

            if (BoardDependencyAnalyzer.Analyze(initialBoard).HardLockedCycleCount > 0)
                return Result(LevelSolverStatus.Deadlocked, "ClosedZeroTravelCycle", "A closed cycle cannot move or exit.");
            if (initialBoard.OccupiedCellCount > options.MaxStoredFootprintCells)
                return Result(LevelSolverStatus.LimitReached, "BreadthFirstSearch", "Stored footprint budget reached.");

            var nodes = new List<SearchNode> { new SearchNode(initialBoard, -1, null) };
            var visited = new HashSet<string>(StringComparer.Ordinal) { LevelStateIdentity.CanonicalKey(initialBoard) };
            long storedCells = initialBoard.OccupiedCellCount;
            for (var cursor = 0; cursor < nodes.Count; cursor++)
            {
                var current = nodes[cursor].Board;
                foreach (var ship in current.Ships.OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    if (timer.ElapsedMilliseconds >= options.TimeLimitMilliseconds)
                        return new LevelSolverResult(LevelSolverStatus.LimitReached, null, nodes.Count, storedCells,
                            "BreadthFirstSearch", "Time budget reached.");
                    var path = current.QueryForwardPath(ship.Id);
                    if (path.IsBlocked && path.TravelDistance == 0) continue;
                    var next = current.ApplyPathResult(path);
                    if (next.ShipCount == 0)
                    {
                        var order = Reconstruct(nodes, cursor);
                        order.Add(ship.Id);
                        return new LevelSolverResult(LevelSolverStatus.Solved, order, nodes.Count, storedCells, "BreadthFirstSearch");
                    }
                    var key = LevelStateIdentity.CanonicalKey(next);
                    if (visited.Contains(key)) continue;
                    if (nodes.Count >= options.MaxVisitedStates ||
                        storedCells + next.OccupiedCellCount > options.MaxStoredFootprintCells)
                        return new LevelSolverResult(LevelSolverStatus.LimitReached, null, nodes.Count, storedCells,
                            "BreadthFirstSearch", "State or stored footprint budget reached.");
                    visited.Add(key);
                    storedCells += next.OccupiedCellCount;
                    nodes.Add(new SearchNode(next, cursor, ship.Id));
                }
            }
            return new LevelSolverResult(LevelSolverStatus.Deadlocked, null, nodes.Count, storedCells,
                "BreadthFirstSearch", "All reachable forward states exhausted.");
        }

        private static LevelSolverResult Result(LevelSolverStatus status, string method, string reason) =>
            new LevelSolverResult(status, null, 0, 0, method, reason);

        private static List<string> Reconstruct(IReadOnlyList<SearchNode> nodes, int index)
        {
            var result = new List<string>();
            while (nodes[index].Parent >= 0)
            {
                result.Add(nodes[index].ShipId);
                index = nodes[index].Parent;
            }
            result.Reverse();
            return result;
        }

        private sealed class SearchNode
        {
            public BoardModel Board { get; }
            public int Parent { get; }
            public string ShipId { get; }
            public SearchNode(BoardModel board, int parent, string shipId)
            { Board = board; Parent = parent; ShipId = shipId; }
        }
    }
}
