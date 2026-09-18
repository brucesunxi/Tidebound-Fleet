using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>Authored instance data. Length and damage come exclusively from the ship catalog.</summary>
    public sealed class ShipPlacementData
    {
        public string Id { get; set; }
        public string TypeId { get; set; }
        public GridPosition Position { get; set; }
        public ShipDirection Direction { get; set; }
    }
}
