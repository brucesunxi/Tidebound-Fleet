using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Save;

namespace Tidebound.Tests
{
    public sealed class AppearanceDrawTests
    {
        private sealed class Store:IPlayerSaveStore
        {public PlayerSaveData Data;public bool Fail;public PlayerSaveData Load()=>Data?.Copy();public void Save(PlayerSaveData d){if(Fail)throw new IOException();Data=d.Copy();}}
        [Test]
        public void NewPoolExcludesLevelShareAndPreservesRarityWeightsThroughAllTiers()
        {
            Assert.That(AppearanceDrawEngine.Pool.Length,Is.EqualTo(14));Assert.That(AppearanceDrawEngine.Pool.GroupBy(x=>x.Rarity).Count(),Is.EqualTo(5));
            var legacy=new CollectionData();var state=AppearanceDrawEngine.Replay(legacy,Array.Empty<CollectionReceipt>(),101);var receipts=new List<CollectionReceipt>();
            receipts.Add(AppearanceDrawEngine.Generate(state,legacy.ProfileSeed,Guid.NewGuid().ToString("N"),"FirstBlue",101));
            for(var n=0;n<20;n++)
            {
                var r=AppearanceDrawEngine.Generate(state,legacy.ProfileSeed,Guid.NewGuid().ToString("N"),"Ten",101);receipts.Add(r);
                Assert.That(r.SkinIds.All(id=>AppearanceCatalog.Find(id).Source==ShowcaseSourceGroup.Draw),Is.True);
                Assert.That(r.SkinIds.Any(id=>AppearanceCatalog.Find(id).Rarity>=SkinRarity.Rare),Is.True);
                Assert.That(state.GoldDry,Is.LessThan(20));Assert.That(state.RedDry,Is.LessThan(60));
            }
            var replay=AppearanceDrawEngine.Replay(legacy,receipts.ToArray(),101);
            Assert.That(replay.Tickets,Is.EqualTo(state.Tickets));Assert.That(replay.Owned,Is.EquivalentTo(state.Owned));Assert.That(replay.Draws,Is.EqualTo(200));
            receipts.Last().CoinCost++;Assert.Throws<ArgumentException>(()=>AppearanceDrawEngine.Replay(legacy,receipts.ToArray(),101));
        }
        [Test]
        public void FailedGiftAndPaidDrawAreAtomicIdempotentAndPreserveLegacyLedger()
        {
            var store=new Store{Data=CoinShopTests.Earned(100)};var service=new PlayerSaveService(store);var originalLegacy=JsonConvert.SerializeObject(service.Snapshot.Collection);
            var gift=Guid.NewGuid().ToString("N");store.Fail=true;
            Assert.That(service.CollectAppearance(gift,"FirstBlue"),Is.EqualTo(CollectionStatus.StorageUnavailable));Assert.That(service.CanClaimFirstBlue,Is.True);
            store.Fail=false;Assert.That(service.CollectAppearance(gift,"FirstBlue"),Is.EqualTo(CollectionStatus.Saved));Assert.That(service.CollectAppearance(gift,"FirstBlue"),Is.EqualTo(CollectionStatus.AlreadySaved));
            var price=service.AppearanceDrawState.Price*9;var coins=service.Coins;var request=Guid.NewGuid().ToString("N");store.Fail=true;
            Assert.That(service.CollectAppearance(request,"Ten"),Is.EqualTo(CollectionStatus.StorageUnavailable));Assert.That(service.Coins,Is.EqualTo(coins));
            store.Fail=false;Assert.That(service.CollectAppearance(request,"Ten"),Is.EqualTo(CollectionStatus.Saved));var results=service.AppearanceReceiptFor(request).SkinIds;
            Assert.That(service.Coins,Is.EqualTo(coins-price));var reloaded=new PlayerSaveService(store);Assert.That(reloaded.IsAvailable,Is.True);
            Assert.That(reloaded.CollectAppearance(request,"Ten"),Is.EqualTo(CollectionStatus.AlreadySaved));Assert.That(reloaded.Coins,Is.EqualTo(coins-price));
            Assert.That(reloaded.AppearanceReceiptFor(request).SkinIds,Is.EqualTo(results));Assert.That(JsonConvert.SerializeObject(reloaded.Snapshot.Collection),Is.EqualTo(originalLegacy));
            Assert.That(reloaded.CollectAppearance(request,"Single"),Is.EqualTo(CollectionStatus.RequestConflict));
            Assert.That(reloaded.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue"),Is.EqualTo(CollectionStatus.InvalidSelection));
            Assert.That(reloaded.AppearanceEquipment,Is.EqualTo(new[]{AppearanceCatalog.DefaultSkin,null,null,null,null}));
        }
        [Test]
        public void MilestoneGrantIsNotChargedOrCountedAsADrawAndExclusiveSkinCannotBeExchanged()
        {
            var service=new PlayerSaveService(new Store{Data=CoinShopTests.Earned(100)});
            Assert.That(service.OwnsAppearance("TF_SKIN_K05"),Is.True);Assert.That(service.AppearanceDrawState.Price,Is.EqualTo(300));
            service.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue");
            Assert.That(service.CollectAppearance(Guid.NewGuid().ToString("N"),"Exchange","TF_SKIN_K18"),Is.EqualTo(CollectionStatus.InvalidSelection));
            Assert.That(service.AppearanceDrawState.Draws,Is.Zero);
        }
        [Test]
        public void ExistingPityTicketsAndOwnedAliasesSeedTheNewPoolWithoutLoss()
        {
            var store=new Store{Data=CoinShopTests.Earned(100)};var service=new PlayerSaveService(store);
            service.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");for(var i=0;i<5;i++)service.Collect(Guid.NewGuid().ToString("N"),"Ten");
            var old=service.Snapshot.Collection;var state=service.AppearanceDrawState;
            Assert.That(state.Tickets,Is.EqualTo(old.Tickets));Assert.That(state.GoldDry,Is.EqualTo(old.GoldDry));Assert.That(state.RedDry,Is.EqualTo(old.RedDry));
            Assert.That(state.Owned,Is.EquivalentTo(old.OwnedIds.Select(AppearanceCatalog.VisualSkin)));
            var before=JsonConvert.SerializeObject(old);Assert.That(service.CollectAppearance(Guid.NewGuid().ToString("N"),"Single"),Is.EqualTo(CollectionStatus.Saved));
            Assert.That(JsonConvert.SerializeObject(service.Snapshot.Collection),Is.EqualTo(before));Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);
        }
    }
}
