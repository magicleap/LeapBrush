// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Detects the focus distance by utilizing the eye tracking fixation point either
    /// directly or in conjunction with sphere casting colliders in the scene.  If
    /// eye tracking is not used or not available, this detector will fall back to
    /// sphere casting from headpose.
    /// This component expects a MagicLeapCamera to be in the scene and will set
    /// the MagicLeapCamera.StereoConvergencePoint to control focus distance.
    /// </summary>
    public class StereoConvergenceDetector : MonoBehaviour
    {
        #region NestedType / Constructors
        [Serializable]
        public enum EyeTrackingOptions
        {
            DoNotUseEyeTracking_UseHeadpose,
            SphereCastThroughEyeFixationPoint,
            UseEyeFixationPointDirectlyAsFocusPoint
        }
        #endregion NestedType / Constructors

        #region Public Members
        public EyeTrackingOptions EyeTrackingOption
        {
            get => _eyeTrackingOption;
        }
        public float SphereCastInterval
        {
            set => _sphereCastInterval = value;
            get => _sphereCastInterval;
        }
        public float SphereCastRadius
        {
            set => _sphereCastRadius = value;
            get => _sphereCastRadius;
        }
        public LayerMask SphereCastMask
        {
            set => _sphereCastMask = value;
            get => _sphereCastMask;
        }
        public bool ShowDebugVisuals
        {
            get => _showDebugVisuals;
        }
        public Material SphereCastMaterial
        {
            get => _sphereCastMaterial;
        }
        public Material HitPointMaterial
        {
            get => _hitPointMaterial;
        }
        #endregion Public Members

        #region [SerializeField] Private Members
        [Header("Sphere Casting")]
        [SerializeField]
        [Tooltip("Choose if eye tracking is used at all along with how to utilize the eye fixation point.  " +
                 "Headpose vector will provide a fall back if eye tracking is not used or not available.")]
        private EyeTrackingOptions _eyeTrackingOption = EyeTrackingOptions.SphereCastThroughEyeFixationPoint;
        [SerializeField]
        [Tooltip("The interval in seconds between detecting the focus point via sphere cast or direct eye fixation point.")]
        private float _sphereCastInterval = .1f;
        [SerializeField]
        [Tooltip("The radius to use for the sphere cast when sphere casting is used.")]
        private float _sphereCastRadius = .075f;
        [SerializeField]
        [Tooltip("The layer mask for the sphere cast.")]
        private LayerMask _sphereCastMask;
        [Header("Debug Visuals")]
        [SerializeField]
        [Tooltip("Whether to show debug visuals for focus point detection.")]
        private bool _showDebugVisuals = false;
        [SerializeField]
        [Tooltip("Material representing sphere cast radius and focus point location.")]
        private Material _sphereCastMaterial;
        [SerializeField]
        [Tooltip("Material representing sphere cast hit point.")]
        private Material _hitPointMaterial;
        #endregion [SerializeField] Private Members

        #region Private Members
        private GameObject _convergencePoint = null;
        private GameObject _sphereCastVisual = null;
        private GameObject _hitPointVisual = null;
        private MagicLeapCamera _magicLeapCamera = null;
        private Coroutine _raycastRoutine = null;
        private MagicLeapInputs _mlInputs;
        private MagicLeapInputs.EyesActions _eyesActions;
        private InputDevice _eyesDevice;
        private readonly MLPermissions.Callbacks _permissionCallbacks = new MLPermissions.Callbacks();
        #endregion Private Members

        #region MonoBehaviour Methods
        private void Awake()
        {
            _permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            _permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            _permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

            SetupConvergencePointObject();
        }

        private void Start()
        {
            _mlInputs = new MagicLeapInputs();
            _mlInputs.Enable();

            // Request EyeTracking when an eye tracking option is selected
            if (_eyeTrackingOption != EyeTrackingOptions.DoNotUseEyeTracking_UseHeadpose)
            {
                MLPermissions.RequestPermission(MLPermission.EyeTracking, _permissionCallbacks);
            }

            _magicLeapCamera = FindObjectOfType<MagicLeapCamera>();
            if (_magicLeapCamera == null)
            {
                Debug.LogWarning("No MagicLeapCamera component found, will not be able to set stereo convergence point.");
            }
        }

        private void OnEnable()
        {
            _raycastRoutine = StartCoroutine(DetectConvergencePoint());
        }

        private void OnDisable()
        {
            if (_raycastRoutine != null)
            {
                StopCoroutine(_raycastRoutine);
                _raycastRoutine = null;
            }

            if (_showDebugVisuals)
            {
                DisplayDebugVisuals(false);
            }
        }

        private void OnDestroy()
        {
            _permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            _permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            _permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            _mlInputs.Disable();
            _mlInputs.Dispose();

            if (_raycastRoutine != null)
            {
                StopCoroutine(_raycastRoutine);
                _raycastRoutine = null;
            }

            if (_convergencePoint != null)
            {
                Destroy(_convergencePoint);
                _convergencePoint = null;
            }
        }
        #endregion MonoBehaviour Methods

        #region Private Methods
        private void SetupConvergencePointObject()
        {
            // Empty game object to represent the transform for the stereo convergence point
            _convergencePoint = new GameObject("Stereo Convergence Point");

            // Create visuals representing the sphere cast radius and hit point
            if (_showDebugVisuals)
            {
                Func<Material, GameObject> createSpherePrimitive = (Material material) =>
                {
                    GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    primitive.layer = this.gameObject.layer;
                    primitive.transform.SetParent(_convergencePoint.transform);
                    primitive.SetActive(false);
                    if (material != null)
                    {
                        primitive.GetComponent<Renderer>().material = material;
                    }

                    // Remove collider to not interfere with scene
                    Collider collider = primitive.GetComponent<Collider>();
                    if (collider != null)
                    {
                        Destroy(collider);
                    }

                    return primitive;
                };

                _sphereCastVisual = createSpherePrimitive(_sphereCastMaterial);
                _hitPointVisual = createSpherePrimitive(_hitPointMaterial);
            }
        }

        private IEnumerator DetectConvergencePoint()
        {
            YieldInstruction intervalDelay = _sphereCastInterval > 0 ?
                new WaitForSeconds(_sphereCastInterval) :
                null;

            while (true)
            {
                yield return intervalDelay;

                bool focusPointDetected = false;
                Vector3 focusPoint = Vector3.zero;
                bool useSphereCast = true;

                // Default Headpose parameters for sphere cast
                Vector3 rayOrigin = Camera.main.transform.position;
                Vector3 rayDirection = Camera.main.transform.forward;

                // Eye Tracking option
                if (_eyeTrackingOption != EyeTrackingOptions.DoNotUseEyeTracking_UseHeadpose &&
                    MLPermissions.CheckPermission(MLPermission.EyeTracking).IsOk)
                {
                    if (!_eyesDevice.isValid)
                    {
                        _eyesActions = new MagicLeapInputs.EyesActions(_mlInputs);
                        _eyesDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
                    }

                    var eyes = _eyesActions.Data.ReadValue<UnityEngine.InputSystem.XR.Eyes>();
                    InputSubsystem.Extensions.TryGetEyeTrackingState(_eyesDevice, out var trackingState);

                    if (trackingState.FixationConfidence > .6f)
                    {
                        switch (_eyeTrackingOption)
                        {
                            case EyeTrackingOptions.UseEyeFixationPointDirectlyAsFocusPoint:
                                focusPoint = eyes.fixationPoint;
                                focusPointDetected = true;
                                useSphereCast = false;
                                rayDirection = (focusPoint - rayOrigin).normalized;
                                break;

                            case EyeTrackingOptions.SphereCastThroughEyeFixationPoint:
                            default:
                                useSphereCast = true;
                                rayDirection = (eyes.fixationPoint - rayOrigin).normalized;
                                break;
                        }
                    }
                }

                if (useSphereCast && Physics.SphereCast(new Ray(rayOrigin, rayDirection), _sphereCastRadius, out RaycastHit hitInfo, Camera.main.farClipPlane, _sphereCastMask))
                {
                    focusPoint = hitInfo.point;
                    focusPointDetected = true;
                }

                if (focusPointDetected)
                {
                    _convergencePoint.transform.position = focusPoint;

                    if (_magicLeapCamera != null)
                    {
                        _magicLeapCamera.StereoConvergencePoint = _convergencePoint.transform;
                    }

                    if (_showDebugVisuals)
                    {
                        DisplayDebugVisuals(true);

                        _sphereCastVisual.transform.localScale = Vector3.one * _sphereCastRadius * 2.0f;
                        _sphereCastVisual.transform.position = rayOrigin + Vector3.Project(focusPoint - rayOrigin, rayDirection);

                        _hitPointVisual.transform.localScale = Vector3.one * .02f;
                        _hitPointVisual.transform.position = focusPoint;
                    }
                }
                else
                {
                    if (_magicLeapCamera != null)
                    {
                        _magicLeapCamera.StereoConvergencePoint = null;
                    }

                    if (_showDebugVisuals)
                    {
                        DisplayDebugVisuals(false);
                    }
                }
            }
        }

        private void DisplayDebugVisuals(bool show)
        {
            if (_sphereCastVisual != null)
            {
                _sphereCastVisual.SetActive(show);
            }

            if (_hitPointVisual != null)
            {
                _hitPointVisual.SetActive(show);
            }
        }

        private void OnPermissionGranted(string permission)
        {
            InputSubsystem.Extensions.MLEyes.StartTracking();
        }

        private void OnPermissionDenied(string permission)
        {
            MLPluginLog.Error($"{permission} denied, falling back to Headpose sphere cast.");
        }
        #endregion Private Methods
    }
}
