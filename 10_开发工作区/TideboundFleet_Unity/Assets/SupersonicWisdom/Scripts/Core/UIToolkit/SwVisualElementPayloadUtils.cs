using UnityEngine;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK
{
    public static class SwVisualElementPayloadUtils
    {
        #region --- Constants ---

        private const int BASE_FONT_SIZE = 120;

        #endregion
        
        
        #region --- Public Methods ---
        
        public static string GetName(string name)
        {
            return name;
        }

        public static Font GetFont(string font)
        {
            // Assume font is stored as a Unity Resources
            return Resources.Load<Font>(font);
        }

        public static string GetText(string text)
        {
            return text;
        }

        public static int GetFontSize(string fontSizeData)
        {
            if (!fontSizeData.EndsWith("%"))
            {
                return int.Parse(fontSizeData);
            }
            
            var sizeValue = int.Parse(fontSizeData.TrimEnd('%'));
            return (BASE_FONT_SIZE * sizeValue) / 100;

        }

        public static Color GetFontColor(string color)
        {
            ColorUtility.TryParseHtmlString(color, out var fontColor);
            return fontColor;
        }

        public static StyleLength GetMargin(string marginData)
        {
            return GetStyleLength(marginData);
        }

        public static StyleLength GetPadding(string paddingData)
        {
            return GetStyleLength(paddingData);
        }

        public static Color GetBackgroundColor(string color)
        {
            ColorUtility.TryParseHtmlString(color, out var backgroundColor);
            return backgroundColor;
        }

        public static Sprite GetBackgroundImageSprite(string path)
        {
            // Assume image is stored as a Unity Resources
            return Resources.Load<Sprite>(path);
        }
        
        #endregion
        
        
        #region --- Private Methods ---

        private static StyleLength GetStyleLength(string value)
        {
            if (value.EndsWith("%"))
            {
                var percentValue = float.Parse(value.TrimEnd('%')) / 100;
                return new StyleLength(new Length(percentValue, LengthUnit.Percent));
            }

            var pixelValue = float.Parse(value);
            return new StyleLength(pixelValue);
        }
        
        #endregion
    }
}