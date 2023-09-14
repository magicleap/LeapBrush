using System;
using MixedReality.Toolkit;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup allowing the user to confirm or cancel starting a draw solo (offline) drawing
    /// session.
    /// </summary>
    public class DrawSoloAreYourSurePopup : BasePopup
    {
        public event Action OnConfirmSelected;

        [SerializeField]
        private StatefulInteractable _drawSoloCancelButton;

        [SerializeField]
        private StatefulInteractable _drawSoloContinueButton;

        private DelayedButtonHandler _delayedButtonHandler;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _drawSoloContinueButton.OnClicked.AddListener(OnContinueButtonClicked);
            _drawSoloCancelButton.OnClicked.AddListener(OnCancelButtonClicked);
        }

        private void OnContinueButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
                OnConfirmSelected?.Invoke();
            });
        }

        private void OnCancelButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }
    }
}
