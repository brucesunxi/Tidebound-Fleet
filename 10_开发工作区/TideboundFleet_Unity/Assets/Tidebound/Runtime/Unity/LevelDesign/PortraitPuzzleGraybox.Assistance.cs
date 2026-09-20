using Tidebound.Core;
using Tidebound.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private BoardAssistance assistance;
        private DeadlockPanel deadlockView;
        private RectTransform deadlockPanel;
        private bool deadlockPauseOwned, backgrounded, inputSinceTick;
        private string paintedHint;
        private float hintPhase;
        private bool autoHintsEnabled = true, reducedHintMotion;
        public bool IsDeadlockOpen => deadlockPanel != null && deadlockPanel.gameObject.activeSelf;
        public string AutoHintShipId => assistance?.HighlightedShipId;
        public DeadlockPanel DeadlockView => deadlockView;
        public bool AutoHintsEnabled => autoHintsEnabled;

        public void SetAssistancePreferences(bool enabled, bool reducedMotion, bool persist = false)
        {
            autoHintsEnabled = enabled; reducedHintMotion = reducedMotion; NotifyUserActivity();
            if (persist)
            {
                PlayerPrefs.SetInt("Tidebound.AutoHints", enabled ? 1 : 0);
                PlayerPrefs.SetInt("Tidebound.ReducedHintMotion", reducedMotion ? 1 : 0); PlayerPrefs.Save();
            }
            UpdateAssistanceLabels();
        }
        public void NotifyUserActivity()
        { inputSinceTick = true; assistance?.ResetIdle(); ClearAutoHighlight(); }

        private void BuildAssistance(Transform parent)
        {
            assistance = new BoardAssistance();
            deadlockPanel = Panel("DeadlockPrompt", parent, new Rect(), new Color(.025f, .055f, .08f, .86f));
            deadlockPanel.GetComponent<Image>().raycastTarget = true;
            deadlockView = deadlockPanel.gameObject.AddComponent<DeadlockPanel>();
            deadlockView.Initialize(font, UseDeadlockTool, OpenShopFromDeadlock, RestartFromDeadlock, CloseDeadlock);
            deadlockPanel.gameObject.SetActive(false);
            Button("AutoHints", menuPanel, "", () => SetAssistancePreferences(!autoHintsEnabled, reducedHintMotion, true));
            Button("HintMotion", menuPanel, "", () => SetAssistancePreferences(autoHintsEnabled, !reducedHintMotion, true));
        }
        private void TickAssistance(float seconds)
        {
            if (assistance == null) return;
            // Includes empty-board taps, held pointers, keyboard/controller submit and scrolling over HUD.
            var activity = inputSinceTick || input.IsPointerHeld || UnityEngine.Input.touchCount > 0 ||
                UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetMouseButtonUp(0) ||
                UnityEngine.Input.anyKey || UnityEngine.Input.mouseScrollDelta.sqrMagnitude > 0;
            inputSinceTick = false;
            var eligible = !backgrounded && session.State == GameState.Playing && !IsBusy &&
                world.PendingMoveId == null && demo == null && !IsMenuOpen && !IsAcquisitionOpen && !IsDeadlockOpen &&
                tools.Selection == ShipTool.None && session.Board.ShipCount > 0;
            assistance.Advance(session.Board, seconds, eligible, activity, autoHintsEnabled);
            if (eligible && transit.ActiveCount == 0 && combat.PendingCount == 0 && combat.FaultReason == null &&
                world.IsTerminalDeadlock && assistance.TryAnnounceDeadlock()) OpenDeadlock();
            PaintAutoHighlight(seconds);
            UpdateAssistanceLabels();
        }
        private void PaintAutoHighlight(float seconds)
        {
            var id = assistance.HighlightedShipId;
            if (id != paintedHint) { ClearAutoHighlight(); paintedHint = id; hintPhase = 0; }
            if (id == null) return;
            if (!session.Board.TryGetShip(id, out var ship) || !session.Board.QueryForwardPath(id).CanExit)
            { assistance.ResetIdle(); ClearAutoHighlight(); return; }
            hintPhase += seconds;
            var strength = reducedHintMotion ? 1 : .65f + .35f * Mathf.Sin(hintPhase / .7f * Mathf.PI * 2);
            var body = shipViews[id].transform.Find("Body");
            body.GetComponent<Image>().color = Color.Lerp(BodyColor(ship.Length), new Color(.98f, .78f, .22f), strength);
        }
        private void ClearAutoHighlight()
        {
            if (paintedHint != null && shipViews.TryGetValue(paintedHint, out var view) && view != null && session != null)
                view.transform.Find("Body").GetComponent<Image>().color = BodyColor(session.GetShip(paintedHint).Length);
            paintedHint = null; hintPhase = 0;
        }
        private void OpenDeadlock()
        {
            NotifyUserActivity(); input.CancelSelection();
            deadlockPauseOwned = session.State == GameState.Playing;
            if (deadlockPauseOwned) { world.PresentationPause = true; movement.Pause(); }
            deadlockPanel.gameObject.SetActive(true); SaveCheckpoint(true); UpdateAssistanceLabels();
        }
        public void CloseDeadlock()
        {
            if (!IsDeadlockOpen) return;
            NotifyUserActivity(); deadlockPanel.gameObject.SetActive(false);
            if (deadlockPauseOwned && IsPaused && SaveCheckpoint(true)) movement.Resume();
            deadlockPauseOwned = false; world.PresentationPause = false;
            SaveCheckpoint(true); UpdateLabels();
        }
        public void UseDeadlockTool(ShipTool kind)
        {
            if (!IsDeadlockOpen || !tools.Enabled || tools.UsesLeft == 0 || !toolInventory.IsAvailable) return;
            if (tools.Remaining(kind) == 0) { OpenShopFromDeadlock(); return; }
            CloseDeadlock(); if (!IsPaused) SelectTool(kind);
        }
        public void OpenShopFromDeadlock()
        {
            if (!IsDeadlockOpen || !tools.Enabled || saveService?.IsAvailable != true) return;
            // Transfer the temporary pause without resuming a frame between the two modals.
            acquisitionPauseOwned = deadlockPauseOwned;
            deadlockPauseOwned = false; deadlockPanel.gameObject.SetActive(false);
            NotifyUserActivity(); shopPanel.Open(); SaveCheckpoint(true); UpdateLabels();
        }
        public void RestartFromDeadlock()
        {
            if (!IsDeadlockOpen) return;
            NotifyUserActivity(); Restart();
        }
        private void UpdateAssistanceLabels()
        {
            if (menuPanel != null && menuPanel.Find("AutoHints") != null)
            {
                menuPanel.Find("AutoHints").GetComponentInChildren<Text>().text = "Auto hints: " + (autoHintsEnabled ? "On" : "Off");
                menuPanel.Find("HintMotion").GetComponentInChildren<Text>().text = "Hint motion: " + (reducedHintMotion ? "Reduced" : "Pulse");
            }
            if (IsDeadlockOpen)
                deadlockView.Present(toolInventory, tools.Enabled, tools.UsesLeft, tools.Enabled && saveService?.IsAvailable == true,
                    saveService?.IsAvailable == true, saveService?.IsAvailable == true ? saveService.RestartReward : 0);
        }
    }
}
