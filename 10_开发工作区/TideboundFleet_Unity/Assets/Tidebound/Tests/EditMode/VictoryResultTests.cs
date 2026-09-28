using System;
using NUnit.Framework;
using System.Linq;
using Tidebound.Collection;
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
        [TestCase(1)][TestCase(9)][TestCase(10)][TestCase(100)]
        public void StarsDeriveFromCommittedHistoryAndRepeatedViewsNeverGrant(int level)
        {
            var data=Cleared(level);var revision=data.Revision;var coins=data.Coins;
            for(var i=0;i<3;i++)
            {
                var result=VictoryResult.FromSaved(data.Copy(),100);
                Assert.That(result.TotalStars,Is.EqualTo(level));Assert.That(result.PreviousStars,Is.EqualTo(level-1));
                Assert.That(result.AwardedStars,Is.EqualTo(1));
            }
            Assert.That(data.Revision,Is.EqualTo(revision));Assert.That(data.Coins,Is.EqualTo(coins));
            Assert.That(data.Settlements.Count(x=>x.Kind=="Victory"),Is.EqualTo(level));
        }
        [Test]
        public void MilestonesMergeRealEntitlementsAndExcludeNonClearSources()
        {
            var next=ClearRewardMilestones.Next(9,100);
            Assert.That(next.Select(x=>x.Stars),Is.EqualTo(new[]{10,15,20}));
            Assert.That(next[0].Rewards.Select(x=>x.Id),Does.Contain("TF_SKIN_K06"));
            Assert.That(next[0].Rewards.Select(x=>x.Id),Does.Contain("TF_TRAIL_W02"));
            var expected=ShowcaseCatalog.All.Count(x=>x.SourceGroup==ShowcaseSourceGroup.Level&&x.TargetClearLevel==10)+
                AppearanceCatalog.All.Count(x=>x.Source==ShowcaseSourceGroup.Level&&x.Requirement==10);
            Assert.That(next[0].Rewards.Count,Is.EqualTo(expected));
            Assert.That(next.SelectMany(x=>x.Rewards).Any(x=>x.Id=="TF_SKIN_K12"),Is.False);
            Assert.That(VictoryResult.FromSaved(Cleared(9),100).NextRewardProgress,Is.EqualTo(.75f));
        }
        [Test]
        public void RewardPreviewRespectsInstalledBoundaryAndStopsAfterAllRewards()
        {
            Assert.That(ClearRewardMilestones.Next(9,10).Select(x=>x.Stars),Is.EqualTo(new[]{10}));
            Assert.That(ClearRewardMilestones.Next(99,100).Select(x=>x.Stars),Is.EqualTo(new[]{100}));
            Assert.That(ClearRewardMilestones.Next(100,100),Is.Empty);
            Assert.That(new PlayerSaveData().ClearStars,Is.Zero);
        }
        [Test]
        public void InvalidPublishedRangeAndUninstalledResultAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>VictoryResult.FromSaved(new PlayerSaveData(),0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>VictoryResult.FromSaved(new PlayerSaveData(),10001));
            Assert.Throws<ArgumentException>(()=>VictoryResult.FromSaved(Cleared(2),1));
        }
    }
}
