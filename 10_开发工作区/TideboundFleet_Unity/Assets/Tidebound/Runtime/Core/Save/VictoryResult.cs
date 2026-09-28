using System;
using System.Linq;
using System.Collections.Generic;
using Tidebound.Collection;

namespace Tidebound.Save
{
    /// <summary>Immutable display projection of a durable receipt; never grants rewards or starts attempts.</summary>
    public sealed class VictoryResult
    {
        public string AttemptId { get; }
        public int LevelNumber { get; }
        public int BattleCoins { get; }
        public int FirstClearCoins { get; }
        public long TotalCoins => (long)BattleCoins + FirstClearCoins;
        public int PublishedLevels { get; }
        public int TotalStars { get; }
        // This is the star on this unique committed victory receipt, not a grant triggered by the view.
        public int AwardedStars => 1;
        public int PreviousStars => TotalStars-AwardedStars;
        public IReadOnlyList<ClearRewardMilestone> NextRewards { get; }
        public ClearRewardMilestone UnlockedRewards => ClearRewardMilestones.At(LevelNumber);
        public float NextRewardProgress => NextRewards.Count==0?1f:
            (TotalStars-ClearRewardMilestones.Previous(TotalStars))/(float)(NextRewards[0].Stars-ClearRewardMilestones.Previous(TotalStars));
        public bool HasNext => LevelNumber < PublishedLevels;
        public int NextLevel => LevelNumber + 1;
        public int ChapterNumber => (LevelNumber - 1) / 10 + 1;
        public int ChapterStart => (ChapterNumber - 1) * 10 + 1;
        public int ChapterSize => Math.Min(10, PublishedLevels - ChapterStart + 1);
        public int ChapterCompleted => LevelNumber - ChapterStart + 1;
        public float PreviousProgress => (ChapterCompleted - 1f) / ChapterSize;
        public float Progress => (float)ChapterCompleted / ChapterSize;
        public bool IsCollectionCheckpoint => LevelNumber == 2;
        private VictoryResult(SettlementRecord receipt,int publishedLevels,int stars)
        {
            AttemptId=receipt.AttemptId;LevelNumber=receipt.LevelNumber;BattleCoins=receipt.BattleCoins;
            FirstClearCoins=receipt.FirstClearCoins;PublishedLevels=publishedLevels;
            TotalStars=stars;NextRewards=ClearRewardMilestones.Next(stars,publishedLevels);
        }
        public static VictoryResult FromSaved(PlayerSaveData data,int publishedLevels)
        {
            if(data==null)throw new ArgumentNullException(nameof(data));
            if(publishedLevels<1 || publishedLevels>10000)throw new ArgumentOutOfRangeException(nameof(publishedLevels));
            data.Validate();
            var attempt=data.Attempt;
            if(attempt==null || !attempt.Victory)return null;
            var receipt=data.Settlements.SingleOrDefault(s=>s.AttemptId==attempt.AttemptId && s.Kind=="Victory");
            if(receipt==null)return null;
            if(receipt.LevelNumber>publishedLevels)throw new ArgumentException("Result is outside installed content.");
            return new VictoryResult(receipt,publishedLevels,data.ClearStars);
        }
    }
}
