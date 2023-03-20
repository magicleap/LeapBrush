using System;
using System.Text;
using MagicLeap.DesignToolkit.Actions;
using MagicLeap.DesignToolkit.Keyboard;
using TMPro;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// UI Panel allowing the user to select their name and confirm the space to use before
    /// continuing on to drawing.
    /// </summary>
    public class StartPanel : MonoBehaviour
    {
        public event Action OnContinueSelected;
        public event Action<string> OnSetUserDisplayName;

        [Header("External Dependencies")]

        [SerializeField]
        private KeyboardManager _keyboardManager;

        [SerializeField]
        private SpaceLocalizationManager _localizationManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private Interactable _startContinueButton;

        [SerializeField]
        private TMP_Text _startDescriptionText;

        [SerializeField]
        private Interactable _changeSpaceButton;

        [SerializeField]
        private Interactable _changeUserNameButton;

        private DelayedButtonHandler _delayedButtonHandler;
        private string _userDisplayName;
        private bool _drawSolo;
        private int _otherUserCount;

        public void Show(string userDisplayName)
        {
            _userDisplayName = userDisplayName;
            gameObject.SetActive(true);

            UpdateStartDescriptionText();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnUserDisplayNameChanged(string userDisplayName)
        {
            _userDisplayName = userDisplayName;

            if (isActiveAndEnabled)
            {
                UpdateStartDescriptionText();
            }
        }

        public void OnDrawSolo()
        {
            _drawSolo = true;

            if (isActiveAndEnabled)
            {
                UpdateStartDescriptionText();
            }
        }

        public void OnOtherUserCountChanged(int otherUserCount)
        {
            _otherUserCount = otherUserCount;

            if (isActiveAndEnabled)
            {
                UpdateStartDescriptionText();
            }
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _startContinueButton.Events.OnSelect.AddListener(OnContinueButtonSelected);
            _changeSpaceButton.Events.OnSelect.AddListener(OnChangeSpaceButtonSelected);
            _changeUserNameButton.Events.OnSelect.AddListener(OnChangeUserNameButtonSelected);

#if !UNITY_ANDROID
            _changeSpaceButton.gameObject.SetActive(false);
#endif
        }

        private void OnEnable()
        {
            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;
        }

        private void OnDisable()
        {
            _localizationManager.OnLocalizationInfoChanged -= OnLocalizationInfoChanged;
        }

        private void OnContinueButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                gameObject.SetActive(false);
                OnContinueSelected?.Invoke();
            });
        }

        private void OnChangeSpaceButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                SpacesAppApi.StartApp();
            });
        }

        private void OnChangeUserNameButtonSelected(Interactor interactor)
        {
            _keyboardManager.gameObject.SetActive(true);
            _keyboardManager.OnKeyboardClose.AddListener(OnChangeUsernameKeyboardClosed);
            _keyboardManager.PublishKeyEvent.AddListener(OnChangeUsernameKeyboardKeyPressed);

            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _keyboardManager.TypedContent = _userDisplayName;
                _keyboardManager.InputField.text = _userDisplayName;
            });
        }

        private void OnChangeUsernameKeyboardKeyPressed(
            string textToType, KeyType keyType, bool doubleClicked, string typedContent)
        {
            if (keyType == KeyType.kEnter || keyType == KeyType.kJPEnter)
            {
                OnSetUserDisplayName?.Invoke(typedContent.Trim());
            }
        }

        private void OnChangeUsernameKeyboardClosed()
        {
            _keyboardManager.PublishKeyEvent.RemoveListener(OnChangeUsernameKeyboardKeyPressed);
            _keyboardManager.OnKeyboardClose.RemoveListener(OnChangeUsernameKeyboardClosed);

            OnSetUserDisplayName?.Invoke(_keyboardManager.TypedContent.Trim());

            _keyboardManager.gameObject.SetActive(false);
        }

        private void OnLocalizationInfoChanged(AnchorsApi.LocalizationInfo localizationInfo)
        {
            if (isActiveAndEnabled)
            {
                UpdateStartDescriptionText();
            }
        }

        private void UpdateStartDescriptionText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Hi <b>");

            sb.Append(_userDisplayName);

            sb.Append("</b>, select continue to draw in the <b>");
            sb.Append(_localizationManager.LocalizationInfo.SpaceName);
            sb.Append("</b> space. ");

            if (!_drawSolo)
            {
                if (_otherUserCount == 0)
                {
                    sb.Append("You are the first person here.");
                }
                else if (_otherUserCount == 1)
                {
                    sb.Append("There is one other person here.");
                }
                else
                {
                    sb.AppendFormat("There are {0} other people here.", _otherUserCount);
                }

#if !UNITY_ANDROID
                sb.Append("\n\n(You can join another user's session from the Settings "
                          + "menu after continuing)");
#endif
            }

            _startDescriptionText.text = sb.ToString();
        }
    }
}