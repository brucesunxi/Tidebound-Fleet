#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal abstract class SwBaseAppUpdateValidator : ISwAppUpdateValidator
    {
        #region --- Public Methods ---

        public virtual IEnumerator CheckForUpdate() { yield break; }
        
        #endregion
        
        
        #region --- Properties ---
        
        public bool IsUpdateAvailable { get; protected set; }
        
        #endregion
    }
}
#endif