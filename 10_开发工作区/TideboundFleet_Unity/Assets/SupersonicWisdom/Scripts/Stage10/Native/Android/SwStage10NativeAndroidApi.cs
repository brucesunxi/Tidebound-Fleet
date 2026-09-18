#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal class SwStage10NativeAndroidApi: SwNativeAndroidApi, ISwStage10NativeApi
    {
        #region --- Member ---
        
        private readonly SwStage10NativeAndroidBridge _stage10NativeAndroidBridge;
        
        #endregion
        
        
        #region --- Construction ---
        
        public SwStage10NativeAndroidApi(SwStage10NativeAndroidBridge nativeBridge) : base(nativeBridge)
        {
            _stage10NativeAndroidBridge = nativeBridge;
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public void StartAppUpdate(SwAppUpdateParam updateParam)
        {
            _stage10NativeAndroidBridge.StartAppUpdate(updateParam);
        }
        
        public void CheckForUpdate()
        {
            _stage10NativeAndroidBridge.CheckForUpdate();
        }
        
        public void AddUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            _stage10NativeAndroidBridge.AddUpdateCheckListener(onUpdateCheckResult);
        }
        
        public void RemoveUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            _stage10NativeAndroidBridge.RemoveUpdateCheckListener(onUpdateCheckResult);
        }
        
        public void AddUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            _stage10NativeAndroidBridge.AddUpdateStartedListener(onUpdateStarted);
        }
        
        public void RemoveUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            _stage10NativeAndroidBridge.RemoveUpdateStartedListener(onUpdateStarted);
        }
        
        #endregion
    }
}
#endif