using NUnit.Framework;
using Tidebound.Unity.Layout;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Boss;
using Tidebound.Unity.UI;
using Tidebound.Lane;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class WideBossLayoutTests
    {
        [TestCase(390,766,14,18)] [TestCase(360,640,14,18)] [TestCase(430,839,14,18)]
        [TestCase(390,766,6,6)] [TestCase(360,640,16,8)]
        public void MapKeepsInputCellsAndNarrowLanesInsideSafeArea(float w,float h,int columns,int rows)
        {
            var l=PortraitBoardLayout.Calculate(new Rect(0,34,w,h),columns,rows,true);
            Assert.That(l.Lane.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(l.Lane.xMax,Is.LessThanOrEqualTo(w+.001f));
            Assert.That(l.Lane.yMin,Is.GreaterThanOrEqualTo(l.Tools.yMax));Assert.That(l.Lane.yMax,Is.LessThanOrEqualTo(l.Top.yMin));
            Assert.That(l.Grid.width/columns,Is.EqualTo(l.Grid.height/rows).Within(.001));
            if(w==390&&h==766&&columns==14)Assert.That(l.CellSize,Is.GreaterThan(24),"Approved board enlarges the visible hulls");
        }
        [TestCase(390,204)] [TestCase(360,160.6154f)]
        public void AllFifteenSpritesUseCommonWaterlineAndSeparateFleet(float w,float h)
        {
            var layout=new GameplayBattleLayout(w,h);
            foreach(var kind in new[]{"Octopus","Crab","Manta","Shark","Turtle"})
            {
                float? bottom=null;
                foreach(var state in new[]{"Healthy","Tense","Weak"})
                {
                    var id="BossWide/"+kind+"_"+state;var record=GameplayArt.Get(id);
                    Assert.That(Resources.Load<Texture2D>("TideboundUI/Gameplay/"+id),Is.Not.Null,id);
                    Assert.That(record.aspect,Is.GreaterThan(2.9));
                    var bounds=layout.Boss(FleetBattleArtView.SideInset(kind),record.aspect);
                    Assert.That(bounds.yMin,Is.GreaterThan(layout.Fleet(2).yMax));
                    if(bottom.HasValue)Assert.That(bounds.yMin,Is.EqualTo(bottom.Value));bottom=bounds.yMin;
                    Assert.That(bounds.xMin,Is.GreaterThan(0));Assert.That(bounds.xMax,Is.LessThan(w));
                }
            }
        }
        [TestCase(1,"Octopus")][TestCase(20,"Octopus")][TestCase(21,"Crab")][TestCase(41,"Manta")][TestCase(61,"Shark")][TestCase(81,"Turtle")][TestCase(100,"Turtle")]
        public void CampaignAppearanceMapping(int level,string kind)=>Assert.That(FleetBattleArtView.KindForLevel(level),Is.EqualTo(kind));
        [TestCase(.61f,"Healthy")][TestCase(.60f,"Tense")][TestCase(.26f,"Tense")][TestCase(.25f,"Weak")][TestCase(0,"Weak")]
        public void HealthBoundaryPreservesExistingRules(float hp,string state)=>Assert.That(FleetBattleArtView.StateForHealth(hp),Is.EqualTo(state));
        [Test] public void NewPerimeterRoutesNeverShortcutThroughBoard()
        {
            var provider=new PortraitLanePathProvider(14,18,true);var grid=new Rect(0,0,14,18);
            foreach(var route in new[]{LaneRoute.Top,LaneRoute.Left,LaneRoute.Right,LaneRoute.BottomViaLeft,LaneRoute.BottomViaRight})
            {
                var path=provider.CreatePath(route,new Vector3(7,9,0));
                for(var i=0;i<=400;i++)Assert.That(grid.Contains(path.Sample(i/400f)),Is.False,route.ToString());
            }
        }
    }
}
