using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class BoardValidationTests
    {
        private static ShipDefinition[] Ships() => new[] { new ShipDefinition("TF_SPEEDBOAT", 2, 10) };
        private static BossDefinition[] Bosses() => new[] { new BossDefinition("TF_KRAKEN_01") };
        private static LevelData Level() => LevelJsonReader.Read(LevelLoadingTests.Fixture().LevelJson.text);

        [TestCase(ShipDirection.Up, 2, 1, 2, 2, 2, 3)]
        [TestCase(ShipDirection.Down, 2, 3, 2, 2, 2, 1)]
        [TestCase(ShipDirection.Left, 3, 2, 2, 2, 1, 2)]
        [TestCase(ShipDirection.Right, 1, 2, 2, 2, 3, 2)]
        public void TailAnchorCoversEveryCellInEachDirection(ShipDirection direction, int x, int y, int mx, int my, int hx, int hy)
        {
            CollectionAssert.AreEqual(new[] { new GridPosition(x, y), new GridPosition(mx, my), new GridPosition(hx, hy) },
                GridFootprint.Cells(new GridPosition(x, y), direction, 3).ToArray());
        }

        [TestCase("null", "LEVEL_NULL")]
        [TestCase("schema", "SCHEMA_UNSUPPORTED")]
        [TestCase("levelId", "ID_INVALID")]
        [TestCase("widthZero", "BOARD_SIZE_INVALID")]
        [TestCase("widthLarge", "BOARD_SIZE_INVALID")]
        [TestCase("heightLarge", "BOARD_SIZE_INVALID")]
        [TestCase("boss", "BOSS_UNKNOWN")]
        [TestCase("empty", "SHIPS_EMPTY")]
        [TestCase("nullShips", "SHIPS_EMPTY")]
        [TestCase("nullShip", "SHIP_NULL")]
        [TestCase("shipId", "ID_INVALID")]
        [TestCase("duplicateId", "SHIP_DUPLICATE")]
        [TestCase("type", "TYPE_UNKNOWN")]
        [TestCase("direction", "DIRECTION_INVALID")]
        [TestCase("tailOutside", "OUT_OF_BOUNDS")]
        [TestCase("headOutside", "OUT_OF_BOUNDS")]
        [TestCase("overflowCoordinate", "OUT_OF_BOUNDS")]
        [TestCase("overlap", "OVERLAP")]
        public void InvalidLayoutsReturnSpecificDiagnosticsAndCannotStartSession(string mutation, string expected)
        {
            var level = Level();
            switch (mutation)
            {
                case "null": level = null; break;
                case "schema": level.SchemaVersion = 2; break;
                case "levelId": level.LevelId = " "; break;
                case "widthZero": level.Width = 0; break;
                case "widthLarge": level.Width = 9; break;
                case "heightLarge": level.Height = 10; break;
                case "boss": level.BossId = "missing"; break;
                case "empty": level.Ships = new ShipPlacementData[0]; break;
                case "nullShips": level.Ships = null; break;
                case "nullShip": level.Ships[0] = null; break;
                case "shipId": level.Ships[0].Id = " S001"; break;
                case "duplicateId": level.Ships[1].Id = level.Ships[0].Id; break;
                case "type": level.Ships[0].TypeId = "missing"; break;
                case "direction": level.Ships[0].Direction = (ShipDirection)42; break;
                case "tailOutside": level.Ships[0].Position = new GridPosition(-1, 0); break;
                case "headOutside": level.Ships[0].Position = new GridPosition(3, 0); break;
                case "overflowCoordinate": level.Ships[0].Position = new GridPosition(int.MaxValue, 0); break;
                case "overlap": level.Ships[1].Position = new GridPosition(1, 0); break;
            }
            var result = BoardValidator.Validate(level, Ships(), Bosses());
            Assert.That(result.Issues.Any(x => x.Code == expected), Is.True, string.Join("\n", result.Issues));
            Assert.Throws<LevelValidationException>(() => LevelSessionFactory.Create(level, Ships(), Bosses()));
        }

        [TestCase(0, 10, "LENGTH_INVALID")]
        [TestCase(5, 10, "LENGTH_INVALID")]
        [TestCase(2, 0, "DAMAGE_INVALID")]
        [TestCase(2, -1, "DAMAGE_INVALID")]
        [TestCase(2, int.MaxValue, "TOTAL_DAMAGE_INVALID")]
        public void InvalidLogicConfigsAndDamageOverflowAreRejected(int length, int damage, string expected)
        {
            var result = BoardValidator.Validate(Level(), new[] { new ShipDefinition("TF_SPEEDBOAT", length, damage) }, Bosses());
            Assert.That(result.Issues.Any(x => x.Code == expected), Is.True);
        }

        [Test]
        public void DuplicateCatalogIdsAreRejectedInsteadOfChoosingAnArbitraryAsset()
        {
            var result = BoardValidator.Validate(Level(), new[] { Ships()[0], Ships()[0] }, new[] { Bosses()[0], Bosses()[0] });
            Assert.That(result.Issues.Any(x => x.Code == "TYPE_DUPLICATE"), Is.True);
            Assert.That(result.Issues.Any(x => x.Code == "BOSS_DUPLICATE"), Is.True);
        }

        [Test]
        public void MixedTypesDetermineLengthAndHpFromTheCatalog()
        {
            var level = new LevelData { SchemaVersion = 1, LevelId = "mixed", Width = 4, Height = 6, BossId = "TF_KRAKEN_01",
                Ships = new[] {
                    new ShipPlacementData { Id = "a", TypeId = "short", Position = new GridPosition(0, 0), Direction = ShipDirection.Up },
                    new ShipPlacementData { Id = "b", TypeId = "long", Position = new GridPosition(1, 0), Direction = ShipDirection.Up } } };
            using (var session = LevelSessionFactory.Create(level, new[] { new ShipDefinition("short", 2, 10), new ShipDefinition("long", 4, 50) }, Bosses()))
            { Assert.That(session.Boss.Hp, Is.EqualTo(60)); Assert.That(session.InitialBoard.OccupiedCellCount, Is.EqualTo(6)); }
        }
    }
}
