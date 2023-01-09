using MagicLeap.DesignToolkit.Utilities;
using UnityEngine;

namespace MagicLeap
{
    public class ColorPicker : MonoBehaviour
    {
        [SerializeField]
        private ColorPickerHueRing _hueRing;

        [SerializeField]
        private ColorPickerSaturationBrighnessQuad _saturationBrighnessQuad;

        [System.Serializable]
        public class ColorPickerEvents
        {
            public ColorEvent OnColorChanged;
        }

        public Color Color => _color;
        public ColorPickerEvents Events = new ColorPickerEvents();

        Color _color = Color.white;

        public void SetColor(Color color)
        {
            _color = color;

            float hue;
            float saturation;
            float brightness;
            Color.RGBToHSV(color, out hue, out saturation, out brightness);
            _hueRing.SetHue(hue);
            _saturationBrighnessQuad.SetColor(color);
        }

        private void OnEnable()
        {
            _hueRing.OnHueChanged += OnHueChanged;
            _saturationBrighnessQuad.OnSaturationBrightnessChanged += OnSaturationBrightnessChanged;
        }

        private void OnDisable()
        {
            _hueRing.OnHueChanged -= OnHueChanged;
            _saturationBrighnessQuad.OnSaturationBrightnessChanged -= OnSaturationBrightnessChanged;
        }

        private void OnHueChanged()
        {
            _saturationBrighnessQuad.SetHue(_hueRing.Hue);

            UpdateColorAndDispatchChangedEvent();
        }

        private void OnSaturationBrightnessChanged()
        {
            UpdateColorAndDispatchChangedEvent();
        }

        private void UpdateColorAndDispatchChangedEvent()
        {
            _color = Color.HSVToRGB(_hueRing.Hue, _saturationBrighnessQuad.Saturation,
                _saturationBrighnessQuad.Brightness);
            Events.OnColorChanged?.Invoke(_color);
        }
    }
}