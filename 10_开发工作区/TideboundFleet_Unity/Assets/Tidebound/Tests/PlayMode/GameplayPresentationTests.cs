using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class GameplayPresentationTests
    {
        private GameObject root;
        [TearDown] public void Cleanup(){if(root)UnityEngine.Object.DestroyImmediate(root);}
        private PortraitPuzzleGraybox Create()
        {
            var catalog=new PlayableLevelCatalog(new[]{"ArtFeedback"},_=>true,id=>new LevelData
            {
                SchemaVersion=2,LevelId=id,BossId="TF_KRAKEN_01",Width=6,Height=6,
                Ships=new[]{new ShipPlacementData{Id="A",TypeId="TF_BASE_SHIP",Position=new GridPosition(0,0),Direction=ShipDirection.Up,Length=2},
                    new ShipPlacementData{Id="B",TypeId="TF_BASE_SHIP",Position=new GridPosition(0,3),Direction=ShipDirection.Up,Length=2},
                    new ShipPlacementData{Id="L",TypeId="TF_BASE_SHIP",Position=new GridPosition(4,0),Direction=ShipDirection.Up,Length=3}}
            });
            root=new GameObject("GameplayArt_Test");var game=root.AddComponent<PortraitPuzzleGraybox>();game.Initialize(catalog,useGameplayArt:true);return game;
        }
        [UnityTest] public IEnumerator PartialMoveImpactFreezesAndDoesNotUndoLogicalAdvance()
        {
            var game=Create();yield return null;
            var ship=game.GetComponentsInChildren<ShipFloatPresentation>().Single(x=>x.transform.parent.name=="Ship_A");
            game.ClickShip("A");var deadline=Time.realtimeSinceStartup+5;
            while(ship.ImpactCount==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(ship.ImpactCount,Is.EqualTo(1));var location=game.ViewPosition("A");
            Assert.That(location.y,Is.EqualTo(1.5f).Within(.001f));
            game.TogglePause();var age=ship.ImpactAge;yield return new WaitForSecondsRealtime(.12f);
            Assert.That(ship.ImpactAge,Is.EqualTo(age));Assert.That(game.ViewPosition("A"),Is.EqualTo(location));
            game.TogglePause();while(game.IsBusy)yield return null;
            Assert.That(game.Session.Board.Ships.Single(s=>s.Id=="A").Position,Is.EqualTo(new GridPosition(0,1)));
            game.ClickShip("A");while(game.IsBusy)yield return null;
            Assert.That(ship.ImpactCount,Is.EqualTo(2));Assert.That(game.ViewPosition("A"),Is.EqualTo(location));
            Assert.That(game.Session.Board.ShipCount,Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator LongBoatUsesStretchedDefaultArtAndSameDisplayedFleetCount()
        {
            var game=Create();yield return null;
            var standard=root.transform.Find("PortraitPresentation/BoardCanvas/Ship_A/Body/ActualShipSkin").GetComponent<RawImage>();
            var longer=root.transform.Find("PortraitPresentation/BoardCanvas/Ship_L/Body/ActualShipSkin").GetComponent<RawImage>();
            Assert.That(longer.texture,Is.SameAs(standard.texture));Assert.That(longer.rectTransform.sizeDelta.x,Is.EqualTo(standard.rectTransform.sizeDelta.x));
            Assert.That(longer.rectTransform.sizeDelta.y/standard.rectTransform.sizeDelta.y,Is.EqualTo(1.5f).Within(.001f));
            game.ClickShip("L");var deadline=Time.realtimeSinceStartup+8;
            while(game.Combat.HitCount<1&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(game.Combat.HitCount,Is.EqualTo(1));Assert.That(game.CombatView.Artwork.DisplayedDefaultCount,Is.EqualTo(1));
            Assert.That(game.CombatView.transform.Find("LongSupport"),Is.Null);
            Assert.That(root.GetComponentsInChildren<RawImage>().Count(i=>i.name=="HullContactShadow"),Is.EqualTo(2));
        }
        [Test] public void RedHullNormalsAndGameplayArtAssetsExist()
        {
            foreach(var id in new[]{"TF_SKIN_K02","TF_SKIN_K08"})Assert.That(HarborAppearanceArt.Skin(id).rotation,Is.EqualTo(180));
            foreach(var id in new[]{"Settings_Button","Ocean_Background","Icon_Rescue","Icon_Shuffle","Icon_Reverse","Side/K01","Boss/Octopus_Healthy","Boss/Turtle_Weak"})
                Assert.That(Resources.Load<Texture2D>("TideboundUI/Gameplay/"+id),Is.Not.Null,id);
        }
        [UnityTest] public IEnumerator ScreenInputMatchesOccupiedCellsAfterViewportAndArtResizing()
        {
            var game=Create();yield return null;
            var sizes=new[]{new Rect(0,0,390,844),new Rect(0,18,360,600)};
            foreach(var size in sizes)
            {
                game.ApplyViewport(size,1);
                var point=game.Layout.CellCenter(4,0);
                var projected=game.BoardCamera.WorldToScreenPoint(game.ViewPosition("L"));
                Assert.That(Vector2.Distance(point,projected),Is.LessThan(.1f));
            }
            var click=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {position=game.Layout.CellCenter(4,0),pointerId=0,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            game.InputSurface.OnPointerDown(click);game.InputSurface.OnPointerUp(click);
            var deadline=Time.realtimeSinceStartup+5;while(game.IsBusy&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(game.Session.Board.ShipCount,Is.EqualTo(2));Assert.That(game.ExitedIds,Does.Contain("L"));
        }
    }
}
