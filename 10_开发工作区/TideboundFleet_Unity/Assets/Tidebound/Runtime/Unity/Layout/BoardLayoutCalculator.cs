using System;
using UnityEngine;

namespace Tidebound.Unity.Layout
{
    public readonly struct BoardLayoutMetrics
    {
        public float BossRatio { get; }
        public float BoardRatio { get; }
        public float ToolsRatio { get; }
        public float CellSize { get; }
        public float LaneThickness { get; }
        public Vector2 GridSize { get; }
        public Vector2 BoardAndLaneSize { get; }

        internal BoardLayoutMetrics(float bossRatio, float boardRatio, float toolsRatio,
            float cellSize, float laneThickness, int columns, int rows)
        {
            BossRatio = bossRatio;
            BoardRatio = boardRatio;
            ToolsRatio = toolsRatio;
            CellSize = cellSize;
            LaneThickness = laneThickness;
            GridSize = new Vector2(columns * cellSize, rows * cellSize);
            BoardAndLaneSize = GridSize + Vector2.one * laneThickness * 2f;
        }
    }

    /// <summary>Pure SafeArea sizing. It does not inspect sprites, renderers, colliders, or authored ships.</summary>
    public static class BoardLayoutCalculator
    {
        private const float LaneCellRatio = 0.45f;
        private const float HorizontalOuterPaddingRatio = 0.015f;

        public static BoardLayoutMetrics Calculate(float safeWidth, float safeHeight, int columns, int rows)
        {
            if (safeWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(safeWidth));
            if (safeHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(safeHeight));
            if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));

            var aspect = safeHeight / safeWidth;
            float bossRatio;
            float boardRatio;
            float toolsRatio;
            if (aspect >= 2f)
            {
                bossRatio = 0.17f; boardRatio = 0.68f; toolsRatio = 0.15f;
            }
            else if (aspect >= 1.85f)
            {
                bossRatio = 0.15f; boardRatio = 0.70f; toolsRatio = 0.15f;
            }
            else
            {
                bossRatio = 0.12f; boardRatio = 0.74f; toolsRatio = 0.14f;
            }

            var horizontalPadding = safeWidth * HorizontalOuterPaddingRatio * 2f;
            var byWidth = (safeWidth - horizontalPadding) / (columns + LaneCellRatio * 2f);
            var byHeight = safeHeight * boardRatio / (rows + LaneCellRatio * 2f);
            var cellSize = Mathf.Min(byWidth, byHeight);
            var lane = Mathf.Min(cellSize * LaneCellRatio, safeWidth * 0.04f);
            return new BoardLayoutMetrics(bossRatio, boardRatio, toolsRatio, cellSize, lane, columns, rows);
        }
    }
}
