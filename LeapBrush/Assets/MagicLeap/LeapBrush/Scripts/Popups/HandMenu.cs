using System;
using MixedReality.Toolkit.UX;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class HandMenu : MonoBehaviour
    {
        public event Action OnEnabledChanged;
        public event Action OnShowMainMenuClicked;
        public event Action OnToolSelected;

        [Header("External Dependencies")]

        [SerializeField]
        private ToolManager _toolManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private ColorPicker _strokeColorPicker;

        [SerializeField]
        private PressableButton _showMainMenuButton;

        [SerializeField]
        private PressableButton _scribbleBrushButton;

        [SerializeField]
        private PressableButton _polyBrushButton;

        [SerializeField]
        private PressableButton _eraserToolButton;

        [SerializeField]
        private PressableButton _laserPointerButton;

        [SerializeField]
        private Slider _toolOffsetSlider;

        private void OnEnable()
        {
            OnEnabledChanged?.Invoke();

            _strokeColorPicker.OnColorUpdated.AddListener(OnStrokeColorPickerColorChanged);
            _showMainMenuButton.OnClicked.AddListener(OnShowMainMenuButtonClicked);
            _scribbleBrushButton.OnClicked.AddListener(OnScribbleBrushToolButtonClicked);
            _polyBrushButton.OnClicked.AddListener(OnPolyBrushToolButtonClicked);
            _eraserToolButton.OnClicked.AddListener(OnEraserToolButtonClicked);
            _laserPointerButton.OnClicked.AddListener(OnLaserPointerButtonClicked);
            _toolOffsetSlider.OnValueUpdated.AddListener(OnToolOffsetSliderChanged);

            HandleBrushColorsChanged();
            HandleActiveToolChanged();
            HandleToolOffsetChanged();
            _toolManager.OnBrushColorsChanged += HandleBrushColorsChanged;
            _toolManager.OnActiveToolChanged += HandleActiveToolChanged;
            _toolManager.OnToolOffsetChanged += HandleToolOffsetChanged;
        }

        private void OnDisable()
        {
            OnEnabledChanged?.Invoke();

            _strokeColorPicker.OnColorUpdated.RemoveListener(OnStrokeColorPickerColorChanged);
            _showMainMenuButton.OnClicked.RemoveListener(OnShowMainMenuButtonClicked);
            _scribbleBrushButton.OnClicked.RemoveListener(OnScribbleBrushToolButtonClicked);
            _polyBrushButton.OnClicked.RemoveListener(OnPolyBrushToolButtonClicked);
            _eraserToolButton.OnClicked.RemoveListener(OnEraserToolButtonClicked);
            _laserPointerButton.OnClicked.RemoveListener(OnLaserPointerButtonClicked);
            _toolOffsetSlider.OnValueUpdated.RemoveListener(OnToolOffsetSliderChanged);

            _toolManager.OnBrushColorsChanged -= HandleBrushColorsChanged;
            _toolManager.OnActiveToolChanged -= HandleActiveToolChanged;
            _toolManager.OnToolOffsetChanged -= HandleToolOffsetChanged;
        }

        private void HandleBrushColorsChanged()
        {
            if (!ColorUtils.Color32sEqual(_strokeColorPicker.Color, _toolManager.StrokeColor,
                    ignoreAlpha: true))
            {
                _strokeColorPicker.SetColor(_toolManager.StrokeColor, fireEvents: false);
            }
        }

        private void HandleActiveToolChanged()
        {
            _scribbleBrushButton.ForceSetToggled(
                _toolManager.ActiveTool == ToolType.BrushScribble, false);
            _polyBrushButton.ForceSetToggled(
                _toolManager.ActiveTool == ToolType.BrushPoly, false);
            _eraserToolButton.ForceSetToggled(
                _toolManager.ActiveTool == ToolType.Eraser, false);
            _laserPointerButton.ForceSetToggled(
                _toolManager.ActiveTool == ToolType.Laser, false);
        }

        private void HandleToolOffsetChanged()
        {
            _toolOffsetSlider.MinValue = ToolManager.MinToolOffset;
            _toolOffsetSlider.MaxValue = ToolManager.MaxToolOffset;
            _toolOffsetSlider.OnValueUpdated.RemoveListener(OnToolOffsetSliderChanged);
            _toolOffsetSlider.Value = _toolManager.ToolOffset;
            _toolOffsetSlider.OnValueUpdated.AddListener(OnToolOffsetSliderChanged);
        }

        private void OnStrokeColorPickerColorChanged(Color newColor)
        {
            _toolManager.SetStrokeColor(newColor, true);
        }

        private void OnShowMainMenuButtonClicked()
        {
            OnShowMainMenuClicked?.Invoke();
        }

        private void OnScribbleBrushToolButtonClicked()
        {
            _toolManager.SetActiveTool(ToolType.BrushScribble);
            OnToolSelected?.Invoke();
        }

        private void OnPolyBrushToolButtonClicked()
        {
            _toolManager.SetActiveTool(ToolType.BrushPoly);
            OnToolSelected?.Invoke();
        }

        private void OnEraserToolButtonClicked()
        {
            _toolManager.SetActiveTool(ToolType.Eraser);
            OnToolSelected?.Invoke();
        }

        private void OnLaserPointerButtonClicked()
        {
            _toolManager.SetActiveTool(ToolType.Laser);
            OnToolSelected?.Invoke();
        }

        private void OnToolOffsetSliderChanged(SliderEventData _)
        {
            _toolManager.SetToolOffset(_toolOffsetSlider.Value);
        }
    }
}