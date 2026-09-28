using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.Save;
using Tidebound.Config;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    /// <summary>Actual product components with an isolated memory save; no scene or player save writes.</summary>
    [InitializeOnLoad]
    public static class SettingsPauseReview
    {
        private const string Key="Tidebound.SettingsPauseReview";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260925_设置暂停正式接入");
        private static readonly List<string> checks=new List<string>();
        private static bool running, exiting;
        static SettingsPauseReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        public static void Run()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+"language",(int)UILanguage.Preference);
            EditorApplication.EnterPlaymode();
        }
        private static void Log(string value,string trace,LogType kind)
        {
            if(!running || (kind!=LogType.Error&&kind!=LogType.Exception))return;
            Directory.CreateDirectory(Output);File.AppendAllText(Path.Combine(Output,"capture-errors.txt"),value+"\n"+trace+"\n");
            Finish(1);
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();
            Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().FullName=="UnityEditor.GameView")?.Repaint();
            if(running)return;running=true;
            new GameObject("SettingsPause_ReviewMemorySave").AddComponent<PortraitPuzzleGraybox>().StartCoroutine(Frames());
        }
        private static void Require(bool value,string note){if(!value)throw new InvalidOperationException(note);checks.Add("PASS "+note);}
        private static IEnumerator Size(int w,int h)
        {
            typeof(PortraitGrayboxCapture).GetMethod("SetGameViewSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new Vector2Int(w,h)});
            var until=Time.realtimeSinceStartup+15;while((Screen.width!=w||Screen.height!=h)&&Time.realtimeSinceStartup<until)yield return null;
            Require(Screen.width==w&&Screen.height==h,"GameView "+w+"x"+h);yield return null;yield return null;
        }
        private static IEnumerator Capture(string name,HarborSettingsPanel panel)
        {
            Canvas.ForceUpdateCanvases();yield return null;yield return new WaitForEndOfFrame();
            foreach(var b in panel.GetComponentsInChildren<Button>().Where(x=>x.gameObject.activeInHierarchy))
            {
                var c=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(c);
                Require(c.All(v=>v.x>=-.5f&&v.y>=-.5f&&v.x<=Screen.width+.5f&&v.y<=Screen.height+.5f),name+" button bounds "+b.name);
            }
            var path=Path.Combine(Output,name+".png");if(File.Exists(path))File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);
            var until=Time.realtimeSinceStartup+10;while(!File.Exists(Path.Combine(Output,name+".png"))&&Time.realtimeSinceStartup<until)yield return null;
            Require(File.Exists(Path.Combine(Output,name+".png")),"Capture "+name);
        }
        private static IEnumerator Frames()
        {
            Directory.CreateDirectory(Output);var game=UnityEngine.Object.FindObjectOfType<PortraitPuzzleGraybox>();
            const string folder=Phase5RTenLevelExport.Folder;
            var manifest=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/manifest.json");
            var files=Directory.GetFiles(folder,"*.json").ToDictionary(Path.GetFileName,p=>(Func<string>)(()=>File.ReadAllText(p)));
            var catalog=PlayableLevelCatalog.FromManifest(manifest.text,files);
            foreach(var language in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640)})
            {
                UILanguage.SetPreference(language,false);yield return Size(size.x,size.y);
                game.Initialize(catalog,saveService:new PlayerSaveService(new MemoryPlayerSaveStore()),campaign:true,useHomeNavigation:true);
                game.AudioSettings.PersistPreferences=false;game.AudioSettings.SetMusic(true);game.AudioSettings.SetSound(true);game.AudioSettings.MusicSource.volume=0;game.AudioSettings.SoundSource.volume=0;
                game.OpenHomeSettings();yield return null;var suffix=language+"_"+size.x+"x"+size.y;
                yield return Capture("Settings_"+suffix,game.HomeSettingsView);
                game.HomeSettingsView.OpenLegal(true);yield return null;yield return Capture("Privacy_"+suffix,game.HomeSettingsView);
                game.HomeSettingsView.CloseLegal();game.HomeSettingsView.OpenLegal(false);yield return null;yield return Capture("Terms_"+suffix,game.HomeSettingsView);
                game.HomeSettingsView.CloseLegal();game.HomeSettingsView.transform.Find("Close").GetComponent<Button>().onClick.Invoke();
                game.ContinueFromHome();yield return null;yield return null;
                game.ToggleMenu();yield return null;yield return Capture("Pause_"+suffix,game.PauseSettingsView);
                Require(game.IsPaused&&game.IsMenuOpen&&!game.PauseSettingsView.SkipButton.interactable,"Paused; skip disabled "+suffix);
                game.PauseSettingsView.ContinueButton.onClick.Invoke();Require(!game.IsPaused&&!game.IsMenuOpen,"Continue "+suffix);
                game.ToggleMenu();var attempt=game.Session.SessionId;game.PauseSettingsView.RestartButton.onClick.Invoke();yield return null;
                Require(game.Session.SessionId!=attempt&&!game.IsMenuOpen,"Direct restart "+suffix);
                game.ToggleMenu();game.PauseSettingsView.ExitButton.onClick.Invoke();Require(game.IsHomeOpen,"Exit to home "+suffix);
            }
            File.WriteAllLines(Path.Combine(Output,"visual_checks.txt"),checks);Finish(0);
        }
        private static void Finish(int code)
        {
            if(exiting)return;exiting=true;SessionState.SetBool(Key,false);
            UILanguage.SetPreference((LanguagePreference)SessionState.GetInt(Key+"language",0),false);
            EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
        }
    }
}
