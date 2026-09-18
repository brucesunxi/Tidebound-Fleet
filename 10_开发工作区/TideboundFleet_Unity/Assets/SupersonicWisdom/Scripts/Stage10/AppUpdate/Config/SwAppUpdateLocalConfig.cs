#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    internal class SwAppUpdateLocalConfig : SwLocalConfig
    {
        #region --- Constants ---
        
        // Popup infra settings
        public const string APP_UPDATE_POPUP_TYPE_KEY = "swAppUpdatePopupType";
        public const string APP_UPDATE_POPUP_TYPE_VALUE = "None";
        public const string APP_UPDATE_WISDOM_POPUP_ENABLED_KEY = "swAppUpdateWisdomPopupEnabled";
        public const bool APP_UPDATE_WISDOM_POPUP_ENABLED_VALUE = true;
        public const string APP_UPDATE_NATIVE_POPUP_TYPE_KEY = "swAppUpdateNativePopupType";
        public const string APP_UPDATE_NATIVE_POPUP_TYPE_VALUE = "Permode";
        
        // Custom Popup visual settings
        public const string APP_UPDATE_POPUP_TEXT_KEY = "swAppUpdateCustomPopupText";
        public const string APP_UPDATE_POPUP_TEXT_VALUE = "To keep using this app, please update to the latest version";
        public const string APP_UPDATE_POPUP_TITLE_KEY = "swAppUpdateCustomPopupTitle";
        public const string APP_UPDATE_POPUP_TITLE_VALUE = "Update Required";
        public const string APP_UPDATE_POPUP_BUTTON_TEXT_KEY = "swAppUpdateCustomPopupButtonText";
        public const string APP_UPDATE_POPUP_BUTTON_TEXT_VALUE = "Update";
        
        #endregion


        #region --- Properties ---

        public override Dictionary<string, object> LocalConfigValues
        {
            get
            {
                return new Dictionary<string, object>
                {
                    { APP_UPDATE_WISDOM_POPUP_ENABLED_KEY, APP_UPDATE_WISDOM_POPUP_ENABLED_VALUE },
                    { APP_UPDATE_POPUP_TYPE_KEY, APP_UPDATE_POPUP_TYPE_VALUE },
                    { APP_UPDATE_POPUP_TEXT_KEY, APP_UPDATE_POPUP_TEXT_VALUE },
                    { APP_UPDATE_POPUP_TITLE_KEY, APP_UPDATE_POPUP_TITLE_VALUE },
                    { APP_UPDATE_POPUP_BUTTON_TEXT_KEY, APP_UPDATE_POPUP_BUTTON_TEXT_VALUE },
                    { APP_UPDATE_NATIVE_POPUP_TYPE_KEY, APP_UPDATE_NATIVE_POPUP_TYPE_VALUE },
                };
            }
        }

        #endregion
    }
}
#endif