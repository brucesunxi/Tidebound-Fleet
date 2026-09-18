#if SW_STAGE_STAGE10_OR_ABOVE
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK
{
    internal class SwAppUpdateUiToolkitWindow : SwUiToolkitWindow
    {
        #region --- Constants ---
        
        internal const string TITLE_VE_NAME = "Title";
        internal const string BODY_VE_NAME = "Body";
        internal const string UPDATE_BUTTON_VE_NAME = "UpdateButton";
        internal const string SKIP_BUTTON_VE_NAME = "SkipButton";
        internal const string NO_THANKS_TEXT = "No thanks";
        private const string ICON_VE_NAME = "Icon";
        private const string MOVE_FORWARD_CLASS = "move-forward";
        private const string MOVE_BACKWARD_CLASS = "move-backward";
        private const ushort START_TRANSITION_DELAY = 500;
        
        #endregion
        
        
        #region --- Members ---
        
        private VisualElement _rocketIcon;
        
        #endregion
        
        
        #region --- Properties ---

        internal override ESwUiToolkitType Type
        {
            get { return ESwUiToolkitType.AppUpdate; }
        }

        internal override int Priority
        {
            get { return 80; }
        }
        
        internal override bool IsBlockingAds
        {
            get { return true; }
        }

        #endregion


        #region --- Private Methods ---

        protected override void OnDisplay(SwVisualElementPayload[] payload = null)
        {
            ConfigureIconAnimation();
            
            if (payload == null) return;

            foreach (var item in payload)
            {
                switch (item.Name)
                {
                    case TITLE_VE_NAME:
                        SetTitle(item.Text);
                        break;
                    case BODY_VE_NAME:
                        SetDescription(item.Text);
                        break;
                    case UPDATE_BUTTON_VE_NAME:
                        SetupUpdateButton(item.Text);
                        break;
                    case SKIP_BUTTON_VE_NAME:
                        SetupSkipButton(item.Text);
                        break;
                }
            }
        }

        protected override void OnClose()
        {
        }
        
        private void SetTitle(string title)
        {
            var titleContainer = TryToGetVisualElement<VisualElement>(TITLE_VE_NAME);
            titleContainer.Q<Label>().text = title;
        }
        
        private void SetDescription(string description)
        {
            var descriptionContainer = TryToGetVisualElement<VisualElement>(BODY_VE_NAME);
            descriptionContainer.Q<Label>().text = description;
        }
        
        private void SetupUpdateButton(string text)
        {
            var updateButton = TryToGetVisualElement<Button>(UPDATE_BUTTON_VE_NAME);
            updateButton.text = text;
            updateButton.clicked += () =>
            {
                SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "Update button clicked");
            };
        }
        
        private void SetupSkipButton(string text)
        {
            var skipButton = TryToGetVisualElement<Button>(SKIP_BUTTON_VE_NAME);

            if (text.SwIsNullOrEmpty())
            {
                skipButton.style.display = DisplayStyle.None;
                return;
            }
            
            skipButton.text = text;
            skipButton.clicked += () =>
            {
                SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "Skip button clicked");
            };
        }
        
        private void ConfigureIconAnimation()
        {
            _rocketIcon = _uiToolkitManager.UiDocument.rootVisualElement.Q<VisualElement>(ICON_VE_NAME);
            _rocketIcon.RegisterCallback<TransitionEndEvent>(OnTransitionEnded);
            _rocketIcon.schedule.Execute(() => _rocketIcon.AddToClassList(MOVE_FORWARD_CLASS)).StartingIn(START_TRANSITION_DELAY);
        }
        
        private void OnTransitionEnded(TransitionEndEvent evt)
        {
            if (!evt.stylePropertyNames.Contains(SwUiToolkitWindowHelper.TRANSLATE_TRANSITION_PROPERTY_NAME)) return;
            
            // Toggle between the two classes based on the current state
            if (_rocketIcon.ClassListContains(MOVE_FORWARD_CLASS))
            {
                _rocketIcon.RemoveFromClassList(MOVE_FORWARD_CLASS);
                _rocketIcon.AddToClassList(MOVE_BACKWARD_CLASS);
            }
            else
            {
                _rocketIcon.RemoveFromClassList(MOVE_BACKWARD_CLASS);
                _rocketIcon.AddToClassList(MOVE_FORWARD_CLASS);
            }
        }

        private void OnDestroy()
        {
            _rocketIcon?.UnregisterCallback<TransitionEndEvent>(OnTransitionEnded);
        }

        #endregion
    }
}
#endif