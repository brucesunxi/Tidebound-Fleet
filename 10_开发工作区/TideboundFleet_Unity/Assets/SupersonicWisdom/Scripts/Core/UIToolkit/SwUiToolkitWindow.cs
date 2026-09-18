using UnityEngine;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK
{
    internal abstract class SwUiToolkitWindow : MonoBehaviour
    {
        #region --- Inspector ---

        [SerializeField] private VisualTreeAsset _visualTreeAsset;

        #endregion


        #region --- Constants ---

        private const string USS_CLOSE_WINDOW_CLASS = "window-closed";
        private const string USS_OPEN_WINDOW_CLASS = "window-open";

        #endregion


        #region --- Members ---

        private SwVisualElementPayload[] _payload;
        protected SwUiToolkitManager _uiToolkitManager;

        #endregion


        #region --- Properties ---

        internal abstract ESwUiToolkitType Type { get; }
        internal abstract int Priority { get; } // The higher the priority, the more likely it is to be displayed first.
        internal abstract bool IsBlockingAds { get; }

        #endregion


        #region --- Public Methods ---

        internal void SetPayload(params SwVisualElementPayload[] payload)
        {
            _payload = payload;

            SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Payload for window of type {Type} was set.");
        }

        internal void Open(SwUiToolkitManager uiToolkitManager)
        {
            Initialize(uiToolkitManager);
            TryToDisplay(_payload);
        }

        internal void Close()
        {
            TryToClose();
        }
        
        #endregion
        
        
        #region --- Private Methods ---

        private void Initialize(SwUiToolkitManager manager)
        {
            _uiToolkitManager = manager;
        }
        
        protected abstract void OnDisplay(SwVisualElementPayload[] payload = null);
        
        protected abstract void OnClose();

        protected void CloseWindow()
        {
            _uiToolkitManager.CloseWindow(Type);
        }
        
        private void TryToDisplay(params SwVisualElementPayload[] payload)
        {
            if (_uiToolkitManager?.UiDocument == null) return;

            _visualTreeAsset.CloneTree(_uiToolkitManager.UiDocument.rootVisualElement);

            if (payload != null && !payload.SwIsNullOrEmpty())
            {
                OnDisplay(payload);

                SwInfra.Logger.Log(EWisdomLogType.UiToolkit,
                    $"Payload for window of type {Type} was used to open popup.");
            }
            else
            {
                OnDisplay();

                SwInfra.Logger.Log(EWisdomLogType.UiToolkit,
                    $"No payload for window of type {Type} was provided, opening popup without payload.");
            }

            // CSS based animation function
            if (_uiToolkitManager == null) return;

            if (_uiToolkitManager.UiDocument != null)
            {
                SwUiToolkitWindowHelper.SwitchClass(_uiToolkitManager.UiDocument.rootVisualElement,
                    USS_OPEN_WINDOW_CLASS, USS_CLOSE_WINDOW_CLASS);
            }
        }

        private void TryToClose()
        {
            OnClose();

            // CSS based animation function
            SwUiToolkitWindowHelper.SwitchClass(_uiToolkitManager.UiDocument.rootVisualElement, USS_CLOSE_WINDOW_CLASS,
                USS_OPEN_WINDOW_CLASS);

            SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Window of type {Type} was closed.");
        }


        protected T TryToGetVisualElement<T>(string elementName) where T : VisualElement
        {
            if (string.IsNullOrWhiteSpace(elementName))
            {
                SwInfra.Logger.Log(EWisdomLogType.UiToolkit, "Element name cannot be null or white space.");
                return null;
            }

            SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Trying to find {elementName} in the root visual element.");

            var element = _uiToolkitManager.UiDocument.rootVisualElement.Q<T>(elementName);

            SwInfra.Logger.Log(EWisdomLogType.UiToolkit,
                element == null
                    ? $"No {typeof(T).Name} found with name {elementName}."
                    : $"{typeof(T).Name} found with name {elementName}.");

            return element;
        }

        #endregion
    }
}