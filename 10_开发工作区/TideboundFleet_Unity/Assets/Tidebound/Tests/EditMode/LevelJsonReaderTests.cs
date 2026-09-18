using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;

namespace Tidebound.Tests
{
    public sealed class LevelJsonReaderTests
    {
        [TestCase("")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("{broken")]
        [TestCase("{\"schemaVersion\":1,\"schemaVersion\":1}")]
        public void MalformedOrDuplicateJsonIsRejected(string json)
        { Assert.Throws<LevelFormatException>(() => LevelJsonReader.Read(json)); }

        [TestCase("missing")]
        [TestCase("hp")]
        [TestCase("length")]
        [TestCase("damage")]
        [TestCase("state")]
        [TestCase("stringWidth")]
        [TestCase("decimalWidth")]
        [TestCase("overflowWidth")]
        [TestCase("numericDirection")]
        [TestCase("numericStringDirection")]
        [TestCase("lowercaseDirection")]
        [TestCase("diagonalDirection")]
        [TestCase("missingX")]
        [TestCase("nullShip")]
        [TestCase("trailingDocument")]
        public void SchemaRejectsMissingFieldsImplicitConversionsAndDuplicateConfigurationSources(string mutation)
        {
            var root = JObject.Parse(LevelLoadingTests.Fixture().LevelJson.text);
            var ship = (JObject)root["ships"][0];
            switch (mutation)
            {
                case "missing": root.Remove("bossId"); break;
                case "hp": root["hp"] = 70; break;
                case "length": ship["length"] = 2; break;
                case "damage": ship["damage"] = 10; break;
                case "state": ship["state"] = "Idle"; break;
                case "stringWidth": root["width"] = "4"; break;
                case "decimalWidth": root["width"] = 4.5; break;
                case "overflowWidth": root["width"] = (long)int.MaxValue + 1; break;
                case "numericDirection": ship["direction"] = 0; break;
                case "numericStringDirection": ship["direction"] = "0"; break;
                case "lowercaseDirection": ship["direction"] = "right"; break;
                case "diagonalDirection": ship["direction"] = "Diagonal"; break;
                case "missingX": ((JObject)ship["position"]).Remove("x"); break;
                case "nullShip": root["ships"][0] = null; break;
            }
            var text = root.ToString() + (mutation == "trailingDocument" ? " {}" : "");
            Assert.Throws<LevelFormatException>(() => LevelJsonReader.Read(text));
        }
    }
}
