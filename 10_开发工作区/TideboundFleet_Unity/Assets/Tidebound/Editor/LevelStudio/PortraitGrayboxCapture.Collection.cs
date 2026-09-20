using System;
using System.Linq;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private static System.Collections.IEnumerator CaptureCollectionFrames(PortraitPuzzleGraybox game)
        {
            game.enabled=false;var catalog=ResultCatalog(game);
            foreach(var size in Sizes.Take(3))
            {
                SetGameViewSize(size);var expires=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<expires)yield return null;
                BindResultCapture(game,catalog,ResultStore(catalog,2));game.ApplyViewport(new Rect(0,0,size.x,size.y),size.x/390f);
                game.ContinueFromResult();yield return SaveResultFrame(game,$"Collection_{size.x}x{size.y}.png");
            }
            game.CollectionView.SelectTransaction("FirstBlue");yield return SaveResultFrame(game,"Collection_FreeBlue_430x932.png");
            game.CollectionView.ConfirmTransaction();yield return SaveResultFrame(game,"Collection_FreeResult_430x932.png");
            game.CollectionView.transform.Find("Transaction/Back").GetComponent<Button>().onClick.Invoke();game.CollectionView.EquipSelected();
            game.CloseCollection();game.ContinueFromResult();game.SetReducedEntryMotion(true);game.enabled=true;
            var until=Time.realtimeSinceStartup+8;while(!game.IsEntryReady && Time.realtimeSinceStartup<until)yield return null;
            game.enabled=false;yield return SaveResultFrame(game,"Collection_NewFleet_430x932.png");
            var store=ResultStore(catalog,9);
            // Isolated review receipts: ten earlier 50-coin deadlock settlements fund a ten-draw preview.
            for(var i=1;i<=10;i++)store.Data.Settlements=store.Data.Settlements.Concat(new[]{new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="ReviewRestart",LevelNumber=9,Kind="DeadlockRestart",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=50,RestartOrdinal=i}}).ToArray();
            store.Data.Coins+=500;store.Data.MaxLocalDay="2026-09-20";store.Data.DailyRestartCount=10;store.Data.Validate();
            BindResultCapture(game,catalog,store);game.OpenCollection();game.CollectionView.SelectTransaction("FirstBlue");game.CollectionView.ConfirmTransaction();game.CollectionView.transform.Find("Transaction/Back").GetComponent<Button>().onClick.Invoke();
            game.CollectionView.SelectTransaction("Ten");game.CollectionView.ConfirmTransaction();
            if(game.CollectionView.LastResult!=CollectionStatus.Saved)throw new InvalidOperationException("Ten draw capture failed.");
            yield return SaveResultFrame(game,"Collection_TenResult_430x932.png");
            game.CollectionView.transform.Find("Transaction/Back").GetComponent<Button>().onClick.Invoke();game.CollectionView.ShowOdds();yield return SaveResultFrame(game,"Collection_Rates_430x932.png");
        }
    }
}
