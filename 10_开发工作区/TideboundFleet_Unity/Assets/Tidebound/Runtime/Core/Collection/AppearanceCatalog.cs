using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Core;

namespace Tidebound.Collection
{
    public enum AppearanceKind { Skin, Trail, Scene }
    public sealed class AppearanceDefinition
    {
        public string Id {get;} public AppearanceKind Kind {get;} public string Name {get;} public string ChineseName {get;}
        public ShowcaseSourceGroup? Source {get;} public int Requirement {get;} public SkinRarity Rarity {get;}
        public AppearanceDefinition(string id,AppearanceKind kind,string name,string chinese,ShowcaseSourceGroup? source,int requirement,int rarity=0)
        {Id=id;Kind=kind;Name=name;ChineseName=chinese;Source=source;Requirement=requirement;Rarity=(SkinRarity)rarity;}
    }
    /// <summary>Approved artwork identities. Historical CollectionPoolV1 remains immutable for receipt replay.</summary>
    public static class AppearanceCatalog
    {
        public const string DefaultSkin="TF_SKIN_K01",DefaultTrail="TF_TRAIL_W01",DefaultScene="TF_SCENE_S01";
        public static IReadOnlyList<AppearanceDefinition> All {get;}=Array.AsReadOnly(new[]{
            new AppearanceDefinition("TF_SKIN_K01",AppearanceKind.Skin,"Sea Breeze","海风号",ShowcaseSourceGroup.Initial,0,0),
            new AppearanceDefinition("TF_SKIN_K02",AppearanceKind.Skin,"Red Racer","赤浪号",ShowcaseSourceGroup.Level,5,0),
            new AppearanceDefinition("TF_SKIN_K03",AppearanceKind.Skin,"Voyager","远航号",ShowcaseSourceGroup.Draw,0,0),
            new AppearanceDefinition("TF_SKIN_K04",AppearanceKind.Skin,"Black Sail","黑帆号",ShowcaseSourceGroup.Draw,0,3),
            new AppearanceDefinition("TF_SKIN_K05",AppearanceKind.Skin,"Harbor Tug","港湾号",ShowcaseSourceGroup.Level,3,0),
            new AppearanceDefinition("TF_SKIN_K06",AppearanceKind.Skin,"Rescue","救援号",ShowcaseSourceGroup.Level,10,1),
            new AppearanceDefinition("TF_SKIN_K07",AppearanceKind.Skin,"Gold Diver","金潜号",ShowcaseSourceGroup.Level,50,1),
            new AppearanceDefinition("TF_SKIN_K08",AppearanceKind.Skin,"Gale Jet","疾风号",ShowcaseSourceGroup.Level,20,1),
            new AppearanceDefinition("TF_SKIN_K09",AppearanceKind.Skin,"Red Paddle","红轮号",ShowcaseSourceGroup.Draw,0,0),
            new AppearanceDefinition("TF_SKIN_K10",AppearanceKind.Skin,"Steamworks","蒸汽号",ShowcaseSourceGroup.Level,40,2),
            new AppearanceDefinition("TF_SKIN_K11",AppearanceKind.Skin,"Silver Shark","银鲨号",ShowcaseSourceGroup.Draw,0,1),
            new AppearanceDefinition("TF_SKIN_K12",AppearanceKind.Skin,"Ducky","萌鸭号",ShowcaseSourceGroup.Share,1,1),
            new AppearanceDefinition("TF_SKIN_K13",AppearanceKind.Skin,"Strawberry","草莓号",ShowcaseSourceGroup.Draw,0,1),
            new AppearanceDefinition("TF_SKIN_K14",AppearanceKind.Skin,"Sundae","甜筒号",ShowcaseSourceGroup.Level,30,1),
            new AppearanceDefinition("TF_SKIN_K15",AppearanceKind.Skin,"Mint Choco","薄荷号",ShowcaseSourceGroup.Draw,0,1),
            new AppearanceDefinition("TF_SKIN_K16",AppearanceKind.Skin,"Candy Dream","糖梦号",ShowcaseSourceGroup.Draw,0,2),
            new AppearanceDefinition("TF_SKIN_K17",AppearanceKind.Skin,"Frost Crystal","冰晶号",ShowcaseSourceGroup.Level,60,2),
            new AppearanceDefinition("TF_SKIN_K18",AppearanceKind.Skin,"Magma","熔岩号",ShowcaseSourceGroup.Level,120,3),
            new AppearanceDefinition("TF_SKIN_K19",AppearanceKind.Skin,"Thunderbolt","雷霆号",ShowcaseSourceGroup.Draw,0,3),
            new AppearanceDefinition("TF_SKIN_K20",AppearanceKind.Skin,"Pearl","珍珠号",ShowcaseSourceGroup.Share,7,2),
            new AppearanceDefinition("TF_SKIN_K21",AppearanceKind.Skin,"Rainbow","虹梦号",ShowcaseSourceGroup.Draw,0,3),
            new AppearanceDefinition("TF_SKIN_K22",AppearanceKind.Skin,"Woodland","森语号",ShowcaseSourceGroup.Level,15,1),
            new AppearanceDefinition("TF_SKIN_K23",AppearanceKind.Skin,"Honeybee","蜜蜂号",ShowcaseSourceGroup.Share,3,1),
            new AppearanceDefinition("TF_SKIN_K24",AppearanceKind.Skin,"Pumpkin","南瓜号",ShowcaseSourceGroup.Draw,0,2),
            new AppearanceDefinition("TF_SKIN_K25",AppearanceKind.Skin,"Gift Voyage","礼遇号",ShowcaseSourceGroup.Share,14,1),
            new AppearanceDefinition("TF_SKIN_K26",AppearanceKind.Skin,"Moon Bunny","月兔号",ShowcaseSourceGroup.Draw,0,2),
            new AppearanceDefinition("TF_SKIN_K27",AppearanceKind.Skin,"Sakura","樱语号",ShowcaseSourceGroup.Level,80,2),
            new AppearanceDefinition("TF_SKIN_K28",AppearanceKind.Skin,"Star Rocket","星际号",ShowcaseSourceGroup.Draw,0,4),
            new AppearanceDefinition("TF_SKIN_K29",AppearanceKind.Skin,"Alien Scout","异星号",ShowcaseSourceGroup.Draw,0,4),
            new AppearanceDefinition("TF_SKIN_K30",AppearanceKind.Skin,"Robo Mate","机灵号",ShowcaseSourceGroup.Level,100,2),
            new AppearanceDefinition("TF_SKIN_K31",AppearanceKind.Skin,"Treasure","秘宝号",ShowcaseSourceGroup.Draw,0,4),
            new AppearanceDefinition("TF_SCENE_S01",AppearanceKind.Scene,"Sunny Harbor","阳光海港",ShowcaseSourceGroup.Initial,0),
            new AppearanceDefinition("TF_SCENE_S02",AppearanceKind.Scene,"Thunder Bay","雷霆风暴",ShowcaseSourceGroup.Level,25),
            new AppearanceDefinition("TF_SCENE_S03",AppearanceKind.Scene,"Sakura Coast","樱花海岸",ShowcaseSourceGroup.Share,1),
            new AppearanceDefinition("TF_SCENE_S04",AppearanceKind.Scene,"Sugar Cove","糖霜乐湾",ShowcaseSourceGroup.Draw,0),
            new AppearanceDefinition("TF_SCENE_S05",AppearanceKind.Scene,"Astral Isles","星河浮岛",ShowcaseSourceGroup.Draw,0),
            new AppearanceDefinition("TF_SCENE_S06",AppearanceKind.Scene,"Lantern Harbor","暮灯庆港",ShowcaseSourceGroup.Share,3),
            new AppearanceDefinition("TF_SCENE_S07",AppearanceKind.Scene,"Crystal Haven","蓝晶海城",ShowcaseSourceGroup.Level,50),
            new AppearanceDefinition("TF_SCENE_S08",AppearanceKind.Scene,"Pearl Lagoon","虹贝仙湾",ShowcaseSourceGroup.Draw,0),
            new AppearanceDefinition("TF_SCENE_S09",AppearanceKind.Scene,"Starfall Gate","群星之门",ShowcaseSourceGroup.Draw,0),
            new AppearanceDefinition("TF_SCENE_S10",AppearanceKind.Scene,"Ember Strait","熔火海峡",ShowcaseSourceGroup.Level,100),
            new AppearanceDefinition("TF_SCENE_S11",AppearanceKind.Scene,"Candy Falls","糖果虹瀑",ShowcaseSourceGroup.Draw,0),
            new AppearanceDefinition("TF_SCENE_S12",AppearanceKind.Scene,"Aurora Bay","极光月湾",ShowcaseSourceGroup.Level,15),
            new AppearanceDefinition("TF_TRAIL_W01",AppearanceKind.Trail,"Soft Foam","轻沫航迹",ShowcaseSourceGroup.Initial,0),
            new AppearanceDefinition("TF_TRAIL_W02",AppearanceKind.Trail,"Gold Wake","金辉航迹",ShowcaseSourceGroup.Level,10,1),
            new AppearanceDefinition("TF_TRAIL_W03",AppearanceKind.Trail,"Bubbles","泡泡航迹",ShowcaseSourceGroup.Level,5,0),
            new AppearanceDefinition("TF_TRAIL_W04",AppearanceKind.Trail,"Starlight","星芒航迹",ShowcaseSourceGroup.Draw,0,1),
            new AppearanceDefinition("TF_TRAIL_W05",AppearanceKind.Trail,"Sea Melody","潮汐乐章",ShowcaseSourceGroup.Share,1,1),
            new AppearanceDefinition("TF_TRAIL_W06",AppearanceKind.Trail,"Love Tide","心海涟漪",ShowcaseSourceGroup.Share,3,1),
            new AppearanceDefinition("TF_TRAIL_W07",AppearanceKind.Trail,"Pearl Shell","珠贝流光",ShowcaseSourceGroup.Level,25,2),
            new AppearanceDefinition("TF_TRAIL_W08",AppearanceKind.Trail,"Dragon Tide","龙鳞碧浪",ShowcaseSourceGroup.Draw,0,3),
            new AppearanceDefinition("TF_TRAIL_W09",AppearanceKind.Trail,"Ink Tide","墨潮幽影",ShowcaseSourceGroup.Level,50,2),
            new AppearanceDefinition("TF_TRAIL_W10",AppearanceKind.Trail,"Candy Wake","糖果浪花",ShowcaseSourceGroup.Draw,0,1),
            new AppearanceDefinition("TF_TRAIL_W11",AppearanceKind.Trail,"Rainbow","虹彩航迹",ShowcaseSourceGroup.Draw,0,2),
            new AppearanceDefinition("TF_TRAIL_W12",AppearanceKind.Trail,"Storm Wake","雷霆破浪",ShowcaseSourceGroup.Draw,0,4)
        });
        // Same-quality visual aliases preserve each of the 16 legacy entitlements and slot positions.
        private static readonly int[] LegacyArtwork={1,2,3,5,9,11,6,7,22,26,17,20,21,4,28,31};
        public static AppearanceDefinition Find(string id)=>All.FirstOrDefault(x=>x.Id==id);
        public static string VisualSkin(string id)
        {var old=SkinCatalog.Find(id);return old!=null?"TF_SKIN_K"+LegacyArtwork[old.CollectionOrder].ToString("D2"):Find(id)?.Kind==AppearanceKind.Skin?id:DefaultSkin;}
        public static SkinRarity? RarityFor(string id)=>SkinCatalog.Find(id)?.Rarity??Find(id)?.Rarity;
        public static bool Owns(string id,int cleared,CollectionData legacy)
        {
            var item=Find(id);if(item==null)return false;
            return item.Source==ShowcaseSourceGroup.Initial || item.Source==ShowcaseSourceGroup.Level&&cleared>=item.Requirement ||
                item.Kind==AppearanceKind.Skin&&legacy.OwnedIds.Any(x=>VisualSkin(x)==id);
        }
    }
    public sealed class AppearanceData
    {
        public string SceneId=AppearanceCatalog.DefaultScene,TrailId=AppearanceCatalog.DefaultTrail;
        // Null means inherited legacy equipment; five explicit positions (including holes) after first new UI selection.
        public string[] Equipment;
        public CollectionReceipt[] DrawReceipts=Array.Empty<CollectionReceipt>();
        public UniqueDrawReceipt[] UniqueReceipts=Array.Empty<UniqueDrawReceipt>();
        public long CoinSpent=>DrawReceipts.Sum(r=>(long)r.CoinCost)+UniqueReceipts.Sum(r=>(long)r.CoinCost);
        public bool Owns(string id,int cleared,CollectionData legacy)=>AppearanceCatalog.Owns(id,cleared,legacy)||DrawReceipts.Any(r=>r.SkinIds.Contains(id))||UniqueReceipts.Any(r=>r.ItemId==id);
        public AppearanceData Copy()=>new AppearanceData{UniqueReceipts=UniqueReceipts?.Select(r=>r?.Copy()).ToArray(),SceneId=SceneId,TrailId=TrailId,Equipment=Equipment?.ToArray(),DrawReceipts=DrawReceipts?.Select(r=>r?.Copy()).ToArray()};
        public string[] EffectiveSlots(CollectionData legacy)=>Equipment?.ToArray()??legacy.Equipment.Select(x=>x==null?null:AppearanceCatalog.VisualSkin(x)).ToArray();
        public void Validate(int cleared,CollectionData legacy)
        {
            AppearanceDrawEngine.Replay(legacy,DrawReceipts,cleared+1);
            UniqueDrawEngine.Validate(legacy,this,cleared+1);
            if(AppearanceCatalog.Find(SceneId)?.Kind!=AppearanceKind.Scene||!Owns(SceneId,cleared,legacy)||
               AppearanceCatalog.Find(TrailId)?.Kind!=AppearanceKind.Trail||!Owns(TrailId,cleared,legacy))throw new ArgumentException("Locked appearance selection.");
            if(Equipment==null)return;
            if(Equipment.Length!=5||Equipment.Where(x=>x!=null).Distinct().Count()!=Equipment.Count(x=>x!=null)||
                Equipment.Any(x=>x!=null&&(AppearanceCatalog.Find(x)?.Kind!=AppearanceKind.Skin||!Owns(x,cleared,legacy))))throw new ArgumentException("Invalid appearance equipment.");
        }
    }
}
