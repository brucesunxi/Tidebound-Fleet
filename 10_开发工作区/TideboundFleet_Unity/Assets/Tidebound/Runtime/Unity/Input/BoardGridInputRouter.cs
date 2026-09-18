using System;
using Tidebound.Board;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tidebound.Unity.Input
{
    /// <summary>One board-sized input surface for dense levels. Per-ship colliders are not required.</summary>
    public sealed class BoardGridInputRouter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField] private GridWorldMapper mapper;

        private BoardGridSelection selection;
        private Action<string> submit;

        public void Configure(Func<BoardModel> boardProvider, Action<string> submitMove)
        {
            selection = new BoardGridSelection(boardProvider);
            submit = submitMove ?? throw new ArgumentNullException(nameof(submitMove));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (TryCell(eventData, out var cell)) selection?.PointerDown(cell);
            else selection?.Cancel();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left) return;
            if (TryCell(eventData, out var cell))
            {
                var shipId = selection?.PointerUp(cell);
                if (shipId != null) submit?.Invoke(shipId);
            }
            else selection?.Cancel();
        }

        public void OnPointerExit(PointerEventData eventData) => selection?.Cancel();

        private bool TryCell(PointerEventData eventData, out GridPosition cell)
        {
            cell = default;
            if (mapper == null) return false;
            var camera = inputCamera != null ? inputCamera : eventData.pressEventCamera;
            if (camera == null) camera = Camera.main;
            return camera != null && mapper.TryRayToCell(camera.ScreenPointToRay(eventData.position), out cell);
        }

        private void OnDisable() => selection?.Cancel();
    }
}
