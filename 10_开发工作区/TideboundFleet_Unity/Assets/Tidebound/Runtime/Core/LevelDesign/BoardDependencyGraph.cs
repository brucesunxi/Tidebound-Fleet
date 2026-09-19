using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public sealed class ShipDependencyNode
    {
        public string ShipId { get; }
        public string BlockerShipId { get; }
        public IReadOnlyList<string> AllBlockerShipIds { get; }
        public bool CanExit { get; }
        public int TravelDistance { get; }

        internal ShipDependencyNode(string shipId, ForwardPathResult path, IList<string> allBlockers)
        {
            ShipId = shipId;
            BlockerShipId = path.BlockerShipId;
            AllBlockerShipIds = Array.AsReadOnly(allBlockers.ToArray());
            CanExit = path.CanExit;
            TravelDistance = path.TravelDistance;
        }
    }

    public sealed class DependencyComponent
    {
        public IReadOnlyList<string> ShipIds { get; }
        public bool IsHardLocked { get; }

        internal DependencyComponent(IList<string> shipIds, bool isHardLocked)
        {
            ShipIds = Array.AsReadOnly(shipIds.OrderBy(x => x, StringComparer.Ordinal).ToArray());
            IsHardLocked = isHardLocked;
        }
    }

    public sealed class BoardDependencyGraph
    {
        private readonly IReadOnlyDictionary<string, ShipDependencyNode> byId;

        public IReadOnlyList<ShipDependencyNode> Nodes { get; }
        public IReadOnlyList<DependencyComponent> Cycles { get; }
        public int InitialExitCount { get; }
        public int InitialMoveCount { get; }
        // Legacy nearest-blocker edge count; retained for historical prototype reports.
        public int DependencyDepth { get; }
        public IReadOnlyList<DependencyComponent> CompleteCycles { get; }
        // Complete-path DAG depth in nodes (an unblocked ship has depth 1); null for a cycle.
        public int? CompleteDependencyDepth { get; }
        public int HardLockedCycleCount => Cycles.Count(x => x.IsHardLocked);

        internal BoardDependencyGraph(IList<ShipDependencyNode> nodes, IList<DependencyComponent> cycles,
            int dependencyDepth, IList<DependencyComponent> completeCycles, int? completeDepth)
        {
            var copy = nodes.OrderBy(x => x.ShipId, StringComparer.Ordinal).ToArray();
            Nodes = Array.AsReadOnly(copy);
            byId = copy.ToDictionary(x => x.ShipId, StringComparer.Ordinal);
            Cycles = Array.AsReadOnly(cycles.ToArray());
            InitialExitCount = copy.Count(x => x.CanExit);
            InitialMoveCount = copy.Count(x => !x.CanExit && x.TravelDistance > 0);
            DependencyDepth = dependencyDepth;
            CompleteCycles = Array.AsReadOnly(completeCycles.ToArray());
            CompleteDependencyDepth = completeDepth;
        }

        public ShipDependencyNode GetNode(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) throw new ArgumentException("A ship id is required.", nameof(shipId));
            return byId[shipId];
        }
    }

    public static class BoardDependencyAnalyzer
    {
        public static BoardDependencyGraph Analyze(BoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var nodes = new List<ShipDependencyNode>(board.ShipCount);
            foreach (var ship in board.Ships)
                nodes.Add(new ShipDependencyNode(ship.Id, board.QueryForwardPath(ship.Id), ScanAllBlockers(board, ship)));

            var byId = nodes.ToDictionary(x => x.ShipId, StringComparer.Ordinal);
            var components = StrongComponents(nodes, byId);
            var cycles = new List<DependencyComponent>();
            foreach (var component in components)
            {
                var isCycle = component.Count > 1 ||
                              (component.Count == 1 && byId[component[0]].BlockerShipId == component[0]);
                if (!isCycle) continue;
                var members = new HashSet<string>(component, StringComparer.Ordinal);
                var hardLocked = component.All(id =>
                {
                    var node = byId[id];
                    return !node.CanExit && node.TravelDistance == 0 &&
                           node.BlockerShipId != null && members.Contains(node.BlockerShipId);
                });
                cycles.Add(new DependencyComponent(component, hardLocked));
            }
            var completeCycles = StrongComponents(nodes, byId, true)
                .Where(x => x.Count > 1)
                .Select(x => new DependencyComponent(x, false)).ToList();
            return new BoardDependencyGraph(nodes, cycles, LongestDependencyChain(nodes, byId),
                completeCycles, completeCycles.Count == 0 ? (int?)CompleteDepth(nodes, byId) : null);
        }

        private static List<List<string>> StrongComponents(IReadOnlyList<ShipDependencyNode> nodes,
            IReadOnlyDictionary<string, ShipDependencyNode> byId, bool complete = false)
        {
            var index = 0;
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            var lowLinks = new Dictionary<string, int>(StringComparer.Ordinal);
            var stack = new Stack<string>();
            var onStack = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<List<string>>();

            Action<string> visit = null;
            visit = id =>
            {
                indices[id] = index;
                lowLinks[id] = index;
                index++;
                stack.Push(id);
                onStack.Add(id);

                var blockers = complete ? byId[id].AllBlockerShipIds :
                    (IReadOnlyList<string>)new[] { byId[id].BlockerShipId };
                foreach (var blocker in blockers)
                {
                    if (blocker == null || !byId.ContainsKey(blocker)) continue;
                    if (!indices.ContainsKey(blocker))
                    {
                        visit(blocker);
                        lowLinks[id] = Math.Min(lowLinks[id], lowLinks[blocker]);
                    }
                    else if (onStack.Contains(blocker))
                    {
                        lowLinks[id] = Math.Min(lowLinks[id], indices[blocker]);
                    }
                }
                if (lowLinks[id] != indices[id]) return;
                var component = new List<string>();
                string current;
                do
                {
                    current = stack.Pop();
                    onStack.Remove(current);
                    component.Add(current);
                } while (current != id);
                result.Add(component);
            };

            foreach (var node in nodes)
                if (!indices.ContainsKey(node.ShipId)) visit(node.ShipId);
            return result;
        }

        private static List<string> ScanAllBlockers(BoardModel board, BoardShipSnapshot ship)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var step = GridFootprint.DirectionStep(ship.Direction);
            var head = ship.OccupiedCells[ship.Length - 1];
            var cell = new GridPosition(head.X + step.X, head.Y + step.Y);
            while (board.IsInside(cell))
            {
                var id = board.GetShipId(cell);
                if (id != null && seen.Add(id)) result.Add(id);
                cell = new GridPosition(cell.X + step.X, cell.Y + step.Y);
            }
            return result;
        }

        private static int CompleteDepth(IReadOnlyList<ShipDependencyNode> nodes,
            IReadOnlyDictionary<string, ShipDependencyNode> byId)
        {
            var memo = new Dictionary<string, int>(StringComparer.Ordinal);
            Func<string, int> depth = null;
            depth = id =>
            {
                if (memo.TryGetValue(id, out var value)) return value;
                var result = 1;
                foreach (var blocker in byId[id].AllBlockerShipIds)
                    result = Math.Max(result, 1 + depth(blocker));
                memo[id] = result;
                return result;
            };
            return nodes.Count == 0 ? 0 : nodes.Max(x => depth(x.ShipId));
        }

        private static int LongestDependencyChain(IReadOnlyList<ShipDependencyNode> nodes,
            IReadOnlyDictionary<string, ShipDependencyNode> byId)
        {
            var longest = 0;
            foreach (var node in nodes)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var cursor = node;
                var depth = 0;
                while (cursor.BlockerShipId != null && byId.TryGetValue(cursor.BlockerShipId, out var next) &&
                       seen.Add(cursor.ShipId))
                {
                    depth++;
                    cursor = next;
                }
                longest = Math.Max(longest, depth);
            }
            return longest;
        }
    }
}
