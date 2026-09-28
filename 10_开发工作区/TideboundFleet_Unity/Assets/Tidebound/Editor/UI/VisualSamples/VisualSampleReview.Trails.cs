using System.Collections;
using System.IO;
using System.Linq;
using Tidebound.Unity.UI.VisualSamples;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebound.EditorTools
{
    public static partial class VisualSampleReview
    {
        [MenuItem("Tools/Tidebound/Visual Samples/Review Trail Samples")]
        public static void ReviewTrails()
        {SessionState.SetBool(Key+".Trails",true);Capture();}
        private static IEnumerator CaptureTrailFrames(VisualSampleGallery g)
        {
            yield return Settle();g.ShowPopup(true);g.SelectTab(1);g.SetArtworkInspection(false);
            Require(g.TrailCards.Length==12&&g.TrailCards.Select(x=>x.Image.texture).Distinct().Count()==12,"12 whole independent supplied trail textures");
            Require(Enumerable.Range(0,12).Count(g.IsTrailPreviewOwned)==1,"Only W01 is preview-owned; trial never grants");
            Require(g.TrailCards.Skip(4).All(c=>c.Image.texture.height==1774),"Eight new originals keep native height 1774");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                SetSize(size);var until=Time.realtimeSinceStartup+10;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
                Require(Screen.width==size.x&&Screen.height==size.y,"Exact Game View "+size);g.ApplyLayout();g.SetEnglish(false);g.SelectTab(1);g.SetArtworkInspection(true);yield return Settle();
                Require(g.TrailCards.Skip(1).Take(4).Select(c=>c.Root.anchoredPosition.y).Distinct().Count()==1,"Four columns "+size);
                Require(g.TrailCards.All(c=>c.Name.preferredWidth<=c.Name.rectTransform.rect.width+1&&c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1),"Chinese names fit "+size);
                yield return Save("Trails_CN_"+size.x+"x"+size.y+"_GameView.png");
                var header=g.Header.position;var tabs=g.Tabs.position;g.Scroll.verticalNormalizedPosition=0;yield return Settle();
                var corners=new Vector3[4];var viewport=new Vector3[4];g.TrailCards[11].Root.GetWorldCorners(corners);g.Viewport.GetWorldCorners(viewport);
                Require(corners[0].y>=viewport[0].y-1,"Last trail row clears footer "+size);
                Require(header==g.Header.position&&tabs==g.Tabs.position,"Fixed header and tabs "+size);
                RequireRaycast(g.TrailCards[11].Button,"Last-row trial receives pointer "+size);ExecuteEvents.Execute(g.TrailCards[11].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                Require(g.TrailViewerOpen&&g.SelectedTrail==11&&!g.IsTrailPreviewOwned(11),"Locked trial opens without grant "+size);
                g.TrailVoyage.Manual=true;g.TrailVoyage.Seek(3.1f);yield return Settle();
                Require(g.TrailViewerTitle.preferredHeight<=g.TrailViewerTitle.rectTransform.rect.height&&g.TrailViewerTitle.cachedTextGenerator.vertexCount>4,"Viewer title fits and generates glyphs "+size);
                yield return Save("Trail_W12_Motion_"+size.x+"x"+size.y+"_GameView.png");
                RequireRaycast(g.TrailBack,"Viewer back is reachable "+size);ExecuteEvents.Execute(g.TrailBack.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                Require(!g.TrailViewerOpen&&g.Scroll.verticalNormalizedPosition<.01f,"Back preserves scroll "+size);
                if(size.y==640)yield return Save("Trails_LastRow_360x640_GameView.png");
            }
            SetSize(new Vector2Int(390,844));yield return Settle();g.ApplyLayout();g.SetEnglish(true);g.SelectTab(1);yield return Settle();
            Require(g.TrailCards.All(c=>c.Name.preferredWidth<=c.Name.rectTransform.rect.width+1&&c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1),"English names fit without auto shrinking");
            yield return Save("Trails_EN_390x844_GameView.png");g.SetEnglish(false);
            // Every style uses the same actual ribbon component; capture a representative pose of all supplied art.
            for(var i=0;i<12;i++)
            {
                g.TryTrail(i);g.TrailVoyage.Manual=true;g.TrailVoyage.Seek(3.1f);yield return Settle();
                Require(g.TrailVoyage.Ribbons.All(r=>r.Texture==g.TrailCards[i].Image.texture),"Whole-style switch "+g.TrailRecords[i].id);
                yield return Save("Trail_"+g.TrailRecords[i].id+"_Turn_GameView.png");
            }
            g.TryTrail(0);g.TrailVoyage.Manual=true;g.TrailVoyage.Seek(5.4f);
            var ribbon=g.TrailVoyage.Ribbons[0];Require(ribbon.SampleCount>2,"Real movement grows sampled history");
            var elapsed=g.TrailVoyage.Elapsed;g.TrailVoyage.Paused=true;g.TrailVoyage.Advance(.5f);Require(g.TrailVoyage.Elapsed==elapsed,"Pause freezes simulation");g.TrailVoyage.Paused=false;
            g.TrailVoyage.Seek(7.3f);Require(!g.TrailVoyage.MainMoving&&ribbon.SampleCount==0,"Stop fades fully and blocked shake emits no wake");
            yield return Settle();yield return Save("Trail_StopAndBlocked_GameView.png");
            ribbon.ClearHistory();ribbon.Advance(.02f,Vector2.zero,false);ribbon.Advance(.02f,new Vector2(3,0),false);Require(ribbon.SampleCount==0,"Non-travel displacement never emits");
            for(var i=0;i<5;i++)ribbon.Advance(.02f,new Vector2(i*4,0),true);
            var oldest=ribbon.OldestPosition;ribbon.Advance(.02f,new Vector2(16,4),true);Require(ribbon.OldestPosition==oldest,"Changing heading preserves old history center");
            ribbon.Advance(.02f,new Vector2(10000,10000),true);Require(ribbon.SampleCount==1,"Teleport clears history instead of bridging");
            g.CloseTrailPreview();g.SelectTab(1);g.SetArtworkInspection(false);yield return Settle();yield return Save("Trails_Ownership_GameView.png");
            g.SceneDrawButton.onClick.Invoke();Require(g.DrawPopupOpen&&g.DrawCategory==1&&!g.DrawOnceButton.interactable,"Shared draw route remains safe and unconfigured");g.CloseDrawPopup();
            // Real Game View frame sequence; deterministic 15 fps simulation, never an HTML or generated animation.
            var frames=Path.Combine(Work,"trail_video_frames");Directory.CreateDirectory(frames);
            for(var clip=0;clip<2;clip++)
            {
                g.TryTrail(clip);g.TrailVoyage.Manual=true;g.TrailVoyage.Seek(0);
                for(var frame=0;frame<120;frame++)
                {
                    g.TrailVoyage.Advance(1f/30);g.TrailVoyage.Advance(1f/30);yield return new WaitForEndOfFrame();
                    var path=Path.Combine(frames,"frame_"+(clip*120+frame).ToString("D4")+".png");if(File.Exists(path))File.Delete(path);ScreenCapture.CaptureScreenshot(path);
                    var until=Time.realtimeSinceStartup+5;while(!File.Exists(path)&&Time.realtimeSinceStartup<until)yield return null;
                    if(!File.Exists(path))throw new IOException("Missing Unity video frame "+path);
                }
            }
            Require(Directory.GetFiles(frames,"frame_*.png").Length==240,"240 actual Game View video frames captured");
            g.CloseTrailPreview();g.SelectTab(1);g.SetArtworkInspection(true);g.TrailVoyage.Manual=false;
            File.WriteAllText(Path.Combine(Output,"interaction_checks.txt"),string.Join("\n",checks));
            SessionState.SetBool(Key,false);capturing=false;Status("COMPLETE TRAILS: "+checks.Count+" scoped checks; 12 styles, Game View captures and 240 video frames. Left in Trails.");
        }
    }
}
