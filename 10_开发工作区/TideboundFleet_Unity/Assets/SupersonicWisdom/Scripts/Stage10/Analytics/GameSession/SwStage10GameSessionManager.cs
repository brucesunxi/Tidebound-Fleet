#if SW_STAGE_STAGE10_OR_ABOVE
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal class SwStage10GameSessionManager : ISwStage10ConfigListener, ISwScriptLifecycleListener, ISwReadyEventListener, ISwLocalConfigProvider, ISwTrackerDataProvider
    {
        #region --- Constants ---
        
        internal const string BRUTTO_DURATION = "gameSessionDurationBrutto";
        internal const string NETTO_DURATION = "gameSessionDurationNetto";
        protected const string REPORT_SESSION_FLAG_WISDOM = "WISDOM";
        private const string GAME_SESSION_ID = "gameSessionId";
        private const string CHECK_SESSION_CLOSURE = "CheckSessionClosure";
        private const string REPORT_SESSION_FLAG_USER = "USER";
        private const float SAVE_PLAYTIME_INTERVAL = 10f;
        
        #endregion
        
        
        #region --- Members ---
        
        private readonly List<ISwGameSessionListener> _sessionListeners;
        private static string _cachedSessionID;
        private float _timeoutIntervalSeconds;
        private TimeSpan _timeoutInterval;
        private bool _sdkReady;
        private DateTime? _lastPauseTime;
        private SwCrossSessionTimer _nettoPlaytimeTimer;
        protected SwCrossSessionTimer _bruttoPlaytimeTimer;
        
        #endregion


        #region --- Properties ---
        
        protected static string SessionID
        {
            get
            {
                if (_cachedSessionID.SwIsNullOrEmpty())
                {
                    _cachedSessionID = SwInfra.KeyValueStore.GetString(GAME_SESSION_ID);
                }
                
                return _cachedSessionID;
            }
            private set
            {
                _cachedSessionID = value;
                SwInfra.KeyValueStore.SetString(GAME_SESSION_ID, value).Save();
            }
        }

        private static bool IsSessionClosureCheckEnabled
        {
            get { return SwInfra.KeyValueStore.GetBoolean(CHECK_SESSION_CLOSURE); }
            set { SwInfra.KeyValueStore.SetBoolean(CHECK_SESSION_CLOSURE, value).Save(); }
        }

        public Tuple<EConfigListenerType, EConfigListenerType> ListenerType
        {
            get { return new Tuple<EConfigListenerType, EConfigListenerType>(EConfigListenerType.FinishWaitingForRemote, EConfigListenerType.FinishWaitingForRemote); }
        }
        
        protected int BruttoSessionTime
        {
            get
            {
                if(_bruttoPlaytimeTimer == null)
                {
                    return 0;
                }

                return (int)_bruttoPlaytimeTimer.Elapsed;
            }
        }

        protected int NettoSessionTime
        {
            get
            {
                if(_nettoPlaytimeTimer == null)
                {
                    return 0;
                }

                return (int)_nettoPlaytimeTimer.Elapsed;
            }
        }

        #endregion


        #region --- Construction ---
        
        public SwStage10GameSessionManager(ISwMonoBehaviour mono, IEnumerable<ISwGameSessionListener> sessionListeners)
        {
            mono.ApplicationFocusEvent += OnApplicationFocus;
            _sessionListeners = new List<ISwGameSessionListener>(sessionListeners);
            CreateTimers(mono);
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private void CreateTimers(ISwMonoBehaviour mono)
        {
            _bruttoPlaytimeTimer = CreateTimer(mono, BRUTTO_DURATION, true);
            _nettoPlaytimeTimer = CreateTimer(mono, NETTO_DURATION, true);
        }
        
        private static SwCrossSessionTimer CreateTimer(ISwMonoBehaviour mono, string timerName, bool pauseWhenUnityOutOfFocus)
        {
            return SwCrossSessionTimer.Create(mono.GameObject, timerName, Mathf.Infinity, pauseWhenUnityOutOfFocus, SAVE_PLAYTIME_INTERVAL);
        }

        private void ReportSession(string flag, ESessionStatus sessionStatus)
        {
            NotifySessionListeners(flag, sessionStatus);
            LogSession("Game session duration brutto", BruttoSessionTime, flag);
            LogSession("Game session duration netto", NettoSessionTime, flag);
            NotifyThirdParties(flag, sessionStatus);
        }
        
        protected virtual void NotifyThirdParties(string flag, ESessionStatus sessionStatus) { }

        private static void LogSession(string message, int elapsedTime, string flag)
        {
            SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, {message}: {elapsedTime}, flag: {flag}");
        }
        
        private void NotifySessionListeners(string flag, ESessionStatus sessionStatus)
        {
            if (_sessionListeners.SwIsNullOrEmpty()) return;
            
            try
            {
                if (sessionStatus == ESessionStatus.Started)
                {
                    _sessionListeners.ForEach(listener =>
                        listener.OnSessionStarted(SessionID, flag, 0, 0));
                }
                else
                {
                    _sessionListeners.ForEach(listener =>
                        listener.OnSessionEnded(SessionID, flag, BruttoSessionTime, NettoSessionTime));
                }
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogException(e, EWisdomLogType.GameSession, "Failed to notify session listeners");
            }
        }

        private void RestartTimers()
        {
            SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, Timers restart");
            _bruttoPlaytimeTimer.StartTimer();
            _nettoPlaytimeTimer.StartTimer();
        }

        private void ReportAndResetSession(string flag)
        {
            LogSession("Game session ended", 0, flag);
            ReportSession(flag, ESessionStatus.Ended);
            RestartTimers();
            GenerateAndSaveNewSessionId();
            OpenSession();
        }

        private void GenerateAndSaveNewSessionId()
        {
            SessionID = Guid.NewGuid().ToString();
        }
        
        private void OpenSession()
        {
            GenerateAndSaveNewSessionId();
            RestartTimers();
            ReportSession(REPORT_SESSION_FLAG_WISDOM, ESessionStatus.Started);
        }
        
        protected void CloseSession(string flag)
        {
            ReportAndResetSession(flag);
        }
        
        protected virtual void AttemptToCloseSession(string flag)
        {
            CloseSession(flag);
        }

        #endregion
        
        
        #region --- Interface Methods ---

        public void OnConfigResolved(ISwStage10InternalConfig swConfigAccessor, ISwConfigManagerState state)
        {
            var timeoutInterval = swConfigAccessor.GetValue(SwGameSessionManagerLocalConfig.GAME_SESSION_END_INTERVAL_KEY, SwGameSessionManagerLocalConfig.GAME_SESSION_END_INTERVAL_VALUE);
            _timeoutIntervalSeconds = SwDateAndTimeUtils.GetSecondsFromMinutes(timeoutInterval);
            _timeoutInterval = TimeSpan.FromSeconds(_timeoutIntervalSeconds);
        }
        
        public SwLocalConfig GetLocalConfig()
        {
            return new SwGameSessionManagerLocalConfig();
        }
        
        public void OnSwReady()
        {
            _sdkReady = true;
            
            if (IsSessionClosureCheckEnabled)
            {
                ReportSession(REPORT_SESSION_FLAG_USER, ESessionStatus.Ended);
                RestartTimers();
            }

            OpenSession();
        }
        
        public void OnApplicationPause(bool isPaused)
        {
            if (!_sdkReady) return; // For some reason this method is called on app launch, before the SDK is ready
            
            SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, Application paused: {isPaused}");

            if (isPaused)
            {
                IsSessionClosureCheckEnabled = true;
                _lastPauseTime = DateTime.UtcNow;
                SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, App paused at {_lastPauseTime}");
            }
            else
            {
                IsSessionClosureCheckEnabled = false;
                
                if (_lastPauseTime.HasValue)
                {
                    var elapsed = DateTime.UtcNow - _lastPauseTime.Value;
                    SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, App paused after {elapsed}, timeout interval: {_timeoutInterval}");
                    
                    if (elapsed > _timeoutInterval)
                    {
                        AttemptToCloseSession(REPORT_SESSION_FLAG_WISDOM);
                    }
                }
                
                _lastPauseTime = null;
                SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, App resumed");
            }
        }
        
        // Lost focus, save the session in case of abrupt closure
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!_sdkReady) return; // For some reason this method is called on app launch, before the SDK is ready
            
            SwInfra.Logger.Log(EWisdomLogType.GameSession, $"Session ID: {SessionID}, Application focus: {hasFocus}");
            
            if (!hasFocus)
            {
                IsSessionClosureCheckEnabled = true;
            }
        }

        public (SwJsonDictionary dataDictionary, IEnumerable<string> keysToEncrypt) ConditionallyAddExtraDataToTrackEvent(SwCoreUserData coreUserData, string eventName = "")
        {
            var extraDataToSendTracker = new SwJsonDictionary
            {
                { GAME_SESSION_ID, SessionID },
            };
            
            return (extraDataToSendTracker, KeysToEncrypt());
            
            IEnumerable<string> KeysToEncrypt()
            {
                yield break;
            }
        }

        public void OnApplicationQuit() { }
        
        public void OnAwake() { }
        
        public void OnStart() { }
        
        public void OnUpdate() { }
        
        #endregion
        
        
        #region --- Internal Enum ---
        
        protected enum ESessionStatus
        {
            Started,
            Ended,
        }
        
        #endregion
    }
}
#endif
