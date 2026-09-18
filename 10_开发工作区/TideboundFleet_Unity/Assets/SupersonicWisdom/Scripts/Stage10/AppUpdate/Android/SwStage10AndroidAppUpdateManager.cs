#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal class SwStage10AndroidAppUpdateManager : SwBaseAppUpdateManager, ISwAppUpdateListener
    {
        #region --- Members ---
        
        private readonly ISwStage10NativeApi _swNativeApi;
        
        #endregion
        
        
        #region --- Construction ---
        
        public SwStage10AndroidAppUpdateManager(SwCoreTracker tracker, ISwStage10NativeApi swNativeApi, SwUiToolkitManager uiToolkitManager) : base(uiToolkitManager, tracker)
        {
            _swNativeApi = swNativeApi;
            _appUpdateValidator = new SwStage10AndroidAppUpdateValidator(swNativeApi);
            _appUpdateExecutor = new SwStage10AndroidAppUpdateExecutor(swNativeApi);
            CustomUpdatePopupUpdateSelectedEvent += TriggerNativePopupWhenCustomPopupIsSelected;
        }
        
        ~SwStage10AndroidAppUpdateManager()
        {
            CustomUpdatePopupUpdateSelectedEvent -= TriggerNativePopupWhenCustomPopupIsSelected;
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public void OnUpdateCheckResult(string versionCode)
        {
            if (_appUpdateValidator is SwStage10AndroidAppUpdateValidator androidAppUpdateValidator)
            {
                androidAppUpdateValidator.OnUpdateCheckResult(versionCode);
            }
            
            CleanupListenerAfterNativeExecution();
        }
        
        public void OnUpdateStarted(bool didStart, string error)
        {
            if (_appUpdateExecutor is SwStage10AndroidAppUpdateExecutor androidAppUpdateExecutor)
            {
                androidAppUpdateExecutor.OnUpdateStarted(didStart, error);
            }
            
            OnNativeUpdatePopupShownEvent(new SwAppUpdateEventArgs.NativeUpdatePopupShownEvent
            {
                eventValue = didStart ? AppUpdaterConfig.nativePopupType.SwToString() : error,
            });
            CleanupListenerAfterNativeExecution();
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        protected override IEnumerator CheckAndPromptForCustomAppUpdate()
        {
            _swNativeApi.AddUpdateCheckCallback(OnUpdateCheckResult);
            
            yield return base.CheckAndPromptForCustomAppUpdate();
        }
        
        protected override IEnumerator StartNativeUpdateFlow(ESwAppUpdatePopupType popupType, ESwAppUpdateNativePopupType nativePopupSubType = ESwAppUpdateNativePopupType.Permode)
        {
            _swNativeApi.AddUpdateStartedCallback(OnUpdateStarted);
            
            if (nativePopupSubType == ESwAppUpdateNativePopupType.Permode)
            {
                nativePopupSubType = popupType == ESwAppUpdatePopupType.Force ? ESwAppUpdateNativePopupType.Immediate : ESwAppUpdateNativePopupType.Flexible;
            }
            
            yield return base.StartNativeUpdateFlow(popupType, nativePopupSubType);
        }
        
        private void TriggerNativePopupWhenCustomPopupIsSelected(ISwAppUpdateEvent customUpdatePopupUpdateSelectedEvent)
        {
            SwInfra.CoroutineService.StartCoroutine(StartNativeUpdateFlow(AppUpdaterConfig.updatePopupType));
        }
        
        private void CleanupListenerAfterNativeExecution()
        {
            _swNativeApi.RemoveUpdateCheckCallback(OnUpdateCheckResult);
            _swNativeApi.RemoveUpdateStartedCallback(OnUpdateStarted);
        }
        
        #endregion
    }
}
#endif