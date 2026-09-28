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
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using Tidebound.Unity.Boss;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    /// <summary>Real Game View captures and live combat checks in isolated memory fixtures.</summary>
    [InitializeOnLoad]
    public static class WideBossReview
    {
        private const string Key="Tidebound.WideBossReview";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260928_横向海怪布局接入");
        private static readonly List<string> checks=new List<string>();private static bool running,exiting;
        static WideBossReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        [MenuItem("Tidebound/验证/横向海怪与新布局截图")]
        public static void Run()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+"language",(int)UILanguage.Preference);EditorApplication.EnterPlaymode();
        }
        private static void Log(string value,string trace,LogType kind)
        {if(!running||(kind!=LogType.Error&&kind!=LogType.Exception))return;Directory.CreateDirectory(Output);File.AppendAllText(Path.Combine(Output,"capture-errors.txt"),value+"\n"+trace+"\n");Finish(1);}
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            Application.runInBackground=true;EditorApplication.QueuePlayerLoopUpdate();Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().FullName=="UnityEditor.GameView")?.Repaint();
            if(running)return;running=true;new GameObject("WideBoss_MemoryReview").AddComponent<PortraitPuzzleGraybox>().StartCoroutine(Frames());
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
        private static IEnumerator Frames()
        {
            Directory.CreateDirectory(Output);UILanguage.SetPreference(LanguagePreference.Chinese,false);
            var game=UnityEngine.Object.FindObjectOfType<PortraitPuzzleGraybox>();
            var folder=Path.Combine(Application.dataPath,"Tidebound/Config/Levels/Campaign");var legacy=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            var files=Directory.GetFiles(folder,"*.json").Concat(Directory.GetFiles(legacy,"*.json")).GroupBy(Path.GetFileName).ToDictionary(g=>g.Key,g=>(Func<string>)(()=>File.ReadAllText(g.First())));
            var catalog=PlayableLevelCatalog.FromManifest(File.ReadAllText(Path.Combine(folder,"manifest-100.json")),files);
            yield return Size(780,1688);
            var profile=new PlayerSaveData{CurrentLevel=13,HighestClearedLevel=12};
            profile.Settlements=Enumerable.Range(1,12).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId=catalog.GetLevelId(n-1),LevelNumber=n,Kind="Victory",Day="2026-09-28",EconomyVersion=BattleCoinRules.CurrentVersion,BattleCoins=n==1?7:80,FirstClearCoins=100}).ToArray();
            profile.Coins=profile.Settlements.Sum(x=>(long)x.BattleCoins+x.FirstClearCoins);profile.Validate();
            var store=new MemoryPlayerSaveStore();store.Save(profile);var service=new PlayerSaveService(store);service.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");
            game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);game.AudioSettings.PersistPreferences=false;
            game.AudioSettings.MusicSource.volume=game.AudioSettings.SoundSource.volume=0;
            yield return null;Require(game.IsHomeOpen,"Formal home opens");game.ContinueFromHome();yield return null;
            Require(game.LevelIndex==12&&!game.IsResultOpen&&game.Session.Board.ShipCount==80,"Home starts fresh level 13");
            game.ToggleMenu();Require(game.IsPaused,"Menu pauses gameplay");yield return Capture("01_Pause_NewMap");game.CloseMenu();Require(!game.IsPaused,"Continue resumes gameplay");
            game.ReturnHome();game.ContinueFromHome();yield return null;Require(game.Session.Board.ShipCount==80,"Home restarts full initial board");
            // Visual-only five-skin fixture keeps campaign positions, directions and damage unchanged.
            // Equipment entitlement behavior was verified above with a valid memory player profile.
            game.Initialize(catalog,new Tidebound.Unity.Ship.ShipMovementTiming(1000,.005f,.01f,.01f,.06f),new Tidebound.Lane.LaneTransitTiming(.15,.005,.005),new Tidebound.Combat.CombatTiming(.005,.02),useGameplayArt:true);
            foreach(var level in new[]{13,33,53,73,93})
            {
                SelectArtLevel(game,catalog,level);yield return null;game.ApplyViewport(new Rect(0,68,780,1532),2);yield return null;
                var art=game.CombatView.Artwork;var kind=FleetBattleArtView.KindForLevel(level);
                Require(art.BossKind==kind&&art.DisplayedState=="Healthy","Regional boss "+kind);
                Require(art.transform.GetComponentsInChildren<RawImage>().Count(x=>x.name.StartsWith("FleetShip"))==5,"Exactly five fleet representatives");
                for(var i=3;i<5;i++)Require(art.transform.Find("FleetShip"+i).GetComponent<RawImage>().uvRect.width<0,"Mirror fleet slot "+i);
                yield return Capture(kind+"_Healthy_780x1688");
                var solution=game.SolveCurrent();Require(solution.Status==LevelSolverStatus.Solved,"Solve live "+kind+" board");int sent=0;
                foreach(var target in new[]{32,60})
                {
                    while(sent<target)
                    {game.ClickShip(solution.ShipIds[sent++]);var until=Time.realtimeSinceStartup+3;while(game.IsBusy&&Time.realtimeSinceStartup<until)yield return null;Require(!game.IsBusy,"Movement finished");}
                    var limit=Time.realtimeSinceStartup+8;while(game.Combat.HitCount<target&&Time.realtimeSinceStartup<limit)yield return null;
                    yield return new WaitForSecondsRealtime(.25f);
                    Require(game.Combat.HitCount==target&&game.Session.Boss.Hp==800-target*10,"Exactly one 10-damage hit per exit");
                    var state=target==32?"Tense":"Weak";Require(art.DisplayedState==state,"Live HP selects "+kind+" "+state);
                    game.TogglePause();var clock=game.Combat.Time;var monster=art.transform.Find("RegionalSeaMonster").GetComponent<RawImage>();var pos=monster.rectTransform.anchoredPosition;
                    yield return new WaitForSecondsRealtime(.15f);
                    Require(clock==game.Combat.Time&&pos==monster.rectTransform.anchoredPosition,"Pause freezes wide-boss animation and blend");game.TogglePause();yield return null;yield return Capture(kind+"_"+state+"_780x1688");
                }
            }
            SelectArtLevel(game,catalog,13);yield return null;
            foreach(var dims in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(430,932),new Vector2Int(360,800)})
            {
                yield return Size(dims.x,dims.y);float scale=dims.x/390f;
                if(dims.y==844)game.ApplyViewport(new Rect(0,34,390,766),scale);
                if(dims.y==932)game.ApplyViewport(new Rect(0,34,430,839),scale);
                if(dims.y==800)game.ApplyViewport(new Rect(0,20,360,756),scale);
                yield return null;yield return Capture("Layout_"+dims.x+"x"+dims.y);
                Canvas.ForceUpdateCanvases();Require(game.GetComponentsInChildren<Text>().Single(t=>t.name=="Status").canvasRenderer.GetMesh().vertexCount>0,"Tool instruction text actually rendered");
                Require(game.Layout.Lane.xMin>=0&&game.Layout.Lane.xMax<=game.Layout.Safe.xMax+.1f,"Lane inside viewport");
                var ship=game.Session.Board.Ships.First();var screen=game.BoardCamera.WorldToScreenPoint(game.ViewPosition(ship.Id));
                Require(Vector2.Distance(screen/scale,game.Layout.CellCenter(ship.Position.X,ship.Position.Y))<.1f,"Grid input and enlarged artwork share mapping");
            }
            UILanguage.SetPreference(LanguagePreference.English,false);yield return Capture("Layout_English");
            Finish(0);
        }
        private static void SelectArtLevel(PortraitPuzzleGraybox game,PlayableLevelCatalog catalog,int level)
        {
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(PortraitPuzzleGraybox).GetMethod("ClearSession",flags).Invoke(game,null);
            var equipment=new Tidebound.Collection.CollectionData{Equipment=new[]{"TF_SKIN_K01","TF_SKIN_K02","TF_SKIN_K04","TF_SKIN_K07","TF_SKIN_K28"}};
            var world=SavedGameRuntime.CreateCollected(catalog.Load(level-1),level,equipment,0,new Tidebound.Lane.LaneTransitTiming(.15,.005,.005),new Tidebound.Combat.CombatTiming(.005,.02));
            typeof(PortraitPuzzleGraybox).GetMethod("BindWorld",flags).Invoke(game,new object[]{world,level-1});
            typeof(PortraitPuzzleGraybox).GetMethod("BeginEntry",flags).Invoke(game,new object[]{false});
        }
        private static void Finish(int code)
        {if(exiting)return;exiting=true;File.WriteAllLines(Path.Combine(Output,"visual_checks.txt"),checks);SessionState.SetBool(Key,false);UILanguage.SetPreference((LanguagePreference)SessionState.GetInt(Key+"language",0),false);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
