using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.Combat;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Unity.LevelDesign;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tidebound.Tests
{
    public sealed class CampaignPlaybackTests
    {
        [UnityTest]
        public IEnumerator EveryPublishedCampaignLevelClearsThroughAnimationLaneAndCombat() => Playback(false);

        [UnityTest]
        public IEnumerator Level27AutoDiagnostic() => Playback(true);

        private static IEnumerator Playback(bool diagnostic)
        {
            var folder = Path.Combine(Application.dataPath, "Tidebound/Config/Levels/Campaign");
            var legacy = Path.Combine(Application.dataPath, "Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates");
            var manifest = File.ReadAllText(Path.Combine(folder, File.Exists(Path.Combine(folder, "manifest-100.json")) ? "manifest-100.json" : "manifest-30.json"));
            var assets = JObject.Parse(manifest)["levels"].ToDictionary(r => (string)r["layoutFile"], r =>
            {
                var file = (string)r["layoutFile"];
                return (Func<string>)(() => File.ReadAllText(Path.Combine(file.StartsWith("P5R_Ten_", StringComparison.Ordinal) ? legacy : folder, file)));
            });
            var catalog = PlayableLevelCatalog.FromManifest(manifest, assets);
            var game = new GameObject("CampaignPlayback_Test").AddComponent<PortraitPuzzleGraybox>();
            var total = 0;
            try
            {
                game.Initialize(catalog, new ShipMovementTiming(1000, .005f, .01f, .01f, .06f),
                    new LaneTransitTiming(.03, .005, .005), new CombatTiming(.005, .01),useGameplayArt:!diagnostic);
                yield return null;
                for (var i = diagnostic ? 26 : 0; i < (diagnostic ? 27 : catalog.Count); i++)
                {
                    game.SelectLevel(i); yield return null;
                    var count = game.Session.Board.ShipCount; var hits = new List<string>(); var wins = 0;
                    using (game.Session.Events.Subscribe<BossDamagedEvent>(e => hits.Add(e.AttackId)))
                    using (game.Session.Events.Subscribe<GameWinEvent>(_ => wins++))
                    {
                        if (diagnostic)
                        {
                            var solved = game.SolveCurrent();
                            Debug.Log("L27 solver: " + solved.Status + "; " + solved.Reason);
                            game.ToggleAuto();
                            Debug.Log("L27 auto=" + game.IsAutoPlaying + "; ready=" + game.IsEntryReady);
                        }
                        else
                        {
                            // Drive the same player click entry point using the independently certified solution.
                            // The developer Auto button has a separate 150ms synchronous search budget.
                            var solution = LevelSolver.Solve(game.Session.Board);
                            Assert.That(solution.Status, Is.EqualTo(LevelSolverStatus.Solved));
                            foreach (var id in solution.ShipIds)
                            {
                                var before = game.Session.Board.ShipCount;
                                game.ClickShip(id); var moveDeadline = Time.realtimeSinceStartup + 5;
                                while (game.IsBusy && Time.realtimeSinceStartup < moveDeadline) yield return null;
                                Assert.That(game.IsBusy, Is.False, "Animation did not finish: " + id);
                                Assert.That(game.Session.Board.ShipCount, Is.EqualTo(before - 1), "Click did not exit: " + id);
                            }
                        }
                        var deadline = Time.realtimeSinceStartup + 40;
                        while (!game.IsCleared && Time.realtimeSinceStartup < deadline) yield return null;
                        Assert.That(game.IsCleared, Is.True, "Level " + (i + 1) + "; remaining=" + game.Session.Board.ShipCount +
                            "; busy=" + game.IsBusy + "; auto=" + game.IsAutoPlaying + "; paused=" + game.IsPaused +
                            "; ready=" + game.IsEntryReady + "; hits=" + game.Combat.HitCount);
                        Assert.That(game.Session.Boss.Hp, Is.Zero);
                        Assert.That(game.Combat.HitCount, Is.EqualTo(count));
                        Assert.That(hits.Distinct().Count(), Is.EqualTo(count));
                        Assert.That(wins, Is.EqualTo(1));
                        Assert.That(game.ActiveViewCount, Is.Zero);
                        Assert.That(game.ExitedIds.Count, Is.EqualTo(count));
                        CollectionAssert.AreEqual(game.ExitedIds, game.EnteredIds);
                        total += count;
                    }
                    Debug.Log("Campaign animated clear " + (i + 1) + "/" + catalog.Count);
                }
                Assert.That(total, Is.EqualTo(diagnostic ? 80 : 7 + (catalog.Count - 1) * 80));
                Debug.Log("Campaign animation complete: " + catalog.Count + " levels / " + total + " ships");
            }
            finally { UnityEngine.Object.Destroy(game.gameObject); }
            yield return null;
        }
    }
}
