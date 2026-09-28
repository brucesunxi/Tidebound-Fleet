using System.Collections;
using System.IO;
using System.Linq;
using Tidebound.Collection;
using Tidebound.Unity.UI.VisualSamples;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebound.EditorTools
{
    public static partial class VisualSampleReview
    {
        private static IEnumerator CaptureGroupFrames(VisualSampleGallery g)
        {
            yield return null;yield return null;g.SetPreviewProgress(0);g.SetArtworkInspection(false);g.ShowPopup(true);
            Require(ShowcaseCatalog.Groups.Select(s=>s.CatalogIndices.Count).SequenceEqual(new[]{1,10,3,11}),"Confirmed source groups contain 1 / 10 / 3 / 11 ships");
            Require(ShowcaseCatalog.Groups.SelectMany(s=>s.CatalogIndices).Distinct().Count()==25,"Grouping neither duplicates nor omits ships");
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                SetSize(size);var until=Time.realtimeSinceStartup+12;
                while((Screen.width!=size.x||Screen.height!=size.y)&&Time.realtimeSinceStartup<until)yield return null;
                Require(Screen.width==size.x&&Screen.height==size.y,"Exact viewport "+size);g.ApplyLayout();
                foreach(var english in new[]{false,true})
                {
                    g.SetEnglish(english);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
                    Require(g.SourceLabels.All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Group captions fit "+size+" "+english);
                    Require(g.Cards.All(c=>c.Name.preferredHeight<=c.Name.rectTransform.rect.height+1),"Ship names fit "+size+" "+english);
                    Require(ShowcaseCatalog.Groups[1].CatalogIndices.Take(4).Select(i=>g.Cards[i].Root.anchoredPosition.y).Distinct().Count()==1,"Level group retains four columns "+size);
                    yield return Save("Groups_"+(english?"EN":"CN")+"_"+size.x+"x"+size.y+"_Top_GameView.png");
                }
                g.SetEnglish(false);var heading=g.Header.position;var tabs=g.Tabs.position;var statistics=g.Statistics.transform.position;
                g.Scroll.verticalNormalizedPosition=0;yield return Settle();
                var card=new Vector3[4];var view=new Vector3[4];g.Cards[ShowcaseCatalog.Groups.Last().CatalogIndices.Last()].Root.GetWorldCorners(card);g.Viewport.GetWorldCorners(view);
                Require(card[0].y>=view[0].y-1&&card[1].y<=view[1].y+1,"Whole last row visible above fixed navigation "+size);
                Require(g.Header.position==heading&&g.Tabs.position==tabs&&g.Statistics.transform.position==statistics,"Title statistics and tabs remain fixed "+size);
                yield return Save("Groups_CN_"+size.x+"x"+size.y+"_Bottom_GameView.png");
            }
            SetSize(new Vector2Int(390,844));yield return Settle();g.ApplyLayout();g.SetEnglish(false);
            for(var group=1;group<4;group++)
            {
                var range=Mathf.Max(1,g.Content.rect.height-g.Viewport.rect.height);
                g.Scroll.verticalNormalizedPosition=1-Mathf.Clamp(-g.SourceHeadings[group].anchoredPosition.y,0,range)/range;
                yield return Settle();yield return Save("Groups_CN_390x844_"+ShowcaseCatalog.Groups[group].Source+"_GameView.png");
            }
            g.Scroll.verticalNormalizedPosition=1;yield return Settle();RequireRaycast(g.Cards[0].Button,"Default card remains clickable");RequireRaycast(g.CloseButton,"Close stays above mask");
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(g.Cards[1].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.SelectedCard==0,"Locked milestone card cannot select after regrouping");
            g.SetPreviewProgress(10);ExecuteEvents.Execute(g.Cards[1].Button.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(g.SelectedCard==1,"Owned milestone card keeps its stable selection ID");
            g.SetPreviewProgress(0);g.SelectCard(0);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
            var drag=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=new Vector2(180,380),pressPosition=new Vector2(180,380)};
            var dragRoot=ExecuteEvents.GetEventHandler<IDragHandler>(g.Cards[0].Button.gameObject);var before=g.Content.anchoredPosition;var fixedTitle=g.Header.position;
            ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.initializePotentialDrag);ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.beginDragHandler);
            drag.position+=Vector2.up*130;drag.delta=Vector2.up*130;ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.dragHandler);ExecuteEvents.Execute(dragRoot,drag,ExecuteEvents.endDragHandler);g.Scroll.StopMovement();
            Require(g.Content.anchoredPosition!=before&&g.Header.position==fixedTitle,"All source groups scroll together while the popup title stays fixed");
            ExecuteEvents.Execute(g.CloseButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);Require(!g.PopupOpen,"Close works after grouped scrolling");
            g.ShowPopup(true);g.Scroll.verticalNormalizedPosition=1;yield return Settle();
            File.WriteAllText(Path.Combine(Output,"group_checks.txt"),string.Join("\n",checks));
            SessionState.SetBool(Key,false);SessionState.SetBool(Key+".Groups",false);capturing=false;Status("COMPLETE GROUPS: "+checks.Count+" focused checks; real Game View screenshots.");
        }
    }
}
