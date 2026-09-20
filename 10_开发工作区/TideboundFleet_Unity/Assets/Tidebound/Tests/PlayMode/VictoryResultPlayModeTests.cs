using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Lane;
using Tidebound.Combat;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class VictoryResultPlayModeTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail,FailVictory,FailNext,Corrupt;public int Writes;
            public PlayerSaveData Load(){if(Corrupt)throw new IOException();return Data?.Copy();}
            public void Save(PlayerSaveData d){if(Fail || (FailVictory && d.Attempt?.Victory==true) || (FailNext && d.Attempt?.LevelNumber==2 && !d.Attempt.Victory))throw new IOException();Data=d.Copy();Writes++;}
        }
        private static CandidateLevelCatalog Catalog()
        {
            var p=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(p,"manifest.json")),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".solution.json"))));
        }
        private static PortraitPuzzleGraybox Create(Store store)
        {
            var g=new GameObject("Result_Test").AddComponent<PortraitPuzzleGraybox>();
            g.Initialize(Catalog(),new ShipMovementTiming(1000,.005f,.01f,.01f,.06f),new LaneTransitTiming(.3,.005,.005),new CombatTiming(.005,.08),saveService:new PlayerSaveService(store),campaign:true,animateEntry:true);
            Call(g,"OnApplicationFocus",true);return g;
        }
        private static void Call(PortraitPuzzleGraybox g,string name,params object[] args)=>typeof(PortraitPuzzleGraybox).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,args);
        private static IEnumerator Until(Func<bool> predicate,float seconds=15)
        {var end=Time.realtimeSinceStartup+seconds;while(!predicate() && Time.realtimeSinceStartup<end)yield return null;Assert.That(predicate(),Is.True);}
        private static PlayerSaveData BeforeLevel(int level)=>new PlayerSaveData{CurrentLevel=level,HighestClearedLevel=level-1,
            Coins=Enumerable.Range(1,level-1).Sum(n=>(long)(n==1?7:80)+BattleCoinRules.FirstClear(n)),
            Settlements=Enumerable.Range(1,level-1).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="Seed"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
        private static void CompleteModel(SavedGameRuntime game)
        {
            var proof=LevelSolver.Solve(game.Session.Board,new LevelSolverOptions(4000,400000,500));Assert.That(proof.Status,Is.EqualTo(LevelSolverStatus.Solved));
            foreach(var id in proof.ShipIds){var op=game.Movement.TryBeginMove(id);Assert.That(op.IsAccepted,Is.True);if(op.Operation.Stage==ShipMoveStage.Traveling)game.Movement.CompleteTravel(op.Operation.OperationId);if(game.Movement.IsBusy)game.Movement.CompleteBlockedFeedback(op.Operation.OperationId);}
            game.Transit.Advance(100);game.Combat.Advance();Assert.That(game.Combat.IsVictorious,Is.True);
        }
        private static Store Won(int level)
        {
            var store=new Store{Data=BeforeLevel(level)};var service=new PlayerSaveService(store);
            using(var game=SavedGameRuntime.Create(Catalog().Load(level-1),level))
            {Assert.That(service.Start(game),Is.True);CompleteModel(game);Assert.That(service.Checkpoint(),Is.True);}return store;
        }
        [UnityTest]
        public IEnumerator FullVictoryBarrierThenReadableResultWaitsForExplicitContinue()
        {
            var store=new Store();var g=Create(store);
            try
            {
                yield return Until(()=>g.IsEntryReady);var id=g.Session.SessionId;g.ToggleAuto();yield return Until(()=>g.Session.Board.ShipCount==0);
                Assert.That(g.IsCleared,Is.False);Assert.That(g.IsResultOpen,Is.False);Assert.That(g.SaveService.Coins,Is.Zero);
                yield return Until(()=>g.IsResultReadable);Assert.That(g.CurrentResult.BattleCoins,Is.EqualTo(7));Assert.That(g.CurrentResult.FirstClearCoins,Is.EqualTo(100));
                yield return new WaitForSecondsRealtime(1.4f);Assert.That(g.Session.SessionId,Is.EqualTo(id));Assert.That(g.LevelIndex,Is.Zero);
                g.ToggleResultOverview();Assert.That(g.ResultView.IsOverview,Is.True);Assert.That(g.SaveService.Coins,Is.EqualTo(107));g.ToggleResultOverview();
                g.ContinueFromResult();var next=g.Session.SessionId;g.ContinueFromResult();g.Restart();g.SelectLevel(1);
                Assert.That(next,Is.Not.EqualTo(id));Assert.That(g.Session.SessionId,Is.EqualTo(next));Assert.That(g.LevelIndex,Is.EqualTo(1));
                Assert.That(store.Data.Settlements.Length,Is.EqualTo(1));Assert.That(g.SaveService.DailyRestartCount,Is.Zero);Assert.That(g.IsResultOpen,Is.False);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FailedRewardSaveShowsNoCreditedAmountAndRetriesWithoutDuplicates()
        {
            var store=new Store{FailVictory=true};var g=Create(store);
            try
            {
                yield return Until(()=>g.IsEntryReady);g.ToggleAuto();yield return Until(()=>g.IsResultSaveFailed);
                Assert.That(g.IsResultOpen,Is.True);Assert.That(g.CurrentResult,Is.Null);Assert.That(g.SaveService.Coins,Is.Zero);
                Assert.That(g.ResultView.transform.Find("TotalCoins").gameObject.activeSelf,Is.False);
                var id=g.Session.SessionId;g.ContinueFromResult();g.SelectLevel(1);Assert.That(g.Session.SessionId,Is.EqualTo(id));
                store.FailVictory=false;yield return new WaitForSecondsRealtime(.2f);Assert.That(g.SaveService.Coins,Is.Zero,"Failure retries must be explicit, not every frame.");
                g.RetryResultSave();g.RetryResultSave();yield return Until(()=>g.IsResultReadable);
                Assert.That(g.SaveService.Coins,Is.EqualTo(107));Assert.That(store.Data.Settlements.Length,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator StoredResultRestoresWithoutReplayRegrantOrNewAttemptEvenIfStorageIsReadOnly()
        {
            var store=Won(1);var id=store.Data.Attempt.AttemptId;store.Fail=true;var g=Create(store);
            try
            {
                Assert.That(g.IsResultReadable,Is.True);Assert.That(g.Session.SessionId,Is.EqualTo(id));var writes=store.Writes;
                g.ToggleResultOverview();Call(g,"OnApplicationPause",true);Call(g,"OnApplicationPause",false);yield return null;
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(store);
                Assert.That(g.IsResultReadable,Is.True);Assert.That(g.ResultView.IsOverview,Is.False);Assert.That(g.SaveService.Coins,Is.EqualTo(107));
                Assert.That(g.Session.SessionId,Is.EqualTo(id));Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(g.SaveService.Snapshot.Settlements.Length,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FailedNextAttemptCanRetryAndReloadNeverLosesStoredResult()
        {
            var store=Won(1);var g=Create(store);
            try
            {
                var id=g.Session.SessionId;store.FailNext=true;g.ContinueFromResult();g.ContinueFromResult();
                Assert.That(g.IsEntrySaveBlocked,Is.True);Assert.That(store.Data.Attempt.AttemptId,Is.EqualTo(id));
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(store);Assert.That(g.IsResultReadable,Is.True);Assert.That(g.Session.SessionId,Is.EqualTo(id));
                g.ContinueFromResult();Assert.That(g.IsEntrySaveBlocked,Is.True);store.FailNext=false;g.RetryEntry();g.RetryEntry();
                var next=g.Session.SessionId;Assert.That(next,Is.Not.EqualTo(id));Assert.That(g.SaveService.Coins,Is.EqualTo(107));
                UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(store);
                Assert.That(g.IsResultOpen,Is.False);Assert.That(g.IsResumingEntry,Is.True);Assert.That(g.Session.SessionId,Is.EqualTo(next));Assert.That(g.LevelIndex,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator LastInstalledLevelShowsCompleteWithoutCreatingLevelEleven()
        {
            var store=Won(10);var g=Create(store);
            try
            {
                Assert.That(g.IsResultReadable,Is.True);Assert.That(g.CurrentResult.HasNext,Is.False);Assert.That(g.CurrentResult.Progress,Is.EqualTo(1));
                var id=g.Session.SessionId;g.ContinueFromResult();g.Restart();g.ToggleResultOverview();g.ContinueFromResult();
                Assert.That(g.Session.SessionId,Is.EqualTo(id));Assert.That(store.Data.Attempt.LevelNumber,Is.EqualTo(10));Assert.That(g.SaveService.CurrentLevel,Is.EqualTo(11));
                Assert.That(g.ResultView.transform.Find("NextLevel").GetComponent<Button>().interactable,Is.False);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator CollectionGateRunsBeforeLevelThreeCreationAndCannotBeBypassed()
        {
            var store=Won(2);var g=Create(store);
            try
            {
                g.SaveService.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");
                var id=g.Session.SessionId;var allow=false;var calls=0;
                g.BeforeNextLevel=result=>{calls++;Assert.That(result.IsCollectionCheckpoint,Is.True);Assert.That(g.Session.SessionId,Is.EqualTo(id));return allow;};
                g.ContinueFromResult();g.SelectLevel(2);Assert.That(g.Session.SessionId,Is.EqualTo(id));Assert.That(calls,Is.EqualTo(1));
                allow=true;g.ContinueFromResult();g.ContinueFromResult();Assert.That(calls,Is.EqualTo(2));Assert.That(g.LevelIndex,Is.EqualTo(2));
                Assert.That(g.SaveService.Coins,Is.EqualTo(307));Assert.That(g.Tools.UsesLeft,Is.EqualTo(5));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator ResultFitsSafeAreasAndReducedMotionCompletesDisplayWithoutChangingReward()
        {
            var g=Create(Won(6));
            try
            {
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                {
                    g.ApplyViewport(new Rect(0,0,size.x,size.y),size.x/390);Canvas.ForceUpdateCanvases();var panel=(RectTransform)g.ResultView.transform;
                    foreach(var child in panel.GetComponentsInChildren<RectTransform>().Where(t=>t.parent==panel))
                    {Assert.That(child.anchoredPosition.y,Is.GreaterThanOrEqualTo(0));Assert.That(child.anchoredPosition.y+child.rect.height,Is.LessThanOrEqualTo(panel.rect.height+.01f));}
                }
                var coins=g.SaveService.Coins;g.SetReducedResultMotion(true);Call(g,"OnApplicationFocus",false);Call(g,"TickResult",10f);Call(g,"OnApplicationFocus",true);
                Assert.That(g.IsResultReadable,Is.True);Assert.That(g.SaveService.Coins,Is.EqualTo(coins));Assert.That(g.CurrentResult.ChapterCompleted,Is.EqualTo(6));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator CorruptAccountPracticeStillHasExitWithoutFakeRewards()
        {
            var g=Create(new Store{Corrupt=true});
            try
            {
                yield return Until(()=>g.IsEntryReady);g.ToggleAuto();yield return Until(()=>g.IsResultOpen);
                Assert.That(g.CurrentResult,Is.Null);Assert.That(g.ResultView.transform.Find("TotalCoins").gameObject.activeSelf,Is.False);
                var id=g.Session.SessionId;g.ContinueFromResult();Assert.That(g.Session.SessionId,Is.Not.EqualTo(id));Assert.That(g.SaveService.IsAvailable,Is.False);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
    }
}
