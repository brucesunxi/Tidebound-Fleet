using System;
using System.Collections;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Tidebound.EditorTools
{
    public static partial class PortraitGrayboxCapture
    {
        private static IEnumerator C2Frame(string name)
        {
            var diagnostics=new System.Collections.Generic.List<string>();
            foreach(var label in UnityEngine.Object.FindObjectsOfType<HarborText>())
                if(label.isActiveAndEnabled && System.Text.RegularExpressions.Regex.IsMatch(label.text,@"[A-Za-z]{2,}"))
                    diagnostics.Add(label.name+" | "+label.text.Replace("\n"," / "));
            System.IO.Directory.CreateDirectory(OutputDirectory);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(OutputDirectory,name+".labels.txt"),diagnostics);
            yield return SaveFrame(name);
        }
        private static IEnumerator CaptureC2Frames(PortraitPuzzleGraybox game)
        {
            if(Environment.GetEnvironmentVariable("TIDEBOUND_CAPTURE_SHOWCASE_ONLY")=="1")
            {yield return CaptureShowcasePolishFrames(game);yield break;}
            var catalog=ResultCatalog(game);var previous=UILanguage.Preference;
            foreach(var locale in new[]{LanguagePreference.English,LanguagePreference.Chinese})
            foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(390,844)})
            {
                UILanguage.SetPreference(locale,false);SetGameViewSize(size);
                var end=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<end)yield return null;
                if(Screen.width!=size.x||Screen.height!=size.y)throw new TimeoutException("C2 viewport did not settle.");
                var service=new PlayerSaveService(ResultStore(catalog,4));service.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");
                game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);yield return null;yield return null;
                var tag=locale+"_"+size.x+"x"+size.y;
                yield return C2Frame("C2_Home_"+tag+".png");
                var startButton=game.transform.Find("HomeNavigation/HomeControls/Continue").gameObject;
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(startButton,pointer,ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(.15f);yield return C2Frame("C2_HomePressed_"+tag+".png");
                ExecuteEvents.Execute(startButton,pointer,ExecuteEvents.pointerUpHandler);yield return new WaitForSecondsRealtime(.2f);
                game.OpenCollection();yield return null;yield return C2Frame("C2_Collection_"+tag+".png");
                game.CollectionView.OpenDraw();yield return null;yield return C2Frame("C2_Draw_"+tag+".png");
                game.CollectionView.ShowOdds();yield return null;yield return C2Frame("C2_Odds_"+tag+".png");
                game.CloseCollection();game.OpenShop();yield return null;yield return C2Frame("C2_Supplies_"+tag+".png");
                game.CloseAcquisition();
                if(locale==LanguagePreference.Chinese && size.x==390)
                {
                    game.OpenCollection();game.CollectionView.SelectCategory(2);yield return null;yield return C2Frame("C2_ShowcaseShell_Chinese.png");game.CloseCollection();
                    game.transform.Find("HomeNavigation/HomeControls/Settings").GetComponent<Button>().onClick.Invoke();yield return null;
                    yield return C2Frame("C2_Settings_Chinese.png");
                    game.transform.Find("HomeNavigation/HomeSettings/Close").GetComponent<Button>().onClick.Invoke();
                    game.ContinueFromHome();yield return null;yield return C2Frame("C2_Result_Chinese.png");
                    game.ContinueFromResult();yield return null;yield return null;yield return C2Frame("C2_Level5_Chinese.png");
                }
            }
            UILanguage.SetPreference(LanguagePreference.English,false);UILanguage.SetPseudoForReview(true);
            game.Initialize(catalog,saveService:new PlayerSaveService(ResultStore(catalog,4)),campaign:true,useHomeNavigation:true);
            game.OpenCollection();yield return null;yield return C2Frame("C2_Pseudo_Collection.png");
            UILanguage.SetPseudoForReview(false);UILanguage.SetPreference(previous,false);
        }
        private static IEnumerator CaptureShowcasePolishFrames(PortraitPuzzleGraybox game)
        {
            var catalog=ResultCatalog(game);var previous=UILanguage.Preference;
            try
            {
                foreach(var locale in new[]{LanguagePreference.English,LanguagePreference.Chinese})
                foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(390,844)})
                {
                    UILanguage.SetPreference(locale,false);SetGameViewSize(size);var expires=Time.realtimeSinceStartup+10;
                    while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<expires)yield return null;
                    if(Screen.width!=size.x||Screen.height!=size.y)throw new TimeoutException("Showcase viewport did not settle.");
                    var service=new PlayerSaveService(ResultStore(catalog,4));service.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");
                    game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);yield return null;yield return null;
                    var preview=game.transform.Find("HomeNavigation/HomeControls/ShowcasePreview").GetComponent<CollectionShipPreview>();
                    preview.AllowMotion=()=>false;yield return null;
                    var tag=locale+"_"+size.x+"x"+size.y;yield return SaveFrame("Showcase_Home_"+tag+".png");
                    if(locale==LanguagePreference.Chinese&&size.x==390)
                    {
                        var model=preview.GetComponentInChildren<HarborShowcaseModel>();model.SetWaterVisible(false);yield return null;
                        yield return SaveFrame("Showcase_WithoutWater.png");model.SetWaterVisible(true);
                        // Record actual changing game frames; capture never invokes the continue click.
                        preview.AllowMotion=()=>true;var timeline=new System.Collections.Generic.List<string>();
                        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
                        var button=game.transform.Find("HomeNavigation/HomeControls/Continue").gameObject;
                        for(var frame=0;frame<28;frame++)
                        {
                            if(frame==10)ExecuteEvents.Execute(button,pointer,ExecuteEvents.pointerDownHandler);
                            if(frame==15)ExecuteEvents.Execute(button,pointer,ExecuteEvents.pointerUpHandler);
                            yield return new WaitForSecondsRealtime(.13f);yield return SaveFrame("Motion_"+frame.ToString("D3")+".png");
                            timeline.Add(Time.realtimeSinceStartup.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                        }
                        System.IO.File.WriteAllLines(System.IO.Path.Combine(OutputDirectory,"Motion_Timestamps.txt"),timeline);
                        ExecuteEvents.Execute(button,pointer,ExecuteEvents.pointerUpHandler);
                    }
                }
            }
            finally{UILanguage.SetPreference(previous,false);}
        }
    }
}
