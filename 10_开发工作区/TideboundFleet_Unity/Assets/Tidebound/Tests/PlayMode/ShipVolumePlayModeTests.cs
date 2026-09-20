using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class ShipVolumePlayModeTests
    {
        private static CandidateLevelCatalog Catalog()
        {
            var path=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(path,"manifest.json")),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".solution.json"))));
        }
        private static PortraitPuzzleGraybox Create(bool entry=false)
        {
            var g=new GameObject("Volume_Test").AddComponent<PortraitPuzzleGraybox>();
            g.Initialize(Catalog(),animateEntry:entry);Call(g,"OnApplicationFocus",true);return g;
        }
        private static void Call(PortraitPuzzleGraybox g,string method,params object[] args)=>typeof(PortraitPuzzleGraybox).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g,args);
        private static ShipPrototypeAppearance[] Views(PortraitPuzzleGraybox g)=>g.GetComponentsInChildren<ShipPrototypeAppearance>(true);
        [UnityTest]
        public IEnumerator DenseBoardUsesSharedVolumeMeshesAndKeepsIdentityWhenComparingFlat()
        {
            var g=Create();
            try
            {
                g.SelectLevel(9);yield return null;g.TogglePause();var board=g.Session.Board;var id=g.Session.SessionId;
                var views=Views(g);Assert.That(views.Length,Is.EqualTo(80));
                Assert.That(views.All(v=>v.IsThreeDimensional && v.HullRenderer.enabled && v.ShadowRenderer.enabled),Is.True);
                Assert.That(views.Select(v=>v.HullRenderer.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(),Is.EqualTo(2));
                Assert.That(views.All(v=>v.HullRenderer.GetComponent<MeshFilter>().sharedMesh.bounds.size.z>.3f),Is.True);
                Assert.That(views.All(v=>v.HullRenderer.shadowCastingMode==ShadowCastingMode.Off && !v.HullRenderer.receiveShadows),Is.True);
                Assert.That(g.GetComponentsInChildren<Collider>(true).Length,Is.Zero);
                Assert.That(GraphicsSettings.currentRenderPipeline,Is.Not.Null,"Validate volume rendering with the formal URP settings installed.");
                var resources=g.GetComponentInChildren<ShipPrototypeResources>();
                Assert.That(Enumerable.Range(0,5).Select(s=>resources.HullMaterial(2,s)).Distinct().Count(),Is.EqualTo(5));
                Assert.That(Enumerable.Range(0,5).All(s=>resources.HullMaterial(2,s).shader.isSupported),Is.True);
                Assert.That(resources.HullMaterial(3,0),Is.SameAs(resources.HullMaterial(3,4)));
                foreach(var v in views)
                {
                    var ship=g.Session.GetShip(v.transform.parent.GetComponent<ShipMovementView>().ShipId);
                    Assert.That(v.PaletteSlot,Is.EqualTo(ship.Length==3 ? -1 : g.Combat.Fleet.StandardGroups.Single(s=>s.SkinId==ship.SkinId).SlotIndex));
                }
                g.SetShipPrototypeMode(false);Assert.That(views.All(v=>!v.HullRenderer.enabled && !v.ShadowRenderer.enabled),Is.True);
                g.SetShipPrototypeMode(true);Assert.That(views.All(v=>v.HullRenderer.enabled),Is.True);
                Assert.That(g.Session.Board,Is.SameAs(board));Assert.That(g.Session.SessionId,Is.EqualTo(id));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator VolumeAndShadowStayInsideFootprintAndProjectionAtThreeSafeAreas()
        {
            var g=Create();
            try
            {
                g.SelectLevel(9);yield return null;g.TogglePause();
                foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                {
                    var factor=Mathf.Min(Screen.width/(size.x+10),Screen.height/(size.y+22))*.95f;
                    g.ApplyViewport(new Rect(new Vector2(5,11)*factor,size*factor),factor);Canvas.ForceUpdateCanvases();
                    Assert.That(g.BoardCamera.orthographic,Is.True);Assert.That(g.BoardCamera.transform.forward,Is.EqualTo(Vector3.forward));
                    var mapper=g.GetComponentInChildren<GridWorldMapper>();
                    foreach(var v in Views(g))
                    {
                        var root=v.transform.parent;var ship=g.Session.GetShip(root.GetComponent<ShipMovementView>().ShipId);
                        foreach(var r in new[]{v.HullRenderer,v.ShadowRenderer})foreach(var vertex in r.GetComponent<MeshFilter>().sharedMesh.vertices)
                        {
                            var world=r.transform.TransformPoint(vertex);var local=root.InverseTransformPoint(world);
                            Assert.That(Mathf.Abs(local.x),Is.LessThan(.5f));Assert.That(local.y,Is.InRange(-.5f,ship.Length-.5f));
                            Assert.That(g.BoardCamera.pixelRect.Contains(g.BoardCamera.WorldToScreenPoint(world)),Is.True);
                        }
                        foreach(var cell in g.Session.Board.Ships.Single(s=>s.Id==ship.Id).OccupiedCells)
                        {
                            var screen=g.BoardCamera.WorldToScreenPoint(mapper.TailToWorld(cell));
                            Assert.That(mapper.TryRayToCell(g.BoardCamera.ScreenPointToRay(screen),out var selected),Is.True);
                            Assert.That(selected,Is.EqualTo(cell));
                        }
                    }
                }
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator MeshLaneCentersAndExtentsFollowExistingPathAtPointEightScale()
        {
            var g=Create();
            try
            {
                g.SelectLevel(9);yield return null;g.TogglePause();
                var provider=new PortraitLanePathProvider(g.Session.Width,g.Session.Height);
                foreach(var length in new[]{2,3})
                {
                    var ship=g.Session.Ships.First(s=>s.Length==length);
                    var view=g.GetComponentsInChildren<PlanarShipLaneView>().Single(v=>v.ShipId==ship.Id);
                    var appearance=view.GetComponentInChildren<ShipPrototypeAppearance>();
                    foreach(LaneRoute route in Enum.GetValues(typeof(LaneRoute)))
                    {
                        var path=provider.CreatePath(route,new Vector3(6.5f,8.5f));
                        for(var i=0;i<=60;i++)
                        {
                            var t=i/60f;var center=path.Sample(t);view.ApplyLanePose(center,path.Tangent(t),.8f);
                            Assert.That(Vector3.Distance(appearance.HullRenderer.transform.position,center),Is.LessThan(.0001f));
                            Assert.That(view.transform.localScale,Is.EqualTo(Vector3.one*.8f));
                            foreach(var r in new[]{appearance.HullRenderer,appearance.ShadowRenderer})foreach(var v in r.GetComponent<MeshFilter>().sharedMesh.vertices)
                                Assert.That(g.BoardCamera.pixelRect.Contains(g.BoardCamera.WorldToScreenPoint(r.transform.TransformPoint(v))),Is.True);
                        }
                    }
                }
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator EntryAndFiveSecondHintReachTheActualMeshWithoutMovingItsGridAnchor()
        {
            var g=Create(true);
            try
            {
                Assert.That(Views(g).All(v=>v.EntryAlpha==0 && !v.HullRenderer.enabled && !v.ShadowRenderer.enabled),Is.True);
                var anchors=Views(g).Select(v=>v.transform.parent.position).ToArray();
                Call(g,"TickEntry",.4f);Assert.That(Views(g).Any(v=>v.EntryAlpha>0 && v.HullRenderer.enabled),Is.True);
                g.TogglePause();var alpha=Views(g).Select(v=>v.EntryAlpha).ToArray();Call(g,"TickEntry",10f);
                Assert.That(Views(g).Select(v=>v.EntryAlpha),Is.EqualTo(alpha));g.TogglePause();
                Call(g,"TickEntry",2f);Assert.That(g.IsEntryReady,Is.True);
                Assert.That(Views(g).Select(v=>v.transform.parent.position),Is.EqualTo(anchors));
                Call(g,"TickAssistance",0f);Call(g,"TickAssistance",4.9f);Assert.That(g.AutoHintShipId,Is.Null);
                Call(g,"TickAssistance",.11f);Assert.That(g.AutoHintShipId,Is.Not.Null);
                var target=Views(g).Single(v=>v.transform.parent.GetComponent<ShipMovementView>().ShipId==g.AutoHintShipId);
                var properties=new MaterialPropertyBlock();target.HullRenderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_Hint"),Is.GreaterThan(0));Assert.That(g.Session.Board.QueryForwardPath(g.AutoHintShipId).CanExit,Is.True);
                g.NotifyUserActivity();target.HullRenderer.GetPropertyBlock(properties);Assert.That(properties.GetFloat("_Hint"),Is.Zero);
                Assert.That(Views(g).All(v=>v.EntryAlpha==1 && v.transform.localScale==Vector3.one),Is.True);
                g.ShowHint();Assert.That(Views(g).Any(v=>v.HintStrength==1),Is.True,"Manual solver hint must also reach the mesh.");
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator RebuildingPresentationReleasesOwnedMeshesAndMaterials()
        {
            var g=Create();
            try
            {
                var old=Views(g).First().HullRenderer;var mesh=old.GetComponent<MeshFilter>().sharedMesh;var material=old.sharedMaterial;
                g.SelectLevel(1);yield return null;yield return null;
                Assert.That(mesh==null,Is.True);Assert.That(material==null,Is.True);
                Assert.That(Views(g).All(v=>v.HullRenderer.sharedMaterial!=null && v.HullRenderer.GetComponent<MeshFilter>().sharedMesh!=null),Is.True);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
    }
}
