using System;
using System.Linq;
using NUnit.Framework;
using Tidebound.Collection;
using Tidebound.Save;
using Tidebound.Unity.UI;

namespace Tidebound.Tests
{
    public sealed class ShowcaseRosterTests
    {
        [Test]
        public void FreshAccountOwnsOnlyOneAndImportedArtworkDoesNotGrantRewards()
        {
            var save=new PlayerSaveService(new MemoryPlayerSaveStore());
            Assert.That(save.IsAvailable,Is.True);
            Assert.That(ShowcaseCatalog.All.Count,Is.EqualTo(25));
            Assert.That(ShowcaseCatalog.All.Where(s=>save.OwnsShowcase(s.Id)).Select(s=>s.Id),Is.EqualTo(new[]{ShowcaseCatalog.DefaultId}));
            foreach(var ship in ShowcaseCatalog.All.Skip(4))
            {
                Assert.That(ShowcaseCatalog.IsOwned(ship.Id,10000),Is.False,ship.Id);
                Assert.That(save.SelectShowcase(ship.Id),Is.EqualTo(ShowcaseSelectionStatus.Locked));
                var invalid=save.Snapshot;invalid.ShowcaseId=ship.Id;
                Assert.Throws<ArgumentException>(()=>invalid.Validate());
            }
            Assert.That(ShowcaseCatalog.NextReward(10),Is.Null,"Channels not yet operational cannot appear as active level rewards.");
        }
        [Test]
        public void ApprovedSourceGroupsPartitionStableCatalogWithoutGrantingOwnership()
        {
            Assert.That(ShowcaseCatalog.Groups.Select(g=>g.CatalogIndices.Count),Is.EqualTo(new[]{1,10,3,11}));
            Assert.That(ShowcaseCatalog.Groups.SelectMany(g=>g.CatalogIndices).OrderBy(i=>i),Is.EqualTo(Enumerable.Range(0,25)));
            Assert.That(ShowcaseCatalog.Groups[2].CatalogIndices.Select(i=>ShowcaseCatalog.All[i].Id),Is.EqualTo(new[]{"TF_SHOWCASE_H09","TF_SHOWCASE_H18","TF_SHOWCASE_H25"}));
            Assert.That(ShowcaseCatalog.Groups[1].CatalogIndices.Select(i=>ShowcaseCatalog.All[i].TargetClearLevel),Is.EqualTo(new[]{3,6,10,20,35,60,90,120,150,200}));
            foreach(var group in ShowcaseCatalog.Groups)foreach(var index in group.CatalogIndices)
                Assert.That(ShowcaseCatalog.All[index].SourceGroup,Is.EqualTo(group.Source));
        }
        [Test]
        public void EveryStableIdHasDistinctTextureAndValidVisibleBounds()
        {
            Assert.That(ShowcaseCatalog.All.Select(s=>s.Id).Distinct().Count(),Is.EqualTo(25));
            Assert.That(ShowcaseCatalog.All.Select(s=>s.ResourcePath).Distinct().Count(),Is.EqualTo(25));
            foreach(var ship in ShowcaseCatalog.All)
            {
                var texture=ShowcaseArt.Load(ship.Id);Assert.That(texture,Is.Not.Null,ship.Id);
                Assert.That(texture.width,Is.GreaterThanOrEqualTo(1024),ship.Id);
                var uv=ShowcaseArt.VisibleUV(ship.Id);
                Assert.That(uv.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(uv.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(uv.xMax,Is.LessThanOrEqualTo(1.00001));Assert.That(uv.yMax,Is.LessThanOrEqualTo(1.00001));
                Assert.That(uv.width*uv.height,Is.GreaterThan(.5f));Assert.That(ship.ChineseName,Is.Not.Empty);
            }
            Assert.That(ShowcaseArt.LockedMaterial,Is.Not.Null);
            Assert.That(ShowcaseArt.LockedMaterial.shader.isSupported,Is.True);
        }
    }
}
