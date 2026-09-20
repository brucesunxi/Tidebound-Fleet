using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class LevelEntryPlayModeTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail, FailVictory;public int Writes;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData d){if(Fail || (FailVictory && d.Attempt?.Victory==true))throw new IOException();Data=d.Copy();Writes++;}
        }
        private static CandidateLevelCatalog Catalog()
        {
            var p=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(p,"manifest.json")),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".solution.json"))));
        }
        private static void Call(PortraitPuzzleGraybox g,string method,params object[] args)=>typeof(PortraitPuzzleGraybox).GetMethod(method,Private).Invoke(g,args);
        private static PortraitPuzzleGraybox Create(Store store,bool campaign=false)
        {
            var g=new GameObject("Entry_Test").AddComponent<PortraitPuzzleGraybox>();
            g.Initialize(Catalog(),saveService:new PlayerSaveService(store),campaign:campaign,animateEntry:true);
            Call(g,"OnApplicationFocus",true);return g;
        }
        private static IEnumerator Until(Func<bool> condition,float seconds=10)
        {var end=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True);}
        private static PlayerSaveData Earned()=>new PlayerSaveData{CurrentLevel=3,HighestClearedLevel=2,Coins=307,Settlements=Enumerable.Range(1,2).Select(n=>new SettlementRecord
        {AttemptId=Guid.NewGuid().ToString("N"),LevelId="L"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
        private static CanvasGroup[] Bodies(PortraitPuzzleGraybox g)=>g.GetComponentsInChildren<CanvasGroup>(true).Where(c=>c.name=="Body").ToArray();
        [UnityTest]
        public IEnumerator FreshEntrySavesOnceBlocksEarlyInputAndHasNoQueuedPointerRelease()
        {
            var store=new Store{Data=Earned()};var g=Create(store);
            try
            {
                var attempt=g.Session.SessionId;var board=g.Session.Board;var balance=g.SaveService.Coins;var writes=store.Writes;
                Assert.That(store.Data.Attempt.AttemptId,Is.EqualTo(attempt));Assert.That(g.EntryPhase,Is.EqualTo(LevelEntryPhase.Field));
                Assert.That(Bodies(g).Length,Is.EqualTo(80));Assert.That(Bodies(g).All(b=>b.alpha==0),Is.True);
                var ship=board.Ships.First(s=>board.QueryForwardPath(s.Id).CanExit);
                var pointer=new PointerEventData(EventSystem.current){pointerId=22,position=g.BoardCamera.WorldToScreenPoint(g.ViewPosition(ship.Id)),button=PointerEventData.InputButton.Left};
                g.InputSurface.OnPointerDown(pointer);g.ClickShip(ship.Id);g.SelectTool(ShipTool.Rescue);g.OpenShop();g.ToggleAuto();g.SelectLevel(2);g.Restart();
                Assert.That(g.Session.Board,Is.SameAs(board));Assert.That(g.Session.SessionId,Is.EqualTo(attempt));Assert.That(g.Tools.UsesLeft,Is.EqualTo(5));
                Assert.That(g.IsAcquisitionOpen || g.IsAutoPlaying || g.IsBusy,Is.False);Assert.That(store.Writes,Is.EqualTo(writes));
                Call(g,"TickAssistance",10f);Assert.That(g.AutoHintShipId,Is.Null);
                yield return Until(()=>g.IsEntryReady);g.InputSurface.OnPointerUp(pointer);yield return null;
                Assert.That(g.Session.Board,Is.SameAs(board));Assert.That(g.SaveService.Coins,Is.EqualTo(balance));
                Assert.That(Bodies(g).All(b=>b.alpha==1 && b.transform.localScale==Vector3.one),Is.True);
                Call(g,"TickAssistance",0f);Call(g,"TickAssistance",4.9f);Assert.That(g.AutoHintShipId,Is.Null);
                Call(g,"TickAssistance",.11f);Assert.That(g.AutoHintShipId,Is.Not.Null);
                g.ClickShip(ship.Id);Assert.That(g.IsBusy,Is.True);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator EntryPauseMenuBackgroundAndViewportPreserveGridAndFinishSafely()
        {
            var g=Create(new Store());
            try
            {
                var board=g.Session.Board;g.TogglePause();Call(g,"TickEntry",5f);
                Assert.That(g.EntryPhase,Is.EqualTo(LevelEntryPhase.Field));g.TogglePause();
                g.ToggleMenu();Call(g,"TickEntry",5f);Assert.That(g.IsEntryReady,Is.False);g.CloseMenu();
                Call(g,"OnApplicationPause",true);Call(g,"TickEntry",5f);Assert.That(g.IsEntryReady,Is.False);
                Call(g,"OnApplicationPause",false);g.TogglePause();
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                    g.ApplyViewport(new Rect(0,0,size.x,size.y),size.x/390);
                Assert.That(g.Session.Board,Is.SameAs(board));g.SetReducedEntryMotion(true);Call(g,"TickEntry",.12f);
                Assert.That(g.IsEntryReady,Is.True);Assert.That(Bodies(g).All(b=>b.alpha==1 && b.transform.localScale==Vector3.one),Is.True);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator ExitDuringRevealRestoresSameAttemptUsingShortPath()
        {
            var store=new Store();var g=Create(store);var id=g.Session.SessionId;var seed=store.Data.Attempt.RewardSeed;
            try
            {
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(store);
                Assert.That(g.IsResumingEntry,Is.True);Assert.That(g.EntryPhase,Is.EqualTo(LevelEntryPhase.Ships));Assert.That(g.Session.SessionId,Is.EqualTo(id));
                Assert.That(store.Data.Attempt.RewardSeed,Is.EqualTo(seed));Assert.That(g.SaveService.DailyRestartCount,Is.Zero);
                Call(g,"TickEntry",.12f);Assert.That(g.IsEntryReady,Is.True);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator RestoreKeepsSpentToolsPauseAndActiveTransitWithoutAdvancingDuringFade()
        {
            var store=new Store{Data=Earned()};var g=Create(store);
            try
            {
                yield return Until(()=>g.IsEntryReady);g.SelectTool(ShipTool.Rescue);g.TogglePause();var id=g.Session.SessionId;
                var before=store.Data.Attempt.Copy();var stock=g.ToolInventory.Count(ShipTool.Rescue);
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(store);
                Assert.That(g.IsResumingEntry,Is.True);Assert.That(g.IsPaused,Is.True);Assert.That(g.Session.SessionId,Is.EqualTo(id));
                Assert.That(g.Tools.UsesLeft,Is.EqualTo(4));Assert.That(g.ToolInventory.Count(ShipTool.Rescue),Is.EqualTo(stock));
                Call(g,"TickEntry",1f);Assert.That(g.IsEntryReady,Is.False);Assert.That(g.Combat.Time,Is.EqualTo(before.Elapsed));
                g.TogglePause();Call(g,"TickEntry",.12f);Assert.That(g.IsEntryReady,Is.True);Assert.That(g.Combat.Time,Is.EqualTo(before.Elapsed));
                yield return Until(()=>g.Combat.HitCount==2);Assert.That(g.SaveService.Coins,Is.EqualTo(307));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator InitialSaveFailureRetriesSameCandidateWithoutEnteringPractice()
        {
            var store=new Store{Fail=true};var g=Create(store);
            try
            {
                var id=g.Session.SessionId;Assert.That(g.IsEntrySaveBlocked,Is.True);Assert.That(g.SaveService.IsAvailable,Is.True);
                g.ClickShip(g.Session.Board.Ships.First().Id);g.SelectLevel(0);Call(g,"TickEntry",20f);
                Assert.That(g.IsEntryReady,Is.False);Assert.That(store.Data,Is.Null);g.RetryEntry();Assert.That(g.Session.SessionId,Is.EqualTo(id));
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                {
                    g.ApplyViewport(new Rect(0,0,size.x,size.y),size.x/390);Canvas.ForceUpdateCanvases();
                    var panel=g.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="EntrySaveError");
                    foreach(var child in panel.GetComponentsInChildren<RectTransform>().Where(t=>t!=panel))
                    {Assert.That(child.anchoredPosition.y,Is.GreaterThanOrEqualTo(0));Assert.That(child.anchoredPosition.y+child.rect.height,Is.LessThanOrEqualTo(panel.rect.height));}
                }
                store.Fail=false;g.RetryEntry();g.RetryEntry();Assert.That(store.Data.Attempt.AttemptId,Is.EqualTo(id));
                Assert.That(g.IsEntrySaveBlocked,Is.False);yield return Until(()=>g.IsEntryReady);
                Assert.That(g.SaveService.Coins,Is.Zero);Assert.That(g.SaveService.DailyRestartCount,Is.Zero);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FailedRestartRetainsOldAttemptAndRetryCommitsOnlyOneReplacement()
        {
            var store=new Store{Data=Earned()};var g=Create(store);
            try
            {
                yield return Until(()=>g.IsEntryReady);g.SelectTool(ShipTool.Reverse);g.ClickShip(g.Session.Board.Ships.First().Id);
                var old=g.Session.SessionId;var coins=g.SaveService.Coins;store.Fail=true;g.Restart();g.Restart();
                Assert.That(g.Session.SessionId,Is.EqualTo(old));Assert.That(store.Data.Attempt.AttemptId,Is.EqualTo(old));
                Assert.That(g.IsEntrySaveBlocked,Is.True);Assert.That(g.Tools.UsesLeft,Is.EqualTo(4));Assert.That(g.SaveService.DailyRestartCount,Is.Zero);
                store.Fail=false;g.RetryEntry();var next=g.Session.SessionId;g.Restart();g.SelectLevel(2);g.RetryEntry();
                Assert.That(next,Is.Not.EqualTo(old));Assert.That(g.Session.SessionId,Is.EqualTo(next));Assert.That(g.SaveService.DailyRestartCount,Is.EqualTo(1));
                Assert.That(g.Tools.UsesLeft,Is.EqualTo(5));Assert.That(g.ToolInventory.Count(ShipTool.Reverse),Is.Zero);Assert.That(g.SaveService.Coins,Is.EqualTo(coins));
                yield return Until(()=>g.IsEntryReady);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator VictoryBarrierAndFailedSettlementCannotOpenAnotherAttempt()
        {
            var store=new Store{FailVictory=true};var g=Create(store,true);
            try
            {
                yield return Until(()=>g.IsEntryReady);var id=g.Session.SessionId;g.ToggleAuto();
                yield return Until(()=>g.Session.Board.ShipCount==0,30);
                Assert.That(g.IsCleared,Is.False);Assert.That(g.LevelIndex,Is.Zero);Assert.That(g.SaveService.Coins,Is.Zero);
                yield return Until(()=>g.IsCleared,20);yield return null;
                g.Restart();g.SelectLevel(0);g.SelectLevel(1);
                Assert.That(g.IsEntrySaveBlocked,Is.False);Assert.That(g.Session.SessionId,Is.EqualTo(id));
                Assert.That(g.SaveService.Coins,Is.Zero);Assert.That(g.SaveService.CurrentAttemptSettled,Is.False);
                store.FailVictory=false;g.Restart();
                Assert.That(g.SaveService.Coins,Is.EqualTo(107));Assert.That(g.SaveService.CurrentAttemptSettled,Is.True);
                yield return Until(()=>g.IsResultReadable);g.ContinueFromResult();yield return Until(()=>g.LevelIndex==1);var next=g.Session.SessionId;g.SelectLevel(1);g.Restart();
                Assert.That(g.Session.SessionId,Is.EqualTo(next));Assert.That(g.SaveService.DailyRestartCount,Is.Zero);
                Assert.That(g.SaveService.Snapshot.Settlements.Length,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FailedRestartFreezesActiveMovementUntilRetry()
        {
            var store=new Store();var g=Create(store);
            try
            {
                yield return Until(()=>g.IsEntryReady);
                var ship=g.Session.Board.Ships.First(s=>g.Session.Board.QueryForwardPath(s.Id).CanExit);g.ClickShip(ship.Id);
                Assert.That(g.IsBusy,Is.True);store.Fail=true;g.Restart();var position=g.ViewPosition(ship.Id);var board=g.Session.Board;
                yield return new WaitForSecondsRealtime(.4f);
                Assert.That(g.ViewPosition(ship.Id),Is.EqualTo(position));Assert.That(g.Session.Board,Is.SameAs(board));
                store.Fail=false;g.RetryEntry();yield return Until(()=>g.IsEntryReady);Assert.That(g.IsPaused,Is.False);
                Assert.That(g.Session.Board.ShipCount,Is.EqualTo(7));Assert.That(g.SaveService.DailyRestartCount,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
    }
}
