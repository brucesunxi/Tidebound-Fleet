#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal interface ISwAppUpdateManager : ISwCoreConfigListener, ISwLocalConfigProvider
    {
        #region --- Properties ---
        
        public SwAppUpdaterConfig AppUpdaterConfig { get; }
        public SwVisualElementPayload[] UiToolkitWindowPayload { get; }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public IEnumerator TryToUpdateApp();
        
        #endregion
    }
}
#endif