using Tidebound.Board;

namespace Tidebound.Ship
{
    /// <summary>Per-session state. Mutable position/state never write back into JSON or ScriptableObjects.</summary>
    public sealed class ShipRuntimeData
    {
        public string Id { get; }
        public string TypeId { get; }
        public GridPosition Position { get; set; }
        public ShipDirection Direction { get; set; }
        public int Length { get; }
        public int Damage { get; }
        public ShipState State { get; set; }

        public ShipRuntimeData(string id, string typeId, GridPosition position,
            ShipDirection direction, int length, int damage)
        {
            Id = id; TypeId = typeId; Position = position; Direction = direction;
            Length = length; Damage = damage; State = ShipState.Idle;
        }
    }
}
