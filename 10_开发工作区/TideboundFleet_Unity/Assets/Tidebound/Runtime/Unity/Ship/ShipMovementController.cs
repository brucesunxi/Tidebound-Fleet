using System;
using System.Collections.Generic;
using Tidebound.Core;
using Tidebound.Ship;
using UnityEngine;

namespace Tidebound.Unity.Ship
{
    /// <summary>Coordinates logical completion points with pluggable Unity views. It owns no gameplay data.</summary>
    public sealed class ShipMovementController : IDisposable
    {
        private readonly ShipMovementSystem movement;
        private readonly IGridWorldMapper mapper;
        private readonly ShipMovementTiming timing;
        private readonly Dictionary<string, IShipMovementView> views =
            new Dictionary<string, IShipMovementView>(StringComparer.Ordinal);

        private PendingAdvance pendingAdvance;
        private long pendingOperationId;
        private bool disposed;
        private long lastPresentedOperationId;

        public ShipMoveOperation ActiveOperation => movement.ActiveOperation;
        public GameState GameState => movement.GameState;

        public ShipMovementController(ShipMovementSystem movement, IGridWorldMapper mapper,
            IEnumerable<IShipMovementView> views, ShipMovementTiming timing = null)
        {
            this.movement = movement ?? throw new ArgumentNullException(nameof(movement));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.timing = timing ?? new ShipMovementTiming();
            if (views == null) throw new ArgumentNullException(nameof(views));

            foreach (var view in views)
            {
                if (view == null) throw new ArgumentException("A ship view cannot be null.", nameof(views));
                if (string.IsNullOrWhiteSpace(view.ShipId))
                    throw new ArgumentException("Every ship view requires an id.", nameof(views));
                if (this.views.ContainsKey(view.ShipId))
                    throw new ArgumentException($"Duplicate ship view id '{view.ShipId}'.", nameof(views));
                this.views.Add(view.ShipId, view);
            }

            foreach (var ship in movement.Board.Ships)
                if (!this.views.ContainsKey(ship.Id))
                    throw new ArgumentException($"Missing movement view for board ship '{ship.Id}'.", nameof(views));

            foreach (var view in this.views.Values) view.BindClickHandler(RequestMoveFromView);
        }

        public bool StartPlaying()
        {
            ThrowIfDisposed();
            return movement.StartPlaying();
        }

        public ShipMoveRequestResult RequestMove(string shipId)
        {
            ThrowIfDisposed();
            var request = movement.TryBeginMove(shipId);
            if (!request.IsAccepted) return request;
            PresentActiveOperation();
            return request;
        }

        public bool PresentActiveOperation()
        {
            ThrowIfDisposed();
            var operation=movement.ActiveOperation;
            if(operation==null || operation.OperationId==lastPresentedOperationId)return false;
            lastPresentedOperationId=operation.OperationId;Present(operation);return true;
        }

        public bool Pause()
        {
            ThrowIfDisposed();
            if (!movement.Pause()) return false;
            SetViewsPaused(true);
            return true;
        }

        public bool Resume()
        {
            ThrowIfDisposed();
            if (!movement.Resume()) return false;
            SetViewsPaused(false);
            RetryPendingAdvance();
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var view in views.Values) view.BindClickHandler(null);
            views.Clear();
        }

        private void RequestMoveFromView(string shipId) => RequestMove(shipId);

        private void Present(ShipMoveOperation operation)
        {
            var view = views[operation.ShipId];
            if (operation.Stage == ShipMoveStage.BlockedFeedback)
            {
                PresentBlockedFeedback(operation, view);
                return;
            }

            var target = mapper.TailToWorld(operation.TargetTail);
            var duration = timing.CalculateTravelDuration(operation.TravelDistance);
            if(operation.IsRescue && view is IShipRescueView rescue)
                rescue.PlayRescue(target,duration,()=>OnTravelCompleted(operation.OperationId));
            else view.PlayTravel(target, duration, () => OnTravelCompleted(operation.OperationId));
        }

        private void PresentBlockedFeedback(ShipMoveOperation operation, IShipMovementView view)
        {
            var perpendicularDirection = operation.Direction == ShipDirection.Up || operation.Direction == ShipDirection.Down
                ? ShipDirection.Right
                : ShipDirection.Up;
            var lateral = mapper.DirectionToWorld(perpendicularDirection) *
                timing.CalculateBlockedLateralDistance(mapper.CellSize);
            view.PlayBlockedFeedback(lateral, timing.BlockedFeedbackDuration,
                () => OnBlockedFeedbackCompleted(operation.OperationId));
        }

        private void OnTravelCompleted(long operationId)
        {
            if (disposed) return;
            var status = movement.CompleteTravel(operationId);
            if (status == ShipMoveAdvanceStatus.SessionPaused)
            {
                SetPending(PendingAdvance.Travel, operationId);
                return;
            }
            if (status != ShipMoveAdvanceStatus.Applied) return;

            var operation = movement.ActiveOperation;
            if (operation != null && operation.OperationId == operationId &&
                operation.Stage == ShipMoveStage.BlockedFeedback)
                PresentBlockedFeedback(operation, views[operation.ShipId]);
        }

        private void OnBlockedFeedbackCompleted(long operationId)
        {
            if (disposed) return;
            var status = movement.CompleteBlockedFeedback(operationId);
            if (status == ShipMoveAdvanceStatus.SessionPaused)
                SetPending(PendingAdvance.BlockedFeedback, operationId);
        }

        private void SetPending(PendingAdvance pending, long operationId)
        {
            pendingAdvance = pending;
            pendingOperationId = operationId;
        }

        private void RetryPendingAdvance()
        {
            var pending = pendingAdvance;
            var operationId = pendingOperationId;
            pendingAdvance = PendingAdvance.None;
            pendingOperationId = 0;
            if (pending == PendingAdvance.Travel) OnTravelCompleted(operationId);
            else if (pending == PendingAdvance.BlockedFeedback) OnBlockedFeedbackCompleted(operationId);
        }

        private void SetViewsPaused(bool paused)
        {
            foreach (var view in views.Values) view.SetPaused(paused);
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(ShipMovementController));
        }

        private enum PendingAdvance
        {
            None = 0,
            Travel = 1,
            BlockedFeedback = 2
        }
    }
}
