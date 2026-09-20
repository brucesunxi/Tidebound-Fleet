using System;

namespace Tidebound.LevelDesign
{
    public enum LevelEntryPhase { WaitingForSave, Field, Ships, Ready }

    /// <summary>Presentation clock only. Grid, attempts and rewards remain owned by their existing services.</summary>
    public sealed class LevelEntrySequence
    {
        public const double FieldSeconds = .18, ShipsSeconds = .48, ResumeSeconds = .12;
        public LevelEntryPhase Phase { get; private set; } = LevelEntryPhase.Ready;
        public bool IsReady => Phase == LevelEntryPhase.Ready;
        public bool IsResume { get; private set; }
        public bool ReducedMotion { get; private set; }
        public double Elapsed { get; private set; }
        public void WaitForSave() { Phase = LevelEntryPhase.WaitingForSave; Elapsed = 0; }
        public void Begin(bool resume, bool reducedMotion, bool skipPresentation = false)
        {
            IsResume = resume; ReducedMotion = reducedMotion; Elapsed = 0;
            Phase = skipPresentation ? LevelEntryPhase.Ready : resume || reducedMotion ? LevelEntryPhase.Ships : LevelEntryPhase.Field;
        }
        public void Advance(double seconds, bool suspended)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsReady || Phase == LevelEntryPhase.WaitingForSave || suspended) return;
            Elapsed += seconds;
            var shortPath = IsResume || ReducedMotion;
            var duration = shortPath ? ResumeSeconds : FieldSeconds + ShipsSeconds;
            Phase = Elapsed + 1e-7 >= duration ? LevelEntryPhase.Ready : !shortPath && Elapsed < FieldSeconds ? LevelEntryPhase.Field : LevelEntryPhase.Ships;
        }
        public float Alpha(int index, int count)
        {
            if (count <= 0 || index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            if (IsReady) return 1;
            if (Phase == LevelEntryPhase.WaitingForSave || Phase == LevelEntryPhase.Field) return 0;
            if (IsResume || ReducedMotion) return Clamp(Elapsed / ResumeSeconds);
            // At most eight overlapping groups; 80 ships do not take eighty animation durations.
            var groups = Math.Min(8, count); var group = index * groups / count;
            var delay = groups == 1 ? 0 : group * .28 / (groups - 1);
            return Clamp((Elapsed - FieldSeconds - delay) / .20);
        }
        private static float Clamp(double value) => (float)Math.Max(0, Math.Min(1, value));
    }
}
