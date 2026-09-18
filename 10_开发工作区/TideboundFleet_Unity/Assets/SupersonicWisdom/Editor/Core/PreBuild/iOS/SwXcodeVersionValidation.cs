#if UNITY_IOS

namespace SupersonicWisdomSDK.Editor
{
    public class SwXcodeVersionValidation : SwBaseVersionValidation
    {
        #region --- Constants ---

        private const string XCODEBUILD = "xcodebuild";
        private const string VERSION_FLAG = "-version";
        private const string REGEX = @"Xcode (\d+(\.\d+){0,3})";

        #endregion


        #region --- Construction ---

        public SwXcodeVersionValidation() : base(SwPreBuildVersionConstants.XCODE,SwPreBuildVersionConstants.REQUIRED_XCODE) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            return ExecuteTerminalCommand(XCODEBUILD,VERSION_FLAG , REGEX);
        }

        #endregion
    }
}

#endif