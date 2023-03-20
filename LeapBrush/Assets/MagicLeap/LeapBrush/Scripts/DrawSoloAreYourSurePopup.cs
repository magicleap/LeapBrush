using System;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Popup allowing the user to confirm or cancel starting a draw solo (offline) drawing
    /// session.
    /// </summary>
    public class DrawSoloAreYourSurePopup : MonoBehaviour
    {
        public event Action OnConfirmSelected;

        [SerializeField]
        private Interactable _drawSoloCancelButton;

        [SerializeField]
        private Interactable _drawSoloContinueButton;

        private DelayedButtonHandler _delayedButtonHandler;

        public void Show()
        {
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
            _drawSoloContinueButton.Events.OnSelect.AddListener(OnContinueButtonSelected);
            _drawSoloCancelButton.Events.OnSelect.AddListener(OnCancelButtonSelected);
        }

        private void OnContinueButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                gameObject.SetActive(false);
                OnConfirmSelected?.Invoke();
            });
        }

        private void OnCancelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                gameObject.SetActive(false);
            });
        }
    }
}
