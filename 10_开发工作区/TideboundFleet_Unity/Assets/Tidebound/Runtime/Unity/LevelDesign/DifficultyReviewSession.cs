using System;
using System.Linq;
using Tidebound.Config;
using Tidebound.LevelDesign;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Independent development scene. No save service or campaign routing is ever created here.</summary>
    public sealed class DifficultyReviewSession : MonoBehaviour
    {
        [SerializeField] private TextAsset[] layouts;
        [SerializeField] private TextAsset[] proofs;
        private LevelSolutionProof[] verified;
        private PortraitPuzzleGraybox game;
        private string notice;
        private bool autoReplay, hints;
        private bool controlsOpen = true;
        private int observedLevel = -1;
        public PortraitPuzzleGraybox Game => game;

        public void Configure(TextAsset[] levelAssets, TextAsset[] proofAssets) { layouts = levelAssets; proofs = proofAssets; }

        private void Start()
        {
            try
            {
                if (layouts == null || proofs == null || layouts.Length != proofs.Length || layouts.Length == 0)
                    throw new InvalidOperationException("Review layout/proof assets are missing.");
                var levels = layouts.Select(a => LevelJsonReader.Read(a.text)).ToArray();
                verified = proofs.Select(a => LevelProofJson.Read(a.text)).ToArray();
                for (var i = 0; i < levels.Length; i++)
                    using (var session = CampaignPackValidator.Create(levels[i]))
                        if (!verified[i].Replay(levels[i].LevelId, session.Board).IsComplete)
                            throw new InvalidOperationException("Review proof mismatch: " + levels[i].LevelId);
                var data = levels.ToDictionary(l => l.LevelId);
                var catalog = new PlayableLevelCatalog(levels.Select(l => l.LevelId), data.ContainsKey, id => data[id]);
                game = gameObject.AddComponent<PortraitPuzzleGraybox>();
                game.Initialize(catalog, useGameplayArt: true);
                game.SetAssistancePreferences(false, false);
                observedLevel = game.LevelIndex;
                notice = "Review only / hints off for comparison. No campaign progress is saved.";
            }
            catch (Exception e) { notice = e.Message; Debug.LogError(notice); enabled = false; }
        }

        private void Update()
        {
            if (game != null && observedLevel != game.LevelIndex)
            {
                observedLevel = game.LevelIndex; autoReplay = false; hints = false;
                game.SetAssistancePreferences(false, false);
                notice = "Loaded " + layouts[game.LevelIndex].name + ". Proof replay starts from a verified state.";
            }
            if (game != null && game.AutoHintsEnabled && verified[game.LevelIndex].Steps.Any(s => s.Outcome == Tidebound.Board.ForwardPathOutcome.Blocked))
            { hints = false; game.SetAssistancePreferences(false, false); }
            if (autoReplay && game != null && !game.IsBusy && !game.IsPaused && game.IsEntryReady) ReplayStep();
        }

        public bool ReplayStep()
        {
            if (game == null || game.IsBusy || game.IsPaused || !game.IsEntryReady) return false;
            var proof = verified[game.LevelIndex]; var hash = LevelStateIdentity.Fingerprint(game.Session.Board);
            var step = proof.Steps.FirstOrDefault(s => s.BeforeHash == hash);
            if (step == null)
            {
                autoReplay = false;
                notice = game.Session.Board.ShipCount == 0 ? "Proof board cleared; waiting for lane/combat." : "Current state is off the recorded route. Restart or inspect in Level Studio.";
                return false;
            }
            game.ClickShip(step.ShipId); notice = step.ShipId + " / " + step.Outcome; return true;
        }

        private void Select(int index)
        {
            if (game.IsBusy || !game.IsEntryReady) return;
            autoReplay = false; game.SelectLevel(index);
            hints = false; game.SetAssistancePreferences(false, false);
            notice = "Independent review level loaded. Current B hint integration remains pending.";
        }

        private void OnGUI()
        {
            if (game == null) return;
            var area = new Rect(8, 8, Math.Min(Screen.width - 16, 520), controlsOpen ? 128 : 32);
            GUI.color = new Color(.025f, .07f, .1f, .95f); GUI.DrawTexture(area, Texture2D.whiteTexture); GUI.color = Color.white;
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(layouts[game.LevelIndex].name + " | Review / no saves");
            if (GUILayout.Button(controlsOpen ? "−" : "+", GUILayout.Width(26))) controlsOpen = !controlsOpen;
            GUILayout.EndHorizontal();
            if (!controlsOpen) { GUILayout.EndArea(); return; }
            GUILayout.BeginHorizontal();
            GUI.enabled = !game.IsBusy && game.IsEntryReady;
            if (GUILayout.Button("◀")) Select((game.LevelIndex + layouts.Length - 1) % layouts.Length);
            if (GUILayout.Button("▶")) Select((game.LevelIndex + 1) % layouts.Length);
            if (GUILayout.Button("Restart")) { autoReplay = false; game.Restart(); }
            if (GUILayout.Button("Proof step")) ReplayStep();
            if (GUILayout.Button(autoReplay ? "Stop replay" : "Replay proof")) autoReplay = !autoReplay;
            GUI.enabled = true; GUILayout.EndHorizontal();
            // Current hint picks direct exits only. Keep it explicitly restricted to A controls in this experiment.
            var dynamic = verified[game.LevelIndex].Steps.Any(s => s.Outcome == Tidebound.Board.ForwardPathOutcome.Blocked);
            GUI.enabled = !dynamic;
            var enabledHints = GUILayout.Toggle(hints, dynamic ? "B: verified dynamic hints pending" : "A: compare existing 5-second hints");
            if (enabledHints != hints) { hints = enabledHints; game.SetAssistancePreferences(hints, false); }
            GUI.enabled = true;
            GUILayout.Label(notice); GUILayout.EndArea();
        }
    }
}
