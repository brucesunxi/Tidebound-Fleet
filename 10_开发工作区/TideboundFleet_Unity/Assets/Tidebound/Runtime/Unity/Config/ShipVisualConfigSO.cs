using UnityEngine;

namespace Tidebound.Config
{
    /// <summary>Presentation metadata only; intentionally never read by the grid or session factory.</summary>
    [CreateAssetMenu(menuName = "Tidebound/Ship Visual Config")]
    public sealed class ShipVisualConfigSO : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 localScale = Vector3.one;
        public GameObject Prefab => prefab;
        public Vector3 LocalScale => localScale;
    }
}
