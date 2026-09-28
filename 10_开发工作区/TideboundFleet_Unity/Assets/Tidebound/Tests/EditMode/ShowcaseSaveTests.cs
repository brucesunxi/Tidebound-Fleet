using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class ShowcaseSaveTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public int Writes;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData data){if(Fail)throw new IOException();Data=data.Copy();Writes++;}
        }
        [TestCase(0,1)][TestCase(2,1)][TestCase(3,2)][TestCase(5,2)][TestCase(6,3)][TestCase(9,3)][TestCase(10,4)]
        public void OwnershipUsesCommittedMilestonesAndOlderPlayersDoNotNeedReplay(int level,int count)
        {
            var store=new Store{Data=CoinShopTests.Earned(level)};var save=new PlayerSaveService(store);
            Assert.That(save.IsAvailable,Is.True);Assert.That(ShowcaseCatalog.All.Count(s=>save.OwnsShowcase(s.Id)),Is.EqualTo(count));
            Assert.That(save.SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.DefaultId));Assert.That(store.Writes,Is.Zero);
        }
        [Test]
        public void SelectionPersistsAndFailedWritesDoNotChangeAppearanceOrOtherAccountState()
        {
            var store=new Store{Data=CoinShopTests.Earned(10)};var save=new PlayerSaveService(store);var before=save.Snapshot;
            Assert.That(save.SelectShowcase(ShowcaseCatalog.All[1].Id),Is.EqualTo(ShowcaseSelectionStatus.Saved));var writes=store.Writes;
            Assert.That(save.SelectShowcase(ShowcaseCatalog.All[1].Id),Is.EqualTo(ShowcaseSelectionStatus.AlreadySelected));Assert.That(store.Writes,Is.EqualTo(writes));
            store.Fail=true;Assert.That(save.SelectShowcase(ShowcaseCatalog.All[2].Id),Is.EqualTo(ShowcaseSelectionStatus.StorageUnavailable));
            Assert.That(save.SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.All[1].Id));Assert.That(store.Data.ShowcaseId,Is.EqualTo(ShowcaseCatalog.All[1].Id));
            store.Fail=false;Assert.That(save.SelectShowcase(ShowcaseCatalog.All[2].Id),Is.EqualTo(ShowcaseSelectionStatus.Saved));
            save=new PlayerSaveService(store);Assert.That(save.SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.All[2].Id));
            var after=save.Snapshot;Assert.That(after.Coins,Is.EqualTo(before.Coins));Assert.That(after.CurrentLevel,Is.EqualTo(before.CurrentLevel));
            Assert.That(JsonConvert.SerializeObject(after.Collection),Is.EqualTo(JsonConvert.SerializeObject(before.Collection)));
            Assert.That(JsonConvert.SerializeObject(after.Tools),Is.EqualTo(JsonConvert.SerializeObject(before.Tools)));
        }
        [Test]
        public void LockedUnknownOrInvalidSavedSelectionsCannotBeApplied()
        {
            var store=new Store{Data=CoinShopTests.Earned(2)};var save=new PlayerSaveService(store);
            Assert.That(save.SelectShowcase(ShowcaseCatalog.All[1].Id),Is.EqualTo(ShowcaseSelectionStatus.Locked));
            Assert.That(save.SelectShowcase("bad"),Is.EqualTo(ShowcaseSelectionStatus.InvalidShip));Assert.That(store.Writes,Is.Zero);
            foreach(var id in new[]{"bad",null,ShowcaseCatalog.All[1].Id}){var invalid=store.Data.Copy();invalid.ShowcaseId=id;Assert.Throws<ArgumentException>(()=>invalid.Validate());}
        }
        [Test]
        public void FailedVictorySaveCannotUnlockButSuccessfulRetryUnlocksExactlyOnce()
        {
            var store=new Store{Data=CoinShopTests.Earned(2)};var save=new PlayerSaveService(store);
            using(var game=new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,LevelSolverTests.Ship("A",1,1,ShipDirection.Up)),3))
            {
                Assert.That(save.Start(game),Is.True);var move=game.Movement.TryBeginMove("A");game.Movement.CompleteTravel(move.Operation.OperationId);
                game.Transit.Advance(100);game.Combat.Advance();Assert.That(game.Combat.IsVictorious,Is.True);
                store.Fail=true;Assert.That(save.Checkpoint(true),Is.False);Assert.That(save.OwnsShowcase(ShowcaseCatalog.All[1].Id),Is.False);
                store.Fail=false;Assert.That(save.Checkpoint(true),Is.True);Assert.That(save.OwnsShowcase(ShowcaseCatalog.All[1].Id),Is.True);
                var coins=save.Coins;var settlements=save.Snapshot.Settlements.Length;save.Checkpoint(true);
                Assert.That(save.Coins,Is.EqualTo(coins));Assert.That(save.Snapshot.Settlements.Length,Is.EqualTo(settlements));
                Assert.That(new PlayerSaveService(store).OwnsShowcase(ShowcaseCatalog.All[1].Id),Is.True);
            }
        }
        [TestCase(2)][TestCase(3)][TestCase(4)]
        public void HistoricalFileWithoutSelectionDefaultsSafelyAndNewFieldRoundTrips(int version)
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundShowcase_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                var data=CoinShopTests.Earned(10);data.Version=version;var json=JObject.FromObject(data);json.Remove("ShowcaseId");
                var payload=json.ToString(Formatting.None);string digest;using(var sha=SHA256.Create())digest=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
                var path=Path.Combine(folder,"player.json");File.WriteAllText(path,JsonConvert.SerializeObject(new{EnvelopeVersion=1,Payload=payload,Digest=digest}));
                var store=new PlayerSaveFileStore(path);var save=new PlayerSaveService(store);Assert.That(save.IsAvailable,Is.True);
                Assert.That(save.SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.DefaultId));Assert.That(save.OwnsShowcase(ShowcaseCatalog.All[3].Id),Is.True);
                Assert.That(save.SelectShowcase(ShowcaseCatalog.All[3].Id),Is.EqualTo(ShowcaseSelectionStatus.Saved));
                Assert.That(new PlayerSaveService(store).SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.All[3].Id));
            }
            finally{Directory.Delete(folder,true);}
        }
    }
}
