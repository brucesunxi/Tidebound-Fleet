using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Lane;
using Tidebound.Combat;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class PlayerSavePlayModeTests
    {
        private sealed class Store:IPlayerSaveStore
        {
            private PlayerSaveData data;public bool Fail;
            public PlayerSaveData Load()=>data?.Copy();
            public void Save(PlayerSaveData value){if(Fail)throw new IOException();data=value.Copy();}
        }
        private static CandidateLevelCatalog Catalog()
        {
            var path=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(path,"manifest.json")),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".json"))),
                Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(path,r.LevelId+".solution.json"))));
        }
        private static PortraitPuzzleGraybox Create(IPlayerSaveStore store,bool campaign=false,bool fast=true)
        {
            var game=new GameObject("SaveGame_Test").AddComponent<PortraitPuzzleGraybox>();
            game.Initialize(Catalog(),new ShipMovementTiming(1000,.005f,.01f,.01f,.06f),
                fast?new LaneTransitTiming(.03,.005,.005):null,fast?new CombatTiming(.005,.01):null,
                saveService:new PlayerSaveService(store),campaign:campaign);return game;
        }
        private static IEnumerator Until(Func<bool> condition,float seconds=10)
        {var end=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True);}
        [UnityTest]
        public IEnumerator PartialMoveAndPausedProjectileResumeSameAttemptWithoutRestartOrRewardDuplication()
        {
            var store=new Store();var game=Create(store,fast:false);
            try
            {
                yield return null;var id=game.Session.SessionId;
                var partial=game.Session.Board.Ships.First(s=>{var p=game.Session.Board.QueryForwardPath(s.Id);return p.IsBlocked && p.TravelDistance>0;});
                var target=game.Session.Board.QueryForwardPath(partial.Id).TargetTail;game.ClickShip(partial.Id);yield return Until(()=>!game.IsBusy);
                var ship=game.Session.Board.Ships.First(s=>game.Session.Board.QueryForwardPath(s.Id).CanExit);game.ClickShip(ship.Id);
                yield return Until(()=>game.Combat.InFlightCount==1);game.TogglePause();var time=game.Combat.Time;
                UnityEngine.Object.Destroy(game.gameObject);yield return null;game=Create(store,fast:false);yield return null;
                Assert.That(game.Session.SessionId,Is.EqualTo(id));Assert.That(game.IsPaused,Is.True);Assert.That(game.Combat.InFlightCount,Is.EqualTo(1));
                Assert.That(game.Combat.Time,Is.EqualTo(time));Assert.That(game.Session.Board.GetShip(partial.Id).Position,Is.EqualTo(target));
                Assert.That(game.ActiveViewCount,Is.EqualTo(6));Assert.That(game.SaveService.DailyRestartCount,Is.Zero);
                game.TogglePause();game.ToggleAuto();yield return Until(()=>game.IsCleared,15);yield return null;
                Assert.That(game.SaveService.Coins,Is.EqualTo(107));Assert.That(game.SaveService.CurrentLevel,Is.EqualTo(2));
                UnityEngine.Object.Destroy(game.gameObject);yield return null;game=Create(store);yield return null;
                Assert.That(game.SaveService.Coins,Is.EqualTo(107));Assert.That(game.IsCleared,Is.True);game.Restart();yield return null;
                Assert.That(game.LevelIndex,Is.EqualTo(1));Assert.That(game.SaveService.Coins,Is.EqualTo(107));
            }
            finally {UnityEngine.Object.Destroy(game.gameObject);}
            yield return null;
        }
        [UnityTest]
        public IEnumerator CampaignWaitsForResultContinueAndCannotSelectOldLevelForFarming()
        {
            var store=new Store();var game=Create(store,true);
            try
            {
                yield return null;game.ToggleAuto();yield return Until(()=>game.IsResultReadable,15);
                yield return new WaitForSecondsRealtime(1.3f);Assert.That(game.LevelIndex,Is.Zero);game.ContinueFromResult();yield return Until(()=>game.LevelIndex==1);
                Assert.That(game.SaveService.Coins,Is.EqualTo(107));Assert.That(game.Session.Board.ShipCount,Is.EqualTo(80));
                game.SelectLevel(0);Assert.That(game.LevelIndex,Is.EqualTo(1));
                Assert.That(game.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name=="Next" || b.name=="Previous").All(b=>!b.gameObject.activeSelf),Is.True);
                var id=game.Session.SessionId;game.Restart();Assert.That(game.Session.SessionId,Is.Not.EqualTo(id));Assert.That(game.SaveService.DailyRestartCount,Is.EqualTo(1));
            }
            finally {UnityEngine.Object.Destroy(game.gameObject);}
            yield return null;
        }
        [UnityTest]
        public IEnumerator TwoShipRescueAndSpentInventorySurviveSceneRecreation()
        {
            var store=new Store();var game=Create(store);
            try
            {
                yield return null;
                for(var i=0;i<2;i++){game.ToggleAuto();yield return Until(()=>game.IsCleared,25);yield return null;game.Restart();yield return null;}
                Assert.That(game.LevelIndex,Is.EqualTo(2));Assert.That(game.Tools.Remaining(ShipTool.Rescue),Is.EqualTo(1));
                game.SelectTool(ShipTool.Rescue);game.TogglePause();var ids=game.ExitedIds.ToArray();Assert.That(ids.Length,Is.EqualTo(2));
                UnityEngine.Object.Destroy(game.gameObject);yield return null;game=Create(store);yield return null;
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(78));Assert.That(game.Tools.Remaining(ShipTool.Rescue),Is.Zero);
                CollectionAssert.AreEqual(ids,game.ExitedIds);Assert.That(game.IsPaused,Is.True);
                foreach(var id in ids){var view=game.GetComponentsInChildren<PlanarShipLaneView>().Single(v=>v.ShipId==id);Assert.That(view.transform.localScale,Is.EqualTo(Vector3.one*.8f));}
                game.TogglePause();game.ToggleAuto();yield return Until(()=>game.IsCleared,25);yield return null;
                Assert.That(game.Combat.HitCount,Is.EqualTo(80));Assert.That(game.SaveService.Coins,Is.EqualTo(527));
                Assert.That(game.SaveService.Snapshot.Tools.Receipts.Count(r=>r=="milestone:tools:unlock"),Is.EqualTo(1));
            }
            finally {UnityEngine.Object.Destroy(game.gameObject);}
            yield return null;
        }
        [UnityTest]
        public IEnumerator FailedIntentWriteLeavesShipStillAndAllowsRetry()
        {
            var store=new Store();var game=Create(store);
            try
            {
                yield return null;var ship=game.Session.Board.Ships.First(s=>game.Session.Board.QueryForwardPath(s.Id).CanExit);
                store.Fail=true;game.ClickShip(ship.Id);yield return null;Assert.That(game.IsBusy,Is.False);Assert.That(game.Session.Board.ShipCount,Is.EqualTo(7));
                Assert.That(game.IsPaused,Is.True);store.Fail=false;game.TogglePause();game.ClickShip(ship.Id);yield return Until(()=>game.Combat.HitCount==1);
                Assert.That(game.Session.Board.ShipCount,Is.EqualTo(6));Assert.That(game.SaveService.Coins,Is.Zero);
                Assert.That(game.SaveService.Snapshot.Attempt.PendingCoins,Is.EqualTo(1));
            }
            finally {store.Fail=false;UnityEngine.Object.Destroy(game.gameObject);}
            yield return null;
        }
    }
}
