using System.Linq;
using UnityEditor;
using UnityEngine;
using static System.String;

namespace SupersonicWisdomSDK.Editor
{
    internal class SwGeneralCoreSettingsTab : SwCoreSettingsTab
    {
        #region --- Members ---
        
        private readonly GUIContent _accountTokenLabel = new GUIContent(
            ACCOUNT_TOKEN_TEXT,
            EditorGUIUtility.IconContent(SwEditorConstants.UI.ICON_QUESTION_MARK).image, SwEditorConstants.UI.TOKEN_TOOLTIP_TEXT
        );        
        #endregion
        
        
        #region --- Constants ---

        private const string ACCOUNT_TOKEN_TEXT = "Token";
        private const string ACCOUNT_LOGIN_LABEL = "Login";
        private const string ACCOUNT_LOGOUT_LABEL = "Logout";
        private const string ACCOUNT_REFRESH_GAME_LIST_LABEL = "Refresh Game list";
        private const string ACCOUNT_RETRIEVE_CONFIGURATION = "Retrieve configuration";
        private const string ACCOUNT_STATUS_LABEL = "Status";
        private const string ACCOUNT_STATUS_TEXT = "Logged in";

        private const string ACCOUNT_TITLE_LABEL = "Account";

        #endregion


        #region --- Construction ---

        public SwGeneralCoreSettingsTab(SerializedObject soSettings) : base(soSettings)
        { }

        #endregion


        #region --- Private Methods ---

        protected internal override void DrawContent()
        {
            GUILayout.Space(SPACE_BETWEEN_FIELDS * 5);
            DrawAccountFields();
            GUILayout.Space(SPACE_BETWEEN_FIELDS * 2);
        }

        protected internal override string Name ()
        {
            return "General";
        }

        private void SaveToSettings()
        {
            if (SwAccountUtils.IsSelectedTitleDummy)
            {
                SwEditorAlerts.AlertError(SwEditorConstants.UI.PLEASE_CHOOSE_TITLE, (int)SwErrors.ESettings.ChooseTitle, SwEditorConstants.UI.ButtonTitle.OK);

                return;
            }

            SwAccountUtils.SaveSelectedTitleGamesToSettings();
            SwEditorAlerts.TitleSavedToSettingsSuccess();
        }

        protected virtual void OnTitleSelected()
        {
            // Should be handled on subclasses
        }

        private void DrawAccountFields()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ACCOUNT_TITLE_LABEL, EditorStyles.largeLabel);
            GUILayout.EndHorizontal();

            if (SwAccountUtils.IsLoggedIn)
            {
                DrawAccountTools();
            }
            else
            {
                DrawAccountLogin();
            }
        }

        private void DrawAccountLogin()
        {
            GUILayout.Space(SPACE_BETWEEN_FIELDS);
            GUILayout.BeginHorizontal();
            GUILayout.Label(_accountTokenLabel, GUILayout.Width(LABEL_WIDTH));
            Settings.accountToken = TextFieldWithoutSpaces(Settings.accountToken, alwaysEnable: true);
            GUILayout.EndHorizontal();

            GUILayout.Space(SPACE_BETWEEN_FIELDS);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(ACCOUNT_LOGIN_LABEL, GUILayout.Width(80)))
            {
                if (IsNullOrEmpty(Settings.accountToken))
                {
                    SwEditorAlerts.AlertError(SwEditorConstants.UI.MISSING_TOKEN, (int)SwErrors.ESettings.MissingToken, SwEditorConstants.UI.ButtonTitle.CLOSE);

                    return;
                }

                SwAccountUtils.Login();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(SPACE_BETWEEN_FIELDS * 2);

            DrawHorizontalLine();
        }

        private void DrawAccountTools()
        {
            GUILayout.Space(SPACE_BETWEEN_FIELDS);
            GUILayout.BeginHorizontal();
            GUILayout.Label(_accountTokenLabel, GUILayout.Width(LABEL_WIDTH));
            const int tokenCharsToShowFromEachEnd = 4;
            const int tokenTotalCharsToShow = 2 * tokenCharsToShowFromEachEnd;
            var tokenDisplay = Settings.accountToken.Length <= tokenTotalCharsToShow ? Settings.accountToken : Settings.accountToken[..tokenCharsToShowFromEachEnd] + new string('*', Settings.accountToken.Length - tokenTotalCharsToShow) + Settings.accountToken.Substring(Settings.accountToken.Length - tokenCharsToShowFromEachEnd, tokenCharsToShowFromEachEnd);
            GUILayout.Label(tokenDisplay);
            GUILayout.EndHorizontal();

            GUILayout.Space(SPACE_BETWEEN_FIELDS);
            GUILayout.BeginHorizontal();
            GUILayout.Label(ACCOUNT_STATUS_LABEL, GUILayout.Width(LABEL_WIDTH));
            GUILayout.Label(ACCOUNT_STATUS_TEXT);
            GUILayout.EndHorizontal();

            GUILayout.Space(SPACE_BETWEEN_FIELDS * 3);
            GUILayout.BeginHorizontal();
            var isTitleSelectedAutomatically = Settings.isTitleSelectedAutomatically;

            if (isTitleSelectedAutomatically)
            {
                GUI.enabled = false;
            }

            var names = SwAccountUtils.TitlesList?.Select(game => game.name).ToArray();
            Settings.selectedGameIndex = EditorGUILayout.Popup(Settings.selectedGameIndex, names, GUILayout.Width(LABEL_WIDTH));

            if (isTitleSelectedAutomatically)
            {
                GUI.enabled = true;

                if (GUILayout.Button(ACCOUNT_RETRIEVE_CONFIGURATION, GUILayout.Width(LABEL_WIDTH)))
                {
                    SwAccountUtils.FetchTitles();
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button(ACCOUNT_RETRIEVE_CONFIGURATION, GUILayout.Width(LABEL_WIDTH)))
                {
                    if (SwAccountUtils.IsSelectedTitleDummy)
                    {
                        SwEditorAlerts.AlertError(SwEditorConstants.UI.PLEASE_CHOOSE_TITLE, (int)SwErrors.ESettings.ChooseTitle, SwEditorConstants.UI.ButtonTitle.OK);

                        return;
                    }
                    
                    OnTitleSelected();
                    SaveToSettings();
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(ACCOUNT_REFRESH_GAME_LIST_LABEL, GUILayout.Width(LABEL_WIDTH)))
                {
                    SwAccountUtils.FetchTitles();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(SPACE_BETWEEN_FIELDS);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(ACCOUNT_LOGOUT_LABEL, GUILayout.Width(80)))
            {
                SwAccountUtils.Logout(Settings);
                DrawContent();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(SPACE_BETWEEN_FIELDS * 2);

            DrawHorizontalLine();
        }

        #endregion
    }
}