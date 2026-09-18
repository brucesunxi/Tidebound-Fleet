using UnityEngine;
using UnityEngine.EventSystems;

namespace SupersonicWisdomSDK
{
    public class SwUiUtils
    {
        #region --- Members ---
         
        private bool _prevAutoRotateToPortrait;
        private bool _prevAutorotateToPortraitUpsideDown;
        
        #endregion
        
        #region --- Public Methods ---
        
        public void LockUI()
        {
            LockUnityUI();
            LockScreenRotation();
        }
        
        public void UnlockUI()
        {
            UnlockUnityUI();
            RevertLockScreenRotation();
        }
        
        public bool IsLandscapeLayout()
        {
#if UNITY_EDITOR
            return Screen.height <= Screen.width;
#elif (UNITY_IOS || UNITY_ANDROID) && UNITY_2019_4_OR_NEWER
            return Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight;
#elif UNITY_IOS || UNITY_ANDROID
            return Screen.orientation == ScreenOrientation.Landscape;
#else
            return true;
#endif
        }
        
        #endregion
        
        #region --- Private Methods ---
        
        private void LockUnityUI()
        {
            ToggleAllEventSystems(enable: false);
        }
        
        private void LockScreenRotation()
        {
            _prevAutoRotateToPortrait = Screen.autorotateToPortrait;
            _prevAutorotateToPortraitUpsideDown = Screen.autorotateToPortraitUpsideDown;

            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }
        
        private void UnlockUnityUI()
        {
            ToggleAllEventSystems(enable: true);
        }
        
        private void RevertLockScreenRotation()
        {
            Screen.autorotateToPortrait = _prevAutoRotateToPortrait;
            Screen.autorotateToPortraitUpsideDown = _prevAutorotateToPortraitUpsideDown;
        }

        private static void ToggleAllEventSystems(bool enable)
        {
            var eventSystems = Object.FindObjectsOfType<EventSystem>();
            
            foreach (var eventSystem in eventSystems)
            {
                eventSystem.enabled = enable;
            }
        }
        
        
        #endregion
    }
}