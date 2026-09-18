#if UNITY_ANDROID

using System.IO;
using System.Linq;
using UnityEngine;

namespace SupersonicWisdomSDK.Editor
{
    public class SwAndroidXSettingsValidation : SwBaseVersionValidation
    {
        #region --- Constants ---

        private const string FILE_PATH = "Plugins/Android/gradleTemplate.properties";
        private const string SEARCH_STRING_ANDROID_X = "android.useAndroidX=true";
        private const string SEARCH_STRING_JETIFIER = "android.enableJetifier=true";

        #endregion


        #region --- Construction ---

        public SwAndroidXSettingsValidation() : base(SwPreBuildVersionConstants.ANDROIDX, SwPreBuildVersionConstants.REQUIRED_ANDROIDX) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            var gradlePropertiesPath = Path.Combine(Application.dataPath, FILE_PATH);
            var androidxEnabled = CheckStringInFile(gradlePropertiesPath, SEARCH_STRING_ANDROID_X);
            var jetifierEnabled = CheckStringInFile(gradlePropertiesPath, SEARCH_STRING_JETIFIER);

            return (androidxEnabled && jetifierEnabled).ToString();
        }

        public override string GetDialogTitle()
        {
            return "AndroidX Version Error";
        }

        public override string ReportVersionDiscrepancy(string currentVersion, string requiredVersion)
        {
            return $"In {FILE_PATH}, please set both {SEARCH_STRING_ANDROID_X} and {SEARCH_STRING_JETIFIER} to true";
        }

        public override string ReportMissingProgram(string requiredVersion)
        {
            return $"File not found: {FILE_PATH}, couldn't verify {SEARCH_STRING_ANDROID_X} or {SEARCH_STRING_JETIFIER}";
        }

        #endregion


        #region --- Private Methods ---

        private bool CheckStringInFile(string filePath, string searchString)
        {
            if (!File.Exists(filePath)) return false;

            var content = File.ReadAllText(filePath);

            return content.Contains(searchString);
        }

        #endregion
    }
}

#endif