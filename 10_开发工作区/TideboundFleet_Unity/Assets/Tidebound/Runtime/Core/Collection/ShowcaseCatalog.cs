using System;
using System.Collections.Generic;
using System.Linq;

namespace Tidebound.Collection
{
    public enum ShowcaseSourceGroup { Initial, Level, Share, Draw }
    public enum ShowcaseAcquisition { Default, Level, Unassigned }
    public sealed class ShowcaseDefinition
    {
        public string Id {get;}
        public string Name {get;}
        public int ClearLevel {get;}
        public int VisualIndex {get;}
        public string ChineseName {get;}
        public ShowcaseAcquisition Acquisition {get;}
        // Confirmed presentation route is separate from a currently operational reward channel.
        public ShowcaseSourceGroup SourceGroup {get;}
        public int TargetClearLevel {get;}
        public string ResourcePath => "TideboundUI/Showcase/Showcase_H"+(VisualIndex+1).ToString("D2")+"_v1";
        public ShowcaseDefinition(string id,string name,int clearLevel,int visualIndex)
            : this(id,name,name,clearLevel,visualIndex) {}
        public ShowcaseDefinition(string id,string name,string chineseName,int clearLevel,int visualIndex,ShowcaseSourceGroup? sourceGroup=null,int targetClearLevel=-1)
        {Id=id;Name=name;ChineseName=chineseName;ClearLevel=clearLevel;VisualIndex=visualIndex;
            SourceGroup=sourceGroup??(clearLevel==0?ShowcaseSourceGroup.Initial:ShowcaseSourceGroup.Level);TargetClearLevel=targetClearLevel>=0?targetClearLevel:clearLevel;
            Acquisition=clearLevel<0?ShowcaseAcquisition.Unassigned:clearLevel==0?ShowcaseAcquisition.Default:ShowcaseAcquisition.Level;}
    }
    public sealed class ShowcaseGroup
    {
        public ShowcaseSourceGroup Source {get;}
        public string Title {get;}
        public string ChineseTitle {get;}
        public IReadOnlyList<int> CatalogIndices {get;}
        public ShowcaseGroup(ShowcaseSourceGroup source,string title,string chineseTitle,int[] indices)
        {Source=source;Title=title;ChineseTitle=chineseTitle;CatalogIndices=Array.AsReadOnly(indices);}
    }
    /// <summary>Display-only rewards. Ownership derives from durably validated victory progress.</summary>
    public static class ShowcaseCatalog
    {
        public const string Version="ShowcaseV2",DefaultId="TF_SHOWCASE_H01";
        public static IReadOnlyList<ShowcaseDefinition> All {get;}=Array.AsReadOnly(new[]{
            new ShowcaseDefinition(DefaultId,"Sea Breeze","海风旗舰",0,0),
            new ShowcaseDefinition("TF_SHOWCASE_H02","White Sail","白帆游艇",3,1),
            new ShowcaseDefinition("TF_SHOWCASE_H03","Royal Flagship","皇家旗舰",6,2),
            new ShowcaseDefinition("TF_SHOWCASE_H04","Crimson Corsair","赤帆海盗",10,3),
            new ShowcaseDefinition("TF_SHOWCASE_H05","Explorer","远洋探险",-1,4,ShowcaseSourceGroup.Level,20),
            new ShowcaseDefinition("TF_SHOWCASE_H06","Steamworks","蒸汽齿轮",-1,5,ShowcaseSourceGroup.Level,35),
            new ShowcaseDefinition("TF_SHOWCASE_H07","Bakery","烘焙工坊",-1,6,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H08","Blue Shark","蓝鲨突击",-1,7,ShowcaseSourceGroup.Level,60),
            new ShowcaseDefinition("TF_SHOWCASE_H09","Sailor Duck","水手萌鸭",-1,8,ShowcaseSourceGroup.Share),
            new ShowcaseDefinition("TF_SHOWCASE_H10","Pumpkin","南瓜夜航",-1,9,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H11","Santa","圣诞礼舟",-1,10,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H12","Frost Crystal","冰晶远航",-1,11,ShowcaseSourceGroup.Level,90),
            new ShowcaseDefinition("TF_SHOWCASE_H13","Lava Heart","熔岩之心",-1,12,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H14","Star Voyager","星际探索",-1,13,ShowcaseSourceGroup.Level,120),
            new ShowcaseDefinition("TF_SHOWCASE_H15","Treasure","黄金宝藏",-1,14,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H16","Pearl Bubble","幻彩珍珠",-1,15,ShowcaseSourceGroup.Level,150),
            new ShowcaseDefinition("TF_SHOWCASE_H17","Black Sail","黑帆船长",-1,16,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H18","Bamboo Panda","竹海熊猫",-1,17,ShowcaseSourceGroup.Share),
            new ShowcaseDefinition("TF_SHOWCASE_H19","Sakura","樱花春信",-1,18,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H20","Moon Mage","星月魔法",-1,19,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H21","Ruby Crown","赤金王冠",-1,20,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H22","Dragon Boat","祥龙竞渡",-1,21,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H23","Lighthouse","灯塔港湾",-1,22,ShowcaseSourceGroup.Level,200),
            new ShowcaseDefinition("TF_SHOWCASE_H24","Octopus","深海章鱼",-1,23,ShowcaseSourceGroup.Draw),
            new ShowcaseDefinition("TF_SHOWCASE_H25","Palm Holiday","椰岛假日",-1,24,ShowcaseSourceGroup.Share)});
        public static IReadOnlyList<ShowcaseGroup> Groups {get;}=Array.AsReadOnly(new[]{
            Group(ShowcaseSourceGroup.Initial,"Initially Owned","初始拥有"),
            Group(ShowcaseSourceGroup.Level,"Level Rewards","通关获取"),
            Group(ShowcaseSourceGroup.Share,"Share Rewards","分享获取"),
            Group(ShowcaseSourceGroup.Draw,"Draw Rewards","抽奖获取")});
        private static ShowcaseGroup Group(ShowcaseSourceGroup source,string title,string chineseTitle) =>
            new ShowcaseGroup(source,title,chineseTitle,All.Select((ship,index)=>new {ship,index}).Where(x=>x.ship.SourceGroup==source).Select(x=>x.index).ToArray());
        public static ShowcaseDefinition Find(string id)=>All.FirstOrDefault(s=>s.Id==id);
        public static bool IsOwned(string id,int highestCleared)=>Find(id) is ShowcaseDefinition s &&
            (s.SourceGroup==ShowcaseSourceGroup.Initial || s.SourceGroup==ShowcaseSourceGroup.Level && s.TargetClearLevel<=highestCleared);
        public static ShowcaseDefinition NextReward(int highestCleared)=>All.FirstOrDefault(s=>s.Acquisition==ShowcaseAcquisition.Level && s.ClearLevel>highestCleared);
    }
}
