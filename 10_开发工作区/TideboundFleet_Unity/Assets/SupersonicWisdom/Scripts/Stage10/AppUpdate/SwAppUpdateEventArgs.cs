#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal abstract class SwAppUpdateEventArgs 
    {
        #region --- Constants ---
        
        private const string APP_UPDATE_WISDOM_POPUP_IMPRESSION = "AppUpdateWisdomPopupImpression";
        private const string APP_UPDATE_OPERATION_SYSTEM_POPUP_IMPRESSION = "AppUpdateOperationSystemPopupImpression";
        private const string APP_UPDATE_WISDOM_POPUP_CLICKED = "AppUpdateWisdomPopupClicked";
        
        #endregion
        
        
        #region --- Event Args ---
        
        public struct CustomUpdatePopupShownEvent : ISwAppUpdateEvent
        {
            public string eventName => APP_UPDATE_WISDOM_POPUP_IMPRESSION;
            public string eventValue { get; set; }
        }
        
        public struct NativeUpdatePopupShownEvent : ISwAppUpdateEvent
        {
            public string eventName => APP_UPDATE_OPERATION_SYSTEM_POPUP_IMPRESSION;
            public string eventValue { get; set; }
        }
        
        public struct CustomUpdatePopupUpdateSkippedEvent : ISwAppUpdateEvent
        {
            public string eventName => APP_UPDATE_WISDOM_POPUP_CLICKED;
            public string eventValue { get; set; }
        }
        
        public struct CustomUpdatePopupUpdateSelectedEvent : ISwAppUpdateEvent
        {
            public string eventName => APP_UPDATE_WISDOM_POPUP_CLICKED;
            public string eventValue { get; set; }
        }
        
        #endregion
    }
}
#endif