using System;

namespace Tidebound.Lane
{
    public sealed class LaneTransitTiming
    {
        public const double DefaultLaneDuration = 1.20d;
        public const double DefaultEntranceInterval = 0.15d;
        public const double DefaultFleetEntryDuration = 0.15d;

        public double LaneDuration { get; }
        public double EntranceInterval { get; }
        public double FleetEntryDuration { get; }

        public LaneTransitTiming(
            double laneDuration = DefaultLaneDuration,
            double entranceInterval = DefaultEntranceInterval,
            double fleetEntryDuration = DefaultFleetEntryDuration)
        {
            LaneDuration = PositiveFinite(laneDuration, nameof(laneDuration));
            EntranceInterval = PositiveFinite(entranceInterval, nameof(entranceInterval));
            FleetEntryDuration = PositiveFinite(fleetEntryDuration, nameof(fleetEntryDuration));
        }

        private static double PositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }
}
