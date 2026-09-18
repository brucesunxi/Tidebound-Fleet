#if SW_STAGE_STAGE10_OR_ABOVE
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK
{
    internal abstract class SwBaseAppUpdateManager : ISwAppUpdateManager, ISwTacSystemListener
    {
        #region --- Constants ---
        
        private const int BUTTON_TEXT_LIMIT = 15;
        
        #endregion
        
        
        #region --- Members ---
        
        protected readonly SwUiToolkitManager _uiToolkitManager;
        private readonly SwCoreTracker _tracker;
        protected ISwAppUpdateValidator _appUpdateValidator;
        protected ISwAppUpdateExecutor _appUpdateExecutor;
        private bool _appUpdateCheckComplete;
        
        #endregion
        
        
        #region --- Events ---
        
        internal event Action<ISwAppUpdateEvent> NativeUpdatePopupShownEvent;
        internal event Action<ISwAppUpdateEvent> CustomUpdatePopupShownEvent;
        internal event Action<ISwAppUpdateEvent> CustomUpdatePopupUpdateSelectedEvent;
        internal event Action<ISwAppUpdateEvent> CustomUpdatePopupUpdateSkippedEvent;
        
        #endregion
        
        
        #region --- Properties ---

        public bool DidPerformAction { get; set; }

        public ESwActionType ActionType
        {
            get { return ESwActionType.ShowAppUpdate; }
        }

        public bool IsUiActionType
        {
            get { return true; }
        }
        
        public Tuple<EConfigListenerType, EConfigListenerType> ListenerType {
            get
            {
                return new Tuple<EConfigListenerType, EConfigListenerType>
                (EConfigListenerType.FinishWaitingForRemote,
                    EConfigListenerType.EndOfGame);
            }
        }
        
        private bool IsFeatureEnabled
        {
            get { return AppUpdaterConfig?.updatePopupType != ESwAppUpdatePopupType.None; }
        }
        
        private bool IsWisdomUpdatePopupEnabled
        {
            get { return AppUpdaterConfig.shouldShowWisdomPopup; }
        }

        public SwAppUpdaterConfig AppUpdaterConfig { get; private set; }
        public SwVisualElementPayload[] UiToolkitWindowPayload { get; private set; }

        public SwLocalConfig GetLocalConfig()
        {
            return new SwAppUpdateLocalConfig();
        }

        #endregion
        
        
        #region --- Construction ---

        protected SwBaseAppUpdateManager(SwUiToolkitManager uiToolkitManager, SwCoreTracker tracker)
        {
            _uiToolkitManager = uiToolkitManager;
            _tracker = tracker;
            SubscribeToAppUpdateEvents();
            SwInfra.TacSystem.AddListeners(this);
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public IEnumerator TryPerformAction(Dictionary<string, object> metaData)
        {
            yield return TryToUpdateApp();
            
            DidPerformAction = true;
        }
        
        public void OnConfigResolved(ISwCoreInternalConfig swConfigAccessor, ISwConfigManagerState state)
        {
            AppUpdaterConfig = new SwAppUpdaterConfig
            {
                shouldShowWisdomPopup = swConfigAccessor.GetValue(SwAppUpdateLocalConfig.APP_UPDATE_WISDOM_POPUP_ENABLED_KEY, SwAppUpdateLocalConfig.APP_UPDATE_WISDOM_POPUP_ENABLED_VALUE),
                customUpdatePopupText = swConfigAccessor.GetValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TEXT_KEY, SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TEXT_VALUE),
                customUpdatePopupTitle = swConfigAccessor.GetValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TITLE_KEY, SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TITLE_VALUE),
                customUpdatePopupButtonText = swConfigAccessor.GetValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_BUTTON_TEXT_KEY, SwAppUpdateLocalConfig.APP_UPDATE_POPUP_BUTTON_TEXT_VALUE),
                nativePopupType = SetNativePopupType(swConfigAccessor.GetValue(SwAppUpdateLocalConfig.APP_UPDATE_NATIVE_POPUP_TYPE_KEY, SwAppUpdateLocalConfig.APP_UPDATE_NATIVE_POPUP_TYPE_VALUE)),
                updatePopupType = SetCustomPopupType(swConfigAccessor.GetValue(SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TYPE_KEY, SwAppUpdateLocalConfig.APP_UPDATE_POPUP_TYPE_VALUE)),
            };

            if (!IsFeatureEnabled)
            {
                SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "App update is not required for this app");
                return;
            }
            
            SetUiToolkitWindowPayload(AppUpdaterConfig.customUpdatePopupTitle, AppUpdaterConfig.customUpdatePopupText, AppUpdaterConfig.customUpdatePopupButtonText);
            
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "App update config resolved successfully");
        }
        
        public virtual IEnumerator TryToUpdateApp()
        {
            if (AppUpdaterConfig == null)
            {
                SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, "App update config is not resolved");
                yield break;    
            }
            
            if (!IsFeatureEnabled)
            {
                SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "Update popup is disabled");
                yield break;
            }
            
            if (IsWisdomUpdatePopupEnabled)
            {
                yield return CheckAndPromptForCustomAppUpdate();
                yield break;
            }
            
