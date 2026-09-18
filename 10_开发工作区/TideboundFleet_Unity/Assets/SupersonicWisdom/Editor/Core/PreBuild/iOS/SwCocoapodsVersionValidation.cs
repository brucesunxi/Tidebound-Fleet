#if UNITY_IOS

namespace SupersonicWisdomSDK.Editor
{
    public class SwCocoapodsVersionValidation : SwBaseVersionValidation
    {
        #region --- Constants ---

        private const string POD = "pod";
        private const string VERSION_FLAG = "--version";
        private const string REGEX = @"(\d+(\.\d+){0,3})";

        #endregion


        #region --- Construction ---

        public SwCocoapodsVersionValidation() : base(SwPreBuildVersionConstants.COCOAPODS,SwPreBuildVersionConstants.REQUIRED_COCOAPODS) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            return ExecuteTerminalCommand(POD, VERSION_FLAG, REGEX);
        }

        #endregion
    }
}

#endif