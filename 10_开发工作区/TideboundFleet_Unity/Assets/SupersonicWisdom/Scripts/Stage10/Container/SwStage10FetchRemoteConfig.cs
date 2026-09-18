#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal class SwStage10FetchRemoteConfig : ISwAsyncRunnable
    {
        #region --- Constants ---

        private const string CONFIG_READY_KEY = "config_ready";

        #endregion
        
        
        #region --- Members ---

        private readonly SwStage10ConfigManager _configManager;

        #endregion


        #region --- Construction ---

        public SwStage10FetchRemoteConfig(SwStage10ConfigManager configManager)
        {
            _configManager = configManager;
        }

        #endregion


        #region --- Public Methods ---

        public IEnumerator Run ()
        {
            yield return _configManager.Fetch();
            yield return SwInfra.TacSystem.InternalFireTriggersRoutine(CONFIG_READY_KEY);
        }

        #endregion
    }
}
#endif