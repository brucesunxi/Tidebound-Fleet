using System;
using System.Collections.Generic;
using System.Linq;

namespace Tidebound.Collection
{
    public enum ClearRewardKind { Showcase, Skin, Scene, Trail }
    public sealed class ClearReward
    {
        public string Id { get; }
        public string Name { get; }
        public string ChineseName { get; }
        public int Stars { get; }
        public ClearRewardKind Kind { get; }
        public ClearReward(string id,string name,string chineseName,int stars,ClearRewardKind kind)
        { Id=id;Name=name;ChineseName=chineseName;Stars=stars;Kind=kind; }
    }
    public sealed class ClearRewardMilestone
    {
        public int Stars { get; }
        public IReadOnlyList<ClearReward> Rewards { get; }
        public ClearReward Primary => Rewards[0];
        internal ClearRewardMilestone(int stars,IEnumerable<ClearReward> rewards)
        { Stars=stars;Rewards=Array.AsReadOnly(rewards.OrderBy(x=>x.Kind).ThenBy(x=>x.Id,StringComparer.Ordinal).ToArray()); }
    }
    /// <summary>Read-only projection of actual clear entitlements, never a second reward configuration.</summary>
    public static class ClearRewardMilestones
    {
        private static readonly IReadOnlyList<ClearRewardMilestone> all=Build();
        private static IReadOnlyList<ClearRewardMilestone> Build()
        {
            var ships=ShowcaseCatalog.All.Where(x=>x.SourceGroup==ShowcaseSourceGroup.Level && x.TargetClearLevel>0)
                .Select(x=>new ClearReward(x.Id,x.Name,x.ChineseName,x.TargetClearLevel,ClearRewardKind.Showcase));
            var other=AppearanceCatalog.All.Where(x=>x.Source==ShowcaseSourceGroup.Level && x.Requirement>0)
                .Select(x=>new ClearReward(x.Id,x.Name,x.ChineseName,x.Requirement,x.Kind==AppearanceKind.Skin?ClearRewardKind.Skin:x.Kind==AppearanceKind.Scene?ClearRewardKind.Scene:ClearRewardKind.Trail));
            return Array.AsReadOnly(ships.Concat(other).GroupBy(x=>x.Stars).OrderBy(x=>x.Key).Select(x=>new ClearRewardMilestone(x.Key,x)).ToArray());
        }
        public static IReadOnlyList<ClearRewardMilestone> Next(int stars,int publishedLevels)
        {
            if(stars<0 || publishedLevels<1)throw new ArgumentOutOfRangeException();
            return Array.AsReadOnly(all.Where(x=>x.Stars>stars && x.Stars<=publishedLevels).Take(3).ToArray());
        }
        public static ClearRewardMilestone At(int stars)=>all.FirstOrDefault(x=>x.Stars==stars);
        public static int Previous(int stars)=>all.Where(x=>x.Stars<=stars).LastOrDefault()?.Stars??0;
    }
}
