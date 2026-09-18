using System;

namespace Tidebound.Unity.Lane
{
    [Serializable]
    public sealed class LanePresentationTiming
    {
        public const float DefaultMiniatureScale = 0.45f;
        public const float DefaultShrinkProgress = 0.20f;
        public const float DefaultFleetArrivalScale = 0.20f;

        public float MiniatureScale { get; }
        public float ShrinkProgress { get; }
        public float FleetArrivalScale { get; }

        public LanePresentationTiming(
            float miniatureScale = DefaultMiniatureScale,
            float shrinkProgress = DefaultShrinkProgress,
            float fleetArrivalScale = DefaultFleetArrivalScale)
        {
            if (miniatureScale <= 0f || miniatureScale > 1f)
                throw new ArgumentOutOfRangeException(nameof(miniatureScale));
            if (shrinkProgress <= 0f || shrinkProgress > 1f)
                throw new ArgumentOutOfRangeException(nameof(shrinkProgress));
            if (fleetArrivalScale < 0f || fleetArrivalScale > miniatureScale)
                throw new ArgumentOutOfRangeException(nameof(fleetArrivalScale));
            MiniatureScale = miniatureScale;
            ShrinkProgress = shrinkProgress;
            FleetArrivalScale = fleetArrivalScale;
        }

        public float LaneScale(float laneProgress)
        {
            var normalized = Math.Min(1f, Math.Max(0f, laneProgress / ShrinkProgress));
            return 1f + (MiniatureScale - 1f) * normalized;
        }

        public float FleetEntryScale(float entryProgress)
        {
            var normalized = Math.Min(1f, Math.Max(0f, entryProgress));
            return MiniatureScale + (FleetArrivalScale - MiniatureScale) * normalized;
        }
    }
}
