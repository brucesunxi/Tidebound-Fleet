using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.LevelDesign;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;
using UnityEditor;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private sealed class ResultCaptureStore:IPlayerSaveStore
        {
            public PlayerSaveData Data;public bool Fail;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData value){if(Fail)throw new IOException();Data=value.Copy();}
        }
        private static IPlayableLevelCatalog ResultCatalog(PortraitPuzzleGraybox game)=>(IPlayableLevelCatalog)typeof(PortraitPuzzleGraybox).GetField("catalog",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
        private static void CompleteResultModel(SavedGameRuntime game)
        {
            var proof=LevelSolver.Solve(game.Session.Board,new LevelSolverOptions(4000,400000,500));
            if(proof.Status!=LevelSolverStatus.Solved)throw new InvalidOperationException("Capture board is not solvable.");
            foreach(var id in proof.ShipIds)
            {var op=game.Movement.TryBeginMove(id);if(op.Operation.Stage==ShipMoveStage.Traveling)game.Movement.CompleteTravel(op.Operation.OperationId);if(game.Movement.IsBusy)game.Movement.CompleteBlockedFeedback(op.Operation.OperationId);}
            game.Transit.Advance(100);game.Combat.Advance();
        }
        private static ResultCaptureStore ResultStore(IPlayableLevelCatalog catalog,int level,bool settled=true)
        {
            var store=new ResultCaptureStore{Data=new PlayerSaveData{CurrentLevel=level,HighestClearedLevel=level-1,
                Coins=Enumerable.Range(1,level-1).Sum(n=>(long)(n==1?7:80)+BattleCoinRules.FirstClear(n)),
                Settlements=Enumerable.Range(1,level-1).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="Review"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()}};
            var service=new PlayerSaveService(store);
            using(var runtime=SavedGameRuntime.Create(catalog.Load(level-1),level))
            {
                if(!service.Start(runtime))throw new IOException("Cannot seed result capture.");CompleteResultModel(runtime);
                if(settled){if(!service.Checkpoint())throw new IOException("Cannot settle result capture.");}
                else {store.Data.Attempt=runtime.Capture();store.Data.Validate();store.Fail=true;}
            }
            return store;
        }
        private static void BindResultCapture(PortraitPuzzleGraybox game,IPlayableLevelCatalog catalog,ResultCaptureStore store)
        {game.Initialize(catalog,saveService:new PlayerSaveService(store),campaign:true,animateEntry:true);game.enabled=false;}
        private static System.Collections.IEnumerator SaveResultFrame(PortraitPuzzleGraybox game,string name)
        {yield return null;Canvas.ForceUpdateCanvases();game.BoardCamera.Render();yield return SaveFrame(name);}
        private static System.Collections.IEnumerator CaptureResultFrames(PortraitPuzzleGraybox game)
        {
            game.enabled=false;var catalog=ResultCatalog(game);game.SetReducedResultMotion(false);
            foreach(var size in Sizes.Take(3))
            {
                SetGameViewSize(size);var expires=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<expires)yield return null;
                if(Screen.width!=size.x || Screen.height!=size.y)throw new TimeoutException("Result viewport did not settle.");
                BindResultCapture(game,catalog,ResultStore(catalog,6));game.ApplyViewport(new Rect(0,0,size.x,size.y),size.x/390f);
                yield return SaveResultFrame(game,$"Result_Level6_{size.x}x{size.y}.png");
            }
            SetGameViewSize(new Vector2Int(390,844));yield return null;yield return null;
            game.ApplyViewport(new Rect(0,0,390,844),1);
            typeof(PortraitPuzzleGraybox).GetField("resultRevealTime",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,.2f);
            typeof(PortraitPuzzleGraybox).GetMethod("PaintResult",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
            yield return SaveResultFrame(game,"Result_Progress_390x844.png");
            game.ToggleResultOverview();yield return SaveResultFrame(game,"Result_Overview_390x844.png");
            BindResultCapture(game,catalog,ResultStore(catalog,10));yield return SaveResultFrame(game,"Result_LastLevel_390x844.png");
            BindResultCapture(game,catalog,ResultStore(catalog,1,false));yield return SaveResultFrame(game,"Result_SaveFailure_390x844.png");
            var stored=ResultStore(catalog,1);BindResultCapture(game,catalog,stored);yield return SaveResultFrame(game,"Result_Restored_390x844.png");
            stored.Fail=true;game.ContinueFromResult();yield return SaveResultFrame(game,"Result_NextSaveFailure_390x844.png");
        }
    }
}
