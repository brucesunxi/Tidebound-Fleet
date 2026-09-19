using Tidebound.Board;
using Tidebound.Ship;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Development-only visual identity. Logical occupancy remains in BoardModel.</summary>
    public sealed class PrototypeShipView : MonoBehaviour
    {
        public string ShipId { get; private set; }
        public ShipDirection Direction { get; private set; }
        public int Length { get; private set; }

        internal void Configure(BoardShipSnapshot ship)
        {
            ShipId = ship.Id;
            Direction = ship.Direction;
            Length = ship.Length;
            name = "PrototypeShip_" + ship.Id;
        }
    }
}
