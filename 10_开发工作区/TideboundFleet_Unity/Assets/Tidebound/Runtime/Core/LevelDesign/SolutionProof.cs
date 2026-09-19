using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public sealed class SolutionProof
    {
        public string LevelId { get; }
        public string LayoutFingerprint { get; }
        public IReadOnlyList<string> ShipIds { get; }

        public SolutionProof(string levelId, string layoutFingerprint, IEnumerable<string> shipIds)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level id is required.", nameof(levelId));
            if (string.IsNullOrWhiteSpace(layoutFingerprint)) throw new ArgumentException("A layout fingerprint is required.", nameof(layoutFingerprint));
            if (shipIds == null) throw new ArgumentNullException(nameof(shipIds));
            LevelId = levelId;
            LayoutFingerprint = layoutFingerprint;
            ShipIds = Array.AsReadOnly(shipIds.ToArray());
        }
    }

    public sealed class SolutionReplayResult
    {
        public bool IsValid { get; }
        public bool IsComplete { get; }
        public int AppliedStepCount { get; }
        public string Error { get; }
        public BoardModel FinalBoard { get; }

        internal SolutionReplayResult(bool isValid, int appliedSteps, string error, BoardModel finalBoard)
        {
            IsValid = isValid;
            FinalBoard = finalBoard;
            IsComplete = isValid && finalBoard.ShipCount == 0;
            AppliedStepCount = appliedSteps;
            Error = error;
        }
    }

    public static class BoardStateFingerprint
    {
        public static string Compute(BoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var hash = 14695981039346656037UL;
            Add(ref hash, board.Width);
            Add(ref hash, board.Height);
            foreach (var ship in board.Ships.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                Add(ref hash, ship.Id);
                Add(ref hash, ship.TypeId);
                Add(ref hash, ship.Position.X);
                Add(ref hash, ship.Position.Y);
                Add(ref hash, (int)ship.Direction);
                Add(ref hash, ship.Length);
            }
            return hash.ToString("X16");
        }

        private static void Add(ref ulong hash, string value)
        {
            if (value == null) { Add(ref hash, -1); return; }
            Add(ref hash, value.Length);
            for (var i = 0; i < value.Length; i++) Add(ref hash, value[i]);
        }

        private static void Add(ref ulong hash, int value)
        {
            unchecked
            {
                for (var shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (byte)(value >> shift);
                    hash *= 1099511628211UL;
                }
            }
        }
    }

    public sealed class SolutionProofRecorder
    {
        private readonly BoardModel initialBoard;
        private readonly List<string> shipIds = new List<string>();

        public BoardModel CurrentBoard { get; private set; }
        public IReadOnlyList<string> ShipIds => shipIds.AsReadOnly();
        public bool IsComplete => CurrentBoard.ShipCount == 0;

        public SolutionProofRecorder(BoardModel initialBoard)
        {
            this.initialBoard = initialBoard ?? throw new ArgumentNullException(nameof(initialBoard));
            CurrentBoard = initialBoard;
        }

        public bool TryApply(string shipId, out ForwardPathResult path)
        {
            path = CurrentBoard.QueryForwardPath(shipId);
            if (path.IsBlocked && path.TravelDistance == 0) return false;
            CurrentBoard = CurrentBoard.ApplyPathResult(path);
            shipIds.Add(shipId);
            return true;
        }

        public void Reset()
        {
            CurrentBoard = initialBoard;
            shipIds.Clear();
        }

        public SolutionProof CreateProof(string levelId) =>
            new SolutionProof(levelId, BoardStateFingerprint.Compute(initialBoard), shipIds);
    }

    public static class SolutionProofReplay
    {
        public static SolutionReplayResult Replay(BoardModel initialBoard, SolutionProof proof)
        {
            if (initialBoard == null) throw new ArgumentNullException(nameof(initialBoard));
            if (proof == null) throw new ArgumentNullException(nameof(proof));
            if (!string.Equals(BoardStateFingerprint.Compute(initialBoard), proof.LayoutFingerprint,
                    StringComparison.Ordinal))
                return new SolutionReplayResult(false, 0, "The proof fingerprint does not match the layout.", initialBoard);

            var board = initialBoard;
            for (var i = 0; i < proof.ShipIds.Count; i++)
            {
                var shipId = proof.ShipIds[i];
                if (!board.TryGetShip(shipId, out _))
                    return new SolutionReplayResult(false, i, $"Step {i + 1} references missing ship '{shipId}'.", board);
                var path = board.QueryForwardPath(shipId);
                if (path.IsBlocked && path.TravelDistance == 0)
                    return new SolutionReplayResult(false, i, $"Step {i + 1} cannot change the board.", board);
                board = board.ApplyPathResult(path);
            }
            return new SolutionReplayResult(true, proof.ShipIds.Count, null, board);
        }
    }
}
