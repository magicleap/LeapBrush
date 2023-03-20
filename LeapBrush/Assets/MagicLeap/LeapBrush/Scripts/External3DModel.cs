using System;
using System.Collections;
using MagicLeap.DesignToolkit.Actions;
using MagicLeap.LeapBrush;
using TMPro;
using UnityEngine;

namespace MagicLeap
{
    /// <summary>
    /// A 3D model imported into the scene.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class External3DModel : MonoBehaviour
    {
        [HideInInspector]
        public string Id;

        [HideInInspector]
        public string AnchorId;

        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private GameObject _loadingCube;

        [SerializeField]
        private float _rotationSpeed = 75;

        public event Action<External3DModel> OnTransformChanged;

        public string FileName => _fileName;

        public TransformProto TransformProto => _transformProto;

        public event Action OnDestroyed;

        private string _fileName;
        private Quaternion _loadingCubeInitialRotation;
        private Interactable _interactable;
        private TransformProto _transformProto;
        private bool _loaded;
        private bool _failed;
        private IEnumerator _delayLoadingCubeCoroutine;
        private bool _started;

        private const float MaxInitialModelDimension = 2.0f;
        private const float MinScaleFactor = 0.1f;
        private const float MaxScaleFactor = 5.0f;

        public void Start()
        {
            _interactable = GetComponent<Interactable>();
            _interactable.Settings.Grabbable = false;
            _loadingCubeInitialRotation = _loadingCube.transform.rotation;

            _loadingCube.SetActive(false);
            _delayLoadingCubeCoroutine = ShowLoadingCubeWithDelayCoroutine();
            StartCoroutine(_delayLoadingCubeCoroutine);

            _started = true;
            if (_loaded)
            {
                OnLoadedAndStarted();
            }
        }

        public void Update()
        {
            if (_loadingCube.activeSelf && !_failed)
            {
                _loadingCube.transform.rotation =
                    Quaternion.AngleAxis(Time.timeSinceLevelLoad * _rotationSpeed, Vector3.up)
                    * _loadingCubeInitialRotation;
            }

            if (_loaded && _interactable.IsGrabbed)
            {
                if (!ProtoUtils.EpsilonEquals(transform, _transformProto))
                {
                    _transformProto = ProtoUtils.ToProto(transform);
                    OnTransformChanged?.Invoke(this);
                }
            }
        }

        public void Initialize(string fileName)
        {
            _fileName = fileName;
            _statusText.text = string.Format("Loading {0}...", fileName);
            _loadingCube.GetComponent<Renderer>().material.color = new Color(0, 0.7f, 0);
        }

        public void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        private IEnumerator ShowLoadingCubeWithDelayCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            if (!_loaded)
            {
                _loadingCube.SetActive(true);
            }
        }

        public void OnLoadCompleted()
        {
            if (_delayLoadingCubeCoroutine != null)
            {
                StopCoroutine(_delayLoadingCubeCoroutine);
                _delayLoadingCubeCoroutine = null;
            }

            _loaded = true;
            if (_started)
            {
                OnLoadedAndStarted();
            }
        }

        private void OnLoadedAndStarted()
        {
            SetInitialModelScale();

            _interactable.Settings.Grabbable = true;
            _statusText.gameObject.SetActive(false);
            _loadingCube.SetActive(false);
            _transformProto = ProtoUtils.ToProto(transform);
        }

        private void SetInitialModelScale()
        {
            Bounds modelBounds = new Bounds();
            bool boundsSet = false;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (!boundsSet)
                {
                    boundsSet = true;
                    modelBounds = renderer.bounds;
                }
                else
                {
                    modelBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!boundsSet)
            {
                return;
            }

            float maxDimension = Mathf.Max(
                modelBounds.size.x * transform.localScale.x,
                Mathf.Max(modelBounds.size.y * transform.localScale.y,
                    modelBounds.size.z * transform.localScale.z));

            float initialScale = maxDimension > MaxInitialModelDimension ? MaxInitialModelDimension / maxDimension
                : transform.localScale.x;
            transform.localScale = Vector3.one * initialScale;
            _interactable.ScaleSettings = Instantiate(_interactable.ScaleSettings);
            _interactable.ScaleSettings.MinScale = initialScale * MinScaleFactor;
            _interactable.ScaleSettings.MaxScale = initialScale * MaxScaleFactor;
            _interactable.ScaleSettings.ConstantLerpSpeed = 1.0f;
        }

        public void OnLoadFailed(bool notFound)
        {
            _failed = true;
            if (notFound)
            {
                _statusText.text = string.Format("Model {0} not found", _fileName);
                _loadingCube.GetComponent<Renderer>().material.color = new Color(0.75f, 0.4f, 0);
            }
            else
            {
                _statusText.text = string.Format("Failed to load {0}", _fileName);
                _loadingCube.GetComponent<Renderer>().material.color = new Color(0.75f, 0, 0);
            }
        }
    }
}