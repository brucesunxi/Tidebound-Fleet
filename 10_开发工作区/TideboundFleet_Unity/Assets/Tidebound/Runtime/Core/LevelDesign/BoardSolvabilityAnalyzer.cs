using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public enum BoardSearchStatus { Solved, Deadlocked, LimitReached }

    public sealed class BoardSearchResult
    {
        public BoardSearchStatus Status { get; }
        public int VisitedStateCount { get; }
        public IReadOnlyList<string> ShipIds { get; }

        internal BoardSearchResult(BoardSearchStatus status, int visited, IList<string> shipIds)
        {
            Status = status;
            VisitedStateCount = visited;
            ShipIds = Array.AsReadOnly((shipIds ?? Array.Empty<string>()).ToArray());
        }
    }

    public static class BoardSolvabilityAnalyzer
    {
        public static BoardSearchResult Search(BoardModel initialBoard, int maxVisitedStates)
        {
            if (initialBoard == null) throw new ArgumentNullException(nameof(initialBoard));
            if (maxVisitedStates <= 0) throw new ArgumentOutOfRangeException(nameof(maxVisitedStates));
            var result = LevelSolver.Solve(initialBoard,
                new LevelSolverOptions(maxVisitedStates: maxVisitedStates, useExitPeeling: false));
            var status = result.Status == LevelSolverStatus.Solved ? BoardSearchStatus.Solved :
                result.Status == LevelSolverStatus.Deadlocked ? BoardSearchStatus.Deadlocked : BoardSearchStatus.LimitReached;
            if (result.Status == LevelSolverStatus.Invalid)
                throw new ArgumentException(result.Reason, nameof(initialBoard));
            return new BoardSearchResult(status, result.VisitedStateCount, result.ShipIds.ToArray());
        }
    }
}
