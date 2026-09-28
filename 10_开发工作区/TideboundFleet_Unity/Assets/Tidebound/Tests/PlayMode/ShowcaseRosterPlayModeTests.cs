using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class ShowcaseRosterPlayModeTests
    {
        [UnityTest]
        public IEnumerator ProductionCollectionContainsWholeRosterAndFinalRowIsReachable()
        {
            var root=new GameObject("RosterVerification",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var panelObject=new GameObject("Collection",typeof(RectTransform),typeof(Image));panelObject.transform.SetParent(root.transform,false);
            var save=new PlayerSaveService(new MemoryPlayerSaveStore());var panel=panelObject.AddComponent<CollectionPanel>();
            try
            {
                panel.Initialize(save,null,()=>{});panel.Open();panel.SelectCategory(2);
                foreach(var size in new[]{new Vector2(390,844),new Vector2(360,640),new Vector2(360,800)})
                {
                    panel.Layout(new Rect(Vector2.zero,size));yield return null;Canvas.ForceUpdateCanvases();
                    var grid=panelObject.transform.Find("CollectionScroll/Content/CategoryPlaceholder/ShowcaseGrid");
                    Assert.That(grid,Is.Not.Null);Assert.That(grid.GetComponentsInChildren<Button>().Length,Is.EqualTo(25));
                    Assert.That(grid.GetComponentsInChildren<ShowcaseSectionRule>().Length,Is.EqualTo(4));
                    var first=grid.Find("Showcase_TF_SHOWCASE_H02").GetComponent<RectTransform>();var fourth=grid.Find("Showcase_TF_SHOWCASE_H05").GetComponent<RectTransform>();
                    Assert.That(first.anchoredPosition.y,Is.EqualTo(fourth.anchoredPosition.y));
                    Assert.That(grid.GetComponentsInChildren<Button>().Count(b=>b.interactable),Is.EqualTo(1));
                    foreach(var ship in grid.GetComponentsInChildren<CollectionShipPreview>())
                    {
                        var image=ship.transform.Find("ShipRender").GetComponent<RawImage>();
                        Assert.That(ship.UsesIllustration,Is.True);
                        Assert.That(image.material==ShowcaseArt.LockedMaterial,Is.EqualTo(ship.DisplayedShowcaseId!=ShowcaseCatalog.DefaultId));
                    }
                    var scroll=panelObject.transform.Find("CollectionScroll").GetComponent<ScrollRect>();scroll.verticalNormalizedPosition=0;yield return null;
                    var corners=new Vector3[4];var bounds=new Vector3[4];grid.Find("Showcase_TF_SHOWCASE_H24").GetComponent<RectTransform>().GetWorldCorners(corners);scroll.viewport.GetWorldCorners(bounds);
                    Assert.That(corners[0].y,Is.GreaterThanOrEqualTo(bounds[0].y-1),"Last row at "+size);
                    Assert.That(corners[1].y,Is.LessThanOrEqualTo(bounds[1].y+1),"Whole last card visible at "+size);
                }
                var before=save.Snapshot;
                Assert.That(panel.ChooseShowcase("TF_SHOWCASE_H25"),Is.EqualTo(ShowcaseSelectionStatus.Locked));
                Assert.That(save.SelectedShowcaseId,Is.EqualTo(ShowcaseCatalog.DefaultId));Assert.That(save.Snapshot.Revision,Is.EqualTo(before.Revision));
            }
            finally{Object.Destroy(root);}yield return null;
        }
    }
}
