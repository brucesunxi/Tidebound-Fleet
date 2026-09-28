using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Save;

namespace Tidebound.Tests
{
    public sealed class AppearanceSaveTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;public int Writes;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData value){if(Fail)throw new IOException();Data=value.Copy();Writes++;}
        }
        [Test]
        public void NewPlayerOwnsOnlyDefaultInEachCategoryAndLockedCannotBeSelected()
        {
            var s=new PlayerSaveService(new Store());
            foreach(AppearanceKind k in Enum.GetValues(typeof(AppearanceKind)))Assert.That(AppearanceCatalog.All.Count(x=>x.Kind==k&&s.OwnsAppearance(x.Id)),Is.EqualTo(1));
            Assert.That(s.SelectAppearance("TF_SKIN_K05"),Is.EqualTo(AppearanceSelectionStatus.Locked));
            Assert.That(s.SelectAppearance("TF_SCENE_S02"),Is.EqualTo(AppearanceSelectionStatus.Locked));
            Assert.That(s.SelectAppearance("TF_TRAIL_W02"),Is.EqualTo(AppearanceSelectionStatus.Locked));
        }
        [Test]
        public void LegacyAliasesPreserveAllSixteenQualitiesAndHaveNoCollisions()
        {
            Assert.That(SkinCatalog.All.Select(s=>AppearanceCatalog.VisualSkin(s.Id)).Distinct().Count(),Is.EqualTo(16));
            foreach(var skin in SkinCatalog.All)Assert.That(AppearanceCatalog.Find(AppearanceCatalog.VisualSkin(skin.Id)).Rarity,Is.EqualTo(skin.Rarity));
        }
        [Test]
        public void V4MigrationPreservesPaidHistoryAndSlotHolesWithoutReplayingNewCatalog()
        {
            var store=new Store{Data=CoinShopTests.Earned(10)};var old=new PlayerSaveService(store);
            Assert.That(old.Collect(Guid.NewGuid().ToString("N"),"FirstBlue"),Is.EqualTo(CollectionStatus.Saved));
            Assert.That(old.Collect(Guid.NewGuid().ToString("N"),"Single"),Is.EqualTo(CollectionStatus.Saved));
            var blue=old.Snapshot.Collection.OwnedIds.First(x=>SkinCatalog.Find(x).Rarity==SkinRarity.Uncommon);
            old.SetEquipment(Guid.NewGuid().ToString("N"),new[]{blue,null,SkinCatalog.All[0].Id,null,null});
            store.Data.Version=4;var before=store.Data.Copy();var baseline=JsonConvert.SerializeObject(before.Collection);
            var migrated=new PlayerSaveService(store);Assert.That(migrated.IsAvailable,Is.True);Assert.That(migrated.Snapshot.Version,Is.EqualTo(PlayerSaveData.CurrentVersion));
            Assert.That(JsonConvert.SerializeObject(migrated.Snapshot.Collection),Is.EqualTo(baseline));Assert.That(migrated.Coins,Is.EqualTo(before.Coins));
            Assert.That(migrated.AppearanceEquipment,Is.EqualTo(before.Collection.Equipment.Select(x=>x==null?null:AppearanceCatalog.VisualSkin(x))));
            var writes=store.Writes;Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);Assert.That(store.Writes,Is.EqualTo(writes));
        }
        [Test]
        public void MilestonesUnlockFromDurableProgressAndDoNotSpendCoins()
        {
            var store=new Store{Data=CoinShopTests.Earned(15)};var s=new PlayerSaveService(store);var coins=s.Coins;
            Assert.That(s.OwnsAppearance("TF_SKIN_K05"),Is.True);Assert.That(s.OwnsAppearance("TF_SCENE_S12"),Is.True);
            Assert.That(s.SelectAppearance("TF_SCENE_S12"),Is.EqualTo(AppearanceSelectionStatus.Saved));
            Assert.That(s.SelectAppearance("TF_SKIN_K05"),Is.EqualTo(AppearanceSelectionStatus.Saved));
            var loaded=new PlayerSaveService(store);Assert.That(loaded.SelectedSceneId,Is.EqualTo("TF_SCENE_S12"));Assert.That(loaded.AppearanceEquipment,Does.Contain("TF_SKIN_K05"));Assert.That(loaded.Coins,Is.EqualTo(coins));
            Assert.That(loaded.OwnsAppearance("TF_SCENE_S03"),Is.False);Assert.That(loaded.OwnsAppearance("TF_SKIN_K04"),Is.False);
        }
        [Test]
        public void FailedWriteIsAtomicAndSnapshotsDoNotMutateLiveSelection()
        {
            var store=new Store{Data=CoinShopTests.Earned(25)};var s=new PlayerSaveService(store);var before=JsonConvert.SerializeObject(s.Snapshot);store.Fail=true;
            Assert.That(s.SelectAppearance("TF_SCENE_S02"),Is.EqualTo(AppearanceSelectionStatus.StorageUnavailable));Assert.That(JsonConvert.SerializeObject(s.Snapshot),Is.EqualTo(before));
            store.Fail=false;Assert.That(s.SelectAppearance("TF_SKIN_K05"),Is.EqualTo(AppearanceSelectionStatus.Saved));
            var copy=s.Snapshot;copy.Appearance.Equipment[0]="bad";Assert.That(s.AppearanceEquipment[0],Is.EqualTo(AppearanceCatalog.DefaultSkin));
        }
        [Test]
        public void AllFiveSlotsAndEmptyFallbackRemainValid()
        {
            var s=new PlayerSaveService(new Store{Data=CoinShopTests.Earned(100)});
            foreach(var id in new[]{"TF_SKIN_K02","TF_SKIN_K05","TF_SKIN_K06","TF_SKIN_K08"})Assert.That(s.SelectAppearance(id),Is.EqualTo(AppearanceSelectionStatus.Saved));
            Assert.That(s.SelectAppearance("TF_SKIN_K07"),Is.EqualTo(AppearanceSelectionStatus.SlotsFull));
            foreach(var id in s.AppearanceEquipment.Where(x=>x!=null).ToArray())Assert.That(s.SelectAppearance(id),Is.EqualTo(AppearanceSelectionStatus.Saved));
            Assert.That(s.AppearanceEquipment,Is.EqualTo(new string[5]));Assert.DoesNotThrow(()=>s.Snapshot.Validate());
        }
        [Test]
        public void NewSkinEquipmentSurvivesActualAttemptSaveAndDoesNotReplaceAnOngoingFleet()
        {
            var store=new Store{Data=CoinShopTests.Earned(100)};var service=new PlayerSaveService(store);
            service.SelectAppearance(AppearanceCatalog.DefaultSkin);service.SelectAppearance("TF_SKIN_K17");
            var level=new Tidebound.Config.LevelData{SchemaVersion=2,LevelId="AppearanceReview",Width=8,Height=8,BossId="Kraken",Ships=new[]{
                new Tidebound.Board.ShipPlacementData{Id="A",TypeId=Tidebound.Core.FoundationLimits.BaseShipTypeId,Length=2,Position=new Tidebound.Board.GridPosition(1,1),Direction=Tidebound.Ship.ShipDirection.Up},
                new Tidebound.Board.ShipPlacementData{Id="B",TypeId=Tidebound.Core.FoundationLimits.BaseShipTypeId,Length=3,Position=new Tidebound.Board.GridPosition(4,1),Direction=Tidebound.Ship.ShipDirection.Up}}};
            using(var runtime=service.CreateNextAttempt(level))
            {
                Assert.That(service.Start(runtime),Is.True);var before=JsonConvert.SerializeObject(runtime.Capture());
                Assert.That(runtime.Capture().Ships.Single(x=>x.Length==2).SkinId,Is.EqualTo("TF_SKIN_K17"));
                Assert.That(runtime.Capture().Ships.Single(x=>x.Length==3).SkinId,Is.EqualTo(Tidebound.Core.FoundationLimits.DefaultLongSkinId));
                Assert.That(service.SelectAppearance("TF_SKIN_K17"),Is.EqualTo(AppearanceSelectionStatus.Saved));
                Assert.That(service.SelectAppearance("TF_SKIN_K18"),Is.EqualTo(AppearanceSelectionStatus.Locked));
                Assert.That(JsonConvert.SerializeObject(runtime.Capture()),Is.EqualTo(before));
                using(var restored=SavedGameRuntime.Restore(store.Data.Attempt))Assert.That(restored.Session.Ships.First().Length,Is.GreaterThan(0));
            }
        }
    }
}
