#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal interface ISwStage10NativeApi : ISwNativeApi
    {
        #region --- Public Methods ---
        
        public void StartAppUpdate(SwAppUpdateParam updateParam);
        public void CheckForUpdate();
        public void AddUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult);
        public void RemoveUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult);
        public void AddUpdateStartedCallback(OnUpdateStarted onUpdateStarted);
        public void RemoveUpdateStartedCallback(OnUpdateStarted onUpdateStarted);
        
        #endregion
    }
}
#endif