using System;
using MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace MagicLeap.LeapBrush
{
    public class ColorPickerSaturationBrightnessQuad : StatefulInteractable, ISnapInteractable
    {
        #region Public Members
        public event Action OnValueUpdated;
        public float Saturation => _value.x;
        public float Brightness => _value.y;
        #endregion Public Members

        #region ISnapInteractable
        [SerializeField]
        [Tooltip("Transform of the handle affordance")]
        private Transform _handleTransform;

        /// <inheritdoc/>
        public Transform HandleTransform => _handleTransform;
        #endregion ISnapInteractable

        #region [SerializeField] Private Members
        [SerializeField]
        private float _hue;
        #endregion [SerializeField] Private Members

        #region Private Members
        /// <summary>
        /// Vector2 of (Brightness, Saturation)
        /// </summary>
        private Vector2 _value = Vector2.one;
        private float _textureHue;
        private Texture2D _texture;
        private Material _material;
        private const int TextureSize = 256;
        private Vector2 _startInteractionValue;
        private Vector3 _startInteractionPoint;
        private RectTransform _rectTransform;
        #endregion Private Members

        #region MonoBehaviour Methods
        private void Awake()
        {
            base.Awake();
            ApplyRequiredSettings();
        }

        private void Reset()
        {
            base.Reset();
            ApplyRequiredSettings();
        }

        private void OnValidate()
        {
            ApplyRequiredSettings();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _rectTransform = GetComponent<RectTransform>();
        }
        #endregion MonoBehaviour Methods

        #region Public Methods
        public void SetHue(float hue)
        {
            _hue = hue;
            if (_hue != _textureHue)
            {
                RegenerateTexture();
            }
        }

        public void SetColor(Color color, bool fireEvents = true)
        {
            float newHue;
            Vector2 newValue;
            Color.RGBToHSV(color, out newHue, out newValue.x, out newValue.y);
            SetHue(newHue);

            SetValue(newValue, fireEvents: fireEvents);
        }
        #endregion Public Methods

        #region Private Methods
        private void SetValue(Vector2 newValue, bool fireEvents = true)
        {
            _value = newValue;

            _handleTransform.localPosition = new Vector3(
                (_value.x - 0.5f) * _rectTransform.rect.width,
                (_value.y - 0.5f) * _rectTransform.rect.height);

            if (fireEvents)
            {
                OnValueUpdated?.Invoke();
            }
        }

        private void RegenerateTexture()
        {
            _textureHue = _hue;

            if (_texture == null)
            {
                _material = new Material(GetComponent<Image>().material);

                _texture = new(TextureSize, TextureSize,
                    TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };

                _material.mainTexture = _texture;
                GetComponent<Image>().material = _material;
            }

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
        }

        void ApplyRequiredSettings()
        {
            // Use InteractableSelectMode.Single to ignore incoming interactors after a
            // first/valid interactor has been acquired.
            selectMode = InteractableSelectMode.Single;
        }

        private void UpdateValue()
        {
            Vector3 interactionPoint = interactorsSelecting[0].GetAttachTransform(this).position;
            Vector3 interactorDelta = interactionPoint - _startInteractionPoint;

            Vector3 interactorDeltaLocal = transform.InverseTransformVector(interactorDelta);

            SetValue(new Vector2(
                Mathf.Clamp(_startInteractionValue.x + interactorDeltaLocal.x
                    / _rectTransform.rect.width, 0, 1.0f),
                Mathf.Clamp(_startInteractionValue.y + interactorDeltaLocal.y
                    / _rectTransform.rect.height, 0, 1.0f)));
        }
        #endregion Private Methods

        #region XRI methods

        /// <inheritdoc />
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // Snap to position by setting the startPosition to the center of the component
            // and setting the value to match.
            if (!(args.interactorObject is IGrabInteractor))
            {
                _startInteractionPoint = transform.position;
                _startInteractionValue = Vector2.one * 0.5f;
            }
            else
            {
                _startInteractionPoint = args.interactorObject.GetAttachTransform(this).position;
                _startInteractionValue = _value;
            }
        }

        /// <inheritdoc />
        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            // Only allow the first interactor selecting the slider to be able to control it.
            if (isSelected)
            {
                return base.IsSelectableBy(interactor) && interactor == interactorsSelecting[0];
            }

            // Don't allow grabbing -- rely on poking instead.
            if (interactor is IGrabInteractor)
            {
                return false;
            }

            return base.IsSelectableBy(interactor);
        }

        ///<inheritdoc />
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected)
            {
                UpdateValue();
            }
        }

        #endregion
    }
}