using System;
using System.Collections;
using MagicLeap.LeapBrush;
using MixedReality.Toolkit.SpatialManipulation;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace MagicLeap
{
    /// <summary>
    /// A 3D model imported into the scene.
    /// </summary>
    public class External3DModel : MonoBehaviour
    {
        [HideInInspector]
        public string Id;

        [HideInInspector]
        public string AnchorId;

        [SerializeField]
        private GameObject _statusCanvas;

        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private GameObject _loadingCube;

        [SerializeField]
        private float _rotationSpeed = 75;

        [SerializeField]
        private ObjectManipulator _objectManipulator;

        [SerializeField]
        private GameObject _boundsVisualsPrefab;

        [SerializeField]
        SelectEnterEvent _boundsControlManipulationStarted = new();

        [SerializeField]
        SelectExitEvent _boundsControlManipulationEnded = new();

        public event Action<External3DModel> OnTransformChanged;

        public string FileName => _fileName;

        public TransformProto TransformProto => _transformProto;

        public event Action OnDestroyed;

        /// <summary>
        /// Whether to restrict the initial dimensions of this model once loaded.
        /// </summary>
        [HideInInspector]
        public bool RestrictInitialModelDimensions;

        private string _fileName;
        private Quaternion _loadingCubeInitialRotation;
        private TransformProto _transformProto;
        private GameObject _model;
        private bool _failed;
        private IEnumerator _delayLoadingCubeCoroutine;
        private bool _started;
        private float _statusCanvasCenterOffset;
        private BoundsControl _boundsControl;

        private IEnumerator _enableManipulationAfterDelayCoroutine;

        private const float MinInitialModelDimension = 0.05f;
        private const float MaxInitialModelDimension = 2.0f;

        public void Start()
        {
            _loadingCubeInitialRotation = _loadingCube.transform.rotation;

            _loadingCube.SetActive(false);
            _delayLoadingCubeCoroutine = ShowLoadingCubeWithDelayCoroutine();
            StartCoroutine(_delayLoadingCubeCoroutine);

            _statusCanvasCenterOffset = _statusCanvas.transform.localPosition.magnitude;

            _started = true;
            if (_model != null)
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

            if (_model != null && (_objectManipulator.isSelected ||
                                   (_boundsControl != null && _boundsControl.HandlesActive)))
            {
                UpdateTransformProtoDispatchIfChanged();
            }

            if (_statusCanvas.activeSelf)
            {
                Vector3 toCamera =
                    Camera.main.transform.position - transform.position;
                _statusCanvas.transform.position =
                    transform.position + toCamera.normalized * _statusCanvasCenterOffset;
                _statusCanvas.transform.LookAt(
                    _statusCanvas.transform.position - toCamera, Vector3.up);
            }
        }

        private void UpdateTransformProtoDispatchIfChanged()
        {
            if (ProtoUtils.EpsilonEquals(transform, _transformProto))
            {
                return;
            }

            _transformProto = ProtoUtils.ToProto(transform);
            OnTransformChanged?.Invoke(this);
        }

        public void Initialize(string fileName)
        {
            _fileName = fileName;
            _statusText.text = string.Format("Loading {0}...", fileName);
            _loadingCube.GetComponent<Renderer>().material.color = new Color(0, 0.7f, 0);
        }

        public void SetPoseAndScale(ProtoUtils.PoseAndScale poseAndScale)
        {
            if (ProtoUtils.EpsilonEquals(transform, poseAndScale))
            {
                return;
            }

            transform.SetLocalPose(poseAndScale.Pose);
            transform.localScale = poseAndScale.Scale;

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_boundsControl != null)
            {
                _boundsControl.HandlesActive = false;
            }

            // Disable object manipulation until other users have stopped moving the object.
            _objectManipulator.enabled = false;

            if (_enableManipulationAfterDelayCoroutine != null)
            {
                StopCoroutine(_enableManipulationAfterDelayCoroutine);
            }

            _enableManipulationAfterDelayCoroutine = EnableManipulationAfterDelayCoroutine();
            StartCoroutine(_enableManipulationAfterDelayCoroutine);
        }

        public void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        private IEnumerator ShowLoadingCubeWithDelayCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            if (_model == null)
            {
                _loadingCube.SetActive(true);
            }
        }

        public void OnLoadCompleted(GameObject model)
        {
            if (_delayLoadingCubeCoroutine != null)
            {
                StopCoroutine(_delayLoadingCubeCoroutine);
                _delayLoadingCubeCoroutine = null;
            }

            _model = model;

            if (_started)
            {
                OnLoadedAndStarted();
            }
        }

        private void OnLoadedAndStarted()
        {
            _transformProto = ProtoUtils.ToProto(transform);

            if (RestrictInitialModelDimensions)
            {
                SetInitialModelScale();
            }

            // Reload colliders now that the 3D model has been loaded.
            _objectManipulator.colliders.Clear();
            _model.GetComponentsInChildren(_objectManipulator.colliders);

            // Toggle the object manipulator XR Interactable to re-register updated colliders.
            _objectManipulator.enabled = false;
            _objectManipulator.enabled = true;

            // Add the bounds control so that it initializes with a valid absolute minimum scale.
            _boundsControl = transform.AddComponent<BoundsControl>();
            _boundsControl.EnabledHandles =
                HandleType.Rotation | HandleType.Scale | HandleType.Translation;
            _boundsControl.RotateAnchor = RotateAnchorType.ObjectOrigin;
            _boundsControl.BoundsVisualsPrefab = _boundsVisualsPrefab;
            _boundsControl.ManipulationStarted.AddListener(_boundsControlManipulationStarted.Invoke);
            _boundsControl.ManipulationEnded.AddListener(_boundsControlManipulationEnded.Invoke);

            _statusCanvas.gameObject.SetActive(false);
            _loadingCube.SetActive(false);

            UpdateTransformProtoDispatchIfChanged();
        }

        private IEnumerator EnableManipulationAfterDelayCoroutine()
        {
            yield return new WaitForSeconds(0.5f);

            _objectManipulator.enabled = true;
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

            float initialScale = maxDimension < MinInitialModelDimension
                ? MinInitialModelDimension / maxDimension
                : (maxDimension > MaxInitialModelDimension
                    ? MaxInitialModelDimension / maxDimension
                    : transform.localScale.x);
            transform.localScale = Vector3.one * initialScale;
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