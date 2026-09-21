using Tidebound.Core;
using Tidebound.Board;
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
        private bool backgrounded, inputSinceTick;
        private float deadlockNoticeTime;
        private BoardModel deadlockBoard;
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
            deadlockPanel = Panel("DeadlockPrompt", parent, new Rect(), new Color(.025f, .055f, .08f, .68f));
            deadlockPanel.GetComponent<Image>().raycastTarget = false;
            deadlockView = deadlockPanel.gameObject.AddComponent<DeadlockPanel>();
            deadlockView.Initialize(font);
            deadlockPanel.gameObject.SetActive(false);
            Button("AutoHints", menuPanel, "", () => SetAssistancePreferences(!autoHintsEnabled, reducedHintMotion, true));
            Button("HintMotion", menuPanel, "", () => SetAssistancePreferences(autoHintsEnabled, !reducedHintMotion, true));
        }
        private void TickAssistance(float seconds)
        {
            if (assistance == null || IsHomeOpen) return;
            if (IsDeadlockOpen)
            {
                if (!ReferenceEquals(deadlockBoard, session.Board)) CloseDeadlock();
                else if (!backgrounded && !IsPaused && !IsMenuOpen && !IsAcquisitionOpen && !IsCollectionOpen)
                {
                    deadlockNoticeTime += seconds;
                    if (deadlockNoticeTime >= 4) CloseDeadlock();
                }
            }
            // Includes empty-board taps, held pointers, keyboard/controller submit and scrolling over HUD.
            var activity = inputSinceTick || input.IsPointerHeld || UnityEngine.Input.touchCount > 0 ||
                UnityEngine.Input.GetMouseButton(0) || UnityEngine.Input.GetMouseButtonUp(0) ||
                UnityEngine.Input.anyKey || UnityEngine.Input.mouseScrollDelta.sqrMagnitude > 0;
            inputSinceTick = false;
            var eligible = IsEntryReady && !IsEntrySaveBlocked && !backgrounded && session.State == GameState.Playing && !IsBusy &&
                world.PendingMoveId == null && demo == null && !IsCollectionOpen && !IsMenuOpen && !IsAcquisitionOpen &&
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
            body.GetComponent<ShipPrototypeAppearance>().SetHint(strength);
        }
        private void ClearAutoHighlight()
        {
            if (paintedHint != null && shipViews.TryGetValue(paintedHint, out var view) && view != null && session != null)
                view.GetComponentInChildren<ShipPrototypeAppearance>(true).SetHint(0);
            paintedHint = null; hintPhase = 0;
        }
        private void OpenDeadlock()
        {
            NotifyUserActivity(); deadlockBoard = session.Board; deadlockNoticeTime = 0;
            deadlockPanel.gameObject.SetActive(true); UpdateAssistanceLabels();
        }
        public void CloseDeadlock()
        {
            if (!IsDeadlockOpen) return;
            deadlockPanel.gameObject.SetActive(false); deadlockNoticeTime = 0;
            UpdateToolCue(ShipTool.None); NotifyUserActivity();
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
            CloseDeadlock();
            if (homeNavigation) { ReturnHome(); OpenHomeShop(); return; }
            world.PresentationPause = session.State == GameState.Playing;
            ShowAcquisition(ShipTool.None);
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
            {
                deadlockView.Present(toolInventory, tools.Enabled, tools.UsesLeft);
                deadlockView.SetVisibility(!IsPaused && !IsMenuOpen && !IsAcquisitionOpen && !IsCollectionOpen);
                UpdateToolCue(deadlockView.RecommendedTool);
            }
            else UpdateToolCue(ShipTool.None);
        }
        private void UpdateToolCue(ShipTool recommended)
        {
            var kinds = new[] { ShipTool.Rescue, ShipTool.Shuffle, ShipTool.Reverse };
            var buttons = new[] { rescueButton, shuffleButton, reverseButton };
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var outline = buttons[i].GetComponent<Outline>();
                if (outline == null) { outline = buttons[i].gameObject.AddComponent<Outline>(); outline.effectColor = new Color(1, .85f, .2f); outline.effectDistance = new Vector2(3, 3); }
                outline.enabled = kinds[i] == recommended && !IsPaused && !IsMenuOpen && !IsAcquisitionOpen;
            }
        }
    }
}
