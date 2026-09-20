using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Core;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class CollectionSaveTests
    {
        private const string Default=FoundationLimits.DefaultStandardSkinId,Blue="TF_SKIN_UNCOMMON_01",White="TF_SKIN_COMMON_01";
        private static string Id()=>Guid.NewGuid().ToString("N");
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;public int Writes;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData value){if(Fail)throw new IOException();Data=value.Copy();Writes++;}
        }
        private static CollectionReceipt Receipt(string kind,params string[] skins)=>new CollectionReceipt{RequestId=Id(),Kind=kind,AtLevel=5,AtUtc="2026-09-20T00:00:00.0000000+00:00",SkinIds=skins};
        private static PlayerSaveData WithBlue()
        {
            var d=CoinShopTests.Earned(5);d.Collection.OwnedIds=new[]{Default,Blue};d.Collection.FirstBlueClaimed=true;
            d.Collection.Receipts=new[]{Receipt("FirstBlue",Blue)};d.Validate();return d;
        }
        private static string Envelope(JObject json)
        {
            var payload=json.ToString(Formatting.None);string digest;
            using(var sha=SHA256.Create())digest=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            return JsonConvert.SerializeObject(new{EnvelopeVersion=1,Payload=payload,Digest=digest});
        }
        [Test]
        public void CatalogContainsSixteenStandardSkinsAndNoGameplayAttributes()
        {
            Assert.That(SkinCatalog.All.Count,Is.EqualTo(16));Assert.That(SkinCatalog.All.Select(s=>s.Id).Distinct().Count(),Is.EqualTo(16));
            Assert.That(SkinCatalog.All.Count(s=>s.IsDefault),Is.EqualTo(1));Assert.That(SkinCatalog.Find(FoundationLimits.DefaultLongSkinId),Is.Null);
            Assert.That(Enum.GetValues(typeof(SkinRarity)).Cast<SkinRarity>().Select(r=>SkinCatalog.All.Count(s=>s.Rarity==r)),Is.EqualTo(new[]{5,4,3,2,2}));
            Assert.That(typeof(SkinDefinition).GetProperties().Any(p=>new[]{"Damage","Speed","GridSize","Length"}.Contains(p.Name)),Is.False);
        }
        [TestCase(0,false)][TestCase(1,false)][TestCase(2,true)][TestCase(10,true)]
        public void ProgressGrantsEligibilityButNeverSilentlyGrantsFirstBlue(int completed,bool eligible)
        {
            var d=CoinShopTests.Earned(completed);d.Version=3;var store=new Store{Data=d};var service=new PlayerSaveService(store);
            Assert.That(service.IsAvailable,Is.True);Assert.That(service.CanClaimFirstBlue,Is.EqualTo(eligible));
            Assert.That(service.Snapshot.Collection.OwnedIds,Is.EqualTo(new[]{Default}));Assert.That(service.Coins,Is.EqualTo(d.Coins));
            Assert.That(service.Snapshot.Collection.Receipts,Is.Empty);Assert.That(service.Snapshot.Collection.Tickets,Is.Zero);
            Assert.That(service.Snapshot.Collection.Equipment.Length,Is.EqualTo(5));
        }
        [Test]
        public void SnapshotCopiesCannotMutateOwnershipEquipmentOrReceipts()
        {
            var service=new PlayerSaveService(new Store{Data=WithBlue()});service.SetEquipment(Id(),new[]{Blue,null,Default,null,null});
            var copy=service.Snapshot;copy.Collection.OwnedIds[0]="bad";copy.Collection.Equipment[0]=null;
            copy.Collection.Receipts[0].SkinIds[0]="bad";copy.Collection.EquipmentReceipts[0].Slots[0]=null;
            var live=service.Snapshot;Assert.That(live.Collection.OwnedIds,Does.Contain(Default));Assert.That(live.Collection.Equipment[0],Is.EqualTo(Blue));
            Assert.That(live.Collection.Receipts[0].SkinIds[0],Is.EqualTo(Blue));Assert.That(live.Collection.EquipmentReceipts[0].Slots[0],Is.EqualTo(Blue));live.Validate();
        }
        [Test]
        public void EquipmentKeepsFivePositionsAndOldRequestsCannotUndoLaterChanges()
        {
            var store=new Store{Data=WithBlue()};var service=new PlayerSaveService(store);var request=Id();var slots=new[]{Blue,null,null,Default,null};
            Assert.That(service.SetEquipment(request,slots),Is.EqualTo(EquipmentStatus.Saved));
            Assert.That(service.SetEquipment(Id(),new string[5]),Is.EqualTo(EquipmentStatus.Saved));var writes=store.Writes;
            service=new PlayerSaveService(store);Assert.That(service.SetEquipment(request,slots),Is.EqualTo(EquipmentStatus.AlreadySaved));
            Assert.That(service.SetEquipment(request,new string[5]),Is.EqualTo(EquipmentStatus.RequestConflict));Assert.That(store.Writes,Is.EqualTo(writes));
            Assert.That(service.Snapshot.Collection.Equipment,Is.EqualTo(new string[5]));
            Assert.That(service.Snapshot.Collection.EffectiveEquipment(),Is.EqualTo(new[]{Default}));
            Assert.That(service.Coins,Is.EqualTo(WithBlue().Coins));
        }
        [TestCase(0)][TestCase(4)][TestCase(6)]
        public void WrongSlotCountIsRejected(int length)
        {
            var s=new PlayerSaveService(new Store{Data=WithBlue()});Assert.That(s.SetEquipment(Id(),new string[length]),Is.EqualTo(EquipmentStatus.InvalidEquipment));
        }
        [TestCase("TF_LONG_DEFAULT")][TestCase("TF_SKIN_LEGENDARY_01")][TestCase("")][TestCase("unknown")]
        public void LockedOrInvalidSkinsCannotBeEquipped(string skin)
        {
            var s=new PlayerSaveService(new Store{Data=WithBlue()});Assert.That(s.SetEquipment(Id(),new[]{skin,null,null,null,null}),Is.EqualTo(EquipmentStatus.InvalidEquipment));
        }
        [Test]
        public void DuplicateSlotsLockedEntryAndCrossTypeRequestReuseAreRejected()
        {
            var s=new PlayerSaveService(new Store());Assert.That(s.SetEquipment(Id(),new string[5]),Is.EqualTo(EquipmentStatus.Locked));
            var store=new Store{Data=WithBlue()};s=new PlayerSaveService(store);
            Assert.That(s.SetEquipment(Id(),new[]{Blue,Blue,null,null,null}),Is.EqualTo(EquipmentStatus.InvalidEquipment));
            var id=Id();s.SetEquipment(id,new string[5]);Assert.That(s.BuyWithCoins(id,"rescue_1",CoinShopCatalogReader.LoadDefault()),Is.EqualTo(CoinPurchaseStatus.RequestConflict));
            var purchase=Id();s.BuyWithCoins(purchase,"rescue_1",CoinShopCatalogReader.LoadDefault());
            Assert.That(s.SetEquipment(purchase,new string[5]),Is.EqualTo(EquipmentStatus.RequestConflict));
            Assert.That(s.SetEquipment(store.Data.Collection.Receipts[0].RequestId,new string[5]),Is.EqualTo(EquipmentStatus.RequestConflict));
        }
        [Test]
        public void FailedEquipmentCommitRetainsEverythingAndRetryUsesSameRequest()
        {
            var store=new Store{Data=WithBlue()};var s=new PlayerSaveService(store);var before=JsonConvert.SerializeObject(store.Data);var id=Id();
            store.Fail=true;Assert.That(s.SetEquipment(id,new string[5]),Is.EqualTo(EquipmentStatus.StorageUnavailable));
            Assert.That(JsonConvert.SerializeObject(store.Data),Is.EqualTo(before));Assert.That(s.Snapshot.Collection.Equipment[0],Is.EqualTo(Default));
            store.Fail=false;Assert.That(s.SetEquipment(id,new string[5]),Is.EqualTo(EquipmentStatus.Saved));
            Assert.That(new PlayerSaveService(store).SetEquipment(id,new string[5]),Is.EqualTo(EquipmentStatus.AlreadySaved));
            Assert.That(store.Data.Collection.EquipmentReceipts.Length,Is.EqualTo(1));
        }
        [Test]
        public void EditingEquipmentCannotChangeActiveAttemptIdentityRewardsOrAllowance()
        {
            var store=new Store{Data=WithBlue()};var s=new PlayerSaveService(store);
            using(var game=new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,LevelSolverTests.Ship("A",1,1,ShipDirection.Up)),6))
            {
                s.Start(game);var before=JsonConvert.SerializeObject(game.Capture());var board=game.Session.Board;
                Assert.That(s.SetEquipment(Id(),new[]{Blue,null,null,null,null}),Is.EqualTo(EquipmentStatus.Saved));
                Assert.That(JsonConvert.SerializeObject(game.Capture()),Is.EqualTo(before));Assert.That(game.Session.Board,Is.SameAs(board));
                Assert.That(store.Data.Attempt.Ships.Single().SkinId,Is.EqualTo(Default));Assert.That(store.Data.Attempt.ToolUses,Is.Zero);
                Assert.That(s.PrepareMove("A"),Is.True);Assert.That(s.SetEquipment(Id(),new string[5]),Is.EqualTo(EquipmentStatus.Busy));
            }
        }
        [TestCase(2)][TestCase(3)]
        public void RealLegacyJsonMigratesOnceAndPreservesBackupAndHistory(int version)
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundCollection_"+Id());var path=Path.Combine(folder,"player-save-v2.json");
            try
            {
                Directory.CreateDirectory(folder);var d=CoinShopTests.Earned(5);d.Version=version;d.Tools.Rescue=2;d.Tools.Receipts=new[]{"legacy"};
                var json=JObject.FromObject(d);json.Remove("Collection");if(version==2)json.Remove("Purchases");var original=Envelope(json);File.WriteAllText(path,original);
                var blocked=new PlayerSaveService(new PlayerSaveFileStore(path,()=>throw new IOException()));
                Assert.That(blocked.IsAvailable,Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(original));Assert.That(File.Exists(path+".tmp"),Is.False);
                var s=new PlayerSaveService(new PlayerSaveFileStore(path));Assert.That(s.IsAvailable,Is.True);Assert.That(s.Snapshot.Version,Is.EqualTo(4));
                Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(original));Assert.That(s.Coins,Is.EqualTo(d.Coins));Assert.That(s.Snapshot.Tools.Rescue,Is.EqualTo(2));
                var seed=s.Snapshot.Collection.ProfileSeed;var rev=s.Snapshot.Revision;s=new PlayerSaveService(new PlayerSaveFileStore(path));
                Assert.That(s.Snapshot.Revision,Is.EqualTo(rev));Assert.That(s.Snapshot.Collection.ProfileSeed,Is.EqualTo(seed));Assert.That(s.CanClaimFirstBlue,Is.True);
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        [TestCase("Collection")][TestCase("ProfileSeed")][TestCase("Equipment")][TestCase("Receipts")][TestCase("Version")]
        public void CurrentJsonCannotSilentlyDefaultMissingCollectionFields(string missing)
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundCollection_"+Id());var path=Path.Combine(folder,"player-save-v2.json");
            try
            {
                Directory.CreateDirectory(folder);var json=JObject.FromObject(WithBlue());
                if(missing=="Collection")json.Remove(missing);else ((JObject)json["Collection"]).Remove(missing);
                var original=Envelope(json);File.WriteAllText(path,original);var s=new PlayerSaveService(new PlayerSaveFileStore(path));
                Assert.That(s.IsAvailable,Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        [Test]
        public void CollectionReceiptReplayRejectsUnbackedOwnershipTicketsAndPity()
        {
            var data=WithBlue();data.Collection.Tickets=20;Assert.Throws<ArgumentException>(()=>data.Validate());
            data=WithBlue();data.Collection.GoldDry=1;Assert.Throws<ArgumentException>(()=>data.Validate());
            data=WithBlue();data.Collection.OwnedIds=new[]{Default,Blue,White};Assert.Throws<ArgumentException>(()=>data.Validate());
            data=WithBlue();data.Collection.Receipts=data.Collection.Receipts.Concat(data.Collection.Receipts).ToArray();Assert.Throws<ArgumentException>(()=>data.Validate());
            data=WithBlue();data.Collection.Receipts[0].SkinIds[0]=White;Assert.Throws<ArgumentException>(()=>data.Validate());
            data=WithBlue();data.Collection.CatalogVersion="future";Assert.Throws<ArgumentException>(()=>data.Validate());
            data=WithBlue();data.Collection.Equipment[1]=Blue;Assert.Throws<ArgumentException>(()=>data.Validate());
        }
        [Test]
        public void DuplicateTicketsAndExchangeReplayWithoutChangingDrawCounters()
        {
            var d=CoinShopTests.Earned(20);var c=d.Collection;var red="TF_SKIN_LEGENDARY_01";
            var draws=Enumerable.Range(0,3).Select(_=>{var r=Receipt("Single",red);r.CoinCost=300;return r;}).ToArray();
            var exchange=Receipt("Exchange",Blue);exchange.TicketCost=150;
            c.Receipts=draws.Concat(new[]{exchange}).ToArray();c.OwnedIds=new[]{Default,red,Blue};c.Tickets=450;c.TotalDraws=3;c.DuplicateDry=2;d.Coins-=900;d.Validate();
            var bad=d.Copy();bad.Collection.TotalDraws=4;Assert.Throws<ArgumentException>(()=>bad.Validate());
            bad=d.Copy();bad.Collection.Receipts.Last().TicketCost=149;Assert.That(Assert.Throws<ArgumentException>(()=>bad.Validate()).Message,Does.Contain("exchange"));
            c=new CollectionData{OwnedIds=new[]{Default,White},TotalDraws=5,GoldDry=5,RedDry=5,DuplicateDry=4,Tickets=40,
                Receipts=Enumerable.Range(0,5).Select(_=>{var r=Receipt("Single",White);r.CoinCost=300;return r;}).ToArray()};c.Validate(5);
            var duplicate=Receipt("Single",White);duplicate.CoinCost=300;c.Receipts=c.Receipts.Concat(new[]{duplicate}).ToArray();
            Assert.That(Assert.Throws<ArgumentException>(()=>c.Validate(5)).Message,Does.Contain("protection"));
        }
        [Test]
        public void GoldAndRedHardPityCannotBeSkippedByStoredResults()
        {
            var c=new CollectionData();var low=SkinCatalog.All.Where(s=>!s.IsDefault && s.Rarity<=SkinRarity.Rare).Select(s=>s.Id).ToArray();
            for(var n=1;n<=60;n++)
            {
                var chosen=n<=low.Length ? low[n-1] : n==60 ? "TF_SKIN_LEGENDARY_01" : n%20==0 ? "TF_SKIN_EPIC_01" : White;
                if(n==20 || n==60)
                {
                    var bad=c.Copy();var illegal=Receipt("Single",White);illegal.CoinCost=CollectionRules.SinglePrice(c.OwnedIds.Length-1);
                    bad.Receipts=bad.Receipts.Concat(new[]{illegal}).ToArray();
                    Assert.That(Assert.Throws<ArgumentException>(()=>bad.Validate(100)).Message,Does.Contain("protection"));
                }
                var r=Receipt("Single",chosen);r.CoinCost=CollectionRules.SinglePrice(c.OwnedIds.Length-1);c.Receipts=c.Receipts.Concat(new[]{r}).ToArray();
                var rarity=SkinCatalog.Find(chosen).Rarity;
                if(c.OwnedIds.Contains(chosen)){c.Tickets+=CollectionRules.DuplicateTickets(rarity);c.DuplicateDry=Math.Min(4,c.DuplicateDry+1);}
                else{c.OwnedIds=c.OwnedIds.Concat(new[]{chosen}).ToArray();c.DuplicateDry=0;}
                c.TotalDraws++;c.GoldDry=rarity>=SkinRarity.Epic ? 0 : c.GoldDry+1;c.RedDry=rarity==SkinRarity.Legendary ? 0 : c.RedDry+1;c.Validate(100);
            }
            Assert.That(c.TotalDraws,Is.EqualTo(60));Assert.That(c.RedDry,Is.Zero);Assert.That(c.GoldDry,Is.Zero);
        }
        [Test]
        public void EquipmentDiskFailureLeavesOriginalBytesAndSuccessfulRetrySurvivesReload()
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundCollection_"+Id());var path=Path.Combine(folder,"player-save-v2.json");
            try
            {
                var store=new PlayerSaveFileStore(path);store.Save(WithBlue());var before=File.ReadAllText(path);var fail=true;
                var s=new PlayerSaveService(new PlayerSaveFileStore(path,()=>{if(fail)throw new IOException();}));var request=Id();
                Assert.That(s.IsAvailable,Is.True);Assert.That(s.Snapshot.Collection.Receipts[0].AtUtc,Is.EqualTo(WithBlue().Collection.Receipts[0].AtUtc));
                Assert.That(s.SetEquipment(request,new string[5]),Is.EqualTo(EquipmentStatus.StorageUnavailable));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
                fail=false;Assert.That(s.SetEquipment(request,new string[5]),Is.EqualTo(EquipmentStatus.Saved));
                Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(before));Assert.That(File.Exists(path+".tmp"),Is.False);
                s=new PlayerSaveService(store);Assert.That(s.SetEquipment(request,new string[5]),Is.EqualTo(EquipmentStatus.AlreadySaved));
                Assert.That(s.Snapshot.Collection.Equipment,Is.EqualTo(new string[5]));
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
        [Test]
        public void TenReceiptLocksPriceAndEnforcesPurpleFloorAndWalletDebit()
        {
            var d=CoinShopTests.Earned(20);var items=SkinCatalog.All.Where(s=>!s.IsDefault && s.Rarity<=SkinRarity.Uncommon).Select(s=>s.Id).Concat(new[]{White,"TF_SKIN_RARE_01"}).ToArray();
            var receipt=Receipt("Ten",items);receipt.CoinCost=2700;var c=d.Collection;c.Receipts=new[]{receipt};
            c.OwnedIds=new[]{Default}.Concat(items).Distinct().ToArray();c.TotalDraws=10;c.GoldDry=c.RedDry=10;c.Tickets=10;d.Coins-=2700;d.Validate();
            var wrong=d.Copy();wrong.Collection.Receipts[0].CoinCost=4050;Assert.That(Assert.Throws<ArgumentException>(()=>wrong.Validate()).Message,Does.Contain("price"));
            wrong=d.Copy();wrong.Collection.Receipts[0].SkinIds[9]=White;Assert.That(Assert.Throws<ArgumentException>(()=>wrong.Validate()).Message,Does.Contain("protection"));
            wrong=d.Copy();wrong.Coins++;Assert.Throws<ArgumentException>(()=>wrong.Validate());
            wrong=d.Copy();wrong.Collection.Receipts[0].RulesVersion="candidate";Assert.Throws<ArgumentException>(()=>wrong.Validate());
        }
    }
}
