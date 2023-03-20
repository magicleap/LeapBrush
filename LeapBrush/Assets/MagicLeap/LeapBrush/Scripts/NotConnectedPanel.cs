using System;
using MagicLeap.DesignToolkit.Actions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Panel displayed when the currently selected server cannot be connected to.
    /// </summary>
    public class NotConnectedPanel : MonoBehaviour
    {
        public event Action OnChooseServerSelected;
        public event Action OnDrawSoloSelected;

        [SerializeField]
        private Interactable _chooseServerButton;

        [SerializeField]
        private Interactable _drawSoloButton;

        [SerializeField]
        private TMP_Text _notConnectedDescriptionText;

        private DelayedButtonHandler _delayedButtonHandler;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _chooseServerButton.Events.OnSelect.AddListener(OnChooseServerButtonSelected);
            _drawSoloButton.Events.OnSelect.AddListener(OnDrawSoloButtonSelected);
        }

        private void OnChooseServerButtonSelected(Interactor arg0)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnChooseServerSelected?.Invoke();
            });
        }

        private void OnDrawSoloButtonSelected(Interactor arg0)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnDrawSoloSelected?.Invoke();
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