using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using Tidebound.Collection;
using Tidebound.Core;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Board;
using Tidebound.Boss;

namespace Tidebound.Tests
{
    public sealed class CollectionDrawTests
    {
        private static string Id()=>Guid.NewGuid().ToString("N");
        private sealed class Store:IPlayerSaveStore
        {public PlayerSaveData Data;public bool Fail;public PlayerSaveData Load()=>Data?.Copy();public void Save(PlayerSaveData d){if(Fail)throw new IOException();Data=d.Copy();}}
        private static PlayerSaveService Service(int completed=30)=>new PlayerSaveService(new Store{Data=CoinShopTests.Earned(completed)});
        private static void Gift(PlayerSaveService s)=>Assert.That(s.Collect(Id(),"FirstBlue"),Is.EqualTo(CollectionStatus.Saved));
        [TestCase(1,CollectionStatus.Locked)][TestCase(2,CollectionStatus.Saved)][TestCase(9,CollectionStatus.Saved)]
        public void FirstBlueUnlockAndCatchup(int completed,CollectionStatus expected)
        {
            var s=Service(completed);var coins=s.Coins;var id=Id();Assert.That(s.Collect(id,"FirstBlue"),Is.EqualTo(expected));
            Assert.That(s.Coins,Is.EqualTo(coins));Assert.That(s.Snapshot.Collection.TotalDraws,Is.Zero);Assert.That(s.Snapshot.Collection.GoldDry,Is.Zero);
            if(expected==CollectionStatus.Saved){Assert.That(SkinCatalog.Find(s.CollectionReceiptFor(id).SkinIds[0]).Rarity,Is.EqualTo(SkinRarity.Uncommon));Assert.That(s.Collect(Id(),"FirstBlue"),Is.EqualTo(CollectionStatus.InvalidSelection));}
            Assert.That(s.Snapshot.Collection.Equipment[0],Is.EqualTo(FoundationLimits.DefaultStandardSkinId));
        }
        [Test]
        public void PendingGiftMustPrecedePaidDrawsAndExchange()
        {var s=Service();foreach(var kind in new[]{"Single","Ten","Exchange"})Assert.That(s.Collect(Id(),kind,kind=="Exchange"?"TF_SKIN_COMMON_01":null),Is.EqualTo(CollectionStatus.FirstBluePending));}
        [TestCase("FirstBlue")][TestCase("Single")][TestCase("Ten")]
        public void FailedWriteReopenAndNewRequestCannotReroll(string kind)
        {
            var store=new Store{Data=CoinShopTests.Earned(30)};var s=new PlayerSaveService(store);if(kind!="FirstBlue")Gift(s);
            var before=s.Snapshot;var expected=before.Collection.Copy();var result=CollectionDrawEngine.Generate(expected,Id(),kind,s.CurrentLevel);
            var request=Id();store.Fail=true;Assert.That(s.Collect(request,kind),Is.EqualTo(CollectionStatus.StorageUnavailable));
            Assert.That(s.CollectionReceiptFor(request),Is.Null);Assert.That(JsonConvert.SerializeObject(s.Snapshot),Is.EqualTo(JsonConvert.SerializeObject(before)));
            store.Fail=false;s=new PlayerSaveService(store);request=Id();Assert.That(s.Collect(request,kind),Is.EqualTo(CollectionStatus.Saved));
            Assert.That(s.CollectionReceiptFor(request).SkinIds,Is.EqualTo(result.SkinIds));Assert.That(s.Coins,Is.EqualTo(before.Coins-result.CoinCost));
            var revision=s.Snapshot.Revision;Assert.That(s.Collect(request,kind),Is.EqualTo(CollectionStatus.AlreadySaved));Assert.That(s.Snapshot.Revision,Is.EqualTo(revision));
            Assert.That(s.Collect(request,"Exchange","TF_SKIN_COMMON_01"),Is.EqualTo(CollectionStatus.RequestConflict));
            s=new PlayerSaveService(store);Assert.That(s.IsAvailable,Is.True);Assert.That(s.Collect(request,kind),Is.EqualTo(CollectionStatus.AlreadySaved));
        }
        [Test]
        public void TenLocksPriceAcrossUniqueCollectionTierAndRestoresAllCounters()
        {
            var s=Service(100);Gift(s);while(s.Snapshot.Collection.OwnedIds.Length<4)s.Collect(Id(),"Single");
            var before=s.Snapshot;var id=Id();Assert.That(s.Collect(id,"Ten"),Is.EqualTo(CollectionStatus.Saved));var r=s.CollectionReceiptFor(id);
            Assert.That(r.CoinCost,Is.EqualTo(2700));Assert.That(r.SkinIds.Length,Is.EqualTo(10));Assert.That(r.SkinIds.Any(x=>SkinCatalog.Find(x).Rarity>=SkinRarity.Rare),Is.True);
            Assert.That(s.Snapshot.Collection.TotalDraws,Is.EqualTo(before.Collection.TotalDraws+10));s.Snapshot.Validate();
        }
        [TestCase(19,0,false,SkinRarity.Epic)][TestCase(19,59,true,SkinRarity.Legendary)][TestCase(0,0,true,SkinRarity.Rare)]
        public void HigherPityWins(int gold,int red,bool tenth,SkinRarity expected)
        {Assert.That(CollectionDrawEngine.Required(gold,red,tenth),Is.EqualTo(expected));var d=new CollectionData{GoldDry=gold,RedDry=red};var random=new CollectionRandom("pity");for(var i=0;i<200;i++)Assert.That(CollectionDrawEngine.Pick(d,random,expected).Rarity,Is.GreaterThanOrEqualTo(expected));}
        [Test]
        public void HundredThousandDrawsRespectRarityRatesAndHardPity()
        {
            var baseCounts=new int[5];var random=new CollectionRandom("base-rates");var d=new CollectionData();
            for(var i=0;i<100000;i++)baseCounts[(int)CollectionDrawEngine.Pick(d,random,SkinRarity.Common).Rarity]++;
            var rates=new[]{.48,.30,.15,.06,.01};for(var i=0;i<5;i++)Assert.That(baseCounts[i]/100000d,Is.EqualTo(rates[i]).Within(.005));
            random=new CollectionRandom("pity-run");
            for(var i=0;i<100000;i++){var s=CollectionDrawEngine.Pick(d,random,CollectionDrawEngine.Required(d.GoldDry,d.RedDry,false));CollectionDrawEngine.ApplyDraw(d,s);Assert.That(d.GoldDry,Is.LessThan(20));Assert.That(d.RedDry,Is.LessThan(60));}
        }
        [Test]
        public void DuplicateProtectionYieldsLastNewLowSkinButNeverOverridesHardPity()
        {
            var missing="TF_SKIN_COMMON_01";var d=new CollectionData{OwnedIds=SkinCatalog.All.Where(s=>s.Id!=missing).Select(s=>s.Id).ToArray(),DuplicateDry=4};var random=new CollectionRandom("fresh");
            for(var i=0;i<200;i++){var picked=CollectionDrawEngine.Pick(d,random,SkinRarity.Common);if(picked.Rarity<=SkinRarity.Rare)Assert.That(picked.Id,Is.EqualTo(missing));}
            Assert.That(CollectionDrawEngine.Pick(d,random,SkinRarity.Legendary).Rarity,Is.EqualTo(SkinRarity.Legendary));
            d.OwnedIds=SkinCatalog.All.Select(s=>s.Id).ToArray();var red=SkinCatalog.All.Last();CollectionDrawEngine.ApplyDraw(d,red);Assert.That(d.Tickets,Is.EqualTo(300));Assert.That(d.DuplicateDry,Is.EqualTo(4));
        }
        [Test]
        public void ExchangeDeductsTicketsOnceWithoutChangingPityAndRejectsOwned()
        {
            var data=CoinShopTests.Earned(30);CollectionDrawEngine.Generate(data.Collection,Id(),"FirstBlue",31);
            foreach(var code in new[]{"01","01","01","01","01","02","01","01"})
            {
                var skin=SkinCatalog.Find("TF_SKIN_COMMON_"+code);
                var r=new CollectionReceipt{RequestId=Id(),Kind="Single",AtLevel=31,AtUtc="2026-09-20T00:00:00.0000000+00:00",CoinCost=CollectionRules.SinglePrice(data.Collection.OwnedIds.Length-1),SkinIds=new[]{skin.Id}};
                CollectionDrawEngine.ApplyDraw(data.Collection,skin);data.Collection.Receipts=data.Collection.Receipts.Concat(new[]{r}).ToArray();data.Coins-=r.CoinCost;
            }
            data.Validate();var s=new PlayerSaveService(new Store{Data=data});var target=SkinCatalog.Find("TF_SKIN_COMMON_03");
            var before=s.Snapshot;var id=Id();Assert.That(s.Collect(id,"Exchange",target.Id),Is.EqualTo(CollectionStatus.Saved));
            Assert.That(s.Snapshot.Collection.Tickets,Is.EqualTo(before.Collection.Tickets-60));Assert.That(s.Snapshot.Collection.TotalDraws,Is.EqualTo(before.Collection.TotalDraws));
            Assert.That(s.Snapshot.Collection.GoldDry,Is.EqualTo(before.Collection.GoldDry));Assert.That(s.Coins,Is.EqualTo(before.Coins));
            Assert.That(s.Collect(id,"Exchange",target.Id),Is.EqualTo(CollectionStatus.AlreadySaved));Assert.That(s.Collect(Id(),"Exchange",target.Id),Is.EqualTo(CollectionStatus.InvalidSelection));s.Snapshot.Validate();
        }
        [Test]
        public void LockedTenInsufficientCurrencyAndConflictingEquipmentRequestsDoNotMutate()
        {
            var s=Service(2);Gift(s);Assert.That(s.Collect(Id(),"Ten"),Is.EqualTo(CollectionStatus.Locked));
            Assert.That(s.Collect(Id(),"Single"),Is.EqualTo(CollectionStatus.Saved));Assert.That(s.Collect(Id(),"Single"),Is.EqualTo(CollectionStatus.InsufficientCoins));
            var id=Id();s.SetEquipment(id,new string[5]);Assert.That(s.Collect(id,"Single"),Is.EqualTo(CollectionStatus.RequestConflict));
        }
        [TestCase(1)][TestCase(4)][TestCase(5)][TestCase(7)][TestCase(80)]
        public void BalancedBagIsStableIndependentOfInputOrderAndLongShips(int count)
        {
            var d=new CollectionData{Equipment=SkinCatalog.All.Skip(4).Take(5).Select(s=>s.Id).ToArray()};
            var source=Enumerable.Range(0,count).Select(i=>new ShipRuntimeData("s"+i,FoundationLimits.BaseShipTypeId,FoundationLimits.DefaultStandardSkinId,new GridPosition(i,0),ShipDirection.Up,2,10)).Concat(new[]{new ShipRuntimeData("long",FoundationLimits.BaseShipTypeId,FoundationLimits.DefaultLongSkinId,new GridPosition(count,0),ShipDirection.Up,3,10)}).ToArray();
            var a=CollectionShipAllocator.Assign(source,d,7);var b=CollectionShipAllocator.Assign(source.Reverse().ToArray(),d,7);
            Assert.That(a.OrderBy(s=>s.Id).Select(s=>s.SkinId),Is.EqualTo(b.OrderBy(s=>s.Id).Select(s=>s.SkinId)));
            var counts=d.Equipment.Select(id=>a.Count(s=>s.Length==2 && s.SkinId==id));Assert.That(counts.Max()-counts.Min(),Is.LessThanOrEqualTo(1));Assert.That(a.Last().SkinId,Is.EqualTo(FoundationLimits.DefaultLongSkinId));
            if(count<5)Assert.That(Enumerable.Range(0,5).SelectMany(i=>CollectionShipAllocator.Assign(source,d,i).Where(s=>s.Length==2).Select(s=>s.SkinId)).Distinct().Count(),Is.EqualTo(5));
        }
        [Test]
        public void SparseEquipmentRosterAndRewardsSurviveRestoreAndEmptyPoolFallsBack()
        {
            var skin="TF_SKIN_UNCOMMON_01";var ship=new ShipRuntimeData("a",FoundationLimits.BaseShipTypeId,skin,new GridPosition(0,0),ShipDirection.Up,2,10);
            using(var session=new GameSession("test",3,3,new[]{ship},new BossRuntimeData("boss",10)))
            using(var game=new SavedGameRuntime(session,1,caps:new System.Collections.Generic.Dictionary<string,int>{{skin,2}},equipmentSlots:new[]{null,null,null,null,skin}))
            {
                var saved=game.Capture();using(var restored=SavedGameRuntime.Restore(saved))
                {Assert.That(restored.Combat.Fleet.StandardGroups.Single().SlotIndex,Is.EqualTo(4));Assert.That(restored.Capture().Ships.Single().Coins,Is.EqualTo(saved.Ships.Single().Coins));Assert.That(restored.Capture().RewardSeed,Is.EqualTo(saved.RewardSeed));}
                var d=new CollectionData{Equipment=new string[5]};Assert.That(CollectionShipAllocator.Assign(new[]{ship},d,0)[0].SkinId,Is.EqualTo(FoundationLimits.DefaultStandardSkinId));
                saved.Ships[0].CoinCap=8;Assert.Throws<ArgumentException>(()=>SavedGameRuntime.Validate(saved));
            }
        }
    }
}
