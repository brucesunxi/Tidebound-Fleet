using System;
using System.Collections.Generic;
using JetBrains.Annotations;
#if DEVELOPMENT_BUILD
using UnityEngine;
#endif

namespace SupersonicWisdomSDK
{
    internal class SwGameStateSystem
    {
        #region --- Constants ---

        private const string UNAUTHORIZED_GAME_TRANSITION = "unauthorized_game_transition";
        private const string TIME_BASED_GAME_STARTED = "time_based_game_started";
        private const string TRIGGER_FORMAT = "level_{0}";

        #endregion


        #region --- Events ---

        internal delegate void SwProgressionCallback(ESwLevelType swLevelType, long level, string customString, long attempts, long revives);

        public event Action OnTimeBasedGameStartedEvent;
        public event SwProgressionCallback OnLevelCompletedEvent;
        public event SwProgressionCallback OnLevelFailedEvent;
        public event SwProgressionCallback OnLevelRevivedEvent;
        public event SwProgressionCallback OnLevelSkippedEvent;
        public event SwProgressionCallback OnLevelStartedEvent;
        public event SwProgressionCallback OnLevelContinuedEvent;
        public event Action<string> OnMetaStartedEvent;
        public event Action<string> OnMetaEndedEvent;

        #endregion
        
        
        #region --- Private Variables ---

        private SwSystemState.EGameState _currentGameState = SwSystemState.EGameState.Loading;
        private SwSystemState.EStateEvent _currentStateEvent = SwSystemState.EStateEvent.Completed;
        private SwSystemState.EStateEvent _previousStateExitReason = SwSystemState.EStateEvent.Completed;
        private List<ISwGameStateSystemListener> _gameStateListeners;
        private List<ISwGameProgressionListener> _gameProgressionListeners;
        private readonly SwCoreUserData _coreUserData;
        private readonly SwGameStateSystemDataModule _gameStateSystemDataModule;

        #endregion


        #region --- Public Properties ---
        
        public SwSystemState.EGameState CurrentGameState => _currentGameState;
        public SwSystemState.EGameState PreviousGameState { get; private set; }
        
        public bool IsDuringLevel => UserState.isDuringLevel;
		
        public bool IsPlaytime
        {
            get { return CurrentGameState != SwSystemState.EGameState.Loading && CurrentGameState != SwSystemState.EGameState.BetweenLevels; }
        }

        #endregion


        #region --- Private Properties ---
        
        private SwUserState UserState { get { return _coreUserData?.ImmutableUserState(); } }

        #endregion
        
        
        #region --- Constructor ---

        internal SwGameStateSystem(SwCoreUserData coreUserData)
        {
            _coreUserData = coreUserData;
            _gameStateSystemDataModule = new SwGameStateSystemDataModule(_coreUserData);
        }

        #endregion


        #region --- Public Methods ---

        public void AddGameStateListeners(IEnumerable<ISwGameStateSystemListener> listeners)
        {
            _gameStateListeners ??= new List<ISwGameStateSystemListener>();
            _gameStateListeners.AddRange(listeners);
        }
        
        public void AddGameProgressionListeners(IEnumerable<ISwGameProgressionListener> gameProgressionListeners)
        {
            _gameProgressionListeners ??= new List<ISwGameProgressionListener>();
            _gameProgressionListeners.AddRange(gameProgressionListeners);

            foreach (var gameProgressionListener in _gameProgressionListeners)
            {
                OnTimeBasedGameStartedEvent -= gameProgressionListener.OnTimeBasedGameStarted;
                OnTimeBasedGameStartedEvent += gameProgressionListener.OnTimeBasedGameStarted;
                OnLevelStartedEvent -= gameProgressionListener.OnLevelStarted;
                OnLevelStartedEvent += gameProgressionListener.OnLevelStarted;
                OnLevelCompletedEvent -= gameProgressionListener.OnLevelCompleted;
                OnLevelCompletedEvent += gameProgressionListener.OnLevelCompleted;
                OnLevelFailedEvent -= gameProgressionListener.OnLevelFailed;
                OnLevelFailedEvent += gameProgressionListener.OnLevelFailed;
                OnLevelSkippedEvent -= gameProgressionListener.OnLevelSkipped;
                OnLevelSkippedEvent += gameProgressionListener.OnLevelSkipped;
                OnLevelRevivedEvent -= gameProgressionListener.OnLevelRevived;
                OnLevelRevivedEvent += gameProgressionListener.OnLevelRevived;
                OnMetaStartedEvent -= gameProgressionListener.OnMetaStarted;
                OnMetaStartedEvent += gameProgressionListener.OnMetaStarted;
                OnLevelContinuedEvent -= gameProgressionListener.OnLevelContinued;
                OnLevelContinuedEvent += gameProgressionListener.OnLevelContinued;
                OnMetaEndedEvent -= gameProgressionListener.OnMetaEnded;
                OnMetaEndedEvent += gameProgressionListener.OnMetaEnded;
            }
        }

