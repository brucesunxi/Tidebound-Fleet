#if SW_STAGE_STAGE10_OR_ABOVE

namespace SupersonicWisdomSDK
{
    internal class SwStage10UserData : SwCoreUserData, ISwStage10ConfigListener, ISwSessionListener
    {
        #region --- Properties ---
        
        public string AppsFlyerId { get; set; }
        
        #endregion
        
        
        #region --- Construction ---
        
        public SwStage10UserData(SwSettings settings, ISwAdvertisingIdsGetter idsGetter, SwUserActiveDay activeDay) : base(settings, idsGetter, activeDay) { }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public virtual void OnSessionEnded(string sessionId)
        {
            SwInfra.Logger.Log(EWisdomLogType.Session, $"Unity:SwSessions:OnSessionEnded:{sessionId}");
        }
        
        public virtual void OnSessionStarted(string sessionId)
        {
            UpdateUserStateOnStartSession(sessionId);
            SwInfra.Logger.Log(EWisdomLogType.Session, $"Unity:SwSessions:OnSessionStarted:{sessionId}");
        }
        
        
        public void OnConfigResolved(ISwStage10InternalConfig swConfigAccessor, ISwConfigManagerState state)
        {
            if (swConfigAccessor?.Agent != null)
            {
                Country = swConfigAccessor.Agent.country;
            }
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private void UpdateUserStateOnStartSession(string sessionId)
        {
            ModifyUserStateSync(mutableUserState =>
            {
                UpdateAge(mutableUserState);
                mutableUserState.SessionId = sessionId;
                mutableUserState.todaySessionsCount++;
                mutableUserState.totalSessionsCount++;
            });
            
            AfterUserStateChangeInternal(SwUserStateChangeReason.SessionStart, true);
        }
        
        #endregion
    }
}

#endif