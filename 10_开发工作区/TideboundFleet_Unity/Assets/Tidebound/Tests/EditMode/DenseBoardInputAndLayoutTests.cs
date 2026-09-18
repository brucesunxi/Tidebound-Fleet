using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Ship;
using Tidebound.Unity.Input;
using Tidebound.Unity.Layout;
using Tidebound.Unity.Ship;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class DenseBoardInputAndLayoutTests
    {
        [TestCase(360f, 640f, 0.12f, 0.74f, 0.14f, 24f, 26f)]
        [TestCase(390f, 844f, 0.17f, 0.68f, 0.15f, 28f, 30f)]
        [TestCase(430f, 932f, 0.17f, 0.68f, 0.15f, 31f, 33f)]
        public void SafeAreaClassesKeepFormalBoardWithinPlannedCellRange(float width, float height,
            float boss, float board, float tools, float minCell, float maxCell)
        {
            var metrics = BoardLayoutCalculator.Calculate(width, height);
            Assert.That(metrics.BossRatio, Is.EqualTo(boss).Within(0.0001f));
            Assert.That(metrics.BoardRatio, Is.EqualTo(board).Within(0.0001f));
            Assert.That(metrics.ToolsRatio, Is.EqualTo(tools).Within(0.0001f));
            Assert.That(metrics.CellSize, Is.InRange(minCell, maxCell));
            Assert.That(metrics.GridSize.x, Is.EqualTo(metrics.CellSize * 12f).Within(0.001f));
            Assert.That(metrics.GridSize.y, Is.EqualTo(metrics.CellSize * 18f).Within(0.001f));
            Assert.That(metrics.LaneThickness, Is.LessThanOrEqualTo(width * 0.04f));
        }

        [Test]
        public void GridSelectionUsesOccupiedCellsAndNeverSnapsEmptyWater()
        {
            using (var session = ShipMovementSystemTests.CreateSession(6, 4,
                ShipMovementSystemTests.Ship("A", 1, 1, ShipDirection.Right),
                ShipMovementSystemTests.Ship("B", 4, 1, ShipDirection.Up)))
            {
                var selection = new BoardGridSelection(() => session.Board);
                Assert.That(selection.PointerDown(new GridPosition(2, 1)), Is.EqualTo("A"));
                Assert.That(selection.PointerUp(new GridPosition(1, 1)), Is.EqualTo("A"));
                Assert.That(selection.PointerDown(new GridPosition(3, 3)), Is.Null);
                Assert.That(selection.PointerUp(new GridPosition(4, 1)), Is.Null);
                Assert.That(selection.PointerDown(new GridPosition(2, 1)), Is.EqualTo("A"));
                Assert.That(selection.PointerUp(new GridPosition(4, 1)), Is.Null);
            }
        }

        [Test]
        public void WorldMapperRoundsToTheLogicalCellCenteredAtEachTailPosition()
        {
            var gameObject = new GameObject("GridWorldMapper_Test");
            try
            {
                var mapper = gameObject.AddComponent<GridWorldMapper>();
                Assert.That(mapper.WorldToCell(new Vector3(2.49f, 0f, 3.49f)), Is.EqualTo(new GridPosition(2, 3)));
                Assert.That(mapper.WorldToCell(new Vector3(2.51f, 0f, 3.51f)), Is.EqualTo(new GridPosition(3, 4)));
                Assert.That(mapper.TryRayToCell(new Ray(new Vector3(1f, 10f, 2f), Vector3.down), out var cell), Is.True);
                Assert.That(cell, Is.EqualTo(new GridPosition(1, 2)));
            }
            finally { Object.DestroyImmediate(gameObject); }
        }
    }
}
