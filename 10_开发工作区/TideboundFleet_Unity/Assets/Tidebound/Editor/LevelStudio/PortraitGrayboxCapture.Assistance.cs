using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private static void AssistanceFixture(PortraitPuzzleGraybox game, bool empty = false, int used = 0)
        {
            game.EnableReviewMode();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PortraitPuzzleGraybox).GetMethod("ClearSession",flags).Invoke(game,null);
            var ships = new List<ShipPlacementData>();
            for(var i=0;i<40;i++)
            {
                var x=(i%3)*4;var y=i/3;
                ships.Add(new ShipPlacementData{Id="A"+i,TypeId="TF_BASE_SHIP",Position=new GridPosition(x,y),Direction=ShipDirection.Right,Length=2});
                ships.Add(new ShipPlacementData{Id="B"+i,TypeId="TF_BASE_SHIP",Position=new GridPosition(x+3,y),Direction=ShipDirection.Left,Length=2});
            }
            var first=SavedGameRuntime.Create(new LevelData{SchemaVersion=2,LevelId="E1_DeadlockReview",BossId="TF_KRAKEN_01",Width=14,Height=18,Ships=ships.ToArray()},3);
            var attempt=first.Capture();attempt.ToolUses=used;first.Dispose();var runtime=SavedGameRuntime.Restore(attempt);
            var data=new PlayerSaveData{CurrentLevel=3,HighestClearedLevel=2,Coins=307,Settlements=new[]{
                new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="Review1",LevelNumber=1,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=7,FirstClearCoins=100},
                new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="Review2",LevelNumber=2,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=80,FirstClearCoins=120}}};
            if(empty)data.Tools.Receipts=new[]{"milestone:tools:unlock"};
            var store=new MemoryPlayerSaveStore();store.Save(data);var service=new PlayerSaveService(store);service.Start(runtime);
            typeof(PortraitPuzzleGraybox).GetField("saveService",flags).SetValue(game,service);
            typeof(PortraitPuzzleGraybox).GetField("toolInventory",flags).SetValue(game,service.Inventory);
            typeof(PortraitPuzzleGraybox).GetMethod("BindWorld",flags).Invoke(game,new object[]{runtime,2});
            typeof(PortraitPuzzleGraybox).GetMethod("OnApplicationFocus",flags).Invoke(game,new object[]{true});
        }
        private static IEnumerator CaptureAssistanceFrames(PortraitPuzzleGraybox game)
        {
            foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(430,932)})
            {
                SetGameViewSize(size);var deadline=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<deadline)yield return null;
                AssistanceFixture(game);yield return null;yield return null;
                if(!game.IsDeadlockOpen)throw new InvalidOperationException("Deadlock prompt was not presented.");
                yield return SaveFrame($"Deadlock_{size.x}x{size.y}.png");
            }
            SetGameViewSize(new Vector2Int(390,844));yield return null;yield return null;
            AssistanceFixture(game,true);yield return null;yield return SaveFrame("Deadlock_EmptyStock.png");
            game.UseDeadlockTool(ShipTool.Rescue);yield return SaveFrame("Deadlock_Shop.png");
            AssistanceFixture(game,false,5);yield return null;yield return SaveFrame("Deadlock_FiveUses.png");
            game.EnableReviewMode();game.SelectLevel(2);game.SetAssistancePreferences(true,true);yield return null;
            var expires=Time.realtimeSinceStartup+9;
            while(game.AutoHintShipId==null && Time.realtimeSinceStartup<expires)yield return null;
            if(game.AutoHintShipId==null)throw new TimeoutException("No automatic direct-exit highlight appeared.");
            yield return SaveFrame("IdleHint_390x844.png");
            game.ToggleMenu();yield return SaveFrame("Assistance_Settings.png");
        }
    }
}
