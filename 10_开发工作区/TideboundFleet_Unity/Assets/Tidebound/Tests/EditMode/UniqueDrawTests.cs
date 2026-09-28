using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
namespace Tidebound.Tests
{
    public sealed class UniqueDrawTests
    {
        private sealed class Store:IPlayerSaveStore
        {public PlayerSaveData Data;public bool Fail;public PlayerSaveData Load()=>Data?.Copy();public void Save(PlayerSaveData x){if(Fail)throw new IOException();Data=x.Copy();}}
        private static string Id()=>Guid.NewGuid().ToString("N");
        private static PlayerSaveService Ready(Store store){store.Data=CoinShopTests.Earned(100);var s=new PlayerSaveService(store);Assert.That(s.CollectAppearance(Id(),"FirstBlue"),Is.EqualTo(CollectionStatus.Saved));return s;}
        [TestCase(DrawPool.Skin,13,46600)][TestCase(DrawPool.Trail,5,7900)][TestCase(DrawPool.Showcase,11,45700)]
        public void FinitePoolsAreCompleteUniqueAndMatchApprovedBudget(DrawPool pool,int count,int total)
        {
            for(var seed=0;seed<20;seed++)
            {
                var store=new Store();var s=Ready(store);var balance=s.Coins;
                for(var i=0;i<count;i++)
                {
                    var q=s.DrawQuote(pool);Assert.That(q.PaidCount,Is.EqualTo(i));Assert.That(q.Eligible.Length,Is.GreaterThan(0));Assert.That(q.Eligible.Sum(x=>q.Probability(x.Id)),Is.EqualTo(1).Within(.000001));
                    var request=Id();Assert.That(s.DrawUnique(request,pool,i,q.Price),Is.EqualTo(UniqueDrawStatus.Saved),s.LastError);var receipt=s.UniqueReceiptFor(request);var item=UniqueDrawEngine.Items.Single(x=>x.Id==receipt.ItemId);
                    Assert.That(UniqueDrawEngine.OpensAt(pool,item.Rarity),Is.LessThanOrEqualTo(i+1));Assert.That(s.DrawUnique(request,pool,i,q.Price),Is.EqualTo(UniqueDrawStatus.AlreadySaved));
                    s=new PlayerSaveService(store);Assert.That(s.IsAvailable,Is.True,s.LastError);
                }
                Assert.That(balance-s.Coins,Is.EqualTo(total));Assert.That(s.DrawQuote(pool).Remaining,Is.Empty);
                Assert.That(s.DrawUnique(Id(),pool,count,s.DrawQuote(pool).Price),Is.EqualTo(UniqueDrawStatus.Empty));Assert.That(s.Coins,Is.EqualTo(balance-total));
                Assert.That(s.Snapshot.Appearance.UniqueReceipts.Select(x=>x.ItemId).Distinct().Count(),Is.EqualTo(count));
            }
        }
        [Test]
        public void FailureAndStaleQuoteNeverChargeOrAdvanceAndRetriesStayIdentical()
        {
            var store=new Store();var s=Ready(store);var request=Id();var balance=s.Coins;var q=s.DrawQuote(DrawPool.Trail);store.Fail=true;
            Assert.That(s.DrawUnique(request,DrawPool.Trail,0,300),Is.EqualTo(UniqueDrawStatus.StorageUnavailable));Assert.That(s.Coins,Is.EqualTo(balance));Assert.That(s.DrawQuote(DrawPool.Trail).PaidCount,Is.Zero);
            store.Fail=false;Assert.That(s.DrawUnique(request,DrawPool.Trail,0,300),Is.EqualTo(UniqueDrawStatus.Saved));
            Assert.That(s.DrawUnique(Id(),DrawPool.Trail,0,300),Is.EqualTo(UniqueDrawStatus.QuoteChanged));Assert.That(s.DrawQuote(DrawPool.Skin).Price,Is.EqualTo(200));Assert.That(s.DrawQuote(DrawPool.Showcase).Price,Is.EqualTo(500));
            Assert.That(s.DrawUnique(request,DrawPool.Skin,0,300),Is.EqualTo(UniqueDrawStatus.RequestConflict));
            Assert.That(s.BuyWithCoins(request,"reverse_1",CoinShopCatalogReader.LoadDefault()),Is.EqualTo(CoinPurchaseStatus.RequestConflict));
            Assert.That(s.Coins,Is.EqualTo(balance-300));
        }
        [Test]
        public void AwardCanBeSelectedAndSurvivesReloadWhileNoAutoEquipOccurs()
        {
            var store=new Store();var s=Ready(store);
            foreach(var pool in new[]{DrawPool.Trail,DrawPool.Showcase})
            {
                var q=s.DrawQuote(pool);var id=Id();Assert.That(s.DrawUnique(id,pool,0,q.Price),Is.EqualTo(UniqueDrawStatus.Saved));var item=s.UniqueReceiptFor(id).ItemId;
                if(pool==DrawPool.Trail){Assert.That(s.SelectedTrailId,Is.EqualTo(AppearanceCatalog.DefaultTrail));Assert.That(s.SelectAppearance(item),Is.EqualTo(AppearanceSelectionStatus.Saved));}
                else {Assert.That(s.SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.DefaultId));Assert.That(s.SelectShowcase(item),Is.EqualTo(ShowcaseSelectionStatus.Saved));}
                s=new PlayerSaveService(store);Assert.That(s.IsAvailable,Is.True);Assert.That(pool==DrawPool.Trail?s.SelectedTrailId:s.SelectedShowcaseId,Is.EqualTo(item));
            }
        }
        [Test]
        public void InsufficientCoinsSceneAndPendingGiftDoNotMutateLedger()
        {
            var store=new Store{Data=CoinShopTests.Earned(2)};var s=new PlayerSaveService(store);
            Assert.That(s.DrawUnique(Id(),DrawPool.Skin,0,200),Is.EqualTo(UniqueDrawStatus.FirstBluePending));s.CollectAppearance(Id(),"FirstBlue");
            Assert.That(s.DrawUnique(Id(),DrawPool.Showcase,0,500),Is.EqualTo(UniqueDrawStatus.InsufficientCoins));Assert.That(s.DrawUnique(Id(),DrawPool.Scene,0,0),Is.EqualTo(UniqueDrawStatus.Locked));
            Assert.That(s.Snapshot.Appearance.UniqueReceipts,Is.Empty);Assert.That(s.Coins,Is.EqualTo(307));
        }
        [Test]
        public void OldOwnershipCarriesEligibilityWithoutChargingOldDrawsAtNewPrices()
        {
            var store=new Store();var s=Ready(store);s.CollectAppearance(Id(),"Ten");s.CollectAppearance(Id(),"Ten");var old=s.Snapshot.Appearance.DrawReceipts.Select(x=>x.Copy()).ToArray();
            var before=s.Coins;var q=s.DrawQuote(DrawPool.Skin);Assert.That(q.PaidCount,Is.Zero);Assert.That(q.Price,Is.EqualTo(200));
            while(q.Remaining.Length>0){Assert.That(q.Eligible.Length,Is.GreaterThan(0));Assert.That(s.DrawUnique(Id(),DrawPool.Skin,q.PaidCount,q.Price),Is.EqualTo(UniqueDrawStatus.Saved));q=s.DrawQuote(DrawPool.Skin);}
            Assert.That(s.Snapshot.Appearance.DrawReceipts.Select(x=>x.RequestId),Is.EqualTo(old.Select(x=>x.RequestId)));Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);
        }
        [Test]
        public void TamperedLedgerIsRejectedAndEveryPoolPriceCapsWithoutWrapping()
        {
            var store=new Store();var s=Ready(store);s.DrawUnique(Id(),DrawPool.Skin,0,200);var tampered=s.Snapshot;tampered.Appearance.UniqueReceipts[0].CoinCost++;Assert.Throws<ArgumentException>(()=>tampered.Validate());
            foreach(var p in new[]{DrawPool.Skin,DrawPool.Trail,DrawPool.Showcase}){Assert.That(UniqueDrawEngine.Price(p,int.MaxValue),Is.EqualTo(7500));Assert.That(UniqueDrawEngine.Price(p,100),Is.EqualTo(7500));}
        }
        [Test]
        public void ActiveLegacyAttemptKeepsItsRewardVersionAfterRestore()
        {
            using(var game=new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,LevelSolverTests.Ship("A",1,1,Tidebound.Ship.ShipDirection.Up)),2))
            {
                var checkpoint=game.Capture();Assert.That(checkpoint.EconomyVersion,Is.EqualTo(BattleCoinRules.CurrentVersion));checkpoint.EconomyVersion=BattleCoinRules.Version;
                using(var restored=SavedGameRuntime.Restore(checkpoint))
                {Assert.That(restored.Capture().EconomyVersion,Is.EqualTo(BattleCoinRules.Version));Assert.That(BattleCoinRules.FirstClear(2,restored.Capture().EconomyVersion),Is.EqualTo(120));}
            }
        }
        [Test]
        public void BothVictoryVersionsValidateAndOldWalletSurvivesUpgrade()
        {
            var old=CoinShopTests.Earned(10);old.Version=5;var store=new Store{Data=old};var s=new PlayerSaveService(store);Assert.That(s.IsAvailable,Is.True);Assert.That(s.Coins,Is.EqualTo(old.Coins));Assert.That(s.Snapshot.Version,Is.EqualTo(6));
            Assert.That(BattleCoinRules.FirstClear(10,BattleCoinRules.Version),Is.EqualTo(280));Assert.That(BattleCoinRules.FirstClear(10,BattleCoinRules.CurrentVersion),Is.EqualTo(100));
            var modern=CoinShopTests.Earned(10);foreach(var r in modern.Settlements){r.EconomyVersion=BattleCoinRules.CurrentVersion;r.FirstClearCoins=100;}modern.Coins=modern.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);Assert.DoesNotThrow(()=>modern.Validate());
        }
    }
}
