using UnityEditor.Build.Reporting;

namespace SupersonicWisdomSDK.Editor
{
    public static class SwPreBuildVersions
    {
        #region --- Public Methods ---

        public static void OnPreprocessBuild(BuildReport report)
        {
            var factory = new SwPreBuildValidatorFactory();
            var validator = factory.GetValidator(report.summary.platform);
            validator?.CheckBuild();
        }

        #endregion
    }
}