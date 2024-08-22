using System;
using MagicLeap.DesignToolkit.Keyboard;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicLeap.LeapBrush
{
    public class LeapBrushKeyboard : MonoBehaviour, IPopup
    {
        public Action<string> OnTextEntered;
        public Action OnClosed;

        private KeyboardManager _keyboardManager;

        public event Action<IPopup, bool> OnShownChanged;

        public bool IsShown => _showRequested;

        private bool _showRequested;
        private AsyncOperation _sceneLoadOperation;
        private bool _enterKeyPressedBeforeClose;

        private const string KeyboardSceneName = "LeapBrushKeyboard";

        public void Show(string text)
        {
            Preload();

            if (!_showRequested)
            {
                _showRequested = true;
                OnShownChanged?.Invoke(this, true);
            }

            if (_keyboardManager != null)
            {
                ShowInternal(text);
                return;
            }

            _sceneLoadOperation.completed += (_) =>
            {
                if (_showRequested)
                {
                    ShowInternal(text);
                }
            };
        }

        public void Preload()
        {
            if (_sceneLoadOperation == null)
            {
                _sceneLoadOperation = SceneManager.LoadSceneAsync(KeyboardSceneName,
                    LoadSceneMode.Additive);
                _sceneLoadOperation.completed += (_) =>
                {
                    Scene scene = SceneManager.GetSceneByName(KeyboardSceneName);
                    if (!scene.IsValid())
                    {
                        Debug.LogError("Failed to find keyboard scene");
                    }

                    _keyboardManager = scene.GetRootGameObjects()[0].GetComponent<KeyboardManager>();
                };
            }
        }

        private void ShowInternal(string text)
        {
            _keyboardManager.gameObject.SetActive(true);
            _keyboardManager.PublishKeyEvent.AddListener(OnKeyboardKeyPressed);
            _keyboardManager.OnKeyboardClose.AddListener(OnKeyboardClosed);

            _enterKeyPressedBeforeClose = false;
            _keyboardManager.TypedContent = text;
            _keyboardManager.InputField.text = text;
        }

        public void Hide()
        {
            if (_keyboardManager != null)
            {
                _keyboardManager.gameObject.SetActive(false);
            }

            if (_showRequested)
            {
                _showRequested = false;
                OnShownChanged?.Invoke(this, false);
            }
        }

        private void OnKeyboardKeyPressed(
            string textToType, KeyType keyType, bool doubleClicked, string typedContent)
        {
            if (keyType == KeyType.kEnter || keyType == KeyType.kJPEnter)
            {
                _enterKeyPressedBeforeClose = true;
                OnTextEntered?.Invoke(typedContent);
            }
        }

        private void OnKeyboardClosed()
        {
            _keyboardManager.PublishKeyEvent.RemoveListener(OnKeyboardKeyPressed);
            _keyboardManager.OnKeyboardClose.RemoveListener(OnKeyboardClosed);

            if (!_enterKeyPressedBeforeClose)
            {
                OnTextEntered?.Invoke(_keyboardManager.TypedContent);
            }

            Hide();

            OnClosed?.Invoke();
        }
    }
}