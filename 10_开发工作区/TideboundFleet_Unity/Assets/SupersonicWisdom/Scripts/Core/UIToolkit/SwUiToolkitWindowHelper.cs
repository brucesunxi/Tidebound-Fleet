#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK
{
    internal static class SwUiToolkitWindowHelper
    {
        #region --- Constants ---
        
        #if UNITY_EDITOR
        private const string LANDSCAPE_PREFS_KEY = "SimulateLandscapeFontSizeChange";
        #endif
        private const string LINK_BUTTON_CLASSNAME = "link-button";
        public const string TRANSLATE_TRANSITION_PROPERTY_NAME = "translate";
        
        #endregion
        
        
        #region --- Properties ---

        internal static bool IsLandscape
        {
            #if UNITY_EDITOR
            get { return EditorPrefs.GetBool(LANDSCAPE_PREFS_KEY, false);}
            #else
            get { return (Screen.orientation == ScreenOrientation.LandscapeRight || Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.width > Screen.height); }
            #endif
        }
        
        #endregion
        
        
        #region --- Public Methods ---

        /// <summary>
        /// Animates the background of a VisualElement using a series of sprites.
        /// </summary>
        /// <param name="gifElement">The VisualElement to animate the background of.</param>
        /// <param name="sprites">An array of Sprites representing the frames of the animation.</param>
        /// <param name="i">A reference to the current index of the animation frame.</param>
        /// <param name="direction">A reference to the direction of animation (1 for forward, -1 for backward).</param>
        internal static void AnimateBackground(VisualElement gifElement, Sprite[] sprites, ref int i, ref int direction)
        {
            var bg = new Background
            {
                texture = sprites[i].texture,
                sprite = sprites[i],
            };

            gifElement.style.backgroundImage = bg;

            i += direction;

            if (i == sprites.Length - 1 || i == 0)
            {
                direction *= -1; // Reverse the direction when end or start of the array is reached
            }
        }

        /// <summary>
        /// Switches the class of a VisualElement by removing a class and adding a new class.
        /// </summary>
        /// <param name="element">The VisualElement for which to switch the class.</param>
        /// <param name="classToAdd">The class to add to the VisualElement.</param>
        /// <param name="classToRemove">The class to remove from the VisualElement.</param>
        internal static void SwitchClass(VisualElement element, string classToAdd, string classToRemove)
        {
            element.RemoveFromClassList(classToRemove);
            element.AddToClassList(classToAdd);
        }

        internal static void AddLinkButton(VisualElement container, string text, string link, string styleClassname)
        {
            var linkButton = new Button(() => Application.OpenURL(link));
            var buttonText = new Label($"<u>{text}</u>");

            linkButton.Add(buttonText);
            linkButton.AddToClassList(LINK_BUTTON_CLASSNAME);
            linkButton.AddToClassList(styleClassname);
            container.Add(linkButton);
        }
        
        #endregion
    }
}