#if UNITY_ANDROID

using UnityEditor;

namespace SupersonicWisdomSDK.Editor
{
    public class SwAndroidMinimumApiVersionValidation : SwBaseVersionValidation
    {
        #region --- Construction ---

        public SwAndroidMinimumApiVersionValidation() : base(SwPreBuildVersionConstants.ANDROID_MINIMUM_API, SwPreBuildVersionConstants.REQUIRED_ANDROID_MINIMUM_API) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            return ((int)PlayerSettings.Android.minSdkVersion).ToString();
        }

        public override string ReportMissingProgram(string requiredVersion)
        {
            return $"The Android Minimum API version is not set. Please set it to {requiredVersion} or higher in the Player Settings.";
        }

        public override string ReportVersionDiscrepancy(string currentVersion, string requiredVersion)
        {
            return currentVersion == "0"
                ? $"The Android Minimum API version was set to Automatic. Please set it to {requiredVersion} or higher manually in the Player Settings."
                : $"The Android Minimum API version is too low. Please set it to {requiredVersion} or higher in the Player Settings.";
        }


        #endregion
    }
}

#endif