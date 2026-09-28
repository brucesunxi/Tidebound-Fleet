using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Unity.LevelDesign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class CampaignPackTests
    {
        private static string Folder => Path.Combine(Application.dataPath, "Tidebound/Config/Levels/Campaign");
        private static string Read(string file) => File.ReadAllText(Path.Combine(file.StartsWith("P5R_Ten_", StringComparison.Ordinal)
            ? Path.Combine(Application.dataPath, "Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates") : Folder, file));
        [Test]
        public void ThirtyLevelsKeepLegacyIdentityAndPassContentGate()
        {
            Assert.That(CampaignLevelPlan.All.Count, Is.EqualTo(100));
            Assert.That(CampaignLevelPlan.All.Take(10).Select(x => x.Recipe), Is.EqualTo(Phase5RLevelRecipes.All));
            Assert.That(CampaignLevelPlan.All.Select(x => x.Recipe.LevelId).Distinct().Count(), Is.EqualTo(100));
            CampaignPackValidator.Validate(Read("manifest-30.json"), Read);
            foreach (var n in new[] { 15, 25 }) Assert.That(CampaignLevelPlan.All[n - 1].Pace, Is.EqualTo(CampaignPace.Relief));
        }
        [Test]
        public void HundredLevelPackExtendsTheExactValidatedThirty()
        {
            var first = JObject.Parse(Read("manifest-30.json"));
            var full = JObject.Parse(Read("manifest-100.json"));
            CampaignPackValidator.Validate(full.ToString(), Read);
            Assert.That(full["levels"].Count(), Is.EqualTo(100));
            for (var i = 0; i < 30; i++) Assert.That(JToken.DeepEquals(first["levels"][i], full["levels"][i]), Is.True, "Changed prefix " + i);
        }
        [Test]
        public void PortraitSceneReferencesAllHundredLayoutsAndProofs()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/Tidebound/Scenes/Phase5R_PortraitGraybox.unity");
                var game = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PortraitPuzzleGraybox>(true)).Single();
                var serialized = new SerializedObject(game);
                var folder=Path.Combine(Application.dataPath,"Tidebound/Config/Levels/CampaignV3");
                var manifest=JObject.Parse(File.ReadAllText(Path.Combine(folder,"manifest-100.json")));
                Assert.That(((TextAsset)serialized.FindProperty("manifest").objectReferenceValue).text,Is.EqualTo(manifest.ToString()));
                var layouts=serialized.FindProperty("layouts");var proofs=serialized.FindProperty("proofs");
                Assert.That(layouts.arraySize,Is.EqualTo(200));Assert.That(proofs.arraySize,Is.EqualTo(100));
                for(var i=0;i<100;i++)
                {
                    var id=(string)manifest["levels"][i]["levelId"];
                    Assert.That(((TextAsset)layouts.GetArrayElementAtIndex(i).objectReferenceValue).text,Is.EqualTo(File.ReadAllText(Path.Combine(folder,id+".json"))));
                    Assert.That(((TextAsset)proofs.GetArrayElementAtIndex(i).objectReferenceValue).text,Is.EqualTo(File.ReadAllText(Path.Combine(folder,id+".solution.json"))));
                    Assert.That(((TextAsset)layouts.GetArrayElementAtIndex(i+100).objectReferenceValue).text,Is.EqualTo(Read((string)manifest["previousRevisions"][i]["layoutFile"])));
                }
            }
            finally
            {
                if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
        [TestCase("order")]
        [TestCase("missing")]
        [TestCase("revision")]
        [TestCase("hash")]
        [TestCase("family")]
        [TestCase("status")]
        public void TamperedPackCannotBeCertified(string mutation)
        {
            var pack = JObject.Parse(Read("manifest-30.json")); var rows = (JArray)pack["levels"];
            switch (mutation)
            {
                case "order": rows[10]["levelId"] = "wrong"; break;
                case "missing": rows.RemoveAt(29); break;
                case "revision": rows[10]["contentRevision"] = 0; break;
                case "hash": rows[10]["layoutSha256"] = "stale"; break;
                case "family": rows[10]["structure"] = "LongBridge"; break;
                case "status": pack["status"] = "Approved"; break;
            }
            Assert.Throws<ArgumentException>(() => CampaignPackValidator.Validate(pack.ToString(), Read));
        }
        [Test]
        public void ChangedSolutionIsRejectedEvenIfItsFileChecksumIsUpdated()
        {
            var pack = JObject.Parse(Read("manifest-30.json"));
            var id = CampaignLevelPlan.All[10].Recipe.LevelId; var file = id + ".solution.json";
            var proof = JObject.Parse(Read(file)); ((JArray)proof["solutionSteps"]).RemoveAt(0);
            var altered = proof.ToString(); pack["levels"][10]["proofSha256"] = CampaignPackValidator.Sha256(altered);
            Assert.Throws<ArgumentException>(() => CampaignPackValidator.Validate(pack.ToString(), name => name == file ? altered : Read(name)));
        }
        [Test]
        public void GeometryComparisonIgnoresShipIdentityAndArrayOrder()
        {
            var a = LevelJsonReader.Read(Read(CampaignLevelPlan.All[10].Recipe.LevelId + ".json"));
            var b = LevelJsonReader.Read(LevelJsonWriter.Write(a));
            b.Ships = b.Ships.Reverse().ToArray();
            foreach (var s in b.Ships) s.Id = "renamed_" + s.Id;
            using (var first = CampaignPackValidator.Create(a))
            using (var second = CampaignPackValidator.Create(b))
                Assert.That(CampaignPackValidator.GeometryOverlap(first.Board, second.Board), Is.EqualTo(1));
        }
    }
}
