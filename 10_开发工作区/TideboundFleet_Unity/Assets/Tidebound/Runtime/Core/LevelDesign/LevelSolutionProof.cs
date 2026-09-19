using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public static class LevelRules
    {
        public const string Version = "ForwardUntilBlockedV1";
        public const string ExitMode = "AllBoundaryEdges";
    }

    /// <summary>Collision-free search identity. Hashes are for persisted proof integrity, not state deduplication.</summary>
    public static class LevelStateIdentity
    {
        public static string CanonicalKey(BoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            var text = new StringBuilder();
            Add(text, LevelRules.Version);
            Add(text, LevelRules.ExitMode);
            Add(text, board.Width); Add(text, board.Height); Add(text, board.ShipCount);
            foreach (var ship in board.Ships.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                Add(text, ship.Id); Add(text, ship.TypeId);
                Add(text, ship.Position.X); Add(text, ship.Position.Y);
                Add(text, (int)ship.Direction); Add(text, ship.Length);
            }
            return text.ToString();
        }

        public static string Fingerprint(BoardModel board)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(CanonicalKey(board))))
                    .Replace("-", string.Empty);
        }

        private static void Add(StringBuilder text, int value) =>
            text.Append(value.ToString(CultureInfo.InvariantCulture)).Append(';');

        private static void Add(StringBuilder text, string value)
        {
            Add(text, value.Length);
            text.Append(value).Append(';');
        }
    }

    public sealed class LevelSolutionStep
    {
        public string ShipId { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public ForwardPathOutcome Outcome { get; }
        public string BeforeHash { get; }
        public string AfterHash { get; }

        public LevelSolutionStep(string shipId, GridPosition from, GridPosition to, ForwardPathOutcome outcome,
            string beforeHash, string afterHash)
        {
            ShipId = shipId; From = from; To = to; Outcome = outcome;
            BeforeHash = beforeHash; AfterHash = afterHash;
        }
    }

    /// <summary>Versioned sidecar; the schema-v2 layout remains the single source of ship placement.</summary>
    public sealed class LevelSolutionProof
    {
        public const int CurrentVersion = 1;
        public int ProofVersion { get; }
        public string RulesVersion { get; }
        public string ExitMode { get; }
        public string LevelId { get; }
        public string LayoutFingerprint { get; }
        public IReadOnlyList<LevelSolutionStep> Steps { get; }

        public LevelSolutionProof(int proofVersion, string rulesVersion, string exitMode, string levelId,
            string layoutFingerprint, IEnumerable<LevelSolutionStep> steps)
        {
            ProofVersion = proofVersion; RulesVersion = rulesVersion; ExitMode = exitMode;
            LevelId = levelId; LayoutFingerprint = layoutFingerprint;
            Steps = Array.AsReadOnly((steps ?? throw new ArgumentNullException(nameof(steps))).ToArray());
        }

        public static LevelSolutionProof Create(string levelId, BoardModel initialBoard, IEnumerable<string> shipIds)
        {
            if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A level id is required.", nameof(levelId));
            if (initialBoard == null) throw new ArgumentNullException(nameof(initialBoard));
            if (shipIds == null) throw new ArgumentNullException(nameof(shipIds));
            var board = initialBoard;
            var steps = new List<LevelSolutionStep>();
            foreach (var id in shipIds)
            {
                var path = board.QueryForwardPath(id);
                if (path.IsBlocked && path.TravelDistance == 0)
                    throw new ArgumentException("A proof cannot contain a zero-distance action.", nameof(shipIds));
                var next = board.ApplyPathResult(path);
                steps.Add(new LevelSolutionStep(id, path.OriginTail, path.TargetTail, path.Outcome,
                    LevelStateIdentity.Fingerprint(board), LevelStateIdentity.Fingerprint(next)));
                board = next;
            }
            if (board.ShipCount != 0) throw new ArgumentException("A proof must clear the board.", nameof(shipIds));
            return new LevelSolutionProof(CurrentVersion, LevelRules.Version, LevelRules.ExitMode, levelId,
                LevelStateIdentity.Fingerprint(initialBoard), steps);
        }

        public SolutionReplayResult Replay(string expectedLevelId, BoardModel initialBoard)
        {
            if (initialBoard == null) throw new ArgumentNullException(nameof(initialBoard));
            if (ProofVersion != CurrentVersion || RulesVersion != LevelRules.Version || ExitMode != LevelRules.ExitMode)
                return new SolutionReplayResult(false, 0, "Unsupported proof, rules or exit version.", initialBoard);
            if (string.IsNullOrWhiteSpace(expectedLevelId) || LevelId != expectedLevelId ||
                LayoutFingerprint != LevelStateIdentity.Fingerprint(initialBoard))
                return new SolutionReplayResult(false, 0, "Proof identity does not match the level.", initialBoard);
            var board = initialBoard;
            for (var i = 0; i < Steps.Count; i++)
            {
                var step = Steps[i];
                if (step == null || !board.TryGetShip(step.ShipId, out _) ||
                    step.BeforeHash != LevelStateIdentity.Fingerprint(board))
                    return new SolutionReplayResult(false, i, "Missing ship or mismatched pre-state.", board);
                var path = board.QueryForwardPath(step.ShipId);
                if ((path.IsBlocked && path.TravelDistance == 0) || !path.OriginTail.Equals(step.From) ||
                    !path.TargetTail.Equals(step.To) || path.Outcome != step.Outcome)
                    return new SolutionReplayResult(false, i, "Step does not match the current forward path.", board);
                var next = board.ApplyPathResult(path);
                if (step.AfterHash != LevelStateIdentity.Fingerprint(next))
                    return new SolutionReplayResult(false, i, "Mismatched post-state.", board);
                board = next;
            }
            return new SolutionReplayResult(board.ShipCount == 0, Steps.Count,
                board.ShipCount == 0 ? null : "Proof does not clear the board.", board);
        }
    }
}
