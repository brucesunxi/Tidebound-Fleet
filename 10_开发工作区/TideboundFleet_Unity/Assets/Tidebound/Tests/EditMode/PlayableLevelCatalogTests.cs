using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Board;
using Tidebound.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class PlayableLevelCatalogTests
    {
        [Test]
        public void FiveHundredMetadataEntriesLoadOnlyRequestedContent()
        {
            var reads = new List<string>();
            var catalog = new PlayableLevelCatalog(Enumerable.Range(1, 500).Select(n => "L" + n), _ => true,
                id => { reads.Add(id); return new LevelData { LevelId = id }; });
            Assert.That(catalog.Count, Is.EqualTo(500)); Assert.That(reads, Is.Empty);
            Assert.That(catalog.GetLevelId(499), Is.EqualTo("L500"));
            Assert.That(catalog.Load(10).LevelId, Is.EqualTo("L11"));
            Assert.That(reads, Is.EqualTo(new[] { "L11" }));
        }
        [Test]
        public void MissingContentAndOutOfRangeNeverInvokeLoader()
        {
            var catalog = new PlayableLevelCatalog(new[] { "L1", "L2" }, id => id == "L1", _ => throw new AssertionException("Unexpected read"));
            foreach (var index in new[] { -1, 1, 2, 500 })
            { Assert.That(catalog.IsAvailable(index), Is.False); Assert.Throws<InvalidOperationException>(() => catalog.Load(index)); }
        }
        [Test]
        public void IdentityMismatchIsRejectedBeforeAttemptCreation()
        {
            var catalog = new PlayableLevelCatalog(new[] { "L1" }, _ => true, _ => new LevelData { LevelId = "L2" });
            Assert.Throws<InvalidOperationException>(() => catalog.Load(0));
        }
        [TestCase("L1", "L1")]
        [TestCase("L1", "")]
        public void UnstableMetadataIsRejected(string first, string second)
        { Assert.Throws<ArgumentException>(() => new PlayableLevelCatalog(new[] { first, second }, _ => true, _ => null)); }
        [Test]
        public void CandidateManifestAdapterKeepsOrderWithoutLoadingLayoutsOrAssumingTen()
        {
            var reads = 0;
            var catalog = PlayableLevelCatalog.FromManifest("{\"manifestVersion\":1,\"rulesVersion\":\"" + LevelRules.Version +
                "\",\"levels\":[{\"levelId\":\"B\",\"layoutFile\":\"B.json\"},{\"levelId\":\"A\",\"layoutFile\":\"A.json\"}]}",
                new Dictionary<string, Func<string>> { ["B.json"] = () => { reads++; return "{}"; } });
            Assert.That(catalog.GetLevelId(0), Is.EqualTo("B")); Assert.That(catalog.IsAvailable(0), Is.True);
            Assert.That(catalog.IsAvailable(1), Is.False); Assert.That(reads, Is.Zero);
        }
        [Test]
        public void ArchiveResumesOnlyItsOriginalNumberWhileNewAttemptsUseTheNewLayout()
        {
            var old = LevelJsonWriter.Write(new LevelData { SchemaVersion=2, LevelId="old", Width=3, Height=3, BossId="TF_KRAKEN_01", Ships=Array.Empty<ShipPlacementData>() });
            var fresh = old.Replace("old", "new");
            var manifest = new Newtonsoft.Json.Linq.JObject {
                ["manifestVersion"]=1,["rulesVersion"]=LevelRules.Version,
                ["levels"]=new Newtonsoft.Json.Linq.JArray(
                    new Newtonsoft.Json.Linq.JObject { ["levelId"]="new",["layoutFile"]="new.json" },
                    new Newtonsoft.Json.Linq.JObject { ["levelId"]="second",["layoutFile"]="missing.json" }),
                ["previousRevisions"]=new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject {
                    ["number"]=1,["levelId"]="old",["layoutFile"]="old.json",["layoutSha256"]=CampaignPackValidator.Sha256(old) }) };
            var reads=0;
            var catalog=PlayableLevelCatalog.FromManifest(manifest.ToString(),new Dictionary<string,Func<string>> {
                ["old.json"]=()=>{reads++;return old;},["new.json"]=()=>fresh });
            Assert.That(reads,Is.Zero);
            Assert.That(catalog.Count,Is.EqualTo(2));
            Assert.That(catalog.Load(0).LevelId,Is.EqualTo("new"));
            Assert.That(PlayableLevelCatalog.LoadForAttempt(catalog,0,"old").LevelId,Is.EqualTo("old"));
            Assert.That(reads,Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(()=>PlayableLevelCatalog.LoadForAttempt(catalog,1,"old"));
            Assert.Throws<InvalidOperationException>(()=>PlayableLevelCatalog.LoadForAttempt(catalog,0,"unshipped"));
            old=old.Replace("3", "4");
            Assert.Throws<InvalidOperationException>(()=>PlayableLevelCatalog.LoadForAttempt(catalog,0,"old"));
        }
    }
}
