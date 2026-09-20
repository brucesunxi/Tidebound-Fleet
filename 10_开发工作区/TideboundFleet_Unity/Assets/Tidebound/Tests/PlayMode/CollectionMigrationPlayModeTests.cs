using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class CollectionMigrationPlayModeTests
    {
        private static string Id()=>Guid.NewGuid().ToString("N");
        private static CandidateLevelCatalog Catalog()
        {
            var p=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(p,"manifest.json")),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".json"))),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".solution.json"))));
        }
        private static PlayerSaveData Earned(int levels)
        {
            var d=new PlayerSaveData{CurrentLevel=levels+1,HighestClearedLevel=levels,
                Settlements=Enumerable.Range(1,levels).Select(n=>new SettlementRecord{AttemptId=Id(),LevelId="Review"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
            d.Coins=d.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);return d;
        }
        private static string WriteV3(string path,PlayerSaveData d)
        {
            var json=JObject.FromObject(d);json["Version"]=3;json.Remove("Collection");var payload=json.ToString(Formatting.None);string digest;
            using(var sha=SHA256.Create())digest=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            var bytes=JsonConvert.SerializeObject(new{EnvelopeVersion=1,Payload=payload,Digest=digest});Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,bytes);return bytes;
        }
        private static PortraitPuzzleGraybox Create(PlayerSaveService service)
        {var g=new GameObject("Collection_Migration").AddComponent<PortraitPuzzleGraybox>();g.Initialize(Catalog(),saveService:service,campaign:true,animateEntry:true);return g;}
        [UnityTest]
        public IEnumerator V3ActiveLanePurchaseAndSpentToolSurviveMigrationAndEquipmentChange()
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundCollectionPlay_"+Id());var path=Path.Combine(folder,"player-save-v2.json");
            PortraitPuzzleGraybox g=null;
            try
            {
                var memory=new MemoryPlayerSaveStore();memory.Save(Earned(2));g=Create(new PlayerSaveService(memory));
                var until=Time.realtimeSinceStartup+8;while(!g.IsEntryReady && Time.realtimeSinceStartup<until)yield return null;Assert.That(g.IsEntryReady,Is.True);
                var order=Id();Assert.That(g.SaveService.BuyWithCoins(order,"rescue_1",CoinShopCatalogReader.LoadDefault()),Is.EqualTo(CoinPurchaseStatus.Purchased));
                g.SelectTool(ShipTool.Rescue);g.TogglePause();var before=g.SaveService.Snapshot;Assert.That(before.Attempt.Departures.Length,Is.EqualTo(2));Assert.That(before.Attempt.ToolUses,Is.EqualTo(1));
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=null;
                var original=WriteV3(path,before);var service=new PlayerSaveService(new PlayerSaveFileStore(path));
                Assert.That(service.IsAvailable,Is.True);Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(original));
                Assert.That(JsonConvert.SerializeObject(service.Snapshot.Attempt),Is.EqualTo(JsonConvert.SerializeObject(before.Attempt)));
                g=Create(service);Assert.That(g.IsResumingEntry,Is.True);Assert.That(g.IsPaused,Is.True);Assert.That(g.Tools.UsesLeft,Is.EqualTo(4));
                Assert.That(g.SaveService.Coins,Is.EqualTo(57));Assert.That(g.Combat.Time,Is.EqualTo(before.Attempt.Elapsed));
                Assert.That(g.SaveService.Snapshot.Purchases.Single().RequestId,Is.EqualTo(order));Assert.That(g.SaveService.CanClaimFirstBlue,Is.True);
                Assert.That(service.SetEquipment(Id(),new string[5]),Is.EqualTo(EquipmentStatus.Saved));
                Assert.That(service.Snapshot.Attempt.Ships.Select(s=>s.SkinId),Is.EqualTo(before.Attempt.Ships.Select(s=>s.SkinId)));
                Assert.That(service.Snapshot.Attempt.RewardSeed,Is.EqualTo(before.Attempt.RewardSeed));
                g.TogglePause();until=Time.realtimeSinceStartup+10;while(g.Combat.HitCount<2 && Time.realtimeSinceStartup<until)yield return null;
                Assert.That(g.Combat.HitCount,Is.EqualTo(2));Assert.That(g.SaveService.Coins,Is.EqualTo(57));Assert.That(g.SaveService.Snapshot.Collection.Equipment,Is.EqualTo(new string[5]));
            }
            finally{if(g!=null)UnityEngine.Object.Destroy(g.gameObject);}yield return null;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTest]
        public IEnumerator V3WonLevelRestoresResultAndPendingFreeBlueWithoutRegrantingCoins()
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundCollectionPlay_"+Id());var path=Path.Combine(folder,"player-save-v2.json");PortraitPuzzleGraybox g=null;
            try
            {
                var memory=new MemoryPlayerSaveStore();memory.Save(Earned(1));var service=new PlayerSaveService(memory);var catalog=Catalog();
                using(var world=SavedGameRuntime.Create(catalog.Load(1),2))
                {
                    service.Start(world);var proof=LevelSolver.Solve(world.Session.Board,new LevelSolverOptions(4000,400000,500));Assert.That(proof.Status,Is.EqualTo(LevelSolverStatus.Solved));
                    foreach(var id in proof.ShipIds){var op=world.Movement.TryBeginMove(id);if(op.Operation.Stage==ShipMoveStage.Traveling)world.Movement.CompleteTravel(op.Operation.OperationId);if(world.Movement.IsBusy)world.Movement.CompleteBlockedFeedback(op.Operation.OperationId);}
                    world.Transit.Advance(100);world.Combat.Advance();Assert.That(service.Checkpoint(),Is.True);
                }
                var before=memory.Load();WriteV3(path,before);g=Create(new PlayerSaveService(new PlayerSaveFileStore(path)));yield return null;
                Assert.That(g.IsResultReadable,Is.True);Assert.That(g.CurrentResult.IsCollectionCheckpoint,Is.True);Assert.That(g.SaveService.CanClaimFirstBlue,Is.True);
                Assert.That(g.SaveService.Coins,Is.EqualTo(307));Assert.That(g.SaveService.Snapshot.Collection.Receipts,Is.Empty);Assert.That(g.SaveService.Snapshot.Collection.OwnedIds,Is.EqualTo(new[]{FoundationLimits.DefaultStandardSkinId}));
                Assert.That(g.SaveService.Snapshot.Attempt.AttemptId,Is.EqualTo(before.Attempt.AttemptId));
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(new PlayerSaveService(new PlayerSaveFileStore(path)));
                Assert.That(g.IsResultReadable,Is.True);Assert.That(g.SaveService.Coins,Is.EqualTo(307));Assert.That(g.SaveService.Snapshot.Settlements.Length,Is.EqualTo(2));
            }
            finally{if(g!=null)UnityEngine.Object.Destroy(g.gameObject);}yield return null;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
    }
}
