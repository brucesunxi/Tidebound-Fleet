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
        public IEnumerator BrowsingNeverStartsAttemptAndHomeResumeKeepsIdentityAndPauseOwnership()
        {
            var store = new Store(); var game = Create(store);
            try
            {
                Assert.That(game.IsHomeOpen, Is.True); Assert.That(game.Session, Is.Null); Assert.That(store.Data?.Attempt, Is.Null);
                var writes = store.Writes; game.OpenCollection(); game.OpenShop(); yield return null;
                game.CloseAcquisition(); Assert.That(store.Data?.Attempt, Is.Null); Assert.That(store.Writes, Is.EqualTo(writes));
                game.ContinueFromHome(); var id = game.Session.SessionId;
                Assert.That(game.IsHomeOpen, Is.False); game.ReturnHome();
                Assert.That(game.IsHomeOpen, Is.True); Assert.That(store.Data.Attempt.Paused, Is.False);
                game.Restart(); game.ClickShip("A"); game.SelectTool(ShipTool.Rescue); yield return null;
                Assert.That(game.Session.SessionId, Is.EqualTo(id)); Assert.That(game.Session.Board.ShipCount, Is.EqualTo(2));
                game.ContinueFromHome(); Assert.That(game.IsPaused, Is.False); Assert.That(game.Session.SessionId, Is.EqualTo(id));
                game.TogglePause(); game.ReturnHome(); game.ContinueFromHome();
                Assert.That(game.IsPaused, Is.True, "An explicit player pause is preserved.");
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator SuppliesDoNotResetSpentUsesAndRestartingAppFromHomeRestoresSameAttempt()
        {
            var store = new Store { Data = Before(3) }; var seed = new PlayerSaveService(store);
            seed.Collect(Guid.NewGuid().ToString("N"), "FirstBlue");
            var game = Create(store);
            try
            {
                game.ContinueFromHome(); game.SelectTool(ShipTool.Reverse); game.ClickShip("A");
                Assert.That(game.Tools.UsesLeft, Is.EqualTo(4)); var id = game.Session.SessionId;
                game.ToggleMenu(); game.ReturnHome(); Assert.That(game.IsHomeOpen, Is.True);
                game.OpenShop(); game.ShopPanel.SelectProduct("rescue_1"); game.ShopPanel.ConfirmPurchase(); game.ShopPanel.ConfirmPurchase(); game.CloseAcquisition();
                Assert.That(game.Tools.UsesLeft, Is.EqualTo(4)); Assert.That(store.Data.Attempt.ToolUses, Is.EqualTo(1));
                Assert.That(store.Data.Attempt.Paused, Is.False); Assert.That(store.Data.Purchases.Length, Is.EqualTo(1));
                UnityEngine.Object.Destroy(game.gameObject); yield return null;
                game = Create(store); Assert.That(game.Session, Is.Null); game.ContinueFromHome();
                Assert.That(game.Session.SessionId, Is.EqualTo(id)); Assert.That(game.Tools.UsesLeft, Is.EqualTo(4));
                Assert.That(game.IsResumingEntry, Is.True); Assert.That(game.IsPaused, Is.False);
                Assert.That(game.Session.GetShip("A").Direction, Is.EqualTo(ShipDirection.Down));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator ReturningHomeFreezesOutstandingLaneWorkUntilExplicitContinue()
        {
            var store = new Store(); var game = Create(store);
            try
            {
                game.ContinueFromHome(); game.ClickShip("A");
                var deadline = Time.realtimeSinceStartup + 8;
                while (game.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(game.IsBusy, Is.False); Assert.That(game.SaveService.Runtime.Transit.ActiveCount, Is.GreaterThan(0));
                game.ReturnHome(); var elapsed = game.SaveService.Runtime.Transit.ElapsedTime; var id = game.Session.SessionId;
                yield return new WaitForSecondsRealtime(.2f);
                Assert.That(game.SaveService.Runtime.Transit.ElapsedTime, Is.EqualTo(elapsed));
                Assert.That(game.Session.SessionId, Is.EqualTo(id)); game.ContinueFromHome(); yield return null; yield return null;
                Assert.That(game.SaveService.Runtime.Transit.ElapsedTime, Is.GreaterThan(elapsed));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator FailedHomeCheckpointStaysInGameWithoutReplacingAttempt()
        {
            var store = new Store(); var game = Create(store);
            try
            {
                game.ContinueFromHome(); var id = game.Session.SessionId; store.Fail = true;
                game.ReturnHome(); Assert.That(game.IsHomeOpen, Is.False); Assert.That(game.Session.SessionId, Is.EqualTo(id));
                store.Fail = false; game.ReturnHome(); Assert.That(game.IsHomeOpen, Is.True);
                game.ContinueFromHome(); Assert.That(game.Session.SessionId, Is.EqualTo(id));
            }
            finally { store.Fail = false; UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator LevelTenIsAnOrdinaryResultWithUnavailableNextAndNoChapterStatistics()
        {
            var store = Won(10); var game = Create(store);
            try
            {
                game.ContinueFromHome(); Assert.That(game.IsResultReadable, Is.True); Assert.That(game.IsHomeOpen, Is.False);
                var id = game.Session.SessionId; var coins = game.SaveService.Coins; var writes = store.Writes;
                game.ContinueFromResult(); game.ContinueFromResult(); yield return null;
                Assert.That(game.IsResultReadable, Is.True); Assert.That(game.IsHomeOpen, Is.False);
                Assert.That(game.Session.SessionId, Is.EqualTo(id)); Assert.That(game.SaveService.Coins, Is.EqualTo(coins));
                Assert.That(store.Writes, Is.EqualTo(writes));
                Assert.That(game.ResultView.transform.Find("NextLevel").GetComponent<Button>().interactable, Is.True);
                Assert.That(game.ResultView.transform.Find("Chapter").gameObject.activeSelf, Is.False);
                Assert.That(game.ResultView.transform.Find("ProgressTrack").gameObject.activeSelf, Is.False);
                Assert.That(game.ResultView.transform.Find("ResultSubtitle").GetComponent<Text>().text, Does.Contain("not available"));
                game.ReturnHome(); Assert.That(game.IsHomeOpen, Is.True); game.ContinueFromHome();
                Assert.That(game.IsResultReadable, Is.True); Assert.That(game.Session.SessionId, Is.EqualTo(id));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator FiveHundredEntryCatalogAllowsElevenOnlyWhenItsContentIsAvailable()
        {
            var reads = new List<string>(); var available = false; var broken = false;
            var catalog = new PlayableLevelCatalog(Enumerable.Range(1, 500).Select(n => "Nav_" + n),
                id => id != "Nav_11" || available, id => { reads.Add(id); if (id == "Nav_11" && broken) throw new IOException(); return Level(id); });
            var store = Won(10); var game = Create(store, catalog);
            try
            {
                Assert.That(reads, Is.Empty); game.ContinueFromHome(); Assert.That(reads, Is.EqualTo(new[] { "Nav_10" }));
                var id = game.Session.SessionId; game.ContinueFromResult(); Assert.That(game.Session.SessionId, Is.EqualTo(id));
                available = true; broken = true; game.ContinueFromResult(); Assert.That(game.IsResultReadable, Is.True);
                Assert.That(game.Session.SessionId, Is.EqualTo(id)); broken = false; game.ContinueFromResult();
                Assert.That(game.LevelIndex, Is.EqualTo(10)); Assert.That(game.Session.LevelId, Is.EqualTo("Nav_11"));
                var next = game.Session.SessionId; game.ContinueFromResult(); Assert.That(game.Session.SessionId, Is.EqualTo(next));
                Assert.That(store.Data.Settlements.Length, Is.EqualTo(10));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator FirstBlueIsClaimedAtHomeBeforeCreatingLevelThreeWithoutAutoEquipment()
        {
            var store = Won(2); var game = Create(store);
            try
            {
                game.ContinueFromHome(); var id = game.Session.SessionId; var equipment = game.SaveService.Snapshot.Collection.Equipment;
                game.ContinueFromResult(); Assert.That(game.IsHomeOpen, Is.True); Assert.That(game.IsCollectionOpen, Is.True);
                Assert.That(game.Session.SessionId, Is.EqualTo(id)); Assert.That(store.Data.Attempt.LevelNumber, Is.EqualTo(2));
                game.CollectionView.SelectTransaction("FirstBlue"); game.CollectionView.ConfirmTransaction(); game.CollectionView.ConfirmTransaction();
                Assert.That(game.SaveService.CanClaimFirstBlue, Is.False); Assert.That(game.SaveService.Snapshot.Collection.Equipment, Is.EqualTo(equipment));
                game.CloseCollection(); game.ContinueFromHome(); game.ContinueFromResult();
                Assert.That(game.LevelIndex, Is.EqualTo(2)); Assert.That(store.Data.Collection.Receipts.Length, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator UnavailableAccountStillAllowsExplicitPracticeWithoutWritingOverSave()
        {
            var store = new Store { Corrupt = true }; var game = Create(store);
            try
            {
                Assert.That(game.IsHomeOpen, Is.True); Assert.That(game.SaveService.IsAvailable, Is.False);
                game.ContinueFromHome(); Assert.That(game.Session.Board.ShipCount, Is.EqualTo(2));
                Assert.That(game.IsHomeOpen, Is.False); game.ReturnHome(); Assert.That(game.IsHomeOpen, Is.True);
                Assert.That(store.Writes, Is.Zero);
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
        [UnityTest]
        public IEnumerator MissingSavedLevelStaysHomeAndPreservesStoredAttempt()
        {
            var store = Won(10); var id = store.Data.Attempt.AttemptId; var game = Create(store, Catalog(9));
            try
            {
                var writes = store.Writes; game.ContinueFromHome();
                Assert.That(game.IsHomeOpen, Is.True); Assert.That(game.Session, Is.Null);
                Assert.That(store.Data.Attempt.AttemptId, Is.EqualTo(id)); Assert.That(store.Writes, Is.EqualTo(writes));
                Assert.That(game.HomeNotice, Does.Contain("not available"));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); } yield return null;
        }
    }
}