#if UNITY_IOS
            // iOS doesn't have native popup, so we only show the native popup
            // The custom popup will be shown in Android when the user approves the update by clicking on the custom popup
            yield break;
#endif
            
            yield return StartNativeUpdateFlow(AppUpdaterConfig.updatePopupType, AppUpdaterConfig.nativePopupType);
            
            _appUpdateCheckComplete = true;
        }

        #endregion

        
        #region --- Private Methods ---
        
        protected virtual IEnumerator CheckAndPromptForCustomAppUpdate()
        {
            yield return _appUpdateValidator.CheckForUpdate();
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "Check for update complete. Is update available: " + _appUpdateValidator.IsUpdateAvailable);
            
            if (_appUpdateValidator.IsUpdateAvailable)
            {
                yield return ShowCustomUpdatePopup();
            }
            
            _appUpdateCheckComplete = true;
        }
        
        protected virtual IEnumerator StartNativeUpdateFlow(ESwAppUpdatePopupType customPopupType,
            ESwAppUpdateNativePopupType nativePopupSubType = ESwAppUpdateNativePopupType.Permode)
        {
            yield return _appUpdateExecutor.InitiateAppUpdate(nativePopupSubType);

            if (customPopupType != ESwAppUpdatePopupType.Suggest) yield break;
            
            _uiToolkitManager.CloseWindow(ESwUiToolkitType.AppUpdate); // Suggest update will perform a background update, in this case close the popup and let the player play.
            SwUtils.Ui.UnlockUI();
        }
        
        protected IEnumerator ShowCustomUpdatePopup()
        {
            if (!IsWisdomUpdatePopupEnabled) yield break;
            
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "Showing custom update popup");
            yield return _uiToolkitManager.OpenWindow(ESwUiToolkitType.AppUpdate, null, () =>
            {
                CustomUpdatePopupShownEvent?.Invoke(new SwAppUpdateEventArgs.CustomUpdatePopupShownEvent
                {
                    eventValue = $"{AppUpdaterConfig.updatePopupType}",
                });
                
                SetupUserInteractionsWithCustomPopup();
                
            }, UiToolkitWindowPayload);
        }

        private void SetupUserInteractionsWithCustomPopup()
        {
            SwUtils.Ui.LockUI();
            
            _uiToolkitManager.UiDocument.rootVisualElement.Q<Button>(SwAppUpdateUiToolkitWindow.UPDATE_BUTTON_VE_NAME).clicked += () =>
            {
                CustomUpdatePopupUpdateSelectedEvent?.Invoke(new SwAppUpdateEventArgs.CustomUpdatePopupUpdateSelectedEvent
                {
                    eventValue = ISwAppUpdateEvent.UPDATE_PRESSED_EVENT_VALUE,
                });
            };
            _uiToolkitManager.UiDocument.rootVisualElement.Q<Button>(SwAppUpdateUiToolkitWindow.SKIP_BUTTON_VE_NAME).clicked += () =>
            {
                CustomUpdatePopupUpdateSkippedEvent?.Invoke(new SwAppUpdateEventArgs.CustomUpdatePopupUpdateSkippedEvent
                {
                    eventValue = ISwAppUpdateEvent.UPDATE_CLOSE_EVENT_VALUE,
                });
                _uiToolkitManager.CloseWindow(ESwUiToolkitType.AppUpdate);
                SwUtils.Ui.UnlockUI();
            };
        }

        private static ESwAppUpdateNativePopupType SetNativePopupType(string nativePopupType)
        {
            if (Enum.TryParse(nativePopupType, out ESwAppUpdateNativePopupType nativePopupSubType))
            {
                return nativePopupSubType;
            }
            
            SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, $"Failed to parse native update popup type: {nativePopupType}");
            
            return ESwAppUpdateNativePopupType.Permode;
        }
        
        private static ESwAppUpdatePopupType SetCustomPopupType(string customUpdatePopupType)
        {
            if (Enum.TryParse(customUpdatePopupType, out ESwAppUpdatePopupType customPopupType))
            {
                return customPopupType;
            }
            
            SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, $"Failed to parse custom update popup type: {customUpdatePopupType}");
            
            return ESwAppUpdatePopupType.None;
        }
        
        private void SetUiToolkitWindowPayload(string title, string message, string buttonText)
        {
            UiToolkitWindowPayload = new SwVisualElementPayload[]
            {
                new SwVisualElementPayload
                {
                    Name = SwAppUpdateUiToolkitWindow.TITLE_VE_NAME,
                    Text = title,
                },
                new SwVisualElementPayload
                {
                    Name = SwAppUpdateUiToolkitWindow.BODY_VE_NAME,
                    Text = message,
                },
                new SwVisualElementPayload
                {
                    Name = SwAppUpdateUiToolkitWindow.UPDATE_BUTTON_VE_NAME,
                    Text = buttonText.SelectFirstXCharacters(BUTTON_TEXT_LIMIT),
                },
                new SwVisualElementPayload
                {
                    Name = SwAppUpdateUiToolkitWindow.SKIP_BUTTON_VE_NAME,
                    Text = AppUpdaterConfig.updatePopupType == ESwAppUpdatePopupType.Force ? string.Empty : SwAppUpdateUiToolkitWindow.NO_THANKS_TEXT,
                },
            };
        }
        
        private void SubscribeToAppUpdateEvents()
        {
            CustomUpdatePopupUpdateSelectedEvent -= TrackEvent;
            CustomUpdatePopupUpdateSelectedEvent += TrackEvent;
            CustomUpdatePopupUpdateSkippedEvent -= TrackEvent;
            CustomUpdatePopupUpdateSkippedEvent += TrackEvent;
            NativeUpdatePopupShownEvent -= TrackEvent;
            NativeUpdatePopupShownEvent += TrackEvent;
            CustomUpdatePopupShownEvent -= TrackEvent;
            CustomUpdatePopupShownEvent += TrackEvent;
        }
        
        private void TrackEvent(ISwAppUpdateEvent appUpdateEvent)
        {
            if (appUpdateEvent != null)
            {
                _tracker.TrackEvent(appUpdateEvent.eventName, appUpdateEvent.eventValue);
            }
            else
            {
                SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, "App update event is null");
            }
        }
        
        protected void OnNativeUpdatePopupShownEvent(ISwAppUpdateEvent obj)
        {
            NativeUpdatePopupShownEvent?.Invoke(obj);
        }
        
        #endregion
    }
}
#endif