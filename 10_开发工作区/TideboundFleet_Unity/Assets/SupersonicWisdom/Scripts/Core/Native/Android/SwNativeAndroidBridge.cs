using System;
using System.Collections;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal class SwNativeAndroidBridge : SwNativeBridge
    {
        #region --- Constants ---

        //Fields
        public const string CURRENT_ACTIVITY_FIELD = "currentActivity";
        private const string DESTROY_METHOD = "destroy";
        private const string EVENT_METADATA_DTO_CLASS = "wisdom.library.domain.events.dto.EventMetadataDto";
        private const string EXTRA_EVENT_DETAILS_DTO_CLASS = "wisdom.library.domain.events.dto.ExtraEventDetailsDto";
        private const string GET_ADVERTISING_IDENTIFIER_METHOD = "getAdvertisingIdentifier";
        private const string GET_APP_SET_IDENTIFIER_METHOD = "getAppSetIdentifier";
        private const string GET_CONNECTION_STATUS_METHOD = "getConnectionStatus";
        private const string GET_APP_INSTALL_SOURCE_METHOD = "getAppInstallSource";
        private const string INITIALIZE_SESSION_METHOD = "initializeSession";

        //Methods
        private const string GET_SHARED_PREFERENCES = "getSharedPreferences";
        private const string GET_DEFAULT_SHARED_PREFERENCES_METHOD = "getDefaultSharedPreferences";
        private const string INIT_METHOD = "init";
        public const string GET_APP_CONTEXT_METHOD = "getApplicationContext";
        private const string REGISTER_INIT_LISTENER_METHOD = "registerInitListener";
        private const string REGISTER_SESSION_LISTENER_METHOD = "registerSessionListener";
        private const string REGISTER_WEB_REQUEST_LISTENER_METHOD = "registerWebRequestListener";
        private const string REGISTER_CONNECTIVITY_LISTENER_METHOD = "registerConnectivityListener";
        private const string SET_EVENTS_METADATA_METHOD = "setEventsMetadata";
        private const string SW_BLOCKING_LOADER_RESOURCE_RELATIVE_PATH_FIELD = "blockingLoaderResourceRelativePath";
        private const string SW_BLOCKING_LOADER_VIEWPORT_PERCENTAGE_FIELD = "blockingLoaderViewportPercentage";
        private const string SW_CONNECT_TIMEOUT_FIELD = "connectTimeout";
        private const string SW_INITIAL_SYNC_INTERVAL_FIELD = "initialSyncInterval";
        private const string SW_IS_LOGGING_ENABLED = "isLoggingEnabled";
        private const string SW_READ_TIMEOUT_FIELD = "readTimeout";
        private const string SW_STREAMING_ASSETS_FOLDER_PATH_FIELD = "streamingAssetsFolderPath";
        private const string SW_SUBDOMAIN_FIELD = "subdomain";
        private const string TOGGLE_BLOCKING_LOADER_METHOD = "toggleBlockingLoader";
        private const string REQUEST_RATE_US_POPUP_METHOD = "requestRateUsPopup";
        private const string TRACK_EVENT_METHOD = "trackEvent";
        private const string SEND_REQUEST_METHOD = "sendRequest";
        private const string GET_MEGA_SESSION_ID_METHOD = "getMegaSessionId";
        private const string OPEN_APPLICATION_METHOD = "openApplication";
        private const string GET_SCREEN_WIDTH = "getScreenWidth";
        private const string GET_SCREEN_HEIGHT = "getScreenHeight";
        private const string CAPTURE_SESSION_BACKGROUND_TIME_METHOD = "captureSessionBackgroundTime";
        private const string RELEASE_SESSION_BACKGROUND_TIME_METHOD = "releaseSessionBackgroundTime";
        
        //Classes
        private const string UNITY_PLAYER_CLASS = "com.unity3d.player.UnityPlayer";
        private const string PREFERENCE_MANAGER_CLASS = "android.preference.PreferenceManager";
        private const string UNREGISTER_INIT_LISTENER_METHOD = "unregisterInitListener";
        private const string UNREGISTER_SESSION_LISTENER_METHOD = "unregisterSessionListener";
        private const string UNREGISTER_WEB_REQUEST_LISTENER_METHOD = "unregisterWebRequestListener";
        private const string UNREGISTER_CONNECTIVITY_LISTENER_METHOD = "unregisterConnectivityListener";
        private const string UPDATE_EVENTS_METADATA_METHOD = "updateEventsMetadata";
        private const string UPDATE_WISDOM_CONFIGURATION_METHOD = "updateWisdomConfiguration";
        private const string WISDOM_CONFIGURATION_CLASS = "wisdom.library.api.dto.WisdomConfigurationDto";
        private const string WISDOM_SDK_CLASS = "wisdom.library.api.WisdomSDK";
        private const string APP_UPDATE_WRAPPER_CLASS = "wisdom.library.store.SwAppUpdateManagerWrapper";

        #endregion


        #region --- Members ---

        private readonly AndroidJavaObject _nativeSdk = new AndroidJavaClass(WISDOM_SDK_CLASS);
        private readonly SwNativeAndroidInitListener _initListener;
        private readonly SwNativeAndroidSessionListener _sessionListener;
        private readonly SwNativeAndroidWebRequestListener _webRequestListener;
        private readonly SwNativeAndroidConnectivityListener _connectivityListener;
        protected readonly AndroidJavaObject _appUpdateManagerWrapper;

        #endregion


        #region --- Construction ---

        public SwNativeAndroidBridge ()
        {
            _sessionListener = new SwNativeAndroidSessionListener();
            _initListener = new SwNativeAndroidInitListener();
            _webRequestListener = new SwNativeAndroidWebRequestListener();
            _connectivityListener = new SwNativeAndroidConnectivityListener();
            _appUpdateManagerWrapper = new AndroidJavaObject(APP_UPDATE_WRAPPER_CLASS, GetCurrentActivity());
        }

        #endregion


        #region --- Public Methods ---

        public override void Destroy()
        {
            _nativeSdk.CallStatic(DESTROY_METHOD);
        }

        public override string GetAdvertisingId()
        {
            var advertisingId = _nativeSdk.CallStatic<string>(GET_ADVERTISING_IDENTIFIER_METHOD);

            return advertisingId;
        }

        public override string GetOrganizationAdvertisingId()
        {
            var appSetId = _nativeSdk.CallStatic<string>(GET_APP_SET_IDENTIFIER_METHOD);

            return appSetId;
        }
        
        public override string GetConnectionStatus()
        {
            var connectionStatus = _nativeSdk.CallStatic<string>(GET_CONNECTION_STATUS_METHOD);

            return connectionStatus;
        }

        public override string GetMegaSessionId()
        {
            return _nativeSdk.CallStatic<string>(GET_MEGA_SESSION_ID_METHOD);
        }

        public override string GetAppInstallSource()
        {
            return _nativeSdk.CallStatic<string>(GET_APP_INSTALL_SOURCE_METHOD);
        }

        public override void InitializeSession(SwEventMetadataDto metadata)
        {
            _nativeSdk.CallStatic(INITIALIZE_SESSION_METHOD, SwUtils.JsonHandler.SerializeObject(metadata));
        }

        public override IEnumerator InitSdk(SwNativeConfig configuration)
        {
            var didFinishInit = false;
            _initListener.OnInitEnded += () => { didFinishInit = true; };

            _nativeSdk.CallStatic(REGISTER_INIT_LISTENER_METHOD, _initListener);
            _nativeSdk.CallStatic(INIT_METHOD, GetCurrentActivity(), CreateWisdomConfig(configuration));

            while (!didFinishInit)
            {
                yield return null;
            }

            RegisterSessionListener();
            RegisterWebRequestListener();
            RegisterConnectivityListener();

            _nativeSdk.CallStatic(UNREGISTER_INIT_LISTENER_METHOD, _initListener);
        }

        public override void RegisterSessionEndedCallback(OnSessionEnded callback)
        {
            _sessionListener.OnSessionEndedEvent += callback;
        }

        public override void RegisterWebRequestListener(OnWebResponse callback)
        {
            _webRequestListener.OnWebResponse += callback;
        }
        
        public override void RegisterConnectivityStatusChanged(OnConnectivityStatusChanged callback)
        {
            _connectivityListener.ConnectivityStatusChangedEvent += callback;
        }

        public override void SendRequest(string requestJsonString)
        {
            _nativeSdk.CallStatic(SEND_REQUEST_METHOD, requestJsonString);
        }

        public override void RegisterSessionStartedCallback(OnSessionStarted callback)
        {
            _sessionListener.OnSessionStartedEvent += callback;
        }
        
        public override void RegisterGetAdditionalDataJsonMethod(GetAdditionalDataJsonMethod callback)
        {
            _sessionListener.GetAdditionalDataJsonMethodEvent += callback;
        }

        public override void SetEventMetadata(SwEventMetadataDto metadata)
        {
            _nativeSdk.CallStatic(SET_EVENTS_METADATA_METHOD, SwUtils.JsonHandler.SerializeObject(metadata));
        }

        public override bool ToggleBlockingLoader(bool shouldPresent)
        {
            return _nativeSdk.CallStatic<bool>(TOGGLE_BLOCKING_LOADER_METHOD, shouldPresent);
        }

        public override void TrackEvent(string eventName, string customsJson, string extraJson)
        {
            _nativeSdk.CallStatic(TRACK_EVENT_METHOD, eventName, customsJson, extraJson);
        }

        public override void UnregisterSessionEndedCallback(OnSessionEnded callback)
        {
            _sessionListener.OnSessionEndedEvent -= callback;
        }

        public override void UnregisterSessionStartedCallback(OnSessionStarted callback)
        {
            _sessionListener.OnSessionStartedEvent -= callback;
        }
        
        public override void UnregisterGetAdditionalDataJsonMethod(GetAdditionalDataJsonMethod callback)
        {
            _sessionListener.GetAdditionalDataJsonMethodEvent -= callback;
        }

        public override void UnregisterWebRequestListener(OnWebResponse callback)
        {
            _webRequestListener.OnWebResponse -= callback;
        }
        
        public override void UnregisterConnectivityStatusChanged(OnConnectivityStatusChanged callback)
        {
            _connectivityListener.ConnectivityStatusChangedEvent -= callback;
        }

        public override void UpdateEventMetadata(SwEventMetadataDto metadata)
        {
            _nativeSdk.CallStatic(UPDATE_EVENTS_METADATA_METHOD, SwUtils.JsonHandler.SerializeObject(metadata));
        }

        public override void UpdateWisdomConfiguration(SwNativeConfig configuration)
        {
            _nativeSdk.CallStatic(UPDATE_WISDOM_CONFIGURATION_METHOD, CreateWisdomConfig(configuration));
        }

        public override void RequestRateUsPopup()
        {
            _nativeSdk.CallStatic(REQUEST_RATE_US_POPUP_METHOD);
        }
        
        public override bool OpenApplication(string appIdentifier)
        {
            SwInfra.Logger.Log(EWisdomLogType.Native, "android OpenApplication");
            return _nativeSdk.CallStatic<bool>(OPEN_APPLICATION_METHOD, appIdentifier);
        }

        public override float GetScreenWidth()
        {
            return _nativeSdk.CallStatic<float>(GET_SCREEN_WIDTH);
        }

        public override float GetScreenHeight()
        {
            return _nativeSdk.CallStatic<float>(GET_SCREEN_HEIGHT);
        }

        public override float GetScreenScaleFactor()
        {
            return 1f;
        }
        
        public override DateTime ReleaseSessionBackgroundTime()
        {
            var releaseTime = _nativeSdk.CallStatic<long>(RELEASE_SESSION_BACKGROUND_TIME_METHOD);
            return new DateTime(1970, 1, 1).AddSeconds(releaseTime).ToUniversalTime();
        }

        public override DateTime CaptureSessionBackgroundTime()
        {
            var captureTime = _nativeSdk.CallStatic<long>(CAPTURE_SESSION_BACKGROUND_TIME_METHOD);
            return new DateTime(1970, 1, 1).AddSeconds(captureTime).ToUniversalTime();
        }

        #endregion


        #region --- Private Methods ---

        private static AndroidJavaObject CreateWisdomConfig(SwNativeConfig config)
        {
            var readTimeoutMillis = config.ReadTimeout * 1000;
            var connectTimeoutMillis = config.ConnectTimeout * 1000;
            var initialSyncIntervalMillis = config.InitialSyncInterval * 1000;

            var nativeConfig = new AndroidJavaObject(WISDOM_CONFIGURATION_CLASS);
            nativeConfig.Set(SW_IS_LOGGING_ENABLED, config.IsLoggingEnabled);
            nativeConfig.Set(SW_SUBDOMAIN_FIELD, config.Subdomain);
            nativeConfig.Set(SW_READ_TIMEOUT_FIELD, readTimeoutMillis);
            nativeConfig.Set(SW_CONNECT_TIMEOUT_FIELD, connectTimeoutMillis);
            nativeConfig.Set(SW_INITIAL_SYNC_INTERVAL_FIELD, initialSyncIntervalMillis);
            nativeConfig.Set(SW_STREAMING_ASSETS_FOLDER_PATH_FIELD, config.StreamingAssetsFolderPath);
            nativeConfig.Set(SW_BLOCKING_LOADER_RESOURCE_RELATIVE_PATH_FIELD, config.BlockingLoaderResourceRelativePath);
            nativeConfig.Set(SW_BLOCKING_LOADER_VIEWPORT_PERCENTAGE_FIELD, config.BlockingLoaderViewportPercentage);

            return nativeConfig;
        }

        private static AndroidJavaObject GetApplicationContext()
        {
            return GetCurrentActivity()
                .Call<AndroidJavaObject>("getApplicationContext");
        }
        
        public static AndroidJavaObject GetDefaultSharedPreferences()
        {
            var preferenceManagerClass = new AndroidJavaClass(PREFERENCE_MANAGER_CLASS);
            
            return preferenceManagerClass.CallStatic<AndroidJavaObject>(GET_DEFAULT_SHARED_PREFERENCES_METHOD, GetApplicationContext());
        }

        public static AndroidJavaObject GetCurrentActivity()
        {
            using var unityPlayer = new AndroidJavaClass(UNITY_PLAYER_CLASS);

            return unityPlayer.GetStatic<AndroidJavaObject>(CURRENT_ACTIVITY_FIELD);
        }

        private void RegisterSessionListener ()
        {
            _nativeSdk.CallStatic(REGISTER_SESSION_LISTENER_METHOD, _sessionListener);
        }

        private void UnregisterSessionListener ()
        {
            _nativeSdk.CallStatic(UNREGISTER_SESSION_LISTENER_METHOD, _sessionListener);
        }

        private void RegisterWebRequestListener ()
        {
            _nativeSdk.CallStatic(REGISTER_WEB_REQUEST_LISTENER_METHOD, _webRequestListener);
        }

        private void UnregisterWebRequestListener ()
        {
            _nativeSdk.CallStatic(UNREGISTER_WEB_REQUEST_LISTENER_METHOD, _webRequestListener);
        }

        private void RegisterConnectivityListener()
        {
            _nativeSdk.CallStatic(REGISTER_CONNECTIVITY_LISTENER_METHOD, _connectivityListener); 
        }
        
        private void UnregisterConnectivityListener()
        {
            _nativeSdk.CallStatic(UNREGISTER_CONNECTIVITY_LISTENER_METHOD, _connectivityListener);
        }

        #endregion
    }
}