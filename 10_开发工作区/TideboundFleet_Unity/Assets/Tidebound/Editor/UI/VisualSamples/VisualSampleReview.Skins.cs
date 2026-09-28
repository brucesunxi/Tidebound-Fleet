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
        private static IEnumerator CaptureSkinFrames(VisualSampleGallery g)
        {
            yield return Settle();g.ShowPopup(true);g.SelectTab(0);g.SetArtworkInspection(false);g.SetSkinPreviewProgress(0);
            Require(g.SkinCards.Length==31&&g.SkinCards.Select(c=>c.Ship.texture).Distinct().Count()==31,"31 distinct supplied ship textures");
            Require(Enumerable.Range(0,31).Count(g.IsSkinPreviewOwned)==1&&g.SkinEquippedCount==1,"New preview owns/equips default only");
            Require(g.SkinRecords.GroupBy(s=>s.rarity).OrderBy(x=>x.Key).Select(x=>x.Count()).SequenceEqual(new[]{5,11,8,4,3}),"Five grade counts 5/11/8/4/3");
            Require(Enumerable.Range(0,4).Select(x=>g.SkinIndices(x).Length).SequenceEqual(new[]{1,12,4,14}),"Initial/level/share/draw = 1/12/4/14");
            Require(g.SkinCards.All(c=>c.Ship.texture.height>=1700),"Imported native resolution preserved; not reduced to 256");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                SetSize(size);var until=Time.realtimeSinceStartup+10;while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
                Require(Screen.width==size.x&&Screen.height==size.y,"Exact Game View "+size);g.ApplyLayout();
                foreach(var en in new[]{false,true})
                {
                    g.SetEnglish(en);g.SelectTab(0);yield return Settle();
                    Require(g.SkinIndices(1).Take(4).Select(i=>g.SkinCards[i].Root.anchoredPosition.y).Distinct().Count()==1,"Four columns "+size+" "+en);
                    Require(g.SkinCards.All(c=>c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1&&c.State.preferredHeight<=c.State.rectTransform.rect.height+1),"31 names and ownership labels fit "+size+" "+en);
                    Require(g.SkinEquipmentBase.anchoredPosition.x+g.SkinEquipmentBase.rect.width<((RectTransform)g.Statistics.transform.parent).anchoredPosition.x,"Equipment and collection pills do not overlap "+size+" "+en);
                    Require(g.SkinCards.All(c=>c.Name.preferredWidth<=c.Name.rectTransform.rect.width+1),"31 names fit width "+size+" "+en);
                    yield return Save("Skins_"+(en?"EN":"CN")+"_"+size.x+"x"+size.y+"_GameView.png");
                }
                g.SetEnglish(false);var header=g.Header.position;var tabs=g.Tabs.position;g.Scroll.verticalNormalizedPosition=0;yield return Settle();
                var corners=new Vector3[4];var viewport=new Vector3[4];g.SkinCards[g.SkinIndices(3).Last()].Root.GetWorldCorners(corners);g.Viewport.GetWorldCorners(viewport);
                Require(corners[0].y>=viewport[0].y-1,"Final row is not covered by footer "+size);
                Require(header==g.Header.position&&tabs==g.Tabs.position,"Title and tabs fixed while scrolling "+size);
                RequireRaycast(g.SceneDrawButton,"Shared draw entry receives input "+size);ExecuteEvents.Execute(g.SceneDrawButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                Require(g.DrawPopupOpen&&!g.PopupOpen&&g.DrawCategory==0&&g.DrawCount.text.Contains("14"),"Skin entry opens shared draw with 14 preview rewards "+size);
                Require(!g.DrawOnceButton.interactable,"Preview cannot spend/grant "+size);g.CloseDrawPopup();Require(g.SelectedTab==0&&g.Scroll.verticalNormalizedPosition<.01f,"Draw return preserves skin list "+size);
                if(size.y==640)yield return Save("Skins_CN_360x640_LastRow_GameView.png");
            }
            SetSize(new Vector2Int(390,844));yield return Settle();g.ApplyLayout();g.SetEnglish(false);g.SelectTab(0);yield return Settle();
            RequireRaycast(g.SkinCards[0].Inspect,"Magnifier has separate pointer target");ExecuteEvents.Execute(g.SkinCards[0].Inspect.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Require(g.SkinViewerOpen&&g.SkinEquippedCount==1,"Inspect does not toggle equipment");g.SkinBackButton.onClick.Invoke();
            g.ToggleSkin(17);Require(g.SkinViewerOpen&&!g.IsSkinPreviewOwned(17),"Locked click inspects, never grants");yield return Save("Skins_LockedDetail_GameView.png");g.SkinBackButton.onClick.Invoke();
            g.SetSkinPreviewProgress(20);foreach(var i in g.SkinIndices(1).Take(4))g.ToggleSkin(i);Require(g.SkinEquippedCount==5,"Five distinct skins can equip");
            var sixth=g.SkinIndices(1)[4];g.ToggleSkin(sixth);Require(g.SkinEquippedCount==5&&!g.IsSkinEquipped(sixth),"Sixth equip rejected without disturbing slots");
            g.ToggleSkin(0);g.ToggleSkin(sixth);Require(g.SkinEquippedCount==5&&g.IsSkinEquipped(sixth),"Unequip then replace slot");
            g.SetSkinPreviewProgress(0);g.ToggleSkin(0);Require(g.SkinEquippedCount==0&&g.Hint.text.Contains("默认白船"),"Empty equipment allowed; original default fallback is explained");g.ToggleSkin(0);
            g.SetSkinPreviewProgress(10);g.ToggleSkin(4);g.ScrollSkinGroup(1);yield return Settle();yield return Save("Skins_Level10_ThreeStates_GameView.png");
            var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=new Vector2(180,380),pressPosition=new Vector2(180,380)};
            var dragRoot=ExecuteEvents.GetEventHandler<IDragHandler>(g.SkinCards[4].Button.gameObject);Require(dragRoot==g.Viewport.gameObject,"Skin card drag bubbles to ScrollRect");
            ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.beginDragHandler);var before=g.SkinContent.anchoredPosition;drag.position+=Vector2.up*100;drag.delta=Vector2.up*100;ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.dragHandler);ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.endDragHandler);g.Scroll.StopMovement();Require(g.SkinContent.anchoredPosition!=before,"EventSystem drag moves skin content");
            g.SetSkinPreviewProgress(0);g.SetArtworkInspection(true);
            for(var group=1;group<4;group++){g.ScrollSkinGroup(group);yield return Settle();yield return Save("Skins_Artwork_Group"+group+"_GameView.png");}
            g.Scroll.verticalNormalizedPosition=0;yield return Settle();yield return Save("Skins_Artwork_DrawLast_GameView.png");
            Require(Enumerable.Range(0,31).Count(g.IsSkinPreviewOwned)==1,"Artwork inspection never grants skins");
            g.SetArtworkInspection(false);g.SelectTab(0);g.OpenDrawPopup();yield return Settle();yield return Save("Skins_UnifiedDraw_GameView.png");g.CloseDrawPopup();
            for(var tab=1;tab<4;tab++){g.SelectTab(tab);g.SceneDrawButton.onClick.Invoke();Require(g.DrawPopupOpen&&g.DrawCategory==tab,"Other tab still routes to unified draw "+tab);g.CloseDrawPopup();}
            g.SelectTab(0);ExecuteEvents.Execute(g.CloseButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(!g.PopupOpen,"Skin close returns to home sample");g.ShowPopup(true);g.SelectTab(0);
            yield return CaptureSkinBoards(g);
            File.WriteAllText(Path.Combine(Output,"interaction_checks.txt"),string.Join("\n",checks));SessionState.SetBool(Key,false);capturing=false;Status("COMPLETE: "+checks.Count+" skin focused checks; real Game View screenshots. Left in skin preview.");
        }
        private static IEnumerator CaptureSkinBoards(VisualSampleGallery g)
        {
            g.SetEnglish(false);g.SetArtworkInspection(true);g.RefreshText();g.PreviewCanvas.gameObject.SetActive(false);
            var board=new GameObject("RarityReview_NativeUnityComponents",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var bg=VisualSampleGallery.Rect("Background",board.transform).gameObject.AddComponent<Image>();VisualSampleGallery.Fill(bg.rectTransform);bg.color=new Color(.96f,.94f,.88f);
            SetSize(new Vector2Int(470,435));yield return Settle();CompareLabel(board.transform,"五档品质 · Unity 实际卡片 / 390 基准尺寸",10,7,450,26,17);
            CompareLabel(board.transform,"上排：彩色素材检视　下排：未获得状态",10,34,450,24,13);
            for(var grade=0;grade<5;grade++)
            {
                var i=System.Array.FindIndex(g.SkinRecords,s=>s.rarity==grade&&s.group!="Initial");
                for(var row=0;row<2;row++)
                {
                    var clone=Object.Instantiate(g.SkinCards[i].Root,board.transform);VisualSampleGallery.Place(clone,18+grade*88,67+row*177,82,168);
                    var ship=clone.Find("Visual/ProportionalShip").GetComponent<RawImage>();ship.material=row==0?null:Resources.Load<Material>("TideboundUI/VisualSamples/Scenes/SceneLocked");
                }
            }
            yield return Settle();yield return Save("Skins_FiveRarities_UnityComponentBoard.png");Object.Destroy(board);yield return null;
            var textures=new List<Texture2D>();var target=ReadTexture(Path.Combine(Repo,"30_设计与素材库/船舰/20260923_船体皮肤/皮肤弹窗-船只皮肤.png"),textures);var sample=ReadTexture(Path.Combine(Output,"Skins_CN_390x844_GameView.png"),textures);
            board=new GameObject("SkinTargetComparison_DocumentOnly",typeof(RectTransform),typeof(Canvas));board.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            bg=VisualSampleGallery.Rect("Background",board.transform).gameObject.AddComponent<Image>();VisualSampleGallery.Fill(bg.rectTransform);bg.color=new Color(.93f,.95f,.96f);
            SetSize(new Vector2Int(824,948));yield return Settle();CompareLabel(board.transform,"目标参考 · 原图等比",16,8,390,31,18);CompareLabel(board.transform,"Unity Game View · 新玩家",418,8,390,31,18);
            Crop(board.transform,target,new Rect(0,0,target.width,target.height),new Vector2(16,47),390f/target.width);Crop(board.transform,sample,new Rect(0,0,390,844),new Vector2(418,47),1);
            CompareLabel(board.transform,"同为 390 宽；品质底色、31 款数量和统一抽奖入口按本轮要求更新",16,904,792,28,14);
            yield return Settle();yield return Save("Skins_TargetComparison_390Width.png");Object.Destroy(board);foreach(var texture in textures)Object.Destroy(texture);
            SetSize(new Vector2Int(390,844));yield return Settle();g.PreviewCanvas.gameObject.SetActive(true);g.SetArtworkInspection(false);g.ApplyLayout();g.SelectTab(0);yield return Settle();
        }
    }
}
