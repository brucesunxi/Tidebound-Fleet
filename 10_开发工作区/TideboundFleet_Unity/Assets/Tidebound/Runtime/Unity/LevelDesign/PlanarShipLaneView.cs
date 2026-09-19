using System;
using Tidebound.Unity.Lane;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>XY presentation adapter for the existing lane controller; no gameplay decisions.</summary>
    public sealed class PlanarShipLaneView : MonoBehaviour, ILaneTransitView
    {
        public string ShipId { get; private set; }
        public Vector3 CurrentPosition => transform.position;
        public void Configure(string id) => ShipId = id ?? throw new ArgumentNullException(nameof(id));
        public void ApplyLanePose(Vector3 position, Vector3 forward, float scale)
        {
            transform.position = position;
            if (forward.sqrMagnitude > .000001f)
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90);
            transform.localScale = Vector3.one * scale;
        }
        public void CompleteFleetEntry() => gameObject.SetActive(false);
    }
}
