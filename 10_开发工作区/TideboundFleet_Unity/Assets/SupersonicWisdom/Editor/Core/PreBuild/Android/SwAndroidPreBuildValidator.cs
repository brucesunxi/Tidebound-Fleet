#if UNITY_ANDROID

using System.Collections.Generic;

namespace SupersonicWisdomSDK.Editor
{
    public class SwAndroidPreBuildValidator : SwPreBuildValidator
    {
        #region --- Construction ---

        public SwAndroidPreBuildValidator()
        {
            Validations = new List<ISwCrossPlatformVersionValidation>
            {
                new SwAndroidXSettingsValidation(),
                new SwAndroidTargetApiVersionValidation(),
                new SwAndroidMinimumApiVersionValidation(),
                new SwGradleVersionValidation(),
            };
        }

        #endregion
    }
}

#endif