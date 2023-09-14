using System;
using UnityEngine;
using UnityEngine.Events;

namespace MagicLeap.LeapBrush
{
    public class ColorPicker : MonoBehaviour
    {
        #region Events

        [SerializeField]
        public UnityEvent<Color> OnColorUpdated;

        #endregion

        #region Public Members
        public Color Color => _color;
        #endregion Public Members

        #region [SerializeField] Private Members
        [SerializeField]
        private Color _color = Color.white;
        [SerializeField]
        private ColorPickerHueRing _hueRing;
        [SerializeField]
        private ColorPickerSaturationBrightnessQuad _saturationBrightnessQuad;
        #endregion [SerializeField] Private Members

        #region MonoBehaviour Methods
        private void OnEnable()
        {
            _hueRing.OnHueUpdated += OnHueUpdated;
            _saturationBrightnessQuad.OnValueUpdated += OnSaturationBrightnessUpdated;

            SetColor(_color, fireEvents: false);
        }

        private void OnDisable()
        {
            _hueRing.OnHueUpdated -= OnHueUpdated;
            _saturationBrightnessQuad.OnValueUpdated -= OnSaturationBrightnessUpdated;
        }
        #endregion MonoBehaviour Methods

        #region Public Methods
        public void SetColor(Color color, bool fireEvents = true)
        {
            if (_color != color)
            {
                _color = color;

                float hue;
                float saturation;
                float brightness;
                Color.RGBToHSV(color, out hue, out saturation, out brightness);
                _hueRing.SetHue(hue, fireEvents: false);
                _saturationBrightnessQuad.SetColor(color, fireEvents: false);
            }

            if (fireEvents)
            {
                OnColorUpdated?.Invoke(_color);
            }
        }
        #endregion Public Methods

        #region Private Methods
        private void OnHueUpdated()
        {
            _saturationBrightnessQuad.SetHue(_hueRing.Hue);

            UpdateColorAndDispatchChangedEvent();
        }

        private void OnSaturationBrightnessUpdated()
        {
            UpdateColorAndDispatchChangedEvent();
        }

        private void UpdateColorAndDispatchChangedEvent()
        {
            _color = Color.HSVToRGB(_hueRing.Hue, _saturationBrightnessQuad.Saturation,
                _saturationBrightnessQuad.Brightness);
            OnColorUpdated?.Invoke(_color);
        }
        #endregion Private Methods
    }
}