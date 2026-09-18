using Tidebound.Board;

namespace Tidebound.Config
{
    /// <summary>Detached JSON document. Never used directly as mutable session state.</summary>
    public sealed class LevelData
    {
        public int SchemaVersion { get; set; }
        public string LevelId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ShipPlacementData[] Ships { get; set; }
        public string BossId { get; set; }
    }
}
