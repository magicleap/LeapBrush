using System;
using MixedReality.Toolkit;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup asking the user if they would like to join another user eithe remotely or in person.
    /// </summary>
    public class JoinUserLocalOrRemotePopup : BasePopup
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _descriptionText;

        [SerializeField]
        private StatefulInteractable _joinRemotelyButton;

        [SerializeField]
        private StatefulInteractable _joinInPersonButton;

        [SerializeField]
        private StatefulInteractable _cancelButton;

        private Action _onJoinRemotely;
        private Action _onJoinInPerson;
        private DelayedButtonHandler _delayedButtonHandler;

        public void Show(string userDisplayName, string spaceName, Action onJoinRemotely,
            Action onJoinInPerson)
        {
            base.Show();

            LocalizeStringEvent titleTextLocalized = _titleText.GetComponent<LocalizeStringEvent>();
            ((StringVariable) titleTextLocalized.StringReference["UserName"]).Value
                = userDisplayName;
            _titleText.text = titleTextLocalized.StringReference.GetLocalizedString();

            LocalizeStringEvent descriptionTextLocalized =
                _descriptionText.GetComponent<LocalizeStringEvent>();
            ((StringVariable) descriptionTextLocalized.StringReference["UserName"]).Value
                = userDisplayName;
            ((StringVariable) descriptionTextLocalized.StringReference["SpaceName"]).Value
                = spaceName;
            _descriptionText.text = descriptionTextLocalized.StringReference.GetLocalizedString();

            _onJoinRemotely = onJoinRemotely;
            _onJoinInPerson = onJoinInPerson;
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _joinRemotelyButton.OnClicked.AddListener(OnJoinRemotelyButtonClicked);
            _joinInPersonButton.OnClicked.AddListener(OnJoinInPersonButtonClicked);
            _cancelButton.OnClicked.AddListener(OnCancelButtonClicked);
        }

        private void OnJoinInPersonButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _onJoinInPerson?.Invoke();
            });
        }

        private void OnJoinRemotelyButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _onJoinRemotely?.Invoke();
            });
        }

        private void OnCancelButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(Hide);
        }
    }
}