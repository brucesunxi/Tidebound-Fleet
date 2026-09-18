using UnityEditor;

namespace SupersonicWisdomSDK.Editor
{
    public class SwPreBuildValidatorFactory
    {
        #region --- Public Methods ---

        public SwPreBuildValidator GetValidator(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.Android:
#if UNITY_ANDROID
                    return new SwAndroidPreBuildValidator();
#endif
                case BuildTarget.iOS:
#if UNITY_IOS
                    return new SwIosPreBuildValidator();
#endif
                default:
                    return new SwUnsupportedPreBuildValidator();
            }
        }

        #endregion
    }
}