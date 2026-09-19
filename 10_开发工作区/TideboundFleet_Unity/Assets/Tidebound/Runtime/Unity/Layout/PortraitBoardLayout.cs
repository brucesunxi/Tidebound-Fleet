using System;
using UnityEngine;

namespace Tidebound.Unity.Layout
{
    /// <summary>5R candidate layout in logical UI units; no device millimetre/dp claim.</summary>
    public sealed class PortraitBoardLayout
    {
        public const float OuterPadding = 16f;
        public const float LaneWidthInCells = 1.5f;
        public float LaneWidth => LaneWidthInCells * CellSize;
        public Rect Safe { get; }
        public Rect Top { get; }
        public Rect Tools { get; }
        public Rect Middle { get; }
        public Rect Grid { get; }
        public Rect Lane { get; }
        public float CellSize { get; }
        private PortraitBoardLayout(Rect safe, int columns, int rows)
        {
            Safe = safe;
            var top = Mathf.Clamp(safe.height * .22f, 128, 196);
            var tools = Mathf.Clamp(safe.height * .14f, 88, 120);
            Top = new Rect(safe.x, safe.yMax - top, safe.width, top);
            Tools = new Rect(safe.x, safe.y, safe.width, tools);
            Middle = new Rect(safe.x, Tools.yMax + 8, safe.width, Top.yMin - Tools.yMax - 16);
            CellSize = Mathf.Min((safe.width - 2 * OuterPadding) / (columns + 2 * LaneWidthInCells),
                (Middle.height - 2 * OuterPadding) / (rows + 2 * LaneWidthInCells));
            if (CellSize <= 0) throw new ArgumentException("Safe area is too small for the portrait layout.");
            Grid = new Rect(Middle.center - new Vector2(columns, rows) * CellSize / 2,
                new Vector2(columns, rows) * CellSize);
            Lane = new Rect(Grid.x - LaneWidth, Grid.y - LaneWidth, Grid.width + 2 * LaneWidth, Grid.height + 2 * LaneWidth);
        }
        public static PortraitBoardLayout Calculate(Rect safe, int columns, int rows)
        {
            if (columns <= 0 || rows <= 0 || !Finite(safe.x) || !Finite(safe.y) ||
                !Finite(safe.width) || !Finite(safe.height) || safe.width <= 40 || safe.height <= 0)
                throw new ArgumentException("Invalid portrait layout input.");
            return new PortraitBoardLayout(safe, columns, rows);
        }
        public Vector2 CellCenter(int x, int y) => Grid.min + new Vector2(x + .5f, y + .5f) * CellSize;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
