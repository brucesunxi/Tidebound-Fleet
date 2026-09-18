#if SW_STAGE_STAGE10_OR_ABOVE
using UnityEngine;
using UnityEngine.Scripting;

namespace SupersonicWisdomSDK
{
    internal class SwNativeAppUpdateListener : AndroidJavaProxy
    {
        #region --- Constants ---
        
        private const string APP_UPDATE_LISTENER_INTERFACE = "wisdom.library.api.listener.IWisdomAppUpdateListener";

        #endregion
        
        
        #region --- Members ---
        
        public OnUpdateStarted UpdateStartedEvent;
        public OnUpdateCheckResult UpdateCheckResultEvent;
        
        #endregion


        #region --- Construction ---
        
        public SwNativeAppUpdateListener() : base(APP_UPDATE_LISTENER_INTERFACE) { }
        
        #endregion
        
        
        #region --- Public Methods ---
            
        // Methods below will be called from Java
        // ReSharper disable once InconsistentNaming
        [Preserve]
        public void onUpdateCheckResult(string versionCode)
        {
            SwInfra.MainThreadRunner.RunOnMainThread(() => UpdateCheckResultEvent?.Invoke(versionCode));
        }
        
        // ReSharper disable once InconsistentNaming
        [Preserve]
        public void onUpdateStarted(bool didStart, string error)
        {
            SwInfra.MainThreadRunner.RunOnMainThread(() => UpdateStartedEvent?.Invoke(didStart, error));
        }
        
        #endregion
    }
}
#endif