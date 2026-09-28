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
        private static IEnumerator CaptureDrawFrames(VisualSampleGallery g)
        {
            yield return null;yield return null;g.ShowPopup(true);g.SetArtworkInspection(false);g.SetScenePreviewProgress(0);g.SetPreviewProgress(0);
            var popupId=g.DrawPopup.GetInstanceID();var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                SetSize(size);var until=Time.realtimeSinceStartup+12;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
                Require(Screen.width==size.x&&Screen.height==size.y,"Exact viewport "+size);g.ApplyLayout();g.SetEnglish(false);
                for(var tab=0;tab<4;tab++)
                {
                    g.SelectTab(tab);yield return Settle();Require(g.SceneDrawButton.gameObject.activeInHierarchy,"Shared draw entry visible on category "+tab+" "+size);
                    RequireRaycast(g.SceneDrawButton,"Shared entry receives pointer "+tab+" "+size);
                    Require(g.SceneDrawTitle.preferredHeight<=g.SceneDrawTitle.rectTransform.rect.height+1&&g.SceneDrawSubtitle.preferredHeight<=g.SceneDrawSubtitle.rectTransform.rect.height+1,"Entry text fits "+tab+" "+size);
                    Require(Mathf.Abs(g.DrawEntryCoin.rect.width/g.DrawEntryCoin.rect.height-1052f/1071)<.01f,"Coin ornament keeps aspect "+tab+" "+size);
                    if(size.x==390)yield return Save("Entry_"+tab+"_CN_390x844_GameView.png");
                    if(tab>=2){g.Scroll.verticalNormalizedPosition=0;yield return Settle();var content=tab==3?g.SceneContent:g.Content;var last=tab==3?g.SceneCards[g.SceneIndices(3).Last()].Root:g.Cards[Tidebound.Collection.ShowcaseCatalog.Groups.Last().CatalogIndices.Last()].Root;var c=new Vector3[4];var v=new Vector3[4];last.GetWorldCorners(c);g.Viewport.GetWorldCorners(v);Require(c[0].y>=v[0].y-1,"Final row clears unified footer "+tab+" "+size);}
                    var position=g.Scroll.content.anchoredPosition;
                    ExecuteEvents.Execute(g.SceneDrawButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return Settle();
                    Require(g.DrawPopupOpen&&g.DrawPopup.GetInstanceID()==popupId&&g.DrawCategory==tab,"One unified popup receives source category "+tab+" "+size);
                    Require(g.DrawPoolName.preferredHeight<=g.DrawPoolName.rectTransform.rect.height+1&&g.DrawInfo.preferredHeight<=g.DrawInfo.rectTransform.rect.height+1,"Draw popup text fits "+tab+" "+size);
                    Require(!g.DrawOnceButton.interactable,"Preview cannot spend or grant "+tab+" "+size);
                    RequireRaycast(g.DrawCloseButton,"Popup close receives pointer "+tab+" "+size);
                    // The active modal must intercept input aimed at its underlying collection action.
                    var r=g.SceneDraw;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center))},hits);
                    Require(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(g.DrawPopup),"Popup blocks underlying collection entry "+tab+" "+size);
                    if(tab==3)yield return Save("DrawPopup_CN_"+size.x+"x"+size.y+"_GameView.png");
                    ExecuteEvents.Execute(g.DrawCloseButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return Settle();Require(!g.DrawPopupOpen&&g.SelectedTab==tab&&Vector2.Distance(position,g.Scroll.content.anchoredPosition)<.1f,"Close restores category and scroll "+tab+" "+size);
                }
                g.SelectTab(3);g.Scroll.verticalNormalizedPosition=1;yield return Settle();yield return Save("Entry_Scenes_CN_"+size.x+"x"+size.y+"_GameView.png");
                if(size.y==640)
                {
                    g.SetEnglish(true);yield return Settle();Require(g.SceneDrawTitle.preferredHeight<=g.SceneDrawTitle.rectTransform.rect.height+1,"English entry fits short screen");yield return Save("Entry_Scenes_EN_360x640_GameView.png");
                    g.OpenDrawPopup();yield return Settle();Require(g.DrawInfo.preferredHeight<=g.DrawInfo.rectTransform.rect.height+1,"English popup text fits short screen");yield return Save("DrawPopup_EN_360x640_GameView.png");g.CloseDrawPopup();g.SetEnglish(false);
                }
            }
            SetSize(new Vector2Int(390,844));yield return Settle();g.ApplyLayout();g.SelectTab(3);
            var rect=g.SceneDraw.rect;var pos=g.SceneDraw.position;ExecuteEvents.Execute(g.SceneDrawButton.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return Settle();Require(g.SceneDraw.rect==rect&&g.SceneDraw.position==pos,"Press keeps hit target and layout fixed");yield return Save("Entry_Pressed_390x844_GameView.png");ExecuteEvents.Execute(g.SceneDrawButton.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            g.OpenDrawPopup();for(var i=0;i<4;i++){ExecuteEvents.Execute(g.DrawCategoryButtons[i].gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.DrawCategory==i&&g.SelectedTab==3,"Modal category changes without changing collection origin "+i);}
            ExecuteEvents.Execute(g.DrawBackButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(!g.DrawPopupOpen&&g.SelectedTab==3,"Back button returns to original collection category");
            Require(Enumerable.Range(0,12).Count(g.IsScenePreviewOwned)==1&&Enumerable.Range(0,g.Cards.Length).Count(g.IsPreviewOwned)==1,"Navigation grants no scene or showcase ownership");
            yield return CaptureDrawComparison(g);
            File.WriteAllText(Path.Combine(Output,"draw_checks.txt"),string.Join("\n",checks));SessionState.SetBool(Key,false);SessionState.SetBool(Key+".Draw",false);capturing=false;Status("COMPLETE DRAW: "+checks.Count+" focused checks; unified popup and four category entries.");
        }
        private static IEnumerator CaptureDrawComparison(VisualSampleGallery g)
        {
            var textures=new List<Texture2D>();var before=ReadTexture(Path.Combine(Repo,"40_项目交接文档/验证记录/20260923_场景弹窗小样/Scenes_CN_390x844_Top_GameView.png"),textures);
            var after=ReadTexture(Path.Combine(Output,"Entry_Scenes_CN_390x844_GameView.png"),textures);
            g.PreviewCanvas.gameObject.SetActive(false);var board=new GameObject("DrawButtonComparison_NotAsset",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var bg=VisualSampleGallery.Rect("Paper",board.transform).gameObject.AddComponent<Image>();VisualSampleGallery.Fill(bg.rectTransform);bg.color=new Color(.96f,.94f,.88f);
            SetSize(new Vector2Int(824,294));yield return Settle();CompareLabel(board.transform,"之前 · 场景专用入口",16,5,390,30,18);CompareLabel(board.transform,"优化后 · 四栏共用抽奖入口",418,5,390,30,18);
            Crop(board.transform,before,new Rect(0,636,390,160),new Vector2(16,45),1);Crop(board.transform,after,new Rect(0,636,390,160),new Vector2(418,45),1);
            CompareLabel(board.transform,"均取自390宽Unity Game View原图：固定金币图标，紧凑两行文字，点击打开统一抽奖弹窗。",16,226,792,40,14);
            yield return Settle();yield return Save("DrawEntry_BeforeAfter_390Scale.png");UnityEngine.Object.Destroy(board);foreach(var t in textures)UnityEngine.Object.Destroy(t);
            SetSize(new Vector2Int(390,844));yield return Settle();g.PreviewCanvas.gameObject.SetActive(true);g.ApplyLayout();yield return Settle();
        }
    }
}
