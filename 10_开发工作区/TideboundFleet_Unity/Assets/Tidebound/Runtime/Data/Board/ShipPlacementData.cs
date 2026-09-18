using Tidebound.Ship;

namespace Tidebound.Board
{
    /// <summary>Authored instance data. Length belongs to the level; damage comes from the one base ship config.</summary>
    public sealed class ShipPlacementData
    {
        public string Id { get; set; }
        public string TypeId { get; set; }
        public int Length { get; set; }
        public GridPosition Position { get; set; }
        public ShipDirection Direction { get; set; }
    }
}
