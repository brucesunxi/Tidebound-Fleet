using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tidebound.Unity.UI.VisualSamples;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.EditorTools
{
    public static partial class VisualSampleReview
    {
        private static IEnumerator CaptureSceneFrames(VisualSampleGallery g)
        {
            yield return null;yield return null;g.ShowPopup(true);g.SelectTab(3);g.SetScenePreviewProgress(0);g.SetArtworkInspection(false);
            Require(g.SceneRecords.Length==12&&g.SceneRecords.Select(s=>s.id).Distinct().Count()==12,"12 unique scene IDs");
            Require(Enumerable.Range(0,4).Select(i=>g.SceneIndices(i).Length).SequenceEqual(new[]{1,4,2,5}),"Acquisition distribution 1 / 4 / 2 / 5");
            Require(g.SceneCards.All(c=>c.Image.Texture)&&g.SceneCards.Select(c=>c.Image.Texture).Distinct().Count()==12,"12 real distinct textures loaded");
            Require(g.SceneCards.Skip(1).All(c=>c.Image.Texture.width>=900&&c.Image.Texture.height>=1600),"New backgrounds retain source resolution beyond generic icon cap");
            Require(Enumerable.Range(0,12).Count(g.IsScenePreviewOwned)==1,"Default preview owns exactly one scene");
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                SetSize(size);var until=Time.realtimeSinceStartup+12;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
                Require(Screen.width==size.x&&Screen.height==size.y,"Exact viewport "+size);g.ApplyLayout();g.SetEnglish(false);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
                Require(g.SceneCards.All(c=>c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1),"Chinese scene names fit "+size);
                Require(g.SceneDrawTitle.preferredHeight<=g.SceneDrawTitle.rectTransform.rect.height+1,"Chinese draw title visible "+size);
                var level=g.SceneIndices(1);Require(g.SceneCards[level[0]].Root.anchoredPosition.y==g.SceneCards[level[1]].Root.anchoredPosition.y,"Two columns at "+size);
                foreach(var c in g.SceneCards){var ratio=c.Image.Texture.width*c.Image.UV.width/(c.Image.Texture.height*c.Image.UV.height);Require(Mathf.Abs(ratio-c.Image.rectTransform.rect.width/c.Image.rectTransform.rect.height)<.005f,"Thumbnail preserves source aspect "+c.Root.name+" "+size);}
                yield return Save("Scenes_CN_"+size.x+"x"+size.y+"_Top_GameView.png");
                g.SetEnglish(true);yield return Settle();Require(g.SceneCards.All(c=>c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1),"English scene names fit "+size);
                Require(g.SceneHeadingLabels.All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"English group titles fit "+size);
                if(size.y==640)yield return Save("Scenes_EN_360x640_Top_GameView.png");
                g.SetEnglish(false);var title=g.Header.position;var tabs=g.Tabs.position;var draw=g.SceneDraw.position;var stats=g.Statistics.transform.position;
                g.Scroll.verticalNormalizedPosition=0;yield return Settle();
                var cc=new Vector3[4];var vc=new Vector3[4];g.SceneCards[g.SceneIndices(3).Last()].Root.GetWorldCorners(cc);g.Viewport.GetWorldCorners(vc);
                Require(cc[0].y>=vc[0].y-1&&cc[1].y<=vc[1].y+1,"Last card fully above fixed tabs and action "+size);
                Require(title==g.Header.position&&tabs==g.Tabs.position&&draw==g.SceneDraw.position&&stats==g.Statistics.transform.position,"Fixed title statistics tabs draw action "+size);
                yield return Save("Scenes_CN_"+size.x+"x"+size.y+"_Bottom_GameView.png");
            }
            SetSize(new Vector2Int(390,844));yield return Settle();g.ApplyLayout();g.SetEnglish(false);
            foreach(var group in new[]{1,2,3}){g.ScrollSceneGroup(group);yield return Settle();yield return Save("Scenes_CN_390x844_"+new[]{"Initial","Level","Share","Draw"}[group]+"_GameView.png");}
            g.Scroll.verticalNormalizedPosition=1;yield return Settle();RequireRaycast(g.SceneCards[0].Button,"Default scene accepts pointer");RequireRaycast(g.CloseButton,"Close remains above scroll mask");RequireRaycast(g.SceneDrawButton,"Draw preview action accepts pointer");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(g.SceneDrawButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return Settle();Require(g.DrawPopupOpen&&g.DrawCategory==3,"Draw action opens unified scene draw popup without charging");g.CloseDrawPopup();
            g.ScrollSceneGroup(1);yield return Settle();var locked=g.SceneIndices(1)[0];RequireRaycast(g.SceneCards[locked].Button,"Locked scene is inspectable");
            ExecuteEvents.Execute(g.SceneCards[locked].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return Settle();
            Require(g.SceneViewerOpen&&g.SelectedScene==0&&Enumerable.Range(0,12).Count(g.IsScenePreviewOwned)==1,"Inspect does not grant ownership or equip");
            Require(g.SceneLargeImage.texture==g.SceneCards[locked].Image.Texture,"Inspection uses original source texture");
            RequireRaycast(g.SceneBackButton,"Inspection return accepts pointer");yield return Save("Scenes_CN_390x844_LockedInspection_GameView.png");
            ExecuteEvents.Execute(g.SceneBackButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(!g.SceneViewerOpen,"Return closes inspection");
            g.SetScenePreviewProgress(25);g.ScrollSceneGroup(1);yield return Settle();
            ExecuteEvents.Execute(g.SceneCards[locked].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return Settle();
            Require(g.SelectedScene==locked&&g.Background.texture==g.SceneCards[locked].Image.Texture,"Owned preview selection changes preview background");
            Require(Enumerable.Range(0,12).Count(g.IsScenePreviewOwned)==3,"Level 25 simulated progress yields default plus two level scenes");
            Require(g.SceneCards[locked].Check.gameObject.activeSelf&&!g.SceneCards[locked].Lock.gameObject.activeSelf,"Using state has green check and no lock");
            yield return Save("Scenes_CN_390x844_Level25Preview_ThreeStates_GameView.png");
            g.ShowPopup(false);yield return Settle();yield return Save("Scenes_CN_390x844_HomeBackgroundPreview_GameView.png");
            g.ShowPopup(true);g.SelectTab(3);g.SetScenePreviewProgress(0);g.SetArtworkInspection(true);g.ScrollSceneGroup(3);yield return Settle();
            Require(Enumerable.Range(0,12).Count(g.IsScenePreviewOwned)==1,"Color inspection never unlocks scenes");g.SetArtworkInspection(false);
            g.Scroll.verticalNormalizedPosition=1;yield return Settle();var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=new Vector2(180,340),pressPosition=new Vector2(180,340)};
            var root=ExecuteEvents.GetEventHandler<IDragHandler>(g.SceneCards[0].Button.gameObject);var before=g.SceneContent.anchoredPosition;
            ExecuteEvents.Execute(root,drag,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(root,drag,ExecuteEvents.beginDragHandler);drag.position+=Vector2.up*120;drag.delta=Vector2.up*120;ExecuteEvents.Execute(root,drag,ExecuteEvents.dragHandler);ExecuteEvents.Execute(root,drag,ExecuteEvents.endDragHandler);g.Scroll.StopMovement();
            Require(g.SceneContent.anchoredPosition!=before,"Scene card drag routes to shared ScrollRect");
            ExecuteEvents.Execute(g.CategoryButtons[2].gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.Scroll.content==g.Content&&g.Content.gameObject.activeSelf&&g.SceneDraw.gameObject.activeSelf,"Showcase tab restores four-column list and shared draw entry");
            ExecuteEvents.Execute(g.CategoryButtons[3].gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.Scroll.content==g.SceneContent&&g.SceneContent.gameObject.activeSelf,"Scene tab restores two-column list");
            ExecuteEvents.Execute(g.CloseButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(!g.PopupOpen,"Close works from scene tab");
            g.ShowPopup(true);g.SelectTab(3);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
            yield return CaptureSceneComparison(g);
            File.WriteAllText(Path.Combine(Output,"scene_checks.txt"),string.Join("\n",checks));
            SessionState.SetBool(Key,false);SessionState.SetBool(Key+".Scenes",false);capturing=false;Status("COMPLETE SCENES: "+checks.Count+" focused checks; real Game View screenshots.");
        }
        private static IEnumerator CaptureSceneComparison(VisualSampleGallery g)
        {
            var textures=new List<Texture2D>();var target=ReadTexture(Path.Combine(Repo,"30_设计与素材库/场景/20260923_主页场景/目标_皮肤弹窗-场景.png"),textures);var sample=ReadTexture(Path.Combine(Output,"Scenes_CN_390x844_Top_GameView.png"),textures);
            g.PreviewCanvas.gameObject.SetActive(false);var board=new GameObject("SceneComparison_NotProductUI",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var bg=VisualSampleGallery.Rect("Paper",board.transform).gameObject.AddComponent<Image>();VisualSampleGallery.Fill(bg.rectTransform);bg.color=new Color(.94f,.94f,.91f);SetSize(new Vector2Int(824,948));yield return Settle();
            CompareLabel(board.transform,"目标稿 · 同为390宽",16,8,390,32,18);CompareLabel(board.transform,"场景小样 · Unity Game View",418,8,390,32,18);
            Crop(board.transform,target,new Rect(0,0,target.width,target.height),new Vector2(16,47),390f/target.width);Crop(board.transform,sample,new Rect(0,0,390,844),new Vector2(418,47),1);
            CompareLabel(board.transform,"已加入获取来源分区；默认1/12。原始截图未修饰；不沿用参考图的星星与3500定价。",16,905,792,34,13);
            yield return Settle();yield return Save("Comparison_Scenes_390Width.png");UnityEngine.Object.Destroy(board);foreach(var t in textures)UnityEngine.Object.Destroy(t);
            SetSize(new Vector2Int(390,844));yield return Settle();g.PreviewCanvas.gameObject.SetActive(true);g.ApplyLayout();yield return Settle();
        }
    }
}
