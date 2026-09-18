using System;
using System.Text.RegularExpressions;

namespace SupersonicWisdomSDK.Editor
{
    public abstract class SwBaseVersionValidation : ISwCrossPlatformVersionValidation
    {
        #region --- Members ---

        private static readonly SwTerminalCommandExecute Terminal = new();

        #endregion


        #region --- Properties ---

        public string MinimumVersion { get; }
        public string ValidationName { get; }

        #endregion


        #region --- Construction ---

        protected SwBaseVersionValidation(string validationName, string minimumVersion)
        {
            ValidationName = validationName;
            MinimumVersion = minimumVersion;
        }

        #endregion


        #region --- Public Methods ---

        public abstract string GetVersion();

        public virtual string GetDialogTitle()
        {
            return $"{ValidationName} Version Warning";
        }

        public virtual string ReportVersionDiscrepancy(string currentVersion, string requiredVersion)
        {
            return $"{ValidationName} version is too low: {currentVersion}, required: {requiredVersion}";
        }

        public virtual string ReportMissingProgram(string requiredVersion)
        {
            return $"{ValidationName} file can't be found, we require: {requiredVersion}, please make sure your {ValidationName} is set correctly in the settings";
        }

        public virtual bool IsValid(string version)
        {
            return IsVersionGreaterOrEqual(version, MinimumVersion);
        }

        #endregion


        #region --- Private Methods ---

        private bool IsVersionGreaterOrEqual(string versionStr, string requiredVersionStr)
        {
            if (Version.TryParse(versionStr, out var version) &&
                Version.TryParse(requiredVersionStr, out var requiredVersion))
            {
                return version >= requiredVersion;
            }

            if (bool.TryParse(versionStr, out var versionBool) &&
                bool.TryParse(requiredVersionStr, out var requiredVersionBool))
            {
                return versionBool == requiredVersionBool;
            }

            if (int.TryParse(versionStr, out var versionInt) &&
                int.TryParse(requiredVersionStr, out var requiredVersionInt))
            {
                return versionInt >= requiredVersionInt;
            }

            var numericVersion = ExtractNumericVersion(versionStr);
            var numericRequiredVersion = ExtractNumericVersion(requiredVersionStr);
            
            if (numericVersion.HasValue && numericRequiredVersion.HasValue)
            {
                return numericVersion.Value >= numericRequiredVersion.Value;
            }

            return false;
        }

        
        private int? ExtractNumericVersion(string version)
        {
            var match = Regex.Match(version, @"\d+");
            
            if (match.Success && int.TryParse(match.Value, out var numericVersion))
            {
                return numericVersion;
            }
            
            return null;
        }

        protected string ExecuteTerminalCommand(string command, string args, string regexPattern)
        {
            try
            {
                return Terminal.ExecuteTerminalCommand(command, args, regexPattern);
            }
            catch (Exception e)
            {
                var exceptionMessage = $"ExecuteTerminalCommand failed with error: {e}";
                SwInfra.Logger.LogWarning(EWisdomLogType.Build, exceptionMessage);
                SwEditorTracker.TrackEditorEvent(nameof(ExecuteTerminalCommand),ESwEditorWisdomLogType.PreBuild, ESwEventSeverity.Error, exceptionMessage);
                return null;
            }
        }

        #endregion
    }
}