using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Board;
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
    public sealed class DifficultyPrototypePlaybackTests
    {
        [UnityTest]
        public IEnumerator ReviewHostLoadsWithoutPlayerSaveAndCanStepADynamicProof()
        {
            var folder = Path.Combine(Application.dataPath, "Tidebound/Config/LevelPrototypes/DifficultyV2");
            var ids = new[] { "DV2_P4", "DV2_R38", "DV2_B80_T" };
            var layouts = ids.Select(id => new TextAsset(File.ReadAllText(Path.Combine(folder, id + ".json"))) { name = id }).ToArray();
            var proofs = ids.Select(id => new TextAsset(File.ReadAllText(Path.Combine(folder, id + ".solution.json")))).ToArray();
            var host = new GameObject("DifficultyReviewHost_Test").AddComponent<DifficultyReviewSession>();
            host.Configure(layouts, proofs);
            try
            {
                yield return null; yield return null;
                Assert.That(host.Game, Is.Not.Null); Assert.That(host.Game.SaveService, Is.Null);
                Assert.That(host.Game.AutoHintsEnabled, Is.False);
                Assert.That(host.ReplayStep(), Is.True);
                var end = Time.realtimeSinceStartup + 5;
                while (host.Game.IsBusy && Time.realtimeSinceStartup < end) yield return null;
                Assert.That(host.Game.Session.Board.ShipCount, Is.EqualTo(4));
                Assert.That(host.Game.Session.Board.GetShip("A").Position, Is.EqualTo(new GridPosition(1, 2)));
                host.Game.SelectLevel(2); host.Game.SetAssistancePreferences(true, false); yield return null;
                Assert.That(host.Game.AutoHintsEnabled, Is.False, "The legacy direct-exit hint must stay off for B review content.");
                var capture = Environment.GetEnvironmentVariable("TIDEBOUND_DIFFICULTY_CAPTURE");
                if (!string.IsNullOrEmpty(capture))
                {
#if UNITY_EDITOR
                    var type = Type.GetType("Tidebound.EditorTools.PortraitGrayboxCapture, Tidebound.Editor", true);
                    type.GetMethod("SetGameViewSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        .Invoke(null, new object[] { new Vector2Int(390, 844) });
#endif
                    Directory.CreateDirectory(capture);
                    var sizeDeadline = Time.realtimeSinceStartup + 10;
                    while ((Screen.width != 390 || Screen.height != 844) && Time.realtimeSinceStartup < sizeDeadline) yield return null;
                    Assert.That(Screen.width, Is.EqualTo(390)); Assert.That(Screen.height, Is.EqualTo(844));
                    for (var i = 0; i < ids.Length; i++)
                    {
                        host.Game.SelectLevel(i); yield return null; yield return null;
                        var file = Path.Combine(capture, ids[i] + ".png");
                        if (File.Exists(file)) File.Delete(file);
                        ScreenCapture.CaptureScreenshot(file);
                        var captureDeadline = Time.realtimeSinceStartup + 5;
                        while (!File.Exists(file) && Time.realtimeSinceStartup < captureDeadline) yield return null;
                        Assert.That(File.Exists(file), Is.True);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(host.gameObject);
                foreach (var a in layouts.Concat(proofs)) UnityEngine.Object.Destroy(a);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReviewPackReplaysPartialAndExitActionsThroughRealAnimations()
        {
            var folder = Path.Combine(Application.dataPath, "Tidebound/Config/LevelPrototypes/DifficultyV2");
            var ids = JObject.Parse(File.ReadAllText(Path.Combine(folder, "review-manifest.json")))["levels"].Select(r => (string)r["levelId"]).ToArray();
            var catalog = new PlayableLevelCatalog(ids, _ => true, id => LevelJsonReader.Read(File.ReadAllText(Path.Combine(folder, id + ".json"))));
            var game = new GameObject("DifficultyV2_Playback_Test").AddComponent<PortraitPuzzleGraybox>();
            var actions = 0; var partials = 0; var exits = 0;
            try
            {
                game.Initialize(catalog, new ShipMovementTiming(1000, .005f, .01f, .01f, .06f),
                    new LaneTransitTiming(.03, .005, .005), new CombatTiming(.005, .01), useGameplayArt: true);
                game.SetAssistancePreferences(false, false);
                yield return null;
                for (var i = 0; i < ids.Length; i++)
                {
                    game.SelectLevel(i); yield return null;
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
                    Debug.Log("Difficulty v2 animated proof passed: " + ids[i]);
                }
                Assert.That(actions, Is.EqualTo(658)); Assert.That(partials, Is.EqualTo(6)); Assert.That(exits, Is.EqualTo(652));
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }
    }
}
