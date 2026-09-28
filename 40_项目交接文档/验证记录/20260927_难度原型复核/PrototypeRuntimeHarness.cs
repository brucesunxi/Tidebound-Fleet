// Research-only harness. Compiles unchanged current Data/Core sources, not a Unity test assembly.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

internal static class PrototypeRuntimeHarness
{
    static Dictionary<string, object> D(params object[] items)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i < items.Length; i += 2) d.Add((string)items[i], items[i + 1]);
        return d;
    }
    static object P(GridPosition p) { return new[] {p.X, p.Y}; }
    static BoardModel Board(int count)
    {
        int[] xs = {1, 0, 4, 5, 3, 2, 2, 5};
        int[] ys = {1, 4, 5, 1, 1, 0, 4, 2};
        var dirs = new[] {ShipDirection.Up, ShipDirection.Right, ShipDirection.Down, ShipDirection.Left,
            ShipDirection.Left, ShipDirection.Left, ShipDirection.Down, ShipDirection.Left};
        return new BoardModel(6, 6, Enumerable.Range(0, count).Select(i =>
            new ShipRuntimeData(((char)('A'+i)).ToString(), FoundationLimits.BaseShipTypeId,
                FoundationLimits.DefaultStandardSkinId, new GridPosition(xs[i], ys[i]), dirs[i], 2,
                FoundationLimits.BaseShipDamage)));
    }
    static object ActionRecord(ForwardPathResult p)
    {
        return D("ship", p.ShipId, "kind", p.CanExit ? "exit" : "partial",
            "from", P(p.OriginTail), "target_tail", P(p.TargetTail), "travel_distance", p.TravelDistance,
            "blocker", p.BlockerShipId);
    }
    static LevelSolverOptions Options(bool peel) { return new LevelSolverOptions(50000, 2000000, 10000, peel); }
    static object SolverRecord(LevelSolverResult r)
    {
        return D("status", r.Status.ToString(), "optimality", r.Optimality.ToString(), "method", r.Method,
            "reason", r.Reason, "visited_state_count", r.VisitedStateCount, "stored_footprint_cells", r.StoredFootprintCells,
            "effective_actions", r.ShipIds.Count, "solution", r.ShipIds.ToArray());
    }
    // Deliberately no hard-cycle pruning. Traverses every reachable state via actual BoardModel transactions.
    sealed class FullGraph
    {
        public readonly List<BoardModel> Nodes = new List<BoardModel>();
        public readonly Dictionary<string, int> Ids = new Dictionary<string, int>();
        public readonly List<List<int>> Prev = new List<List<int>>();
        public readonly HashSet<int> Winning = new HashSet<int>();
        public int EdgeCount;
        public FullGraph(BoardModel initial)
        {
            Nodes.Add(initial); Ids.Add(LevelStateIdentity.CanonicalKey(initial), 0); Prev.Add(new List<int>());
            for (int i = 0; i < Nodes.Count; i++)
            {
                foreach (var ship in Nodes[i].Ships.OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    var path = Nodes[i].QueryForwardPath(ship.Id);
                    if (path.IsBlocked && path.TravelDistance == 0) continue;
                    var next = Nodes[i].ApplyPathResult(path);
                    var key = LevelStateIdentity.CanonicalKey(next);
                    int to;
                    if (!Ids.TryGetValue(key, out to))
                    {
                        if (Nodes.Count >= 50000) throw new Exception("Exhaustive harness budget exceeded: UNKNOWN, not deadlock.");
                        to = Nodes.Count; Ids.Add(key, to); Nodes.Add(next); Prev.Add(new List<int>());
                    }
                    Prev[to].Add(i); EdgeCount++;
                }
            }
            var queue = new Queue<int>();
            for (int i = 0; i < Nodes.Count; i++) if (Nodes[i].ShipCount == 0) { Winning.Add(i); queue.Enqueue(i); }
            while (queue.Count > 0)
                foreach (int parent in Prev[queue.Dequeue()]) if (Winning.Add(parent)) queue.Enqueue(parent);
        }
        public bool Solvable(BoardModel board) { return Winning.Contains(Ids[LevelStateIdentity.CanonicalKey(board)]); }
        public object Record()
        {
            return D("status", Winning.Contains(0) ? "SOLVED_GRAPH_EXHAUSTED" : "UNSOLVABLE_EXHAUSTIVE",
                "all_reachable_states", Nodes.Count, "effective_edges", EdgeCount,
                "solvable_states", Winning.Count, "unsolvable_states", Nodes.Count - Winning.Count,
                "terminal_clear_states", Nodes.Count(x => x.ShipCount == 0),
                "terminal_stuck_states", Nodes.Count(x => x.ShipCount > 0 && x.Ships.All(s =>
                    x.QueryForwardPath(s.Id).IsBlocked && x.QueryForwardPath(s.Id).TravelDistance == 0)));
        }
    }
    static object Check(int count)
    {
        var initial = Board(count);
        var expected = count == 4 ? new[] {"A", "D", "C", "B", "A"} : new[] {"A", "E", "D", "F", "G", "B", "A", "H", "C", "B"};
        var solver = LevelSolver.Solve(initial, Options(true));
        var withoutPeeling = LevelSolver.Solve(initial, Options(false));
        if (solver.Status != LevelSolverStatus.Solved || solver.Optimality != SolutionOptimality.Proven ||
            solver.ShipIds.Count != expected.Length || withoutPeeling.ShipIds.Count != expected.Length)
            throw new Exception("Unexpected solver result.");
        var graph = new FullGraph(initial);
        var initialMoves = new List<object>();
        foreach (var ship in initial.Ships.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var p = initial.QueryForwardPath(ship.Id);
            if (p.IsBlocked && p.TravelDistance == 0) continue;
            var next = initial.ApplyPathResult(p);
            initialMoves.Add(D("action", ActionRecord(p), "solvable_full_graph", graph.Solvable(next),
                "solver", SolverRecord(LevelSolver.Solve(next, Options(false)))));
        }
        var board = initial;
        var peeling = new List<string>();
        while (board.ShipCount > 0)
        {
            var first = board.Ships.OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault(x => board.QueryForwardPath(x.Id).CanExit);
            if (first == null) break;
            peeling.Add(first.Id); board = board.ApplyPathResult(board.QueryForwardPath(first.Id));
        }
        var remaining = board.Ships.Select(x => x.Id).ToArray();
        board = initial;
        var replay = new List<object>();
        int partials = 0;
        foreach (var id in expected)
        {
            var p = board.QueryForwardPath(id);
            if (p.IsBlocked && p.TravelDistance == 0) throw new Exception("Zero-distance replay step.");
            if (p.IsBlocked) partials++;
            var next = board.ApplyPathResult(p);
            replay.Add(D("step", replay.Count + 1, "action", ActionRecord(p), "before", LevelStateIdentity.Fingerprint(board),
                "after", LevelStateIdentity.Fingerprint(next), "remaining", next.ShipCount));
            board = next;
        }
        if (board.ShipCount != 0 || partials != (count == 4 ? 1 : 2)) throw new Exception("Incomplete or incorrect replay.");
        var proof = LevelSolutionProof.Create("RESEARCH_DYNAMIC_"+count, initial, expected);
        var proofReplay = proof.Replay("RESEARCH_DYNAMIC_"+count, initial);
        if (!proofReplay.IsValid || !proofReplay.IsComplete) throw new Exception("Versioned proof failed.");
        var result = D("name", count+"-ship prototype", "layout_fingerprint", LevelStateIdentity.Fingerprint(initial),
            "initial_placements", initial.Ships.Select(x => D("id", x.Id, "tail", P(x.Position), "direction", x.Direction.ToString(),
                "length", x.Length, "occupied", x.OccupiedCells.Select(c => P(c)).ToArray())).ToArray(),
            "initial_direct_exits", initial.Ships.Where(x => initial.QueryForwardPath(x.Id).CanExit).Select(x => x.Id).ToArray(),
            "pure_exit_peeling", peeling.ToArray(), "remaining_after_peeling", remaining,
            "solver_default", SolverRecord(solver), "solver_no_exit_peeling", SolverRecord(withoutPeeling),
            "minimum_effective_actions", solver.ShipIds.Count, "minimum_necessary_partial_moves", solver.ShipIds.Count-count,
            "optimality_basis", "BFS unit-cost shortest solution; every complete no-prop solution has exactly N exits, so Pmin=Lopt-N.",
            "full_reachable_graph", graph.Record(), "initial_effective_actions", initialMoves.ToArray(),
            "provided_solution_replay", replay.ToArray(), "proof_replay", D("version", proof.ProofVersion,
                "valid", proofReplay.IsValid, "complete", proofReplay.IsComplete, "applied_steps", proofReplay.AppliedStepCount));
        if (count == 8)
        {
            var wrongPath = initial.QueryForwardPath("H");
            var bad = initial.ApplyPathResult(wrongPath);
            var badGraph = new FullGraph(bad);
            if (badGraph.Winning.Count != 0 || badGraph.Nodes.Count != 18) throw new Exception("Unexpected H branch result.");
            result.Add("wrong_H_first", D("action", ActionRecord(wrongPath), "solver", SolverRecord(LevelSolver.Solve(bad, Options(false))),
                "full_reachable_graph", badGraph.Record()));
        }
        return result;
    }
    static string Json(object value)
    {
        if (value == null) return "null";
        if (value is string) return "\"" + ((string)value).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
        if (value is bool) return (bool)value ? "true" : "false";
        if (value is IDictionary)
        {
            var pairs = new List<string>();
            foreach (DictionaryEntry p in (IDictionary)value) pairs.Add(Json(p.Key) + ":" + Json(p.Value));
            return "{" + string.Join(",", pairs) + "}";
        }
        if (value is IEnumerable) return "[" + string.Join(",", ((IEnumerable)value).Cast<object>().Select(Json)) + "]";
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
    public static void Main()
    {
        Console.WriteLine(Json(D("scope", "CURRENT_CSHARP_CORE_SOURCES_MONO_HARNESS_NOT_UNITY_EDITOR_OR_PLAYMODE",
            "rules_version", LevelRules.Version, "exit_mode", LevelRules.ExitMode, "solver_version", LevelSolver.Version,
            "budget", D("max_states", 50000, "max_footprint_cells", 2000000, "solver_time_ms", 10000),
            "prototypes", new[] {Check(4), Check(8)})));
    }
}
