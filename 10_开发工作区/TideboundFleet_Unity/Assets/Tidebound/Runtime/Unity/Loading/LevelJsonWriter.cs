using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;

namespace Tidebound.Config
{
    /// <summary>Writes the canonical level JSON shape. It never serializes runtime state or presentation data.</summary>
    public static class LevelJsonWriter
    {
        public static string Write(LevelData level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            var root = new JObject
            {
                ["schemaVersion"] = level.SchemaVersion,
                ["levelId"] = level.LevelId,
                ["width"] = level.Width,
                ["height"] = level.Height,
                ["bossId"] = level.BossId
            };
            var ships = new JArray();
            if (level.Ships != null)
            {
                foreach (var ship in level.Ships)
                {
                    if (ship == null) throw new LevelFormatException("A ship cannot be null when writing JSON.");
                    ships.Add(new JObject
                    {
                        ["id"] = ship.Id,
                        ["typeId"] = ship.TypeId,
                        ["length"] = ship.Length,
                        ["position"] = new JObject { ["x"] = ship.Position.X, ["y"] = ship.Position.Y },
                        ["direction"] = ship.Direction.ToString()
                    });
                }
            }
            root["ships"] = ships;
            return root.ToString(Formatting.Indented) + Environment.NewLine;
        }
    }
}
