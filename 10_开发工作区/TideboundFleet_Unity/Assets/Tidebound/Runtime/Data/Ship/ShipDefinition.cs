namespace Tidebound.Ship
{
    /// <summary>Immutable snapshot of the single base ship config. Length is authored per level instance.</summary>
    public sealed class ShipDefinition
    {
        public string TypeId { get; }
        public int DamageLv1 { get; }
        public ShipDefinition(string typeId, int damageLv1)
        { TypeId = typeId; DamageLv1 = damageLv1; }
    }
}
