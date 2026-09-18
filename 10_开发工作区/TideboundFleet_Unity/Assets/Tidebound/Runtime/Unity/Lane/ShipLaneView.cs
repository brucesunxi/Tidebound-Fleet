using System;
using UnityEngine;

namespace Tidebound.Unity.Lane
{
    /// <summary>Default lane Transform adapter. Its scale and hit area never affect logical occupancy.</summary>
    public sealed class ShipLaneView : MonoBehaviour, ILaneTransitView
    {
        [SerializeField] private string shipId;
        [SerializeField] private Transform visualRoot;

        private Vector3 initialScale;
        private bool initialized;

        public string ShipId => shipId;
        public Vector3 CurrentPosition => VisualRoot.position;
        private Transform VisualRoot => visualRoot != null ? visualRoot : transform;

        public void ConfigureShipId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A ship id is required.", nameof(value));
            shipId = value.Trim();
        }

        public void ApplyLanePose(Vector3 position, Vector3 forward, float scale)
        {
            if (scale < 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            EnsureInitialized();
            var root = VisualRoot;
            root.position = position;
            if (forward.sqrMagnitude > 0.000001f)
                root.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            root.localScale = initialScale * scale;
        }

        public void CompleteFleetEntry()
        {
            EnsureInitialized();
            VisualRoot.gameObject.SetActive(false);
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialScale = VisualRoot.localScale;
            initialized = true;
        }
    }
}
