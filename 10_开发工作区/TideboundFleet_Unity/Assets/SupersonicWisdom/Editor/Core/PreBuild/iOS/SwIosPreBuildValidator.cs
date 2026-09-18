#if UNITY_IOS

using System.Collections.Generic;

namespace SupersonicWisdomSDK.Editor
{
    public class SwIosPreBuildValidator : SwPreBuildValidator
    {
        #region --- Construction ---

        public SwIosPreBuildValidator()
        {
            Validations = new List<ISwCrossPlatformVersionValidation>
            {
                new SwXcodeVersionValidation(),
                new SwCocoapodsVersionValidation(),
                new SwIosTargetApiVersionValidation(),
            };
        }

        #endregion
    }
}

#endif