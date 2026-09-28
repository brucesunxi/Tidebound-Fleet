using System.Linq;
using NUnit.Framework;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class UIDesignSystemTests
    {
        [Test]
        public void DisplayFontContainsBothHomeLanguagesAndDigitsAfterLocaleSwitch()
        {
            var previous=UILanguage.Preference;
            var go=new GameObject("DisplayFontTest",typeof(RectTransform));
            try
            {
                var label=go.AddComponent<HarborText>();label.UseDisplayFont=true;label.text="Start Voyage";
                foreach(var language in new[]{LanguagePreference.Chinese,LanguagePreference.English})
                {
                    UILanguage.SetPreference(language,false);
                    Assert.That(label.font,Is.EqualTo(Resources.Load<Font>(language==LanguagePreference.Chinese?"TideboundUI/ResourceHanRoundedCN-Bold":"TideboundUI/LilitaOne-Regular")));
                    foreach(var c in language==LanguagePreference.Chinese?"开始航行海风旗舰收藏抽奖补给每日奖励邀请有礼排行榜0123456789":"0123456789Start VoyageCollectionSkin DrawSuppliesDaily GiftInviteRankingsSea Breeze")
                        Assert.That(label.font.HasCharacter(c),Is.True,"Missing glyph: "+c);
                }
            }
            finally{Object.DestroyImmediate(go);UILanguage.SetPreference(previous,false);}
        }

        [TestCase(84,98)]
        [TestCase(280,52)]
        [TestCase(24,24)]
        public void SlicedSkinKeepsVerticesInsideHitAreaAtRestAndWhenPressed(int width,int height)
        {
            var source=Resources.Load<GameObject>("TideboundUI/Prefabs/UI_Button_Common");
            var go=Object.Instantiate(source);
            try
            {
                var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(width,height);
                var graphic=go.GetComponent<HarborReliefImage>();graphic.color=new Color(1,1,1,.4f);
                var populate=typeof(HarborReliefImage).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance,null,new[]{typeof(VertexHelper)},null);
                foreach(var pressed in new[]{0f,1f})using(var helper=new VertexHelper())
                {
                    graphic.Depression=pressed;populate.Invoke(graphic,new object[]{helper});
                    var v=new UIVertex();
                    for(var i=0;i<helper.currentVertCount;i++)
                    {
                        helper.PopulateUIVertex(ref v,i);
                        Assert.That(v.position.x,Is.InRange(rect.rect.xMin,rect.rect.xMax));
                        Assert.That(v.position.y,Is.InRange(rect.rect.yMin,rect.rect.yMax));
                        Assert.That(v.uv0.x,Is.InRange(0f,1f));Assert.That(v.uv0.y,Is.InRange(0f,1f));
                        Assert.That(v.color.a,Is.EqualTo(102));
                    }
                }
            }
            finally{Object.DestroyImmediate(go);}
        }

        [TestCase("169BD5", nameof(HarborDesignTokens.OceanBlue))]
        [TestCase("FFFFFF", nameof(HarborDesignTokens.PearlWhite))]
        [TestCase("FFC928", nameof(HarborDesignTokens.Gold))]
        [TestCase("39C98A", nameof(HarborDesignTokens.EmeraldGreen))]
        [TestCase("FF6655", nameof(HarborDesignTokens.CoralRed))]
        public void BrandPaletteKeepsTheFiveApprovedBaseColours(string hex,string token)
        {
            Assert.That(ColorUtility.TryParseHtmlString("#"+hex,out var expected),Is.True);
            var actual=(Color)typeof(HarborDesignTokens).GetField(token).GetValue(null);
            Assert.That(actual,Is.EqualTo(expected));
        }

        [TestCase("UI_Popup")]
        [TestCase("UI_Tab")]
        [TestCase("UI_ItemCard")]
        [TestCase("UI_Button_Common")]
        [TestCase("UI_Button_Main")]
        [TestCase("UI_HUD_Resource")]
        [TestCase("UI_Ship_Display")]
        [TestCase("UI_MainMenu")]
        public void RequiredDesignSystemPrefabsLoadWithoutMissingComponents(string name)
        {
            var prefab=Resources.Load<GameObject>("TideboundUI/Prefabs/"+name);
            Assert.That(prefab,Is.Not.Null,name);
            Assert.That(prefab.GetComponentsInChildren<Component>(true).Any(component=>component==null),Is.False,name);
        }

        [TestCase(HarborSurfaceKind.Panel)]
        [TestCase(HarborSurfaceKind.Tab)]
        [TestCase(HarborSurfaceKind.Card)]
        public void CommonSurfacesRespectMaskBoundsAndTransparentTint(HarborSurfaceKind kind)
        {
            var go=new GameObject("SurfaceMeshTest",typeof(RectTransform));
            try
            {
                var rect=go.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(48,48);
                var graphic=go.AddComponent<HarborImage>();graphic.Kind=kind;graphic.color=new Color(1,1,1,.4f);
                graphic.Selected=true;graphic.Pressed=true;
                var populate=typeof(HarborImage).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance,null,new[]{typeof(VertexHelper)},null);
                using(var helper=new VertexHelper())
                {
                    populate.Invoke(graphic,new object[]{helper});Assert.That(helper.currentVertCount,Is.GreaterThan(0));
                    var vertex=new UIVertex();
                    for(var i=0;i<helper.currentVertCount;i++)
                    {
                        helper.PopulateUIVertex(ref vertex,i);
                        Assert.That(vertex.position.x,Is.InRange(rect.rect.xMin,rect.rect.xMax));
                        Assert.That(vertex.position.y,Is.InRange(rect.rect.yMin,rect.rect.yMax));
                        Assert.That(vertex.color.a,Is.EqualTo(102));
                    }
                }
            }
            finally{Object.DestroyImmediate(go);}
        }

        [Test]
        public void MainMenuAndButtonsKeepTheReviewedHierarchy()
        {
            var menu=Resources.Load<GameObject>("TideboundUI/Prefabs/UI_MainMenu");
            foreach(var node in new[]{"Background","BackgroundFocusOverlay","TopHUD","PlayerShipView","LeftMenu","RightMenu","MainActionButton","Notice"})
                Assert.That(menu.transform.Find(node),Is.Not.Null,node);
            Assert.That(menu.transform.Find("Background").GetComponent<HarborBackgroundTreatment>(),Is.Not.Null);
            Assert.That(menu.transform.Find("BackgroundFocusOverlay").GetComponent<HarborFocusOverlay>(),Is.Not.Null);

            var common=Resources.Load<GameObject>("TideboundUI/Prefabs/UI_Button_Common");
            foreach(var node in new[]{"Face/IconSlot","Face/Label","Face/Status"})Assert.That(common.transform.Find(node),Is.Not.Null,node);
            Assert.That(common.GetComponent<Button>().targetGraphic,Is.TypeOf<HarborReliefImage>());
            Assert.That(common.GetComponent<HarborReliefImage>().FaceTexture,Is.Not.Null);
            Assert.That(common.transform.Find("Face/Status").GetComponent<RectTransform>().anchoredPosition.y,Is.GreaterThanOrEqualTo(0));

            var main=Resources.Load<GameObject>("TideboundUI/Prefabs/UI_Button_Main");
            foreach(var node in new[]{"VoyageGlow","Face/Label","Face/SubLabel","Face/WheelAccent"})Assert.That(main.transform.Find(node),Is.Not.Null,node);
            Assert.That(main.transform.Find("Face/WheelAccent").GetComponent<RawImage>().texture,Is.Not.Null);
            Assert.That(main.GetComponent<HarborReliefImage>().FaceTexture,Is.Not.Null);
            Assert.That(main.GetComponent<HarborMainActionPulse>(),Is.Not.Null);
        }
    }
}
