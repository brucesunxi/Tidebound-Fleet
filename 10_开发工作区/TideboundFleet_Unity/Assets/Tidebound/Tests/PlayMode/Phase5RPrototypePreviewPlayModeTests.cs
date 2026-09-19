using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Unity.Input;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class Phase5RPrototypePreviewPlayModeTests
    {
        [UnityTest]
        public IEnumerator FourCandidateSizesBuildFromCanonicalJsonAndStayInSyncAfterExit()
        {
            var names = new[]
            {
                "P5R_18x18_Open_080", "P5R_18x22_Open_090",
                "P5R_20x20_Deep_100", "P5R_22x22_Deep_110"
            };
            var expected = new[] { 80, 90, 100, 110 };
            for (var i = 0; i < names.Length; i++)
            {
                var root = new GameObject("PrototypePreview_Test");
                TextAsset json = null;
                try
                {
                    var path = Path.Combine(Application.dataPath,
                        "Tidebound/Config/LevelPrototypes/Phase5R/" + names[i] + ".json");
                    json = new TextAsset(File.ReadAllText(path));
                    var preview = root.AddComponent<BoardPrototypePreview>();
                    preview.Build(json);
                    Assert.That(preview.Board.ShipCount, Is.EqualTo(expected[i]));
                    Assert.That(preview.ShipViewCount, Is.EqualTo(expected[i]));
                    Assert.That(preview.ShipViews.Select(x => x.ShipId).Distinct().Count(), Is.EqualTo(expected[i]));

                    var firstExit = preview.Board.Ships.First(x => preview.Board.QueryForwardPath(x.Id).CanExit);
                    var selection = new BoardGridSelection(() => preview.Board);
                    Assert.That(selection.PointerDown(firstExit.OccupiedCells[0]), Is.EqualTo(firstExit.Id));
                    Assert.That(selection.PointerUp(firstExit.OccupiedCells[0]), Is.EqualTo(firstExit.Id));
                    Assert.That(preview.TryApplyMove(firstExit.Id, out var pathResult), Is.True);
                    Assert.That(pathResult.CanExit, Is.True);
                    Assert.That(preview.Board.ShipCount, Is.EqualTo(expected[i] - 1));
                    Assert.That(preview.ShipViewCount, Is.EqualTo(expected[i] - 1));
                }
                finally
                {
                    Object.Destroy(root);
                    if (json != null) Object.Destroy(json);
                }
                yield return null;
            }
        }
    }
}
