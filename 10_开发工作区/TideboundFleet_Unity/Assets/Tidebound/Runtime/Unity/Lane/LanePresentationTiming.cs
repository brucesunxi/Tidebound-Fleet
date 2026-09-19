using System;

namespace Tidebound.Unity.Lane
{
    [Serializable]
    public sealed class LanePresentationTiming
    {
        public const float DefaultMiniatureScale = 0.45f;
        public const float DefaultFleetArrivalScale = 0.20f;

        public float MiniatureScale { get; }
        public float FleetArrivalScale { get; }

        public LanePresentationTiming(
            float miniatureScale = DefaultMiniatureScale,
            float fleetArrivalScale = DefaultFleetArrivalScale)
        {
            if (miniatureScale <= 0f || miniatureScale > 1f)
                throw new ArgumentOutOfRangeException(nameof(miniatureScale));
            if (fleetArrivalScale < 0f || fleetArrivalScale > miniatureScale)
                throw new ArgumentOutOfRangeException(nameof(fleetArrivalScale));
            MiniatureScale = miniatureScale;
            FleetArrivalScale = fleetArrivalScale;
        }

        // The lane owns the miniature pose immediately; there is no entry shrink tween.
        public float LaneScale(float laneProgress) => MiniatureScale;

        public float FleetEntryScale(float entryProgress)
        {
            var normalized = Math.Min(1f, Math.Max(0f, entryProgress));
            return MiniatureScale + (FleetArrivalScale - MiniatureScale) * normalized;
        }
    }
}
