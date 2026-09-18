using UnityEngine;

namespace Tidebound.Config
{
    /// <summary>Unity reference wrapper. All layout and level identity live in the referenced JSON.</summary>
    [CreateAssetMenu(menuName = "Tidebound/Level Config")]
    public sealed class LevelConfigSO : ScriptableObject
    {
        [SerializeField] private TextAsset levelJson;
        [SerializeField] private ShipConfigSO[] shipConfigs;
        [SerializeField] private BossConfigSO[] bossConfigs;
        public TextAsset LevelJson => levelJson;
        public ShipConfigSO[] GetShipConfigs() => shipConfigs == null ? null : (ShipConfigSO[])shipConfigs.Clone();
        public BossConfigSO[] GetBossConfigs() => bossConfigs == null ? null : (BossConfigSO[])bossConfigs.Clone();
    }
}
