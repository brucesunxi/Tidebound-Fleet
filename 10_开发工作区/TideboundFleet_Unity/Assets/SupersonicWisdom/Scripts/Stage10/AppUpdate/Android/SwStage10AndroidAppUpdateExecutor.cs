#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal class SwStage10AndroidAppUpdateExecutor : ISwAppUpdateExecutor
    {
        #region --- Members ---
        
        private readonly ISwStage10NativeApi _swNativeApi;
        private bool _isInitiateAppUpdateFlowComplete;
        
        #endregion
        
        
        #region --- Constructor ---

        public SwStage10AndroidAppUpdateExecutor(ISwStage10NativeApi swNativeApi)
        {
            _swNativeApi = swNativeApi;
        }
        
        #endregion
        

        #region --- Public Methods ---
        
        public IEnumerator InitiateAppUpdate(ESwAppUpdateNativePopupType nativePopupType)
        {
            var updateType = (int)nativePopupType;
            var updateParam = new SwAndroidAppUpdateParams(updateType);
            _swNativeApi.StartAppUpdate(updateParam);
            
            yield return new WaitUntil(() => _isInitiateAppUpdateFlowComplete);
        }
        
        public void OnUpdateStarted(bool didStart, string error)
        {
            // Called from native by the manager on this class
            _isInitiateAppUpdateFlowComplete = true;
        }

        #endregion
    }
}
#endif