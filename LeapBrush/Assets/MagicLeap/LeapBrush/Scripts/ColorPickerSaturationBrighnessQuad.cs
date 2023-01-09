using System;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;

namespace MagicLeap
{
    [ExecuteAlways]
    [RequireComponent(typeof(Interactable))]
    public class ColorPickerSaturationBrighnessQuad : MonoBehaviour
    {
        [SerializeField]
        private float _hue = 0.0f;

        [SerializeField]
        Transform _handle;

        public event Action OnSaturationBrightnessChanged;
        public float Saturation => _saturation;
        public float Brightness => _brightness;

        private float _saturation = 1.0f;
        private float _brightness = 1.0f;

        private float _textureHue;
        private Texture2D _texture;

        private const int TextureSize = 256;

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                GetComponent<Interactable>().Events.OnSelect.AddListener(HandleSelect);
            }

            RegenerateTexture();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                GetComponent<Interactable>().Events.OnSelect.RemoveListener(HandleSelect);
            }
        }

        public void Update()
        {
            if (!Application.isPlaying)
            {
                if (_texture == null || _textureHue != _hue)
                {
                    RegenerateTexture();
                }
            }
        }

        public void SetHue(float hue)
        {
            _hue = hue;
            if (_hue != _textureHue)
            {
                RegenerateTexture();
            }
        }

        public void SetColor(Color color)
        {
            float newHue;
            Color.RGBToHSV(color, out newHue, out _saturation, out _brightness);
            SetHue(newHue);

            if (_handle != null)
            {
                _handle.gameObject.SetActive(true);
                _handle.localPosition = new Vector3(_saturation - 0.5f, _brightness - 0.5f);
            }
        }

        private void HandleSelect(Interactor interactor)
        {
            RayInteractor rayInteractor = interactor as RayInteractor;
            if (rayInteractor != null)
            {
                Vector3 hitPosition = transform.InverseTransformPoint(
                    rayInteractor.CursorEnd.transform.position);

                _saturation = hitPosition.x + 0.5f;
                _brightness = hitPosition.y + 0.5f;
                OnSaturationBrightnessChanged?.Invoke();

                if (_handle != null)
                {
                    _handle.gameObject.SetActive(true);
                    _handle.localPosition = hitPosition;
                }
            }
        }

        private void RegenerateTexture()
        {
            _textureHue = _hue;

            _texture = new(TextureSize, TextureSize,
                TextureFormat.RGBA32, false);
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.hideFlags = HideFlags.HideAndDontSave;

            for (int x = 0; x < TextureSize; x++)
            {
                for (int y = 0; y < TextureSize; y++)
                {
                    Color color = Color.HSVToRGB(
                        _hue, (float) x / TextureSize, (float) y / TextureSize);
                    _texture.SetPixel(x, y, color);
                }
            }
            _texture.Apply();

            if (Application.isPlaying)
            {
                GetComponent<MeshRenderer>().material.mainTexture = _texture;
            }
            else
            {
                GetComponent<MeshRenderer>().sharedMaterial.mainTexture = _texture;
            }
        }
    }
}