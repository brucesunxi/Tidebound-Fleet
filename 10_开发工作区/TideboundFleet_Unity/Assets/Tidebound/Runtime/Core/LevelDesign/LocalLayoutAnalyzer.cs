using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.LevelDesign
{
    public enum LayoutScanAxis { Row, Column }

    /// <summary>Diagnostic parameters only; none of these values are product acceptance thresholds.</summary>
    public sealed class LocalLayoutOptions
    {
        public GenerationArea? Area { get; }
        public int WindowSize { get; }
        public int MinimumWindowShips { get; }
        public int AllowedGapCells { get; }

        public LocalLayoutOptions(GenerationArea? area = null, int windowSize = 4,
            int minimumWindowShips = 4, int allowedGapCells = 1)
        {
            Area = area; WindowSize = windowSize;
            MinimumWindowShips = minimumWindowShips; AllowedGapCells = allowedGapCells;
        }
    }

    public sealed class DirectionalLayoutRun
    {
        public LayoutScanAxis Axis { get; }
        public GridPosition Start { get; }
        public GridPosition End { get; }
        public ShipDirection Direction { get; }
        public IReadOnlyList<string> ShipIds { get; }
        public int ShipCount => ShipIds.Count;

        internal DirectionalLayoutRun(LayoutScanAxis axis, GridPosition start, GridPosition end,
            ShipDirection direction, IEnumerable<string> ids)
        {
            Axis = axis; Start = start; End = end; Direction = direction;
            ShipIds = Array.AsReadOnly(ids.ToArray());
        }
    }

    public sealed class DirectionWindow
    {
        public GenerationArea Area { get; }
        public int ShipCount { get; }
        public double Entropy { get; }
        internal DirectionWindow(GenerationArea area, int count, double entropy)
        { Area = area; ShipCount = count; Entropy = entropy; }
    }

    public sealed class LayoutRegionOccupancy
    {
        public GenerationArea Area { get; }
        public int OccupiedCellCount { get; }
        public double OccupancyRatio => (double)OccupiedCellCount / (Area.Width * Area.Height);
        internal LayoutRegionOccupancy(GenerationArea area, int occupied)
        { Area = area; OccupiedCellCount = occupied; }
    }

    /// <summary>Read-only spatial evidence. Empty/sparse windows have no entropy score, not a passing score.</summary>
    public sealed class LocalLayoutReport
    {
        public string AnalyzerVersion => LocalLayoutAnalyzer.Version;
        public GenerationArea Area { get; }
        public int WindowSize { get; }
        public int MinimumWindowShips { get; }
        public int AllowedGapCells { get; }
        public DirectionalLayoutRun LongestAdjacentRow { get; }
        public DirectionalLayoutRun LongestAdjacentColumn { get; }
        public DirectionalLayoutRun LongestGappedRow { get; }
        public DirectionalLayoutRun LongestGappedColumn { get; }
        public IReadOnlyList<DirectionWindow> Windows { get; }
        public int EmptyWindowCount { get; }
        public int SparseWindowCount { get; }
        public int TotalWindowCount => Windows.Count + EmptyWindowCount + SparseWindowCount;
        public double? MinimumWindowEntropy { get; }
        public double? P10WindowEntropy { get; }
        public DirectionWindow LeastMixedWindow { get; }
        public GenerationArea LargestEmptyRectangle { get; }
        public int LargestEmptyArea => LargestEmptyRectangle.Width * LargestEmptyRectangle.Height;
        public IReadOnlyList<LayoutRegionOccupancy> Regions { get; }

        internal LocalLayoutReport(GenerationArea area, LocalLayoutOptions options,
            DirectionalLayoutRun row, DirectionalLayoutRun column,
            DirectionalLayoutRun gappedRow, DirectionalLayoutRun gappedColumn,
            IList<DirectionWindow> windows, int emptyWindows, int sparseWindows,
            GenerationArea emptyRectangle, IList<LayoutRegionOccupancy> regions)
        {
            Area = area; WindowSize = options.WindowSize; MinimumWindowShips = options.MinimumWindowShips;
            AllowedGapCells = options.AllowedGapCells;
            LongestAdjacentRow = row; LongestAdjacentColumn = column;
            LongestGappedRow = gappedRow; LongestGappedColumn = gappedColumn;
            Windows = Array.AsReadOnly(windows.ToArray());
            EmptyWindowCount = emptyWindows; SparseWindowCount = sparseWindows;
            var sorted = windows.OrderBy(w => w.Entropy).ThenBy(w => w.Area.Y).ThenBy(w => w.Area.X).ToArray();
            if (sorted.Length > 0)
            {
                LeastMixedWindow = sorted[0]; MinimumWindowEntropy = sorted[0].Entropy;
                // Nearest-rank 10th percentile; deterministic even with very few eligible windows.
                P10WindowEntropy = sorted[(int)Math.Ceiling(sorted.Length * 0.1) - 1].Entropy;
            }
            LargestEmptyRectangle = emptyRectangle;
            Regions = Array.AsReadOnly(regions.ToArray());
        }
    }

    public static class LocalLayoutAnalyzer
    {
        public const string Version = "LocalLayoutV1";

        public static LocalLayoutReport Analyze(BoardModel board, LocalLayoutOptions options = null)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            options = options ?? new LocalLayoutOptions();
            var area = options.Area ?? new GenerationArea(0, 0, board.Width, board.Height);
            if (area.X < 0 || area.Y < 0 || area.Width <= 0 || area.Height <= 0 ||
                (long)area.X + area.Width > board.Width || (long)area.Y + area.Height > board.Height ||
                options.WindowSize < 1 || options.WindowSize >
                Math.Max(FoundationLimits.MaxTechnicalBoardWidth, FoundationLimits.MaxTechnicalBoardHeight) ||
                options.MinimumWindowShips < 1 || options.MinimumWindowShips >
                (long)options.WindowSize * options.WindowSize || options.AllowedGapCells < 0 ||
                options.AllowedGapCells > Math.Max(board.Width, board.Height))
                throw new ArgumentException("Invalid diagnostic area, window, minimum ship count or gap.", nameof(options));

            var windows = new List<DirectionWindow>();
            var empty = 0; var sparse = 0;
            for (var y = area.Y; y + options.WindowSize <= area.Y + area.Height; y++)
            for (var x = area.X; x + options.WindowSize <= area.X + area.Width; x++)
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                for (var dy = 0; dy < options.WindowSize; dy++)
                for (var dx = 0; dx < options.WindowSize; dx++)
                {
                    var id = board.GetShipId(new GridPosition(x + dx, y + dy));
                    if (id != null) ids.Add(id);
                }
                if (ids.Count == 0) { empty++; continue; }
                if (ids.Count < options.MinimumWindowShips) { sparse++; continue; }
                var counts = new int[4];
                foreach (var id in ids) counts[(int)board.GetShip(id).Direction]++;
                var entropy = 0d;
                foreach (var count in counts)
                {
                    if (count == 0) continue;
                    var p = (double)count / ids.Count;
                    entropy -= p * Math.Log(p, 2) / 2;
                }
                windows.Add(new DirectionWindow(new GenerationArea(x, y, options.WindowSize, options.WindowSize),
                    ids.Count, entropy));
            }

            return new LocalLayoutReport(area, options,
                ScanRuns(board, area, LayoutScanAxis.Row, 0), ScanRuns(board, area, LayoutScanAxis.Column, 0),
                ScanRuns(board, area, LayoutScanAxis.Row, options.AllowedGapCells),
                ScanRuns(board, area, LayoutScanAxis.Column, options.AllowedGapCells),
                windows, empty, sparse, LargestEmpty(board, area), Regions(board, area));
        }

        private static DirectionalLayoutRun ScanRuns(BoardModel board, GenerationArea area,
            LayoutScanAxis axis, int allowedGap)
        {
            DirectionalLayoutRun best = null;
            var rows = axis == LayoutScanAxis.Row;
            var lines = rows ? area.Height : area.Width;
            var length = rows ? area.Width : area.Height;
            for (var line = 0; line < lines; line++)
            {
                var ids = new List<string>();
                var direction = ShipDirection.Up;
                var start = new GridPosition();
                var gap = 0;
                for (var k = 0; k < length; k++)
                {
                    var cell = new GridPosition(area.X + (rows ? k : line), area.Y + (rows ? line : k));
                    var id = board.GetShipId(cell);
                    if (id == null)
                    {
                        if (++gap > allowedGap) ids.Clear();
                        continue;
                    }
                    var nextDirection = board.GetShip(id).Direction;
                    if (ids.Count == 0 || direction != nextDirection)
                    { ids.Clear(); start = cell; direction = nextDirection; }
                    gap = 0;
                    if (!ids.Contains(id)) ids.Add(id);
                    // Update the endpoint through the last footprint cell of the same ship as well.
                    if (best == null || ids.Count > best.ShipCount ||
                        (ids.Count == best.ShipCount && best.Axis == axis && best.Start.Equals(start)))
                        best = new DirectionalLayoutRun(axis, start, cell, direction, ids);
                }
            }
            return best;
        }

        private static GenerationArea LargestEmpty(BoardModel board, GenerationArea area)
        {
            var best = new GenerationArea(area.X, area.Y, 0, 0);
            for (var bottom = area.Y; bottom < area.Y + area.Height; bottom++)
            {
                var clearColumns = Enumerable.Repeat(true, area.Width).ToArray();
                for (var top = bottom; top < area.Y + area.Height; top++)
                {
                    var width = 0;
                    for (var x = 0; x < area.Width; x++)
                    {
                        clearColumns[x] &= board.GetShipId(new GridPosition(area.X + x, top)) == null;
                        width = clearColumns[x] ? width + 1 : 0;
                        var height = top - bottom + 1;
                        if (width * height > best.Width * best.Height)
                            best = new GenerationArea(area.X + x - width + 1, bottom, width, height);
                    }
                }
            }
            return best;
        }

        private static IList<LayoutRegionOccupancy> Regions(BoardModel board, GenerationArea area)
        {
            var result = new List<LayoutRegionOccupancy>();
            // Left/bottom receive floor(size/2); omit zero-size regions on narrow boards.
            var xs = new[] { area.X, area.X + area.Width / 2, area.X + area.Width };
            var ys = new[] { area.Y, area.Y + area.Height / 2, area.Y + area.Height };
            for (var iy = 0; iy < 2; iy++)
            for (var ix = 0; ix < 2; ix++)
            {
                var region = new GenerationArea(xs[ix], ys[iy], xs[ix + 1] - xs[ix], ys[iy + 1] - ys[iy]);
                if (region.Width == 0 || region.Height == 0) continue;
                var occupied = 0;
                for (var y = region.Y; y < region.Y + region.Height; y++)
                for (var x = region.X; x < region.X + region.Width; x++)
                    if (board.GetShipId(new GridPosition(x, y)) != null) occupied++;
                result.Add(new LayoutRegionOccupancy(region, occupied));
            }
            return result;
        }
    }
}
