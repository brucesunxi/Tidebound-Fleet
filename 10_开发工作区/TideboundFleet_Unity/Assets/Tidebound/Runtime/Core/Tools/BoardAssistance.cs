using System;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.Tools
{
    /// <summary>Read-only, board-snapshot-scoped assistance. Never runs a solver or makes a move.</summary>
    public sealed class BoardAssistance
    {
        public const double IdleDelay = 5;
        public const double HighlightDuration = 1.4;
        private BoardModel observed;
        private string directExit;
        private double idle, visible;
        private bool hinted, announced;
        public string HighlightedShipId { get; private set; }
        public bool NoMoves { get; private set; }

        public void Observe(BoardModel board)
        {
            if (ReferenceEquals(observed, board)) return;
            observed = board; directExit = null; announced = false; ResetIdle();
            NoMoves = board != null && board.ShipCount > 0;
            if (board == null) return;
            foreach (var ship in board.Ships.OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                var path = board.QueryForwardPath(ship.Id);
                if (path.TravelDistance > 0) NoMoves = false;
                if (directExit == null && path.CanExit) directExit = ship.Id;
            }
        }
        public void ResetIdle()
        { idle = visible = 0; hinted = false; HighlightedShipId = null; }

        public void Advance(BoardModel board, double seconds, bool eligible, bool userInput, bool hintsEnabled)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            Observe(board);
            if (!eligible || userInput || !hintsEnabled) { ResetIdle(); return; }
            if (HighlightedShipId != null)
            {
                visible += seconds;
                if (visible >= HighlightDuration) HighlightedShipId = null;
            }
            idle += seconds;
            if (!hinted && idle + 1e-9 >= IdleDelay)
            {
                hinted = true; visible = 0;
                HighlightedShipId = directExit;
            }
        }
        public bool TryAnnounceDeadlock()
        {
            if (!NoMoves || announced) return false;
            announced = true; ResetIdle(); return true;
        }
    }
}
