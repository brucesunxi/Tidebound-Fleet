#if SW_STAGE_STAGE10_OR_ABOVE
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    [Serializable]
    internal class SwStage10Config : SwCoreConfig, ISwStage10InternalConfig
    {
        #region --- Members ---

        [JsonProperty("unavailable")]
        public bool Unavailable { get; set; }
        
        [JsonProperty("agent")]
        public SwAgentConfig Agent { get; set; }
        
        [JsonProperty("events")]
        public SwNativeEventsConfig Events { get; set; }
        
        [JsonProperty("cv")]
        public SwCvConfig Cv { get; set; }

        #endregion
        

        #region --- Construction ---

        public SwStage10Config(Dictionary<string, object> defaultDynamicConfig) : base(defaultDynamicConfig)
        {
            Events = new SwNativeEventsConfig();
        }

        #endregion
        
        
        #region --- Private Methods ---
        
        protected internal override void OnAfterDeserialized()
        {
#if SW_STAGE_STAGE10
            ResolveTacConfig(SwStage10TacLocalConfig.TRIGGER_DATA_KEY_, SwStage10TacLocalConfig.TRIGGER_DATA_VALUE_);
#endif
        }

        protected void ResolveTacConfig(string key, string defaultValue)
        {
            if (DynamicConfig == null || Tac != null && !DynamicConfig.HasConfigKey(key)) return;

            try
            {
                var triggerDataStringify = DynamicConfig.GetValue(key, defaultValue);
                Tac = ParseKeyFromDynamicConfigToObject<SwTacConfig>(triggerDataStringify);
                Tac.MaximumUiActions = DynamicConfig.GetValue(SwTacSystem.TAC_MAXIMUM_UI_ACTIONS, Tac.MaximumUiActions);
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogError(EWisdomLogType.Config, e);

                throw;
            }
        }
        
        #endregion
    }
}
#endif