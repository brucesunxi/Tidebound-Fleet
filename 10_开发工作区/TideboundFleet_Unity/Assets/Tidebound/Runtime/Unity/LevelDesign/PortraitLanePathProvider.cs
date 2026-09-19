using System;
using Tidebound.Lane;
using Tidebound.Unity.Lane;
using Tidebound.Unity.Layout;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Visual-center routes use the same lane width as the portrait layout.</summary>
    public sealed class PortraitLanePathProvider : ILanePathProvider
    {
        private readonly float width, height;
        private const float Inset = PortraitBoardLayout.LaneWidthInCells / 2;
        public PortraitLanePathProvider(int width, int height)
        {
            this.width = width; this.height = height;
        }
        public Vector3 FleetIngressPosition => new Vector3(width / 2, height + PortraitBoardLayout.LaneWidthInCells, 0);
        public LaneWorldPath CreatePath(LaneRoute route, Vector3 tail)
        {
            var tl = new Vector3(-Inset, height + Inset, 0);
            var tr = new Vector3(width + Inset, height + Inset, 0);
            var tc = new Vector3(width / 2, height + Inset, 0);
            var bl = new Vector3(-Inset, -Inset, 0);
            var br = new Vector3(width + Inset, -Inset, 0);
            switch (route)
            {
                case LaneRoute.Top: return new LaneWorldPath(true, new Vector3(tail.x, tc.y, 0), tc);
                case LaneRoute.Left: return new LaneWorldPath(true, new Vector3(tl.x, tail.y, 0), tl, tc);
                case LaneRoute.Right: return new LaneWorldPath(true, new Vector3(tr.x, tail.y, 0), tr, tc);
                case LaneRoute.BottomViaLeft: return new LaneWorldPath(true, new Vector3(tail.x, bl.y, 0), bl, tl, tc);
                case LaneRoute.BottomViaRight: return new LaneWorldPath(true, new Vector3(tail.x, br.y, 0), br, tr, tc);
                default: throw new ArgumentOutOfRangeException(nameof(route));
            }
        }
    }
}
