#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal class SwStage10UnsupportedAppUpdateManager : SwBaseAppUpdateManager
    {
        #region --- Construction ---
        
        public SwStage10UnsupportedAppUpdateManager(SwSettings settings, SwCoreTracker tracker, ISwNativeApi swNativeApi, SwUiToolkitManager uiToolkitManager) : base(uiToolkitManager, tracker)
        {
            CustomUpdatePopupUpdateSelectedEvent += CloseOnUpdateSelected;
        }
        
        ~SwStage10UnsupportedAppUpdateManager()
        {
            CustomUpdatePopupUpdateSelectedEvent -= CloseOnUpdateSelected;
        }
        
        #endregion
        
        
        #region --- Public Methods ---

        protected override IEnumerator CheckAndPromptForCustomAppUpdate()
        {
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate,"Showing Custom Update Popup (Update Button is not supported on this platform)");
            yield return ShowCustomUpdatePopup();
        }

        protected override IEnumerator StartNativeUpdateFlow(ESwAppUpdatePopupType customPopupType, ESwAppUpdateNativePopupType nativePopupSubType = ESwAppUpdateNativePopupType.Permode)
        {
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate,"Native update popup is not supported on this platform.");
            yield break;
        }
        
        #endregion
        
        
        #region --- Private Methods ---

        private void CloseOnUpdateSelected(ISwAppUpdateEvent customUpdatePopupUpdateSelectedEvent)
        {
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "Update button is not supported on this platform.");
            _uiToolkitManager.CloseWindow(ESwUiToolkitType.AppUpdate);
        }

        #endregion
    }
}
#endif