using System;
using Tidebound.Board;
using UnityEngine;

namespace Tidebound.Unity.Ship
{
    /// <summary>Presentation contract. Implementations may animate Transforms but never calculate occupancy.</summary>
    public interface IShipMovementView
    {
        string ShipId { get; }
        void BindClickHandler(Action<string> handler);
        void PlayTravel(Vector3 targetTailWorld, float duration, Action completed);
        void PlayBlockedFeedback(Vector3 lateralOffset, float duration, Action completed);
        void SetPaused(bool paused);
    }

    public interface IGridWorldMapper
    {
        float CellSize { get; }
        Vector3 TailToWorld(GridPosition tail);
        Vector3 DirectionToWorld(global::Tidebound.Ship.ShipDirection direction);
    }
}
