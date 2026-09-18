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
        [TestCase("missingLength")]
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
                case "missingLength": ship.Remove("length"); break;
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

        [Test]
        public void CanonicalWriterRoundTripsSchemaTwoWithoutRuntimeFields()
        {
            var source = LevelJsonReader.Read(LevelLoadingTests.DenseFixture().LevelJson.text);
            var json = LevelJsonWriter.Write(source);
            var copy = LevelJsonReader.Read(json);
            Assert.That(copy.SchemaVersion, Is.EqualTo(2));
            Assert.That(copy.Ships.Length, Is.EqualTo(80));
            Assert.That(copy.Ships[0].Length, Is.EqualTo(2));
            Assert.That(json, Does.Not.Contain("damage"));
            Assert.That(json, Does.Not.Contain("state"));
            Assert.That(json, Does.Not.Contain("skinId"));
        }
    }
}
