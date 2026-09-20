using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class CoinShopPlayModeTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();public void Save(PlayerSaveData value){if(Fail)throw new IOException();Data=value.Copy();}
        }
        private static PlayerSaveData Earned(int levels=2)
        {
            var d=new PlayerSaveData{CurrentLevel=levels+1,HighestClearedLevel=levels,Settlements=Enumerable.Range(1,levels).Select(n=>new SettlementRecord
            {AttemptId=Guid.NewGuid().ToString("N"),LevelId="L"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,
                BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};d.Coins=d.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);return d;
        }
        private static PortraitPuzzleGraybox Create(Store store)
        {
            var path=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            var catalog=new CandidateLevelCatalog(File.ReadAllText(Path.Combine(path,"manifest.json")),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".solution.json"))));
            var game=new GameObject("CoinShop_Test").AddComponent<PortraitPuzzleGraybox>();game.Initialize(catalog,saveService:new PlayerSaveService(store));return game;
        }
        [UnityTest]
        public IEnumerator ConfirmCancelDoubleClickAndRestartPreservePurchasedStockAndPauseOwnership()
        {
            var store=new Store{Data=Earned()};var game=Create(store);
            try
            {
                yield return null;game.ToggleMenu();game.OpenShop();Assert.That(game.IsMenuOpen,Is.False);Assert.That(game.IsPaused,Is.True);
                game.ShopPanel.SelectProduct("rescue_1");game.ShopPanel.ShowProducts();Assert.That(game.SaveService.Coins,Is.EqualTo(307));
                game.ShopPanel.SelectProduct("rescue_1");game.ShopPanel.ConfirmPurchase();game.ShopPanel.ConfirmPurchase();
                Assert.That(game.SaveService.Coins,Is.EqualTo(57));Assert.That(game.ToolInventory.Count(ShipTool.Rescue),Is.EqualTo(2));
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(80));Assert.That(game.Session.ToolUses,Is.Zero);Assert.That(game.SaveService.Snapshot.Purchases.Length,Is.EqualTo(1));
                game.CloseAcquisition();Assert.That(game.IsPaused,Is.False);
                game.SelectTool(ShipTool.Rescue);Assert.That(game.Session.Board.ShipCount,Is.EqualTo(78));Assert.That(game.Tools.UsesLeft,Is.EqualTo(4));
                game.TogglePause();game.OpenShop();game.CloseAcquisition();Assert.That(game.IsPaused,Is.True);
                UnityEngine.Object.Destroy(game.gameObject);yield return null;game=Create(store);yield return null;
                Assert.That(game.SaveService.Coins,Is.EqualTo(57));Assert.That(game.ToolInventory.Count(ShipTool.Rescue),Is.EqualTo(1));Assert.That(game.Tools.UsesLeft,Is.EqualTo(4));
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(78));Assert.That(game.IsPaused,Is.True);
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator InsufficientAndFailedSaveShowNoChargeThenRetrySamePurchase()
        {
            var store=new Store{Data=Earned()};var game=Create(store);
            try
            {
                yield return null;game.OpenShop();game.ShopPanel.SelectProduct("tools_bundle_1");game.ShopPanel.ConfirmPurchase();
                Assert.That(game.ShopPanel.LastResult,Is.EqualTo(CoinPurchaseStatus.InsufficientCoins));Assert.That(game.SaveService.Coins,Is.EqualTo(307));
                game.ShopPanel.SelectProduct("shuffle_1");store.Fail=true;game.ShopPanel.ConfirmPurchase();
                Assert.That(game.ShopPanel.LastResult,Is.EqualTo(CoinPurchaseStatus.StorageUnavailable));Assert.That(game.ToolInventory.Count(ShipTool.Shuffle),Is.EqualTo(1));
                store.Fail=false;game.ShopPanel.ConfirmPurchase();Assert.That(game.ShopPanel.LastResult,Is.EqualTo(CoinPurchaseStatus.Purchased));
                Assert.That(game.SaveService.Coins,Is.EqualTo(7));Assert.That(game.ToolInventory.Count(ShipTool.Shuffle),Is.EqualTo(2));
            }
            finally{store.Fail=false;UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator ShopFitsThreePortraitSafeAreasAndDoesNotLetPurchaseBypassToolLimit()
        {
            var store=new Store{Data=Earned(5)};var game=Create(store);
            try
            {
                yield return null;game.SaveService.Inventory.Grant("test",0,0,4);
                var id=game.Session.Board.Ships.First().Id;
                for(var i=0;i<5;i++){game.SelectTool(ShipTool.Reverse);game.ClickShip(id);}
                Assert.That(game.Tools.UsesLeft,Is.Zero);game.OpenShop();
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                {
                    game.ApplyViewport(new Rect(5,11,size.x,size.y),1);Canvas.ForceUpdateCanvases();
                    foreach(var b in game.ShopPanel.GetComponentsInChildren<Button>())
                    {var rt=(RectTransform)b.transform;Assert.That(rt.anchoredPosition.x,Is.GreaterThanOrEqualTo(0));Assert.That(rt.anchoredPosition.y,Is.GreaterThanOrEqualTo(0));
                     Assert.That(rt.anchoredPosition.x+rt.rect.width,Is.LessThanOrEqualTo(size.x));Assert.That(rt.anchoredPosition.y+rt.rect.height,Is.LessThanOrEqualTo(size.y));}
                }
                game.ShopPanel.SelectProduct("tools_bundle_1");game.ShopPanel.ConfirmPurchase();Assert.That(game.ShopPanel.LastResult,Is.EqualTo(CoinPurchaseStatus.Purchased));
                game.CloseAcquisition();var stock=game.Tools.Remaining(ShipTool.Rescue);game.SelectTool(ShipTool.Rescue);
                Assert.That(game.Tools.Remaining(ShipTool.Rescue),Is.EqualTo(stock));Assert.That(game.Session.Board.ShipCount,Is.EqualTo(80));
                game.Restart();Assert.That(game.Tools.UsesLeft,Is.EqualTo(5));Assert.That(game.Tools.Remaining(ShipTool.Rescue),Is.EqualTo(stock));
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
    }
}
