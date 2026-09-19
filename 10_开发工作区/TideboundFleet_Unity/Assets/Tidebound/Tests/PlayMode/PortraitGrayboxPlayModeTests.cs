using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Lane;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class PortraitGrayboxPlayModeTests
    {
        private static CandidateLevelCatalog Catalog()
        {
            var path=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(path,"manifest.json")),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".solution.json"))));
        }
        private static PortraitPuzzleGraybox Create(bool fast=false)
        {
            var game=new GameObject("PortraitGraybox_Test").AddComponent<PortraitPuzzleGraybox>();
            game.Initialize(Catalog(),fast ? new ShipMovementTiming(1000,.005f,.01f,.01f,.06f) : null,
                fast ? new LaneTransitTiming(.03,.005,.005) : null);
            return game;
        }
        private static IEnumerator Until(Func<bool> predicate,float seconds=5)
        {
            var end=Time.realtimeSinceStartup+seconds;
            while(!predicate() && Time.realtimeSinceStartup<end) yield return null;
            Assert.That(predicate(),Is.True,"Runtime did not reach expected state before timeout.");
        }
        private static PointerEventData Pointer(PortraitPuzzleGraybox game,Vector3 world,int pointerId=1) =>
            new PointerEventData(EventSystem.current) {position=game.BoardCamera.WorldToScreenPoint(world),pointerId=pointerId,button=PointerEventData.InputButton.Left};
        private static void ClickThroughGraphicRaycast(PortraitPuzzleGraybox game,Vector3 world)
        {
            Canvas.ForceUpdateCanvases(); var data=Pointer(game,world); var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(data,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject,Is.EqualTo(game.InputSurface.gameObject));
            ExecuteEvents.Execute(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hits[0].gameObject,data,ExecuteEvents.pointerUpHandler);
        }

        [UnityTest]
        public IEnumerator EveryShipHasAnActiveDirectionMesh()
        {
            var game=Create();
            try
            {
                game.SelectLevel(9); yield return null; Canvas.ForceUpdateCanvases();
                var arrows=game.GetComponentsInChildren<GrayboxArrowGraphic>();
                Assert.That(arrows.Length,Is.EqualTo(80));
                foreach(var arrow in arrows)
                {
                    Assert.That(arrow.canvasRenderer,Is.Not.Null);
                    Assert.That(arrow.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(0));
                    Assert.That(arrow.color.a,Is.GreaterThan(0));
                }
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TenCandidatesClearThroughRealAnimationCallbacksAndLaneViews()
        {
            var game=Create(true);
            try
            {
                for(var i=0;i<10;i++)
                {
                    if(i>0) game.SelectLevel(i); yield return null;
                    var count=game.Session.Board.ShipCount;
                    game.ToggleAuto();
                    yield return Until(()=>game.IsCleared,25);
                    Assert.That(game.ActiveViewCount,Is.Zero,"Level "+(i+1));
                    Assert.That(game.ExitedIds.Count,Is.EqualTo(count));
                    Assert.That(game.ExitedIds.Distinct().Count(),Is.EqualTo(count));
                    CollectionAssert.AreEqual(game.ExitedIds,game.EnteredIds);
                }
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PartialMovementPauseAdjacentRetryAndRestartUseCurrentState()
        {
            var game=Create();
            try
            {
                yield return null;
                var ship=game.Session.Board.Ships.First(s=>{var p=game.Session.Board.QueryForwardPath(s.Id);return p.IsBlocked && p.TravelDistance>0;});
                var path=game.Session.Board.QueryForwardPath(ship.Id);
                ClickThroughGraphicRaycast(game,game.ViewPosition(ship.Id));
                Assert.That(game.IsBusy,Is.True);
                game.ClickShip(game.Session.Board.Ships.First(s=>s.Id!=ship.Id).Id);
                game.TogglePause();var frozen=game.ViewPosition(ship.Id);
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(game.ViewPosition(ship.Id),Is.EqualTo(frozen));
                Assert.That(game.Session.Board.GetShip(ship.Id).Position,Is.EqualTo(path.OriginTail));
                game.ApplyViewport(new Rect(9,15,430,932),1); // Camera-only resize cannot invalidate a world-space animation target.
                game.TogglePause(); yield return Until(()=>!game.IsBusy);
                Assert.That(game.Session.Board.GetShip(ship.Id).Position,Is.EqualTo(path.TargetTail));
                Assert.That(game.ViewPosition(ship.Id),Is.EqualTo(new Vector3(path.TargetTail.X+.5f,path.TargetTail.Y+.5f,0)));
                game.ClickShip(ship.Id); yield return Until(()=>!game.IsBusy);
                Assert.That(game.Session.Board.GetShip(ship.Id).Position,Is.EqualTo(path.TargetTail));
                Assert.That(game.ExitedIds,Is.Empty);
                Assert.That(game.SolveCurrent().Status,Is.EqualTo(LevelSolverStatus.Solved));
                var exiting=game.Session.Board.Ships.First(s=>game.Session.Board.QueryForwardPath(s.Id).CanExit);
                game.ClickShip(exiting.Id); var oldSession=game.Session.SessionId;
                game.Restart(); yield return new WaitForSecondsRealtime(.8f);
                Assert.That(game.Session.SessionId,Is.Not.EqualTo(oldSession));
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(7)); Assert.That(game.ExitedIds,Is.Empty);
                Assert.That(game.ActiveViewCount,Is.EqualTo(7));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThreeSafeAreasMapAllCellsAndRouteOnlyTheOwningPointer()
        {
            var game=Create(true);
            try
            {
                game.SelectLevel(1); yield return null;
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                {
                    game.Restart(); yield return null;
                    var scale = Mathf.Min(Screen.width/(size.x+10), Screen.height/(size.y+22))*.95f;
                    game.ApplyViewport(new Rect(new Vector2(5,11)*scale,size*scale),scale); Canvas.ForceUpdateCanvases();
                    foreach(var s in game.Session.Board.Ships)
                    foreach(var cell in s.OccupiedCells)
                    {
                        var screen=game.BoardCamera.WorldToScreenPoint(new Vector3(cell.X+.5f,cell.Y+.5f,0));
                        var expected=game.Layout.CellCenter(cell.X,cell.Y)*scale;
                        var context = $"size={size}, screen={Screen.width}x{Screen.height}, viewport={game.BoardCamera.pixelRect}, middle={game.Layout.Middle}, aspect={game.BoardCamera.aspect}, ortho={game.BoardCamera.orthographicSize}, cell={cell}";
                        Assert.That(screen.x,Is.EqualTo(expected.x).Within(1),"X "+context); Assert.That(screen.y,Is.EqualTo(expected.y).Within(1),"Y "+context);
                        var mapper = game.GetComponentInChildren<GridWorldMapper>();
                        Assert.That(mapper.TryRayToCell(game.BoardCamera.ScreenPointToRay(screen),out var mapped),Is.True);
                        Assert.That(mapped,Is.EqualTo(cell));
                        var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(Pointer(game,new Vector3(cell.X+.5f,cell.Y+.5f)),hits);
                        Assert.That(hits[0].gameObject,Is.EqualTo(game.InputSurface.gameObject));
                    }
                    var a=game.Session.Board.Ships.First(s=>game.Session.Board.QueryForwardPath(s.Id).CanExit);
                    var b=game.Session.Board.Ships.First(s=>s.Id!=a.Id);
                    var first=Pointer(game,game.ViewPosition(a.Id),1); var second=Pointer(game,game.ViewPosition(b.Id),2);
                    game.InputSurface.OnPointerDown(first); game.InputSurface.OnPointerDown(second); game.InputSurface.OnPointerUp(second);
                    Assert.That(game.IsBusy,Is.False);
                    game.InputSurface.OnPointerUp(first); yield return Until(()=>!game.IsBusy);
                    Assert.That(game.ExitedIds,Is.EquivalentTo(new[]{a.Id}));
                }
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }
    }
}
