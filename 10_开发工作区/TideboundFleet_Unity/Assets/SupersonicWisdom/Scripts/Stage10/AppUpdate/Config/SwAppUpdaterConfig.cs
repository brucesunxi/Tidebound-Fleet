#if SW_STAGE_STAGE10_OR_ABOVE
using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    [Serializable]
    internal class SwAppUpdaterConfig
    {
        [JsonProperty(nameof(updatePopupType), DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TYPE_VALUE)]
        public ESwAppUpdatePopupType updatePopupType;
        
        [JsonProperty(nameof(nativePopupType), DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(SwAppUpdateLocalConfig.APP_UPDATE_NATIVE_POPUP_TYPE_VALUE)]
        public ESwAppUpdateNativePopupType nativePopupType;
        
        [JsonProperty(nameof(shouldShowWisdomPopup), DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(SwAppUpdateLocalConfig.APP_UPDATE_WISDOM_POPUP_ENABLED_VALUE)]
        public bool shouldShowWisdomPopup;
        
        [JsonProperty(nameof(customUpdatePopupText), DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TEXT_VALUE)]
        public string customUpdatePopupText;
        
        [JsonProperty(nameof(customUpdatePopupTitle), DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TITLE_VALUE)]
        public string customUpdatePopupTitle;
        
        [JsonProperty(nameof(customUpdatePopupButtonText), DefaultValueHandling = DefaultValueHandling.Populate), DefaultValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_BUTTON_TEXT_VALUE)]
        public string customUpdatePopupButtonText;
    }
}
#endif