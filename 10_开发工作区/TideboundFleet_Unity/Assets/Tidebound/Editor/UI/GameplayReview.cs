using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Save;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Collection;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    [InitializeOnLoad]
    public static class GameplayReview
    {
        private const string Key="Tidebound.GameplayReview";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260926_完整游戏接入");
        private static readonly List<string> checks=new List<string>();
        private static bool running,exiting;
        static GameplayReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        [MenuItem("Tidebound/打开完整游戏（100关）")]
        public static void OpenGame()
        {
            if(EditorApplication.isPlaying)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Tidebound/Scenes/Phase5R_PortraitGraybox.unity");
            typeof(PortraitGrayboxCapture).GetMethod("SetGameViewSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new Vector2Int(390,844)});
            EditorApplication.EnterPlaymode();
        }
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+"language",(int)UILanguage.Preference);
            EditorApplication.EnterPlaymode();
        }
        private static void Log(string value,string trace,LogType kind)
        {
            if(!running||(kind!=LogType.Error&&kind!=LogType.Exception))return;
            Directory.CreateDirectory(Output);File.AppendAllText(Path.Combine(Output,"capture-errors.txt"),value+"\n"+trace+"\n");Finish(1);
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().FullName=="UnityEditor.GameView")?.Repaint();
            if(running)return;running=true;
            new GameObject("FullGameplay_ReviewMemorySave").AddComponent<PortraitPuzzleGraybox>().StartCoroutine(Frames());
        }
        private static void Require(bool value,string note){if(!value)throw new InvalidOperationException(note);checks.Add("PASS "+note);}
        private static IEnumerator Size(int w,int h)
        {
            typeof(PortraitGrayboxCapture).GetMethod("SetGameViewSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new Vector2Int(w,h)});
            var until=Time.realtimeSinceStartup+15;while((Screen.width!=w||Screen.height!=h)&&Time.realtimeSinceStartup<until)yield return null;
            Require(Screen.width==w&&Screen.height==h,"GameView "+w+"x"+h);yield return null;yield return null;
        }
        private static IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return new WaitForEndOfFrame();
            var path=Path.Combine(Output,name+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);
            var until=Time.realtimeSinceStartup+10;while(!File.Exists(path)&&Time.realtimeSinceStartup<until)yield return null;
            Require(File.Exists(path),"Capture "+name);
        }
        private static IEnumerator Frames()
        {
            Directory.CreateDirectory(Output);UILanguage.SetPreference(LanguagePreference.Chinese,false);
            var game=UnityEngine.Object.FindObjectOfType<PortraitPuzzleGraybox>();
            var folder=Path.Combine(Application.dataPath,"Tidebound/Config/Levels/Campaign");
            var legacy=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            var files=Directory.GetFiles(folder,"*.json").Concat(Directory.GetFiles(legacy,"*.json")).GroupBy(Path.GetFileName).ToDictionary(g=>g.Key,g=>(Func<string>)(()=>File.ReadAllText(g.First())));
            var catalog=PlayableLevelCatalog.FromManifest(File.ReadAllText(Path.Combine(folder,"manifest-100.json")),files);
            Require(catalog.Count==100,"Shipped catalog contains 100 levels");
            yield return Size(390,844);
            game.Initialize(catalog,saveService:new PlayerSaveService(new MemoryPlayerSaveStore()),campaign:true,useHomeNavigation:true);
            game.AudioSettings.PersistPreferences=false;game.AudioSettings.MusicSource.volume=0;game.AudioSettings.SoundSource.volume=0;
            yield return null;yield return Capture("01_Home");
            game.ContinueFromHome();yield return null;yield return null;Require(!game.IsHomeOpen&&game.Session.Board.ShipCount==7,"Home enters seven-ship tutorial");
            yield return Capture("02_Tutorial");game.ToggleMenu();yield return null;yield return Capture("03_Pause");game.CloseMenu();
            var solution=LevelSolver.Solve(game.Session.Board);
            foreach(var id in solution.ShipIds){game.ClickShip(id);while(game.IsBusy)yield return null;}
            var deadline=Time.realtimeSinceStartup+10;while(!game.IsResultReadable&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(game.IsResultReadable&&game.SaveService.CurrentLevel==2,"Victory settles once and unlocks level two");
            yield return Capture("04_Victory");game.ContinueFromResult();yield return null;yield return null;
            Require(game.LevelIndex==1&&game.Session.Board.ShipCount==80,"Next enters 80-ship level two");
            var attempt=game.Session.SessionId;game.ToggleMenu();game.PauseSettingsView.ExitButton.onClick.Invoke();Require(game.IsHomeOpen,"Pause returns home");
            game.ContinueFromHome();yield return null;Require(game.Session.SessionId==attempt&&!game.IsPaused,"Home resumes same attempt");yield return Capture("05_Level02");
            // Independent visual review; no persistent progress or currency is altered.
            game.Initialize(catalog,useGameplayArt:true);game.SelectLevel(11);yield return null;yield return null;
            yield return Capture("06_Level12");
            var water=game.GetComponentInChildren<HarborLaneGraphic>();var mesh=water.canvasRenderer.GetMesh();Require(mesh.vertexCount>100,"Lane foam and arrows have rendered mesh");
            File.WriteAllText(Path.Combine(Output,"lane_debug.txt"),"rect="+water.rectTransform.rect+" pos="+water.transform.position+" layer="+water.gameObject.layer+" vertices="+mesh.vertexCount+" alpha="+water.canvasRenderer.GetAlpha()+" culled="+water.canvasRenderer.cull+" color="+water.color+" cam="+game.BoardCamera.pixelRect);

            var blocked=game.Session.Board.Ships.First(s=>game.Session.Board.QueryForwardPath(s.Id).IsBlocked);
            var victim=game.GetComponentsInChildren<ShipFloatPresentation>().First(s=>s.transform.parent.name=="Ship_"+blocked.Id);
            game.ClickShip(blocked.Id);deadline=Time.realtimeSinceStartup+5;
            while(victim.ImpactCount==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Require(victim.ImpactCount==1,"Real blocked click starts collision and dizzy feedback");
            yield return new WaitForSecondsRealtime(.10f);yield return Capture("07_Dizzy");while(game.IsBusy)yield return null;
            foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(430,932)})
            {yield return Size(size.x,size.y);yield return Capture("08_Responsive_"+size.x+"x"+size.y);Require(game.Layout.Lane.xMin>=game.Layout.Safe.xMin&&game.Layout.Lane.xMax<=game.Layout.Safe.xMax,"Lane fits safe width "+size);}
            yield return Size(390,844);
            foreach(var i in new[]{20,40,60,99}){game.SelectLevel(i);yield return null;yield return Capture("09_Level"+(i+1).ToString("D3"));}
            // Five genuinely unlocked equipment slots in an isolated, explicitly labelled memory fixture.
            var profile=new PlayerSaveData{CurrentLevel=100,HighestClearedLevel=99};
            profile.Settlements=Enumerable.Range(1,99).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId=catalog.GetLevelId(n-1),LevelNumber=n,Kind="Victory",Day="2026-09-26",EconomyVersion=BattleCoinRules.CurrentVersion,BattleCoins=n==1?7:80,FirstClearCoins=100}).ToArray();
            profile.Coins=profile.Settlements.Sum(x=>(long)x.BattleCoins+x.FirstClearCoins);
            profile.Appearance.Equipment=new[]{"TF_SKIN_K01","TF_SKIN_K02","TF_SKIN_K05","TF_SKIN_K06","TF_SKIN_K07"};
            profile.Validate();var memory=new MemoryPlayerSaveStore();memory.Save(profile);var saved=new PlayerSaveService(memory);saved.Collect(Guid.NewGuid().ToString("N"),"FirstBlue");
            Require(saved.IsAvailable,"Five-slot memory fixture valid");
            game.Initialize(catalog,saveService:saved,campaign:true,useHomeNavigation:true);game.ContinueFromHome();yield return null;
            Require(game.LevelIndex==99&&!game.IsHomeOpen,"Level 100 enters from home with five equipment slots");
            yield return Capture("10_FiveEquipped_MemoryFixture");
            var fourth=game.CombatView.GetComponentInChildren<Tidebound.Unity.Boss.FleetBattleArtView>().transform.Find("FleetShip3").GetComponent<RawImage>();
            var fifth=game.CombatView.transform.Find("FleetShip4").GetComponent<RawImage>();
            Require(fourth.uvRect.width<0&&fifth.uvRect.width<0,"Fourth and fifth side ships are mirrored");
            File.WriteAllLines(Path.Combine(Output,"visual_checks.txt"),checks);Finish(0);
        }
        private static void Finish(int code)
        {
            if(exiting)return;exiting=true;SessionState.SetBool(Key,false);UILanguage.SetPreference((LanguagePreference)SessionState.GetInt(Key+"language",0),false);
            EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
        }
    }
}
