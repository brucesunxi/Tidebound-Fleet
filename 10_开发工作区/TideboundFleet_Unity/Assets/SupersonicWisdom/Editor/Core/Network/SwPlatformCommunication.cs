using System.Collections.Generic;

namespace SupersonicWisdomSDK.Editor
{
    internal static class SwPlatformCommunication
    {
        #region --- Public Methods ---

        public static Dictionary<string, string> CreateAuthorizationHeadersDictionary()
        {
            return SupersonicWisdomSDK.SwPlatformCommunication.CreateApiTokenHeadersDictionary(SwAccountUtils.AccountToken);
        }

        #endregion


        #region --- Inner Classes ---

        internal static class URLs
        {
            #region --- Constants ---
            
            private const string BASE_WISDOM = SupersonicWisdomSDK.SwPlatformCommunication.URLs.BASE_PARTNERS_V2 + "wisdom/";
            internal const string TITLE = BASE_WISDOM + "title";
            internal const string CURRENT_STAGE_API = BASE_WISDOM + "current-stage";
            internal const string DOWNLOAD_WISDOM_PACKAGE = BASE_WISDOM + "download-package";
            internal const string WISDOM_PACKAGE_MANIFEST = BASE_WISDOM + "package-manifest";

            #endregion
        }

        #endregion
    }
}