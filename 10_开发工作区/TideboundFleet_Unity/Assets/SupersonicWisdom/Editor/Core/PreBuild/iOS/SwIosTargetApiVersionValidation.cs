#if UNITY_IOS

using UnityEditor;

namespace SupersonicWisdomSDK.Editor
{
    public class SwIosTargetApiVersionValidation : SwBaseVersionValidation
    {
        #region --- Construction ---

        public SwIosTargetApiVersionValidation() : base(SwPreBuildVersionConstants.IOS_TARGET_API,SwPreBuildVersionConstants.REQUIRED_IOS_TARGET_API) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            return PlayerSettings.iOS.targetOSVersionString;
        }

        #endregion
    }
}

#endif