using System;
using Tidebound.Lane;
using UnityEngine;

namespace Tidebound.Unity.Lane
{
    /// <summary>Scene-authored waypoints for the thin perimeter lane and central fleet ingress.</summary>
    public sealed class LanePathLayout : MonoBehaviour, ILanePathProvider
    {
        [SerializeField] private Transform topLeft;
        [SerializeField] private Transform topCenter;
        [SerializeField] private Transform topRight;
        [SerializeField] private Transform bottomLeft;
        [SerializeField] private Transform bottomRight;
        [SerializeField] private Transform fleetIngress;

        public Vector3 FleetIngressPosition => Require(fleetIngress, nameof(fleetIngress)).position;

        public LaneWorldPath CreatePath(LaneRoute route, Vector3 startPosition)
        {
            switch (route)
            {
                case LaneRoute.Top:
                    return new LaneWorldPath(startPosition, Require(topCenter, nameof(topCenter)).position);
                case LaneRoute.Left:
                    return new LaneWorldPath(startPosition,
                        Require(topLeft, nameof(topLeft)).position,
                        Require(topCenter, nameof(topCenter)).position);
                case LaneRoute.Right:
                    return new LaneWorldPath(startPosition,
                        Require(topRight, nameof(topRight)).position,
                        Require(topCenter, nameof(topCenter)).position);
                case LaneRoute.BottomViaLeft:
                    return new LaneWorldPath(startPosition,
                        Require(bottomLeft, nameof(bottomLeft)).position,
                        Require(topLeft, nameof(topLeft)).position,
                        Require(topCenter, nameof(topCenter)).position);
                case LaneRoute.BottomViaRight:
                    return new LaneWorldPath(startPosition,
                        Require(bottomRight, nameof(bottomRight)).position,
                        Require(topRight, nameof(topRight)).position,
                        Require(topCenter, nameof(topCenter)).position);
                default:
                    throw new ArgumentOutOfRangeException(nameof(route));
            }
        }

        private static Transform Require(Transform value, string fieldName)
        {
            if (value == null) throw new InvalidOperationException($"LanePathLayout requires '{fieldName}'.");
            return value;
        }
    }
}
