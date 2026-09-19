using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class LocalLayoutAnalyzerTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RunsCountDistinctShipsAndTrackAllFootprintCells(bool vertical)
        {
            var ships = Enumerable.Range(0, 3).Select(i => vertical
                ? Ship("S" + i, 0, i * 3, ShipDirection.Up, 3)
                : Ship("S" + i, i * 3, 0, ShipDirection.Right, 3)).ToArray();
            var board = Board(vertical ? 2 : 9, vertical ? 9 : 2, ships);
            var report = LocalLayoutAnalyzer.Analyze(board);
            var along = vertical ? report.LongestAdjacentColumn : report.LongestAdjacentRow;
            var across = vertical ? report.LongestAdjacentRow : report.LongestAdjacentColumn;
            Assert.That(along.ShipCount, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { "S0", "S1", "S2" }, along.ShipIds);
            Assert.That(along.Start, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(along.End, Is.EqualTo(vertical ? new GridPosition(0, 8) : new GridPosition(8, 0)));
            Assert.That(across.ShipCount, Is.EqualTo(1));
        }

        [Test]
        public void OneCellGapsJoinRunsButLargerGapsAndOtherDirectionsBreakThem()
        {
            var board = Board(14, 4,
                Ship("A", 0, 0, ShipDirection.Right), Ship("B", 3, 0, ShipDirection.Right),
                Ship("C", 7, 0, ShipDirection.Right), Ship("D", 10, 0, ShipDirection.Left),
                Ship("E", 11, 0, ShipDirection.Right));
            var report = LocalLayoutAnalyzer.Analyze(board);
            Assert.That(report.LongestAdjacentRow.ShipCount, Is.EqualTo(1));
            Assert.That(report.LongestGappedRow.ShipCount, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "A", "B" }, report.LongestGappedRow.ShipIds);
            Assert.That(report.LongestGappedRow.End, Is.EqualTo(new GridPosition(4, 0)));
            var zero = LocalLayoutAnalyzer.Analyze(board, new LocalLayoutOptions(allowedGapCells: 0));
            Assert.That(zero.LongestGappedRow.ShipCount, Is.EqualTo(1));
        }

        [Test]
        public void WindowCountsShipsOnceEvenForLongOrPartlyIntersectingShips()
        {
            var board = Board(6, 6,
                Ship("R", 0, 1, ShipDirection.Right, 3), Ship("U", 4, 1, ShipDirection.Up, 3),
                Ship("L", 4, 4, ShipDirection.Left, 3), Ship("D", 1, 4, ShipDirection.Down, 3));
            var report = LocalLayoutAnalyzer.Analyze(board,
                new LocalLayoutOptions(new GenerationArea(1, 1, 4, 4)));
            Assert.That(report.Windows.Count, Is.EqualTo(1));
            Assert.That(report.Windows[0].ShipCount, Is.EqualTo(4));
            Assert.That(report.MinimumWindowEntropy, Is.EqualTo(1d).Within(1e-10));
            Assert.That(report.P10WindowEntropy, Is.EqualTo(1d).Within(1e-10));
        }

        [Test]
        public void BalancedGlobalDirectionsDoNotHideSegregatedQuadrants()
        {
            var segregated = TiledBoard(false);
            var mixed = TiledBoard(true);
            Assert.That(LevelStructureAnalyzer.Analyze(segregated).DirectionEntropy, Is.EqualTo(1d).Within(1e-10));
            Assert.That(LevelStructureAnalyzer.Analyze(mixed).DirectionEntropy, Is.EqualTo(1d).Within(1e-10));
            var bad = LocalLayoutAnalyzer.Analyze(segregated);
            var good = LocalLayoutAnalyzer.Analyze(mixed);
            Assert.That(bad.MinimumWindowEntropy, Is.Zero);
            Assert.That(good.MinimumWindowEntropy, Is.GreaterThan(0.7));
            Assert.That(bad.TotalWindowCount, Is.EqualTo(25));
            Assert.That(good.Windows.Count, Is.EqualTo(25));
            TestContext.WriteLine($"Quadrant fixture: min={bad.MinimumWindowEntropy}, p10={bad.P10WindowEntropy}; " +
                                  $"mixed fixture: min={good.MinimumWindowEntropy}, p10={good.P10WindowEntropy}");
        }

        [Test]
        public void EmptyAndSparseWindowsAreExplicitlyUnscored()
        {
            var board = Board(8, 8, Ship("A", 0, 0, ShipDirection.Right));
            var report = LocalLayoutAnalyzer.Analyze(board);
            Assert.That(report.TotalWindowCount, Is.EqualTo(25));
            Assert.That(report.EmptyWindowCount, Is.GreaterThan(0));
            Assert.That(report.SparseWindowCount, Is.GreaterThan(0));
            Assert.That(report.Windows, Is.Empty);
            Assert.That(report.MinimumWindowEntropy, Is.Null);
            Assert.That(report.P10WindowEntropy, Is.Null);
            Assert.That(report.LeastMixedWindow, Is.Null);
        }

        [Test]
        public void AreaSmallerThanWindowHasNoArtificiallyClippedWindow()
        {
            var report = LocalLayoutAnalyzer.Analyze(Board(3, 2, Ship("A", 0, 0, ShipDirection.Right)));
            Assert.That(report.TotalWindowCount, Is.Zero);
            Assert.That(report.MinimumWindowEntropy, Is.Null);
        }

        [Test]
        public void EmptyBoardHasFullEmptyRectangleAndNoRuns()
        {
            var report = LocalLayoutAnalyzer.Analyze(Board(5, 7));
            Assert.That(report.LongestAdjacentRow, Is.Null);
            Assert.That(report.LongestGappedColumn, Is.Null);
            Assert.That(report.LargestEmptyArea, Is.EqualTo(35));
            Assert.That(report.EmptyWindowCount, Is.EqualTo(8));
            Assert.That(report.Regions.Sum(r => r.OccupiedCellCount), Is.Zero);
        }

        [Test]
        public void FullBoardHasNoEmptyRectangleAndRegionsPartitionOddDimensions()
        {
            var ships = Enumerable.Range(0, 5).Select(x => Ship("S" + x, x, 0, ShipDirection.Up, 3)).ToArray();
            var report = LocalLayoutAnalyzer.Analyze(Board(5, 3, ships));
            Assert.That(report.LargestEmptyArea, Is.Zero);
            Assert.That(report.Regions.Count, Is.EqualTo(4));
            Assert.That(report.Regions.Sum(r => r.Area.Width * r.Area.Height), Is.EqualTo(15));
            Assert.That(report.Regions.Sum(r => r.OccupiedCellCount), Is.EqualTo(15));
            Assert.That(report.Regions.All(r => r.OccupancyRatio == 1), Is.True);
        }

        [Test]
        public void NarrowBoardOmitsEmptyQuadrants()
        {
            var report = LocalLayoutAnalyzer.Analyze(Board(1, 5, Ship("A", 0, 0, ShipDirection.Up)));
            Assert.That(report.Regions.Count, Is.EqualTo(2));
            Assert.That(report.Regions.Sum(r => r.Area.Width * r.Area.Height), Is.EqualTo(5));
            Assert.That(report.Regions.Sum(r => r.OccupiedCellCount), Is.EqualTo(2));
        }

        [Test]
        public void CustomAreaIgnoresReservedWaterAndDoesNotModifyTheBoard()
        {
            var board = Board(14, 18, Ship("A", 4, 5, ShipDirection.Right), Ship("B", 6, 5, ShipDirection.Up));
            var before = LevelStateIdentity.Fingerprint(board);
            var full = LocalLayoutAnalyzer.Analyze(board);
            var scoped = LocalLayoutAnalyzer.Analyze(board, new LocalLayoutOptions(new GenerationArea(4, 5, 6, 8)));
            Assert.That(full.LargestEmptyArea, Is.GreaterThan(scoped.LargestEmptyArea));
            Assert.That(scoped.LargestEmptyRectangle.X, Is.GreaterThanOrEqualTo(4));
            Assert.That(scoped.LargestEmptyRectangle.Y, Is.GreaterThanOrEqualTo(5));
            Assert.That(scoped.Regions.Sum(r => r.Area.Width * r.Area.Height), Is.EqualTo(48));
            Assert.That(scoped.Regions.Sum(r => r.OccupiedCellCount), Is.EqualTo(4));
            Assert.That(LevelStateIdentity.Fingerprint(board), Is.EqualTo(before));
        }

        [Test]
        public void LargestEmptyRectangleMatchesExhaustiveOracleAcrossDeterministicSmallBoards()
        {
            for (var seed = 0; seed < 32; seed++)
            {
                var random = new System.Random(seed);
                var ships = new List<ShipPlacementData>();
                for (var y = 0; y < 6; y += 2)
                for (var x = 0; x < 5; x++)
                    if (random.Next(2) == 1) ships.Add(Ship("S" + ships.Count, x, y, ShipDirection.Up));
                var board = Board(5, 6, ships.ToArray());
                var area = new GenerationArea(1, 1, 3, 4);
                var report = LocalLayoutAnalyzer.Analyze(board, new LocalLayoutOptions(area));
                var maximum = 0;
                for (var y = area.Y; y < area.Y + area.Height; y++)
                for (var x = area.X; x < area.X + area.Width; x++)
                for (var h = 1; y + h <= area.Y + area.Height; h++)
                for (var w = 1; x + w <= area.X + area.Width; w++)
                {
                    var empty = true;
                    for (var dy = 0; dy < h; dy++)
                    for (var dx = 0; dx < w; dx++)
                        if (board.GetShipId(new GridPosition(x + dx, y + dy)) != null) empty = false;
                    if (empty) maximum = Math.Max(maximum, w * h);
                }
                Assert.That(report.LargestEmptyArea, Is.EqualTo(maximum), "seed " + seed);
                var found = report.LargestEmptyRectangle;
                for (var y = found.Y; y < found.Y + found.Height; y++)
                for (var x = found.X; x < found.X + found.Width; x++)
                    Assert.That(board.GetShipId(new GridPosition(x, y)), Is.Null);
            }
        }

        [Test]
        public void InvalidInputsAreRejectedWithoutSilentlyClamping()
        {
            var board = Board(8, 8);
            Assert.Throws<ArgumentNullException>(() => LocalLayoutAnalyzer.Analyze(null));
            foreach (var option in new[]
            {
                new LocalLayoutOptions(new GenerationArea(-1, 0, 4, 4)),
                new LocalLayoutOptions(new GenerationArea(0, 0, 0, 4)),
                new LocalLayoutOptions(new GenerationArea(7, 0, int.MaxValue, 4)),
                new LocalLayoutOptions(windowSize: 0), new LocalLayoutOptions(windowSize: int.MaxValue),
                new LocalLayoutOptions(minimumWindowShips: 0), new LocalLayoutOptions(minimumWindowShips: 17),
                new LocalLayoutOptions(allowedGapCells: -1), new LocalLayoutOptions(allowedGapCells: int.MaxValue)
            }) Assert.Throws<ArgumentException>(() => LocalLayoutAnalyzer.Analyze(board, option));
        }

        [TestCase("P5R_Rebuild_001")]
        [TestCase("P5R_Rebuild_002")]
        public void SavedI1SamplesProduceDeterministicDiagnosticsWithoutInvalidatingProofs(string id)
        {
            const string folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R_Rebuild/";
            var level = LevelJsonReader.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(folder + id + ".json").text);
            var proof = LevelProofJson.Read(AssetDatabase.LoadAssetAtPath<TextAsset>(folder + id + ".solution.json").text);
            using (var session = ReverseLevelGeneratorTests.Create(level))
            {
                var a = LocalLayoutAnalyzer.Analyze(session.InitialBoard);
                var b = LocalLayoutAnalyzer.Analyze(session.InitialBoard);
                Assert.That(a.LargestEmptyArea, Is.EqualTo(b.LargestEmptyArea));
                Assert.That(a.MinimumWindowEntropy, Is.EqualTo(b.MinimumWindowEntropy));
                Assert.That(a.P10WindowEntropy, Is.EqualTo(b.P10WindowEntropy));
                CollectionAssert.AreEqual(a.LongestGappedColumn.ShipIds, b.LongestGappedColumn.ShipIds);
                Assert.That(proof.Replay(id, session.InitialBoard).IsComplete, Is.True);
                TestContext.WriteLine($"{id}: adjacent={a.LongestAdjacentRow.ShipCount}/{a.LongestAdjacentColumn.ShipCount}; " +
                    $"gapped={a.LongestGappedRow.ShipCount}/{a.LongestGappedColumn.ShipCount}; min={a.MinimumWindowEntropy}; " +
                    $"p10={a.P10WindowEntropy}; hole={a.LargestEmptyArea}; windows={a.Windows.Count}/{a.TotalWindowCount}");
            }
        }

        private static BoardModel TiledBoard(bool mixed)
        {
            var ships = new List<ShipPlacementData>();
            for (var by = 0; by < 4; by++)
            for (var bx = 0; bx < 4; bx++)
            {
                var index = mixed ? (bx + 2 * by) % 4 : bx / 2 + 2 * (by / 2);
                var direction = new[] { ShipDirection.Right, ShipDirection.Up, ShipDirection.Left, ShipDirection.Down }[index];
                for (var i = 0; i < 2; i++)
                {
                    var horizontal = direction == ShipDirection.Right || direction == ShipDirection.Left;
                    var x = bx * 2 + (horizontal ? (direction == ShipDirection.Left ? 1 : 0) : i);
                    var y = by * 2 + (horizontal ? i : (direction == ShipDirection.Down ? 1 : 0));
                    ships.Add(Ship("S" + ships.Count, x, y, direction));
                }
            }
            return Board(8, 8, ships.ToArray());
        }

        private static BoardModel Board(int width, int height, params ShipPlacementData[] ships)
        {
            // Empty is a valid runtime state after an exit, but not a valid level JSON input.
            if (ships.Length == 0)
            {
                using (var session = ShipMovementSystemTests.CreateSession(width, height,
                    Ship("last", 0, 0, ShipDirection.Up)))
                    return session.InitialBoard.ApplyPathResult(session.InitialBoard.QueryForwardPath("last"));
            }
            using (var session = ShipMovementSystemTests.CreateSession(width, height, ships)) return session.InitialBoard;
        }

        private static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction, int length = 2) =>
            new ShipPlacementData { Id = id, TypeId = "TF_BASE_SHIP", Position = new GridPosition(x, y), Direction = direction, Length = length };
    }
}
