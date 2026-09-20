using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEditor;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private sealed class EntryCaptureStore:IPlayerSaveStore
        {
            public bool Fail;private PlayerSaveData data;
            public PlayerSaveData Load()=>data?.Copy();
            public void Save(PlayerSaveData next){if(Fail)throw new IOException();data=next.Copy();}
        }
        private static void StepEntry(PortraitPuzzleGraybox game,float seconds)
        {
            typeof(PortraitPuzzleGraybox).GetMethod("OnApplicationFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{true});
            typeof(PortraitPuzzleGraybox).GetMethod("TickEntry",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{seconds});
            typeof(PortraitPuzzleGraybox).GetMethod("UpdateLabels",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        }
        private static System.Collections.IEnumerator SaveEntryFrame(PortraitPuzzleGraybox game,string name)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();game.BoardCamera.Render();
            yield return SaveFrame(name);
        }
        private static PlayerSaveData EntryAccount()=>new PlayerSaveData{CurrentLevel=3,HighestClearedLevel=2,Coins=307,Settlements=Enumerable.Range(1,2).Select(n=>new SettlementRecord
        {AttemptId=Guid.NewGuid().ToString("N"),LevelId="Review"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
        private static System.Collections.IEnumerator CaptureEntryFrames(PortraitPuzzleGraybox game)
        {
            // Stop only the presentation clock for precise keyframes. Logical state comes from real save transactions.
            game.enabled=false;
            foreach(var size in Sizes.Take(3))
            {
                SetGameViewSize(size);var expires=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<expires)yield return null;
                if(Screen.width!=size.x || Screen.height!=size.y)throw new TimeoutException("Entry viewport did not settle.");
                var store=new EntryCaptureStore();store.Save(EntryAccount());game.EnableReviewMode(new PlayerSaveService(store),true);
                game.ApplyViewport(new Rect(0,0,size.x,size.y),size.x/390f);
                if(size.x==390)yield return SaveEntryFrame(game,"Entry_Field_390x844.png");
                StepEntry(game,.38f);
                if(size.x==390)yield return SaveEntryFrame(game,"Entry_Ships_390x844.png");
                StepEntry(game,.28f);yield return SaveEntryFrame(game,$"Entry_Ready_{size.x}x{size.y}.png");
                if(size.x==390)
                {
                    game.EnableReviewMode(new PlayerSaveService(store),true);StepEntry(game,.06f);
                    yield return SaveEntryFrame(game,"Entry_Resume_390x844.png");StepEntry(game,.06f);
                    game.ToggleMenu();game.SetReducedEntryMotion(true);yield return SaveEntryFrame(game,"Entry_Settings_390x844.png");game.CloseMenu();game.SetReducedEntryMotion(false);
                }
                if(size.x!=390)
                {
                    var failing=new EntryCaptureStore{Fail=true};game.EnableReviewMode(new PlayerSaveService(failing),true);
                    yield return SaveEntryFrame(game,$"Entry_SaveFailure_{size.x}x{size.y}.png");
                }
            }
        }
    }
}
