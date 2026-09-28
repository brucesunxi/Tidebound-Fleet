using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Save;
using Tidebound.Config;
using Tidebound.Combat;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class CampaignV3PlaybackTests
    {
        [UnityTest]
        public IEnumerator FirstThirtyNewLevelsReplayWithTheCurrentOceanLayout()=>Playback(30);
        [UnityTest]
        public IEnumerator HundredNewLevelsReplayWithTheCurrentOceanLayout()=>Playback(100);

        private sealed class Store:IPlayerSaveStore
        {
            public PlayerSaveData Data;
            public PlayerSaveData Load()=>Data?.Copy();
            public void Save(PlayerSaveData data){Data=data.Copy();}
        }
        [UnityTest]
        public IEnumerator LegacyPartialAttemptResumesThroughTheNewCatalogThenRestartUsesV3()=>Legacy(false);
        [UnityTest]
        public IEnumerator HomeStartsNewRevisionFromALegacyCheckpoint()=>Legacy(true);
        private IEnumerator Legacy(bool fromHome)
        {
            var folder=Path.Combine(Application.dataPath,"Tidebound/Config/Levels/CampaignV3");
            var manifest=JObject.Parse(File.ReadAllText(Path.Combine(folder,"manifest-100.json")));
            var assets=new Dictionary<string,Func<string>>();
            foreach(var row in manifest["levels"]){var file=(string)row["layoutFile"];assets.Add(file,()=>File.ReadAllText(Path.Combine(folder,file)));}
            foreach(var row in manifest["previousRevisions"])
            {
                var file=(string)row["layoutFile"];var path=Path.Combine(Application.dataPath,file.StartsWith("P5R_Ten_")?
                    "Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates":"Tidebound/Config/Levels/Campaign",file);assets.Add(file,()=>File.ReadAllText(path));
            }
            var catalog=PlayableLevelCatalog.FromManifest(manifest.ToString(),assets);
            var original=PlayableLevelCatalog.LoadForAttempt(catalog,0,(string)manifest["previousRevisions"][0]["levelId"]);
            var store=new Store();
            using(var world=SavedGameRuntime.Create(original,1))
            {
                var ship=world.Session.Board.Ships.First(s=>{var path=world.Session.Board.QueryForwardPath(s.Id);return path.IsBlocked&&path.TravelDistance>0;});
                var move=world.Movement.TryBeginMove(ship.Id);world.Movement.CompleteTravel(move.Operation.OperationId);world.Movement.CompleteBlockedFeedback(move.Operation.OperationId);
                store.Data=new PlayerSaveData{Attempt=world.Capture()};store.Data.Validate();
            }
            var saved=store.Data.Attempt;var game=new GameObject("V3_Migration_Test").AddComponent<PortraitPuzzleGraybox>();
            try
            {
                game.Initialize(catalog,saveService:new PlayerSaveService(store),useGameplayArt:true,useHomeNavigation:fromHome);yield return null;
                if(fromHome)
                {
                    Assert.That(game.IsHomeOpen,Is.True);game.ContinueFromHome();yield return null;
                    Assert.That(game.IsHomeOpen,Is.False);Assert.That(game.Session.LevelId,Is.EqualTo("TF_V3_001"));
                    Assert.That(game.SaveService.IsAvailable,Is.True);Assert.That(game.SaveService.DailyRestartCount,Is.EqualTo(1));yield break;
                }
                Assert.That(game.SaveService.IsAvailable,Is.True);Assert.That(game.Session.SessionId,Is.EqualTo(saved.AttemptId));
                Assert.That(game.Session.LevelId,Is.EqualTo(original.LevelId));Assert.That(game.Session.Board.GetShip(saved.Board[0].Id).Position,Is.EqualTo(new GridPosition(saved.Board[0].X,saved.Board[0].Y)));
                Assert.That(game.Session.Boss.InitialHp,Is.EqualTo(70));
                game.Restart();yield return null;
                Assert.That(game.Session.LevelId,Is.EqualTo("TF_V3_001"));Assert.That(game.Session.SessionId,Is.Not.EqualTo(saved.AttemptId));
                Assert.That(game.SaveService.IsAvailable,Is.True);
            }
            finally{UnityEngine.Object.Destroy(game.gameObject);}
            yield return null;
        }

        private IEnumerator Playback(int limit)
        {
            var folder = Path.Combine(Application.dataPath, "Tidebound/Config/Levels/CampaignV3");
            var ids = JObject.Parse(File.ReadAllText(Path.Combine(folder, "manifest-100.json")))["levels"].Select(r => (string)r["levelId"]).ToArray();
            var catalog = new PlayableLevelCatalog(ids, _ => true, id => LevelJsonReader.Read(File.ReadAllText(Path.Combine(folder, id + ".json"))));
            var game = new GameObject("CampaignV3_Playback_Test").AddComponent<PortraitPuzzleGraybox>();
            var actions = 0; var partials = 0; var exits = 0;
            try
            {
                game.Initialize(catalog, new ShipMovementTiming(1000, .005f, .01f, .01f, .06f),
                    new LaneTransitTiming(.03, .005, .005), new CombatTiming(.005, .01), useGameplayArt: true);
                game.SetAssistancePreferences(false, false);
#if UNITY_EDITOR
                var type=Type.GetType("Tidebound.EditorTools.PortraitGrayboxCapture, Tidebound.Editor",true);
                type.GetMethod("SetGameViewSize",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
                    .Invoke(null,new object[]{new Vector2Int(390,844)});
#endif
                QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                yield return null;
                for (var i = 0; i < limit; i++)
                {
                    game.SelectLevel(i); yield return null;
                    if(limit==100 && new[]{4,24,49,74,99}.Contains(i))
                    {
                        var capture=Environment.GetEnvironmentVariable("TIDEBOUND_V3_CAPTURE");
                        if(!string.IsNullOrEmpty(capture))
                        {
                            Directory.CreateDirectory(capture);
                            var file=Path.Combine(capture,ids[i]+".png");
                            ScreenCapture.CaptureScreenshot(file);
                            var captureEnd=Time.realtimeSinceStartup+5;
                            while(!File.Exists(file)&&Time.realtimeSinceStartup<captureEnd)yield return null;
                            Assert.That(File.Exists(file),Is.True);
                        }
                    }
                    var proof = LevelProofJson.Read(File.ReadAllText(Path.Combine(folder, ids[i] + ".solution.json")));
                    Assert.That(proof.Replay(ids[i], game.Session.Board).IsComplete, Is.True);
                    var count = game.Session.Board.ShipCount; var hits = new List<string>(); var wins = 0;
                    using (game.Session.Events.Subscribe<BossDamagedEvent>(e => hits.Add(e.AttackId)))
                    using (game.Session.Events.Subscribe<GameWinEvent>(_ => wins++))
                    {
                        foreach (var step in proof.Steps)
                        {
                            Assert.That(LevelStateIdentity.Fingerprint(game.Session.Board), Is.EqualTo(step.BeforeHash));
                            var before = game.Session.Board.ShipCount;
                            game.ClickShip(step.ShipId); var deadline = Time.realtimeSinceStartup + 5;
                            while (game.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                            Assert.That(game.IsBusy, Is.False, "Animation deadline: " + ids[i] + "/" + step.ShipId);
                            Assert.That(LevelStateIdentity.Fingerprint(game.Session.Board), Is.EqualTo(step.AfterHash));
                            Assert.That(game.Session.Board.ShipCount, Is.EqualTo(before - (step.Outcome == ForwardPathOutcome.Exit ? 1 : 0)));
                            if (step.Outcome == ForwardPathOutcome.Blocked)
                            {
                                partials++;
                                Assert.That(game.Session.Board.GetShip(step.ShipId).Position, Is.EqualTo(step.To));
                                Assert.That(game.ExitedIds.Contains(step.ShipId), Is.False, "A partial advance must not attack.");
                            }
                            else exits++;
                            actions++;
                        }
                        var finish = Time.realtimeSinceStartup + 30;
                        while (!game.IsCleared && Time.realtimeSinceStartup < finish) yield return null;
                        Assert.That(game.IsCleared, Is.True, ids[i]);
                        Assert.That(game.Combat.HitCount, Is.EqualTo(count));
                        Assert.That(hits.Distinct().Count(), Is.EqualTo(count));
                        Assert.That(wins, Is.EqualTo(1));
                        Assert.That(game.ActiveViewCount, Is.Zero);
                        CollectionAssert.AreEqual(game.ExitedIds, game.EnteredIds);
                    }
                    Debug.Log("Campaign V3 animated proof passed: " + ids[i]);
                }
                Assert.That(actions, Is.EqualTo(limit==30?2595:9130)); Assert.That(partials, Is.EqualTo(limit==30?88:385)); Assert.That(exits, Is.EqualTo(limit==30?2507:8745));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }
    }
}
