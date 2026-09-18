using System;
using Tidebound.Board;
using UnityEngine;

namespace Tidebound.Unity.Ship
{
    /// <summary>Maps logical tail cells to presentation space. Axes and cell size never feed back into Grid rules.</summary>
    public sealed class GridWorldMapper : MonoBehaviour, IGridWorldMapper
    {
        [SerializeField] private Transform origin;
        [SerializeField, Min(0.001f)] private float cellSize = 1f;
        [SerializeField] private Vector3 localRightAxis = Vector3.right;
        [SerializeField] private Vector3 localUpAxis = Vector3.forward;

        public float CellSize => cellSize;

        public Vector3 TailToWorld(GridPosition tail)
        {
            ValidateCellSize();
            var frame = origin != null ? origin : transform;
            return frame.position + Right(frame) * (tail.X * cellSize) + Up(frame) * (tail.Y * cellSize);
        }

        public Vector3 DirectionToWorld(global::Tidebound.Ship.ShipDirection direction)
        {
            ValidateCellSize();
            var frame = origin != null ? origin : transform;
            switch (direction)
            {
                case global::Tidebound.Ship.ShipDirection.Up: return Up(frame);
                case global::Tidebound.Ship.ShipDirection.Down: return -Up(frame);
                case global::Tidebound.Ship.ShipDirection.Left: return -Right(frame);
                case global::Tidebound.Ship.ShipDirection.Right: return Right(frame);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private Vector3 Right(Transform frame)
        {
            if (localRightAxis.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("Grid right axis cannot be zero.");
            return frame.TransformDirection(localRightAxis.normalized);
        }

        private Vector3 Up(Transform frame)
        {
            if (localUpAxis.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("Grid up axis cannot be zero.");
            return frame.TransformDirection(localUpAxis.normalized);
        }

        private void ValidateCellSize()
        {
            if (cellSize <= 0f) throw new InvalidOperationException("Grid cell size must be positive.");
        }
    }
}
