using System;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// UI for the Color Palette popup.
    /// </summary>
    public class ColorPalettePopup : BasePopup
    {
        [Header("External Dependencies")]

        [SerializeField]
        private ToolManager _toolManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private ColorPicker _strokeColorPicker;

        [SerializeField]
        private ColorPicker _fillColorPicker;

        [SerializeField]
        private PressableButton _fillToggle;

        [SerializeField]
        private PressableButton _fillDimToggle;

        [SerializeField]
        private Slider _fillAlphaSlider;

        [SerializeField]
        private Slider _fillDimSlider;

        [SerializeField]
        private GameObject _colorPickerFillPanel;

        [SerializeField]
        private StatefulInteractable _colorPickerCancelButton;

        [SerializeField]
        private GameObject _panelFillDimAvailable;

        [SerializeField]
        private GameObject _panelFillDimNotAvailable;

        [SerializeField]
        private StatefulInteractable _openDimmerSettingsButton;

        private DelayedButtonHandler _delayedButtonHandler;

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        void Start()
        {
            _openDimmerSettingsButton.OnClicked.AddListener(
                OnOpenDimmerSettingsButtonClicked);
            _colorPickerCancelButton.OnClicked.AddListener(
                OnCancelButtonClicked);

            _strokeColorPicker.OnColorUpdated.AddListener(OnStrokeColorPickerColorChanged);
            _fillToggle.IsToggled.OnEntered.AddListener(OnFillToggled);
            _fillToggle.IsToggled.OnExited.AddListener(OnFillToggled);
            _fillColorPicker.OnColorUpdated.AddListener(OnFillColorPickerColorChanged);
            _fillDimToggle.IsToggled.OnEntered.AddListener(OnFillDimToggled);
            _fillDimToggle.IsToggled.OnExited.AddListener(OnFillDimToggled);
            _fillDimSlider.OnValueUpdated.AddListener(OnFillDimSliderChanged);
            _fillAlphaSlider.OnValueUpdated.AddListener(OnFillAlphaSliderChanged);

            HandleBrushColorsChanged();
            _toolManager.OnBrushColorsChanged += HandleBrushColorsChanged;

            if (_fillToggle.IsToggled != (_toolManager.FillColor != Color.clear))
            {
                _fillToggle.ForceSetToggled(!_fillToggle.IsToggled, false);
            }
            UpdateColorPickerFillVisibility();
        }

        private void OnEnable()
        {
            CheckSegmentedDimmerEnabled();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                CheckSegmentedDimmerEnabled();
            }
        }

        private void HandleBrushColorsChanged()
        {
            if (!ColorUtils.Color32sEqual(_strokeColorPicker.Color, _toolManager.StrokeColor,
                    ignoreAlpha: true))
            {
                _strokeColorPicker.SetColor(_toolManager.StrokeColor, fireEvents: false);
            }

            if (!ColorUtils.Color32sEqual(_fillColorPicker.Color, _toolManager.FillColor,
                    ignoreAlpha: true))
            {
                _fillColorPicker.SetColor(_toolManager.FillColor, fireEvents: false);
            }

            _fillAlphaSlider.OnValueUpdated.RemoveListener(OnFillAlphaSliderChanged);
            _fillAlphaSlider.Value = ((Color) _toolManager.FillColor).a;
            _fillAlphaSlider.OnValueUpdated.AddListener(OnFillAlphaSliderChanged);

            _fillDimSlider.OnValueUpdated.RemoveListener(OnFillDimSliderChanged);
            _fillDimSlider.Value = _toolManager.FillDimmerAlpha;
            _fillDimSlider.OnValueUpdated.AddListener(OnFillDimSliderChanged);
        }

        private void OnStrokeColorPickerColorChanged(Color newColor)
        {
            _toolManager.SetStrokeColor(newColor, true);
        }

        private void OnFillColorPickerColorChanged(Color newColor)
        {
            UpdateBrushColorManagerFillColor();
        }

        private void OnFillToggled(float _)
        {
            if (_toolManager.FillColor == Color.clear && _fillToggle.IsToggled)
            {
                _toolManager.SetFillColor(_toolManager.StrokeColor);
            }
            UpdateBrushColorManagerFillColor();
            UpdateBrushColorManagerFillDimmerAlpha();
            UpdateColorPickerFillVisibility();
        }

        private void OnFillDimToggled(float _)
        {
            UpdateBrushColorManagerFillDimmerAlpha();
            UpdateColorPickerFillVisibility();
        }

        private void OnFillDimSliderChanged(SliderEventData _)
        {
            UpdateBrushColorManagerFillDimmerAlpha();
        }

        private void OnFillAlphaSliderChanged(SliderEventData _)
        {
            UpdateBrushColorManagerFillColor();
        }

        private void UpdateBrushColorManagerFillColor()
        {
            Color fillColor = Color.clear;
            if (_fillToggle.IsToggled && _fillAlphaSlider.Value > 0)
            {
                fillColor = _fillColorPicker.Color;
                fillColor.a = _fillAlphaSlider.Value;
            }
            _toolManager.SetFillColor(fillColor);
        }

        private void UpdateBrushColorManagerFillDimmerAlpha()
        {
            _toolManager.SetFillDimmerAlpha(
                _fillToggle.IsToggled && _fillDimToggle.IsToggled ? _fillDimSlider.Value : 0);
        }

        private void UpdateColorPickerFillVisibility()
        {
            _colorPickerFillPanel.SetActive(_fillToggle.IsToggled);
            _fillDimSlider.gameObject.SetActive(_fillDimToggle.IsToggled);
        }

        private void OnCancelButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(Hide);
        }

        private void OnOpenDimmerSettingsButtonClicked()
        {
            try
            {
                using (AndroidJavaClass activityClass = new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>(
                           "currentActivity"))
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                           "android.settings.DISPLAY_SETTINGS"))
                {
                    activity.Call("startActivity", intent);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while launching display settings: " + e);
            }
        }

        private void CheckSegmentedDimmerEnabled()
        {
            ThreadDispatcher.ScheduleWork(() =>
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                bool dimmerEnabled = false;
                try
                {
                    using (AndroidJavaClass activityClass = new AndroidJavaClass(
                               "com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>(
                               "currentActivity"))
                    using (AndroidJavaObject contentResolver = activity.Call<AndroidJavaObject>(
                               "getContentResolver"))
                    using (AndroidJavaClass systemSettings = new AndroidJavaClass(
                               "android.provider.Settings$System"))
                    {
                        int segmentedDimmerEnabledValue = systemSettings.CallStatic<int>(
                            "getInt", contentResolver,
                            "is_segmented_dimmer_enabled", 0);
                        dimmerEnabled = (segmentedDimmerEnabledValue != 0);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while checking segmented dimmer feature: " + e);
                }
#else
                bool dimmerEnabled = true;
#endif

                ThreadDispatcher.ScheduleMain(() =>
                {
                    _panelFillDimAvailable.SetActive(dimmerEnabled);
                    _panelFillDimNotAvailable.gameObject.SetActive(!dimmerEnabled);
                });
            });
        }
    }
}
