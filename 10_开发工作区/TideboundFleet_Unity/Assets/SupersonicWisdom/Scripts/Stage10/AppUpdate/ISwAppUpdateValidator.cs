#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    public interface ISwAppUpdateValidator
    {
        #region --- Properties ---
        
        bool IsUpdateAvailable { get; }
        
        #endregion
        
        
        #region --- Methods ---

        public IEnumerator CheckForUpdate();
        
        #endregion
    }
}
#endif