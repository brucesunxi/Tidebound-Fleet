using System.Collections.Generic;

namespace SupersonicWisdomSDK.Editor
{
    public class SwUnsupportedPreBuildValidator : SwPreBuildValidator
    {
        public SwUnsupportedPreBuildValidator()
        {
            Validations = new List<ISwCrossPlatformVersionValidation> { };
        }
    }
}