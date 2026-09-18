#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    /// <summary>
    /// Factory class that creates an instance of ISwAppUpdateManager.
    /// </summary>
    internal abstract class SwStage10AppUpdateManagerFactory
    {
        public static ISwAppUpdateManager CreateAppUpdateManager(SwSettings settings, SwCoreTracker tracker, ISwStage10NativeApi swNativeApi, SwUiToolkitManager uiToolkitManager)
        {
#if UNITY_EDITOR
            return new SwStage10UnsupportedAppUpdateManager(settings, tracker, swNativeApi, uiToolkitManager);
#elif UNITY_ANDROID
            return new SwStage10AndroidAppUpdateManager(tracker, swNativeApi, uiToolkitManager);
#elif UNITY_IOS
            return new SwStage10IosAppUpdateManager(settings, tracker, swNativeApi, uiToolkitManager);
#else
            return null;
#endif
        }
    }
}
#endif