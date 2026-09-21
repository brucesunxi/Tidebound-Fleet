using System;
using System.Collections;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEngine;
using UnityEngine.UI;

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
    }
}
