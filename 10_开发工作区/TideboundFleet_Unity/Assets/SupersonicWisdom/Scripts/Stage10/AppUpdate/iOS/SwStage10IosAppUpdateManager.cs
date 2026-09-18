#if SW_STAGE_STAGE10_OR_ABOVE

namespace SupersonicWisdomSDK
{
    internal class SwStage10IosAppUpdateManager : SwBaseAppUpdateManager
    {
        #region --- Constructor ---
        
        public SwStage10IosAppUpdateManager(SwSettings settings, SwCoreTracker tracker, ISwStage10NativeApi swNativeApi, SwUiToolkitManager uiToolkitManager) : base(uiToolkitManager, tracker)
        {
            _appUpdateValidator = new SwStage10IosAppUpdateValidator();
            _appUpdateExecutor = new SwStage10IosAppUpdateExecutor(settings.IosAppId, swNativeApi);
            CustomUpdatePopupUpdateSelectedEvent += TriggerNativePopupWhenCustomPopupIsSelected;
        }
        
        ~SwStage10IosAppUpdateManager()
        {
            CustomUpdatePopupUpdateSelectedEvent -= TriggerNativePopupWhenCustomPopupIsSelected;
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private void TriggerNativePopupWhenCustomPopupIsSelected(ISwAppUpdateEvent customUpdatePopupUpdateSelectedEvent)
        {
            SwInfra.CoroutineService.StartCoroutine(StartNativeUpdateFlow(AppUpdaterConfig.updatePopupType));
        }
        
        #endregion
    }
}
#endif