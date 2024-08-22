using System;
using MixedReality.Toolkit;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Panel displayed when the currently selected server cannot be connected to.
    /// </summary>
    public class NotConnectedPanel : BasePanel
    {
        public event Action OnDrawSoloSelected;

        [Header("External Dependencies")]

        [SerializeField]
        private DrawSoloAreYourSurePopup _drawSoloAreYouSurePopup;

        [SerializeField]
        private ServerConnectionManager _serverConnectionManager;

        [SerializeField]
        private LeapBrushKeyboard _keyboard;

        [Header("Internal Dependencies")]

        [SerializeField]
        private StatefulInteractable _chooseServerButton;

        [SerializeField]
        private StatefulInteractable _drawSoloButton;

        [SerializeField]
        private TMP_Text _notConnectedDescriptionText;

        private DelayedButtonHandler _delayedButtonHandler;
        private PopupTracker _popupTracker;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
            _popupTracker = gameObject.AddComponent<PopupTracker>();
        }

        private void Start()
        {
            base.Start();

            _chooseServerButton.OnClicked.AddListener(OnChooseServerButtonClicked);
            _drawSoloButton.OnClicked.AddListener(OnDrawSoloButtonClicked);
            _drawSoloAreYouSurePopup.OnConfirmSelected += OnDrawSoloConfirmButtonSelected;
        }

        private void OnEnable()
        {
            _popupTracker.TrackPopup(_drawSoloAreYouSurePopup);
            _popupTracker.TrackPopup(_keyboard);
            _popupTracker.OnPopupsShownChanged += OnPopupsShownChanged;
            OnPopupsShownChanged(_popupTracker.PopupsShown);

            _keyboard.OnTextEntered += OnChooseServerKeyboardTextEntered;
        }

        private void OnDisable()
        {
            _popupTracker.OnPopupsShownChanged -= OnPopupsShownChanged;
            _drawSoloAreYouSurePopup.Hide();

            _keyboard.OnTextEntered -= OnChooseServerKeyboardTextEntered;
        }

        private void OnPopupsShownChanged(bool popupsShown)
        {
            _chooseServerButton.enabled = !popupsShown;
            _drawSoloButton.enabled = !popupsShown;
        }

        private void OnDrawSoloConfirmButtonSelected()
        {
            _drawSoloAreYouSurePopup.Hide();
            OnDrawSoloSelected?.Invoke();
        }

        private void OnChooseServerButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _keyboard.Show(_serverConnectionManager.ServerUrl);
            });
        }

        private void OnChooseServerKeyboardTextEntered(string text)
        {
            _serverConnectionManager.SetServerUrl(text.Trim());
        }

        private void OnDrawSoloButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _drawSoloAreYouSurePopup.Show();
            });
        }

        public void OnServerUrlChanged(string serverUrl)
        {
            LocalizeStringEvent textLocalized =
                _notConnectedDescriptionText.GetComponent<LocalizeStringEvent>();

            ((StringVariable) textLocalized.StringReference["ServerHostAndPort"]).Value
                = serverUrl;

            textLocalized.StringReference.RefreshString();
        }
    }
}