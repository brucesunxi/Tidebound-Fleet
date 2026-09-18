#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    internal class SwGameSessionManagerLocalConfig : SwLocalConfig
    {
        #region --- Constants ---
        
        public const string GAME_SESSION_END_INTERVAL_KEY = "swGameSessionEndInterval";
        public const float GAME_SESSION_END_INTERVAL_VALUE = 10;
        
        #endregion
        
        
        #region --- Properties ---
        
        public override Dictionary<string, object> LocalConfigValues
        {
            get
            {
                return new Dictionary<string, object>
                {
                    {
                        GAME_SESSION_END_INTERVAL_KEY, GAME_SESSION_END_INTERVAL_VALUE
                    },
                };
            }
        }
        
        #endregion
    }
}
#endif