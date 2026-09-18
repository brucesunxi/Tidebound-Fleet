#if SW_STAGE_STAGE10_OR_ABOVE
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SupersonicWisdomSDK
{
    internal class SwStage10ProgressionTester : MonoBehaviour
    {
        #region --- Inspector ---
        
        [Header("Buttons")]
        [SerializeField] private Button completeButton;
        [SerializeField] private Button failButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button metaStartButton;
        [SerializeField] private Button metaEndButton;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button continueButton;

        [Header("Toggles")]
        [SerializeField] private TMP_Dropdown levelTypeDropdown;
        
        #endregion
        
        
        #region --- Properties ---
        
        public long Level
        {
            get { return _level; }
            set { _level = value; }
        }
        
        public bool InMeta
        {
            get { return _inMeta; }
        }
        
        public bool DidFinishLevel
        {
            get { return _didFinishLevel; }
        }
        
        public bool DidCompleteLevel
        {
            get { return _didCompleteLevel; }
        }
        
        #endregion
        
        
        #region --- Members ---
        
        private long _level;
        private bool _inMeta;
        private bool _isReady;
        private bool _isTimeBased;
        private bool _didFinishLevel;
        private bool _didCompleteLevel;
        private ESwLevelType _levelType;
        
        #endregion
        
        
        #region --- Mono Override ---
        
        private void Awake()
        {
            if (SupersonicWisdom.Api.IsReady())
            {
                OnSwReady();
            }
            else
            {
                SupersonicWisdom.Api.AddOnReadyListener(OnSwReady);
            }
        }

        private void OnEnable()
        {
            levelTypeDropdown.onValueChanged.AddListener(OnLevelTypeChanged);
            ConnectButtonListeners();
            SetMetaStartButtonIntractable(true);
            SetMetaEndButtonIntractable(false);
        }

        #endregion
        
        
        #region --- Private Methods ---
        
        private void OnSwReady()
        {
            _isReady = true;
            _isTimeBased = SupersonicWisdom.Api.GetSettings().isTimeBased;
        }
        
        private void OnLevelTypeChanged(int value)
        {
            _levelType = (ESwLevelType)value;
        }

        private void SetMetaStartButtonIntractable(bool intractable)
        {
            metaStartButton.interactable = intractable;
        }

        private void SetMetaEndButtonIntractable(bool intractable)
        {
            metaEndButton.interactable = intractable;
        }
        
        private void ConnectButtonListeners()
        {
            completeButton.onClick.RemoveListener(OnLevelCompleteClicked);
            completeButton.onClick.AddListener(OnLevelCompleteClicked);
            failButton.onClick.RemoveListener(OnLevelFailClicked);
            failButton.onClick.AddListener(OnLevelFailClicked);
            skipButton.onClick.RemoveListener(OnLevelSkipClicked);
            skipButton.onClick.AddListener(OnLevelSkipClicked);
            startButton.onClick.RemoveListener(OnLevelStartClicked);
            startButton.onClick.AddListener(OnLevelStartClicked);
            metaStartButton.onClick.RemoveListener(OnMetaStartedClick);
            metaStartButton.onClick.AddListener(OnMetaStartedClick);
            metaEndButton.onClick.RemoveListener(OnMetaEndedClick);
            metaEndButton.onClick.AddListener(OnMetaEndedClick);
            reviveButton.onClick.RemoveListener(OnLevelReviveClicked);
            reviveButton.onClick.AddListener(OnLevelReviveClicked);
            continueButton.onClick.RemoveListener(OnLevelContinueClicked);
            continueButton.onClick.AddListener(OnLevelContinueClicked);
        }
        
        #endregion
        
        
        #region --- Button Methods ---

        public void OnLevelStartClicked()
        {
            if (_level == 0 || _didCompleteLevel)
            {
                _level++;
            }

            _didCompleteLevel = _didFinishLevel = false;

            if (_isTimeBased)
            {
                SupersonicWisdom.Api.NotifyTimeBasedGameStarted(() => { });
            }
            else
            {
                SupersonicWisdom.Api.NotifyLevelStarted(_levelType, _level, () => { });
            }
        }
        
        public void OnLevelContinueClicked()
        {
            _didFinishLevel = _didCompleteLevel = false;
            SupersonicWisdom.Api.NotifyLevelContinued(_levelType, _level, () => { });
        }
        
        public void OnLevelCompleteClicked()
        {
            _didCompleteLevel = _didFinishLevel = true;
            SupersonicWisdom.Api.NotifyLevelCompleted(_levelType, _level, () => { });
        }

        public void OnLevelFailClicked()
        {
            _didFinishLevel = true;
            SupersonicWisdom.Api.NotifyLevelFailed(_levelType, _level, () => { });
        }

        public void OnLevelReviveClicked()
        {
            _didFinishLevel = _didCompleteLevel = false;
            SupersonicWisdom.Api.NotifyLevelRevived(_levelType, _level, () => { });
        }
        
        public void OnLevelSkipClicked()
        {
            _didFinishLevel = _didCompleteLevel = true;
            SupersonicWisdom.Api.NotifyLevelSkipped(_levelType, _level, () => { });
        }
        
        public void OnMetaStartedClick()
        {
            SupersonicWisdom.Api.NotifyMetaStarted(() => { });
            SetMetaStartButtonIntractable(false);
            SetMetaEndButtonIntractable(true);
            _inMeta = true;
        }

        public void OnMetaEndedClick()
        {
            SupersonicWisdom.Api.NotifyMetaEnded(() => { });
            SetMetaStartButtonIntractable(true);
            SetMetaEndButtonIntractable(false);
            _inMeta = false;
            _didFinishLevel = true;
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public void SetCompleteButtonIntractable(bool intractable)
        {
            completeButton.interactable = intractable;
        }
        
        public void SetFailButtonIntractable(bool intractable)
        {
            failButton.interactable = intractable;
        }
        
        public void SetSkipButtonIntractable(bool intractable)
        {
            skipButton.interactable = intractable;
        }
        
        public void SetContinueButtonIntractable(bool intractable)
        {
            continueButton.interactable = intractable;
        }
        
        public void SetStartButtonIntractable(bool intractable)
        {
            startButton.interactable = intractable;
        }
        
        public void SetReviveButtonIntractable(bool intractable)
        {
            reviveButton.interactable = intractable;
        }
        
        #endregion
    }
}
#endif