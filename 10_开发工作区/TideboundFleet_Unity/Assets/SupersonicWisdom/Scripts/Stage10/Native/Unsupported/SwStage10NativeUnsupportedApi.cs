#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal class SwStage10NativeUnsupportedApi : SwNativeUnsupportedApi, ISwStage10NativeApi
    {
        #region --- Construction ---
        
        public SwStage10NativeUnsupportedApi(SwNativeBridge nativeBridge) : base(nativeBridge)
        {
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public void StartAppUpdate(SwAppUpdateParam updateParam)
        {
            LogUnsupported(nameof(StartAppUpdate));
        }
        
        public void CheckForUpdate()
        {
            LogUnsupported(nameof(CheckForUpdate));
        }
        
        public void AddUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            LogUnsupported(nameof(AddUpdateCheckCallback));
        }
        
        public void RemoveUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            LogUnsupported(nameof(RemoveUpdateCheckCallback));
        }
        
        public void AddUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            LogUnsupported(nameof(AddUpdateStartedCallback));
        }
        
        public void RemoveUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            LogUnsupported(nameof(RemoveUpdateStartedCallback));
        }
        
        #endregion
    }
}
#endif