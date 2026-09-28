using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public enum CampaignStructure { Legacy, Flow, Layers, Crossed, KeyUnlock, LinkedRegions, LongBridge }
    public enum CampaignPace { Relief, Regular, Challenge }

    /// <summary>Content targets, not a claim about measured human difficulty.</summary>
    public sealed class CampaignLevelDesign
    {
        public int Number { get; }
        public CampaignStructure Structure { get; }
        public CampaignPace Pace { get; }
        public LevelRecipe Recipe { get; }
        public CampaignLevelDesign(int number, CampaignStructure structure, CampaignPace pace, LevelRecipe recipe)
        { Number = number; Structure = structure; Pace = pace; Recipe = recipe; }

        public IReadOnlyList<string> CheckStructure(BoardModel board)
        {
            var f = CampaignStructureFeatures.Measure(board);
            var issues = new List<string>();
            switch (Structure)
            {
                case CampaignStructure.Flow: if (f.InitialExits < 14) issues.Add("FLOW_EXITS"); break;
                case CampaignStructure.Layers: if (f.DeepShips < 8) issues.Add("LAYERED_SHIPS"); break;
                case CampaignStructure.Crossed: if (f.CrossAxisRatio < 0.30) issues.Add("CROSS_AXIS"); break;
                case CampaignStructure.KeyUnlock: if (f.MaximumInitialUnlock < 2) issues.Add("KEY_UNLOCK"); break;
                case CampaignStructure.LinkedRegions: if (f.CrossRegionRatio < 0.20) issues.Add("CROSS_REGION"); break;
                case CampaignStructure.LongBridge: if (f.BridgingLongShips < 3) issues.Add("LONG_BRIDGES"); break;
            }
            return issues.AsReadOnly();
        }
    }

    public sealed class CampaignStructureFeatures
    {
        public int InitialExits { get; private set; }
        public int DeepShips { get; private set; }
        public double CrossAxisRatio { get; private set; }
        public double CrossRegionRatio { get; private set; }
        public int MaximumInitialUnlock { get; private set; }
        public int BridgingLongShips { get; private set; }
        public static CampaignStructureFeatures Measure(BoardModel board)
        {
            var graph = BoardDependencyAnalyzer.Analyze(board);
            var ships = board.Ships.ToDictionary(s => s.Id, StringComparer.Ordinal);
            var depths = new Dictionary<string, int>();
            Func<string, int> depth = null;
            depth = id => depths.TryGetValue(id, out var value) ? value :
                (depths[id] = 1 + graph.GetNode(id).AllBlockerShipIds.Select(depth).DefaultIfEmpty(0).Max());
            Func<string, bool> horizontal = id => GridFootprint.DirectionStep(ships[id].Direction).X != 0;
            Func<string, int> region = id => (ships[id].OccupiedCells.Average(c => c.X) >= board.Width / 2.0 ? 1 : 0) +
                (ships[id].OccupiedCells.Average(c => c.Y) >= board.Height / 2.0 ? 2 : 0);
            var edges = graph.Nodes.SelectMany(n => n.AllBlockerShipIds.Select(b => new { From = n.ShipId, Blocker = b })).ToArray();
            var exits = new HashSet<string>(graph.Nodes.Where(n => n.CanExit).Select(n => n.ShipId));
            return new CampaignStructureFeatures
            {
                InitialExits = exits.Count,
                DeepShips = graph.CompleteCycles.Count == 0 ? ships.Keys.Count(id => depth(id) >= 8) : 0,
                CrossAxisRatio = edges.Length == 0 ? 0 : edges.Count(e => horizontal(e.From) != horizontal(e.Blocker)) / (double)edges.Length,
                CrossRegionRatio = edges.Length == 0 ? 0 : edges.Count(e => region(e.From) != region(e.Blocker)) / (double)edges.Length,
                MaximumInitialUnlock = exits.Select(id => graph.Nodes.Count(n => !n.CanExit &&
                    n.AllBlockerShipIds.Count == 1 && n.AllBlockerShipIds[0] == id)).DefaultIfEmpty(0).Max(),
                BridgingLongShips = ships.Keys.Count(id => ships[id].Length == 3 && edges.Any(e =>
                    e.Blocker == id && horizontal(e.From) != horizontal(id)))
            };
        }
    }

    public static class CampaignLevelPlan
    {
        public const string Version = "Campaign100V1";
        public const string ScreeningVersion = "CampaignStructureV1";
        public static readonly IReadOnlyList<CampaignLevelDesign> All = Build().AsReadOnly();
        private static List<CampaignLevelDesign> Build()
        {
            var result = Phase5RLevelRecipes.All.Select((r, i) => new CampaignLevelDesign(i + 1,
                CampaignStructure.Legacy, i == 0 || i == 5 ? CampaignPace.Relief : CampaignPace.Regular, r)).ToList();
            // Explicit first thirty: recovery after L10, two five-level arcs per chapter; rewards at 15/25 land on relief.
            var families = new[] { CampaignStructure.Flow, CampaignStructure.Crossed, CampaignStructure.KeyUnlock,
                CampaignStructure.Layers, CampaignStructure.Flow, CampaignStructure.LinkedRegions,
                CampaignStructure.LongBridge, CampaignStructure.KeyUnlock, CampaignStructure.Crossed, CampaignStructure.LongBridge,
                CampaignStructure.Flow, CampaignStructure.LinkedRegions, CampaignStructure.KeyUnlock,
                CampaignStructure.Layers, CampaignStructure.Flow, CampaignStructure.Crossed,
                CampaignStructure.LongBridge, CampaignStructure.LinkedRegions, CampaignStructure.KeyUnlock, CampaignStructure.LongBridge };
            for (var n = 11; n <= 100; n++)
            {
                var beat = (n - 1) % 10;
                var pace = beat == 0 || beat == 4 ? CampaignPace.Relief :
                    beat == 3 || beat == 7 || beat == 9 ? CampaignPace.Challenge : CampaignPace.Regular;
                var cycle = new[] { CampaignStructure.Flow, CampaignStructure.Crossed, CampaignStructure.KeyUnlock,
                    CampaignStructure.Layers, CampaignStructure.Flow, CampaignStructure.LinkedRegions,
                    CampaignStructure.LongBridge, CampaignStructure.KeyUnlock, CampaignStructure.Crossed, CampaignStructure.LongBridge };
                var family = n <= 30 ? families[n - 11] : cycle[beat];
                // Later chapters rotate regular/challenge families; never scale density or remove relief beats.
                if (n > 30 && family != CampaignStructure.Flow)
                {
                    var rotation = new[] { CampaignStructure.Crossed, CampaignStructure.KeyUnlock, CampaignStructure.Layers,
                        CampaignStructure.LinkedRegions, CampaignStructure.LongBridge };
                    family = rotation[(Array.IndexOf(rotation, family) + (n - 31) / 10) % rotation.Length];
                }
                int minExit = pace == CampaignPace.Relief ? 14 : pace == CampaignPace.Challenge ? 6 : 10;
                int maxExit = pace == CampaignPace.Relief ? 18 : pace == CampaignPace.Challenge ? 11 : 15;
                int minDepth = pace == CampaignPace.Relief ? 6 : pace == CampaignPace.Challenge ? 11 : 8;
                int maxDepth = pace == CampaignPace.Relief ? 10 : pace == CampaignPace.Challenge ? 18 : 13;
                var longs = family == CampaignStructure.LongBridge ? 8 : pace == CampaignPace.Relief ? 4 : 6;
                var id = "TF_Campaign_" + n.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                var profile = new ReverseGenerationProfile(id, 14, 18, 80, longs, minExit, maxExit, minDepth, maxDepth,
                    26, maxDirectionClustering: 0.55);
                result.Add(new CampaignLevelDesign(n, family, pace, new LevelRecipe(id, profile, 4, 0.30, 0.65, 12)));
            }
            return result;
        }
    }
}
