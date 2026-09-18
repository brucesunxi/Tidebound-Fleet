using System;
using System.Collections.Generic;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>Validates authored data and complete footprints. Does not simulate moves or prove solvability.</summary>
    public static class BoardValidator
    {
        public static ValidationResult Validate(LevelData level, IReadOnlyList<ShipDefinition> ships,
            IReadOnlyList<BossDefinition> bosses)
        {
            var result = new ValidationResult();
            var catalog = new Dictionary<string, ShipDefinition>(StringComparer.Ordinal);
            if (ships == null || ships.Count == 0) result.Add("CATALOG_EMPTY", "shipConfigs", "The base ship config is required.");
            else for (var i = 0; i < ships.Count; i++)
            {
                var item = ships[i]; var path = $"shipConfigs[{i}]";
                if (item == null) { result.Add("CONFIG_NULL", path, "Ship config is null."); continue; }
                if (!ValidId(item.TypeId)) { result.Add("ID_INVALID", path, "A nonempty, trimmed typeId is required."); continue; }
                if (catalog.ContainsKey(item.TypeId)) result.Add("TYPE_DUPLICATE", path, item.TypeId);
                else catalog.Add(item.TypeId, item);
                if (!string.Equals(item.TypeId, FoundationLimits.BaseShipTypeId, StringComparison.Ordinal))
                    result.Add("TYPE_INVALID", path, $"MVP ship type must be {FoundationLimits.BaseShipTypeId}.");
                if (item.DamageLv1 != FoundationLimits.BaseShipDamage)
                    result.Add("DAMAGE_INVALID", path, $"MVP ship damage must be {FoundationLimits.BaseShipDamage}.");
            }
            if (ships != null && ships.Count != 1)
                result.Add("CATALOG_COUNT_INVALID", "shipConfigs", "Exactly one base ship config is required.");

            var bossIds = new HashSet<string>(StringComparer.Ordinal);
            if (bosses == null || bosses.Count == 0) result.Add("BOSS_CATALOG_EMPTY", "bossConfigs", "Boss catalog is required.");
            else for (var i = 0; i < bosses.Count; i++)
            {
                var item = bosses[i]; var path = $"bossConfigs[{i}]";
                if (item == null || !ValidId(item.BossId)) result.Add("BOSS_ID_INVALID", path, "A valid boss config is required.");
                else if (!bossIds.Add(item.BossId)) result.Add("BOSS_DUPLICATE", path, item.BossId);
            }

            if (level == null) { result.Add("LEVEL_NULL", "level", "Level is required."); return result; }
            if (level.SchemaVersion != FoundationLimits.LevelSchemaVersion) result.Add("SCHEMA_UNSUPPORTED", "schemaVersion", $"Expected schemaVersion {FoundationLimits.LevelSchemaVersion}.");
            if (!ValidId(level.LevelId)) result.Add("ID_INVALID", "levelId", "A nonempty, trimmed levelId is required.");
            var boardValid = level.Width > 0 && level.Width <= FoundationLimits.MaxBoardWidth &&
                             level.Height > 0 && level.Height <= FoundationLimits.MaxBoardHeight;
            if (!boardValid) result.Add("BOARD_SIZE_INVALID", "width/height", $"Board must fit within 1..{FoundationLimits.MaxBoardWidth} columns and 1..{FoundationLimits.MaxBoardHeight} rows.");
            if (!ValidId(level.BossId) || !bossIds.Contains(level.BossId)) result.Add("BOSS_UNKNOWN", "bossId", "Boss must resolve in the catalog.");
            if (level.Ships == null || level.Ships.Length == 0)
            { result.Add("SHIPS_EMPTY", "ships", "At least one ship is required."); return result; }
            if (level.Ships.Length > FoundationLimits.MaxShipCount)
                result.Add("SHIP_COUNT_INVALID", "ships", $"A level may contain at most {FoundationLimits.MaxShipCount} ships.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var occupancy = new Dictionary<GridPosition, string>();
            long totalDamage = 0;
            var longShipCount = 0;
            var authoredCellCount = 0;
            for (var i = 0; i < level.Ships.Length; i++)
            {
                var ship = level.Ships[i]; var path = $"ships[{i}]";
                if (ship == null) { result.Add("SHIP_NULL", path, "Ship instance is null."); continue; }
                if (!ValidId(ship.Id)) result.Add("ID_INVALID", path + ".id", "A nonempty, trimmed id is required.");
                else if (!ids.Add(ship.Id)) result.Add("SHIP_DUPLICATE", path + ".id", ship.Id);
                if (!ValidId(ship.TypeId) || !catalog.TryGetValue(ship.TypeId, out var definition))
                { result.Add("TYPE_UNKNOWN", path + ".typeId", "Ship type must resolve in the catalog."); continue; }
                totalDamage += definition.DamageLv1;
                if (ship.Length < FoundationLimits.MinShipLength || ship.Length > FoundationLimits.MaxShipLength)
                { result.Add("LENGTH_INVALID", path + ".length", "MVP logical length must be 2 or 3 cells."); continue; }
                authoredCellCount = checked(authoredCellCount + ship.Length);
                if (ship.Length == FoundationLimits.MaxShipLength) longShipCount++;
                if (!Enum.IsDefined(typeof(ShipDirection), ship.Direction))
                { result.Add("DIRECTION_INVALID", path + ".direction", "Expected Up, Down, Left or Right."); continue; }
                if (!boardValid) continue;
                var step = GridFootprint.DirectionStep(ship.Direction);
                // Use wide arithmetic so hostile coordinates cannot wrap back onto the board.
                long headX = (long)ship.Position.X + step.X * (ship.Length - 1);
                long headY = (long)ship.Position.Y + step.Y * (ship.Length - 1);
                if (ship.Position.X < 0 || ship.Position.X >= level.Width || ship.Position.Y < 0 || ship.Position.Y >= level.Height ||
                    headX < 0 || headX >= level.Width || headY < 0 || headY >= level.Height)
                { result.Add("OUT_OF_BOUNDS", path + ".position", "The complete tail-to-head footprint must be inside the board."); continue; }
                foreach (var cell in GridFootprint.Cells(ship.Position, ship.Direction, ship.Length))
                {
                    if (occupancy.TryGetValue(cell, out var existing)) result.Add("OVERLAP", path + ".position", $"Cell {cell} is already occupied by {existing}.");
                    else occupancy.Add(cell, ship.Id);
                }
            }
            if (longShipCount > FoundationLimits.MaxLongShipCount || longShipCount * 10 > level.Ships.Length)
                result.Add("LONG_SHIP_COUNT_INVALID", "ships", $"Length-3 ships may be at most {FoundationLimits.MaxLongShipCount} and 10% of the level.");
            if (authoredCellCount > FoundationLimits.MaxOccupiedCellCount)
                result.Add("OCCUPANCY_LIMIT_EXCEEDED", "ships", $"Authored footprints may occupy at most {FoundationLimits.MaxOccupiedCellCount} cells.");
            if (boardValid && level.Width == FoundationLimits.MaxBoardWidth && level.Height == FoundationLimits.MaxBoardHeight &&
                level.Width * level.Height - authoredCellCount < FoundationLimits.MinFormalBoardEmptyCellCount)
                result.Add("EMPTY_CELL_MINIMUM", "ships", $"A formal board must keep at least {FoundationLimits.MinFormalBoardEmptyCellCount} empty cells.");
            if (totalDamage <= 0 || totalDamage > int.MaxValue) result.Add("TOTAL_DAMAGE_INVALID", "ships", "Initial Lv1 total damage must fit in a positive Int32.");
            return result;
        }

        private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) && id == id.Trim();
    }
}
