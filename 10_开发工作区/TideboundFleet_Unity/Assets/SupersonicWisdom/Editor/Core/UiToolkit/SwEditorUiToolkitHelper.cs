using UnityEngine;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK.Editor
{
    public static class SwEditorUiToolkitHelper
    {
        #region --- Constants ---
        
        private const string MESSAGE_CLASS_NAME = "message";
        private const string BUTTON_CLASS_NAME = "button";
        private const string IMAGE_CLASS_NAME = "image";
        private const string CONTAINER_CLASS_NAME = "container";
        private const string CORE_PREFIX = "Core/";
        private const string SUPERSONIC_WISDOM_IMAGE_PATH = CORE_PREFIX + "Sprites/logo_white";
        private const string EDITOR_STYLES_PATH = CORE_PREFIX + "UiToolkit/StyleSheets/WisdomEditor";
        
        #endregion
        
        
        #region --- Enums ---
        
        public enum SwEditorUiToolkitStyle
        {
            Container,
            Image,
            Message,
            Button,
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public static void AddClassToVisualElement(VisualElement visualElement, SwEditorUiToolkitStyle classStyle)
        {
            var className = GetClassName(classStyle);
            
            if (className == string.Empty)
            {
                SwEditorLogger.LogError($"Class name not found for style: {classStyle}");
                return;
            }
            
            visualElement.AddToClassList(className);
        }
        
        public static void AddEditorStyleSheetToVisualElement(VisualElement visualElement)
        {
            var styleSheet = Resources.Load<StyleSheet>(EDITOR_STYLES_PATH);
            
            if (styleSheet == null)
            {
                SwEditorLogger.LogError($"Failed to load style sheet: {EDITOR_STYLES_PATH}");
                return;
            }
            
            visualElement.styleSheets.Add(styleSheet);
        }
        
        public static Texture2D GetSupersonicLogo()
        {
            var image = Resources.Load<Texture2D>(SUPERSONIC_WISDOM_IMAGE_PATH);
            
            if (image != null) return image;
            
            SwEditorLogger.LogError($"Failed to load image: {SUPERSONIC_WISDOM_IMAGE_PATH}");
            return null;
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private static string GetClassName(SwEditorUiToolkitStyle classStyle)
        {
            return classStyle switch
            {
                SwEditorUiToolkitStyle.Container => CONTAINER_CLASS_NAME,
                SwEditorUiToolkitStyle.Image => IMAGE_CLASS_NAME,
                SwEditorUiToolkitStyle.Message => MESSAGE_CLASS_NAME,
                SwEditorUiToolkitStyle.Button => BUTTON_CLASS_NAME,
                _ => string.Empty,
            };
        }
        
        #endregion
    }
}