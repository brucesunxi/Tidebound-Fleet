#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal class SwStage10NativeIosApi : SwNativeIosApi, ISwStage10NativeApi
    {
        #region --- Member ---
        
        private readonly SwStage10NativeIosBridge _stage10NativeIosBridge;
        
        #endregion
        
        
        #region --- Construction ---
        
        public SwStage10NativeIosApi(SwStage10NativeIosBridge nativeBridge) : base(nativeBridge)
        {
            _stage10NativeIosBridge = nativeBridge;
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public void StartAppUpdate(SwAppUpdateParam updateParam)
        {
            _stage10NativeIosBridge.StartAppUpdate(updateParam);
        }
        
        public void CheckForUpdate()
        {
            // Not Supported
        }
        
        public void AddUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            // Not Supported
        }
        
        public void RemoveUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            // Not Supported
        }
        
        public void AddUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            // Not Supported
        }
        
        public void RemoveUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            // Not Supported
        }
        
        #endregion
    }
}
#endif