using System;
using MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MagicLeap.LeapBrush
{
    public class ColorPickerHueRing : StatefulInteractable, ISnapInteractable
    {
        #region Public Events
        public event Action OnHueUpdated;
        #endregion Public Events

        #region Public Members
        public float Hue => _hue;
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
        private float _outerRadius = 1.0f;
        [SerializeField]
        private float _innerRadius = 0.8f;
        [SerializeField]
        private int _verticesPerRing = 64;
        [SerializeField]
        private GameObject _ring;
        #endregion [SerializeField] Private Members

        #region Private Members
        private float _meshOuterRadius;
        private float _meshInnerRadius;
        private int _meshVerticesPerRing;
        private float _hue;
        private Mesh _mesh;
        private Texture2D _texture;
        private const int TextureSize = 256;
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
        #endregion MonoBehaviour Methods

        #region Public Methods
        public void SetHue(float hue, bool fireEvents = true)
        {
            _hue = hue;

            float angle = (float) (Math.PI * 2 * hue);
            _handleTransform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0)
                * (_outerRadius + _innerRadius) / 2;

            if (fireEvents)
            {
                OnHueUpdated?.Invoke();
            }
        }

        public void GenerateMesh()
        {
            _meshOuterRadius = _outerRadius;
            _meshInnerRadius = _innerRadius;
            _meshVerticesPerRing = _verticesPerRing;

            _ring.GetComponent<MeshFilter>().sharedMesh = null;
            _ring.GetComponent<MeshCollider>().sharedMesh = null;

            if (_meshInnerRadius <= 0 || _meshOuterRadius < _meshInnerRadius ||
                _meshVerticesPerRing < 3)
            {
                return;
            }

            _mesh = new Mesh();
            _mesh.name = "Color Picker Hue Ring";
            _mesh.hideFlags = HideFlags.HideAndDontSave;

            Vector3[] vertices = new Vector3[_verticesPerRing * 2 + 2];
            Vector2[] uv = new Vector2[_verticesPerRing * 2 + 2];
            int[] triangles = new int[_verticesPerRing * 6];

            for (int i = 0; i <= _verticesPerRing; i++)
            {
                float angle = (float) (Math.PI * 2 * i) / _verticesPerRing;
                Vector3 position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                vertices[i * 2] = position * _outerRadius;
                vertices[i * 2 + 1] = position * _innerRadius;

                float u = (float) i / _verticesPerRing;
                uv[i * 2] = new Vector2(u, 0);
                uv[i * 2 + 1] = new Vector2(u, 1);
            }

            for (int i = 0; i < _verticesPerRing; i++)
            {
                int vertex0 = i * 2;
                triangles[i * 6] = vertex0;
                triangles[i * 6 + 1] = vertex0 + 1;
                triangles[i * 6 + 2] = vertex0 + 3;
                triangles[i * 6 + 3] = vertex0;
                triangles[i * 6 + 4] = vertex0 + 3;
                triangles[i * 6 + 5] = vertex0 + 2;
            }

            _mesh.vertices = vertices;
            _mesh.uv = uv;
            _mesh.triangles = triangles;

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _ring.GetComponent<MeshFilter>().sharedMesh = _mesh;
            _ring.GetComponent<MeshCollider>().sharedMesh = null;
            _ring.GetComponent<MeshCollider>().sharedMesh = _mesh;
        }
        #endregion Public Methods

        #region Private Methods
        private void GenerateTexture()
        {
            if (_texture != null)
            {
                return;
            }

            _texture = new(TextureSize, TextureSize,
                TextureFormat.RGBA32, false);
            _texture.hideFlags = HideFlags.HideAndDontSave;

            for (int x = 0; x < TextureSize; x++)
            {
                Color color = Color.HSVToRGB((float) x / TextureSize, 1, 1);
                for (int y = 0; y < TextureSize; y++)
                {
                    _texture.SetPixel(x, y, color);
                }
            }
            _texture.Apply();

            GetComponent<MeshRenderer>().sharedMaterial.mainTexture = _texture;
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
            Vector3 interactionPointLocal = transform.InverseTransformPoint(interactionPoint);
            interactionPointLocal.z = 0;

            float angle = Vector3.SignedAngle(
                Vector3.right, interactionPointLocal.normalized, Vector3.forward);

            if (angle < 0)
            {
                angle += 360;
            }

            SetHue(Mathf.Clamp(angle / 360, 0, 1.0f));
        }
        #endregion Private Methods

        #region XRI methods

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