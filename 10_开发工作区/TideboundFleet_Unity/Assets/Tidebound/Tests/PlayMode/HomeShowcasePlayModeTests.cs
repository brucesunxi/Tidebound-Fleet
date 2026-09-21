using System.Collections;
using NUnit.Framework;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class HomeShowcasePlayModeTests
    {
        [UnityTest]
        public IEnumerator DisplayMeshFitsCameraAndKeepsExplicitSurfaceMaterials()
        {
            var root=new GameObject("ShowcaseGeometryTest",typeof(RectTransform));
            try
            {
                var preview=root.AddComponent<CollectionShipPreview>();preview.Initialize(true);preview.AllowMotion=()=>false;
                yield return null;
                var ship=root.GetComponentInChildren<HarborShowcaseModel>();var filter=ship.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
                var lens=root.GetComponentInChildren<Camera>();
                Assert.That(mesh.vertexCount,Is.LessThan(65000));Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
                var metal=false;var glass=false;
                foreach(var finish in mesh.uv){metal|=finish.x>.8f;glass|=finish.y<.16f;}
                Assert.That(metal&&glass,Is.True,"Gold and glass must keep distinct surface parameters.");
                foreach(var vertex in mesh.vertices)
                {
                    var viewport=lens.WorldToViewportPoint(filter.transform.TransformPoint(vertex));
                    Assert.That(viewport.x,Is.InRange(.005f,.995f));Assert.That(viewport.y,Is.InRange(.005f,.995f));
                }
                foreach(var normal in mesh.normals)Assert.That(normal.sqrMagnitude,Is.InRange(.99f,1.01f));
                Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.Zero);
                Assert.That(lens.cullingMask,Is.EqualTo(1<<29));Assert.That(ship.transform.Find("WaterContact").gameObject.layer,Is.EqualTo(29));
                Assert.That(ship.GetComponent<Renderer>().sharedMaterial.shader.isSupported,Is.True);
                Assert.That(ship.transform.Find("WaterContact").GetComponent<Renderer>().sharedMaterial.shader.isSupported,Is.True);
            }
            finally{Object.Destroy(root);}yield return null;
        }
        [UnityTest]
        public IEnumerator ReducedMotionAndHiddenPreviewFreezeWaterAndReleaseOwnedResources()
        {
            var root=new GameObject("ShowcaseLifecycleTest",typeof(RectTransform));
            var preview=root.AddComponent<CollectionShipPreview>();preview.Initialize(true);var motion=true;preview.AllowMotion=()=>motion;
            var model=root.GetComponentInChildren<HarborShowcaseModel>();var lens=root.GetComponentInChildren<Camera>();
            var rt=lens.targetTexture;var mesh=model.GetComponent<MeshFilter>().sharedMesh;
            var material=model.GetComponent<Renderer>().sharedMaterial;
            var water=model.transform.Find("WaterContact");var waterMesh=water.GetComponent<MeshFilter>().sharedMesh;var waterMaterial=water.GetComponent<Renderer>().sharedMaterial;
            try
            {
                yield return new WaitForSecondsRealtime(.1f);Assert.That(model.WaterPhase,Is.GreaterThan(0));
                motion=false;yield return null;var stopped=model.WaterPhase;var position=model.transform.localPosition;
                yield return new WaitForSecondsRealtime(.1f);Assert.That(model.WaterPhase,Is.EqualTo(stopped));Assert.That(model.transform.localPosition,Is.EqualTo(position));
                motion=true;root.SetActive(false);yield return new WaitForSecondsRealtime(.1f);Assert.That(model.WaterPhase,Is.EqualTo(stopped));
                root.SetActive(true);yield return new WaitForSecondsRealtime(.1f);Assert.That(model.WaterPhase,Is.GreaterThan(stopped));
                model.SetWaterVisible(false);Assert.That(water.GetComponent<Renderer>().enabled,Is.False);model.SetWaterVisible(true);
            }
            finally{Object.Destroy(root);}yield return null;yield return null;
            Assert.That(rt==null,Is.True);Assert.That(mesh==null,Is.True);Assert.That(material==null,Is.True);
            Assert.That(waterMesh==null,Is.True);Assert.That(waterMaterial==null,Is.True);
        }
    }
}
