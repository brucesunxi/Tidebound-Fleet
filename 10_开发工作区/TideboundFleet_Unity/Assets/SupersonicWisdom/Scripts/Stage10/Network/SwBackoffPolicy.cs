#if SW_STAGE_STAGE10_OR_ABOVE

using System.Collections.Generic;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal class SwBackoffPolicy : ISwStage10ConnectionStatusListener
    {
        #region --- Constants ---

        private const float MAX_RETRY_DELAY_IN_SECONDS = 60;
        private const float BASE_MULTIPLAYER = 2;

        #endregion


        #region --- Events ---

        internal delegate void BackoffPolicyIteration(bool stillTrying,int currentRetryCount);

        internal delegate bool CheckIfInitializationSucceed();

        #endregion


        #region --- Members ---

        private readonly int _numberOfRetries;
        private readonly int _timeBetweenRetries;
        private readonly string _policyName;
        
        private readonly ISwStage10ConnectionStatusProvider _connectionStatusProviderManager;
        private readonly BackoffPolicyIteration _policyCallback;
        private readonly MonoBehaviour _mono;
        
        private int _currentRetryCount;
        
        private SwTimer _retryCooldownTimer;

        #endregion


        #region --- Properties ---

        private bool IsInitialized
        {
            get
            {
                SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | Checking if BackoffPolicy is initialized.");

                if (!OnCheckIfInitializationSucceed())
                {
                    SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | BackoffPolicy is no initialized, keep trying");
                    return false;
                }

                ReleasePolicy();
                
                return true;
            }
        }

        private CheckIfInitializationSucceed OnCheckIfInitializationSucceed { get; }

        #endregion


        #region --- Construction ---

        public SwBackoffPolicy(string policyName, SwCoreMonoBehaviour mono, ISwStage10ConnectionStatusProvider connectionStatusProviderManager, int numberOfRetries, CheckIfInitializationSucceed checkIfInitializationSucceed, BackoffPolicyIteration backoffPolicyIteration)
        {
            _mono = mono;
            _currentRetryCount = 0;
            _numberOfRetries = numberOfRetries;
            _policyName = policyName;
            _policyCallback = backoffPolicyIteration;
            _connectionStatusProviderManager = connectionStatusProviderManager;
            _connectionStatusProviderManager.AddListeners( new List<ISwStage10ConnectionStatusListener> { this });
            
            OnCheckIfInitializationSucceed = checkIfInitializationSucceed;
            
            TryStartCooldownTimer();
        }

        #endregion


        #region --- Private Methods ---
        
        public void OnConnectionStatusChanged(SwConnectionStatusDto statusDto, bool didStatusChanged)
        {
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | Internet connection status changed {statusDto.isAvailable}");
            
            if (!statusDto.isAvailable) return;
            
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | New internet connection status is: {statusDto.isAvailable}");
            
            TryStartCooldownTimer();
        }

        private void TryStartCooldownTimer()
        {
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | Retry initialize");
            
            if (_retryCooldownTimer && (_retryCooldownTimer.IsEnabled || _numberOfRetries <= -1) || !_connectionStatusProviderManager.ConnectionStatus.isAvailable) return;
            
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | NumberOfTimeToRetry: {_numberOfRetries}");
            
            ReinitializeCooldownTimer();
        }

        private void ReinitializeCooldownTimer()
        {
            var delay = CalculateDelay(_currentRetryCount);
            
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | Creating BackoffPolicy timer with delay of: {delay}");

            TryReinitialize();
            
            _retryCooldownTimer = SwTimer.Create(_mono.gameObject, $"{_policyName} + BackoffPolicy", delay, true);
            _retryCooldownTimer.OnFinishEvent += TryReinitializeCooldownTimer;
            _retryCooldownTimer.StartTimer();
        }

        private void LogInitializationAttempt(int tryCount)
        {
            var logMessage = tryCount == 0 ? 
                $"{_policyName} | NumOfRetries is set to: {_numberOfRetries}" : 
                $"{_policyName} | Retry number: {tryCount}";
            
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, logMessage);
        }

        private float CalculateDelay(int tryCount)
        {
            var delay = Mathf.Pow(BASE_MULTIPLAYER, tryCount);
            
            return Mathf.Min(delay, MAX_RETRY_DELAY_IN_SECONDS);
        }

        private void TryReinitialize()
        {
            if (IsInitialized) return;
            
            _currentRetryCount++;
            LogInitializationAttempt(_currentRetryCount);
            
            if (_numberOfRetries == 0 || _currentRetryCount < _numberOfRetries)
            {
                _policyCallback.Invoke(true, _currentRetryCount);
            }
            else
            {
                _policyCallback.Invoke(false, _currentRetryCount);
            }
        }
        
        private void ReleasePolicy()
        {
            _retryCooldownTimer.StopTimer();
            _retryCooldownTimer.OnFinishEvent -= TryReinitializeCooldownTimer;
            _connectionStatusProviderManager.RemoveListeners( new List<ISwStage10ConnectionStatusListener> { this } );
            _retryCooldownTimer = null;
            
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | Released BackoffPolicy timer");
        }

        #endregion


        #region --- Event Handler ---

        private void TryReinitializeCooldownTimer()
        {
            if (IsInitialized) return;
            
            _retryCooldownTimer.StopTimer();
            
            SwInfra.Logger.Log(EWisdomLogType.BackoffPolicy, $"{_policyName} | Restart BackoffPolicy timer");
            
            ReinitializeCooldownTimer();
        }

        #endregion
    }
}

#endif