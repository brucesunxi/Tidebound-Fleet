using System.Linq;
using Tidebound.LevelDesign;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private LevelEntrySequence entry;
        private bool animateEntry, reducedEntryMotion, entryRequestInFlight, entryPauseOwned;
        private SavedGameRuntime pendingEntry;
        private bool pendingRestart;
        private RectTransform entryErrorPanel;
        private ShipPrototypeAppearance[] entryBodies;
        public bool IsEntryReady => entry == null || entry.IsReady;
        public LevelEntryPhase EntryPhase => entry?.Phase ?? LevelEntryPhase.Ready;
        public bool IsResumingEntry => entry?.IsResume == true;
        public bool IsEntrySaveBlocked => pendingEntry != null;

        public void SetReducedEntryMotion(bool reduced, bool persist = false)
        {
            reducedEntryMotion = reduced;
            if (persist) { PlayerPrefs.SetInt("Tidebound.ReducedEntryMotion", reduced ? 1 : 0); PlayerPrefs.Save(); }
            if (!IsEntryReady && !IsEntrySaveBlocked) BeginEntry(entry.IsResume);
            UpdateEntryLabels();
        }
        private void BuildEntryControls(Transform parent)
        {
            entryErrorPanel = Panel("EntrySaveError", parent, new Rect(), Tidebound.Unity.UI.HarborUI.Cream);
            entryErrorPanel.GetComponent<Image>().raycastTarget = true;
            Label("Title", entryErrorPanel, "Unable to save this challenge", 22);
            Label("Explanation", entryErrorPanel, "Your progress and coins have not changed.\nRetry when storage is available.", 16);
            Button("Retry", entryErrorPanel, "Retry", RetryEntry);
            entryErrorPanel.gameObject.SetActive(false);
            Button("EntryMotion", menuPanel, "", () => SetReducedEntryMotion(!reducedEntryMotion, true));
        }
        private void BeginEntry(bool resume)
        {
            entry = new LevelEntrySequence(); entry.Begin(resume, reducedEntryMotion, !animateEntry || (campaign && IsCleared));
            input.CancelSelection(); NotifyUserActivity();
            entryBodies = session.Board.Ships.OrderByDescending(s => s.Position.Y).ThenBy(s => s.Position.X).ThenBy(s => s.Id)
                .Select(s => shipViews[s.Id].GetComponentInChildren<ShipPrototypeAppearance>(true)).ToArray();
            PaintEntry(); UpdateLabels();
        }
        private void PaintEntry()
        {
            if (entryBodies == null || entry == null || IsEntrySaveBlocked) return;
            for (var i = 0; i < entryBodies.Length; i++)
            {
                var body = entryBodies[i]; if (body == null) continue;
                var a = entry.Alpha(i, entryBodies.Length);
                body.SetEntry(a, entry.IsResume || entry.ReducedMotion ? 1 : .9f + .1f * a);
            }
        }
        private bool TickEntry(float seconds)
        {
            if (IsEntrySaveBlocked) { UpdateEntryLabels(); return true; }
            if (IsEntryReady) return false;
            entry.Advance(seconds, backgrounded || IsPaused || IsMenuOpen);
            PaintEntry(); UpdateEntryLabels();
            if (IsEntryReady) { input.CancelSelection(); NotifyUserActivity(); notice = "Tap a ship to move forward."; }
            // Never advance the world or start the 5-second timer on the frame that becomes Ready.
            return true;
        }
        private bool CommitEntry(SavedGameRuntime next, int index, bool restart)
        {
            if (entryRequestInFlight) return false;
            entryRequestInFlight = true;
            try
            {
                var saved = restart ? saveService.Restart(next) : saveService.Start(next);
                if (!saved)
                {
                    pendingEntry = next; pendingRestart = restart;
                    if (world == null) BindWorld(next, index);
                    if (!IsPaused && session.State == Tidebound.Core.GameState.Playing)
                    { entryPauseOwned = true; world.PresentationPause = true; movement.Pause(); }
                    entry = new LevelEntrySequence(); entry.WaitForSave();
                    input.CancelSelection(); NotifyUserActivity();
                    entryErrorPanel.gameObject.SetActive(true); UpdateLabels(); return false;
                }
                pendingEntry = null;
                if (world != next) { if (restart) progress?.EndForRestart(); ClearSession(); BindWorld(next, index); }
                if (entryPauseOwned) { movement.Resume(); world.PresentationPause = false; entryPauseOwned = false; }
                entryErrorPanel.gameObject.SetActive(false); BeginEntry(false); return true;
            }
            finally { entryRequestInFlight = false; }
        }
        public void RetryEntry()
        {
            if (pendingEntry == null || entryRequestInFlight) return;
            var next = pendingEntry; CommitEntry(next, next.LevelNumber - 1, pendingRestart);
        }
        private void LayoutEntryControls()
        {
            if (entryErrorPanel == null) return;
            Place(entryErrorPanel, Layout.Safe);
            var w = Layout.Safe.width; var y = Layout.Safe.height / 2;
            Place((RectTransform)entryErrorPanel.Find("Title"), new Rect(20, y + 48, w - 40, 64));
            Place((RectTransform)entryErrorPanel.Find("Explanation"), new Rect(20, y - 24, w - 40, 64));
            Place((RectTransform)entryErrorPanel.Find("Retry"), new Rect(24, y - 96, w - 48, 48));
            Place((RectTransform)menuPanel.Find("EntryMotion"), new Rect(16, y - 308, w - 32, 48));
        }
        private void UpdateEntryLabels()
        {
            if (menuPanel != null && menuPanel.Find("EntryMotion") != null)
                menuPanel.Find("EntryMotion").GetComponentInChildren<Text>().text = "Entry motion: " + (reducedEntryMotion ? "Reduced" : "Full");
            if (!IsEntryReady && status != null)
                status.text = IsEntrySaveBlocked ? "Save failed. Retry to enter." : IsPaused ? "Entry paused" : IsResumingEntry ? "Resuming challenge..." : EntryPhase == LevelEntryPhase.Field ? "Preparing the sea..." : "Ships taking position...";
        }
        private void ClearEntry()
        {
            if (pendingEntry != null && pendingEntry != world) pendingEntry.Dispose();
            pendingEntry = null; entry = null; entryBodies = null; entryErrorPanel = null; entryPauseOwned = false;
        }
    }
}
