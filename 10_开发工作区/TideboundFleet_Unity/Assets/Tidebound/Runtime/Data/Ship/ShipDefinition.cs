namespace Tidebound.Ship
{
    /// <summary>Immutable logical snapshot of one ShipConfigSO. Contains no presentation dimensions.</summary>
    public sealed class ShipDefinition
    {
        public string TypeId { get; }
        public int Length { get; }
        public int DamageLv1 { get; }
        public ShipDefinition(string typeId, int length, int damageLv1)
        { TypeId = typeId; Length = length; DamageLv1 = damageLv1; }
    }
}
