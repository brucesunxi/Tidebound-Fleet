using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class CampaignV3PackTests
    {
        static string Folder=>Path.Combine(Application.dataPath,"Tidebound/Config/Levels/CampaignV3");
        static string Read(string file)=>File.ReadAllText(Path.Combine(Folder,file));
        [Test]
        public void HundredNewLayoutsReplayCorrectRouteWrongBranchesToolsBattleAndCheckpoints()
        {
            var result=CampaignV3Validator.Validate(Read("manifest-100.json"),Read);
            Assert.That((int)result["ships"],Is.EqualTo(8745));
            Assert.That((int)result["certifiedDeadlocksAndToolRecoveries"],Is.EqualTo(191));
            Assert.That((int)result["partialMoves"],Is.EqualTo(385));
            Assert.That((int)result["restoredCheckpoints"],Is.EqualTo(100));
        }
        [Test]
        public void ValidatedThirtyAreAnUnchangedPrefixOfTheHundred()
        {
            var first=JObject.Parse(Read("manifest-30.json"));var full=JObject.Parse(Read("manifest-100.json"));
            for(int i=0;i<30;i++)Assert.That(JToken.DeepEquals(first["levels"][i],full["levels"][i]),Is.True);
        }
        [Test]
        public void EveryLegacyAttemptResolvesToItsExactOldLayoutAndBossBudget()
        {
            var text=Read("manifest-100.json");var manifest=JObject.Parse(text);var assets=new Dictionary<string,Func<string>>();
            foreach(var row in manifest["levels"]){var file=(string)row["layoutFile"];assets.Add(file,()=>Read(file));}
            foreach(var row in manifest["previousRevisions"])
            {
                var file=(string)row["layoutFile"];var path=Path.Combine(Application.dataPath,file.StartsWith("P5R_Ten_")?
                    "Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates":"Tidebound/Config/Levels/Campaign",file);
                assets.Add(file,()=>File.ReadAllText(path));
            }
            var catalog=PlayableLevelCatalog.FromManifest(text,assets);
            foreach(var row in manifest["previousRevisions"])
            {
                int n=(int)row["number"];var old=PlayableLevelCatalog.LoadForAttempt(catalog,n-1,(string)row["levelId"]);
                using(var oldAttempt=SavedGameRuntime.Create(old,n))using(var restored=SavedGameRuntime.Restore(oldAttempt.Capture()))
                {
                    Assert.That(restored.Capture().LayoutFingerprint,Is.EqualTo(oldAttempt.Capture().LayoutFingerprint));
                    Assert.That(restored.Session.Boss.InitialHp,Is.EqualTo(old.Ships.Length*10));
                    using(var next=SavedGameRuntime.Create(catalog.Load(n-1),n))
                    {
                        Assert.That(PlayableLevelCatalog.CanReplaceAttempt(catalog,oldAttempt.Capture(),next),Is.True);
                        var changed=oldAttempt.Capture();changed.LayoutFingerprint="wrong";
                        Assert.That(PlayableLevelCatalog.CanReplaceAttempt(catalog,changed,next),Is.False);
                    }
                }
                Assert.That(catalog.Load(n-1).LevelId,Is.EqualTo("TF_V3_"+n.ToString("D3")));
            }
        }
        [TestCase("proof")][TestCase("risk")][TestCase("hp")]
        public void TamperedEvidenceFailsEvenWithAnUpdatedAssetChecksum(string mutation)
        {
            var manifest=JObject.Parse(Read("manifest-30.json"));var row=manifest["levels"][4];
            string file,changed;
            if(mutation=="proof")
            {
                file=(string)row["proofFile"];var proof=JObject.Parse(Read(file));((JArray)proof["solutionSteps"]).RemoveAt(0);changed=proof.ToString();row["proofSha256"]=CampaignPackValidator.Sha256(changed);
            }
            else if(mutation=="risk")
            {
                file=(string)row["analysisFile"];var report=JObject.Parse(Read(file));report["riskPoints"][0]["wrongMove"]="K0A";changed=report.ToString();row["analysisSha256"]=CampaignPackValidator.Sha256(changed);
            }
            else{row["ships"]=90;file="unused";changed="";}
            Assert.Throws<ArgumentException>(()=>CampaignV3Validator.Validate(manifest.ToString(),name=>name==file?changed:Read(name)));
        }
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData data){if(Fail)throw new IOException();Data=data.Copy();}
        }
        [TestCase(false)][TestCase(true)]
        public void RevisionReplacementUsesOneAtomicRestartReceiptAndKeepsInventory(bool home)
        {
            var original=LevelJsonReader.Read(File.ReadAllText(Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates/P5R_Ten_001.json")));
            var store=new Store();var service=new PlayerSaveService(store);
            using(var old=SavedGameRuntime.Create(original,1))using(var next=SavedGameRuntime.Create(LevelJsonReader.Read(Read("TF_V3_001.json")),1))
            {
                Assert.That(service.Start(old),Is.True);service.Inventory.Grant("revision-stock",2,1,3);
                Func<AttemptSaveData,SavedGameRuntime,bool> allowed=(prior,replacement)=>prior.LevelId==original.LevelId&&replacement.Session.LevelId=="TF_V3_001";
                Assert.That(home?service.StartFromHome(next):service.Restart(next),Is.False,"No default permission to change an identity");
                var before=service.Snapshot;store.Fail=true;
                Assert.That(home?service.StartFromHome(next,allowed):service.Restart(next,allowed),Is.False);
                Assert.That(service.Snapshot.Attempt.AttemptId,Is.EqualTo(before.Attempt.AttemptId));Assert.That(service.DailyRestartCount,Is.Zero);
                store.Fail=false;
                Assert.That(home?service.StartFromHome(next,allowed):service.Restart(next,allowed),Is.True);
                Assert.That(service.DailyRestartCount,Is.EqualTo(1));Assert.That(service.Snapshot.Settlements.Length,Is.EqualTo(1));
                Assert.That(service.Inventory.Count(Tidebound.Tools.ShipTool.Reverse),Is.EqualTo(3));Assert.That(service.Coins,Is.EqualTo(before.Coins));
                Assert.That(home?service.StartFromHome(next,allowed):service.Restart(next,allowed),Is.False);
                Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);
            }
        }
        [Test]
        public void DifferentFleetCountsCannotHideCopiedGeometry()
        {
            var a=LevelJsonReader.Read(Read("TF_V3_050.json"));var b=LevelJsonReader.Read(LevelJsonWriter.Write(a));b.Ships=b.Ships.Skip(1).ToArray();
            foreach(var ship in b.Ships)ship.Id="renamed_"+ship.Id;
            Assert.That(CampaignV3Validator.GeometryOverlap(a,b),Is.EqualTo(1));
        }
    }
}
