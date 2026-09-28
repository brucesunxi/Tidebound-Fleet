using UnityEngine;

namespace Tidebound.Unity.UI
{
    /// <summary>
    /// Single runtime source for the Tidebound UI design system. New UI may use the five
    /// brand hues and deterministic tints/shades of those hues, but must not introduce
    /// unrelated colours in page code.
    /// </summary>
    public static class HarborDesignTokens
    {
        public static readonly Color OceanBlue = Hex("169BD5");
        public static readonly Color PearlWhite = Hex("FFFFFF");
        public static readonly Color Gold = Hex("FFC928");
        public static readonly Color EmeraldGreen = Hex("39C98A");
        public static readonly Color CoralRed = Hex("FF6655");

        public static readonly Color Ink = Shade(OceanBlue, .32f);
        public static readonly Color OceanShadow = Shade(OceanBlue, .50f);
        public static readonly Color OceanSoft = Tint(OceanBlue, .82f);
        public static readonly Color PearlWarm = Color.Lerp(PearlWhite, Gold, .08f);
        public static readonly Color GoldDark = Shade(Gold, .48f);
        public static readonly Color Disabled = Color.Lerp(PearlWhite, OceanBlue, .30f);

        public const float RadiusSmall = 12f;
        public const float RadiusMedium = 22f;
        public const float RadiusLarge = 36f;
        public const float ShadowDepth = 6f;
        public const float MainShadowDepth = 10f;
        public const float BackgroundSaturation = .82f;
        public const float BackgroundBlurPixels = 2.10f;
        public const float TouchTarget = 48f;

        public static Color Tint(Color color, float amount) => Color.Lerp(color, PearlWhite, Mathf.Clamp01(amount));
        public static Color Shade(Color color, float amount) => Color.Lerp(Color.black, color, Mathf.Clamp01(amount));

        private static Color Hex(string value)
        {
            if (!ColorUtility.TryParseHtmlString("#" + value, out var color))
                throw new System.InvalidOperationException("Invalid design token #" + value);
            return color;
        }
    }
}
