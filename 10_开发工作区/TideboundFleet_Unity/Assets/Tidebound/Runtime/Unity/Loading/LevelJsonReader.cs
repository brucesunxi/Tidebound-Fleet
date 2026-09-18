using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Ship;

namespace Tidebound.Config
{
    public sealed class LevelFormatException : Exception
    {
        public LevelFormatException(string message, Exception inner = null) : base(message, inner) { }
    }

    /// <summary>Strict schema boundary: duplicates, unknown fields and implicit scalar conversions are rejected.</summary>
    public static class LevelJsonReader
    {
        public static LevelData Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new LevelFormatException("Level JSON is empty.");
            try
            {
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 16, DateParseHandling = DateParseHandling.None })
                {
                    var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new LevelFormatException("Unexpected trailing JSON content.");
                    Fields(root, "level", "schemaVersion", "levelId", "width", "height", "ships", "bossId");
                    if (!(root["ships"] is JArray array)) throw new LevelFormatException("ships must be an array.");
                    var ships = new ShipPlacementData[array.Count];
                    for (var i = 0; i < array.Count; i++)
                    {
                        if (!(array[i] is JObject item)) throw new LevelFormatException($"ships[{i}] must be an object.");
                        Fields(item, $"ships[{i}]", "id", "typeId", "position", "direction");
                        if (!(item["position"] is JObject position)) throw new LevelFormatException($"ships[{i}].position must be an object.");
                        Fields(position, $"ships[{i}].position", "x", "y");
                        var directionText = String(item, "direction");
                        if (!Enum.TryParse<ShipDirection>(directionText, false, out var direction) ||
                            !Enum.IsDefined(typeof(ShipDirection), direction) || direction.ToString() != directionText)
                            throw new LevelFormatException($"ships[{i}].direction must be Up, Down, Left or Right.");
                        ships[i] = new ShipPlacementData { Id = String(item, "id"), TypeId = String(item, "typeId"),
                            Position = new GridPosition(Integer(position, "x"), Integer(position, "y")), Direction = direction };
                    }
                    return new LevelData { SchemaVersion = Integer(root, "schemaVersion"), LevelId = String(root, "levelId"),
                        Width = Integer(root, "width"), Height = Integer(root, "height"), Ships = ships, BossId = String(root, "bossId") };
                }
            }
            catch (Exception e) when (e is JsonException || e is OverflowException || e is FormatException)
            { throw new LevelFormatException("Invalid level JSON: " + e.Message, e); }
        }

        private static void Fields(JObject value, string path, params string[] expected)
        {
            var actual = value.Properties().Select(x => x.Name).ToArray();
            if (actual.Length != expected.Length || actual.Except(expected).Any())
                throw new LevelFormatException($"{path} must contain exactly: {string.Join(", ", expected)}.");
        }
        private static int Integer(JObject value, string name)
        {
            if (value[name]?.Type != JTokenType.Integer) throw new LevelFormatException(name + " must be an Int32 integer.");
            return value[name].Value<int>();
        }
        private static string String(JObject value, string name)
        {
            if (value[name]?.Type != JTokenType.String) throw new LevelFormatException(name + " must be a string.");
            return value[name].Value<string>();
        }
    }
}
