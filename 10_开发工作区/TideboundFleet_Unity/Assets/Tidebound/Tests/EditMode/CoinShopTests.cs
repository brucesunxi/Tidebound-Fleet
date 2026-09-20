using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class CoinShopTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData value){if(Fail)throw new IOException();Data=value.Copy();}
        }
        private static string Id()=>Guid.NewGuid().ToString("N");
        private static CoinShopCatalog Catalog()=>CoinShopCatalogReader.LoadDefault();
        // Valid historical income ledger; no direct wallet mutation bypasses profile validation.
        internal static PlayerSaveData Earned(int levels=2)
        {
            var data=new PlayerSaveData{CurrentLevel=levels+1,HighestClearedLevel=levels,
                Settlements=Enumerable.Range(1,levels).Select(n=>new SettlementRecord{AttemptId=Id(),LevelId="L"+n,LevelNumber=n,Kind="Victory",
                    Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
            data.Coins=data.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);data.Validate();return data;
        }
        [TestCase("rescue_1",250,1,0,0)][TestCase("shuffle_1",300,0,1,0)][TestCase("reverse_1",200,0,0,1)][TestCase("tools_bundle_1",650,1,1,1)]
        public void TrustedProductsDebitExactlyOnceAndReloadWithMatchingStock(string id,int price,int rescue,int shuffle,int reverse)
        {
            var store=new Store{Data=Earned(5)};var service=new PlayerSaveService(store);var request=Id();var coins=service.Coins;
            Assert.That(service.BuyWithCoins(request,id,Catalog()),Is.EqualTo(CoinPurchaseStatus.Purchased));
            Assert.That(service.Coins,Is.EqualTo(coins-price));Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.EqualTo(rescue));
            Assert.That(service.Inventory.Count(ShipTool.Shuffle),Is.EqualTo(shuffle));Assert.That(service.Inventory.Count(ShipTool.Reverse),Is.EqualTo(reverse));
            for(var i=0;i<3;i++)Assert.That(service.BuyWithCoins(request,id,Catalog()),Is.EqualTo(CoinPurchaseStatus.AlreadyPurchased));
            service=new PlayerSaveService(store);Assert.That(service.IsAvailable,Is.True);
            Assert.That(service.BuyWithCoins(request,id,Catalog()),Is.EqualTo(CoinPurchaseStatus.AlreadyPurchased));Assert.That(service.Coins,Is.EqualTo(coins-price));
            Assert.That(service.Snapshot.Purchases.Length,Is.EqualTo(1));Assert.That(service.Snapshot.Tools.Receipts,Does.Contain("coin:"+request));
        }
        [Test]
        public void ConfigHasAgreedTestPricesAndRejectsMalformedProducts()
        {
            var catalog=Catalog();Assert.That(catalog.UnlockLevel,Is.EqualTo(3));Assert.That(catalog.Products.Count,Is.EqualTo(4));
            Assert.That(catalog.Find("rescue_1").Price,Is.EqualTo(250));Assert.That(catalog.Find("shuffle_1").Price,Is.EqualTo(300));
            Assert.That(catalog.Find("reverse_1").Price,Is.EqualTo(200));Assert.That(catalog.Find("tools_bundle_1").Price,Is.EqualTo(650));
            Assert.Throws<ArgumentException>(()=>new CoinShopProduct("x","x",0,1,0,0));
            Assert.Throws<ArgumentException>(()=>new CoinShopProduct("x","x",10,0,0,0));
            Assert.Throws<ArgumentException>(()=>new CoinShopProduct("x","x",10,1000,0,0));
            Assert.Throws<ArgumentException>(()=>new CoinShopCatalog("x",3,new[]{catalog.Products[0],catalog.Products[0]}));
            Assert.Throws<ArgumentException>(()=>CoinShopCatalogReader.Read("{}"));
        }
        [Test]
        public void LockedInsufficientInvalidAndConflictingRequestsDoNotMutateAnything()
        {
            var store=new Store{Data=Earned(1)};var service=new PlayerSaveService(store);
            Assert.That(service.BuyWithCoins(Id(),"reverse_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.Locked));
            Assert.That(service.Coins,Is.EqualTo(107));Assert.That(service.Snapshot.Purchases,Is.Empty);
            store=new Store{Data=Earned()};service=new PlayerSaveService(store);var request=Id();
            Assert.That(service.BuyWithCoins(request,"tools_bundle_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.InsufficientCoins));
            Assert.That(service.BuyWithCoins("bad","rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.InvalidRequest));
            Assert.That(service.BuyWithCoins(Id(),"fake",Catalog()),Is.EqualTo(CoinPurchaseStatus.InvalidProduct));
            Assert.That(service.Coins,Is.EqualTo(307));Assert.That(service.Snapshot.Tools.Receipts,Is.Empty);
            Assert.That(service.BuyWithCoins(request,"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.Purchased));
            Assert.That(service.BuyWithCoins(request,"reverse_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.RequestConflict));
            Assert.That(service.Coins,Is.EqualTo(57));Assert.That(service.Inventory.Count(ShipTool.Reverse),Is.Zero);
        }
        [Test]
        public void FailedWriteRetriesTheSameRequestAndNeverLosesCoinsOrGrantsStockEarly()
        {
            var store=new Store{Data=Earned()};var service=new PlayerSaveService(store);var request=Id();var revision=store.Data.Revision;
            store.Fail=true;Assert.That(service.BuyWithCoins(request,"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.StorageUnavailable));
            Assert.That(service.Coins,Is.EqualTo(307));Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.Zero);Assert.That(store.Data.Revision,Is.EqualTo(revision));
            store.Fail=false;Assert.That(service.BuyWithCoins(request,"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.Purchased));
            Assert.That(service.Coins,Is.EqualTo(57));Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.EqualTo(1));
        }
        [Test]
        public void PurchasedInventoryIsNotOverwrittenByLaterGiftSpendOrCheckpoint()
        {
            var store=new Store{Data=Earned()};var service=new PlayerSaveService(store);
            using(var game=new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,LevelSolverTests.Ship("A",1,1,ShipDirection.Up)),3))
            {
                Assert.That(service.Start(game),Is.True);var board=game.Session.Board;
                Assert.That(service.BuyWithCoins(Id(),"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.Purchased));
                Assert.That(game.Session.Board,Is.SameAs(board));Assert.That(game.Combat.HitCount,Is.Zero);
                Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.EqualTo(2));
                service.Inventory.Grant("extra",0,1,0);service.Checkpoint(true);
                using(var tools=new ShipToolSystem(game.Session,game.Movement,service.Inventory))Assert.That(tools.Rescue(),Is.EqualTo(ToolUseStatus.Applied));
                service.Checkpoint();var loaded=new PlayerSaveService(store);
                Assert.That(loaded.Inventory.Count(ShipTool.Rescue),Is.EqualTo(1));Assert.That(loaded.Inventory.Count(ShipTool.Shuffle),Is.EqualTo(2));Assert.That(loaded.Coins,Is.EqualTo(57));
                game.Transit.Advance(10);game.Combat.Advance();service.Checkpoint();Assert.That(service.Coins,Is.EqualTo(198));
                Assert.That(service.Snapshot.Purchases.Length,Is.EqualTo(1));Assert.That(new PlayerSaveService(store).Coins,Is.EqualTo(198));
            }
        }
        [Test]
        public void PendingCombatCoinsCannotFundPurchaseAndMovingShipBlocksNewPurchase()
        {
            var store=new Store{Data=Earned()};var service=new PlayerSaveService(store);
            using(var game=new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,LevelSolverTests.Ship("A",1,1,ShipDirection.Up),LevelSolverTests.Ship("B",3,1,ShipDirection.Up)),3))
            {
                service.Start(game);service.BuyWithCoins(Id(),"rescue_1",Catalog());
                var op=game.Movement.TryBeginMove("A");Assert.That(service.BuyWithCoins(Id(),"reverse_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.Busy));
                game.Movement.CompleteTravel(op.Operation.OperationId);game.Transit.Advance(10);game.Combat.Advance();service.Checkpoint();
                var cost8=new CoinShopCatalog("Test58",3,new[]{new CoinShopProduct("eight","eight",58,1,0,0)});
                Assert.That(game.PendingCoins,Is.EqualTo(1));Assert.That(service.Coins,Is.EqualTo(57));
                Assert.That(service.BuyWithCoins(Id(),"eight",cost8),Is.EqualTo(CoinPurchaseStatus.InsufficientCoins));
            }
        }
        [Test]
        public void HistoricalReceiptSurvivesPriceChangeAndNewRequestUsesNewConfiguration()
        {
            var store=new Store{Data=Earned(5)};var service=new PlayerSaveService(store);var request=Id();
            service.BuyWithCoins(request,"rescue_1",Catalog());var coins=service.Coins;
            var changed=new CoinShopCatalog("Repriced",3,new[]{new CoinShopProduct("rescue_1","Rescue",350,2,0,0)});
            Assert.That(service.BuyWithCoins(request,"rescue_1",changed),Is.EqualTo(CoinPurchaseStatus.AlreadyPurchased));Assert.That(service.Coins,Is.EqualTo(coins));
            Assert.That(service.BuyWithCoins(Id(),"rescue_1",changed),Is.EqualTo(CoinPurchaseStatus.Purchased));
            Assert.That(service.Coins,Is.EqualTo(coins-350));Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.EqualTo(3));
            Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);
        }
        [Test]
        public void InventoryOverflowAndMismatchedLedgerFailClosed()
        {
            var data=Earned();data.Tools.Rescue=int.MaxValue;var store=new Store{Data=data};var service=new PlayerSaveService(store);
            Assert.That(service.BuyWithCoins(Id(),"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.InventoryLimit));Assert.That(service.Coins,Is.EqualTo(307));
            store=new Store{Data=Earned()};service=new PlayerSaveService(store);service.BuyWithCoins(Id(),"rescue_1",Catalog());
            data=store.Load();data.Tools.Receipts=Array.Empty<string>();Assert.Throws<ArgumentException>(()=>data.Validate());
            data=store.Load();data.Coins++;Assert.Throws<ArgumentException>(()=>data.Validate());
            data=store.Load();data.Purchases=data.Purchases.Concat(data.Purchases).ToArray();Assert.Throws<ArgumentException>(()=>data.Validate());
            Assert.Throws<ArgumentException>(()=>service.Inventory.Grant("coin:spoof",1,0,0));
        }
        [Test]
        public void VersionTwoUpgradePreservesEverythingAndFailedMigrationNeverOverwrites()
        {
            var data=Earned();data.Version=2;data.Tools.Rescue=2;data.Tools.Receipts=new[]{"legacy"};var store=new Store{Data=data,Fail=true};
            var blocked=new PlayerSaveService(store);Assert.That(blocked.IsAvailable,Is.False);Assert.That(store.Data.Version,Is.EqualTo(2));
            store.Fail=false;var service=new PlayerSaveService(store);Assert.That(service.IsAvailable,Is.True);Assert.That(store.Data.Version,Is.EqualTo(3));
            Assert.That(service.Coins,Is.EqualTo(307));Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.EqualTo(2));Assert.That(service.Snapshot.Purchases,Is.Empty);
            var revision=store.Data.Revision;service=new PlayerSaveService(store);Assert.That(store.Data.Revision,Is.EqualTo(revision));
        }
        [Test]
        public void RealVersionTwoJsonWithoutPurchasesMigratesAndPurchaseSurvivesDiskReload()
        {
            var folder=Path.Combine(Path.GetTempPath(),"TideboundShopTest_"+Id());var path=Path.Combine(folder,"player-save-v2.json");
            try
            {
                Directory.CreateDirectory(folder);var data=Earned();data.Version=2;
                var obj=Newtonsoft.Json.Linq.JObject.FromObject(data);obj.Remove("Purchases");var payload=obj.ToString(Formatting.None);string digest;
                using(var sha=SHA256.Create())digest=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
                var original=JsonConvert.SerializeObject(new{EnvelopeVersion=1,Payload=payload,Digest=digest});File.WriteAllText(path,original);
                var store=new PlayerSaveFileStore(path);var service=new PlayerSaveService(store);Assert.That(service.IsAvailable,Is.True);
                Assert.That(File.ReadAllText(path+".bak"),Is.EqualTo(original));Assert.That(service.Snapshot.Version,Is.EqualTo(3));
                var request=Id();Assert.That(service.BuyWithCoins(request,"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.Purchased));
                service=new PlayerSaveService(store);Assert.That(service.BuyWithCoins(request,"rescue_1",Catalog()),Is.EqualTo(CoinPurchaseStatus.AlreadyPurchased));
                Assert.That(service.Coins,Is.EqualTo(57));Assert.That(service.Inventory.Count(ShipTool.Rescue),Is.EqualTo(1));
                Assert.That(File.Exists(path+".tmp"),Is.False);
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
    }
}
