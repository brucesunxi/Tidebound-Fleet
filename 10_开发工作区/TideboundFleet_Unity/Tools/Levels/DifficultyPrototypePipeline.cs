using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

// Development-only pipeline. Outputs independent review content, never the campaign or player saves.
internal static partial class DifficultyPrototypePipeline
{
    private static string root, output;
    private const string Version = "DifficultyPrototypeV2.1";
    private static readonly ShipDefinition[] Catalog = { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) };
    private static readonly BossDefinition[] Bosses = { new BossDefinition("TF_KRAKEN_01") };
    private static readonly List<JObject> Rows = new List<JObject>();
    private static readonly string[] Strategies = { "id", "most-unlock", "least-unlock", "spatial", "seeded" };

    public static int Main(string[] args)
    {
        try
        {
            root = Path.GetFullPath(args[0]); output = Path.Combine(root, "Assets/Tidebound/Config/LevelPrototypes/DifficultyV2");
            if (args[1] == "campaign-v3") CampaignV3(args);
            else if (args[1] == "generate") Generate();
            else if (args[1] == "validate") Validate();
            else if (args[1] == "refine-bridges") RefineBridges();
            else if (args[1] == "audit-risk") AuditRisk();
            else throw new ArgumentException("Expected generate, validate or refine-bridges.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static BoardModel Board(LevelData level)
    { using (var session = LevelSessionFactory.Create(level, Catalog, Bosses)) return session.InitialBoard; }
    private static LevelData Clone(LevelData source, string id = null)
    { var copy = LevelJsonReader.Read(LevelJsonWriter.Write(source)); if (id != null) copy.LevelId = id; return copy; }
    private static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction, int length = 2) =>
        new ShipPlacementData { Id = id, TypeId = FoundationLimits.BaseShipTypeId, Length = length, Position = new GridPosition(x, y), Direction = direction };
    private static LevelData Prototype(bool eight, int dx = 0, int dy = 0, bool full = false)
    {
        var ships = new[] { Ship("A", 1, 1, ShipDirection.Up), Ship("B", 0, 4, ShipDirection.Right),
            Ship("C", 4, 5, ShipDirection.Down), Ship("D", 5, 1, ShipDirection.Left),
            Ship("E", 3, 1, ShipDirection.Left), Ship("F", 2, 0, ShipDirection.Left),
            Ship("G", 2, 4, ShipDirection.Down), Ship("H", 5, 2, ShipDirection.Left) }.Take(eight ? 8 : 4).ToArray();
        foreach (var s in ships) s.Position = new GridPosition(s.Position.X + dx, s.Position.Y + dy);
        return new LevelData { SchemaVersion = 2, LevelId = full ? (eight ? "DV2_B80_R" : "DV2_B80_T") : (eight ? "DV2_P8" : "DV2_P4"),
            Width = full ? 14 : 6, Height = full ? 18 : 6, Ships = ships, BossId = "TF_KRAKEN_01" };
    }

    private static void Generate()
    {
        Directory.CreateDirectory(output);
        Require(!File.Exists(Path.Combine(output, "review-manifest.json")), "Review pack already exists. Validate it or generate in a new version; never silently overwrite a certified pack.");
        foreach (var n in new[] { 24, 38 })
        {
            var path = Path.Combine(root, "Assets/Tidebound/Config/Levels/Campaign/TF_Campaign_" + n.ToString("D3") + ".json");
            var source = LevelJsonReader.Read(File.ReadAllText(path));
            var baseline = Clone(source, "DV2_C" + n); Save(baseline, null, "control", null, source);
            var visual = Refine(source, "DV2_V" + n, true, 270900 + n); Save(visual, null, "contour", MaskCells(), source);
            var release = Refine(source, "DV2_R" + n, false, 270940 + n); Save(release, null, "release", null, source);
        }
        foreach (var eight in new[] { false, true })
        {
            var small = Prototype(eight); Save(small, null, "mechanism", null, null);
            List<string> route;
            var full = Fill(eight, out route);
            full = MixBridge(full, route, eight ? 279272 : 279271);
            Save(full, route, eight ? "dynamic-risk" : "dynamic-teaching", null, null);
        }
        var manifest = new JObject { ["version"] = Version, ["status"] = "ReviewOnly", ["rulesVersion"] = LevelRules.Version,
            ["humanPlaytest"] = "Pending", ["deviceValidation"] = "Pending", ["bridgeRevision"] = BridgeRevision, ["levels"] = new JArray(Rows),
            ["strategyWarning"] = "Finite natural-strategy samples, not an exhaustive all-orders proof or a player failure probability." };
        Write("review-manifest.json", manifest.ToString(Formatting.Indented)); Validate();
    }

    // 220 initial placement cells, with eight cells removed from each corner. Outside cells are NOT walls.
    private static bool InMask(int x, int y)
    {
        var ex = Math.Min(x, 13 - x); var ey = Math.Min(y, 17 - y);
        return !(ex < 4 && ey < 2);
    }
    private static JArray MaskCells()
    {
        var cells = new JArray();
        for (var y = 0; y < 18; y++) for (var x = 0; x < 14; x++) if (InMask(x, y)) cells.Add(new JArray(x, y));
        return cells;
    }

    private static int[,] Occupancy(LevelData level)
    {
        var grid = new int[level.Width, level.Height];
        for (var y = 0; y < level.Height; y++) for (var x = 0; x < level.Width; x++) grid[x, y] = -1;
        for (var i = 0; i < level.Ships.Length; i++) foreach (var c in GridFootprint.Cells(level.Ships[i].Position, level.Ships[i].Direction, level.Ships[i].Length))
        { Require(c.X >= 0 && c.X < level.Width && c.Y >= 0 && c.Y < level.Height && grid[c.X, c.Y] == -1, "Invalid geometry"); grid[c.X, c.Y] = i; }
        return grid;
    }

    private sealed class DirectGraph
    {
        public readonly List<int>[] Blockers, Dependents;
        public readonly LevelData Level;
        public DirectGraph(LevelData level)
        {
            Level = level; var count = level.Ships.Length; var grid = Occupancy(level);
            Blockers = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();
            Dependents = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();
            for (var i = 0; i < count; i++)
            {
                var ship = level.Ships[i]; var d = GridFootprint.DirectionStep(ship.Direction);
                var x = ship.Position.X + d.X * ship.Length; var y = ship.Position.Y + d.Y * ship.Length;
                while (x >= 0 && x < level.Width && y >= 0 && y < level.Height)
                {
                    var other = grid[x, y]; if (other >= 0 && !Blockers[i].Contains(other)) { Blockers[i].Add(other); Dependents[other].Add(i); }
                    x += d.X; y += d.Y;
                }
            }
        }
        public List<string> Order(int strategy, out List<int> curve)
        {
            var counts = Blockers.Select(x => x.Count).ToArray(); var available = new List<int>();
            for (var i = 0; i < counts.Length; i++) if (counts[i] == 0) available.Add(i);
            var order = new List<string>(); curve = new List<int>(); var random = new Random(270927);
            while (available.Count > 0)
            {
                var chosen = available[0];
                if (strategy == 4) chosen = available[random.Next(available.Count)];
                else foreach (var candidate in available)
                {
                    var c = Value(candidate, strategy, counts); var b = Value(chosen, strategy, counts);
                    if (c < b || c == b && string.CompareOrdinal(Level.Ships[candidate].Id, Level.Ships[chosen].Id) < 0) chosen = candidate;
                }
                curve.Add(available.Count); order.Add(Level.Ships[chosen].Id); available.Remove(chosen);
                foreach (var dependent in Dependents[chosen]) if (--counts[dependent] == 0) available.Add(dependent);
            }
            return order;
        }
        private int Value(int i, int strategy, int[] counts) => strategy == 1 ? -Dependents[i].Count(x => counts[x] == 1) :
            strategy == 2 ? Dependents[i].Count(x => counts[x] == 1) : strategy == 3 ? Level.Ships[i].Position.Y * Level.Width + Level.Ships[i].Position.X : 0;
    }

    private static double Score(LevelData level, bool visual, double[] baseline, out bool solved)
    {
        var graph = new DirectGraph(level); var means = new double[5]; double shape = 0; double penalty = 0;
        for (var i = 0; i < 5; i++)
        {
            var order = graph.Order(i, out var curve); solved = order.Count == level.Ships.Length;
            if (!solved) return 1e9;
            var early = curve.Take(48).ToArray(); means[i] = early.Average();
            var run = 0; var maxRun = 0;
            foreach (var e in early) { run = e == 1 ? run + 1 : 0; maxRun = Math.Max(maxRun, run); }
            if (visual) penalty += Math.Abs(means[i] - baseline[i]) * 3 + Math.Abs(curve[0] - baseline[5]) * .6;
            else penalty += Math.Abs(means[i] - 4.5) * 3 + Math.Max(0, early.Max() - 8) * .6 + Math.Max(0, maxRun - 3) * 3;
        }
        if (visual) shape = level.Ships.Sum(s => GridFootprint.Cells(s.Position, s.Direction, s.Length).Count(c => !InMask(c.X, c.Y))) * 18;
        else penalty += Math.Max(0, graph.Blockers.Count(b => b.Count == 0) - 6) * 2;
        solved = true; return shape + penalty;
    }

    private static LevelData Refine(LevelData source, string id, bool visual, int seed)
    {
        var baseline = new double[6]; var graph = new DirectGraph(source);
        for (var i = 0; i < 5; i++) { graph.Order(i, out var curve); baseline[i] = curve.Take(48).Average(); baseline[5] = curve[0]; }
        var random = new Random(seed); var level = Clone(source, id); var best = Clone(level);
        var currentScore = Score(level, visual, baseline, out _); var bestScore = currentScore;
        for (var iteration = 0; iteration < 30000; iteration++)
        {
            var i = random.Next(level.Ships.Length); var original = level.Ships[i]; var grid = Occupancy(level);
            var direction = visual ? original.Direction : original.Direction == ShipDirection.Up ? ShipDirection.Down :
                original.Direction == ShipDirection.Down ? ShipDirection.Up : original.Direction == ShipDirection.Left ? ShipDirection.Right : ShipDirection.Left;
            var head = GridFootprint.Cells(original.Position, original.Direction, original.Length).Last();
            var candidate = visual ? Ship(original.Id, random.Next(level.Width), random.Next(level.Height), direction, original.Length) :
                Ship(original.Id, head.X, head.Y, direction, original.Length);
            var cells = GridFootprint.Cells(candidate.Position, candidate.Direction, candidate.Length).ToArray();
            if (cells.Any(c => c.X < 0 || c.X >= level.Width || c.Y < 0 || c.Y >= level.Height || grid[c.X, c.Y] >= 0 && grid[c.X, c.Y] != i)) continue;
            level.Ships[i] = candidate;
            var score = Score(level, visual, baseline, out var solved);
            var temperature = Math.Max(.08, .7 * (1 - iteration / 30000d));
            if (solved && (score <= currentScore || random.NextDouble() < Math.Exp((currentScore - score) / temperature))) currentScore = score;
            else level.Ships[i] = original;
            if (currentScore < bestScore) { best = Clone(level); bestScore = currentScore; }
        }
        if (visual) Require(best.Ships.All(s => GridFootprint.Cells(s.Position, s.Direction, s.Length).All(c => InMask(c.X, c.Y))), "Contour target not reached: " + id);
        else Require(bestScore < Score(source, false, baseline, out _) - 1, "Release target not improved: " + id);
        Console.WriteLine(id + " refinement score " + bestScore.ToString("0.00")); return best;
    }

    private static LevelData Fill(bool eight, out List<string> route)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var level = Prototype(eight, 4, 6, true); var core = Board(level);
            var currentRoute = LevelSolver.Solve(core).ShipIds.ToList(); var random = new Random(279000 + attempt + (eight ? 100 : 0));
            var items = level.Ships.ToList(); var dependents = 0;
            while (items.Count < 80)
            {
                level.Ships = items.ToArray(); var grid = Occupancy(level); var longCount = items.Count(s => s.Length == 3);
                var length = longCount < 6 && (items.Count - (eight ? 8 : 4)) % 10 == 0 ? 3 : 2;
                var candidates = new List<Tuple<ShipPlacementData, double, bool>>();
                var graph = new DirectGraph(level); var roots = graph.Blockers.Select((b, i) => new { b, i }).Where(a => a.b.Count == 0).Select(a => a.i).ToArray();
                for (var d = 0; d < 4; d++) for (var y = 0; y < 18; y++) for (var x = 0; x < 14; x++)
                {
                    var ship = Ship("Z" + items.Count.ToString("D3"), x, y, (ShipDirection)d, length);
                    var cells = GridFootprint.Cells(ship.Position, ship.Direction, length).ToArray();
                    if (cells.Any(c => c.X < 0 || c.X >= 14 || c.Y < 0 || c.Y >= 18 || grid[c.X, c.Y] >= 0 || c.X >= 4 && c.X < 10 && c.Y >= 6 && c.Y < 12)) continue;
                    var step = GridFootprint.DirectionStep(ship.Direction); var rx = x + step.X * length; var ry = y + step.Y * length;
                    while (rx >= 0 && rx < 14 && ry >= 0 && ry < 18 && grid[rx, ry] < 0) { rx += step.X; ry += step.Y; }
                    var direct = !(rx >= 0 && rx < 14 && ry >= 0 && ry < 18);
                    var neighbors = cells.Sum(c => new[] { new GridPosition(c.X+1,c.Y),new GridPosition(c.X-1,c.Y),new GridPosition(c.X,c.Y+1),new GridPosition(c.X,c.Y-1) }
                        .Count(n => n.X >= 0 && n.X < 14 && n.Y >= 0 && n.Y < 18 && grid[n.X,n.Y] >= 0));
                    var balance = items.Count(s => s.Direction == ship.Direction);
                    var edge = Math.Min(Math.Min(x, 13-x), Math.Min(y,17-y));
                    var blocksRoots = roots.Count(i => cells.Any(c => OnRay(items[i], c)));
                    var rootsAfter = roots.Length - blocksRoots + (direct ? 1 : 0);
                    candidates.Add(Tuple.Create(ship, random.NextDouble() * 4 + neighbors * .5 - balance * .25 + edge * .3 -
                        Math.Abs(rootsAfter - 4) * 8 + (direct ? 0 : 10), direct));
                }
                if (candidates.Count == 0) break;
                ShipPlacementData next = null; List<string> nextRoute = null;
                var ranked = candidates.OrderByDescending(c => c.Item2).ToArray();
                foreach (var candidate in ranked.Take(80).Concat(ranked.Skip(80).Where(c => c.Item3)))
                {
                    level.Ships = items.Concat(new[] { candidate.Item1 }).ToArray();
                    var order = candidate.Item3 ? new[] { candidate.Item1.Id }.Concat(currentRoute).ToList() :
                        currentRoute.Concat(new[] { candidate.Item1.Id }).ToList();
                    try { LevelSolutionProof.Create(level.LevelId, Board(level), order); }
                    catch (ArgumentException) { continue; }
                    next = candidate.Item1; nextRoute = order; if (!candidate.Item3) dependents++; break;
                }
                if (next == null) break;
                items.Add(next); currentRoute = nextRoute;
            }
            level.Ships = items.ToArray();
            Console.WriteLine(level.LevelId + " attempt=" + attempt + " ships=" + items.Count + " dependents=" + dependents);
            if (items.Count != 80 || items.Count(s => s.Length == 3) != 6) continue;
            if (dependents < 30) continue;
            route = currentRoute;
            Require(LevelSolutionProof.Create(level.LevelId, Board(level), route).Replay(level.LevelId, Board(level)).IsComplete, "Dynamic witness");
            Require(!LevelDifficultyAnalysis.Peel(Board(level)).IsComplete, "Dynamic core bypassed");
            Console.WriteLine(level.LevelId + " filled attempt " + attempt + "; dependent followers=" + dependents); return level;
        }
        throw new InvalidOperationException("B80 fill budget exhausted.");
    }

    private static bool OnRay(ShipPlacementData ship, GridPosition cell)
    {
        var d = GridFootprint.DirectionStep(ship.Direction); var dx = cell.X - ship.Position.X; var dy = cell.Y - ship.Position.Y;
        return d.X == 0 ? dx == 0 && dy * d.Y >= ship.Length : dy == 0 && dx * d.X >= ship.Length;
    }

    private static JObject TraceRecord(BoardModel board, string name, IEnumerable<string> route)
    {
        var ids = route.ToArray(); var trace = LevelDifficultyAnalysis.Trace(board, ids);
        var early = trace.Take(Math.Max(1, (int)Math.Ceiling(board.ShipCount * .6))).ToArray();
        return new JObject { ["strategy"] = name, ["shipIds"] = new JArray(ids), ["steps"] = JArray.FromObject(trace),
            ["first60MeanExits"] = early.Length == 0 ? 0 : early.Average(s => s.ExitsBefore),
            ["first60MeanExitShare"] = early.Length == 0 ? 0 : early.Average(s => s.ExitShare),
            ["first60PeakExits"] = early.Length == 0 ? 0 : early.Max(s => s.ExitsBefore) };
    }

    private static void Save(LevelData level, List<string> supplied, string role, JArray mask, LevelData source)
    {
        var board = Board(level);
        var analysis = supplied != null ? LevelDifficultyAnalysis.FromProof(board, level.LevelId, LevelSolutionProof.Create(level.LevelId, board, supplied)) :
            level.Ships.Length > 8 ? LevelDifficultyAnalysis.FindWitness(board) : LevelDifficultyAnalysis.Analyze(board);
        var route = supplied ?? analysis.Solution.ToList(); Require(route.Count > 0, "No verified route " + level.LevelId);
        var proof = LevelSolutionProof.Create(level.LevelId, board, route); Require(proof.Replay(level.LevelId, board).IsComplete, "Invalid proof");
        var traces = new JArray { TraceRecord(board, "verified-witness", route) };
        if (analysis.Kind == PuzzleSolutionKind.DirectExit)
        {
            var graph = new DirectGraph(level);
            for (var i = 0; i < Strategies.Length; i++)
            { var order = graph.Order(i, out _); LevelSolutionProof.Create(level.LevelId, board, order); traces.Add(TraceRecord(board, Strategies[i], order)); }
        }
        var report = new JObject { ["version"] = Version, ["levelId"] = level.LevelId, ["role"] = role,
            ["kind"] = analysis.Kind.ToString(), ["fingerprint"] = LevelStateIdentity.Fingerprint(board),
            ["ships"] = board.ShipCount, ["longShips"] = board.Ships.Count(s => s.Length == 3),
            ["occupiedCells"] = board.OccupiedCellCount, ["boardCells"] = board.Width * board.Height,
            ["placementMask"] = mask, ["maskMeaning"] = "Initial placement only; all board cells remain traversable.",
            ["pureExitResidual"] = analysis.Peeling.RemainingBoard.ShipCount,
            ["minimumPartialLowerBound"] = analysis.MinimumPartialLowerBound, ["minimumPartialUpperBound"] = analysis.MinimumPartialUpperBound,
            ["minimumPartialExact"] = analysis.IsMinimumExact, ["analysisMethod"] = analysis.Method,
            ["releaseTraces"] = traces, ["structure"] = JObject.FromObject(CampaignStructureFeatures.Measure(board)),
            ["humanPlaytest"] = "Pending", ["sourceLevelId"] = source?.LevelId,
            ["sourceLayoutFingerprint"] = source == null ? null : LevelStateIdentity.Fingerprint(Board(source)),
            ["sourceGeometryOverlap"] = source == null ? (double?)null : CampaignPackValidator.GeometryOverlap(Board(source), board) };
        if (role == "dynamic-teaching" || role == "dynamic-risk")
        {
            CheckBridgeQuality(board);
            report["bridgeRevision"] = BridgeRevision;
            report["localQuality"] = BridgeQuality(board);
        }
        Write(level.LevelId + ".json", LevelJsonWriter.Write(level)); Write(level.LevelId + ".solution.json", LevelProofJson.Write(proof));
        Write(level.LevelId + ".analysis.json", report.ToString(Formatting.Indented));
        Rows.Add(new JObject { ["levelId"] = level.LevelId, ["role"] = role, ["kind"] = report["kind"],
            ["layoutSha256"] = CampaignPackValidator.Sha256(File.ReadAllText(Path.Combine(output, level.LevelId + ".json"))),
            ["proofSha256"] = CampaignPackValidator.Sha256(File.ReadAllText(Path.Combine(output, level.LevelId + ".solution.json"))),
            ["analysisSha256"] = CampaignPackValidator.Sha256(File.ReadAllText(Path.Combine(output, level.LevelId + ".analysis.json"))) });
        Console.WriteLine("Saved " + level.LevelId + " " + report["kind"] + " steps=" + route.Count);
    }

    private static void Validate()
    {
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(output, "review-manifest.json"))); var total = 0;
        foreach (var row in manifest["levels"])
        {
            var id = (string)row["levelId"]; var layoutText = File.ReadAllText(Path.Combine(output, id + ".json"));
            var proofText = File.ReadAllText(Path.Combine(output, id + ".solution.json")); var analysisText = File.ReadAllText(Path.Combine(output, id + ".analysis.json"));
            Require(CampaignPackValidator.Sha256(layoutText) == (string)row["layoutSha256"] && CampaignPackValidator.Sha256(proofText) == (string)row["proofSha256"] &&
                CampaignPackValidator.Sha256(analysisText) == (string)row["analysisSha256"], "Manifest hash mismatch " + id);
            var level = LevelJsonReader.Read(layoutText); var board = Board(level); var proof = LevelProofJson.Read(proofText);
            Require(proof.Replay(id, board).IsComplete, "Proof replay " + id);
            var a = JObject.Parse(analysisText); var peel = LevelDifficultyAnalysis.Peel(board);
            if ((string)manifest["bridgeRevision"] == BridgeRevision && ((string)a["role"] == "dynamic-teaching" || (string)a["role"] == "dynamic-risk"))
            {
                CheckBridgeQuality(board);
                Require((string)a["bridgeRevision"] == BridgeRevision && JToken.DeepEquals(a["localQuality"], BridgeQuality(board)), "Bridge quality report drift " + id);
            }
            if ((string)a["role"] == "release")
            {
                var source = LevelJsonReader.Read(File.ReadAllText(Path.Combine(root, "Assets/Tidebound/Config/Levels/Campaign", Path.GetFileName((string)a["sourceLevelId"]) + ".json")));
                var old = Board(source);
                Require(level.Ships.All(s => new HashSet<GridPosition>(old.GetShip(s.Id).OccupiedCells)
                    .SetEquals(GridFootprint.Cells(s.Position, s.Direction, s.Length))), "Release contrast changed a ship footprint " + id);
            }
            Require(peel.RemainingBoard.ShipCount == (int)a["pureExitResidual"], "Peeling drift " + id);
            Require((string)a["kind"] == (peel.IsComplete ? "DirectExit" : "RequiresPartial"), "A/B classification " + id);
            if (a["placementMask"] is JArray mask)
            {
                var cells = new HashSet<GridPosition>(mask.Select(c => new GridPosition((int)c[0], (int)c[1])));
                Require(board.Ships.All(s => s.OccupiedCells.All(cells.Contains)), "Placement mask violation " + id);
            }
            foreach (var trace in a["releaseTraces"])
            {
                var ids = trace["shipIds"].Values<string>().ToArray(); LevelSolutionProof.Create(id, board, ids);
                var actual = TraceRecord(board, (string)trace["strategy"], ids);
                Require(JToken.DeepEquals(actual, trace), "Release trace drift " + id);
            }
            Require(board.ShipCount <= 8 || board.ShipCount == 80 && board.Ships.Count(s => s.Length == 3) == 6, "Full scale count " + id);
            total += proof.Steps.Count;
        }
        Require(manifest["levels"].Count() == 10, "Expected six A contrasts, two small prototypes, two full dynamic bridges.");
        Console.WriteLine("Verified 10 review levels, " + total + " effective proof transactions. Human/device review pending.");
    }
    private static void Write(string name, string text)
    {
        var file = Path.Combine(output, name); var temp = file + ".writing";
        File.WriteAllText(temp, text.TrimEnd() + "\n");
        if (File.Exists(file)) File.Replace(temp, file, null); else File.Move(temp, file);
    }
}
