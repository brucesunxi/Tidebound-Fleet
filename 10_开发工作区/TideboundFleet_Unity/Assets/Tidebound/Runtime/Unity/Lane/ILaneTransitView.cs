using System;
using Tidebound.Lane;
using UnityEngine;

namespace Tidebound.Unity.Lane
{
    public interface ILaneTransitView
    {
        string ShipId { get; }
        Vector3 CurrentPosition { get; }
        void ApplyLanePose(Vector3 position, Vector3 forward, float scale);
        void CompleteFleetEntry();
    }

    public interface ILanePathProvider
    {
        LaneWorldPath CreatePath(LaneRoute route, Vector3 startPosition);
        Vector3 FleetIngressPosition { get; }
    }
}
