using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK
{

    internal class SwUiToolkitManager
    {
        #region --- Members ---
        
        private static readonly Vector2Int PortraitResolution = new Vector2Int(REFERENCE_WIDTH_DEFAULT, REFERENCE_HEIGHT_DEFAULT);
        private static readonly Vector2Int LandscapeResolution = new Vector2Int(REFERENCE_HEIGHT_DEFAULT, REFERENCE_WIDTH_DEFAULT);
        private readonly Dictionary<ESwUiToolkitType, SwUiToolkitWindow> _windowToType = new Dictionary<ESwUiToolkitType, SwUiToolkitWindow>();
        private readonly List<ISwUiToolkitWindowStateListener> _windowStateListeners = new List<ISwUiToolkitWindowStateListener>();
        private readonly List<SwUiToolkitQueuedWindow> _windowQueue = new List<SwUiToolkitQueuedWindow>();
        private readonly MonoBehaviour _mono;
        
        private UIDocument _uiDocument;
        private SwUiToolkitWindow _currentWindow = null;
        
        #endregion
        
        
        #region --- Constants ---
        
        private const string WISDOM_PANEL_SETTINGS_PATH = "Core/UIToolkit/WisdomPanelSettings";
        private const int REFERENCE_WIDTH_DEFAULT = 1170;
        private const int REFERENCE_HEIGHT_DEFAULT = 2532;
        
        #endregion
        
        
        #region --- Properties ---

        internal bool AnyWindowOpen
        { 
            get { return _currentWindow != null; }
        }

        internal ESwUiToolkitType CurrentWindowType
        {
            get { return _currentWindow != null ? _currentWindow.Type : ESwUiToolkitType.None; }
        }

        internal UIDocument UiDocument
        {
            get { return GetUiDocument(); }
        }
        
        private Vector2Int ReferenceResolution
        {
            get
            {
                return SwUiToolkitWindowHelper.IsLandscape ? LandscapeResolution : PortraitResolution;
            }
        }
        
        #endregion

        
        #region --- Construction ---
        
        internal SwUiToolkitManager(MonoBehaviour mono, IEnumerable<SwUiToolkitWindow> windows)
        {
            _mono = mono;

            if (windows == null) return;
            
            foreach (var window in windows)
            {
                _windowToType.TryAdd(window.Type, window);
            }
        }
        
        #endregion
        
        
        #region --- Public Methods ---

        internal bool IsWindowOpen(ESwUiToolkitType type)
        {
            return AnyWindowOpen && _currentWindow.Type == type;
        }
        
        internal IEnumerator OpenWindow(ESwUiToolkitType type, Func<bool> condition = null, Action onOpen = null)
        {
            yield return OpenWindow(type, condition, onOpen, Array.Empty<SwVisualElementPayload>());
        }
        
        internal IEnumerator OpenWindow(ESwUiToolkitType type, SwVisualElementPayload payload, Func<bool> condition = null, Action onOpen = null)
        {
            yield return OpenWindow(type, condition, onOpen, new[] { payload });
        }

        internal IEnumerator OpenWindow(ESwUiToolkitType type, Func<bool> condition = null, Action onOpen = null, params SwVisualElementPayload[] payloads)
        {
            if (!WindowTypeExists(type, out SwUiToolkitWindow window))
            {
                SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Window of type {type} does not exist.");
                yield break;
            }

            var isCurrentWindowExist = _currentWindow != null;
            var logMessage = $"Window of type {type}";

            switch (isCurrentWindowExist)
            {
                case true when _currentWindow != window:
                    EnqueueWindow(new SwUiToolkitQueuedWindow(type, payloads, window.Priority, condition, onOpen));
                    SwInfra.Logger.Log(EWisdomLogType.UiToolkit,
                        logMessage + " added to the queue because another window is currently open.");
                    yield break;
                case true when _currentWindow.Type == type:
                    SwInfra.Logger.Log(EWisdomLogType.UiToolkit, logMessage + " is already open.");
                    yield break;
            }

            if (condition != null && !condition())
            {
                SwInfra.Logger.Log(EWisdomLogType.UiToolkit, logMessage + " condition not met, not opening.");
                yield break;
            }

            UiDocument.panelSettings.referenceResolution = ReferenceResolution;
            _currentWindow = window;
            _currentWindow.SetPayload(payloads);
            CreateBlockingPanel();
            _currentWindow.Open(this);
            NotifyWindowOpened(_currentWindow);
            onOpen?.Invoke();
            
            yield return new WaitWhile(() => IsWindowOpen(type));
        }


        internal void CloseWindow(ESwUiToolkitType type)
        {
            if (_currentWindow == null || _windowToType[type] != _currentWindow) return;
            
            SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Closing {_currentWindow.Type}");

            CloseCurrentWindow();
        }

        internal void CloseCurrentWindow()
        {
            if (_currentWindow == null) return;

            _currentWindow.Close();
            
            ClearRootVisualElement();

            NotifyWindowClosed(_currentWindow);
        }
        
        internal void AddListeners(IEnumerable<ISwUiToolkitWindowStateListener> uiToolkitWindowStateListeners)
        {
            _windowStateListeners.AddRange(uiToolkitWindowStateListeners);
        }
        
        #endregion
        
        
        #region --- Private Methods ---

        private bool WindowTypeExists(ESwUiToolkitType type, out SwUiToolkitWindow window)
        {
            return _windowToType.TryGetValue(type, out window);
        }
        
        private void NotifyWindowOpened(SwUiToolkitWindow window)
        {
            SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Notifying {_windowStateListeners.Count} listeners - Window opened!");
            
            foreach (var listener in _windowStateListeners)
            {
                listener.OnWindowOpened(window);
            }
        }

        private void NotifyWindowClosed(SwUiToolkitWindow window)
        {
            SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Notifying {_windowStateListeners.Count} listeners - Window closed!");

            foreach (var listener in _windowStateListeners)
            {
                listener.OnWindowClosed(window);
            }
            
            _currentWindow = null;
            
            // Dequeue and open the next window with the highest priority if its condition is met
            while (_windowQueue.Count > 0)
            {
                var nextWindow = _windowQueue.First();
                _windowQueue.RemoveAt(0); // Remove from the queue

                if (nextWindow.Condition == null || nextWindow.Condition())
                {
                    OpenWindow(nextWindow.Type, nextWindow.Condition, nextWindow.OnOpenAction, nextWindow.Payloads);
                    break;
                }

                SwInfra.Logger.Log(EWisdomLogType.UiToolkit, $"Condition for window of type {nextWindow.Type} not met, removed from queue.");
            }
        }

        private void ClearRootVisualElement()
        {
            UiDocument.rootVisualElement.Clear();
        }
        
        private UIDocument GetUiDocument()
        {
            if (_uiDocument != null) return _uiDocument;

            _uiDocument = _mono.GetComponent<UIDocument>() ?? _mono.gameObject.AddComponent<UIDocument>();
            _uiDocument.visualTreeAsset = ScriptableObject.CreateInstance<VisualTreeAsset>();
            _uiDocument.panelSettings = GetWisdomPanelSettings();

            return _uiDocument;
        }
        
        private static PanelSettings GetWisdomPanelSettings()
        {
            var settings = Resources.Load<PanelSettings>(WISDOM_PANEL_SETTINGS_PATH);

            if (settings != null) return settings;
            
            SwInfra.Logger.LogWarning(EWisdomLogType.UiToolkit ,"PanelSettings asset not found in Resources folder. A new instance will be created.");
            settings = ScriptableObject.CreateInstance<PanelSettings>();

            return settings;
        }
        
        private void EnqueueWindow(SwUiToolkitQueuedWindow window)
        {
            _windowQueue.Add(window);
            _windowQueue.Sort((w1, w2) => w2.Priority.CompareTo(w1.Priority)); // Sorts in descending order of priority
        }
        
        private void CreateBlockingPanel()
        {
            var blocker = new VisualElement
            {
                name = "BlockingPanel",
                pickingMode = PickingMode.Position, 
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = Color.black,
                    opacity = 0.25f,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0
                }
            };

            blocker.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            blocker.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            blocker.RegisterCallback<MouseUpEvent>(evt => evt.StopPropagation());
            blocker.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            blocker.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
            blocker.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());

            UiDocument.rootVisualElement.Insert(0, blocker); // Insert at the lowest index to ensure it's behind other content
        }
        
        #endregion
    }
}