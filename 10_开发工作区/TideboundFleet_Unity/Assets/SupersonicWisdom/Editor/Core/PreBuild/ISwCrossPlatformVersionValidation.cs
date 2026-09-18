namespace SupersonicWisdomSDK.Editor
{
    public interface ISwCrossPlatformVersionValidation
    {
        #region --- Properties ---

        public string MinimumVersion { get; }
        public string ValidationName { get; }

        #endregion


        #region --- Public Methods ---

        public string GetVersion();
        public string GetDialogTitle();
        public string ReportVersionDiscrepancy(string currentVersion, string requiredVersion);
        public string ReportMissingProgram(string requiredVersion);
        public bool IsValid(string version);

        #endregion
    }
}