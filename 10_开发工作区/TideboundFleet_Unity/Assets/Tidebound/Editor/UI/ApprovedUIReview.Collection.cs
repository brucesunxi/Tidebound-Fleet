using System;
using System.Collections;
using System.Linq;
using Tidebound.Collection;
using Tidebound.Config;
using Tidebound.Save;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using Tidebound.Unity.UI;
using UnityEngine;

namespace Tidebound.EditorTools
{
    public static partial class ApprovedUIReview
    {
        private static PlayerSaveData ReviewProgress(int cleared)
        {
            var d=new PlayerSaveData{CurrentLevel=cleared+1,HighestClearedLevel=cleared};
            d.Settlements=Enumerable.Range(1,cleared).Select(n=>new SettlementRecord{AttemptId=Guid.NewGuid().ToString("N"),LevelId="REVIEW_LEVEL_"+n,Kind="Victory",LevelNumber=n,FirstClearCoins=BattleCoinRules.FirstClear(n),EconomyVersion=BattleCoinRules.Version,Day="2026-09-24"}).ToArray();
            d.Coins=d.Settlements.Sum(x=>(long)x.FirstClearCoins);d.Validate();return d;
        }
        private static IEnumerator CollectionFrames(PortraitPuzzleGraybox game,PlayableLevelCatalog catalog)
        {
            var store=new MemoryPlayerSaveStore();var save=new PlayerSaveService(store);
            game.Initialize(catalog,saveService:save,campaign:true,useHomeNavigation:true);yield return null;yield return null;
            game.OpenCollection();yield return null;var view=game.CollectionView.ApprovedGallery;
            Require(view&&view.IsProduction,"Accepted collection layout bound to real save service");
            Require(!view.Toolbar.gameObject.activeSelf&&!view.Stamp.gameObject.activeSelf&&!view.Home.gameObject.activeSelf,"Preview controls and preview home absent in production");
            Require(view.SkinEquippedCount==1&&Enumerable.Range(0,31).Count(view.IsSkinPreviewOwned)==1,"New player has one default skin");
            yield return Save("Collection_NewPlayer_GameView");game.CloseCollection();yield return null;
            store.Save(ReviewProgress(25));save=new PlayerSaveService(store);
            game.Initialize(catalog,saveService:save,campaign:true,useHomeNavigation:true);yield return null;yield return null;game.OpenCollection();yield return null;
            view=game.CollectionView.ApprovedGallery;
            if(view.DrawPopupOpen)view.CloseDrawPopup();
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {
                yield return Size(size);
                foreach(var tab in new[]{0,1,2,3})
                {
                    view.SelectTab(tab);yield return null;yield return null;
                    Require(view.Scroll.content.gameObject.activeSelf,"Correct collection content active "+tab+size);
                    yield return Save("Collection_"+tab+"_"+size.x+"x"+size.y+"_GameView");
                    view.Scroll.verticalNormalizedPosition=0;yield return null;yield return Save("Collection_"+tab+"_LastRow_"+size.x+"x"+size.y+"_GameView");
                }
            }
            yield return Size(new Vector2Int(390,844));view.SelectTab(0);view.ToggleSkin(4);yield return null;
            Require(save.AppearanceEquipment.Contains("TF_SKIN_K05"),"Owned card equips through durable service");
            var selection=save.AppearanceEquipment;view.ToggleSkin(3);yield return null;
            Require(save.AppearanceEquipment.SequenceEqual(selection)&&view.SkinViewerOpen,"Locked card opens inspection without granting");view.SkinViewer.gameObject.SetActive(false);
            view.SetEnglish(true);yield return Size(new Vector2Int(360,640));
            for(var tab=0;tab<4;tab++){view.SelectTab(tab);yield return null;yield return Save("Collection_EN_"+tab+"_360x640_GameView");}
            view.SetEnglish(false);yield return Size(new Vector2Int(390,844));
            view.SelectTab(3);view.SelectScene(11);yield return null;Require(save.SelectedSceneId=="TF_SCENE_S12","Scene card saves actual selection");
            view.SelectTab(2);view.SelectCard(3);yield return null;Require(save.SelectedShowcaseId=="TF_SHOWCASE_H04","Showcase card saves actual selection");
            game.CloseCollection();yield return null;yield return null;yield return Save("Home_SelectedSceneAndShowcase_GameView");
            var reload=new PlayerSaveService(store);Require(reload.SelectedSceneId==save.SelectedSceneId&&reload.SelectedShowcaseId==save.SelectedShowcaseId&&reload.AppearanceEquipment.SequenceEqual(selection),"All collection selections survive service reload");
            store=new MemoryPlayerSaveStore();store.Save(ReviewProgress(3));save=new PlayerSaveService(store);
            game.Initialize(catalog,saveService:save,campaign:true,useHomeNavigation:true);yield return null;yield return null;game.OpenCollection();yield return null;view=game.CollectionView.ApprovedGallery;
            var coins=save.Coins;Require(view.DrawPopupOpen&&view.DrawOnceButton.interactable,"Existing first-blue gift reachable through unified draw");view.DrawOnceButton.onClick.Invoke();yield return null;
            Require(!save.CanClaimFirstBlue&&save.Coins==coins&&save.Snapshot.Appearance.DrawReceipts.Length==1,"Real free gift saved once without charging");
            var blueArt=save.Snapshot.Appearance.DrawReceipts.Last().SkinIds[0];
            var cost=save.AppearanceDrawState.Price;view.DrawOnceButton.onClick.Invoke();yield return null;
            yield return Save("Collection_DrawConfirmation_GameView");
            var confirm=view.PreviewCanvas.transform.Find("DurableCollectionTransaction/ConfirmOnce").GetComponent<UnityEngine.UI.Button>();
            confirm.onClick.Invoke();confirm.onClick.Invoke();yield return null;
            Require(save.Coins==coins-cost&&save.Snapshot.Appearance.DrawReceipts.Length==2,"Paid draw confirms once and repeated click cannot charge twice");
            yield return Save("Collection_DrawSaved_GameView");
            view.PreviewCanvas.transform.Find("DurableCollectionTransaction/Back").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            view.CloseDrawPopup();view.SelectTab(0);view.ToggleSkin(4);
            view.ToggleSkin(int.Parse(blueArt.Substring(blueArt.Length-2))-1);
            game.CloseCollection();game.ContinueFromHome();yield return null;yield return null;
            Require(game.Session!=null&&!game.IsHomeOpen,"Collection gate allows real gameplay after free gift");
            Require(game.Session.Ships.Any(x=>x.SkinId=="TF_SKIN_K05"),"Equipped new catalog skin used on actual board");
            foreach(var size in new[]{new Vector2Int(390,844),new Vector2Int(360,640),new Vector2Int(360,800)})
            {yield return Size(size);yield return null;yield return Save("Gameplay_RealSkins_"+size.x+"x"+size.y+"_GameView");}
            Require(game.GetComponentsInChildren<ShipPrototypeAppearance>().Where(x=>x.PaletteSlot>=0).All(x=>x.transform.Find("ActualShipSkin")!=null),"Real standard ships render supplied skins");
            yield return Size(new Vector2Int(390,844));
            var blocked=game.Session.Board.Ships.FirstOrDefault(x=>game.Session.Board.QueryForwardPath(x.Id).TravelDistance==0);
            if(blocked!=null)
            {
                game.ClickShip(blocked.Id);for(var i=0;i<25;i++)yield return null;
                Require(game.GetComponentsInChildren<ShipWakePresentation>().All(x=>x.Ribbon.SampleCount==0),"Zero-distance blocked feedback emits no wake");
            }
            var partial=game.Session.Board.Ships.FirstOrDefault(x=>!game.Session.Board.QueryForwardPath(x.Id).CanExit&&game.Session.Board.QueryForwardPath(x.Id).TravelDistance>0);
            Require(partial!=null,"Dense board includes a partial movement case");
            var partialPath=game.Session.Board.QueryForwardPath(partial.Id);game.ClickShip(partial.Id);
            var partialDeadline=Time.realtimeSinceStartup+5;while(game.IsBusy&&Time.realtimeSinceStartup<partialDeadline)yield return null;
            Require(game.Session.Board.GetShip(partial.Id).Position.Equals(partialPath.TargetTail),"Blocked-ahead ship remains at its advanced position");
            partialDeadline=Time.realtimeSinceStartup+1;while(Time.realtimeSinceStartup<partialDeadline)yield return null;
            Require(game.GetComponentsInChildren<ShipWakePresentation>().All(x=>x.Ribbon.SampleCount==0),"Partial movement wake expires while ship remains on board");
            foreach(Tidebound.Ship.ShipDirection direction in Enum.GetValues(typeof(Tidebound.Ship.ShipDirection)))
            {
                var ship=game.Session.Board.Ships.Where(x=>x.Direction==direction&&game.Session.Board.QueryForwardPath(x.Id).TravelDistance>0).OrderByDescending(x=>game.Session.Board.QueryForwardPath(x.Id).CanExit).ThenByDescending(x=>game.Session.Board.QueryForwardPath(x.Id).TravelDistance).FirstOrDefault();
                if(ship==null){Checks.Add("UNVERIFIED no movable ship in dense board for "+direction);continue;}
                var expectedLength=Mathf.Min(1.4f,game.Session.Board.QueryForwardPath(ship.Id).TravelDistance*.55f);
                game.ClickShip(ship.Id);var deadline=Time.realtimeSinceStartup+2;
                while(!game.GetComponentsInChildren<ShipWakePresentation>().Any(x=>x.Ribbon.HistoryLength>expectedLength)&&Time.realtimeSinceStartup<deadline)yield return null;
                Require(game.GetComponentsInChildren<ShipWakePresentation>().Any(x=>x.Ribbon.SampleCount>2),"Actual movement emits wake "+direction);
                game.TogglePause();yield return null;var history=game.GetComponentsInChildren<ShipWakePresentation>().Sum(x=>x.Ribbon.SampleCount);yield return Save("Gameplay_Wake_"+direction+"_GameView");
                for(var i=0;i<8;i++)yield return null;
                Require(history==game.GetComponentsInChildren<ShipWakePresentation>().Sum(x=>x.Ribbon.SampleCount),"Pause freezes wake lifetime "+direction);game.TogglePause();
                var lane=game.GetComponentsInChildren<PlanarShipLaneView>().First(x=>x.ShipId==ship.Id);var initialUp=lane.transform.up;
                deadline=Time.realtimeSinceStartup+2;
                while(lane.gameObject.activeSelf&&Vector3.Dot(initialUp,lane.transform.up)>.9f&&Time.realtimeSinceStartup<deadline)yield return null;
                if(lane.HasEnteredLane&&lane.gameObject.activeSelf&&Vector3.Dot(initialUp,lane.transform.up)<=.9f)
                {game.TogglePause();yield return null;yield return Save("Gameplay_LaneTurn_"+direction+"_GameView");game.TogglePause();Checks.Add("PASS Real lane turn sampled "+direction);}
                deadline=Time.realtimeSinceStartup+8;while(game.IsBusy&&Time.realtimeSinceStartup<deadline)yield return null;
                var end=Time.realtimeSinceStartup+2;while(Time.realtimeSinceStartup<end)yield return null;
            }
            var laneDeadline=Time.realtimeSinceStartup+20;
            while(game.GetComponentsInChildren<PlanarShipLaneView>().Any(x=>x.HasEnteredLane)&&Time.realtimeSinceStartup<laneDeadline)yield return null;
            Require(!game.GetComponentsInChildren<PlanarShipLaneView>().Any(x=>x.HasEnteredLane),"All sampled ships finish the real lane route");
            laneDeadline=Time.realtimeSinceStartup+1;while(Time.realtimeSinceStartup<laneDeadline)yield return null;
            Require(game.GetComponentsInChildren<ShipWakePresentation>().All(x=>x.Ribbon.SampleCount==0),"Stopped wakes expire after movement and lane entry finish");
            var sessionId=game.Session.SessionId;game.OpenCollection();yield return null;
            Require(game.IsHomeOpen&&game.IsCollectionOpen&&game.CollectionView.ApprovedGallery.IsProduction,"Gameplay collection menu reaches the same durable gallery");
            game.CloseCollection();game.ContinueFromHome();yield return null;
            Require(!game.IsHomeOpen&&game.Session.SessionId==sessionId,"Return from collection resumes the same game");
            game.ReturnHome();yield return null;
        }
    }
}
