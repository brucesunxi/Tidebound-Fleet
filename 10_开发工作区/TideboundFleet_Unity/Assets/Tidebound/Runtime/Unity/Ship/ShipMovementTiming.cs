using System;
using UnityEngine;

namespace Tidebound.Unity.Ship
{
    [Serializable]
    public sealed class ShipMovementTiming
    {
        [SerializeField, Min(0.01f)] private float gridCellsPerSecond = 12f;
        [SerializeField, Min(0.01f)] private float minimumTravelDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float maximumTravelDuration = 0.60f;
        [SerializeField, Min(0.01f)] private float blockedFeedbackDuration = 0.12f;
        [SerializeField, Range(0.01f, 0.25f)] private float blockedLateralCellRatio = 0.06f;

        public float BlockedFeedbackDuration
        {
            get
            {
                ValidateSerializedValues();
                return blockedFeedbackDuration;
            }
        }

        public ShipMovementTiming()
        {
        }

        public ShipMovementTiming(float gridCellsPerSecond, float minimumTravelDuration,
            float maximumTravelDuration, float blockedFeedbackDuration, float blockedLateralCellRatio)
        {
            if (gridCellsPerSecond <= 0f) throw new ArgumentOutOfRangeException(nameof(gridCellsPerSecond));
            if (minimumTravelDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(minimumTravelDuration));
            if (maximumTravelDuration < minimumTravelDuration)
                throw new ArgumentOutOfRangeException(nameof(maximumTravelDuration));
            if (blockedFeedbackDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(blockedFeedbackDuration));
            if (blockedLateralCellRatio <= 0f) throw new ArgumentOutOfRangeException(nameof(blockedLateralCellRatio));

            this.gridCellsPerSecond = gridCellsPerSecond;
            this.minimumTravelDuration = minimumTravelDuration;
            this.maximumTravelDuration = maximumTravelDuration;
            this.blockedFeedbackDuration = blockedFeedbackDuration;
            this.blockedLateralCellRatio = blockedLateralCellRatio;
        }

        public float CalculateTravelDuration(int gridDistance)
        {
            if (gridDistance <= 0) throw new ArgumentOutOfRangeException(nameof(gridDistance));
            ValidateSerializedValues();
            return Mathf.Clamp(gridDistance / gridCellsPerSecond, minimumTravelDuration, maximumTravelDuration);
        }

        public float CalculateBlockedLateralDistance(float cellSize)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            ValidateSerializedValues();
            return cellSize * blockedLateralCellRatio;
        }

        private void ValidateSerializedValues()
        {
            if (gridCellsPerSecond <= 0f || minimumTravelDuration <= 0f ||
                maximumTravelDuration < minimumTravelDuration || blockedFeedbackDuration <= 0f ||
                blockedLateralCellRatio <= 0f)
                throw new InvalidOperationException("Ship movement timing contains invalid serialized values.");
        }
    }
}
