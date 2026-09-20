using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tidebound.Core;
using Tidebound.Config;
using Tidebound.Collection;
using Tidebound.LevelDesign;
using Tidebound.Save;
using Tidebound.Ship;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class CollectionFlowPlayModeTests
    {
        private sealed class Store:IPlayerSaveStore
        {public PlayerSaveData Data;public bool Fail;public PlayerSaveData Load()=>Data?.Copy();public void Save(PlayerSaveData d){if(Fail)throw new IOException();Data=d.Copy();}}
        private static string Id()=>Guid.NewGuid().ToString("N");
        private static CandidateLevelCatalog Catalog()
        {
            var p=Path.Combine(Application.dataPath,"Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            return new CandidateLevelCatalog(File.ReadAllText(Path.Combine(p,"manifest.json")),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".json"))),Phase5RLevelRecipes.All.Select(r=>File.ReadAllText(Path.Combine(p,r.LevelId+".solution.json"))));
        }
        private static Store Earned(int levels)
        {
            var d=new PlayerSaveData{CurrentLevel=levels+1,HighestClearedLevel=levels,Settlements=Enumerable.Range(1,levels).Select(n=>new SettlementRecord{AttemptId=Id(),LevelId="Seed"+n,LevelNumber=n,Kind="Victory",Day="2026-09-20",EconomyVersion=BattleCoinRules.Version,BattleCoins=n==1?7:80,FirstClearCoins=BattleCoinRules.FirstClear(n)}).ToArray()};
            d.Coins=d.Settlements.Sum(r=>(long)r.BattleCoins+r.FirstClearCoins);return new Store{Data=d};
        }
        private static PortraitPuzzleGraybox Create(Store store)
        {var g=new GameObject("Collection_Flow").AddComponent<PortraitPuzzleGraybox>();g.Initialize(Catalog(),saveService:new PlayerSaveService(store),campaign:true);return g;}
        private static void Back(CollectionPanel p)=>p.transform.Find("Transaction/Back").GetComponent<Button>().onClick.Invoke();
        [UnityTest]
        public IEnumerator FreeClaimFailureRetryDoubleClickAndReloadKeepOneReceiptAndOldAttempt()
        {
            var store=Earned(2);var g=Create(store);
            try
            {
                var id=g.Session.SessionId;var coins=g.SaveService.Coins;g.OpenCollection();Assert.That(g.IsCollectionOpen,Is.True);Assert.That(g.IsPaused,Is.True);
                g.CollectionView.SelectTransaction("FirstBlue");store.Fail=true;g.CollectionView.ConfirmTransaction();
                Assert.That(g.CollectionView.LastResult,Is.EqualTo(CollectionStatus.StorageUnavailable));Assert.That(g.CollectionView.LastReceipt,Is.Null);
                store.Fail=false;g.CollectionView.ConfirmTransaction();var blue=g.CollectionView.LastReceipt.SkinIds[0];g.CollectionView.ConfirmTransaction();
                Assert.That(g.SaveService.Snapshot.Collection.Receipts.Length,Is.EqualTo(1));Assert.That(g.SaveService.Coins,Is.EqualTo(coins));
                Back(g.CollectionView);g.CollectionView.EquipSelected();Assert.That(g.SaveService.Snapshot.Collection.Equipment,Does.Contain(blue));
                g.Restart();g.TogglePause();g.OpenShop();g.ClickShip(g.Session.Board.Ships.First().Id);
                Assert.That(g.Session.SessionId,Is.EqualTo(id));Assert.That(g.Session.Ships.Where(s=>s.Length==2).All(s=>s.SkinId==FoundationLimits.DefaultStandardSkinId),Is.True);
                g.CloseCollection();Assert.That(g.IsPaused,Is.False);
                g.Restart();Assert.That(g.Session.SessionId,Is.Not.EqualTo(id));Assert.That(g.Session.Ships.Any(s=>s.SkinId==blue),Is.True);
                var saved=g.SaveService.Snapshot;UnityEngine.Object.Destroy(g.gameObject);yield return null;g=Create(store);
                Assert.That(g.IsPaused,Is.False);Assert.That(g.SaveService.Snapshot.Collection.Receipts.Length,Is.EqualTo(1));Assert.That(g.Session.Ships.Select(s=>s.SkinId),Is.EqualTo(saved.Attempt.Ships.Select(s=>s.SkinId)));
                Assert.That(g.SaveService.Snapshot.Attempt.RewardSeed,Is.EqualTo(saved.Attempt.RewardSeed));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator LevelTwoResultGateClaimsAndEquipsBeforeCreatingLevelThree()
        {
            var store=Earned(1);var service=new PlayerSaveService(store);
            using(var world=SavedGameRuntime.Create(Catalog().Load(1),2))
            {
                service.Start(world);var proof=LevelSolver.Solve(world.Session.Board,new LevelSolverOptions(4000,400000,500));
                foreach(var id in proof.ShipIds){var op=world.Movement.TryBeginMove(id);if(op.Operation.Stage==ShipMoveStage.Traveling)world.Movement.CompleteTravel(op.Operation.OperationId);if(world.Movement.IsBusy)world.Movement.CompleteBlockedFeedback(op.Operation.OperationId);}
                world.Transit.Advance(100);world.Combat.Advance();Assert.That(service.Checkpoint(),Is.True);
            }
            var g=Create(store);
            try
            {
                var before=g.Session.SessionId;g.ContinueFromResult();Assert.That(g.IsCollectionOpen,Is.True);g.ContinueFromResult();Assert.That(g.Session.SessionId,Is.EqualTo(before));
                g.CollectionView.SelectTransaction("FirstBlue");g.CollectionView.ConfirmTransaction();var blue=g.CollectionView.LastReceipt.SkinIds[0];Back(g.CollectionView);
                g.CollectionView.TapSlot(0);g.CollectionView.EquipSelected();g.CloseCollection();g.ContinueFromResult();
                Assert.That(g.LevelIndex,Is.EqualTo(2));Assert.That(g.Session.Ships.Where(s=>s.Length==2).All(s=>s.SkinId==blue),Is.True);Assert.That(g.SaveService.Coins,Is.EqualTo(307));
                Assert.That(g.SaveService.Snapshot.Attempt.Ships.Where(s=>s.Length==2).All(s=>s.CoinCap==2),Is.True);
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator FullSlotsRequireReplacementAndUserPauseSurvivesCollectionClose()
        {
            var store=Earned(8);var d=store.Data;
            CollectionDrawEngine.Generate(d.Collection,Id(),"FirstBlue",9);
            foreach(var skin in SkinCatalog.All.Where(s=>!s.IsDefault && s.Rarity==SkinRarity.Common))
            {var r=new CollectionReceipt{RequestId=Id(),Kind="Single",AtLevel=9,AtUtc="2026-09-20T00:00:00.0000000+00:00",CoinCost=CollectionRules.SinglePrice(d.Collection.OwnedIds.Length-1),SkinIds=new[]{skin.Id}};CollectionDrawEngine.ApplyDraw(d.Collection,skin);d.Collection.Receipts=d.Collection.Receipts.Concat(new[]{r}).ToArray();d.Coins-=r.CoinCost;}
            d.Validate();var g=Create(store);
            try
            {
                g.SaveService.SetEquipment(Id(),d.Collection.OwnedIds.Take(5).ToArray());g.TogglePause();g.OpenCollection();
                var last=d.Collection.OwnedIds.Last();g.CollectionView.SelectSkin(last);g.CollectionView.EquipSelected();
                Assert.That(g.CollectionView.ReplacementSkinId,Is.EqualTo(last));Assert.That(g.SaveService.Snapshot.Collection.Equipment,Does.Not.Contain(last));
                g.CollectionView.TapSlot(4);Assert.That(g.SaveService.Snapshot.Collection.Equipment[4],Is.EqualTo(last));
                g.CloseCollection();Assert.That(g.IsPaused,Is.True);g.TogglePause();g.Restart();
                Assert.That(g.Combat.Fleet.StandardGroups.Count,Is.EqualTo(5));Assert.That(g.Combat.Fleet.StandardGroups.Last().SkinId,Is.EqualTo(last));
                var initial=g.Session.Ships.ToDictionary(s=>s.Id,s=>s.SkinId);g.SelectTool(ShipTool.Rescue);
                Assert.That(g.Session.Ships.All(s=>initial[s.Id]==s.SkinId),Is.True);
                var end=Time.realtimeSinceStartup+10;while(g.Combat.HitCount<2 && Time.realtimeSinceStartup<end)yield return null;Assert.That(g.Combat.HitCount,Is.EqualTo(2));
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
        [UnityTest]
        public IEnumerator CollectionAndConfirmationFitSmallAndInsetPortraitSafeAreas()
        {
            var g=Create(Earned(4));
            try
            {
                g.OpenCollection();
                foreach(var safe in new[]{new Rect(0,0,360,640),new Rect(0,34,390,760),new Rect(0,24,430,866)})
                {
                    g.ApplyViewport(safe,1);Canvas.ForceUpdateCanvases();var root=(RectTransform)g.CollectionView.transform;
                    foreach(var b in root.GetComponentsInChildren<Button>())
                    {var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);Assert.That(corners[0].y,Is.GreaterThanOrEqualTo(safe.y-1),b.name);Assert.That(corners[2].y,Is.LessThanOrEqualTo(safe.yMax+1),b.name);}
                    g.CollectionView.ShowOdds();yield return null;Back(g.CollectionView);
                }
            }
            finally{UnityEngine.Object.Destroy(g.gameObject);}yield return null;
        }
    }
}
