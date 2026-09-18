using Tidebound.Board;

namespace Tidebound.Ship
{
    /// <summary>Per-session state. Mutable position/state never write back into JSON or ScriptableObjects.</summary>
    public sealed class ShipRuntimeData
    {
        public string Id { get; }
        public string TypeId { get; }
        public string SkinId { get; }
        public GridPosition Position { get; internal set; }
        public ShipDirection Direction { get; internal set; }
        public int Length { get; }
        public int Damage { get; }
        public ShipState State { get; internal set; }

        public ShipRuntimeData(string id, string typeId, string skinId, GridPosition position,
            ShipDirection direction, int length, int damage)
        {
            Id = id; TypeId = typeId; SkinId = skinId; Position = position; Direction = direction;
            Length = length; Damage = damage; State = ShipState.Idle;
        }
    }
}
