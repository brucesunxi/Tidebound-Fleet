using System;
using System.Linq;
using Tidebound.Board;
using Tidebound.LevelDesign;

namespace Tidebound.Tools
{
    /// <summary>Read-only assistance. Optional exit peeling suppresses unsafe hints on stopper puzzles; never makes a move.</summary>
    public sealed class BoardAssistance
    {
        public const double IdleDelay = 5;
        public const double HighlightDuration = 1.4;
        private BoardModel observed;
        private string directExit;
        private double idle, visible;
        private bool hinted, announced;
        private readonly bool requireExitOnlySolution;
        public BoardAssistance(bool requireExitOnlySolution = false) { this.requireExitOnlySolution = requireExitOnlySolution; }
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
                HighlightedShipId = !requireExitOnlySolution || directExit != null && LevelDifficultyAnalysis.Peel(board).IsComplete ? directExit : null;
            }
        }
        public bool TryAnnounceDeadlock()
        {
            if (!NoMoves || announced) return false;
            announced = true; ResetIdle(); return true;
        }
    }
}
