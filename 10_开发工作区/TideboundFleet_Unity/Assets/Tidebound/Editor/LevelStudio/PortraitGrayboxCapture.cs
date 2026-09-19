using System;
using System.IO;
using System.Linq;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Events;
using Tidebound.Combat;
using System.Reflection;
using Tidebound.Unity.LevelDesign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tidebound.EditorTools
{
    /// <summary>Development review capture using the real Game view. Unity 2022.3 editor-only reflection.</summary>
    [InitializeOnLoad]
    public static class PortraitGrayboxCapture
    {
        private const string Key = "Tidebound.PortraitCapture";
        private static readonly Vector2Int[] Sizes = { new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(430,932),new Vector2Int(390,844) };
        private static int index, stage;
        private static double deadline;
        private static string pendingFile;
        private static EditorWindow gameView;
        static PortraitGrayboxCapture() { stage=SessionState.GetInt(Key+".Stage",0); EditorApplication.update += Tick; }

        [MenuItem("Tools/Tidebound/Capture Portrait Graybox Review")]
        public static void Capture()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before starting a review capture.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PortraitGrayboxScene.EnsureScene();
            EditorSceneManager.OpenScene(PortraitGrayboxScene.ScenePath);
            index=0; deadline=0; stage=0;
            SessionState.SetInt(Key+".Stage",0); SessionState.SetBool(Key+".Background",Application.runInBackground);
            SetGameViewSize(Sizes[0]);
            SessionState.SetBool(Key,true); EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key,false)) return;
            Application.runInBackground = true;
            EditorApplication.QueuePlayerLoopUpdate();
            if (deadline==0) deadline=EditorApplication.timeSinceStartup+180;
            try
            {
                if (EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Portrait capture did not complete.");
                if (stage==3)
                {
                    if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        SessionState.SetBool(Key,false);
                        Application.runInBackground=SessionState.GetBool(Key+".Background",false);
                        Debug.Log("Portrait capture completed: "+OutputDirectory);
                        if (Environment.GetEnvironmentVariable("TIDEBOUND_CAPTURE_EXIT")=="1") EditorApplication.Exit(0);
                    }
                    return;
                }
                if (!EditorApplication.isPlaying) return;
                var game=UnityEngine.Object.FindObjectOfType<PortraitPuzzleGraybox>();
                if (game==null || game.Session==null) return;
                if (stage==0)
                {
                    stage=1;
                    game.StartCoroutine(CaptureFrames(game));
                }
                gameView?.Repaint();
            }
            catch(Exception e)
            {
                SessionState.SetBool(Key,false); Application.runInBackground=SessionState.GetBool(Key+".Background",false); Debug.LogException(e);
                if(Environment.GetEnvironmentVariable("TIDEBOUND_CAPTURE_EXIT")=="1") EditorApplication.Exit(1);
                else EditorApplication.ExitPlaymode();
            }
        }

        private static System.Collections.IEnumerator CaptureFrames(PortraitPuzzleGraybox game)
        {
            // Screen dimensions must be read from the player loop, not EditorApplication.update.
            yield return null;
            foreach(var size in Sizes)
            {
                SetGameViewSize(size);
                var expires=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<expires) yield return null;
                if(Screen.width!=size.x || Screen.height!=size.y) throw new InvalidOperationException("Game view size did not settle.");
                game.SelectLevel(index==3 ? 0 : 1);
                yield return new WaitForSecondsRealtime(.5f);
                yield return SaveFrame($"Level{game.LevelIndex+1}_{size.x}x{size.y}.png");
                if(index<3)
                {
                    foreach(var direction in new[]{ShipDirection.Right,ShipDirection.Up})
                    {
                        game.SelectLevel(1); yield return null;
                        var ship=game.Session.Board.Ships.First(x=>x.Direction==direction && game.Session.Board.QueryForwardPath(x.Id).CanExit);
                        // Pause synchronously at handoff: screenshot/import stalls cannot skip the lane.
                        using(game.Session.Events.Subscribe<ShipExitBoardEvent>(e=>
                        {
                            if(e.Ship.ShipId==ship.Id) game.TogglePause();
                        }))
                        {
                            game.ClickShip(ship.Id); expires=Time.realtimeSinceStartup+5;
                            while(!game.IsPaused && Time.realtimeSinceStartup<expires) yield return null;
                            if(!game.IsPaused) throw new TimeoutException("Lane review ship did not exit.");
                        }
                        yield return SaveFrame($"Lane_{direction}_{size.x}x{size.y}.png");
                    }
                }
                index++;
            }
            foreach(var size in Sizes.Take(3))
            {
                SetGameViewSize(size);var expires=Time.realtimeSinceStartup+10;
                while((Screen.width!=size.x || Screen.height!=size.y) && Time.realtimeSinceStartup<expires)yield return null;
                if(Screen.width!=size.x || Screen.height!=size.y)throw new TimeoutException("Tools viewport did not settle.");
                game.SelectLevel(2);yield return null;yield return SaveFrame($"Tools_Level3_{size.x}x{size.y}.png");
            }
            SetGameViewSize(new Vector2Int(390,844));yield return null;yield return null;
            game.SelectLevel(2);yield return null;
            game.SelectTool(ShipTool.Reverse);yield return SaveFrame("Tools_SelectReverse_390x844.png");
            game.ToggleMenu();yield return SaveFrame("Tools_Menu_390x844.png");game.CloseMenu();
            game.SelectTool(ShipTool.Shuffle);yield return SaveFrame("Tools_Shuffled_390x844.png");
            game.SelectTool(ShipTool.Rescue);game.TogglePause();yield return SaveFrame("Tools_Rescue2_390x844.png");
            game.Restart();yield return SaveFrame("Tools_RestartStock_390x844.png");
            game.SelectTool(ShipTool.Rescue);yield return SaveFrame("Tools_Acquisition_390x844.png");
            // Capture the playable combat loop with the same real movement and transit callbacks.
            game.SelectLevel(0);game.ToggleAuto();
            var combatDeadline=Time.realtimeSinceStartup+20;
            while(!game.Combat.Attacks.Any(t=>t.Stage==AttackStage.InFlight && t.FlightProgress(game.Combat.Time)>=.3f && t.FlightProgress(game.Combat.Time)<=.8f) && Time.realtimeSinceStartup<combatDeadline) yield return null;
            if(game.Combat.InFlightCount==0) throw new TimeoutException("No combat projectile became visible.");
            game.TogglePause();yield return SaveFrame("Combat_Firing_390x844.png");game.TogglePause();
            while(!game.IsCleared && Time.realtimeSinceStartup<combatDeadline) yield return null;
            if(!game.IsCleared) throw new TimeoutException("Combat did not reach victory.");
            yield return SaveFrame("Combat_Victory_390x844.png");
            game.SelectLevel(9);game.ToggleAuto();combatDeadline=Time.realtimeSinceStartup+70;
            while(game.Combat.Fleet.Support.ArrivedCount==0 && Time.realtimeSinceStartup<combatDeadline) yield return null;
            if(game.Combat.Fleet.Support.ArrivedCount==0) throw new TimeoutException("Long support did not arrive.");
            game.TogglePause();yield return SaveFrame("Combat_LongSupport_390x844.png");
            stage=3; SessionState.SetInt(Key+".Stage",3); EditorApplication.ExitPlaymode();
        }

        private static System.Collections.IEnumerator SaveFrame(string filename)
        {
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(OutputDirectory);
            pendingFile=Path.Combine(OutputDirectory,filename);
            if(File.Exists(pendingFile)) File.Delete(pendingFile);
            ScreenCapture.CaptureScreenshot(pendingFile);
            var expires=Time.realtimeSinceStartup+10;
            while(!File.Exists(pendingFile) && Time.realtimeSinceStartup<expires) yield return null;
            if(!File.Exists(pendingFile)) throw new IOException("Screenshot was not written.");
            Debug.Log("Captured "+pendingFile);
        }

        private static string OutputDirectory => Environment.GetEnvironmentVariable("TIDEBOUND_CAPTURE_DIR") ?? Path.Combine(Path.GetTempPath(),"TideboundPortraitReview");

        private static void SetGameViewSize(Vector2Int size)
        {
            var assembly=typeof(Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes",true);
            var instance=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            var group=sizesType.GetProperty("currentGroup").GetValue(instance);
            var groupType=group.GetType(); var count=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null);
            var selected=-1;
            for(var i=0;i<count;i++)
            {
                var item=groupType.GetMethod("GetGameViewSize").Invoke(group,new object[]{i}); var type=item.GetType();
                if((int)type.GetProperty("width").GetValue(item)==size.x && (int)type.GetProperty("height").GetValue(item)==size.y &&
                    type.GetProperty("sizeType").GetValue(item).ToString()=="FixedResolution") {selected=i;break;}
            }
            if(selected<0)
            {
                var mode=Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType",true),"FixedResolution");
                var item=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize",true),mode,size.x,size.y,"Tidebound Review");
                groupType.GetMethod("AddCustomSize").Invoke(group,new[]{item}); selected=count;
            }
            var viewType=assembly.GetType("UnityEditor.GameView",true);
            gameView=EditorWindow.GetWindow(viewType); gameView.Show(); gameView.Focus();
            viewType.GetProperty("selectedSizeIndex").SetValue(gameView,0);
            viewType.GetMethod("SizeSelectionCallback").Invoke(gameView,new object[]{selected,null});
            viewType.BaseType.GetProperty("targetSize",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gameView,(Vector2)size);
            viewType.BaseType.GetMethod("SetFocus",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(gameView,new object[]{true});
            gameView.Repaint();
        }
    }
}
