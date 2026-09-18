using System;
using System.Collections.Generic;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Lane;
using UnityEngine;

namespace Tidebound.Unity.Lane
{
    /// <summary>Maps logical lane progress to views. It does not decide route order, timing, or fleet state.</summary>
    public sealed class LaneTransitController : IDisposable
    {
        private readonly TransitSystem transit;
        private readonly ILanePathProvider pathProvider;
        private readonly LanePresentationTiming presentation;
        private readonly Dictionary<string, ILaneTransitView> views =
            new Dictionary<string, ILaneTransitView>(StringComparer.Ordinal);
        private readonly Dictionary<string, LaneWorldPath> paths =
            new Dictionary<string, LaneWorldPath>(StringComparer.Ordinal);
        private readonly IDisposable fleetSubscription;
        private bool disposed;

        public int ActiveCount => transit.ActiveCount;

        public LaneTransitController(
            TransitSystem transit,
            ILanePathProvider pathProvider,
            IEnumerable<ILaneTransitView> views,
            LanePresentationTiming presentation = null)
        {
            this.transit = transit ?? throw new ArgumentNullException(nameof(transit));
            this.pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            this.presentation = presentation ?? new LanePresentationTiming();
            if (views == null) throw new ArgumentNullException(nameof(views));

            foreach (var view in views)
            {
                if (view == null) throw new ArgumentException("A lane view cannot be null.", nameof(views));
                if (string.IsNullOrWhiteSpace(view.ShipId))
                    throw new ArgumentException("Every lane view requires a ship id.", nameof(views));
                if (this.views.ContainsKey(view.ShipId))
                    throw new ArgumentException($"Duplicate lane view id '{view.ShipId}'.", nameof(views));
                this.views.Add(view.ShipId, view);
            }

            foreach (var ship in transit.Ships)
                if (!this.views.ContainsKey(ship.Id))
                    throw new ArgumentException($"Missing lane view for ship '{ship.Id}'.", nameof(views));

            fleetSubscription = transit.Events.Subscribe<ShipEnterFleetEvent>(OnShipEnterFleet);
        }

        public LaneAdvanceStatus Advance(double deltaTime)
        {
            ThrowIfDisposed();
            var status = transit.Advance(deltaTime);
            PresentActiveTransits();
            return status;
        }

        public void PresentActiveTransits()
        {
            ThrowIfDisposed();
            foreach (var operation in transit.GetActiveTransits()) Present(operation);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            fleetSubscription.Dispose();
            paths.Clear();
            views.Clear();
        }

        private void Present(LaneTransitOperation operation)
        {
            var view = views[operation.Ship.ShipId];
            if (!paths.TryGetValue(operation.Ship.ShipId, out var path))
            {
                path = pathProvider.CreatePath(operation.Route, view.CurrentPosition);
                paths.Add(operation.Ship.ShipId, path);
            }

            if (operation.Stage == LaneTransitStage.EnteringFleet)
            {
                var position = Vector3.LerpUnclamped(
                    path.End, pathProvider.FleetIngressPosition, operation.FleetEntryProgress);
                var forward = pathProvider.FleetIngressPosition - path.End;
                view.ApplyLanePose(position, forward,
                    presentation.FleetEntryScale(operation.FleetEntryProgress));
                return;
            }

            view.ApplyLanePose(
                path.Sample(operation.LaneProgress),
                path.Tangent(operation.LaneProgress),
                presentation.LaneScale(operation.LaneProgress));
        }

        private void OnShipEnterFleet(ShipEnterFleetEvent message)
        {
            if (!string.Equals(message.Ship.SessionId, transit.SessionId, StringComparison.Ordinal)) return;
            if (!views.TryGetValue(message.Ship.ShipId, out var view)) return;
            paths.Remove(message.Ship.ShipId);
            view.CompleteFleetEntry();
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(LaneTransitController));
        }
    }
}
