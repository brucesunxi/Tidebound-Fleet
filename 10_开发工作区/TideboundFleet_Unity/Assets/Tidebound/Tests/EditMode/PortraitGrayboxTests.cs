using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Unity.Layout;
using Tidebound.Unity.Lane;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class PortraitGrayboxTests
    {
        private const string Folder = "Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates/";
        private static string Read(string name) => AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+name).text;
        private static string[] Layouts() => Phase5RLevelRecipes.All.Select(r=>Read(r.LevelId+".json")).ToArray();
        private static string[] Proofs() => Phase5RLevelRecipes.All.Select(r=>Read(r.LevelId+".solution.json")).ToArray();

        [TestCase(360,640,17.219048f)]
        [TestCase(390,844,21.0588235f)]
        [TestCase(430,932,23.411765f)]
        public void ThreePortraitProfilesHaveDisjointRegionsAndEqualAxisCells(float width,float height,float cell)
        {
            var safe = new Rect(13,27,width,height);
            var layout = PortraitBoardLayout.Calculate(safe,14,18);
            Assert.That(layout.CellSize,Is.EqualTo(cell).Within(.0001));
            Assert.That(layout.Tools.yMin,Is.EqualTo(safe.yMin));
            Assert.That(layout.Top.yMax,Is.EqualTo(safe.yMax));
            Assert.That(layout.Tools.yMax,Is.LessThan(layout.Middle.yMin));
            Assert.That(layout.Middle.yMax,Is.LessThan(layout.Top.yMin));
            Assert.That(layout.Lane.xMin,Is.GreaterThanOrEqualTo(safe.xMin+16-.0001));
            Assert.That(layout.Lane.xMax,Is.LessThanOrEqualTo(safe.xMax-16+.0001));
            Assert.That(layout.Lane.yMin,Is.GreaterThanOrEqualTo(layout.Middle.yMin+16-.0001));
            Assert.That(layout.Lane.yMax,Is.LessThanOrEqualTo(layout.Middle.yMax-16+.0001));
            for(var x=0;x<14;x++) for(var y=0;y<18;y++) Assert.That(layout.Grid.Contains(layout.CellCenter(x,y)),Is.True);
        }

        [Test]
        public void InvalidOrTooSmallSafeAreasFailExplicitly()
        {
            Assert.Throws<ArgumentException>(()=>PortraitBoardLayout.Calculate(new Rect(0,0,float.NaN,640),14,18));
            Assert.Throws<ArgumentException>(()=>PortraitBoardLayout.Calculate(new Rect(0,0,390,150),14,18));
        }

        [Test]
        public void CatalogOrdersByStableIdsAndReturnsFreshData()
        {
            var catalog=new CandidateLevelCatalog(Read("manifest.json"),Layouts().Reverse(),Proofs().Reverse());
            var level=catalog.Load(0); level.Ships[0].Id="tampered";
            Assert.That(catalog.Load(0).Ships[0].Id,Is.Not.EqualTo("tampered"));
            Assert.That(catalog.Count,Is.EqualTo(10)); Assert.That(catalog.Load(9).Ships.Length,Is.EqualTo(80));
        }

        [TestCase("missing")][TestCase("duplicate")][TestCase("stale")][TestCase("approved")][TestCase("version")][TestCase("hash")]
        public void CatalogRejectsMissingDuplicateStaleAndUnsupportedInputs(string damage)
        {
            var levels=Layouts(); var proofs=Proofs(); var manifest=JObject.Parse(Read("manifest.json"));
            if(damage=="missing") levels=levels.Take(9).ToArray();
            if(damage=="duplicate") levels[1]=levels[0];
            if(damage=="stale") { var p=JObject.Parse(proofs[0]); p["layoutFingerprint"]="bad"; proofs[0]=p.ToString(); }
            if(damage=="approved") manifest["status"]="Approved";
            if(damage=="version") manifest["rulesVersion"]="OldRule";
            if(damage=="hash") manifest["levels"][0]["layoutFingerprint"]="bad";
            Assert.Throws<ArgumentException>(()=>new CandidateLevelCatalog(manifest.ToString(),levels,proofs));
        }

        [Test]
        public void LinearPerimeterCornersNeverCutAcrossTheBoard()
        {
            var path=new LaneWorldPath(true,new Vector3(5,-.2f),new Vector3(-.2f,-.2f),new Vector3(-.2f,18.2f),new Vector3(7,18.2f));
            var board=new Rect(0,0,14,18);
            for(var i=0;i<=1000;i++) Assert.That(board.Contains(path.Sample(i/1000f)),Is.False);
        }
    }
}
