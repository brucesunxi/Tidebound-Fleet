using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static class LocalLayoutDiagnostics
    {
        public static string Describe(BoardModel board, LocalLayoutOptions options = null)
        {
            var report = LocalLayoutAnalyzer.Analyze(board, options);
            var text = new StringBuilder();
            text.AppendLine(report.AnalyzerVersion + " — diagnostics only; no product pass/fail thresholds.");
            text.AppendLine("Area " + Rect(report.Area) + "; coordinates are zero-based, origin bottom-left.");
            text.AppendLine("Adjacent row: " + Run(report.LongestAdjacentRow));
            text.AppendLine("Adjacent column: " + Run(report.LongestAdjacentColumn));
            text.AppendLine("Gapped row (at most " + report.AllowedGapCells + " empty cells per gap): " + Run(report.LongestGappedRow));
            text.AppendLine("Gapped column: " + Run(report.LongestGappedColumn));
            text.AppendLine("Windows " + report.WindowSize + "x" + report.WindowSize + ", minimum " + report.MinimumWindowShips +
                " distinct intersecting ships; eligible/total=" + report.Windows.Count + "/" + report.TotalWindowCount +
                ", sparse=" + report.SparseWindowCount + ", empty=" + report.EmptyWindowCount + ".");
            text.AppendLine("Local entropy min=" + Number(report.MinimumWindowEntropy) + "; p10=" + Number(report.P10WindowEntropy) +
                (report.LeastMixedWindow == null ? "; no eligible window." : "; least mixed at " + Rect(report.LeastMixedWindow.Area) + "."));
            text.AppendLine("Largest empty rectangle: " + Rect(report.LargestEmptyRectangle) + "; cells=" + report.LargestEmptyArea + ".");
            foreach (var region in report.Regions)
                text.AppendLine("Region " + Rect(region.Area) + ": occupied=" + region.OccupiedCellCount +
                    "; ratio=" + Number(region.OccupancyRatio) + ".");
            return text.ToString().TrimEnd();
        }

        [MenuItem("Tools/Tidebound/Export I1 Local Layout Audit")]
        public static void ExportI1Baseline()
        {
            var text = new StringBuilder("# I1 local layout baseline\n\n");
            text.AppendLine("Generated UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            text.AppendLine("\nDiagnostic evidence only. These samples remain AlgorithmPrototypeOnly; no acceptance thresholds are frozen.\n");
            foreach (var id in new[] { "P5R_Rebuild_001", "P5R_Rebuild_002" })
            {
                var path = Phase5RAlgorithmExport.Folder + "/" + id + ".json";
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null) throw new InvalidOperationException("Missing baseline: " + path);
                var level = LevelJsonReader.Read(asset.text);
                using (var session = LevelSessionFactory.Create(level,
                    new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                    new[] { new BossDefinition("TF_KRAKEN_01") }))
                {
                    var board = session.InitialBoard;
                    text.AppendLine("## " + id + "\n");
                    text.AppendLine("Source: `" + path + "`\n");
                    text.AppendLine("Layout fingerprint: `" + LevelStateIdentity.Fingerprint(board) + "`\n");
                    text.AppendLine("```text\n" + Describe(board) + "\n```\n");
                    if (id == "P5R_Rebuild_001")
                    {
                        text.AppendLine("Tutorial placement area (reserved water excluded):\n");
                        text.AppendLine("```text\n" + Describe(board,
                            new LocalLayoutOptions(ReverseGenerationProfiles.Tutorial.Area)) + "\n```\n");
                    }
                }
            }
            var output = Path.Combine(Path.GetTempPath(), "TideboundI2_LayoutAudit.md");
            File.WriteAllText(output, text.ToString().TrimEnd() + "\n", new UTF8Encoding(false));
            Debug.Log("Tidebound diagnostic report: " + output);
        }

        private static string Number(double? value) => value.HasValue
            ? value.Value.ToString("0.0000", CultureInfo.InvariantCulture) : "N/A";
        private static string Rect(GenerationArea area) =>
            FormattableString.Invariant($"({area.X},{area.Y}) {area.Width}x{area.Height}");
        private static string Run(DirectionalLayoutRun run) => run == null ? "none" :
            FormattableString.Invariant($"{run.ShipCount} ships, {run.Direction}, ({run.Start.X},{run.Start.Y})→({run.End.X},{run.End.Y}); ") +
            string.Join(",", run.ShipIds);
    }
}
