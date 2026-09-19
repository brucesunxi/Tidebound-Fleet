using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.Fleet
{
    public sealed class FleetGroup
    {
        public string SkinId { get; }
        public int SlotIndex { get; }
        public bool IsSupport => SlotIndex < 0;
        public int ArrivedCount { get; internal set; }
        public int HitCount { get; internal set; }
        public int PendingCount => ArrivedCount - HitCount;
        internal double NextLaunchAt;
        internal FleetGroup(string skinId, int slotIndex) { SkinId=skinId; SlotIndex=slotIndex; }
    }

    /// <summary>At most five standard-skin groups; long ships never consume a skin seat.</summary>
    public sealed class FleetRoster
    {
        private readonly Dictionary<string,FleetGroup> bySkin = new Dictionary<string,FleetGroup>(StringComparer.Ordinal);
        public IReadOnlyList<FleetGroup> StandardGroups { get; }
        public FleetGroup Support { get; } = new FleetGroup(FoundationLimits.DefaultLongSkinId,-1);
        public FleetRoster(IReadOnlyList<ShipRuntimeData> ships, IReadOnlyList<string> equippedOrder = null)
        {
            if(ships==null) throw new ArgumentNullException(nameof(ships));
            var used=ships.Where(s=>s.Length==2).Select(s=>s.SkinId).Distinct(StringComparer.Ordinal).ToArray();
            var order=equippedOrder?.ToArray() ?? used.OrderBy(s=>s==FoundationLimits.DefaultStandardSkinId ? 0 : 1)
                .ThenBy(s=>s,StringComparer.Ordinal).ToArray();
            if(order.Length>5 || order.Any(string.IsNullOrWhiteSpace) || order.Distinct(StringComparer.Ordinal).Count()!=order.Length ||
                used.Any(s=>!order.Contains(s,StringComparer.Ordinal)))
                throw new ArgumentException("Fleet requires at most five distinct standard skins in equipment order.");
            var groups=new List<FleetGroup>();
            foreach(var skin in order) { var g=new FleetGroup(skin,groups.Count);groups.Add(g);bySkin.Add(skin,g); }
            StandardGroups=groups.AsReadOnly();
        }
        internal FleetGroup GroupFor(ShipRuntimeData ship) => ship.Length==3 ? Support : bySkin[ship.SkinId];
    }
}
