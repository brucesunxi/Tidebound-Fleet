#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    internal class SwStage10TacLocalConfig : SwLocalConfig
    {
        #region --- Constants ---
        
        internal const string TRIGGER_DATA_KEY_ = "swTacActionsData";
        internal const string TRIGGER_DATA_VALUE_ = "{\"maximumUiActions\":2,\"actionsWaterfall\":[{\"id\":\"Show-app-update\",\"priority\":20000,\"action\":\"ShowAppUpdate\",\"searchType\":\"OR\",\"triggers\":[\"sw_config_ready\"],\"conditions\":{\"configStatus\":[\"Remote\"]}}]}";

        #endregion


        #region --- Properties ---

        public override Dictionary<string, object> LocalConfigValues
        {
            get
            {
                return new Dictionary<string, object>
                {
                    { TRIGGER_DATA_KEY_, TRIGGER_DATA_VALUE_ },
                };
            }
        }

        #endregion
    }
}
#endif