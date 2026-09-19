using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebound.Unity.Ship
{
    /// <summary>
    /// Default Transform presentation and pointer adapter. Collider/Graphic hit areas are input only and
    /// never determine logical footprint or movement permission.
    /// </summary>
    public sealed class ShipMovementView : MonoBehaviour, IShipMovementView, IPointerClickHandler
    {
        [SerializeField] private string shipId;
        [SerializeField] private Transform visualRoot;

        private Action<string> clickHandler;
        private Coroutine animationRoutine;
        private bool paused;

        public string ShipId => shipId;
        private Transform VisualRoot => visualRoot != null ? visualRoot : transform;

        public void ConfigureShipId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A ship id is required.", nameof(value));
            shipId = value.Trim();
        }

        public void BindClickHandler(Action<string> handler) => clickHandler = handler;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            clickHandler?.Invoke(shipId);
        }

        public void PlayTravel(Vector3 targetTailWorld, float duration, Action completed)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            StartExclusive(TravelRoutine(targetTailWorld, duration, completed));
        }

        public void PlayBlockedFeedback(Vector3 lateralOffset, float duration, Action completed)
        {
            if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration));
            StartExclusive(BlockedFeedbackRoutine(lateralOffset, duration, completed));
        }

        public void SetPaused(bool value) => paused = value;

        private void StartExclusive(IEnumerator routine)
        {
            if (animationRoutine != null)
                throw new InvalidOperationException($"Ship view '{shipId}' already has an active animation.");
            animationRoutine = StartCoroutine(routine);
        }

        private IEnumerator TravelRoutine(Vector3 target, float duration, Action completed)
        {
            var root = VisualRoot;
            var start = root.position;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!paused)
                {
                    elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                    var t=elapsed/duration;
                    root.position = Vector3.LerpUnclamped(start, target, t);
                }
                yield return null;
            }
            root.position = target;
            animationRoutine = null;
            completed?.Invoke();
        }

        private IEnumerator BlockedFeedbackRoutine(Vector3 lateralOffset, float duration, Action completed)
        {
            var root = VisualRoot;
            var anchor = root.position;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!paused)
                {
                    elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                    var t = elapsed / duration;
                    root.position = anchor + lateralOffset * Mathf.Sin(t * Mathf.PI * 2f);
                }
                yield return null;
            }
            root.position = anchor;
            animationRoutine = null;
            completed?.Invoke();
        }

        private void OnDestroy()
        {
            clickHandler = null;
            animationRoutine = null;
        }
    }
}
