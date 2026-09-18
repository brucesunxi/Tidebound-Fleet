#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal class SwStage10NativeAndroidBridge : SwNativeAndroidBridge
    {
        #region --- Constants ---
        
        private const string INITIATE_APP_UPDATE_METHOD = "initiateUpdate";
        private const string CHECK_FOR_UPDATE_METHOD = "checkForUpdate";
        private const string ADD_APP_UPDATE_LISTENER_METHOD = "addAppUpdateListener";
        private const string REMOVE_APP_UPDATE_LISTENER_METHOD = "removeAppUpdateListener";
        
        #endregion
        
        
        #region --- Members ---
        
        private readonly SwNativeAppUpdateListener _appUpdateListener = new SwNativeAppUpdateListener();
        
        #endregion
        
        
        #region --- Public Methods ---

        public override IEnumerator InitSdk(SwNativeConfig configuration)
        {
            AddAppUpdateListener();
            
            return base.InitSdk(configuration);
        }

        public void StartAppUpdate(SwAppUpdateParam updateParam)
        {
            if (updateParam is SwAndroidAppUpdateParams androidUpdateParam)
            {
                _appUpdateManagerWrapper.Call(INITIATE_APP_UPDATE_METHOD, GetCurrentActivity(), androidUpdateParam.UpdateType);
            }
        }
        
        public void CheckForUpdate()
        {
            _appUpdateManagerWrapper.Call(CHECK_FOR_UPDATE_METHOD);
        }
        
        
        public void AddUpdateCheckListener(OnUpdateCheckResult onUpdateCheckResult)
        {
            _appUpdateListener.UpdateCheckResultEvent += onUpdateCheckResult;
        }
        
        public void AddUpdateStartedListener(OnUpdateStarted onUpdateStarted)
        {
            _appUpdateListener.UpdateStartedEvent += onUpdateStarted;
        }
        
        public void RemoveUpdateCheckListener(OnUpdateCheckResult onUpdateCheckResult)
        {
            _appUpdateListener.UpdateCheckResultEvent -= onUpdateCheckResult;
        }
        
        public void RemoveUpdateStartedListener(OnUpdateStarted onUpdateStarted)
        {
            _appUpdateListener.UpdateStartedEvent -= onUpdateStarted;
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private void RemoveAppUpdateListener()
        {
            _appUpdateManagerWrapper.Call(REMOVE_APP_UPDATE_LISTENER_METHOD, _appUpdateListener);
        }
        
        private void AddAppUpdateListener()
        {
            _appUpdateManagerWrapper.Call(ADD_APP_UPDATE_LISTENER_METHOD, _appUpdateListener);
        }
        
        #endregion
    }
}
#endif