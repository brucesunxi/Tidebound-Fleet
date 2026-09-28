using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class HomeNavigationPlayModeTests
    {
        private sealed class Store : IPlayerSaveStore
        {
            public PlayerSaveData Data; public bool Fail, Corrupt; public int Writes;
            public PlayerSaveData Load() { if(Corrupt)throw new InvalidDataException(); return Data?.Copy(); }
            public void Save(PlayerSaveData data) { if (Fail) throw new IOException(); Data = data.Copy(); Writes++; }
        }
        private static LevelData Level(string id) => new LevelData
        {
            SchemaVersion = 2, LevelId = id, BossId = "TF_KRAKEN_01", Width = 6, Height = 6,
            Ships = new[]
            {
                new ShipPlacementData { Id = "A", TypeId = "TF_BASE_SHIP", Position = new GridPosition(0,0), Direction = ShipDirection.Up, Length = 2 },
                new ShipPlacementData { Id = "B", TypeId = "TF_BASE_SHIP", Position = new GridPosition(3,0), Direction = ShipDirection.Up, Length = 2 }
            }
        };
        private static PlayableLevelCatalog Catalog(int count = 10) => new PlayableLevelCatalog(
            Enumerable.Range(1, count).Select(n => "Nav_" + n), _ => true, Level);
        private static PlayerSaveData Before(int level) => new PlayerSaveData
        {
            CurrentLevel = level, HighestClearedLevel = level - 1, Coins = Enumerable.Range(1, level - 1).Sum(n => (long)(n == 1 ? 7 : 80) + BattleCoinRules.FirstClear(n)),
            Settlements = Enumerable.Range(1, level - 1).Select(n => new SettlementRecord
            {
                AttemptId = Guid.NewGuid().ToString("N"), LevelId = "Nav_" + n, LevelNumber = n,
                Kind = "Victory", Day = "2026-09-20", EconomyVersion = BattleCoinRules.Version,
                BattleCoins = n == 1 ? 7 : 80, FirstClearCoins = BattleCoinRules.FirstClear(n)
            }).ToArray()
        };
        private static Store Won(int level)
        {
            var store = new Store { Data = Before(level) }; var service = new PlayerSaveService(store);
            if (level >= 3) service.Collect(Guid.NewGuid().ToString("N"), "FirstBlue");
            using (var runtime = service.CreateNextAttempt(Level("Nav_" + level)))
            {
                Assert.That(service.Start(runtime), Is.True);
                foreach (var id in new[] { "A", "B" })
                {
                    service.PrepareMove(id); var move = runtime.Movement.TryBeginMove(id).Operation;
                    runtime.Movement.CompleteTravel(move.OperationId);
                    runtime.Transit.Advance(100); runtime.Combat.Advance();
                }
                Assert.That(runtime.Combat.IsVictorious, Is.True); Assert.That(service.Checkpoint(true), Is.True);
            }
            return store;
        }
        private static PortraitPuzzleGraybox Create(Store store, IPlayableLevelCatalog catalog = null)
        {
            var game = new GameObject("HomeNavigation_Test").AddComponent<PortraitPuzzleGraybox>();
            game.Initialize(catalog ?? Catalog(), saveService: new PlayerSaveService(store), campaign: true, useHomeNavigation: true);
            typeof(PortraitPuzzleGraybox).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(game, new object[] { true });
            return game;
        }
        [UnityTest]
        public IEnumerator HomeStartResetsDeparturesAndClockButPreservesAccount()
        {
            var store=new Store();var game=Create(store);
            try
            {
                Assert.That(game.IsHomeOpen,Is.True);Assert.That(game.Session,Is.Null);
                game.ContinueFromHome();var id=game.Session.SessionId;
                game.ClickShip("A");var until=Time.realtimeSinceStartup+5;while(game.IsBusy&&Time.realtimeSinceStartup<until)yield return null;
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(1));game.ReturnHome();Assert.That(game.IsHomeOpen,Is.True);
                var elapsed=game.SaveService.Runtime.Transit.ElapsedTime;var coins=game.SaveService.Coins;
                yield return new WaitForSecondsRealtime(.1f);Assert.That(game.SaveService.Runtime.Transit.ElapsedTime,Is.EqualTo(elapsed));
                game.ContinueFromHome();Assert.That(game.Session.SessionId,Is.Not.EqualTo(id));
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(2));Assert.That(game.SaveService.Runtime.Transit.ElapsedTime,Is.Zero);
                Assert.That(game.SaveService.Runtime.PendingCoins,Is.Zero);Assert.That(game.SaveService.Coins,Is.EqualTo(coins));
                Assert.That(game.IsResultOpen,Is.False);Assert.That(game.IsResumingEntry,Is.False);
                var next=game.Session.SessionId;game.ContinueFromHome();Assert.That(game.Session.SessionId,Is.EqualTo(next));
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator WonTenStartsElevenAndDisplaysStartGameInsteadOfOldReceipt()
        {
            var store=Won(10);var game=Create(store,Catalog(100));
            try
            {
                var stars=game.SaveService.Snapshot.ClearStars;var coins=game.SaveService.Coins;
                var label=game.transform.Find("UI_MainMenu/MainActionButton/UI_Button_Main/Face/SubLabel").GetComponent<Text>();
                Assert.That(label.text,Does.Contain("11"));
                var main=label.transform.parent.Find("Label").GetComponent<Tidebound.Unity.UI.HarborText>();Assert.That(main.Source,Is.EqualTo("Start Game"));
                game.ContinueFromHome();Assert.That(game.LevelIndex,Is.EqualTo(10));Assert.That(game.IsResultOpen,Is.False);
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(2));Assert.That(game.SaveService.Snapshot.ClearStars,Is.EqualTo(stars));Assert.That(game.SaveService.Coins,Is.EqualTo(coins));
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator RelaunchWithIncompleteTenStartsTenFreshAndStorageFailureKeepsOldAttempt()
        {
            var store=new Store{Data=Before(10)};var service=new PlayerSaveService(store);
            using(var attempt=service.CreateNextAttempt(Level("Nav_10")))
            {service.Start(attempt);var op=attempt.Movement.TryBeginMove("A").Operation;attempt.Movement.CompleteTravel(op.OperationId);service.Checkpoint(true);}
            var id=store.Data.Attempt.AttemptId;var game=Create(store,Catalog(100));
            try
            {
                store.Fail=true;game.ContinueFromHome();Assert.That(game.IsHomeOpen,Is.True);Assert.That(game.Session,Is.Null);Assert.That(store.Data.Attempt.AttemptId,Is.EqualTo(id));
                store.Fail=false;game.ContinueFromHome();Assert.That(game.LevelIndex,Is.EqualTo(9));Assert.That(game.Session.SessionId,Is.Not.EqualTo(id));Assert.That(game.Session.Board.ShipCount,Is.EqualTo(2));
            }
            finally{store.Fail=false;UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator MissingOrBrokenNextLevelKeepsHomeAndSavedRewards()
        {
            var available=false;var broken=false;
            var catalog=new PlayableLevelCatalog(Enumerable.Range(1,100).Select(n=>"Nav_"+n),id=>id!="Nav_11"||available,id=>{if(broken)throw new IOException();return Level(id);});
            var store=Won(10);var game=Create(store,catalog);
            try
            {
                var writes=store.Writes;game.ContinueFromHome();Assert.That(game.IsHomeOpen,Is.True);Assert.That(store.Writes,Is.EqualTo(writes));
                available=true;broken=true;game.ContinueFromHome();Assert.That(game.IsHomeOpen,Is.True);Assert.That(store.Writes,Is.EqualTo(writes));
                broken=false;game.ContinueFromHome();Assert.That(game.LevelIndex,Is.EqualTo(10));Assert.That(game.IsResultOpen,Is.False);
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FirstBlueRemainsClaimableWithoutHijackingDirectHomeStart()
        {
            var game=Create(Won(2));
            try
            {Assert.That(game.SaveService.CanClaimFirstBlue,Is.True);game.ContinueFromHome();Assert.That(game.LevelIndex,Is.EqualTo(2));Assert.That(game.IsCollectionOpen,Is.False);Assert.That(game.IsHomeOpen,Is.False);Assert.That(game.SaveService.CanClaimFirstBlue,Is.True);}
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FinishedCampaignDoesNotReopenReceiptOrInventLevel101()
        {
            var store=Won(100);var game=Create(store,Catalog(100));
            try
            {var writes=store.Writes;game.ContinueFromHome();Assert.That(game.IsHomeOpen,Is.True);Assert.That(game.Session,Is.Null);Assert.That(store.Writes,Is.EqualTo(writes));Assert.That(game.GetComponentsInChildren<Button>().Single(b=>b.name=="UI_Button_Main").interactable,Is.False);}
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator UnavailableAccountAllowsPracticeWithoutWritingOverSave()
        {
            var store=new Store{Corrupt=true};var game=Create(store);
            try{game.ContinueFromHome();Assert.That(game.Session.Board.ShipCount,Is.EqualTo(2));game.ReturnHome();Assert.That(game.IsHomeOpen,Is.True);Assert.That(store.Writes,Is.Zero);}
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
    }
}
