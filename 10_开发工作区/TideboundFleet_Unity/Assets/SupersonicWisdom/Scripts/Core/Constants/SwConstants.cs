using System;
using System.IO;

namespace SupersonicWisdomSDK
{
    public static class SwConstants
    {
        #region --- Members ---
        
        internal const string INTERNAL_KEY_SW_PREFIX = "sw_";
        public const string INSTALL_DATE_FORMAT = "yyyy-MM-dd";
        public const string SORTABLE_DATE_STRING_FORMAT = "yyyy'-'MM'-'dd";
        public const string GAME_OBJECT_NAME = "SupersonicWisdom";
        public const int DEFAULT_REQUEST_TIMEOUT = 10;
        public const string FEATURE = "";
        public const long FEATURE_VERSION = 0;
        public const string BUILD_NUMBER = "1747";
        public const string GIT_COMMIT = "f38e0c5";

        public const string SDK_VERSION = "8.0.12";
        public const string SETTINGS_RESOURCE_PATH = "SupersonicWisdom/Settings";
        public const string SUPERSONIC_WISDOM_SCRIPTS_PATH = "Assets/SupersonicWisdom/Scripts";
        public const string EXTRACTED_RESOURCES_DIR_NAME = "Extracted";
        public const string CRASHLYTICS_DEPENDENCIES_FILE_PATH = "Firebase/Editor/";
        public const string CRASHLYTICS_DEPENDENCIES_FILE_NAME = "CrashlyticsDependencies.xml";
        public const string FIREBASE_VERSION_TEXT_FILE_NAME = "FirebaseUnityWrapperVersion";
        public const string IRONSOURCE_EDITOR_FOLDER = "LevelPlay/Editor/";
        public const string IRONSOURCE_ADAPTER_VERSIONS_CACHE_FILENAME = "IronSourceAdapterVersions";
        internal const string PREFS_ENCRYPTION_KEY = "F9cB6vPmK1zN3sR8uW3jH8kL4vN9tB6w";
        internal const string PREFS_ENCRYPTION_IV = "W5jH2kL4vN9tB6wE";
        
        private const string KNOWLEDGE_CENTER_ARTICLE_URL = "https://support.supersonic.com/hc/en-us/articles/";
        public const string REWARDED_VIDEO_HELP_URL = KNOWLEDGE_CENTER_ARTICLE_URL + "9945166026525-Wisdom-SDK-Integration-Guide-Stage-2";
        
        public static readonly long SdkVersionId = SwUtils.System.ComputeVersionId(SDK_VERSION);

        public const string APP_ICON_RESOURCE_NAME = "AppIcon";
        public static readonly string AppIconResourcesPath = Path.Combine("Extracted", APP_ICON_RESOURCE_NAME);
        
        #endregion
    }
}
