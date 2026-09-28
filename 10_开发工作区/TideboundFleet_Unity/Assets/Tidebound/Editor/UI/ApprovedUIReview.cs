using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Tidebound.LevelDesign;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.UI;
using Tidebound.Unity.UI.VisualSamples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    /// <summary>Exercises production components in the existing review scene with an in-memory save.</summary>
    [InitializeOnLoad]
    public static partial class ApprovedUIReview
    {
        private const string Key="Tidebound.ApprovedUI.Review";
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Work=>Path.Combine(Repo,"99_垃圾存储区/20260923_正式UI接入");
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260923_正式UI接入");
        private static double pollAt;
        private static bool running;
        private static readonly List<string> Checks=new List<string>();
        static ApprovedUIReview(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
        private static void Status(string value){Directory.CreateDirectory(Work);File.WriteAllText(Path.Combine(Work,"status.txt"),value);}
        private static void Log(string value,string trace,LogType type)
        {
            if(type!=LogType.Exception&&type!=LogType.Error)return;
            Directory.CreateDirectory(Work);File.AppendAllText(Path.Combine(Work,"errors.log"),value+"\n"+trace+"\n");
            if(running){SessionState.SetBool(Key,false);running=false;Status("FAILED: "+value);}
        }
        private static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(EditorApplication.timeSinceStartup>pollAt)
            {
                pollAt=EditorApplication.timeSinceStartup+1;var p=Path.Combine(Work,"request.txt");
                if(File.Exists(p))
                {
                    var action=File.ReadAllText(p).Trim();File.Delete(p);
                    try
                    {
                        if(action=="build-home")
                        {if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop review first.");ApprovedHomePrefabs.Build();Status("HOME PREFABS BUILT");}
                        else if(action=="capture-home")
                        {
                            if(EditorSceneManager.GetActiveScene().path!=VisualSampleReview.ScenePath)throw new InvalidOperationException("Use the existing VisualSamples scene; user scene preserved.");
                            SessionState.SetBool(Key,true);running=false;Status("STARTING HOME REVIEW");if(!EditorApplication.isPlaying)EditorApplication.EnterPlaymode();
                        }
                        else if(action=="stop"){SessionState.SetBool(Key,false);running=false;EditorApplication.ExitPlaymode();Status("STOPPED");}
                    }
                    catch(Exception e){Status("FAILED: "+e);Debug.LogException(e);}
                }
            }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
            EditorApplication.QueuePlayerLoopUpdate();EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Repaint();
            if(running)return;running=true;Checks.Clear();
            var old=UnityEngine.Object.FindObjectOfType<VisualSampleGallery>();if(old)old.gameObject.SetActive(false);
            var game=new GameObject("ApprovedProductionUI_ReviewMemorySave").AddComponent<PortraitPuzzleGraybox>();
            game.StartCoroutine(HomeFrames(game));
        }
        private static void Require(bool yes,string message){if(!yes)throw new InvalidOperationException(message);Checks.Add("PASS "+message);}
        private static IEnumerator Size(Vector2Int size)
        {
            typeof(PortraitGrayboxCapture).GetMethod("SetGameViewSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{size});
            var until=Time.realtimeSinceStartup+10;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
            Require(Screen.width==size.x&&Screen.height==size.y,"Exact Game View "+size);yield return null;yield return null;
        }
        private static IEnumerator Save(string name)
        {
            Directory.CreateDirectory(Output);yield return new WaitForEndOfFrame();var path=Path.Combine(Output,name+".png");if(File.Exists(path))File.Delete(path);
            ScreenCapture.CaptureScreenshot(path);var until=Time.realtimeSinceStartup+10;while(!File.Exists(path)&&Time.realtimeSinceStartup<until)yield return null;Require(File.Exists(path),"Capture "+name);
        }
        private static IEnumerator HomeFrames(PortraitPuzzleGraybox game)
        {
            const string folder=Phase5RTenLevelExport.Folder;
            var manifest=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"/manifest.json");
            var files=Directory.GetFiles(folder,"*.json").ToDictionary(Path.GetFileName,p=>(Func<string>)(()=>File.ReadAllText(p)));
            var catalog=PlayableLevelCatalog.FromManifest(manifest.text,files);var store=new MemoryPlayerSaveStore();var service=new PlayerSaveService(store);
            var language=UILanguage.Preference;
            foreach(var locale in new[]{LanguagePreference.Chinese,LanguagePreference.English})
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                UILanguage.SetPreference(locale,false);yield return Size(size);game.Initialize(catalog,saveService:service,campaign:true,useHomeNavigation:true);yield return null;yield return null;
                var root=game.transform.Find("UI_MainMenu");var main=root.Find("MainActionButton/UI_Button_Main").GetComponent<Button>();
                var corners=new Vector3[4];main.GetComponent<RectTransform>().GetWorldCorners(corners);
                File.AppendAllText(Path.Combine(Work,"layout.txt"),locale+" "+size+" safe="+Screen.safeArea+" canvas="+root.GetComponent<Canvas>().scaleFactor+" rect="+((RectTransform)root).rect+" main="+string.Join(";",corners.Select(x=>x.ToString()))+"\n");
                Require(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),"Main button inside actual viewport "+locale+size);
                Require(game.IsHomeOpen&&game.Session==null&&service.Snapshot.Attempt==null,"Home browsing does not create an attempt "+locale+size);
                Require(root.Find("TopHUD/Coins/CreamSurface").GetComponent<HarborSliceGraphic>().Texture.name=="Capsule_Cream","Approved cream capsule "+locale+size);
                Require(main.GetComponentInChildren<HarborSliceGraphic>().Texture.name=="CTA_Gold_v3"&&main.GetComponent<HarborHeroButton>()!=null,"Approved hero art with isolated visual motion "+locale+size);
                foreach(var parent in new[]{root.Find("LeftMenu"),root.Find("RightMenu")})
                foreach(var label in parent.GetComponentsInChildren<Text>())Require(label.preferredWidth<=label.rectTransform.rect.width+1,"Entry label fits: "+label.text+" "+size);
                yield return Save("Home_"+locale+"_"+size.x+"x"+size.y+"_GameView");
            }
            UILanguage.SetPreference(LanguagePreference.Chinese,false);yield return Size(new Vector2Int(390,844));yield return null;
            var b=game.transform.Find("UI_MainMenu/MainActionButton/UI_Button_Main").GetComponent<Button>();var rect=(RectTransform)b.transform;var before=rect.position;
            var feedback=b.GetComponent<HarborHeroButton>();var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;
            Require(feedback.IsPressed&&rect.position==before,"Pressed face leaves hit rectangle fixed");yield return Save("Home_MainPressed_GameView");
            ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerUpHandler);b.interactable=false;yield return null;
            Require(!feedback.IsPressed,"Disabled clears held feedback");yield return Save("Home_MainDisabled_GameView");b.interactable=true;
            var coins=service.Coins;ExecuteEvents.Execute(b.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;yield return null;
            Require(!game.IsHomeOpen&&game.Session!=null&&service.Snapshot.Attempt!=null,"Start enters a real saved attempt in isolated store");
            Require(service.Coins==coins,"Starting does not debit coins");game.ReturnHome();yield return null;yield return null;
            Require(game.IsHomeOpen&&service.Snapshot.Attempt!=null,"Return preserves attempt");
            Require(game.transform.Find("UI_MainMenu/MainActionButton/UI_Button_Main/Face/Label").GetComponent<HarborText>().Source=="Continue","Existing attempt displays Continue");
            yield return Save("Home_Continue_GameView");
            var restored=new PlayerSaveService(store);game.Initialize(catalog,saveService:restored,campaign:true,useHomeNavigation:true);game.ContinueFromHome();yield return null;
            Require(!game.IsHomeOpen&&game.Session!=null,"Reloaded isolated save resumes from approved home");game.ReturnHome();yield return null;
            yield return CollectionFrames(game,catalog);
            UILanguage.SetPreference(language,false);Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"home_checks.txt"),Checks);
            SessionState.SetBool(Key,false);running=false;Status("COMPLETE HOME: "+Checks.Count+" checks. Production UI with isolated memory save; no device claim.");
        }
    }
}
