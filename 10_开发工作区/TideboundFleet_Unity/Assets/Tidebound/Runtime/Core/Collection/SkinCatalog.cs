using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Core;

namespace Tidebound.Collection
{
    public enum SkinRarity { Common, Uncommon, Rare, Epic, Legendary }
    public sealed class SkinDefinition
    {
        public string Id { get; }
        public string WorkingName { get; }
        public SkinRarity Rarity { get; }
        public int CollectionOrder { get; }
        public bool IsDefault => Id==FoundationLimits.DefaultStandardSkinId;
        internal SkinDefinition(string id,string name,SkinRarity rarity,int order)
        {Id=id;WorkingName=name;Rarity=rarity;CollectionOrder=order;}
    }
    /// <summary>Versioned first content pool. Names are placeholders; no combat or geometry attributes.</summary>
    public static class SkinCatalog
    {
        public const string Version="CollectionPoolV1";
        public static IReadOnlyList<SkinDefinition> All { get; }=Array.AsReadOnly(new[]{
            new SkinDefinition(FoundationLimits.DefaultStandardSkinId,"海风号",SkinRarity.Common,0),
            new SkinDefinition("TF_SKIN_COMMON_01","木壳号",SkinRarity.Common,1),
            new SkinDefinition("TF_SKIN_COMMON_02","珊瑚号",SkinRarity.Common,2),
            new SkinDefinition("TF_SKIN_COMMON_03","巡逻号",SkinRarity.Common,3),
            new SkinDefinition("TF_SKIN_COMMON_04","渔业号",SkinRarity.Common,4),
            new SkinDefinition("TF_SKIN_UNCOMMON_01","海军号",SkinRarity.Uncommon,5),
            new SkinDefinition("TF_SKIN_UNCOMMON_02","救援号",SkinRarity.Uncommon,6),
            new SkinDefinition("TF_SKIN_UNCOMMON_03","极地号",SkinRarity.Uncommon,7),
            new SkinDefinition("TF_SKIN_UNCOMMON_04","科研号",SkinRarity.Uncommon,8),
            new SkinDefinition("TF_SKIN_RARE_01","幽灵号",SkinRarity.Rare,9),
            new SkinDefinition("TF_SKIN_RARE_02","深海荧光号",SkinRarity.Rare,10),
            new SkinDefinition("TF_SKIN_RARE_03","皇家号",SkinRarity.Rare,11),
            new SkinDefinition("TF_SKIN_EPIC_01","黄金舰队号",SkinRarity.Epic,12),
            new SkinDefinition("TF_SKIN_EPIC_02","海盗王号",SkinRarity.Epic,13),
            new SkinDefinition("TF_SKIN_LEGENDARY_01","未来科技号",SkinRarity.Legendary,14),
            new SkinDefinition("TF_SKIN_LEGENDARY_02","潮汐神舟号",SkinRarity.Legendary,15)});
        public static SkinDefinition Find(string id)=>All.FirstOrDefault(s=>s.Id==id);
    }
    /// <summary>Historical baseline used to validate receipts. Candidate long-term pricing is not activated.</summary>
    public static class CollectionRules
    {
        public const string Version="CollectionBaselineV1";
        public static int SinglePrice(int uniqueDrawSkins)=>uniqueDrawSkins<4 ? 300 : uniqueDrawSkins<8 ? 450 : uniqueDrawSkins<12 ? 700 : 1000;
        public static int DuplicateTickets(SkinRarity rarity)=>new[]{10,20,50,120,300}[(int)rarity];
        public static int ExchangeTickets(SkinRarity rarity)=>new[]{60,150,400,1000,2500}[(int)rarity];
    }
}
