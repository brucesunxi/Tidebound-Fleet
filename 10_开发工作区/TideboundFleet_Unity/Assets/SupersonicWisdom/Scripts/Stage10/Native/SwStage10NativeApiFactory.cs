#if SW_STAGE_STAGE10_OR_ABOVE

#if UNITY_EDITOR
using SupersonicWisdomSDK.Editor;
#endif

namespace SupersonicWisdomSDK
{
    internal static class SwStage10NativeApiFactory
    {
        public static ISwStage10NativeApi GetInstance()
        {
            
            if (SwUtils.System.IsRunningOnAndroid())
            {
                return new SwStage10NativeAndroidApi(new SwStage10NativeAndroidBridge());
            }
            
            if (SwUtils.System.IsRunningOnIos())
            {
                return new SwStage10NativeIosApi(new SwStage10NativeIosBridge());
            }
            
#if UNITY_EDITOR
            if (SwUtils.System.IsRunningOnEditor())
            {
                return new SwStage10NativeEditorApi(null);
            }
#endif
            
            return new SwStage10NativeUnsupportedApi(null);
        }
    }
}

#endif