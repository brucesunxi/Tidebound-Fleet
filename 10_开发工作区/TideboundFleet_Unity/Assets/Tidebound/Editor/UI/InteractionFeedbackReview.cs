using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Save;
using Tidebound.Tools;
using Tidebound.Collection;
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
    public static class InteractionFeedbackReview
    {
        private const string Key="Tidebound.InteractionFeedbackReview";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260928_连击抽奖道具修复");
        private static readonly List<string> checks=new List<string>();private static bool running,exiting;
        static InteractionFeedbackReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        [MenuItem("Tidebound/验证/连击抽奖与道具反馈截图")]
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
            if(running)return;running=true;new GameObject("Feedback_MemoryReview").AddComponent<PortraitPuzzleGraybox>().StartCoroutine(Frames());
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
        [Serializable] private sealed class Proof {public Step[] solutionSteps;}
        [Serializable] private sealed class Step {public string shipId;}
        private static PlayerSaveService Profile(PlayableLevelCatalog catalog,int cleared)
        {
            var data=new PlayerSaveData{CurrentLevel=cleared+1,HighestClearedLevel=cleared};
            data.Settlements=Enumerable.Range(1,cleared).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId=catalog.GetLevelId(n-1),LevelNumber=n,Kind="Victory",Day="2026-09-28",EconomyVersion=BattleCoinRules.CurrentVersion,BattleCoins=n==1?7:80,FirstClearCoins=100}).ToArray();
            data.Coins=data.Settlements.Sum(x=>(long)x.BattleCoins+x.FirstClearCoins);data.Validate();var store=new MemoryPlayerSaveStore();store.Save(data);
            var service=new PlayerSaveService(store);service.CollectAppearance(Guid.NewGuid().ToString("N"),"FirstBlue");return service;
        }
        private static IEnumerator Frames()
        {
            Directory.CreateDirectory(Output);UILanguage.SetPreference(LanguagePreference.Chinese,false);
            var game=UnityEngine.Object.FindObjectOfType<PortraitPuzzleGraybox>();
            var folder=Path.Combine(Application.dataPath,"Tidebound/Config/Levels/CampaignV3");
            var files=Directory.GetFiles(Path.Combine(Application.dataPath,"Tidebound/Config"),"*.json",SearchOption.AllDirectories).GroupBy(Path.GetFileName).ToDictionary(g=>g.Key,g=>(Func<string>)(()=>File.ReadAllText(g.First())));
            var catalog=PlayableLevelCatalog.FromManifest(File.ReadAllText(Path.Combine(folder,"manifest-100.json")),files);
            yield return Size(390,844);
            var service=Profile(catalog,12);
            game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);game.AudioSettings.PersistPreferences=false;
            game.AudioSettings.MusicSource.volume=game.AudioSettings.SoundSource.volume=0;
            game.OpenHomeDraw();yield return null;var draw=game.CollectionView.ApprovedGallery.CommerceDraw;draw.SelectPool(DrawPool.Trail);yield return null;
            var coins=service.Coins;yield return Capture("01_DrawPool");
            draw.BeginDraw();draw.BeginDraw();yield return null;
            Require(draw.Page=="result"&&service.Coins==coins-300,"One draw click immediately grants and debits exactly once");
            Require(!draw.GetComponentsInChildren<Button>(true).Any(b=>b.name=="ConfirmDraw"),"No confirmation page");yield return Capture("02_ImmediateDrawReward");
            game.CloseCollection();
            service=Profile(catalog,2);game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);
            game.AudioSettings.PersistPreferences=false;game.AudioSettings.MusicSource.volume=game.AudioSettings.SoundSource.volume=0;
            game.OpenHomeDraw();yield return null;draw=game.CollectionView.ApprovedGallery.CommerceDraw;draw.SelectPool(DrawPool.Showcase);coins=service.Coins;
            draw.BeginDraw();yield return null;Require(game.IsAcquisitionOpen&&game.ShopPanel.ActiveTab==1,"Insufficient coins directly opens actual coin recharge tab");
            Require(service.Coins==coins&&service.Snapshot.Appearance.UniqueReceipts.Length==0,"Insufficient coins makes no receipt and no debit");yield return Capture("03_InsufficientCoinsRecharge");
            game.CloseAcquisition();yield return null;Require(draw.gameObject.activeInHierarchy&&draw.Pool==DrawPool.Showcase,"Closing recharge returns to the same pool");game.CloseCollection();
            // Actual Campaign V3 placements and published proof; temporary timing accelerates visual validation only.
            game.Initialize(catalog,new Tidebound.Unity.Ship.ShipMovementTiming(1000,.005f,.01f,.01f,.06f),new Tidebound.Lane.LaneTransitTiming(.15,.005,.005),useGameplayArt:true);
            game.SelectLevel(12);yield return null;
            var proof=JsonUtility.FromJson<Proof>(File.ReadAllText(Path.Combine(folder,"TF_V3_013.solution.json")));var captured3=false;
            foreach(var step in proof.solutionSteps)
            {
                game.ClickShip(step.shipId);var until=Time.realtimeSinceStartup+4;while(game.IsBusy&&Time.realtimeSinceStartup<until)yield return null;
                Require(!game.IsBusy,"Proof move finished");yield return null;var combo=game.CombatView.Artwork.Combo;
                if(!captured3&&combo.State.Count>=3){yield return Capture("04_Combo3_390x844");captured3=true;}
                if(combo.State.Count>=10)
                {
                    yield return Capture("05_Combo10_390x844");
                    game.TogglePause();var clock=combo.AnimationTime;yield return new WaitForSecondsRealtime(.15f);Require(combo.AnimationTime==clock,"Pause freezes combo clock");
                    yield return Size(360,640);yield return Capture("06_Combo10_360x640");game.TogglePause();break;
                }
            }
            Require(captured3,"Actual dynamic campaign exits trigger combo");yield return Size(390,844);
            // Use the shipped level 3 pure-exit board: dynamic levels may legitimately reject an unproven shuffle.
            game.SelectLevel(2);yield return Size(780,1688);yield return null;
            var before=game.Session.Board.Ships.ToDictionary(s=>s.Id,s=>s.Direction);var stock=game.Tools.Remaining(ShipTool.Shuffle);
            game.SelectTool(ShipTool.Shuffle);yield return null;Require(game.Tools.Remaining(ShipTool.Shuffle)==stock-1,"Campaign shuffle consumed once; stock="+stock+" remaining="+game.Tools.Remaining(ShipTool.Shuffle)+" paused="+game.IsPaused+" busy="+game.IsBusy);
            var changed=game.Session.Board.Ships.Where(s=>before[s.Id]!=s.Direction).Select(s=>"Ship_"+s.Id).ToArray();
            var highlights=game.GetComponentsInChildren<ShipFloatPresentation>().Where(x=>x.ShuffleHighlightRemaining>0).ToArray();
            Require(highlights.Select(x=>x.transform.parent.name).OrderBy(x=>x).SequenceEqual(changed.OrderBy(x=>x)),"Red outlines match changed directions exactly");
            Require(changed.Length>0,"At least one direction changed");yield return Capture("07_Shuffle_RedOutlines");
            yield return new WaitForSecondsRealtime(3.1f);Require(highlights.All(x=>x.ShuffleHighlightRemaining==0),"Red outline removed after three seconds");yield return Capture("08_Shuffle_After3Seconds");
            yield return Size(390,844);
            game.Initialize(catalog,new Tidebound.Unity.Ship.ShipMovementTiming(2,1,3,.2f,.06f),useGameplayArt:true);game.SelectLevel(12);yield return null;game.ClickShip(proof.solutionSteps[0].shipId);yield return null;
            Require(game.IsBusy,"Real-time campaign ship is moving");var uses=game.Tools.UsesLeft;
            foreach(var name in new[]{"Rescue","Shuffle","Reverse"})
            {
                var button=game.GetComponentsInChildren<Button>().Single(b=>b.name==name);Require(button.interactable,"Tool stays bright while moving: "+name);
                Require(button.transform.Find("ApprovedGoldTool").GetComponent<Graphic>().color==Color.white,"Tool plate stays full color: "+name);button.onClick.Invoke();
            }
            Require(game.Tools.UsesLeft==uses,"Busy clicks consume no tools");yield return Capture("09_Sailing_ToolsRemainBright");
            Finish(0);
        }
        private static void Finish(int code)
        {if(exiting)return;exiting=true;File.WriteAllLines(Path.Combine(Output,"visual_checks.txt"),checks);SessionState.SetBool(Key,false);UILanguage.SetPreference((LanguagePreference)SessionState.GetInt(Key+"language",0),false);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);}
    }
}