        public void ProcessTimeBasedGameStarted() 
        {
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs
                {
                    StateEndStatusRequest = SwSystemState.EStateEvent.Completed,
                    NewGameState = SwSystemState.EGameState.Time,
                },
                nameof(ProcessTimeBasedGameStarted));
            
            if (transitionRequest)
            {
                try
                {
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        IsDuringLevel = true,
                    };
                    
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);
                    
                    NotifyGameStateListeners();
                    OnTimeBasedGameStartedEvent?.Invoke();
                    
                    SwInfra.TacSystem.InternalFireTriggers(TIME_BASED_GAME_STARTED);
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessTimeBasedGameStarted)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.TimeBasedGameStart, null, null);
            }
        }

        public void ProcessLevelCompleted(ESwLevelType levelType, long level, string customString)
        {
            if (ConvertLevelTypeToGameState(levelType) != _currentGameState)
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelCompleted, customString, levelType, level);
                return;
            }
            
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Completed,
                NewGameState = SwSystemState.EGameState.BetweenLevels,
            }, nameof(ProcessLevelCompleted));

            if (transitionRequest)
            {
                try
                {
                    var userStateCopy = UserState;
                    
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        CompletedLevels = levelType == ESwLevelType.Regular ? level : (long?)null,
                        CompletedBonusLevels = levelType == ESwLevelType.Bonus ? level : (long?)null,
                        CompletedTutorialLevels = levelType == ESwLevelType.Tutorial ? level : (long?)null,
                        PlayedLevels = userStateCopy.playedLevels + 1,
                        IsDuringLevel = false,
                        ConsecutiveFailedLevels = 0,
                        ConsecutiveCompletedLevels = userStateCopy.consecutiveCompletedLevels + 1,
                        LevelAttempts = 0,
                        LastLevelStarted = userStateCopy.lastLevelStarted,
                    };
                    
                    _gameStateSystemDataModule?.SetPreviousLevelData(levelType, level);
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);
                    
                    NotifyGameStateListeners();
                    OnLevelCompletedEvent?.Invoke(levelType, level, customString, userStateCopy.levelAttempts, userStateCopy.levelRevives);
                    
                    SwInfra.TacSystem.InternalFireTriggers(GetTriggers(SwSystemState.EStateEvent.Completed));
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessLevelCompleted)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelCompleted, customString, levelType, level);
            }
        }

        public void ProcessLevelFailed(ESwLevelType levelType, long level, string customString)
        {
            if (ConvertLevelTypeToGameState(levelType) != _currentGameState)
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelFailed, customString, levelType, level);
                return;
            }
            
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Failed,
                NewGameState = SwSystemState.EGameState.BetweenLevels,
            }, nameof(ProcessLevelFailed));

            if (transitionRequest)
            {
                try
                {
                    var userStateCopy = UserState;
                    
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        PlayedLevels = userStateCopy.playedLevels + 1,
                        ConsecutiveFailedLevels = userStateCopy.consecutiveFailedLevels + 1,
                        ConsecutiveCompletedLevels = 0,
                        IsDuringLevel = false,
                    };
                    
                    _gameStateSystemDataModule?.SetPreviousLevelData(levelType, level);
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);
                    
                    NotifyGameStateListeners();
                    OnLevelFailedEvent?.Invoke(levelType, level, customString, userStateCopy.levelAttempts, userStateCopy.levelRevives);
                    
                    SwInfra.TacSystem.InternalFireTriggers(GetTriggers(SwSystemState.EStateEvent.Failed));
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessLevelFailed)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelFailed, customString, levelType, level);
            }
        }

        public void ProcessLevelRevived(ESwLevelType levelType, long level, string customString)
        {
            if (_currentGameState != SwSystemState.EGameState.BetweenLevels || 
                _previousStateExitReason != SwSystemState.EStateEvent.Failed && 
                (PreviousGameState != SwSystemState.EGameState.Meta || _previousStateExitReason != SwSystemState.EStateEvent.Completed))
            {
                return;
            }

            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs 
            { 
                StateEndStatusRequest = SwSystemState.EStateEvent.Revived, 
                NewGameState = PreviousGameState, 
            }, nameof(ProcessLevelRevived));

            if (transitionRequest)
            {
                try
                {
                    var userStateCopy = UserState;
                    
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        LevelRevives = userStateCopy.levelRevives + 1,
                        IsDuringLevel = true,
                    };
                    
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);
                    
                    NotifyGameStateListeners();
                    OnLevelRevivedEvent?.Invoke(levelType, level, customString, userStateCopy.levelAttempts, userStateCopy.levelRevives);
                    
                    SwInfra.TacSystem.InternalFireTriggers(GetTriggers(SwSystemState.EStateEvent.Revived));
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessLevelRevived)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelRevived, customString, levelType, level);
            }
        }

        public void ProcessLevelSkipped(ESwLevelType levelType, long level, string customString)
        {
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Skipped,
                NewGameState = SwSystemState.EGameState.BetweenLevels,
            }, nameof(ProcessLevelSkipped));
            
            if (transitionRequest)
            {
                try
                {
                    var userStateCopy = UserState;
                    
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        CompletedLevels = levelType == ESwLevelType.Regular ? level : (long?)null,
                        CompletedBonusLevels = levelType == ESwLevelType.Bonus ? level : (long?)null,
                        CompletedTutorialLevels = levelType == ESwLevelType.Tutorial ? level : (long?)null,
                        LevelAttempts = 0,
                        ConsecutiveCompletedLevels = userStateCopy.consecutiveCompletedLevels + 1,
                        IsDuringLevel = false,
                    };
                    
                    _gameStateSystemDataModule?.SetPreviousLevelData(levelType, level);
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);
                    
                    NotifyGameStateListeners();
                    OnLevelSkippedEvent?.Invoke(levelType, level, customString, userStateCopy.levelAttempts, userStateCopy.levelRevives);
                    
                    SwInfra.TacSystem.InternalFireTriggers(GetTriggers(SwSystemState.EStateEvent.Skipped));
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessLevelSkipped)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelSkipped, customString, levelType, level);
            }
        }

        public void ProcessLevelStarted(ESwLevelType levelType, long level, string customString)
        {
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Started,
                NewGameState = ConvertLevelTypeToGameState(levelType),
            }, nameof(ProcessLevelStarted));
            
            if (transitionRequest)
            {
                try
                {
                    var userStateCopy = UserState;
                    
                    var previousLevelData = _gameStateSystemDataModule?.GetPreviousLevelData();
                    
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        LevelRevives = 0,
                        LevelAttempts = userStateCopy.levelAttempts + 1,
                        IsDuringLevel = true,
                        LastLevelStarted = levelType == ESwLevelType.Regular ? level : (long?)null,
                        LastBonusLevelStarted = levelType == ESwLevelType.Bonus ? level : (long?)null,
                        LastTutorialLevelStarted = levelType == ESwLevelType.Tutorial ? level : (long?)null,
                        PreviousLevelType = previousLevelData?.Item1,
                        CurrentLevelType = levelType,
                        PreviousLevelTypeNumber = previousLevelData?.Item2,
                    };
                    
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);

                    NotifyGameStateListeners();
                    OnLevelStartedEvent?.Invoke(levelType, level, customString, userStateCopy.levelAttempts, userStateCopy.levelRevives);
                    
                    SwInfra.TacSystem.InternalFireTriggers(GetTriggers(SwSystemState.EStateEvent.Started));
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessLevelStarted)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelStarted, customString, levelType, level);
            }
        }
        
        public void ProcessLevelContinued(ESwLevelType levelType, long level, string customString)
        {
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Continued,
                NewGameState = ConvertLevelTypeToGameState(levelType),
            }, nameof(ProcessLevelContinued));
            
            if (transitionRequest)
            {
                try
                {
                    var userStateCopy = UserState;
                    
                    var previousLevelData = _gameStateSystemDataModule?.GetPreviousLevelData();
                    
                    var gameStateProgressionData = new SwGameStateProgressionData
                    {
                        LevelRevives = userStateCopy.levelRevives,
                        LevelAttempts = userStateCopy.levelAttempts,
                        IsDuringLevel = true,
                        LastLevelStarted = levelType == ESwLevelType.Regular ? level : (long?)null,
                        LastBonusLevelStarted = levelType == ESwLevelType.Bonus ? level : (long?)null,
                        LastTutorialLevelStarted = levelType == ESwLevelType.Tutorial ? level : (long?)null,
                        PreviousLevelType = previousLevelData?.Item1,
                        PreviousLevelTypeNumber = previousLevelData?.Item2,
                    };
                    
                    _gameStateSystemDataModule?.SaveGameStateChangeInUserState(gameStateProgressionData);

                    NotifyGameStateListeners();
                    OnLevelContinuedEvent?.Invoke(levelType, level, customString, userStateCopy.levelAttempts, userStateCopy.levelRevives);
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessLevelContinued)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.LevelContinued, customString, levelType, level);
            }
        }

        public void ProcessMetaStarted(string customString)
        {
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs()
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Completed,
                NewGameState = SwSystemState.EGameState.Meta,
            }, nameof(ProcessMetaStarted));

            if (transitionRequest)
            {
                try
                {
                    NotifyGameStateListeners();
                    OnMetaStartedEvent?.Invoke(customString);
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessMetaStarted)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.MetaStarted, customString, null);
            }
        }

        public void ProcessMetaEnded(string customString)
        {
            var transitionRequest = OnGameStateTransitionRequested(new SwSystemStateEventArgs()
            {
                StateEndStatusRequest = SwSystemState.EStateEvent.Completed,
                NewGameState = PreviousGameState,
            }, nameof(ProcessMetaEnded));
            
            if (transitionRequest)
            {
                try
                {
                    NotifyGameStateListeners();
                    OnMetaEndedEvent?.Invoke(customString);
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogException(e, EWisdomLogType.GameStateSystem, $"{nameof(ProcessMetaEnded)}");
                }
            }
            else
            {
                HandleUnauthorizedGameTransition(SwProgressEvent.MetaEnded, customString, null);
            }
        }

        #endregion


        #region --- Private Methods ---

        private static string GetTriggers(SwSystemState.EStateEvent stateEvent)
        {
            return TRIGGER_FORMAT.Format(stateEvent);
        }
        
        private bool OnGameStateTransitionRequested(SwSystemStateEventArgs eventArgs, string reason)
        {
            SwInfra.Logger.Log(EWisdomLogType.GameStateSystem, $"Attempting Transition from {_currentGameState} to {eventArgs.NewGameState} | reason: {reason} | end status level: {eventArgs.StateEndStatusRequest}");

            if (!ShouldTransitionOccur(eventArgs))
            {
                SwInfra.Logger.Log(EWisdomLogType.GameStateSystem, $"Transition blocked! | {eventArgs}");
                
                return false;
            }

            SwInfra.Logger.Log(EWisdomLogType.GameStateSystem, $"Successful transition - Current Game State: {_currentGameState}");

            return true;
        }

        private void NotifyGameStateListeners()
        {
            var updatedEventArgs = new SwSystemStateEventArgs()
            {
                NewGameState = _currentGameState,
                PreviousGameState = PreviousGameState,
                ProgressionData = _gameStateSystemDataModule.LatestGameStateProgressionData ?? new SwGameStateProgressionData(),
            };

            if (_gameStateListeners == null || updatedEventArgs.NewGameState == PreviousGameState)
            {
                return;
            }

            foreach (var listener in _gameStateListeners)
            {
                listener.OnGameSystemStateChange(updatedEventArgs);
            }
        }

        private bool ShouldTransitionOccur (SwSystemStateEventArgs systemStateEventArgs)
        {
            var gameStateToMove = systemStateEventArgs.NewGameState;
            var currentStateEvent = systemStateEventArgs.StateEndStatusRequest;

            switch (gameStateToMove)
            {
                case SwSystemState.EGameState.Loading:
                    return HandleLoadingTransitionRequest(currentStateEvent);
                // Handle Level Based Transitions is handled at lower level (Tutorial)
                case SwSystemState.EGameState.Regular:
                case SwSystemState.EGameState.Bonus:
                case SwSystemState.EGameState.Tutorial:
                    return HandleLevelBasedTransitionRequest(gameStateToMove, currentStateEvent);
                case SwSystemState.EGameState.Time:
                    return HandleTimeTransitionRequest(currentStateEvent);
                case SwSystemState.EGameState.Meta:
                    return HandleMetaTransitionRequest(currentStateEvent);
                case SwSystemState.EGameState.BetweenLevels:
                    return HandleBetweenLevelsTransitionRequest(currentStateEvent);
                
                default:
                        return false;
            }
        }

        private bool HandleTimeTransitionRequest(SwSystemState.EStateEvent currentStateEvent)
        {
            if (!CanEnterTimeState(currentStateEvent)) return false;
            
            OfficiateTransition(SwSystemState.EGameState.Time, currentStateEvent);

            return true;
        }

        private bool HandleMetaTransitionRequest( SwSystemState.EStateEvent currentStateEvent)
        {
            if (!CanEnterMetaState(currentStateEvent)) return false;
            
            OfficiateTransition(SwSystemState.EGameState.Meta, currentStateEvent);

            return true;
        }

        private bool HandleBetweenLevelsTransitionRequest( SwSystemState.EStateEvent currentStateEvent)
        {
            if (!CanEnterBetweenLevelsState(currentStateEvent)) return false;
            
            OfficiateTransition(SwSystemState.EGameState.BetweenLevels, currentStateEvent);
                
            return true;
        }

        private bool HandleLevelBasedTransitionRequest(SwSystemState.EGameState gameStateToMove, SwSystemState.EStateEvent currentStateEvent)
        {
            if (!CanEnterLevelsState(currentStateEvent)) return false;
            
            OfficiateTransition(gameStateToMove, currentStateEvent);

            return true;
        }
        
        private bool HandleLoadingTransitionRequest(SwSystemState.EStateEvent currentStateEvent)
        {
            if (IsCompletedEvent(currentStateEvent) && _currentGameState != SwSystemState.EGameState.Meta) return false;
            
            OfficiateTransition(SwSystemState.EGameState.Loading, currentStateEvent);

            return true;
        }

        public static SwSystemState.EGameState ConvertLevelTypeToGameState(ESwLevelType levelType)
        {
            switch (levelType)
            {
                case ESwLevelType.Regular:
                    return SwSystemState.EGameState.Regular;
                case ESwLevelType.Bonus:
                    return SwSystemState.EGameState.Bonus;
                case ESwLevelType.Tutorial:
                    return SwSystemState.EGameState.Tutorial;
                default:
                    return SwSystemState.EGameState.Regular;
            }
        }

        private void OfficiateTransition(SwSystemState.EGameState? gameState, SwSystemState.EStateEvent currentStateEvent)
        {
            PreviousGameState = _currentGameState;
            _currentGameState = gameState ?? _currentGameState;
            _previousStateExitReason = currentStateEvent;
            
            SwInfra.Logger.Log(EWisdomLogType.GameStateSystem, $"{nameof(_currentGameState)}: {_currentGameState} | {nameof(_currentStateEvent)}: {_currentStateEvent} | {nameof(PreviousGameState)}: {PreviousGameState}");
        }

        private bool IsCompletedEvent(SwSystemState.EStateEvent currentStateEvent)
        {
            return currentStateEvent == SwSystemState.EStateEvent.Completed;
        }

        private bool IsEntryToLevelStateEvent(SwSystemState.EStateEvent currentStateEvent)
        {
            return currentStateEvent == SwSystemState.EStateEvent.Started || currentStateEvent == SwSystemState.EStateEvent.Continued;
        }

        private bool CanEnterTimeState(SwSystemState.EStateEvent currentStateEvent)
        {
            var isCurrentStateLoading = _currentGameState == SwSystemState.EGameState.Loading;
            var isCurrentStateMeta = _currentGameState == SwSystemState.EGameState.Meta;
    
            return IsCompletedEvent(currentStateEvent) && isCurrentStateLoading || isCurrentStateMeta;
        }

        private bool CanEnterMetaState(SwSystemState.EStateEvent currentStateEvent)
        {
            var isCurrentStateTime = _currentGameState == SwSystemState.EGameState.Time;
            var isCurrentStateBetweenLevels = _currentGameState == SwSystemState.EGameState.BetweenLevels;
            var isCurrentStateLoading = _currentGameState == SwSystemState.EGameState.Loading;
            var isContinuedState = currentStateEvent == SwSystemState.EStateEvent.Continued;

            return IsCompletedEvent(currentStateEvent) && isCurrentStateTime || isCurrentStateBetweenLevels || isCurrentStateLoading || isContinuedState;
        }

        private bool CanEnterBetweenLevelsState(SwSystemState.EStateEvent currentStateEvent)
        {
            var isEventFailed = currentStateEvent == SwSystemState.EStateEvent.Failed;
            var isEventSkipped = currentStateEvent == SwSystemState.EStateEvent.Skipped;
            var isNotLevelEntryState = !IsEntryToLevelStateEvent(currentStateEvent);
            var isCurrentStateRegular = _currentGameState == SwSystemState.EGameState.Regular;
            var isCurrentStateBonus = _currentGameState == SwSystemState.EGameState.Bonus;
            var isCurrentStateTutorial = _currentGameState == SwSystemState.EGameState.Tutorial;
            var isCurrentStateMeta = _currentGameState == SwSystemState.EGameState.Meta;
            var isCurrentStateBetweenLevels = _currentGameState == SwSystemState.EGameState.BetweenLevels;

            return isNotLevelEntryState && (IsCompletedEvent(currentStateEvent) || isEventFailed || isEventSkipped) && isCurrentStateRegular || isCurrentStateBonus || isCurrentStateTutorial || isCurrentStateMeta || (isCurrentStateBetweenLevels && isEventSkipped);
        }

        private bool CanEnterLevelsState(SwSystemState.EStateEvent currentStateEvent)
        {
            var isCurrentBetweenLevelsAndNotContinue = _currentGameState == SwSystemState.EGameState.BetweenLevels && currentStateEvent != SwSystemState.EStateEvent.Continued;
            var isValidStartTransition = currentStateEvent == SwSystemState.EStateEvent.Continued && _currentStateEvent != SwSystemState.EStateEvent.Started || currentStateEvent == SwSystemState.EStateEvent.Started && _currentStateEvent != SwSystemState.EStateEvent.Continued;
            var isCurrentStateLoading = _currentGameState == SwSystemState.EGameState.Loading;
            var isCurrentStateRevived = currentStateEvent == SwSystemState.EStateEvent.Revived;

            return  isValidStartTransition && isCurrentBetweenLevelsAndNotContinue || isCurrentStateLoading || isCurrentStateRevived;
        }
        
        private void HandleUnauthorizedGameTransition(SwProgressEvent swProgressEvent, [CanBeNull] string customString, ESwLevelType? levelType, long level = 0)
        {
            SwInfra.Logger.Log(EWisdomLogType.GameStateSystem, $"{nameof(SwGameStateSystem)} | {nameof(HandleUnauthorizedGameTransition)} | {nameof(swProgressEvent)}: {swProgressEvent} | {nameof(customString)}: {customString} | {nameof(levelType)}: {levelType} | {nameof(level)}: {level} "
                + $"| {nameof(_currentGameState)}: {_currentGameState} | {nameof(_currentStateEvent)}: {_currentStateEvent} | {nameof(PreviousGameState)}: {PreviousGameState}");
            SwInfra.TacSystem.InternalFireTriggers(UNAUTHORIZED_GAME_TRANSITION);
            
#if DEVELOPMENT_BUILD
            Application.Quit(1);
#endif
        }
        
        #endregion
    }
}