using System;
using MagicLeap.DesignToolkit.Actions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup asking the user if they would like to join another user eithe remotely or in person.
    /// </summary>
    public class JoinUserLocalOrRemotePopup : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _descriptionText;

        [SerializeField]
        private Interactable _joinRemotelyButton;

        [SerializeField]
        private Interactable _joinInPersonButton;

        [SerializeField]
        private Interactable _cancelButton;

        private Action _onJoinRemotely;
        private Action _onJoinInPerson;
        private DelayedButtonHandler _delayedButtonHandler;

        public void Show(string userDisplayName, string spaceName, Action onJoinRemotely,
            Action onJoinInPerson)
        {
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

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _joinRemotelyButton.Events.OnSelect.AddListener(OnJoinRemotelyButtonSelected);
            _joinInPersonButton.Events.OnSelect.AddListener(OnJoinInPersonButtonSelected);
            _cancelButton.Events.OnSelect.AddListener(OnCancelButtonSelected);
        }

        private void OnJoinInPersonButtonSelected(Interactor _)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _onJoinInPerson?.Invoke();
            });
        }

        private void OnJoinRemotelyButtonSelected(Interactor _)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _onJoinRemotely?.Invoke();
            });
        }

        private void OnCancelButtonSelected(Interactor _)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(Hide);
        }
    }
}