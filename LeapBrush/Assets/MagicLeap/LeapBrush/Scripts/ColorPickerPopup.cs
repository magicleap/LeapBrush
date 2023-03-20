using System;
using MagicLeap.DesignToolkit.Actions;
using MagicLeap.DesignToolkit.Audio;
using MagicLeap.DesignToolkit.UIKit;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// UI for the Color Picker (Palette) popup.
    /// </summary>
    public class ColorPickerPopup : MonoBehaviour
    {
        [Header("External Dependencies")]

        [SerializeField]
        private BrushColorManager _brushColorManager;

        [Header("Internal Dependencies")]

        [SerializeField]
        private ColorPicker _strokeColorPicker;

        [SerializeField]
        private ColorPicker _fillColorPicker;

        [SerializeField]
        private Toggleable _fillToggle;

        [SerializeField]
        private Toggleable _fillDimToggle;

        [SerializeField]
        private Slider _fillAlphaSlider;

        [SerializeField]
        private Slider _fillDimSlider;

        [SerializeField]
        private GameObject _colorPickerFillPanel;

        [SerializeField]
        private Interactable _colorPickerCancelButton;

        [SerializeField]
        private GameObject _panelFillDimAvailable;

        [SerializeField]
        private GameObject _panelFillDimNotAvailable;

        [SerializeField]
        private Interactable _openDimmerSettingsButton;

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

        void Start()
        {
            _openDimmerSettingsButton.Events.OnSelect.AddListener(
                OnOpenDimmerSettingsButtonSelected);
            _colorPickerCancelButton.Events.OnSelect.AddListener(
                OnCancelButtonSelected);

            _strokeColorPicker.Events.OnColorChanged.AddListener(OnStrokeColorPickerColorChanged);
            _fillColorPicker.Events.OnColorChanged.AddListener(OnFillColorPickerColorChanged);
            _fillDimToggle.Events.On.AddListener(OnFillDimToggled);
            _fillDimToggle.Events.Off.AddListener(OnFillDimToggled);
            _fillDimSlider.events.OnSliderValueChanged.AddListener(OnFillDimSliderChanged);
            _fillAlphaSlider.events.OnSliderValueChanged.AddListener(OnFillAlphaSliderChanged);

            HandleBrushColorsChanged();
            _brushColorManager.OnBrushColorsChanged += HandleBrushColorsChanged;

            if (_fillToggle.IsOn != (_brushColorManager.FillColor != Color.clear))
            {
                _fillToggle.GetComponent<ToggleableAudioHandler>().enabled = false;
                _fillToggle.Toggle();
                _fillToggle.GetComponent<ToggleableAudioHandler>().enabled = true;
            }
            UpdateColorPickerFillVisibility();
            _fillToggle.Events.On.AddListener(OnFillToggled);
            _fillToggle.Events.Off.AddListener(OnFillToggled);
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
            _strokeColorPicker.SetColor(_brushColorManager.StrokeColor);
            _fillColorPicker.SetColor(_brushColorManager.FillColor);

            _fillAlphaSlider.events.OnSliderValueChanged.RemoveListener(OnFillAlphaSliderChanged);
            _fillAlphaSlider.SetSliderValue(((Color) _brushColorManager.FillColor).a);
            _fillAlphaSlider.events.OnSliderValueChanged.AddListener(OnFillAlphaSliderChanged);

            _fillDimSlider.events.OnSliderValueChanged.RemoveListener(OnFillDimSliderChanged);
            _fillDimSlider.SetSliderValue(_brushColorManager.FillDimmerAlpha);
            _fillDimSlider.events.OnSliderValueChanged.AddListener(OnFillDimSliderChanged);
        }

        private void OnStrokeColorPickerColorChanged(Color newColor)
        {
            _brushColorManager.SetStrokeColor(newColor, true);
        }

        private void OnFillColorPickerColorChanged(Color newColor)
        {
            UpdateBrushColorManagerFillColor();
        }

        private void OnFillToggled()
        {
            if (_brushColorManager.FillColor == Color.clear && _fillToggle.IsOn)
            {
                _brushColorManager.SetFillColor(_brushColorManager.StrokeColor);
            }
            UpdateBrushColorManagerFillColor();
            UpdateBrushColorManagerFillDimmerAlpha();
            UpdateColorPickerFillVisibility();
        }

        private void OnFillDimToggled()
        {
            UpdateBrushColorManagerFillDimmerAlpha();
            UpdateColorPickerFillVisibility();
        }

        private void OnFillDimSliderChanged(float dimValue)
        {
            UpdateBrushColorManagerFillDimmerAlpha();
        }

        private void OnFillAlphaSliderChanged(float alphaValue)
        {
            UpdateBrushColorManagerFillColor();
        }

        private void UpdateBrushColorManagerFillColor()
        {
            Color fillColor = Color.clear;
            if (_fillToggle.IsOn && _fillAlphaSlider.SliderValue > 0)
            {
                fillColor = _fillColorPicker.Color;
                fillColor.a = _fillAlphaSlider.SliderValue;
            }
            _brushColorManager.SetFillColor(fillColor);
        }

        private void UpdateBrushColorManagerFillDimmerAlpha()
        {
            _brushColorManager.SetFillDimmerAlpha(
                _fillToggle.IsOn && _fillDimToggle.IsOn ? _fillDimSlider.SliderValue : 0);
        }

        private void UpdateColorPickerFillVisibility()
        {
            _colorPickerFillPanel.SetActive(_fillToggle.IsOn);
            _fillDimSlider.gameObject.SetActive(_fillDimToggle.IsOn);
        }

        private void OnCancelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(Hide);
        }

        private void OnOpenDimmerSettingsButtonSelected(Interactor interactor)
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
