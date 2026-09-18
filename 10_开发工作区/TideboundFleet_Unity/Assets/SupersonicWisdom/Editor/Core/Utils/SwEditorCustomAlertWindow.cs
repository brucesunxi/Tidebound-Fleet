using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SupersonicWisdomSDK.Editor
{
    public class SwEditorCustomAlertWindow : EditorWindow
    {
        #region --- Members ---
        
        private string _message;
        private string _okText;
        private string _cancelText;
        private float _interactiveDelay;

        private Button _okButton;
        private Button _cancelButton;
        private VisualElement _container;
        private Label _messageLabel;
        
        #endregion
        
        
        #region --- Constants ---
        
        private const string WINDOW_TITLE = SwEditorConstants.SDK_NAME + " " + SwEditorConstants.UI.ALERT;
        
        #endregion
        
        
        #region --- Static Public Methods ---
        
        public static void ShowAlert(string message, string okText, float interactiveDelay = 0f, string cancelText = "")
        {
            var window = CreateInstance<SwEditorCustomAlertWindow>();
            window._message = message;
            window._okText = okText;
            window._interactiveDelay = interactiveDelay;
            window._cancelText = cancelText;

            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.CenterOnMainWindow();
            window.ShowUtility();
        }
        
        #endregion
        
        
        #region --- Unity Methods ---

        private void CreateGUI()
        {
            var root = rootVisualElement;
            SwEditorUiToolkitHelper.AddEditorStyleSheetToVisualElement(root);
            
            _container = new VisualElement();
            SwEditorUiToolkitHelper.AddClassToVisualElement(_container, SwEditorUiToolkitHelper.SwEditorUiToolkitStyle.Container);
            root.Add(_container);
            
            var myImage = SwEditorUiToolkitHelper.GetSupersonicLogo();
            
            if (myImage != null)
            {
                var imageView = new Image { image = myImage };
                SwEditorUiToolkitHelper.AddClassToVisualElement(imageView, SwEditorUiToolkitHelper.SwEditorUiToolkitStyle.Image);
                _container.Add(imageView);
            }
            
            _messageLabel = new Label(_message);
            SwEditorUiToolkitHelper.AddClassToVisualElement(_messageLabel, SwEditorUiToolkitHelper.SwEditorUiToolkitStyle.Message);
            _container.Add(_messageLabel);

            _okButton = new Button(Close) { text = _okText };
            SwEditorUiToolkitHelper.AddClassToVisualElement(_okButton, SwEditorUiToolkitHelper.SwEditorUiToolkitStyle.Button);
            root.Add(_okButton);
            _okButton.SetEnabled(false); 

            if (!string.IsNullOrEmpty(_cancelText))
            {
                _cancelButton = new Button(Close) { text = _cancelText };
                SwEditorUiToolkitHelper.AddClassToVisualElement(_cancelButton, SwEditorUiToolkitHelper.SwEditorUiToolkitStyle.Button);
                root.Add(_cancelButton);
                _cancelButton.SetEnabled(false);
            }

            if (_interactiveDelay > 0)
            {
                SwEditorCoroutines.StartEditorCoroutineWithDelay(EnableInteractionCoroutine(), _interactiveDelay);
            }
            else
            {
                EnableInteraction();
            }

            root.RegisterCallback<GeometryChangedEvent>(e => AdjustWindowSize());
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private void AdjustWindowSize()
        {
            const float padding = 10f;
            var containerSize = new Vector2(600f, 400f);
            var size = new Vector2(_container.resolvedStyle.width + padding, _container.resolvedStyle.height + _okButton.layout.height + padding + (_cancelButton != null ? _cancelButton.layout.height : 0f));
            size = Vector2.Min(size, containerSize);
            
            minSize = maxSize = size;
            rootVisualElement.style.width = size.x;
            rootVisualElement.style.height = size.y;
        }

        private void EnableInteraction()
        {
            _okButton.SetEnabled(true);
            _cancelButton?.SetEnabled(true);
        }

        private IEnumerator EnableInteractionCoroutine()
        {
            yield return null;
            EnableInteraction();
        }

        private void CenterOnMainWindow()
        {
            EditorWindow mainWindow = GetWindow<SceneView>();
            var mainWinRect = mainWindow.position;
            var winRect = new Rect(mainWinRect.center.x - minSize.x / 2, mainWinRect.center.y - minSize.y / 2, minSize.x, minSize.y);
            position = winRect;
        }
        
        #endregion
    }
}
