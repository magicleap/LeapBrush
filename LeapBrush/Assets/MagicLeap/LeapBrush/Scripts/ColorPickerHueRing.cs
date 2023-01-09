using System;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;

namespace MagicLeap
{
    [ExecuteAlways]
    [RequireComponent(typeof(Interactable))]
    public class ColorPickerHueRing : MonoBehaviour
    {
        [SerializeField]
        private float _outerRadius = 1.0f;

        [SerializeField]
        private float _innerRadius = 0.5f;

        [SerializeField]
        private int _verticesPerRing = 3;

        [SerializeField]
        Transform _handle;

        public event Action OnHueChanged;
        public float Hue => _hue;

        private float _meshOuterRadius;
        private float _meshInnerRadius;
        private int _meshVerticesPerRing;
        private float _hue;

        private Mesh _mesh;
        private Texture2D _texture;

        private const int TextureSize = 256;

        public void SetHue(float hue)
        {
            _hue = hue;

            if (_handle != null)
            {
                _handle.gameObject.SetActive(true);

                float angle = (float) (Math.PI * 2 * hue);
                _handle.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0)
                    * (_outerRadius + _innerRadius) / 2;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                GetComponent<Interactable>().Events.OnSelect.AddListener(HandleSelect);
            }

            MaybeRegenerateMesh();
            MaybeGenerateTexture();
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
                MaybeRegenerateMesh();
            }
        }

        private void MaybeRegenerateMesh()
        {
            if (Application.isPlaying)
            {
                if (_mesh == null)
                {
                    RegenerateMesh();
                }
            }
            else
            {
                if (_mesh == null ||
                    _outerRadius != _meshOuterRadius || _innerRadius != _meshInnerRadius ||
                    _verticesPerRing != _meshVerticesPerRing)
                {
                    RegenerateMesh();
                }
            }
        }

        private void HandleSelect(Interactor interactor)
        {
            RayInteractor rayInteractor = interactor as RayInteractor;
            if (rayInteractor != null)
            {
                Vector3 hitPosition = transform.InverseTransformPoint(
                    rayInteractor.CursorEnd.transform.position);
                hitPosition.z = 0;
                float hitAngle = Vector3.SignedAngle(
                    Vector3.right, hitPosition.normalized, Vector3.forward);

                if (hitAngle < 0)
                {
                    hitAngle += 360;
                }

                _hue = hitAngle / 360;
                OnHueChanged?.Invoke();

                if (_handle != null)
                {
                    _handle.gameObject.SetActive(true);
                    _handle.localPosition = hitPosition;
                }
            }
        }

        private void RegenerateMesh()
        {
            _meshOuterRadius = _outerRadius;
            _meshInnerRadius = _innerRadius;
            _meshVerticesPerRing = _verticesPerRing;

            if (Application.isPlaying)
            {
                GetComponent<MeshFilter>().mesh = null;
            }
            else
            {
                GetComponent<MeshFilter>().sharedMesh = null;
            }

            if (_meshInnerRadius <= 0 || _meshOuterRadius < _meshInnerRadius ||
                _meshVerticesPerRing < 3)
            {
                return;
            }

            _mesh = new Mesh();
            _mesh.name = "Color Picker Ring";
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

            if (Application.isPlaying)
            {
                GetComponent<MeshFilter>().mesh = _mesh;
            }
            else
            {
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }

            GetComponent<MeshCollider>().sharedMesh = null;
            GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        private void MaybeGenerateTexture()
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
    }
}