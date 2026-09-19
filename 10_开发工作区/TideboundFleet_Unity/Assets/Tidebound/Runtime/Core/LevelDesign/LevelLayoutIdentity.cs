using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Tidebound.Board;
using Tidebound.Ship;

namespace Tidebound.LevelDesign
{
    /// <summary>Exact content key for the current all-boundary-exits rule. IDs and input ordering are ignored.</summary>
    public static class LevelLayoutIdentity
    {
        public const string Version = "RectangularSymmetryV1";

        public static string CanonicalKey(BoardModel board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            string best = null;
            for (var mask = 0; mask < 4; mask++)
            {
                var flipX = (mask & 1) != 0; var flipY = (mask & 2) != 0;
                var entries = board.Ships.Select(ship =>
                {
                    var x = flipX ? board.Width - 1 - ship.Position.X : ship.Position.X;
                    var y = flipY ? board.Height - 1 - ship.Position.Y : ship.Position.Y;
                    var d = ship.Direction;
                    if (flipX && d == ShipDirection.Left) d = ShipDirection.Right;
                    else if (flipX && d == ShipDirection.Right) d = ShipDirection.Left;
                    if (flipY && d == ShipDirection.Up) d = ShipDirection.Down;
                    else if (flipY && d == ShipDirection.Down) d = ShipDirection.Up;
                    return new { x, y, direction = (int)d, ship.Length, ship.TypeId };
                }).OrderBy(s => s.x).ThenBy(s => s.y).ThenBy(s => s.direction).ThenBy(s => s.Length)
                    .ThenBy(s => s.TypeId, StringComparer.Ordinal);
                var text = new StringBuilder();
                text.Append(Version).Append('|').Append(LevelRules.Version).Append('|').Append(LevelRules.ExitMode).Append('|');
                text.Append(board.Width.ToString(CultureInfo.InvariantCulture)).Append(',')
                    .Append(board.Height.ToString(CultureInfo.InvariantCulture)).Append('|');
                foreach (var ship in entries)
                    text.Append(FormattableString.Invariant($"{ship.x},{ship.y},{ship.direction},{ship.Length},{ship.TypeId.Length}:"))
                        .Append(ship.TypeId).Append(';');
                var key = text.ToString();
                if (best == null || string.CompareOrdinal(key, best) < 0) best = key;
            }
            return best;
        }
    }
}
