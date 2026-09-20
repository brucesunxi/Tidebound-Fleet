using System;
using System.IO;
using System.Linq;
using System.Globalization;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.Config;
using Tidebound.Combat;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Save;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class PlayerSaveTests
    {
        private sealed class Store : IPlayerSaveStore
        {
            private PlayerSaveData data;
            public bool Fail;
            public PlayerSaveData Load()=>data?.Copy();
            public void Save(PlayerSaveData value){if(Fail)throw new IOException();data=value.Copy();}
        }
        private static ShipPlacementData Ship(string id,int x,int y,ShipDirection d)=>LevelSolverTests.Ship(id,x,y,d);
        private static SavedGameRuntime Single(int level=1)=>new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,Ship("A",1,1,ShipDirection.Up)),level);
        private static SavedGameRuntime Blocked(bool gap=false)=>new SavedGameRuntime(ShipMovementSystemTests.CreateSession(6,4,
            Ship("A",0,0,ShipDirection.Right),Ship("B",gap?5:3,0,ShipDirection.Left),Ship("C",5,1,ShipDirection.Up)),1);
        private static void Move(SavedGameRuntime game,string id)
        {
            var request=game.Movement.TryBeginMove(id);Assert.That(request.IsAccepted,Is.True);
            if(request.Operation.Stage==ShipMoveStage.Traveling)game.Movement.CompleteTravel(request.Operation.OperationId);
            if(game.Movement.IsBusy)game.Movement.CompleteBlockedFeedback(request.Operation.OperationId);
        }
        private static void Advance(SavedGameRuntime game,double seconds=10){game.Transit.Advance(seconds);game.Combat.Advance();}
        [Test]
        public void AcceptedIntentRestoresExactlyOnceIncludingPartialMove()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var original=Blocked(true))
            {
                Assert.That(save.Start(original),Is.True);Assert.That(save.PrepareMove("A"),Is.True);
                var expected=original.Session.Board.QueryForwardPath("A").TargetTail;
                using(var restored=SavedGameRuntime.Restore(store.Load().Attempt))
                {
                    Assert.That(restored.Session.SessionId,Is.EqualTo(original.Session.SessionId));
                    Assert.That(restored.Session.Board.GetShip("A").Position,Is.EqualTo(expected));
                    Assert.That(restored.PendingMoveId,Is.Null);Assert.That(restored.Movement.IsBusy,Is.False);
                    var reload=new PlayerSaveService(store);reload.AttachRestored(restored);Assert.That(reload.Checkpoint(),Is.True);
                    using(var again=SavedGameRuntime.Restore(store.Load().Attempt))Assert.That(again.Session.Board.GetShip("A").Position,Is.EqualTo(expected));
                    Assert.That(reload.DailyRestartCount,Is.Zero);
                }
            }
        }
        [TestCase(.1)][TestCase(1.25)][TestCase(1.50)][TestCase(1.7)]
        public void LaneFifoProjectilesHitsAndPauseSurviveRestore(double at)
        {
            using(var game=new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,Ship("A",1,1,ShipDirection.Up),Ship("B",3,1,ShipDirection.Up)),1))
            {
                Move(game,"A");Move(game,"B");Advance(game,at);game.Movement.Pause();
                var snapshot=game.Capture();using(var restored=SavedGameRuntime.Restore(snapshot))
                {
                    Assert.That(restored.Session.State,Is.EqualTo(game.Session.State));Assert.That(restored.Transit.ElapsedTime,Is.EqualTo(at));
                    Assert.That(restored.Session.Boss.Hp,Is.EqualTo(game.Session.Boss.Hp));
                    CollectionAssert.AreEqual(game.Combat.Attacks.Select(t=>t.AttackId),restored.Combat.Attacks.Select(t=>t.AttackId));
                    CollectionAssert.AreEqual(game.Combat.Attacks.Select(t=>t.Stage),restored.Combat.Attacks.Select(t=>t.Stage));
                    Assert.That(restored.Transit.ActiveCount,Is.EqualTo(game.Transit.ActiveCount));
                    restored.Movement.Resume();Advance(restored);Assert.That(restored.Combat.HitCount,Is.EqualTo(2));Assert.That(restored.Combat.IsVictorious,Is.True);
                    Advance(restored);Assert.That(restored.PendingCoins,Is.EqualTo(2));
                }
            }
        }
        [Test]
        public void ExitIntentCannotDisappearOrAttackTwiceAfterReload()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var game=Single())
            {save.Start(game);save.PrepareMove("A");using(var resumed=SavedGameRuntime.Restore(store.Load().Attempt))
             {Assert.That(resumed.Session.Board.ShipCount,Is.Zero);Assert.That(resumed.Capture().Departures.Length,Is.EqualTo(1));Advance(resumed);Assert.That(resumed.Combat.HitCount,Is.EqualTo(1));}}
        }
        [Test]
        public void VictoryBanksOnceAdvancesCampaignAndRejectsOldLevel()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var first=Single())
            {
                save.Start(first);save.PrepareMove("A");Move(first,"A");Advance(first);
                Assert.That(save.Checkpoint(),Is.True);Assert.That(save.Coins,Is.EqualTo(101));Assert.That(save.CurrentLevel,Is.EqualTo(2));
                save.Checkpoint(true);Assert.That(save.Coins,Is.EqualTo(101));
                var loaded=new PlayerSaveService(store);Assert.That(loaded.IsAvailable,Is.True);
                using(var restored=SavedGameRuntime.Restore(loaded.Snapshot.Attempt))
                {loaded.AttachRestored(restored);loaded.Checkpoint(true);Assert.That(loaded.Coins,Is.EqualTo(101));}
                using(var old=Single())Assert.That(save.Start(old),Is.False);
                using(var second=Single(2))
                {Assert.That(save.Start(second),Is.True);Move(second,"A");Advance(second);save.Checkpoint();Assert.That(save.Coins,Is.EqualTo(222));}
                Assert.That(save.Snapshot.Settlements.Length,Is.EqualTo(2));
            }
        }
        [Test]
        public void FailedVictoryWriteCanRetryWithoutLosingOrDuplicatingCoins()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var game=Single())
            {
                save.Start(game);Move(game,"A");Advance(game);store.Fail=true;
                Assert.That(save.Checkpoint(),Is.False);Assert.That(save.Coins,Is.Zero);Assert.That(save.CurrentLevel,Is.EqualTo(1));
                store.Fail=false;Assert.That(save.Checkpoint(),Is.True);Assert.That(save.Coins,Is.EqualTo(101));
                save.Checkpoint(true);Assert.That(save.Coins,Is.EqualTo(101));
            }
        }
        [Test]
        public void VoluntaryRestartLosesPendingCoinsButKeepsToolsAndCountsOnce()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var game=Blocked(true))using(var next=Blocked(true))
            {
                save.Start(game);save.Inventory.Grant("test",1,1,1);Move(game,"C");Advance(game);save.Checkpoint();
                Assert.That(game.PendingCoins,Is.EqualTo(1));Assert.That(game.IsTerminalDeadlock,Is.False);
                Assert.That(save.Restart(next),Is.True);Assert.That(save.Coins,Is.Zero);Assert.That(save.DailyRestartCount,Is.EqualTo(1));
                Assert.That(save.Restart(next),Is.False);Assert.That(save.Inventory.Count(ShipTool.Rescue),Is.EqualTo(1));
                Assert.That(save.Snapshot.Settlements.Single().Kind,Is.EqualTo("VoluntaryRestart"));
            }
        }
        [Test]
        public void DeadlockQuotaIncludesAllRestartsAndDoesNotResetOnClockRollback()
        {
            var date=new DateTime(2026,9,20);var store=new Store();var save=new PlayerSaveService(store,today:()=>date);
            var game=Blocked();save.Start(game);
            try
            {
                for(var i=1;i<=11;i++)
                {
                    Move(game,"C");Advance(game);Assert.That(game.IsTerminalDeadlock,Is.True);
                    var next=Blocked();Assert.That(save.Restart(next),Is.True);game.Dispose();game=next;
                    Assert.That(save.Coins,Is.EqualTo(Math.Min(i,10)));
                }
                date=date.AddDays(-1);Move(game,"C");Advance(game);var back=Blocked();Assert.That(save.Restart(back),Is.True);game.Dispose();game=back;
                Assert.That(save.DailyRestartCount,Is.EqualTo(12));Assert.That(save.Coins,Is.EqualTo(10));
                date=new DateTime(2026,9,21);Move(game,"C");Advance(game);var forward=Blocked();Assert.That(save.Restart(forward),Is.True);game.Dispose();game=forward;
                Assert.That(save.DailyRestartCount,Is.EqualTo(1));Assert.That(save.Coins,Is.EqualTo(11));
                Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);
            }
            finally {game.Dispose();}
        }
        [Test]
        public void OrdinaryRestartsConsumeQuotaButVictoryStillPaysFullReward()
        {
            var date=new DateTime(2026,9,20);var store=new Store();var save=new PlayerSaveService(store,today:()=>date);
            var game=Blocked();save.Start(game);
            try
            {
                for(var i=0;i<10;i++){var next=Blocked();Assert.That(save.Restart(next),Is.True);game.Dispose();game=next;}
                Move(game,"C");Advance(game);Assert.That(save.RestartReward,Is.Zero);
                date=date.AddDays(1);Assert.That(save.RestartReward,Is.EqualTo(1));date=date.AddDays(-1);
                save.Inventory.Grant("test",1,0,0);
                using(var tools=new ShipToolSystem(game.Session,game.Movement,save.Inventory))Assert.That(tools.Rescue(),Is.EqualTo(ToolUseStatus.Applied));
                Advance(game);Assert.That(save.Checkpoint(),Is.True);Assert.That(save.Coins,Is.EqualTo(103));
                Assert.That(save.DailyRestartCount,Is.EqualTo(10));Assert.That(save.Snapshot.Settlements.Length,Is.EqualTo(11));
            }
            finally {game.Dispose();}
        }
        [Test]
        public void PendingMoveIsNeverEligibleForDeadlockAndFailedRestartKeepsEntireOldAttempt()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var game=Blocked())using(var next=Blocked())
            {
                save.Start(game);Move(game,"C");Advance(game);save.Checkpoint();
                save.PrepareMove("A");Assert.That(game.IsTerminalDeadlock,Is.False);
                store.Fail=true;Assert.That(save.Restart(next),Is.False);Assert.That(save.Runtime,Is.SameAs(game));Assert.That(save.DailyRestartCount,Is.Zero);
                Assert.That(save.Coins,Is.Zero);Assert.That(store.Load().Attempt.AttemptId,Is.EqualTo(game.Session.SessionId));
            }
        }
        [TestCase(ShipTool.Rescue)][TestCase(ShipTool.Reverse)][TestCase(ShipTool.Shuffle)]
        public void ToolStockAndEffectPersistTogetherAndFailedWritesApplyNeither(ShipTool kind)
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var game=Single())
            {
                save.Start(game);save.Inventory.Grant("test",1,1,1);
                using(var tools=new ShipToolSystem(game.Session,game.Movement,save.Inventory,seed:7))
                {
                    Func<ToolUseStatus> apply=()=>{if(kind==ShipTool.Reverse){tools.Select(kind);return tools.UseSelected("A");}return kind==ShipTool.Rescue?tools.Rescue():tools.Shuffle();};
                    store.Fail=true;var original=game.Session.Board;
                    Assert.That(apply(),Is.EqualTo(ToolUseStatus.StorageUnavailable));Assert.That(game.Session.Board,Is.SameAs(original));Assert.That(save.Inventory.Count(kind),Is.EqualTo(1));
                    tools.CancelSelection();store.Fail=false;Assert.That(apply(),Is.EqualTo(ToolUseStatus.Applied));
                    // No normal post-effect checkpoint: reload the transaction written BEFORE the in-memory effect.
                    var loaded=new PlayerSaveService(store);Assert.That(loaded.IsAvailable,Is.True);Assert.That(loaded.Inventory.Count(kind),Is.Zero);
                    using(var restored=SavedGameRuntime.Restore(loaded.Snapshot.Attempt))
                    {Assert.That(LevelStateIdentity.Fingerprint(restored.Session.Board),Is.EqualTo(LevelStateIdentity.Fingerprint(game.Session.Board)));
                     if(kind==ShipTool.Rescue){Advance(restored);Assert.That(restored.Combat.HitCount,Is.EqualTo(1));}}
                }
            }
        }
        [Test]
        public void RescueCrashBetweenDurableCommitAndApplyingEffectRestoresBothShips()
        {
            var store=new Store();var save=new PlayerSaveService(store);
            using(var game=Blocked())
            {
                save.Start(game);save.Inventory.Grant("test",1,0,0);var ids=new[]{"A","B"};
                var after=game.Session.Board.WithoutShip("A").WithoutShip("B");var data=save.Snapshot.Tools;data.Rescue--;
                ((IToolMutationStore)save).SaveToolMutation(data,new ToolMutation(after,game.Movement.PlanRescue(ids)));
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(3));
                var loaded=new PlayerSaveService(store);Assert.That(loaded.Inventory.Count(ShipTool.Rescue),Is.Zero);
                using(var restored=SavedGameRuntime.Restore(loaded.Snapshot.Attempt))
                {Assert.That(restored.Session.Board.ShipCount,Is.EqualTo(1));Advance(restored);Assert.That(restored.Combat.HitCount,Is.EqualTo(2));Assert.That(restored.Capture().Departures.Select(d=>d.Sequence),Is.EqualTo(new long[]{1,2}));}
            }
        }
        [Test]
        public void LegacyInventoryMigratesOnceWithoutReissuingSpentGifts()
        {
            var legacy=new MemoryToolInventoryStore();var stock=new ToolInventory(legacy);stock.ReachLevel(3);stock.TrySpend(ShipTool.Rescue);
            var store=new Store();var save=new PlayerSaveService(store,legacy);Assert.That(save.Snapshot.ImportedToolInventory,Is.True);
            Assert.That(save.Inventory.Count(ShipTool.Rescue),Is.Zero);stock.Grant("later",5,5,5);
            save=new PlayerSaveService(store,legacy);save.Inventory.ReachLevel(3);Assert.That(save.Inventory.Count(ShipTool.Rescue),Is.Zero);
            Assert.That(save.Inventory.Count(ShipTool.Shuffle),Is.EqualTo(1));
        }
        [Test]
        public void AtomicFileWriteKeepsPriorSaveAndCorruptionNeverOverwritesOrReimports()
        {
            var dir=Path.Combine(Path.GetTempPath(),"TideboundSaveTest_"+Guid.NewGuid().ToString("N"));var path=Path.Combine(dir,"save.json");
            try
            {
                var store=new PlayerSaveFileStore(path);var service=new PlayerSaveService(store);
                using(var game=Single())
                {
                    service.Start(game);service.Inventory.Grant("first",1,0,0);var before=File.ReadAllText(path);
                    var fail=new PlayerSaveFileStore(path,()=>throw new IOException());Assert.Throws<IOException>(()=>fail.Save(service.Snapshot));
                    Assert.That(File.ReadAllText(path),Is.EqualTo(before));Assert.That(File.Exists(path+".tmp"),Is.False);
                    Assert.That(new PlayerSaveService(store).IsAvailable,Is.True);
                    File.WriteAllText(path,"corrupt");var broken=new PlayerSaveService(store);Assert.That(broken.IsAvailable,Is.False);Assert.That(broken.Inventory.IsAvailable,Is.False);
                    Assert.That(broken.Start(game),Is.False);Assert.That(File.ReadAllText(path),Is.EqualTo("corrupt"));
                    File.Delete(path);Assert.That(new PlayerSaveService(store).IsAvailable,Is.False,"A missing main with a backup must not become a new account.");
                }
            }
            finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
        }
        [Test]
        public void MalformedProfileAndInconsistentCombatCheckpointAreRejected()
        {
            using(var game=Single())
            {
                var save=game.Capture();save.Board=Array.Empty<SavedPlacement>();Assert.Throws<ArgumentException>(()=>SavedGameRuntime.Validate(save));
                Move(game,"A");Advance(game);save=game.Capture();save.HitIds=Array.Empty<string>();save.Victory=false;
                Assert.Throws<ArgumentException>(()=>SavedGameRuntime.Restore(save));
                var profile=new PlayerSaveData{Coins=1};Assert.Throws<ArgumentException>(()=>profile.Validate());
                profile=new PlayerSaveData{Version=9};Assert.Throws<ArgumentException>(()=>profile.Validate());
            }
        }
        [Test]
        public void CoinProbabilityCapsAndRewardsAreStableAcrossCultureAndOrder()
        {
            Assert.That(BattleCoinRules.Probability(10,10),Is.EqualTo(.1).Within(1e-12));
            Assert.That(BattleCoinRules.Probability(1,10),Is.EqualTo(.8).Within(1e-12));
            Assert.That(BattleCoinRules.Probability(5,10),Is.EqualTo(.5375).Within(1e-12));
            var ships=Enumerable.Range(0,10).Select(i=>new SavedShip{Id="S"+i,SkinId="Skin"+i,Length=2,CoinCap=8}).ToArray();
            var original=CultureInfo.CurrentCulture;
            try
            {
                foreach(var cap in new[]{1,2,3,5,8})
                {
                    ships[0].CoinCap=cap;var values=Enumerable.Range(0,100).Select(i=>BattleCoinRules.Calculate("seed"+i,ships[0],ships)).ToArray();
                    Assert.That(values.All(x=>x>=1 && x<=cap),Is.True);
                    if(cap>1)Assert.That(values.Distinct().Count(),Is.GreaterThan(1));
                    CultureInfo.CurrentCulture=new CultureInfo("ar-SA");
                    CollectionAssert.AreEqual(values,Enumerable.Range(0,100).Select(i=>BattleCoinRules.Calculate("seed"+i,ships[0],ships.Reverse().ToArray())));
                }
                ships[0].Length=3;Assert.That(BattleCoinRules.Calculate("x",ships[0],ships),Is.EqualTo(1));
            }
            finally {CultureInfo.CurrentCulture=original;}
        }
    }
}
