using System.Collections;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class ShowcaseIllustrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator FourShipsSwitchSharedArtworkWithoutAllocatingCamerasOrChangingUnknownSelection()
        {
            var root=new GameObject("IllustrationSwitch",typeof(RectTransform));
            var preview=root.AddComponent<CollectionShipPreview>();preview.Initialize(true);
            try
            {
                Texture previous=null;
                foreach(var ship in ShowcaseCatalog.All)
                {
                    preview.PresentShowcase(ship.Id);yield return null;
                    var image=root.transform.Find("ShipRender").GetComponent<RawImage>();
                    Assert.That(preview.UsesIllustration,Is.True,ship.Name);
                    Assert.That(preview.DisplayedShowcaseId,Is.EqualTo(ship.Id));
                    Assert.That(image.texture,Is.TypeOf<Texture2D>());
                    Assert.That(image.texture.width,Is.GreaterThanOrEqualTo(1024));
                    Assert.That(image.texture,Is.Not.SameAs(previous));
                    Assert.That(image.raycastTarget,Is.False);
                    Assert.That(root.GetComponentsInChildren<Camera>(true),Is.Empty);
                    Assert.That(root.GetComponentsInChildren<HarborShowcaseModel>(true),Is.Empty);
                    Assert.That(root.transform.Find("WaterReflection").GetComponent<RawImage>().texture,Is.SameAs(image.texture));
                    previous=image.texture;
                    preview.PresentShowcase(ship.Id);Assert.That(image.texture,Is.SameAs(previous));
                }
                var selected=preview.DisplayedShowcaseId;
                preview.PresentShowcase("missing");preview.PresentShowcase(null);
                Assert.That(preview.DisplayedShowcaseId,Is.EqualTo(selected));
            }
            finally{Object.Destroy(root);}yield return null;
        }

        [UnityTest]
        public IEnumerator IllustrationMotionFreezesAndOwnedMaterialsReleaseWhileSharedArtworkSurvives()
        {
            var root=new GameObject("IllustrationMotion",typeof(RectTransform));
            ((RectTransform)root.transform).sizeDelta=new Vector2(350,350);
            var preview=root.AddComponent<CollectionShipPreview>();preview.Initialize(true);var motion=true;preview.AllowMotion=()=>motion;
            var image=root.transform.Find("ShipRender").GetComponent<RawImage>();var shared=image.texture;
            var reflection=root.transform.Find("WaterReflection").GetComponent<RawImage>().material;
            var water=root.transform.Find("WaterContact").GetComponent<RawImage>().material;
            try
            {
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(image.rectTransform.anchoredPosition.sqrMagnitude,Is.GreaterThan(0));
                Assert.That(reflection.shader.isSupported&&water.shader.isSupported,Is.True);
                motion=false;yield return null;
                var phase=water.GetFloat("_Phase");var position=image.rectTransform.anchoredPosition;
                yield return new WaitForSecondsRealtime(.1f);
                Assert.That(water.GetFloat("_Phase"),Is.EqualTo(phase));
                Assert.That(reflection.GetFloat("_Phase"),Is.EqualTo(phase));
                Assert.That(image.rectTransform.anchoredPosition,Is.EqualTo(position));
                Assert.That(image.rectTransform.localRotation,Is.EqualTo(Quaternion.identity));
                motion=true;root.SetActive(false);yield return new WaitForSecondsRealtime(.1f);
                Assert.That(water.GetFloat("_Phase"),Is.EqualTo(phase));
                root.SetActive(true);yield return new WaitForSecondsRealtime(.1f);
                Assert.That(water.GetFloat("_Phase"),Is.GreaterThan(phase));
                preview.SetWaterVisible(false);
                Assert.That(root.transform.Find("WaterContact").GetComponent<RawImage>().enabled,Is.False);
                Assert.That(root.transform.Find("WaterReflection").GetComponent<RawImage>().enabled,Is.False);
            }
            finally{Object.Destroy(root);}yield return null;yield return null;
            Assert.That(water==null&&reflection==null,Is.True);
            Assert.That(shared!=null,Is.True,"A preview must not destroy the shared Resources texture.");
        }

        [UnityTest]
        public IEnumerator GameplaySkinPreviewKeepsItsRotatable3DModel()
        {
            var root=new GameObject("GameplayPreview",typeof(RectTransform));
            var preview=root.AddComponent<CollectionShipPreview>();preview.Initialize();
            try
            {
                Assert.That(preview.UsesIllustration,Is.False);
                Assert.That(root.GetComponentInChildren<Camera>(),Is.Not.Null);
                Assert.That(root.transform.Find("ShipRender").GetComponent<RawImage>().texture,Is.TypeOf<RenderTexture>());
                preview.Rotate();preview.Fire();yield return null;
                Assert.That(root.transform.Find("PreviewShot").GetComponent<Image>().enabled,Is.True);
                Assert.That(Quaternion.Angle(root.transform.Find("PreviewRig/StandardShip").localRotation,Quaternion.Euler(0,0,90)),Is.LessThan(.01f));
            }
            finally{Object.Destroy(root);}yield return null;
        }
    }
}
