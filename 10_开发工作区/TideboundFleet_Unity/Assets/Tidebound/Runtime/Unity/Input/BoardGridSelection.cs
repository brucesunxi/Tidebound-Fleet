using System;
using Tidebound.Board;

namespace Tidebound.Unity.Input
{
    /// <summary>Locks selection by logical occupied cells. Empty cells never snap to nearby ships.</summary>
    public sealed class BoardGridSelection
    {
        private readonly Func<BoardModel> boardProvider;
        private string pressedShipId;

        public string PressedShipId => pressedShipId;

        public BoardGridSelection(Func<BoardModel> boardProvider)
        {
            this.boardProvider = boardProvider ?? throw new ArgumentNullException(nameof(boardProvider));
        }

        public string PointerDown(GridPosition cell)
        {
            var board = boardProvider();
            pressedShipId = board != null && board.IsInside(cell) ? board.GetShipId(cell) : null;
            return pressedShipId;
        }

        public string PointerUp(GridPosition cell)
        {
            var locked = pressedShipId;
            pressedShipId = null;
            if (locked == null) return null;
            var board = boardProvider();
            if (board == null || !board.IsInside(cell)) return null;
            return string.Equals(board.GetShipId(cell), locked, StringComparison.Ordinal) ? locked : null;
        }

        public void Cancel() => pressedShipId = null;
    }
}
