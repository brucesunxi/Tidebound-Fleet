using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Ship;

internal static partial class DifficultyPrototypePipeline
{
    private const string BridgeRevision = "MixedBridgeV2";

    // Explicit, staged replacement of the two rejected REVIEW layouts only. No campaign or save IDs change.
    private static void RefineBridges()
    {
        Validate();
        var destination = output;
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(output, "review-manifest.json")));
        Require((string)manifest["bridgeRevision"] != BridgeRevision, "This bridge revision is already installed.");
        var stage = Path.Combine(Path.GetTempPath(), "TideboundBridge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try
        {
            foreach (var file in Directory.GetFiles(destination, "*.json")) File.Copy(file, Path.Combine(stage, Path.GetFileName(file)));
            output = stage; Rows.Clear();
            foreach (var id in new[] { "DV2_B80_T", "DV2_B80_R" })
            {
                var source = LevelJsonReader.Read(File.ReadAllText(Path.Combine(stage, id + ".json")));
                var proof = LevelProofJson.Read(File.ReadAllText(Path.Combine(stage, id + ".solution.json")));
                var route = proof.Steps.Select(s => s.ShipId).ToList();
                Console.WriteLine(id + " before " + BridgeQuality(Board(source)).ToString(Formatting.None));
                var revised = MixBridge(source, route, id.EndsWith("_T", StringComparison.Ordinal) ? 279271 : 279272);
                Save(revised, route, id.EndsWith("_T", StringComparison.Ordinal) ? "dynamic-teaching" : "dynamic-risk", null, null);
                var file = id + ".analysis.json"; var report = JObject.Parse(File.ReadAllText(Path.Combine(stage, file)));
                report["bridgeRevision"] = BridgeRevision;
                report["localQuality"] = BridgeQuality(Board(revised));
                report["replacesFingerprint"] = LevelStateIdentity.Fingerprint(Board(source));
                Write(file, report.ToString(Formatting.Indented));
                var row = Rows.Last(); row["analysisSha256"] = CampaignPackValidator.Sha256(File.ReadAllText(Path.Combine(stage, file)));
                manifest["levels"].First(r => (string)r["levelId"] == id).Replace(row);
                Console.WriteLine(id + " after " + report["localQuality"].ToString(Formatting.None));
            }
            manifest["bridgeRevision"] = BridgeRevision;
            Write("review-manifest.json", manifest.ToString(Formatting.Indented));
            Validate();
            // Certification is complete before any existing asset is touched. Keep asset GUIDs and scene bindings.
            output = destination;
            foreach (var row in Rows)
                foreach (var suffix in new[] { ".json", ".solution.json", ".analysis.json" })
                { var name = (string)row["levelId"] + suffix; Write(name, File.ReadAllText(Path.Combine(stage, name))); }
            Write("review-manifest.json", manifest.ToString(Formatting.Indented));
            Validate();
        }
        finally { output = destination; Directory.Delete(stage, true); }
    }

    private static JObject BridgeQuality(BoardModel board)
    {
        var local = LocalLayoutAnalyzer.Analyze(board);
        return new JObject {
            ["adjacentRun"] = Math.Max(local.LongestAdjacentRow?.ShipCount ?? 0, local.LongestAdjacentColumn?.ShipCount ?? 0),
            ["oneGapRun"] = Math.Max(local.LongestGappedRow?.ShipCount ?? 0, local.LongestGappedColumn?.ShipCount ?? 0),
            ["largestEmptyRectangle"] = local.LargestEmptyArea,
            ["minimumWindowEntropy"] = local.MinimumWindowEntropy,
            ["p10WindowEntropy"] = local.P10WindowEntropy,
            ["initialExits"] = board.Ships.Count(s => board.QueryForwardPath(s.Id).CanExit)
        };
    }

    private static void CheckBridgeQuality(BoardModel board)
    {
        var q = BridgeQuality(board);
        Require((int)q["oneGapRun"] <= 2, "Bridge contains three or more same-direction ships in a row/column (including one-cell gaps).");
        Require((int)q["largestEmptyRectangle"] <= 6, "Bridge contains an oversized empty rectangle.");
        Require((double?)q["minimumWindowEntropy"] >= .65 && (double?)q["p10WindowEntropy"] >= .75, "Bridge has a poorly mixed local direction window.");
        Require((int)q["initialExits"] >= 4 && (int)q["initialExits"] <= 8, "Bridge initial exit count is outside the review target.");
    }

    private static LevelData MixBridge(LevelData source, IList<string> route, int seed)
    {
        var level = Clone(source); var random = new Random(seed);
        var routeIndices = route.Select(id => Array.FindIndex(level.Ships, s => s.Id == id)).ToArray();
        var initial = new BridgeGrid(level); var cost = initial.Cost(); var bestCost = cost;
        var best = Clone(level); var accepted = 0;
        for (var iteration = 0; iteration < 4000000; iteration++)
        {
            if (iteration > 0 && iteration % 50000 == 0)
            {
                Console.WriteLine(level.LevelId + " mix " + iteration + " best=" + bestCost.ToString("0.00") + " accepted=" + accepted);
                level = Clone(best); initial = new BridgeGrid(level); cost = bestCost;
            }
            var index = random.Next(level.Ships.Length); var old = level.Ships[index];
            // The four/eight authored relation ships retain their positions; fillers may use any legal cells.
            if (!old.Id.StartsWith("Z", StringComparison.Ordinal)) continue;
            var mode = random.Next(10); var direction = (ShipDirection)random.Next(4);
            var x = old.Position.X; var y = old.Position.Y;
            if (mode < 3)
            {
                var head = GridFootprint.Cells(old.Position, old.Direction, old.Length).Last();
                direction = old.Direction == ShipDirection.Up ? ShipDirection.Down : old.Direction == ShipDirection.Down ? ShipDirection.Up :
                    old.Direction == ShipDirection.Left ? ShipDirection.Right : ShipDirection.Left;
                x = head.X; y = head.Y;
            }
            else if (mode < 8) { x += random.Next(-3, 4); y += random.Next(-3, 4); }
            else { x = random.Next(level.Width); y = random.Next(level.Height); }
            var replacement = Ship(old.Id, x, y, direction, old.Length);
            if (!initial.Fits(replacement, index)) continue;
            level.Ships[index] = replacement;
            var candidate = new BridgeGrid(level); var nextCost = candidate.Cost();
            var temperature = 3.5 * (1 - (iteration % 50000) / 50000d) + .08;
            if (nextCost > cost && random.NextDouble() >= Math.Exp((cost - nextCost) / temperature) || !candidate.Replays(routeIndices))
            { level.Ships[index] = old; continue; }
            initial = candidate; cost = nextCost; accepted++;
            if (cost < bestCost)
            {
                bestCost = cost; best = Clone(level);
                if (candidate.Passes())
                {
                    var board = Board(best); CheckBridgeQuality(board);
                    Require(LevelSolutionProof.Create(best.LevelId, board, route).Replay(best.LevelId, board).IsComplete, "Mixed witness did not replay in production Core.");
                    Require(!LevelDifficultyAnalysis.Peel(board).IsComplete, "Mixed bridge lost its dynamic relation.");
                    Console.WriteLine(best.LevelId + " mixed after " + iteration + " mutations"); return best;
                }
            }
        }
        throw new InvalidOperationException("Bridge mixing budget exhausted: " + source.LevelId + " " + bestCost + " " + BridgeQuality(Board(best)));
    }

    // Allocation-light candidate screening only. Final acceptance uses production Core transactions and LocalLayoutAnalyzer.
    private sealed partial class BridgeGrid
    {
        private readonly LevelData level;
        private readonly int[] grid, xs, ys, dx, dy, lengths, directions;
        private int maxRun, largestEmpty, exits;
        private double minEntropy, p10;
        public BridgeGrid(LevelData value)
        {
            level = value; var n = value.Ships.Length;
            grid = Enumerable.Repeat(-1, value.Width * value.Height).ToArray();
            xs = new int[n]; ys = new int[n]; dx = new int[n]; dy = new int[n]; lengths = new int[n]; directions = new int[n];
            for (var i = 0; i < n; i++)
            {
                var s = value.Ships[i]; var d = GridFootprint.DirectionStep(s.Direction);
                xs[i] = s.Position.X; ys[i] = s.Position.Y; dx[i] = d.X; dy[i] = d.Y; lengths[i] = s.Length; directions[i] = (int)s.Direction;
                for (var k = 0; k < s.Length; k++) grid[(ys[i] + k * dy[i]) * value.Width + xs[i] + k * dx[i]] = i;
            }
        }
        private bool Inside(int x, int y) => x >= 0 && x < level.Width && y >= 0 && y < level.Height;
        public bool Fits(ShipPlacementData ship, int replacing)
        {
            var d = GridFootprint.DirectionStep(ship.Direction);
            for (var k = 0; k < ship.Length; k++)
            {
                var x = ship.Position.X + k * d.X; var y = ship.Position.Y + k * d.Y;
                if (!Inside(x, y) || grid[y * level.Width + x] != -1 && grid[y * level.Width + x] != replacing) return false;
            }
            return true;
        }
        public bool Passes() => maxRun <= 2 && largestEmpty <= 6 && minEntropy >= .65 && p10 >= .75 && exits >= 4 && exits <= 8;
        public double Cost()
        {
            double penalty = 0; maxRun = 0; exits = 0;
            for (var axis = 0; axis < 2; axis++)
            {
                var rows = axis == 0; var lines = rows ? level.Height : level.Width; var length = rows ? level.Width : level.Height;
                for (var line = 0; line < lines; line++)
                {
                    var last = -1; var lastDirection = -1; var run = 0; var gap = 0;
                    for (var k = 0; k < length; k++)
                    {
                        var i = grid[(rows ? line : k) * level.Width + (rows ? k : line)];
                        if (i < 0) { if (++gap > 1) { run = 0; last = -1; lastDirection = -1; } continue; }
                        gap = 0;
                        if (directions[i] != lastDirection) run = 0;
                        if (last != i) run++;
                        maxRun = Math.Max(maxRun, run); penalty += Math.Max(0, run - 2) * 12;
                        last = i; lastDirection = directions[i];
                    }
                }
            }
            var entropies = new List<double>(); var seen = new int[level.Ships.Length]; var stamp = 0;
            for (var y = 0; y + 4 <= level.Height; y++) for (var x = 0; x + 4 <= level.Width; x++)
            {
                stamp++; var counts = new int[4]; var total = 0;
                for (var sy = y; sy < y + 4; sy++) for (var sx = x; sx < x + 4; sx++)
                {
                    var i = grid[sy * level.Width + sx]; if (i < 0 || seen[i] == stamp) continue;
                    seen[i] = stamp; counts[directions[i]]++; total++;
                }
                if (total < 4) continue;
                double entropy = 0;
                foreach (var count in counts) if (count > 0) { var p = count / (double)total; entropy -= p * Math.Log(p, 2) / 2; }
                entropies.Add(entropy); penalty += Math.Max(0, .8 - entropy) * 12;
            }
            entropies.Sort(); minEntropy = entropies.Count == 0 ? 0 : entropies[0];
            p10 = entropies.Count == 0 ? 0 : entropies[(int)Math.Ceiling(entropies.Count * .1) - 1];
            penalty += Math.Max(0, .65 - minEntropy) * 200 + Math.Max(0, .78 - p10) * 200;
            largestEmpty = 0;
            for (var bottom = 0; bottom < level.Height; bottom++)
            {
                var clear = Enumerable.Repeat(true, level.Width).ToArray();
                for (var top = bottom; top < level.Height; top++)
                {
                    var width = 0;
                    for (var x = 0; x < level.Width; x++)
                    {
                        clear[x] &= grid[top * level.Width + x] < 0; width = clear[x] ? width + 1 : 0;
                        var area = width * (top - bottom + 1); largestEmpty = Math.Max(largestEmpty, area);
                        penalty += Math.Max(0, area - 6) * .5;
                    }
                }
            }
            for (var i = 0; i < xs.Length; i++)
            {
                var x = xs[i] + dx[i] * lengths[i]; var y = ys[i] + dy[i] * lengths[i];
                while (Inside(x, y) && grid[y * level.Width + x] < 0) { x += dx[i]; y += dy[i]; }
                if (!Inside(x, y)) exits++;
            }
            penalty += Math.Abs(exits - 6) * 5;
            return penalty;
        }
        public bool Replays(int[] route)
        {
            var occupancy = (int[])grid.Clone(); var px = (int[])xs.Clone(); var py = (int[])ys.Clone(); var gone = new bool[xs.Length];
            foreach (var i in route)
            {
                if (gone[i]) return false;
                var x = px[i] + dx[i] * lengths[i]; var y = py[i] + dy[i] * lengths[i]; var distance = 0;
                while (Inside(x, y) && occupancy[y * level.Width + x] < 0) { x += dx[i]; y += dy[i]; distance++; }
                var exit = !Inside(x, y); if (!exit && distance == 0) return false;
                for (var k = 0; k < lengths[i]; k++) occupancy[(py[i] + k * dy[i]) * level.Width + px[i] + k * dx[i]] = -1;
                if (exit) gone[i] = true;
                else
                {
                    px[i] += dx[i] * distance; py[i] += dy[i] * distance;
                    for (var k = 0; k < lengths[i]; k++) occupancy[(py[i] + k * dy[i]) * level.Width + px[i] + k * dx[i]] = i;
                }
            }
            return gone.All(x => x);
        }
    }
}
