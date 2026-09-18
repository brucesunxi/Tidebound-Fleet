#if UNITY_ANDROID

using UnityEditor;

namespace SupersonicWisdomSDK.Editor
{
    public class SwAndroidTargetApiVersionValidation : SwBaseVersionValidation
    {
        #region --- Construction ---

        public SwAndroidTargetApiVersionValidation() : base(SwPreBuildVersionConstants.ANDROID_TARGET_API,SwPreBuildVersionConstants.REQUIRED_ANDROID_TARGET_API) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            return PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto ? int.MaxValue.ToString() : PlayerSettings.Android.targetSdkVersion.ToString();
        }

        public override string ReportMissingProgram(string requiredVersion)
        {
            return $"The Android Target API version is not set. Please set it to {requiredVersion} or higher in the Player Settings.";
        }

        public override string ReportVersionDiscrepancy(string currentVersion, string requiredVersion)
        {
            return currentVersion == "0"
                ? $"The Android Target API version was set to Automatic. Please set it to {requiredVersion} or higher manually in the Player Settings."
                : $"The Android Target API version is too low. Please set it to {requiredVersion} or higher in the Player Settings.";
        }


        #endregion
    }
}

#endif