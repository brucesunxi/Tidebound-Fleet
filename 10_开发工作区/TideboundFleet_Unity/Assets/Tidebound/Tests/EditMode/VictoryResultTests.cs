using System;
using NUnit.Framework;
using Tidebound.Save;
using Tidebound.Ship;

namespace Tidebound.Tests
{
    public sealed class VictoryResultTests
    {
        private static SavedGameRuntime Single(int level)=>new SavedGameRuntime(ShipMovementSystemTests.CreateSession(8,8,
            LevelSolverTests.Ship("A",1,1,ShipDirection.Up)),level);
        private static void Win(SavedGameRuntime runtime)
        {var op=runtime.Movement.TryBeginMove("A");runtime.Movement.CompleteTravel(op.Operation.OperationId);runtime.Transit.Advance(10);runtime.Combat.Advance();}
        private static PlayerSaveData Cleared(int levels)
        {
            var store=new MemoryPlayerSaveStore();var service=new PlayerSaveService(store);
            for(var n=1;n<=levels;n++)using(var game=Single(n)){Assert.That(service.Start(game),Is.True);Win(game);Assert.That(service.Checkpoint(),Is.True);}
            return store.Load();
        }
        [Test]
        public void OnlyDurableVictoryReceiptCanCreateRewardDisplay()
        {
            var store=new MemoryPlayerSaveStore();var service=new PlayerSaveService(store);
            using(var game=Single(1))
            {
                service.Start(game);Assert.That(VictoryResult.FromSaved(service.Snapshot,10),Is.Null);Win(game);
                var pending=service.Snapshot;pending.Attempt=game.Capture();Assert.That(VictoryResult.FromSaved(pending,10),Is.Null);
                service.Checkpoint();var snapshot=service.Snapshot;var revision=snapshot.Revision;
                var result=VictoryResult.FromSaved(snapshot,10);Assert.That(result.FirstClearCoins,Is.EqualTo(100));Assert.That(result.BattleCoins,Is.EqualTo(1));
                Assert.That(result.TotalCoins,Is.EqualTo(101));Assert.That(result.AttemptId,Is.EqualTo(game.Session.SessionId));
                snapshot.Settlements[0].BattleCoins=999;Assert.That(result.BattleCoins,Is.EqualTo(1));Assert.That(service.Snapshot.Revision,Is.EqualTo(revision));
            }
        }
        [TestCase(1,10,1,1,10,true)][TestCase(9,10,1,9,10,true)][TestCase(10,10,1,10,10,false)]
        [TestCase(10,20,1,10,10,true)][TestCase(11,12,2,1,2,true)][TestCase(12,12,2,2,2,false)]
        [TestCase(10,500,1,10,10,true)]
        public void ChapterTrackUsesInstalledContentAndActualCompletedLevel(int level,int published,int chapter,int completed,int size,bool next)
        {
            var result=VictoryResult.FromSaved(Cleared(level),published);
            Assert.That(result.ChapterNumber,Is.EqualTo(chapter));Assert.That(result.ChapterCompleted,Is.EqualTo(completed));Assert.That(result.ChapterSize,Is.EqualTo(size));
            Assert.That(result.HasNext,Is.EqualTo(next));Assert.That(result.NextLevel,Is.EqualTo(level+1));
            Assert.That(result.Progress,Is.EqualTo((float)completed/size));Assert.That(result.PreviousProgress,Is.EqualTo((completed-1f)/size));
        }
        [Test]
        public void CollectionCheckpointIsMetadataAndDoesNotGrantOrCreateAnything()
        {var data=Cleared(2);var coins=data.Coins;var result=VictoryResult.FromSaved(data,10);Assert.That(result.IsCollectionCheckpoint,Is.True);Assert.That(data.Coins,Is.EqualTo(coins));Assert.That(data.Attempt.LevelNumber,Is.EqualTo(2));}
        [Test]
        public void InvalidPublishedRangeAndUninstalledResultAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>VictoryResult.FromSaved(new PlayerSaveData(),0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>VictoryResult.FromSaved(new PlayerSaveData(),10001));
            Assert.Throws<ArgumentException>(()=>VictoryResult.FromSaved(Cleared(2),1));
        }
    }
}
