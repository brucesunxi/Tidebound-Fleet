using Tidebound.Boss;
using UnityEngine;

namespace Tidebound.Config
{
    [CreateAssetMenu(menuName = "Tidebound/Boss Config")]
    public sealed class BossConfigSO : ScriptableObject
    {
        [SerializeField] private string bossId;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject prefab;
        public string BossId => bossId;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public BossDefinition CreateDefinition() => new BossDefinition(bossId);
    }
}
