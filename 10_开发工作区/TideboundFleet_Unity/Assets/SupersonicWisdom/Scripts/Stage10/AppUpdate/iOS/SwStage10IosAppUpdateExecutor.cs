#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;

namespace SupersonicWisdomSDK
{
    internal class SwStage10IosAppUpdateExecutor : ISwAppUpdateExecutor
    {
        #region --- Members ---
        
        private readonly string _appStoreId;
        private readonly ISwStage10NativeApi _swNativeApi;
        
        #endregion
        
        
        #region --- Constructor ---

        public SwStage10IosAppUpdateExecutor(string appStoreId, ISwStage10NativeApi swNativeApi)
        {
            _appStoreId = appStoreId;
            _swNativeApi = swNativeApi;
        }
        
        #endregion
        

        #region --- Public Methods ---
        
        public IEnumerator InitiateAppUpdate(ESwAppUpdateNativePopupType popupType)
        {
            var updateParam = new SwIosAppUpdateParams(_appStoreId);
            _swNativeApi.StartAppUpdate(updateParam);
            
            yield break;
        }

        #endregion
    }
}
#endif