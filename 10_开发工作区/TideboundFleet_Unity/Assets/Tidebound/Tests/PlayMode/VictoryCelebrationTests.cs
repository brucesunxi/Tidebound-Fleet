using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tidebound.Tests
{
    public sealed class VictoryCelebrationTests
    {
        [UnityTest]
        public IEnumerator NewStarsAnimateOnceAndFreezeOnInterruptionWithoutMutatingReceipt()
        {
            // Use the same model-victory fixture as the existing navigation regression suite.
            var type=typeof(VictoryResultPlayModeTests);
            var store=type.GetMethod("Won",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{9});
            var game=(PortraitPuzzleGraybox)type.GetMethod("Create",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new[]{store});
            try
            {
                yield return null;var view=game.ResultView;var result=game.CurrentResult;var data=game.SaveService.Snapshot;
                Assert.That(view.DisplayedStars,Is.EqualTo(9),"Restored results show the committed count immediately.");
                Assert.That(view.VisibleRewardCount,Is.EqualTo(1),"This fixture installs only ten levels.");
                Assert.That(view.transform.Find("ResultSurface"),Is.Null);
                var preview=view.GetComponentInChildren<CollectionShipPreview>();
                Assert.That(preview.MotionSpeed,Is.EqualTo(2.3f));Assert.That(preview.RockAmplitude,Is.EqualTo(1.95f));
                // A newly created view starts the receipt's presentation; it cannot grant anything.
                var root=new GameObject("FreshReceiptView",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(view.transform.parent,false);
                var fresh=root.gameObject.AddComponent<VictoryResultPanel>();fresh.Initialize(HarborUI.Font,null,null,null,null);
                try
                {
                    fresh.Layout(new Rect(0,0,390,844));fresh.Present(result,false,true,1,false);
                    Assert.That(fresh.DisplayedStars,Is.EqualTo(8));Canvas.ForceUpdateCanvases();
                    foreach(var graphic in fresh.GetComponentsInChildren<HarborVictoryGraphic>()){Assert.That(graphic.canvasRenderer,Is.Not.Null);Assert.That(graphic.canvasRenderer.GetMesh().vertexCount,Is.GreaterThan(0));}
                    yield return new WaitForSecondsRealtime(1.35f);Assert.That(fresh.DisplayedStars,Is.EqualTo(9));
                    fresh.ShowSharePreview();var time=fresh.CelebrationTime;yield return null;Assert.That(fresh.CelebrationTime,Is.EqualTo(time));
                    fresh.CloseSharePreview();fresh.SendMessage("OnApplicationPause",true);time=fresh.CelebrationTime;yield return null;Assert.That(fresh.CelebrationTime,Is.EqualTo(time));
                    fresh.SendMessage("OnApplicationPause",false);fresh.SendMessage("OnApplicationFocus",false);time=fresh.CelebrationTime;yield return null;Assert.That(fresh.CelebrationTime,Is.EqualTo(time));
                    fresh.SendMessage("OnApplicationFocus",true);yield return null;Assert.That(fresh.CelebrationTime,Is.GreaterThan(time));
                    fresh.Present(result,false,true,1,true);Assert.That(fresh.CanAnimate,Is.False);Assert.That(fresh.DisplayedStars,Is.EqualTo(9));
                    foreach(var language in new[]{LanguagePreference.Chinese,LanguagePreference.English})
                    {
                        var previous=UILanguage.Preference;UILanguage.SetPreference(language,false);
                        try {foreach(var size in new[]{new Vector2(360,640),new Vector2(390,844),new Vector2(430,932)})
                        {fresh.Layout(new Rect(0,0,size.x,size.y));Canvas.ForceUpdateCanvases();Assert.That(fresh.GetComponentsInChildren<RawImage>().Where(x=>x.name=="Art").All(x=>x.texture!=null),Is.True);}}
                        finally{UILanguage.SetPreference(previous,false);}
                    }
                    Assert.That(game.SaveService.Snapshot.Revision,Is.EqualTo(data.Revision));Assert.That(game.SaveService.Coins,Is.EqualTo(data.Coins));
                }
                finally{UnityEngine.Object.Destroy(root.gameObject);}
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}yield return null;
        }
    }
}
