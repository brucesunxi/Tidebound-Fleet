using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    public static class SwPlatformCommunication
    {
        #region --- Constants ---

        private const string AUTHORIZATION_HEADER_NAME = "authorization";
        private const string API_KEY_HEADER_NAME = "X-API-KEY";
        private const string SDK_VER_HEADER_NAME = "X-WISDOM-SDK-VERSION";
        private const string SDK_VER_ID_HEADER_NAME = "X-WISDOM-SDK-VERSION-ID";

        #endregion


        #region --- Public Methods ---

        public static Dictionary<string, string> CreateAuthorizationHeadersDictionary(string token)
        {
            var headers = string.IsNullOrWhiteSpace(token) ? new Dictionary<string, string>() : new Dictionary<string, string>
            {
                { AUTHORIZATION_HEADER_NAME, "Bearer " + token },
            };

            return AddSdkVersionHeaders(headers);
        }

        public static Dictionary<string, string> CreateApiTokenHeadersDictionary(string token)
        {
            var headers = string.IsNullOrWhiteSpace(token) ? new Dictionary<string, string>() : new Dictionary<string, string>
            {
                { API_KEY_HEADER_NAME, token },
            };

            return AddSdkVersionHeaders(headers);
        }

        #endregion


        #region --- Private Methods ---

        private static Dictionary<string, string> AddSdkVersionHeaders(Dictionary<string, string> headers)
        {
            headers.TryAdd(SDK_VER_HEADER_NAME, SwConstants.SDK_VERSION.SwToString());
            headers.TryAdd(SDK_VER_ID_HEADER_NAME, SwConstants.SdkVersionId.SwToString());

            return headers;
        }

        #endregion


        #region --- Inner Classes ---

        internal static class URLs
        {
            #region --- Constants ---

            internal const string BASE_PARTNERS_V2 = BASE_PARTNERS_DOMAIN + V2;
            internal const string USERS_ME = PLATFORM_ADMIN_URL + "users/me";

            private const string URL_SCHEMA = "https://";
            private const string URL_PARTNERS_PREFIX = "partners";
            private const string URL_ADMIN_PREFIX = "admin";
            private const string PLATFORM_BASE_URL = ".super-api.supersonic.com/";

            private const string BASE_PARTNERS_DOMAIN = URL_SCHEMA + URL_PARTNERS_PREFIX + PLATFORM_BASE_URL;
            private const string BASE_ADMIN_DOMAIN = URL_SCHEMA + URL_ADMIN_PREFIX + PLATFORM_BASE_URL;

            private const string V1 = "v1/";
            private const string V2 = "v2/";
            private const string BASE_ADMIN_V1 = BASE_ADMIN_DOMAIN + V1;

            private const string PLATFORM_ADMIN_URL = BASE_ADMIN_V1 + URL_ADMIN_PREFIX + "/";

            #endregion
        }

        #endregion
    }
}