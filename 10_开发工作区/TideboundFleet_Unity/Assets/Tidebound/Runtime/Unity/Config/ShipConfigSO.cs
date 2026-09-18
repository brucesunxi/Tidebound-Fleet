using Tidebound.Ship;
using UnityEngine;

namespace Tidebound.Config
{
    [CreateAssetMenu(menuName = "Tidebound/Ship Config")]
    public sealed class ShipConfigSO : ScriptableObject
    {
        [SerializeField] private string typeId;
        [SerializeField, Min(1)] private int damageLv1 = 10;
        [SerializeField] private ShipVisualConfigSO visual;
        public string TypeId => typeId;
        public int DamageLv1 => damageLv1;
        public ShipVisualConfigSO Visual => visual;
        public ShipDefinition CreateDefinition() => new ShipDefinition(typeId, damageLv1);
    }
}
