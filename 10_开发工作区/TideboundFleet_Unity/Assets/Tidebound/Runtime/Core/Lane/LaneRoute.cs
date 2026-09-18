using System;
using Tidebound.Board;
using Tidebound.Ship;

namespace Tidebound.Lane
{
    public enum LaneRoute
    {
        Top = 0,
        Left = 1,
        Right = 2,
        BottomViaLeft = 3,
        BottomViaRight = 4
    }

    public static class LaneRouteResolver
    {
        public static LaneRoute Resolve(ShipDirection exitDirection, GridPosition exitTail, int boardWidth)
        {
            if (boardWidth <= 0) throw new ArgumentOutOfRangeException(nameof(boardWidth));

            switch (exitDirection)
            {
                case ShipDirection.Up:
                    return LaneRoute.Top;
                case ShipDirection.Left:
                    return LaneRoute.Left;
                case ShipDirection.Right:
                    return LaneRoute.Right;
                case ShipDirection.Down:
                    var leftDistance = Math.Abs(exitTail.X);
                    var rightDistance = Math.Abs((boardWidth - 1) - exitTail.X);
                    return leftDistance < rightDistance ? LaneRoute.BottomViaLeft : LaneRoute.BottomViaRight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(exitDirection));
            }
        }
    }
}
