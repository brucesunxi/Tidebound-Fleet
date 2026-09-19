using System;
using Tidebound.Unity.Lane;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>XY presentation adapter for the existing lane controller; no gameplay decisions.</summary>
    public sealed class PlanarShipLaneView : MonoBehaviour, ILaneTransitView
    {
        private Vector3 visualCenterOffset;
        public Vector3 VisualCenter => transform.TransformPoint(visualCenterOffset);
        public string ShipId { get; private set; }
        public Vector3 CurrentPosition => VisualCenter;
        public void Configure(string id, Vector3 centerOffset)
        {
            ShipId = id ?? throw new ArgumentNullException(nameof(id));
            visualCenterOffset = centerOffset;
        }
        public void ApplyLanePose(Vector3 position, Vector3 forward, float scale)
        {
            if (forward.sqrMagnitude > .000001f)
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90);
            transform.localScale = Vector3.one * scale;
            // Board movement uses a tail anchor; the lane path describes the visual center.
            transform.position = position - transform.TransformVector(visualCenterOffset);
        }
        public void CompleteFleetEntry() => gameObject.SetActive(false);
    }
}
