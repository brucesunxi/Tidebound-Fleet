#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal interface ISwAppUpdateListener
    {
        void OnUpdateCheckResult(string versionCode);
        void OnUpdateStarted(bool didStart, string error);
    }
}
#endif