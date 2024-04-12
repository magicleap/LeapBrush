using System;
using System.Text;
using MixedReality.Toolkit;
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
        private LeapBrushKeyboard _keyboard;

        [SerializeField]
        private LocalizationMapManager _localizationManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private StatefulInteractable _startContinueButton;

        [SerializeField]
        private TMP_Text _startDescriptionText;

        [SerializeField]
        private StatefulInteractable _changeSpaceButton;

        [SerializeField]
        private StatefulInteractable _changeUserNameButton;

        private DelayedButtonHandler _delayedButtonHandler;
        private PopupTracker _popupTracker;
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
            _popupTracker = gameObject.AddComponent<PopupTracker>();
        }

        private void Start()
        {
            _startContinueButton.OnClicked.AddListener(OnContinueButtonClicked);
            _changeSpaceButton.OnClicked.AddListener(OnChangeSpaceButtonClicked);
            _changeUserNameButton.OnClicked.AddListener(OnChangeUserNameButtonClicked);

#if !UNITY_ANDROID
            _changeSpaceButton.gameObject.SetActive(false);
#endif
        }

        private void OnEnable()
        {
            _popupTracker.TrackPopup(_keyboard);
            _popupTracker.OnPopupsShownChanged += OnPopupsShownChanged;
            OnPopupsShownChanged(_popupTracker.PopupsShown);

            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;
            _keyboard.OnTextEntered += OnChangeUsernameKeyboardTextEntered;
        }

        private void OnDisable()
        {
            _popupTracker.OnPopupsShownChanged -= OnPopupsShownChanged;
            _localizationManager.OnLocalizationInfoChanged -= OnLocalizationInfoChanged;
            _keyboard.OnTextEntered -= OnChangeUsernameKeyboardTextEntered;
        }

        private void OnPopupsShownChanged(bool popupsShown)
        {
            _startContinueButton.enabled = !popupsShown;
            _changeSpaceButton.enabled = !popupsShown;
            _changeUserNameButton.enabled = !popupsShown;
        }

        private void OnContinueButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                gameObject.SetActive(false);
                OnContinueSelected?.Invoke();
            });
        }

        private void OnChangeSpaceButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                SpacesAppApi.StartApp();
            });
        }

        private void OnChangeUserNameButtonClicked()
        {
            _keyboard.Show(_userDisplayName);
        }

        private void OnChangeUsernameKeyboardTextEntered(string text)
        {
            OnSetUserDisplayName?.Invoke(text.Trim());
        }

        private void OnLocalizationInfoChanged(LocalizationMapManager.LocalizationMapInfo localizationInfo)
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
            sb.Append(_localizationManager.LocalizationInfo.MapName);
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