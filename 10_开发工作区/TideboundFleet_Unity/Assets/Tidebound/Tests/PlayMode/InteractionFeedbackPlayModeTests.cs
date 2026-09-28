using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Collection;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Tidebound.Tests
{
    public sealed class InteractionFeedbackPlayModeTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData data){if(Fail)throw new IOException();Data=data.Copy();}
        }
        private static PlayerSaveService Profile(Store store,int levels=10)
        {
            var data=new PlayerSaveData{CurrentLevel=levels+1,HighestClearedLevel=levels};
            data.Settlements=Enumerable.Range(1,levels).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="Review"+n,LevelNumber=n,Kind="Victory",Day="2026-09-28",EconomyVersion=BattleCoinRules.CurrentVersion,BattleCoins=n==1?7:80,FirstClearCoins=100}).ToArray();
            data.Coins=data.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);data.Validate();store.Save(data);return new PlayerSaveService(store);
        }
        private static HarborDrawPanel Draw(PlayerSaveService service)
        {
            var root=new GameObject("DrawTest",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var draw=HarborUI.Rect("Draw",root.transform).gameObject.AddComponent<HarborDrawPanel>();draw.Initialize(service,null,null);draw.Open(0);return draw;
        }
        [UnityTest] public IEnumerator SingleClickDrawsOnceAndFreeGiftDoesNotCharge()
        {
            var store=new Store();var service=Profile(store);var draw=Draw(service);
            try
            {
                yield return null;var balance=service.Coins;draw.BeginDraw();draw.BeginDraw();
                Assert.That(draw.Page,Is.EqualTo("result"));Assert.That(service.Coins,Is.EqualTo(balance));Assert.That(service.CanClaimFirstBlue,Is.False);
                foreach(var pool in new[]{DrawPool.Skin,DrawPool.Trail,DrawPool.Showcase})
                {
                    draw.SelectPool(pool);var price=service.DrawQuote(pool).Price;var before=service.Coins;
                    draw.GetComponentsInChildren<Button>().Single(x=>x.name=="DrawOnce").onClick.Invoke();
                    draw.BeginDraw();yield return null;
                    Assert.That(draw.Page,Is.EqualTo("result"));Assert.That(service.Coins,Is.EqualTo(before-price));
                    Assert.That(service.Snapshot.Appearance.UniqueReceipts.Count(r=>r.Pool==pool),Is.EqualTo(1));
                    Assert.That(draw.GetComponentsInChildren<Button>(true).Any(b=>b.name=="ConfirmDraw"||b.name=="CancelDraw"),Is.False);
                }
                Assert.That(new PlayerSaveService(store).Coins,Is.EqualTo(service.Coins));
                draw.SelectPool(DrawPool.Scene);balance=service.Coins;draw.BeginDraw();Assert.That(draw.Page,Is.EqualTo("main"));Assert.That(service.Coins,Is.EqualTo(balance));
            }
            finally{Object.Destroy(draw.transform.root.gameObject);}yield return null;
        }
        [UnityTest] public IEnumerator FailedSaveRetriesSameRequestWithoutDoubleDebit()
        {
            var store=new Store();var service=Profile(store);service.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue");var draw=Draw(service);
            try
            {
                yield return null;draw.SelectPool(DrawPool.Trail);var before=service.Coins;store.Fail=true;draw.BeginDraw();
                Assert.That(draw.Page,Is.EqualTo("main"));Assert.That(draw.LastResult,Is.EqualTo(UniqueDrawStatus.StorageUnavailable));Assert.That(service.Coins,Is.EqualTo(before));
                Assert.That(service.Snapshot.Appearance.UniqueReceipts,Is.Empty);
                store.Fail=false;draw.BeginDraw();draw.BeginDraw();yield return null;
                Assert.That(draw.Page,Is.EqualTo("result"));Assert.That(service.Coins,Is.EqualTo(before-300));Assert.That(service.Snapshot.Appearance.UniqueReceipts.Length,Is.EqualTo(1));
            }
            finally{Object.Destroy(draw.transform.root.gameObject);}yield return null;
        }
        [UnityTest] public IEnumerator InsufficientBalanceGoesStraightToRechargeWithoutReceipt()
        {
            var store=new Store();var service=Profile(store,2);service.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue");var draw=Draw(service);var opened=0;
            draw.SetRecharge(()=>opened++);
            try
            {
                yield return null;draw.SelectPool(DrawPool.Showcase);var before=service.Coins;draw.BeginDraw();
                Assert.That(opened,Is.EqualTo(1));Assert.That(draw.Page,Is.EqualTo("main"));Assert.That(service.Coins,Is.EqualTo(before));Assert.That(service.Snapshot.Appearance.UniqueReceipts,Is.Empty);
            }
            finally{Object.Destroy(draw.transform.root.gameObject);}yield return null;
        }
        private static PortraitPuzzleGraybox Game(float speed=20)
        {
            var game=new GameObject("InteractionFeedback").AddComponent<PortraitPuzzleGraybox>();
            var catalog=new PlayableLevelCatalog(new[]{"One","Two","Three"},_=>true,id=>new LevelData{SchemaVersion=2,LevelId=id,BossId="TF_KRAKEN_01",Width=16,Height=10,
                Ships=Enumerable.Range(0,14).Select(i=>new ShipPlacementData{Id="S"+i,TypeId="TF_BASE_SHIP",Position=new GridPosition(i,2),Direction=ShipDirection.Up,Length=2}).ToArray()});
            game.Initialize(catalog,new Tidebound.Unity.Ship.ShipMovementTiming(speed,.05f,speed<3?3:.1f,.1f,.2f),useGameplayArt:true);game.SelectLevel(2);return game;
        }
        [UnityTest] public IEnumerator ShuffleHighlightsOnlyTurnedShipsForThreeUnpausedSeconds()
        {
            var game=Game();
            try
            {
                yield return null;Assert.That(Resources.Load<Shader>("TideboundUI/ShipHighlight").isSupported,Is.True);var before=game.Session.Board.Ships.ToDictionary(s=>s.Id,s=>s.Direction);var stock=game.Tools.Remaining(ShipTool.Shuffle);
                game.SelectTool(ShipTool.Shuffle);Assert.That(game.Tools.Remaining(ShipTool.Shuffle),Is.EqualTo(stock-1));
                var changed=game.Session.Board.Ships.Where(s=>s.Direction!=before[s.Id]).Select(s=>s.Id).ToArray();Assert.That(changed,Is.Not.Empty);
                var all=game.GetComponentsInChildren<ShipFloatPresentation>();
                Assert.That(all.Count(s=>s.ShuffleHighlightRemaining>0),Is.EqualTo(changed.Length));
                foreach(var ship in all)Assert.That(ship.ShuffleHighlightRemaining>0,Is.EqualTo(changed.Contains(ship.transform.parent.name.Substring("Ship_".Length))));
                game.TogglePause();var ages=all.Select(s=>s.ShuffleHighlightRemaining).ToArray();yield return new WaitForSecondsRealtime(.2f);
                Assert.That(all.Select(s=>s.ShuffleHighlightRemaining),Is.EqualTo(ages));game.TogglePause();
                yield return new WaitForSecondsRealtime(2.8f);Assert.That(all.Count(s=>s.ShuffleHighlightRemaining>0),Is.EqualTo(changed.Length));
                yield return new WaitForSecondsRealtime(.3f);Assert.That(all.All(s=>s.ShuffleHighlightRemaining==0),Is.True);
                Assert.That(game.GetComponentsInChildren<RawImage>().Any(x=>x.name=="ShuffleRedOutline"),Is.False);
            }
            finally{Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest] public IEnumerator SailingDoesNotGrayToolsAndBusyClicksCannotSpendOrMutateBoard()
        {
            var game=Game(2);
            try
            {
                yield return null;game.ClickShip("S0");yield return null;Assert.That(game.IsBusy,Is.True);
                var before=game.Session.Board.Ships.Select(s=>s.Id+":"+s.Direction+":"+s.Position).ToArray();var uses=game.Tools.UsesLeft;
                foreach(var name in new[]{"Rescue","Shuffle","Reverse"})
                {
                    var button=game.GetComponentsInChildren<Button>().Single(b=>b.name==name);Assert.That(button.interactable,Is.True);
                    Assert.That(button.transform.Find("ApprovedGoldTool").GetComponent<Graphic>().color,Is.EqualTo(Color.white));
                    Assert.That(button.transform.Find("ToolIcon").GetComponent<Graphic>().color,Is.EqualTo(Color.white));button.onClick.Invoke();
                }
                Assert.That(game.Tools.UsesLeft,Is.EqualTo(uses));Assert.That(game.Tools.Selection,Is.EqualTo(ShipTool.None));Assert.That(game.Session.Board.Ships.Select(s=>s.Id+":"+s.Direction+":"+s.Position),Is.EqualTo(before));
                game.TogglePause();yield return null;Assert.That(game.GetComponentsInChildren<Button>().Single(b=>b.name=="Shuffle").interactable,Is.False);
            }
            finally{Object.Destroy(game.gameObject);}yield return null;
        }
    }
}
