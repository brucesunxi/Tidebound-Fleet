#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal interface ISwAppUpdateExecutor
    {
        IEnumerator InitiateAppUpdate(ESwAppUpdateNativePopupType popupType);
    }
}
#endif