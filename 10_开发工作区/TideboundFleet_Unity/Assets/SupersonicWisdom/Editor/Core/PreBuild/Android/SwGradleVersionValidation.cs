#if UNITY_ANDROID

using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;

namespace SupersonicWisdomSDK.Editor
{
    public class SwGradleVersionValidation : SwBaseVersionValidation
    {
        #region --- Construction ---

        public SwGradleVersionValidation() : base(SwPreBuildVersionConstants.GRADLE,SwPreBuildVersionConstants.REQUIRED_GRADLE) { }

        #endregion


        #region --- Public Methods ---

        public override string GetVersion()
        {
            var gradleLibPath = Path.Combine(AndroidExternalToolsSettings.gradlePath, "lib");

            if (!Directory.Exists(gradleLibPath))
            {
                return string.Empty;
            }

            var files = Directory.GetFiles(gradleLibPath, "gradle-core-*.jar");
            
            if (files.Length == 0)
            {
                return string.Empty;
            }

            foreach (var file in files)
            {
                var match = Regex.Match(file, @"gradle-core-(\d+(\.\d+){0,3})\.jar");

                if (!match.Success) continue;

                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        public override string ReportMissingProgram(string requiredVersion)
        {
            return "No gradle-core-*.jar file found in the Gradle lib directory." + AndroidExternalToolsSettings.gradlePath;
        }

        #endregion
    }
}

#endif