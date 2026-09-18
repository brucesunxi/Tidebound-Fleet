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
        private static ShipDefinition[] Ships() =>
            new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) };
        private static BossDefinition[] Bosses() => new[] { new BossDefinition("TF_KRAKEN_01") };
        private static LevelData Level() => LevelJsonReader.Read(LevelLoadingTests.Fixture().LevelJson.text);

        [TestCase(ShipDirection.Up, 2, 1, 2, 2, 2, 3)]
        [TestCase(ShipDirection.Down, 2, 3, 2, 2, 2, 1)]
        [TestCase(ShipDirection.Left, 3, 2, 2, 2, 1, 2)]
        [TestCase(ShipDirection.Right, 1, 2, 2, 2, 3, 2)]
        public void TailAnchorCoversEveryCellInEachDirection(ShipDirection direction, int x, int y,
            int mx, int my, int hx, int hy)
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
        [TestCase("length", "LENGTH_INVALID")]
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
                case "schema": level.SchemaVersion = 1; break;
                case "levelId": level.LevelId = " "; break;
                case "widthZero": level.Width = 0; break;
                case "widthLarge": level.Width = 13; break;
                case "heightLarge": level.Height = 19; break;
                case "boss": level.BossId = "missing"; break;
                case "empty": level.Ships = new ShipPlacementData[0]; break;
                case "nullShips": level.Ships = null; break;
                case "nullShip": level.Ships[0] = null; break;
                case "shipId": level.Ships[0].Id = " S001"; break;
                case "duplicateId": level.Ships[1].Id = level.Ships[0].Id; break;
                case "type": level.Ships[0].TypeId = "missing"; break;
                case "length": level.Ships[0].Length = 4; break;
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

        [Test]
        public void CatalogMustContainExactlyOneFixedDamageBaseShip()
        {
            var wrongType = BoardValidator.Validate(Level(), new[] { new ShipDefinition("legacy", 10) }, Bosses());
            var wrongDamage = BoardValidator.Validate(Level(), new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, 20) }, Bosses());
            var duplicate = BoardValidator.Validate(Level(), new[] { Ships()[0], Ships()[0] }, Bosses());
            Assert.That(wrongType.Issues.Any(x => x.Code == "TYPE_INVALID"), Is.True);
            Assert.That(wrongDamage.Issues.Any(x => x.Code == "DAMAGE_INVALID"), Is.True);
            Assert.That(duplicate.Issues.Any(x => x.Code == "CATALOG_COUNT_INVALID"), Is.True);
            Assert.That(duplicate.Issues.Any(x => x.Code == "TYPE_DUPLICATE"), Is.True);
        }

        [Test]
        public void InstanceLengthsTwoAndThreeShareOneLogicAndFixedDamage()
        {
            var placements = new[]
            {
                Ship("L", 0, 0, ShipDirection.Up, 3),
                Ship("S1", 1, 0, ShipDirection.Up), Ship("S2", 2, 0, ShipDirection.Up),
                Ship("S3", 3, 0, ShipDirection.Up), Ship("S4", 4, 0, ShipDirection.Up),
                Ship("S5", 5, 0, ShipDirection.Up), Ship("S6", 6, 0, ShipDirection.Up),
                Ship("S7", 7, 0, ShipDirection.Up), Ship("S8", 0, 4, ShipDirection.Right),
                Ship("S9", 2, 4, ShipDirection.Right)
            };
            var level = new LevelData { SchemaVersion = 2, LevelId = "mixed", Width = 8, Height = 6,
                BossId = "TF_KRAKEN_01", Ships = placements };
            using (var session = LevelSessionFactory.Create(level, Ships(), Bosses()))
            {
                Assert.That(session.Boss.Hp, Is.EqualTo(100));
                Assert.That(session.InitialBoard.OccupiedCellCount, Is.EqualTo(21));
                Assert.That(session.GetShip("L").SkinId, Is.EqualTo(FoundationLimits.DefaultLongSkinId));
                Assert.That(session.GetShip("S1").SkinId, Is.EqualTo(FoundationLimits.DefaultStandardSkinId));
            }
        }

        [Test]
        public void FormalBoardCapacityLimitsReturnSpecificDiagnostics()
        {
            var fixture = LevelJsonReader.Read(LevelLoadingTests.DenseFixture().LevelJson.text);
            var tooMany = Copy(fixture, Enumerable.Range(0, 83).Select(i =>
                Ship("X" + i, 0, 0, ShipDirection.Right)).ToArray());
            var tooManyLong = Copy(fixture, Enumerable.Range(0, 82).Select(i =>
                Ship("L" + i, 0, 0, ShipDirection.Right, i < 9 ? 3 : 2)).ToArray());

            var countResult = BoardValidator.Validate(tooMany, Ships(), Bosses());
            var longResult = BoardValidator.Validate(tooManyLong, Ships(), Bosses());
            Assert.That(countResult.Issues.Any(x => x.Code == "SHIP_COUNT_INVALID"), Is.True);
            Assert.That(longResult.Issues.Any(x => x.Code == "LONG_SHIP_COUNT_INVALID"), Is.True);
            Assert.That(longResult.Issues.Any(x => x.Code == "EMPTY_CELL_MINIMUM"), Is.True);
        }

        private static LevelData Copy(LevelData source, ShipPlacementData[] ships) => new LevelData
        {
            SchemaVersion = source.SchemaVersion,
            LevelId = source.LevelId,
            Width = source.Width,
            Height = source.Height,
            BossId = source.BossId,
            Ships = ships
        };

        private static ShipPlacementData Ship(string id, int x, int y, ShipDirection direction, int length = 2) =>
            new ShipPlacementData
            {
                Id = id,
                TypeId = FoundationLimits.BaseShipTypeId,
                Length = length,
                Position = new GridPosition(x, y),
                Direction = direction
            };
    }
}
