using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Board;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class BoardAssistancePlayModeTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData d){if(Fail)throw new IOException();Data=d.Copy();}
        }
        private static void Call(PortraitPuzzleGraybox g,string name,params object[] args)=>typeof(PortraitPuzzleGraybox).GetMethod(name,Private).Invoke(g,args);
        private static ShipPlacementData Ship(string id,int x,int y,ShipDirection direction)=>new ShipPlacementData
            {Id=id,TypeId="TF_BASE_SHIP",Position=new GridPosition(x,y),Direction=direction,Length=2};
        private static PortraitPuzzleGraybox Create(bool deadlocked=false,bool partial=false,bool outgoing=false,Store store=null,bool releaseA=false)
        {
            var p=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            var catalog=new CandidateLevelCatalog(File.ReadAllText(Path.Combine(p,"manifest.json")),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".solution.json"))));
            var g=new GameObject("Assistance_Test").AddComponent<PortraitPuzzleGraybox>();
            g.Initialize(catalog,saveService:store==null || deadlocked || partial || outgoing ? null : new PlayerSaveService(store));
            if(deadlocked || partial || outgoing)
            {
                // Synthetic board is isolated from shipped assets. Reuse the normal binding, input and presentation.
                Call(g,"ClearSession");
                var ships=partial?new[]{Ship("A",0,0,ShipDirection.Right),Ship("B",5,0,ShipDirection.Left)}:
                    new[]{Ship("A",0,0,ShipDirection.Right),Ship("B",3,0,ShipDirection.Left)};
                if(releaseA)ships[0]=Ship("A",1,0,ShipDirection.Left);
                if(outgoing)ships=ships.Concat(new[]{Ship("C",4,3,ShipDirection.Up)}).ToArray();
                var runtime=SavedGameRuntime.Create(new LevelData{SchemaVersion=2,LevelId=catalog.Load(2).LevelId,BossId="TF_KRAKEN_01",Width=6,Height=5,Ships=ships},3);
                if(store!=null)
                {
                    var service=new PlayerSaveService(store);Assert.That(service.Start(runtime),Is.True);
                    typeof(PortraitPuzzleGraybox).GetField("saveService",Private).SetValue(g,service);
                    typeof(PortraitPuzzleGraybox).GetField("toolInventory",Private).SetValue(g,service.Inventory);
                }
                Call(g,"BindWorld",runtime,2);
            }
            Call(g,"OnApplicationFocus",true);Call(g,"TickAssistance",0f);return g;
        }
        private static void Tick(PortraitPuzzleGraybox g,float seconds)=>Call(g,"TickAssistance",seconds);
        private static IEnumerator Until(Func<bool> condition,float seconds=5)
        {var end=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True);}
        private static PlayerSaveData Earned()=>new PlayerSaveData{CurrentLevel=3,HighestClearedLevel=2,Coins=307,Settlements=Enumerable.Range(1,2).Select(n=>new SettlementRecord
        {AttemptId=Guid.NewGuid().ToString("N"),LevelId="L"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
        [UnityTest]
        public IEnumerator IdleHighlightClearsOnEmptyPointerHoldPauseBackgroundAndPreference()
        {
            var g=Create();
            try
            {
                Tick(g,4.9f);Assert.That(g.AutoHintShipId,Is.Null);Tick(g,.11f);Assert.That(g.AutoHintShipId,Is.Not.Null);
                Assert.That(g.Session.Board.QueryForwardPath(g.AutoHintShipId).CanExit,Is.True);
                var e=new PointerEventData(EventSystem.current){pointerId=7,button=PointerEventData.InputButton.Left,position=new Vector2(-100,-100)};
                g.InputSurface.OnPointerDown(e);Assert.That(g.AutoHintShipId,Is.Null);Tick(g,8);Assert.That(g.AutoHintShipId,Is.Null);
                g.InputSurface.OnPointerUp(e);Tick(g,0);Tick(g,5);Assert.That(g.AutoHintShipId,Is.Not.Null);
                g.TogglePause();Tick(g,8);Assert.That(g.AutoHintShipId,Is.Null);g.TogglePause();Tick(g,0);Tick(g,5);Assert.That(g.AutoHintShipId,Is.Not.Null);
                Call(g,"OnApplicationFocus",false);Tick(g,20);Assert.That(g.AutoHintShipId,Is.Null);Call(g,"OnApplicationFocus",true);Tick(g,0);Tick(g,4.9f);Assert.That(g.AutoHintShipId,Is.Null);
                g.SetAssistancePreferences(false,true);Tick(g,10);Assert.That(g.AutoHintShipId,Is.Null);
                g.SetAssistancePreferences(true,true);Tick(g,0);Tick(g,5);Assert.That(g.AutoHintShipId,Is.Not.Null);
                var id=g.AutoHintShipId;g.ClickShip(id);Assert.That(g.AutoHintShipId,Is.Null);yield return Until(()=>!g.IsBusy);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator PartialOnlyDoesNotPromptAndDeadlockDismissesOnceThenReverseReleasesBoard()
        {
            var g=Create(partial:true);
            try
            {
                Tick(g,10);Assert.That(g.AutoHintShipId,Is.Null);Assert.That(g.IsDeadlockOpen,Is.False);
                g.ClickShip("A");yield return Until(()=>g.IsDeadlockOpen);
                Assert.That(g.IsPaused,Is.False);Assert.That(g.Session.Board.ShipCount,Is.EqualTo(2));
                g.TogglePause();Assert.That(g.IsPaused,Is.True);Tick(g,10);Assert.That(g.IsDeadlockOpen,Is.True);
                g.TogglePause();Tick(g,4.1f);Assert.That(g.IsDeadlockOpen,Is.False);Assert.That(g.IsPaused,Is.False);
                Tick(g,10);Assert.That(g.IsDeadlockOpen,Is.False);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
            g=Create(deadlocked:true);
            try
            {
                Tick(g,0);Assert.That(g.IsDeadlockOpen,Is.True);var stock=g.Tools.Remaining(ShipTool.Reverse);
                g.UseDeadlockTool(ShipTool.Reverse);Assert.That(g.Tools.Selection,Is.EqualTo(ShipTool.Reverse));Assert.That(g.IsPaused,Is.False);
                Tick(g,10);Assert.That(g.AutoHintShipId,Is.Null);g.ClickShip("A");Tick(g,0);
                Assert.That(g.Tools.Remaining(ShipTool.Reverse),Is.EqualTo(stock-1));Assert.That(g.Tools.UsesLeft,Is.EqualTo(4));
                Tick(g,5);Assert.That(g.AutoHintShipId,Is.EqualTo("A"));Assert.That(g.IsDeadlockOpen,Is.False);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator DeadlockWaitsForLastLaneAndHitAndFitsPortraitSafeAreas()
        {
            var g=Create(outgoing:true);
            try
            {
                g.ClickShip("C");yield return Until(()=>!g.IsBusy);
                Assert.That(g.IsDeadlockOpen,Is.False);Assert.That(g.Combat.HitCount,Is.Zero);
                yield return Until(()=>g.IsDeadlockOpen);Assert.That(g.Combat.HitCount,Is.EqualTo(1));Assert.That(g.Combat.PendingCount,Is.Zero);Assert.That(g.IsPaused,Is.False);
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                {
                    g.ApplyViewport(new Rect(5,11,size.x,size.y),1);Canvas.ForceUpdateCanvases();
                    Assert.That(g.DeadlockView.GetComponentsInChildren<Button>(),Is.Empty);
                    Assert.That(g.DeadlockView.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
                    Assert.That(g.DeadlockView.GetComponentsInChildren<Graphic>().All(x=>!x.raycastTarget),Is.True);
                    var rect=(RectTransform)g.DeadlockView.transform;
                    Assert.That(rect.rect.height,Is.LessThan(80));
                    Assert.That(rect.anchoredPosition.y,Is.GreaterThan(11));
                    Assert.That(rect.anchoredPosition.y+rect.rect.height,Is.LessThan(11+size.y));
                }
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator EmptyInventoryOpensRealShopWithoutUseAndTemporaryPauseDoesNotPersist()
        {
            var d=Earned();d.Tools.Receipts=new[]{"milestone:tools:unlock"};var store=new Store{Data=d};var g=Create(deadlocked:true,store:store);
            try
            {
                Tick(g,0);Assert.That(g.IsDeadlockOpen,Is.True);Assert.That(store.Data.Attempt.Paused,Is.False);
                g.UseDeadlockTool(ShipTool.Rescue);Assert.That(g.IsAcquisitionOpen,Is.True);Assert.That(g.IsDeadlockOpen,Is.False);Assert.That(g.IsPaused,Is.True);
                g.ShopPanel.SelectProduct("rescue_1");g.ShopPanel.ConfirmPurchase();g.ShopPanel.ConfirmPurchase();
                Assert.That(g.SaveService.Coins,Is.EqualTo(57));Assert.That(g.Session.ToolUses,Is.Zero);Assert.That(store.Data.Attempt.Paused,Is.False);
                g.CloseAcquisition();Assert.That(g.IsPaused,Is.False);Tick(g,10);Assert.That(g.IsDeadlockOpen,Is.False);
                g.SelectTool(ShipTool.Rescue);Assert.That(g.Session.Board.ShipCount,Is.Zero);Assert.That(g.Tools.UsesLeft,Is.EqualTo(4));
                yield return Until(()=>g.IsCleared);Assert.That(g.IsDeadlockOpen,Is.False);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator DeadlockRestartSettlesOnlyCompletedHitsOnceAndPreservesSpentStock()
        {
            var store=new Store{Data=Earned()};var g=Create(outgoing:true,store:store);
            try
            {
                g.ClickShip("C");yield return Until(()=>g.IsDeadlockOpen);
                Assert.That(g.SaveService.RestartReward,Is.EqualTo(1));Assert.That(store.Data.Attempt.Paused,Is.False);
                var oldId=g.Session.SessionId;store.Fail=true;g.RestartFromDeadlock();
                Assert.That(g.IsDeadlockOpen,Is.True);Assert.That(g.Session.SessionId,Is.EqualTo(oldId));Assert.That(g.SaveService.Coins,Is.EqualTo(307));
                store.Fail=false;g.RestartFromDeadlock();g.RestartFromDeadlock();
                Assert.That(g.IsDeadlockOpen,Is.False);Assert.That(g.SaveService.Coins,Is.EqualTo(308));Assert.That(g.SaveService.DailyRestartCount,Is.EqualTo(1));
                Assert.That(g.Tools.UsesLeft,Is.EqualTo(5));Assert.That(g.SaveService.Snapshot.Settlements.Count(x=>x.AttemptId==oldId),Is.EqualTo(1));
            }
            finally{store.Fail=false;UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FifthUseCanCreateDeadlockButBuyingCannotGrantSixthUseAndRestartFailureIsSafe()
        {
            var store=new Store{Data=Earned()};var g=Create(deadlocked:true,store:store,releaseA:true);
            try
            {
                g.SaveService.Inventory.Grant("test:reverse",0,0,5);
                for(var i=0;i<5;i++){g.SelectTool(ShipTool.Reverse);g.ClickShip("A");}
                Assert.That(g.Tools.UsesLeft,Is.Zero);Tick(g,0);Assert.That(g.IsDeadlockOpen,Is.True);
                g.UseDeadlockTool(ShipTool.Rescue);Assert.That(g.IsDeadlockOpen,Is.True);Assert.That(g.Session.ToolUses,Is.EqualTo(5));
                g.OpenShopFromDeadlock();g.ShopPanel.SelectProduct("rescue_1");g.ShopPanel.ConfirmPurchase();g.CloseAcquisition();
                g.SelectTool(ShipTool.Rescue);Assert.That(g.Session.Board.ShipCount,Is.EqualTo(2));Assert.That(g.Tools.UsesLeft,Is.Zero);
                store.Fail=true;var id=g.Session.SessionId;var coins=g.SaveService.Coins;g.Restart();
                Assert.That(g.Session.SessionId,Is.EqualTo(id));Assert.That(g.SaveService.Coins,Is.EqualTo(coins));
                store.Fail=false;g.Restart();Assert.That(g.Session.SessionId,Is.Not.EqualTo(id));Assert.That(g.Tools.UsesLeft,Is.EqualTo(5));
            }
            finally{store.Fail=false;UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
    }
}
