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
            if (initialBoard.ShipCount == 0)
                return new BoardSearchResult(BoardSearchStatus.Solved, 1, Array.Empty<string>());

            var queue = new Queue<SearchNode>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            queue.Enqueue(new SearchNode(initialBoard, new List<string>()));
            visited.Add(BoardStateFingerprint.Compute(initialBoard));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var ship in current.Board.Ships.OrderBy(x => x.Id, StringComparer.Ordinal))
                {
                    var path = current.Board.QueryForwardPath(ship.Id);
                    if (path.IsBlocked && path.TravelDistance == 0) continue;
                    var nextBoard = current.Board.ApplyPathResult(path);
                    var nextSteps = new List<string>(current.Steps) { ship.Id };
                    if (nextBoard.ShipCount == 0)
                        return new BoardSearchResult(BoardSearchStatus.Solved, visited.Count, nextSteps);
                    var fingerprint = BoardStateFingerprint.Compute(nextBoard);
                    if (!visited.Add(fingerprint)) continue;
                    if (visited.Count >= maxVisitedStates)
                        return new BoardSearchResult(BoardSearchStatus.LimitReached, visited.Count, null);
                    queue.Enqueue(new SearchNode(nextBoard, nextSteps));
                }
            }
            return new BoardSearchResult(BoardSearchStatus.Deadlocked, visited.Count, null);
        }

        private sealed class SearchNode
        {
            public BoardModel Board { get; }
            public List<string> Steps { get; }
            public SearchNode(BoardModel board, List<string> steps) { Board = board; Steps = steps; }
        }
    }
}
