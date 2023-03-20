using System;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The main UI panel. Users can pick from the various brush options, settings, import model,
    /// and color palette.
    /// </summary>
    public class MainPanel : MonoBehaviour
    {
        public event Action OnScribbleBrushToolSelected;
        public event Action OnColorPaletteSelected;
        public event Action OnPolyBrushToolSelected;
        public event Action OnEraserToolSelected;
        public event Action OnLaserPointerSelected;
        public event Action OnImportModelSelected;
        public event Action OnSettingsSelected;

        [SerializeField]
        private Interactable _scribbleBrushButton;

        [SerializeField]
        private Interactable _colorPaletteButton;

        [SerializeField]
        private Interactable _polyBrushButton;

        [SerializeField]
        private Interactable _eraserToolButton;

        [SerializeField]
        private Interactable _laserPointerButton;

        [SerializeField]
        private Interactable _importModelButton;

        [SerializeField]
        private Interactable _settingsButton;

        private DelayedButtonHandler _delayedButtonHandler;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        private void Start()
        {
            _scribbleBrushButton.Events.OnSelect.AddListener(OnScribbleBrushToolButtonSelected);
            _colorPaletteButton.Events.OnSelect.AddListener(OnColorPaletteButtonSelected);
            _polyBrushButton.Events.OnSelect.AddListener(OnPolyBrushToolButtonSelected);
            _eraserToolButton.Events.OnSelect.AddListener(OnEraserToolButtonSelected);
            _laserPointerButton.Events.OnSelect.AddListener(OnLaserPointerButtonSelected);
            _importModelButton.Events.OnSelect.AddListener(OnImportModelButtonSelected);
            _settingsButton.Events.OnSelect.AddListener(OnSettingsButtonSelected);
        }

        private void OnScribbleBrushToolButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnScribbleBrushToolSelected?.Invoke();
            });
        }

        private void OnColorPaletteButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnColorPaletteSelected?.Invoke();
            });
        }

        private void OnPolyBrushToolButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnPolyBrushToolSelected?.Invoke();
            });
        }

        private void OnEraserToolButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnEraserToolSelected?.Invoke();
            });
        }

        private void OnLaserPointerButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnLaserPointerSelected?.Invoke();
            });
        }

        private void OnImportModelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnImportModelSelected?.Invoke();
            });
        }

        private void OnSettingsButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnSettingsSelected?.Invoke();
            });
        }
    }
}