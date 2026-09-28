using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Save;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Collection;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    /// <summary>GUI evidence using isolated memory accounts; never opens or edits the player's save.</summary>
    [InitializeOnLoad]
    public static class VictoryReview
    {
        private const string Key="Tidebound.VictoryReview";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260927_胜利结算接入");
        private static readonly List<string> checks=new List<string>();private static bool running,exiting;
        static VictoryReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+"language",(int)UILanguage.Preference);EditorApplication.EnterPlaymode();
        }
        private static void Log(string value,string trace,LogType kind)
        {if(!running||(kind!=LogType.Error&&kind!=LogType.Exception))return;Directory.CreateDirectory(Output);File.AppendAllText(Path.Combine(Output,"capture-errors.txt"),value+"\n"+trace+"\n");Finish(1);}
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().FullName=="UnityEditor.GameView")?.Repaint();
            if(running)return;running=true;new GameObject("Victory_ReviewMemorySave").AddComponent<PortraitPuzzleGraybox>().StartCoroutine(Frames());
        }
        private static void Require(bool ok,string note){if(!ok)throw new InvalidOperationException(note);checks.Add("PASS "+note);}
        private static IEnumerator Size(int w,int h)
        {
            typeof(PortraitGrayboxCapture).GetMethod("SetGameViewSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new Vector2Int(w,h)});
            var until=Time.realtimeSinceStartup+15;while((Screen.width!=w||Screen.height!=h)&&Time.realtimeSinceStartup<until)yield return null;
            Require(Screen.width==w&&Screen.height==h,"GameView "+w+"x"+h);yield return null;yield return null;
        }
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return new WaitForEndOfFrame();var path=Path.Combine(Output,name+".png");
            if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);var until=Time.realtimeSinceStartup+10;
            while(!File.Exists(path)&&Time.realtimeSinceStartup<until)yield return null;Require(File.Exists(path),"Capture "+name);
        }
        private static PlayerSaveService Won(PlayableLevelCatalog catalog,int level)
        {
            var profile=new PlayerSaveData{CurrentLevel=level,HighestClearedLevel=level-1};
            profile.Settlements=Enumerable.Range(1,level-1).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId=catalog.GetLevelId(n-1),LevelNumber=n,Kind="Victory",Day="2026-09-27",EconomyVersion=BattleCoinRules.CurrentVersion,BattleCoins=n==1?7:80,FirstClearCoins=100}).ToArray();
            profile.Coins=profile.Settlements.Sum(x=>(long)x.BattleCoins+x.FirstClearCoins);profile.Validate();
            var store=new MemoryPlayerSaveStore();store.Save(profile);var service=new PlayerSaveService(store);
            using(var model=SavedGameRuntime.Create(catalog.Load(level-1),level))
            {
                Require(service.Start(model),"Start isolated level "+level);var proof=LevelSolver.Solve(model.Session.Board,new LevelSolverOptions(4000,400000,500));
                Require(proof.Status==LevelSolverStatus.Solved,"Solve level "+level);
                foreach(var id in proof.ShipIds){var op=model.Movement.TryBeginMove(id);Require(op.IsAccepted,"Model move accepted");if(op.Operation.Stage==ShipMoveStage.Traveling)model.Movement.CompleteTravel(op.Operation.OperationId);if(model.Movement.IsBusy)model.Movement.CompleteBlockedFeedback(op.Operation.OperationId);}
                model.Transit.Advance(100);model.Combat.Advance();Require(model.Combat.IsVictorious&&service.Checkpoint(),"Commit real victory receipt "+level);
            }
            if(service.CanClaimFirstBlue)Require(service.Collect(Guid.NewGuid().ToString("N"),"FirstBlue")==CollectionStatus.Saved,"Claim existing first-blue prerequisite in fixture");
            return service;
        }
        private static IEnumerator Frames()
        {
            Directory.CreateDirectory(Output);UILanguage.SetPreference(LanguagePreference.Chinese,false);
            var game=UnityEngine.Object.FindObjectOfType<PortraitPuzzleGraybox>();
            var folder=Path.Combine(Application.dataPath,"Tidebound/Config/Levels/Campaign");var legacy=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            var files=Directory.GetFiles(folder,"*.json").Concat(Directory.GetFiles(legacy,"*.json")).GroupBy(Path.GetFileName).ToDictionary(g=>g.Key,g=>(Func<string>)(()=>File.ReadAllText(g.First())));
            var catalog=PlayableLevelCatalog.FromManifest(File.ReadAllText(Path.Combine(folder,"manifest-100.json")),files);
            yield return Size(390,844);var service=Won(catalog,9);
            game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);game.AudioSettings.PersistPreferences=false;
            game.AudioSettings.MusicSource.volume=0;game.AudioSettings.SoundSource.volume=0;
            yield return null;yield return Capture("01_Home_EnhancedMotion");game.ContinueFromHome();yield return null;yield return null;
            Require(game.ResultView.transform.Find("ResultHeader").GetComponent<RawImage>().texture.width>=1024,"Header imported at full UI resolution");
            Require(game.IsResultReadable&&game.ResultView.DisplayedStars==9,"Home restores nine-star settlement");
            Require(game.CurrentResult.NextRewards.Select(x=>x.Stars).SequenceEqual(new[]{10,15,20}),"Next three thresholds are 10, 15, 20");
            var revision=service.Snapshot.Revision;var coins=service.Coins;
            foreach(var language in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            {
                UILanguage.SetPreference(language,false);
                foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(430,932)})
                {yield return Size(size.x,size.y);yield return Capture("02_Star9_"+language+"_"+size.x+"x"+size.y);}
            }
            UILanguage.SetPreference(LanguagePreference.Chinese,false);
            yield return Size(780,1688);yield return Capture("08_Unity_Retina_780x1688");yield return Size(390,844);
            game.ApplyViewport(new Rect(0,34,390,766),1);yield return Capture("09_SafeArea_Notch");
            var shade=game.ResultView.transform.Find("Celebration").GetComponent<HarborVictoryGraphic>();
            Require(shade.ShadeBounds.yMin==-34&&shade.ShadeBounds.height==844,"Shade covers safe-area margins");game.ApplyViewport(new Rect(0,0,390,844),1);
            game.ResultView.ShowSharePreview();var time=game.ResultView.CelebrationTime;yield return Capture("03_ShareUnavailable");
            game.ContinueFromResult();Require(game.LevelIndex==8&&game.ResultView.CelebrationTime==time,"Share notice freezes celebration and blocks underlying next");game.ResultView.CloseSharePreview();
            game.ResultView.transform.Find("RewardPreview/Reward0").GetComponent<Button>().onClick.Invoke();yield return Capture("04_RewardDetails");game.ResultView.CloseSharePreview();
            Require(service.Snapshot.Revision==revision&&service.Coins==coins,"Reopening details and switching layout grants nothing");
            game.ReturnHome();yield return null;Require(game.IsHomeOpen,"Result returns home");game.ContinueFromHome();yield return null;
            Require(game.IsResultReadable&&game.CurrentResult.TotalStars==9,"Home resumes same saved receipt");
            game.ContinueFromResult();yield return null;Require(game.LevelIndex==9&&!game.IsResultOpen,"Next enters real level 10");yield return Capture("05_NextLevel10");
            foreach(var level in new[]{10,99,100})
            {
                service=Won(catalog,level);if(level==10)service.SelectShowcase("TF_SHOWCASE_H03");game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);game.ContinueFromHome();yield return null;yield return null;
                Require(game.ResultView.GetComponentInChildren<CollectionShipPreview>().DisplayedShowcaseId==service.SelectedShowcaseId,"Settlement uses selected home ship");
                Require(game.IsResultReadable&&game.CurrentResult.TotalStars==level,"Restored star count "+level);yield return Capture("06_Star"+level);
                if(level==100){Require(game.CurrentResult.NextRewards.Count==0&&!game.ResultView.transform.Find("NextLevel").GetComponent<Button>().interactable,"Final level has no phantom rewards or next level");game.ReturnHome();Require(game.IsHomeOpen,"Final result can return home");}
            }
            // Play a genuinely new tutorial win to capture star-flight and the changing total.
            game.Initialize(catalog,new Tidebound.Unity.Ship.ShipMovementTiming(1000,.005f,.01f,.01f,.06f),new Tidebound.Lane.LaneTransitTiming(.3,.005,.005),new Tidebound.Combat.CombatTiming(.005,.08),saveService:new PlayerSaveService(new MemoryPlayerSaveStore()),campaign:true,useHomeNavigation:true);
            game.ContinueFromHome();yield return null;game.ToggleAuto();var end=Time.realtimeSinceStartup+20;while(!game.IsResultReadable&&Time.realtimeSinceStartup<end)yield return null;
            Require(game.IsResultReadable,"New tutorial victory reaches result");
            for(var i=0;i<8;i++){yield return Capture("07_Animation_"+i.ToString("D2"));yield return new WaitForSecondsRealtime(.25f);}
            Require(game.ResultView.DisplayedStars==1,"New victory animation ends with one committed star");
            File.WriteAllLines(Path.Combine(Output,"visual_checks.txt"),checks);Finish(0);
        }
        private static void Finish(int code)
        {if(exiting)return;exiting=true;SessionState.SetBool(Key,false);UILanguage.SetPreference((LanguagePreference)SessionState.GetInt(Key+"language",0),false);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
