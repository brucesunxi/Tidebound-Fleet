using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditorInternal;

namespace SupersonicWisdomSDK.Editor
{
    public abstract class SwPreBuildValidator
    {
        #region --- Constants ---

        private const string DIALOG_TITLE = "Pre-Build Validation Errors";

        #endregion


        #region --- Members ---

        protected List<ISwCrossPlatformVersionValidation> Validations;
        private StringBuilder _warningMessageBuilder = new();

        #endregion


        #region --- Public Methods ---

        public void CheckBuild()
        {
            var hasErrors = false;

            foreach (var validation in Validations)
            {
                var currentVersion = validation.GetVersion();

                if (currentVersion.SwIsNullOrEmpty())
                {
                    hasErrors = true;
                    ReportMissingProgram(validation);
                }
                else if (!validation.IsValid(currentVersion))
                {
                    hasErrors = true;
                    ReportVersionDiscrepancy(validation, currentVersion);
                }
            }

            if (hasErrors)
            {
                DisplayDialog(DIALOG_TITLE, _warningMessageBuilder.ToString());
            }
        }

        #endregion


        #region --- Private Methods ---

        private static void DisplayDialog(string title, string message)
        {
            if (!InternalEditorUtility.isHumanControllingUs) return;
            
            if (EditorUserBuildSettings.development)
            {
                if (EditorUtility.DisplayDialog(title, message, SwEditorConstants.UI.ButtonTitle.OK, SwEditorConstants.UI.ButtonTitle.CLOSE))
                {
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
                }

                // Specifically failing the build if it's a development build
                SwEditorUtils.FailBuildWithMessage(nameof(SwPreBuildValidator), message);
            }
            else
            {
                EditorUtility.DisplayDialog(title, message, SwEditorConstants.UI.ButtonTitle.OK);
            }
        }

        private void ReportMissingProgram(ISwCrossPlatformVersionValidation validation)
        {
            var title = validation.GetDialogTitle();
            var message = validation.ReportMissingProgram(validation.MinimumVersion);
            
            AppendMessage(title, message);
        }

        private void ReportVersionDiscrepancy(ISwCrossPlatformVersionValidation validation, string currentVersion)
        {
            var title = validation.GetDialogTitle();
            var message = validation.ReportVersionDiscrepancy(currentVersion, validation.MinimumVersion);
            
            AppendMessage(title, message);
        }

        private void AppendMessage(string title, string message)
        {
            if (_warningMessageBuilder.Length > 0)
                _warningMessageBuilder.AppendLine(); // Separate multiple messages with a line break

            _warningMessageBuilder.AppendLine($"{title}:");
            _warningMessageBuilder.AppendLine(message);

            var eventSeverity = EditorUserBuildSettings.development ? ESwEventSeverity.Error : ESwEventSeverity.Warning;

            SwInfra.Logger.Log(EWisdomLogType.Build, message);
            SwEditorTracker.TrackEditorEvent(nameof(AppendMessage), ESwEditorWisdomLogType.PreBuild, eventSeverity, message);
        }

        #endregion
    }
}
