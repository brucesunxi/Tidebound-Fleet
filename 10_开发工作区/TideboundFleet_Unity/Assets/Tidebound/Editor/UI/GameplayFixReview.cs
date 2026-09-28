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
    public static class GameplayFixReview
    {
        private const string Key="Tidebound.GameplayFixReview";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260927_首页连击HUD修复");
        private static readonly List<string> checks=new List<string>();private static bool running,exiting;
        static GameplayFixReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
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
            yield return Size(780,1688);var service=Won(catalog,11);
            game.Initialize(catalog,new Tidebound.Unity.Ship.ShipMovementTiming(1000,.005f,.01f,.01f,.06f),saveService:service,campaign:true,useHomeNavigation:true);
            game.AudioSettings.PersistPreferences=false;game.AudioSettings.MusicSource.volume=0;game.AudioSettings.SoundSource.volume=0;
            yield return null;yield return Capture("01_Home_StartLevel12");
            var balance=service.Coins;game.ContinueFromHome();yield return null;
            Require(game.LevelIndex==11&&!game.IsResultOpen,"Cleared eleven enters fresh twelve, no old receipt");
            Require(service.Coins==balance,"Fresh entrance does not duplicate victory coins");
            yield return Capture("02_HUD_Initial_780x1688");
            var combo=game.GetComponentInChildren<Tidebound.Unity.Boss.ComboBattleView>();
            Canvas.ForceUpdateCanvases();Require(game.GetComponentsInChildren<Text>().Single(t=>t.name=="BattleCoins").canvasRenderer.GetMesh().vertexCount>0,"This-run coins text is actually rendered");
            var solution=game.SolveCurrent();Require(solution.Status==LevelSolverStatus.Solved,"Solve current real board for visual movement review");
            foreach(var id in solution.ShipIds)
            {
                game.ClickShip(id);var until=Time.realtimeSinceStartup+3;while(game.IsBusy&&Time.realtimeSinceStartup<until)yield return null;
                if(new[]{3,6,11}.Contains(combo.State.Count))
                {
                    yield return Capture("03_Combo_"+combo.State.Count);
                    if(combo.State.Count==11)break;
                }
            }
            Require(combo.State.Count>=11,"Live eleven-exit combo reached without synthetic events");
            Canvas.ForceUpdateCanvases();Require(combo.transform.Find("ComboBadge/ComboCount").GetComponent<Text>().canvasRenderer.GetMesh().vertexCount>0,"Combo number and label are actually rendered");
            // Let early ships arrive, retaining the real exit-chain countdown.
            yield return new WaitForSecondsRealtime(.4f);
            for(var i=0;i<6;i++){yield return Capture("04_FlameMotion_"+i);yield return new WaitForSecondsRealtime(.12f);}
            game.TogglePause();var at=combo.AnimationTime;var remaining=combo.State.Remaining;yield return new WaitForSecondsRealtime(.2f);
            Require(combo.AnimationTime==at&&combo.State.Remaining==remaining,"Pause freezes combo clock and timer");game.TogglePause();
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(430,932)})
            {yield return Size(size.x,size.y);yield return Capture("05_HUD_"+size.x+"x"+size.y);}
            UILanguage.SetPreference(LanguagePreference.English,false);yield return Capture("06_HUD_English");UILanguage.SetPreference(LanguagePreference.Chinese,false);
            yield return new WaitForSecondsRealtime(5.3f);Require(combo.State.Count==0,"Five seconds without exit clears chain");
            yield return Capture("07_ComboExpired");
            game.ReturnHome();Require(game.IsHomeOpen,"Leave incomplete level to home");var old=game.Session.SessionId;
            game.ContinueFromHome();Require(game.Session.SessionId!=old&&game.LevelIndex==11,"Home restarts same incomplete level with new attempt");
            Require(game.SaveService.Runtime.Capture().Departures.Length==0&&game.SaveService.Runtime.PendingCoins==0,"New attempt clears departures and pending coins");
            File.WriteAllLines(Path.Combine(Output,"visual_checks.txt"),checks);Finish(0);
        }
        private static void Finish(int code)
        {if(exiting)return;exiting=true;SessionState.SetBool(Key,false);UILanguage.SetPreference((LanguagePreference)SessionState.GetInt(Key+"language",0),false);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
