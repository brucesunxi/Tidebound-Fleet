using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.UI
{
    /// <summary>Owns the material used to quiet the lobby backdrop without modifying source art.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class HarborBackgroundTreatment : MonoBehaviour
    {
        public float Saturation = HarborDesignTokens.BackgroundSaturation;
        public float BlurPixels = HarborDesignTokens.BackgroundBlurPixels;
        private Material material;

        private void OnEnable()
        {
            var shader = Resources.Load<Shader>("TideboundUI/HarborBackgroundTreatment");
            if (shader == null) return;
            material = new Material(shader) { name = "UI_MainMenu_BackgroundTreatment" };
            material.SetFloat("_Saturation", Saturation);
            material.SetFloat("_BlurPixels", BlurPixels);
            GetComponent<RawImage>().material = material;
        }

        private void OnDisable()
        {
            var image = GetComponent<RawImage>();
            if (image != null && image.material == material) image.material = null;
            if (material != null) Destroy(material);
            material = null;
        }
    }

}
