#if SW_STAGE_STAGE10_OR_ABOVE
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace SupersonicWisdomSDK
{
    internal class SwStage10NativeIosBridge : SwNativeIosBridge
    {
        #region --- Public Methods ---
        
        public void StartAppUpdate(SwAppUpdateParam updateParam)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (updateParam is SwIosAppUpdateParams iosAppUpdateParams)
            {
                openAppStorePageWithAppId(iosAppUpdateParams.AppStoreId);
            }
#else
            return;
#endif
        }
        
        #endregion
        
#if UNITY_IOS        
        #region --- Native Methods ---
        
        [DllImport("__Internal")]
        private static extern void openAppStorePageWithAppId(string gameId);
        
        #endregion
#endif
    }
}
#endif