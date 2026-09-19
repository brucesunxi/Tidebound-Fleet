using System;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.LevelDesign
{
    public static class LevelProductionValidator
    {
        public static ValidationResult Validate(BoardModel board, LevelProductionProfile profile)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var result = new ValidationResult();
            var report = LevelStructureAnalyzer.Analyze(board);

            if (board.Width != profile.Width || board.Height != profile.Height)
                result.Add("PROFILE_BOARD_SIZE", "width/height",
                    $"Profile {profile.Id} requires {profile.Width}x{profile.Height}.");
            if (board.ShipCount < profile.MinShipCount || board.ShipCount > profile.MaxShipCount)
                result.Add("PROFILE_SHIP_COUNT", "ships",
                    $"Profile {profile.Id} requires {profile.MinShipCount}..{profile.MaxShipCount} ships.");

            var longRatio = board.ShipCount == 0 ? 0d : (double)report.LongShipCount / board.ShipCount;
            if (longRatio > profile.MaxLongShipRatio)
                result.Add("PROFILE_LONG_SHIP_RATIO", "ships", "Too many length-3 ships for this production profile.");
            if (report.DirectionEntropy < profile.MinDirectionEntropy)
                result.Add("DIRECTION_ENTROPY_LOW", "ships", "The four directions are not sufficiently mixed.");
            if (report.MaximumDirectionShare > profile.MaxDirectionShare)
                result.Add("DIRECTION_SHARE_HIGH", "ships", "One direction dominates the board.");
            if (report.AdjacentShipPairCount > 0 && report.DirectionClustering > profile.MaxDirectionClustering)
                result.Add("DIRECTION_CLUSTERED", "ships", "Same-direction ships form spatial bands or regions.");
            if (report.Dependencies.InitialExitCount < profile.MinInitialExitCount)
                result.Add("INITIAL_EXITS_LOW", "ships", "The opening has too few direct exit choices.");
            if (report.Dependencies.Cycles.Any(x => x.IsHardLocked))
                result.Add("HARD_LOCKED_CYCLE", "ships", "A closed zero-travel dependency cycle cannot be unlocked.");
            return result;
        }
    }
}
