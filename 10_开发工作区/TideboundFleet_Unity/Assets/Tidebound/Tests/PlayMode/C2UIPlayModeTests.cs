using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class C2UIPlayModeTests
    {
        private sealed class Store:IPlayerSaveStore
        {public PlayerSaveData Data;public int Writes;public PlayerSaveData Load()=>Data?.Copy();public void Save(PlayerSaveData value){Data=value.Copy();Writes++;}}
        private static Store Earned()
        {
            var d=new PlayerSaveData{CurrentLevel=5,HighestClearedLevel=4,Settlements=Enumerable.Range(1,4).Select(n=>new SettlementRecord
            {AttemptId=Guid.NewGuid().ToString("N"),LevelId="C2_"+n,LevelNumber=n,Kind="Victory",Day="2026-09-21",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
            d.Coins=d.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);return new Store{Data=d};
        }
        private static PortraitPuzzleGraybox Create(Store store)
        {
            var catalog=new PlayableLevelCatalog(Enumerable.Range(1,10).Select(n=>"C2_"+n),_=>true,id=>new LevelData
            {SchemaVersion=2,LevelId=id,BossId="TF_KRAKEN_01",Width=6,Height=6,Ships=new[]{new ShipPlacementData{Id="A",TypeId="TF_BASE_SHIP",Position=new GridPosition(0,0),Direction=ShipDirection.Up,Length=2},new ShipPlacementData{Id="B",TypeId="TF_BASE_SHIP",Position=new GridPosition(3,0),Direction=ShipDirection.Up,Length=2}}});
            var service=new PlayerSaveService(store);service.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");
            var game=new GameObject("C2_UI_Test").AddComponent<PortraitPuzzleGraybox>();game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);return game;
        }
        [UnityTest]
        public IEnumerator LanguageAndCategoriesPreserveAttemptWalletInventoryAndEquipment()
        {
            var pref=UILanguage.Preference;var store=Earned();var game=Create(store);
            try
            {
                game.ContinueFromHome();game.SelectTool(ShipTool.Reverse);game.ClickShip("A");game.ReturnHome();
                var id=game.Session.SessionId;var writes=store.Writes;var snapshot=store.Data.Copy();
                UILanguage.SetPreference(LanguagePreference.Chinese,false);game.OpenCollection();
                for(var i=0;i<4;i++)game.CollectionView.SelectCategory(i);
                Assert.That(game.CollectionView.ActiveCategory,Is.EqualTo(3));
                Assert.That(game.CollectionView.transform.Find("CollectionScroll/Content/CategoryPlaceholder/Info").GetComponent<Text>().text,Does.Contain("通关"));
                game.CloseCollection();game.OpenHomeDraw();Assert.That(game.CollectionView.IsDrawPage,Is.True);
                game.CloseCollection();UILanguage.SetPreference(LanguagePreference.English,false);yield return null;
                Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(store.Data.Coins,Is.EqualTo(snapshot.Coins));
                Assert.That(store.Data.Collection.Equipment,Is.EqualTo(snapshot.Collection.Equipment));Assert.That(store.Data.Attempt.ToolUses,Is.EqualTo(snapshot.Attempt.ToolUses));
                Assert.That(game.Tools.UsesLeft,Is.EqualTo(4));game.ContinueFromHome();Assert.That(game.Session.SessionId,Is.EqualTo(id));
            }
            finally{UILanguage.SetPreference(pref,false);UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator CollectionConfirmationBlocksCategoryNavigationAndChargesExactlyOnce()
        {
            var store=Earned();var game=Create(store);
            try
            {
                game.OpenHomeDraw();var panel=game.CollectionView;var coins=game.SaveService.Coins;
                panel.SelectTransaction("Single");panel.SelectCategory(2);Assert.That(panel.ActiveCategory,Is.Zero);Assert.That(panel.IsDrawPage,Is.True);
                panel.ConfirmTransaction();panel.ConfirmTransaction();yield return null;
                Assert.That(game.SaveService.Coins,Is.EqualTo(coins-300));Assert.That(game.SaveService.Snapshot.Collection.Receipts.Length,Is.EqualTo(2));
                Assert.That(game.SaveService.Snapshot.Attempt,Is.Null);
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator SmallScreenHasScrollableContentAndMinimumButtonTargets()
        {
            var game=Create(Earned());
            try
            {
                game.OpenCollection();var panel=game.CollectionView;panel.Layout(new Rect(0,0,360,600));yield return null;
                var scroll=panel.GetComponentInChildren<ScrollRect>();Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
                foreach(var button in panel.GetComponentsInChildren<Button>())
                {var rect=((RectTransform)button.transform).rect;Assert.That(rect.width,Is.GreaterThanOrEqualTo(48),button.name);Assert.That(rect.height,Is.GreaterThanOrEqualTo(48),button.name);}
                var original=scroll.content.anchoredPosition.y;scroll.verticalNormalizedPosition=0;yield return null;
                Assert.That(scroll.content.anchoredPosition.y,Is.GreaterThan(original));
                game.CloseCollection();game.OpenShop();game.ShopPanel.Layout(new Rect(0,0,360,600));yield return null;
                var shopScroll=game.ShopPanel.GetComponentInChildren<ScrollRect>();shopScroll.verticalNormalizedPosition=0;yield return null;
                var corners=new Vector3[4];game.ShopPanel.transform.Find("ProductScroll/Content/Product_tools_bundle_1").GetComponent<RectTransform>().GetWorldCorners(corners);
                Assert.That(corners[0].y,Is.GreaterThanOrEqualTo(shopScroll.viewport.position.y-1));
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator ModalRaycastsDoNotReachHomeStartAndSettingsCannotStartAnAttempt()
        {
            var store=Earned();var game=Create(store);
            try
            {
                var settings=game.transform.Find("HomeNavigation/HomeControls/Settings").GetComponent<Button>();settings.onClick.Invoke();game.ContinueFromHome();Assert.That(game.Session,Is.Null);
                game.transform.Find("HomeNavigation/HomeSettings/Close").GetComponent<Button>().onClick.Invoke();game.OpenCollection();yield return null;
                var start=game.transform.Find("HomeNavigation/HomeControls/Continue");Assert.That(start.gameObject.activeInHierarchy,Is.False);
                var panel=game.CollectionView;panel.SelectTransaction("Single");yield return null;
                var pos=RectTransformUtility.WorldToScreenPoint(null,panel.transform.Find("Transaction/Confirm").position+new Vector3(30,20));
                var results=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=pos},results);
                Assert.That(results.Count,Is.GreaterThan(0));Assert.That(results[0].gameObject.transform.IsChildOf(panel.transform.Find("Transaction")),Is.True);
                Assert.That(store.Data.Attempt,Is.Null);
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator RaisedButtonsKeepHitTargetsAndNeverSpendOnPressOrCancel()
        {
            var store=Earned();var game=Create(store);
            try
            {
                var root=game.transform.Find("HomeNavigation/HomeControls/Collection").GetComponent<RectTransform>();
                var relief=root.GetComponent<HarborButtonRelief>();relief.AllowMotion=()=>true;
                var corners=new Vector3[4];root.GetWorldCorners(corners);var before=(Vector3[])corners.Clone();
                var writes=store.Writes;var coins=store.Data.Coins;
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(relief.PressAmount,Is.GreaterThan(.95f));root.GetWorldCorners(corners);Assert.That(corners,Is.EqualTo(before));
                Assert.That(game.IsHomeOpen,Is.True);Assert.That(store.Data.Attempt,Is.Null);Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(store.Data.Coins,Is.EqualTo(coins));
                ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerExitHandler);yield return new WaitForSecondsRealtime(.2f);
                Assert.That(relief.PressAmount,Is.Zero);Assert.That(game.IsHomeOpen,Is.True);
                // Disabled future features stay in full colour but cannot acquire a pressed state.
                var future=game.transform.Find("HomeNavigation/HomeControls/Daily Gift");
                ExecuteEvents.Execute(future.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;
                Assert.That(future.GetComponent<HarborButtonRelief>().PressAmount,Is.Zero);
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator ReducedMotionAndModalDisableResetButtonVisuals()
        {
            var game=Create(Earned());
            try
            {
                var root=game.transform.Find("HomeNavigation/HomeControls/Collection");var relief=root.GetComponent<HarborButtonRelief>();
                relief.AllowMotion=()=>false;yield return null;var icon=relief.FloatingIcon.anchoredPosition;
                yield return new WaitForSecondsRealtime(.1f);Assert.That(relief.FloatingIcon.anchoredPosition,Is.EqualTo(icon));
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(root.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;Assert.That(relief.PressAmount,Is.EqualTo(1));
                game.OpenCollection();Assert.That(root.gameObject.activeInHierarchy,Is.False);Assert.That(relief.PressAmount,Is.Zero);
                game.CloseCollection();yield return null;Assert.That(relief.PressAmount,Is.Zero);Assert.That(relief.FloatingIcon.anchoredPosition,Is.EqualTo(icon));
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
    }
}
