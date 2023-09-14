using System;
using MixedReality.Toolkit;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The main UI panel. Users can pick from the various brush options, settings, import model,
    /// and color palette.
    /// </summary>
    public class MainPanel : MonoBehaviour
    {
        public event Action OnSettingsSelected;
        public event Action OnToolSelected;

        [Header("External Dependencies")]

        [SerializeField]
        private ColorPalettePopup _colorPalettePopup;

        [SerializeField]
        private ImportModelsPopup _importModelsPopup;

        [SerializeField]
        private ToolManager _toolManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private StatefulInteractable _scribbleBrushButton;

        [SerializeField]
        private StatefulInteractable _colorPaletteButton;

        [SerializeField]
        private StatefulInteractable _polyBrushButton;

        [SerializeField]
        private StatefulInteractable _eraserToolButton;

        [SerializeField]
        private StatefulInteractable _laserPointerButton;

        [SerializeField]
        private StatefulInteractable _importModelButton;

        [SerializeField]
        private StatefulInteractable _settingsButton;

        private DelayedButtonHandler _delayedButtonHandler;
        private PopupTracker _popupTracker;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
            _popupTracker = gameObject.AddComponent<PopupTracker>();
        }

        private void OnEnable()
        {
            _scribbleBrushButton.OnClicked.AddListener(OnScribbleBrushToolButtonClicked);
            _colorPaletteButton.OnClicked.AddListener(OnColorPaletteButtonClicked);
            _polyBrushButton.OnClicked.AddListener(OnPolyBrushToolButtonClicked);
            _eraserToolButton.OnClicked.AddListener(OnEraserToolButtonClicked);
            _laserPointerButton.OnClicked.AddListener(OnLaserPointerButtonClicked);
            _importModelButton.OnClicked.AddListener(OnImportModelButtonClicked);
            _settingsButton.OnClicked.AddListener(OnSettingsButtonClicked);

            _popupTracker.TrackPopup(_colorPalettePopup);
            _popupTracker.TrackPopup(_importModelsPopup);
            _popupTracker.OnPopupsShownChanged += OnPopupsShownChanged;
            OnPopupsShownChanged(_popupTracker.PopupsShown);
        }

        private void OnDisable()
        {
            _scribbleBrushButton.OnClicked.RemoveListener(OnScribbleBrushToolButtonClicked);
            _colorPaletteButton.OnClicked.RemoveListener(OnColorPaletteButtonClicked);
            _polyBrushButton.OnClicked.RemoveListener(OnPolyBrushToolButtonClicked);
            _eraserToolButton.OnClicked.RemoveListener(OnEraserToolButtonClicked);
            _laserPointerButton.OnClicked.RemoveListener(OnLaserPointerButtonClicked);
            _importModelButton.OnClicked.RemoveListener(OnImportModelButtonClicked);
            _settingsButton.OnClicked.RemoveListener(OnSettingsButtonClicked);

            _popupTracker.OnPopupsShownChanged -= OnPopupsShownChanged;
        }

        private void OnScribbleBrushToolButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _toolManager.SetActiveTool(ToolType.BrushScribble);
                OnToolSelected?.Invoke();
            });
        }

        private void OnColorPaletteButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _colorPalettePopup.Show();
            });
        }

        private void OnPolyBrushToolButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _toolManager.SetActiveTool(ToolType.BrushPoly);
                OnToolSelected?.Invoke();
            });
        }

        private void OnEraserToolButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _toolManager.SetActiveTool(ToolType.Eraser);
                OnToolSelected?.Invoke();
            });
        }

        private void OnLaserPointerButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _toolManager.SetActiveTool(ToolType.Laser);
                OnToolSelected?.Invoke();
            });
        }

        private void OnImportModelButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _importModelsPopup.Show();
            });
        }

        private void OnSettingsButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                OnSettingsSelected?.Invoke();
            });
        }

        private void OnPopupsShownChanged(bool popupsShown)
        {
            _scribbleBrushButton.enabled = !popupsShown;
            _colorPaletteButton.enabled = !popupsShown;
            _polyBrushButton.enabled = !popupsShown;
            _eraserToolButton.enabled = !popupsShown;
            _laserPointerButton.enabled = !popupsShown;
            _importModelButton.enabled = !popupsShown;
            _settingsButton.enabled = !popupsShown;
        }
    }
}