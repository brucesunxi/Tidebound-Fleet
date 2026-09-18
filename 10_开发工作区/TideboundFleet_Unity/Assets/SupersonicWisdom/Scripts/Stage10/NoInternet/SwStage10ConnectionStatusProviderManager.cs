#if SW_STAGE_STAGE10_OR_ABOVE

using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    internal class SwStage10ConnectionStatusProviderManager : ISwStage10ConnectionStatusProvider
    {
        #region --- Members ---

        private readonly List<ISwStage10ConnectionStatusListener> _internetStatusListener;

        #endregion


        #region --- Properties ---

        public SwConnectionStatusDto ConnectionStatus { get; private set; }

        #endregion


        #region --- Construction ---

        public SwStage10ConnectionStatusProviderManager(ISwNativeApi nativeApi, SwCoreNativeAdapter nativeAdapter)
        {
            _internetStatusListener = new List<ISwStage10ConnectionStatusListener>();
            
            ConnectionStatus = new SwConnectionStatusDto();
            
            nativeApi.AddConnectivityCallbacks(OnConnectionStatusChange);
            
            if (nativeAdapter != null)
            {
                nativeAdapter.NativeSDKInitializedEvent += () => InitializeConnectionStatus(nativeApi);
            }
        }

        #endregion


        #region --- Public Methods ---

        public void AddListeners(List<ISwStage10ConnectionStatusListener> listeners)
        {
            _internetStatusListener.AddRange(listeners);
        }

        public void RemoveListeners(List<ISwStage10ConnectionStatusListener> listeners)
        {
            foreach (var listener in listeners)
            {
                _internetStatusListener.Remove(listener);
            }
        }

        #endregion


        #region --- Private Methods ---

        private void InitializeConnectionStatus(ISwNativeApi nativeApi)
        {
            var initialConnectionStatus = nativeApi.GetConnectionStatus();

            if (!initialConnectionStatus.SwIsNullOrEmpty())
            {
                OnConnectionStatusChanged(initialConnectionStatus, true);
            }
            else
            {
                SwInfra.Logger.LogWarning(EWisdomLogType.InternetStatus, "Initial connection status is null or empty");
            }
        }

        private void OnConnectionStatusChange(string connectionStatus)
        {
            OnConnectionStatusChanged(connectionStatus, false);
        }
        
        private void OnConnectionStatusChanged(string connectionStatus, bool isInit)
        {
            var newConnectionStatus = SwUtils.JsonHandler.DeserializeObject<SwConnectionStatusDto>(connectionStatus);
            var didStatusChanged = ConnectionStatus != newConnectionStatus;
            
            ConnectionStatus = newConnectionStatus;
            
            SwInfra.Logger.Log(EWisdomLogType.InternetStatus, $"isAvailable = {ConnectionStatus.isAvailable} | isFlightMode = {ConnectionStatus.isFlightMode}");

            if (isInit) return;
            
            foreach (var listener in _internetStatusListener)
            {
                listener.OnConnectionStatusChanged(ConnectionStatus, didStatusChanged);
            }
        }

#if !UNITY_IOS && !UNITY_ANDROID || UNITY_EDITOR
        //Note: Connection status native
        //This function is to Mock connection test 
        public void OnConnectionStatusChange(bool isAvailable)
        { 
            const string IS_AVAILABLE = "isAvailable";
            
            OnConnectionStatusChanged("{\"" + IS_AVAILABLE + "\":" + isAvailable.ToString().ToLower() + "}", false);
        }
#endif

        #endregion
    }
}
#endif