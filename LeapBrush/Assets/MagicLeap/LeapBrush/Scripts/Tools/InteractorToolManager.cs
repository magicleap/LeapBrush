using System;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manager for tools attached to a single XR Controller, e.g. Left Hand, Right Hand, and
    /// Motion Controller.
    /// </summary>
    public class InteractorToolManager : MonoBehaviour
    {
        public ScribbleBrushTool ScribbleBrush => _scribbleBrush;
        public PolyBrushTool PolyBrush => _polyBrush;
        public EraserTool Eraser => _eraser;
        public Transform ToolOffset => _toolOffset;

        /// <summary>
        /// Whether the XR controller backing these tools is actively tracking
        /// </summary>
        public bool IsTrackingActive =>
            (_actionBasedController.currentControllerState.inputTrackingState
             & RequiredTrackingStates) == RequiredTrackingStates;

        /// <summary>
        /// Whether the tools for this manager are currently visible.
        /// </summary>
        public bool ToolsVisible =>
            !_isHand || _activeTool == ToolType.Laser || _preferences.HandToolsEnabled.Value;

        /// <summary>
        /// The progress towards selection (e.g. how far down the trigger button is pressed).
        /// </summary>
        public float SelectProgress =>
            _actionBasedController.currentControllerState.selectInteractionState.value;

        [Header("External Dependencies")]

        [SerializeField]
        [Tooltip("The ray interactor for which these tools are associated.")]
        private XRRayInteractor _rayInteractor;

        [SerializeField]
        private LineRenderer _rayLineRenderer;

        [SerializeField]
        private ActionBasedController _actionBasedController;

        [SerializeField]
        private GazePinchInteractor _gazePinchInteractor;

        [SerializeField]
        private GrabInteractor _grabInteractor;

        [SerializeField]
        private LeapBrushPreferences _preferences;

        [SerializeField]
        private GameObject _instructions;

        [SerializeReference]
        [InterfaceSelector(true)]
        [Tooltip("The pose source representing the pinch pose")]
        private IPoseSource _pinchPoseSource;

        [Header("Internal Dependencies")]

        [SerializeField]
        private ScribbleBrushTool _scribbleBrush;

        [SerializeField]
        private PolyBrushTool _polyBrush;

        [SerializeField]
        private EraserTool _eraser;

        [SerializeField, Tooltip(
             "The base pose for tools that can have a relative offset from the Controller")]
        private Transform _toolOffset;

        [SerializeField]
        private Pose _toolOffsetFromPoseSource;

        [SerializeField]
        private LayerMask _defaultRayInteractorLayerMask = -1;

        [SerializeField]
        private InteractionLayerMask _defaultGrabInteractorInteractionLayers = -1;

        private const InputTrackingState RequiredTrackingStates =
            InputTrackingState.Rotation | InputTrackingState.Position;

        private MRTKLineVisual _lineVisual;
        private ToolType _activeTool = ToolType.Laser;
        private bool _isHand;
        private float _toolOffsetZ;
        private Vector3[] _cachedRayLinePoints = Array.Empty<Vector3>();

        /// <summary>
        /// Set which tool is currently active for this tool manager.
        /// </summary>
        /// <param name="activeTool">The currently active tool</param>
        public void SetActiveTool(ToolType activeTool)
        {
            _activeTool = activeTool;

            UpdateToolActiveStates();
            UpdateLaserRaysEnabled();
            UpdateGazePinchEnabled();
        }

        /// <summary>
        /// Set the z offset for tools attached to this manager.
        /// </summary>
        /// <param name="toolOffsetZ">The new z offset</param>
        public void SetToolOffsetZ(float toolOffsetZ)
        {
            _toolOffsetZ = toolOffsetZ;
        }

        /// <summary>
        /// Get the points that make up the laser pointer ray attached to this manager, or an
        /// empty array if the laser is not visible.
        /// </summary>
        /// <param name="relativeTransform">The relative transform to returns points against</param>
        /// <returns>
        /// A temporary array containing the results. The array is reused for future
        /// calls to this function.
        /// </returns>
        public Vector3[] GetRayLinePointsRelative(Transform relativeTransform)
        {
            if (_activeTool != ToolType.Laser)
            {
                return Array.Empty<Vector3>();
            }

            if (_cachedRayLinePoints.Length != _rayLineRenderer.positionCount)
            {
                _cachedRayLinePoints = new Vector3[_rayLineRenderer.positionCount];
            }

            _rayLineRenderer.GetPositions(_cachedRayLinePoints);

            for (int i = 0; i < _cachedRayLinePoints.Length; i++)
            {
                Vector3 positionWorld = _rayLineRenderer.useWorldSpace ?
                    _cachedRayLinePoints[i] :
                    _rayLineRenderer.transform.TransformPoint(_cachedRayLinePoints[i]);
                _cachedRayLinePoints[i] = relativeTransform.InverseTransformPoint(positionWorld);
            }

            return _cachedRayLinePoints;
        }

        private void Awake()
        {
            _lineVisual = _rayInteractor.GetComponent<MRTKLineVisual>();
            _isHand = _actionBasedController is ArticulatedHandController;
        }

        private void Update()
        {
            XRControllerState controllerState = _actionBasedController.currentControllerState;
            _toolOffset.gameObject.SetActive(IsTrackingActive);
            if (_toolOffset.gameObject.activeSelf)
            {
                Pose toolOffsetPoseLocal = _toolOffsetFromPoseSource;
                toolOffsetPoseLocal.position.z += _toolOffsetZ;

                if (_isHand)
                {
                    Pose toolOffsetPose = _rayInteractor.transform.TransformPose(toolOffsetPoseLocal);
                    if (_pinchPoseSource.TryGetPose(out Pose pinchPose))
                    {
                        toolOffsetPose.rotation = pinchPose.rotation * toolOffsetPoseLocal.rotation;
                    }
                    ToolOffset.transform.SetWorldPose(toolOffsetPose);
                }
                else
                {
                    Pose toolOffsetPose = _rayInteractor.transform.TransformPose(toolOffsetPoseLocal);
                    ToolOffset.transform.SetWorldPose(toolOffsetPose);
                }
            }

            if (_instructions != null)
            {
                _instructions.SetActive(IsTrackingActive);
            }

            if (controllerState.selectInteractionState.activatedThisFrame)
            {
                if (ScribbleBrush.gameObject.activeSelf)
                {
                    ScribbleBrush.OnSelectStarted();
                }
                if (PolyBrush.gameObject.activeSelf)
                {
                    PolyBrush.OnSelectStarted();
                }
            }

            if (controllerState.selectInteractionState.deactivatedThisFrame)
            {
                if (ScribbleBrush.gameObject.activeSelf)
                {
                    ScribbleBrush.OnSelectEnded();
                }
                if (PolyBrush.gameObject.activeSelf)
                {
                    PolyBrush.OnSelectEnded();
                }
            }
        }

        private void OnEnable()
        {
            _preferences.GazePinchEnabled.OnChanged += OnGazePinchPreferenceChanged;
            OnGazePinchPreferenceChanged();

            _preferences.HandLasersEnabled.OnChanged += OnHandLasersPreferenceChanged;
            OnHandLasersPreferenceChanged();

            _preferences.HandToolsEnabled.OnChanged += HandToolsPreferenceChanged;
            HandToolsPreferenceChanged();
        }

        private void OnDisable()
        {
            _preferences.GazePinchEnabled.OnChanged -= OnGazePinchPreferenceChanged;
            _preferences.HandLasersEnabled.OnChanged -= OnHandLasersPreferenceChanged;
            _preferences.HandToolsEnabled.OnChanged -= HandToolsPreferenceChanged;
        }

        private void OnGazePinchPreferenceChanged()
        {
            UpdateGazePinchEnabled();
        }

        private void OnHandLasersPreferenceChanged()
        {
            UpdateLaserRaysEnabled();
        }

        private void HandToolsPreferenceChanged()
        {
            UpdateToolActiveStates();
        }

        private void UpdateLaserRaysEnabled()
        {
            bool laserEnabled = _activeTool == ToolType.Laser
                                && (!_isHand || _preferences.HandLasersEnabled.Value);
            _rayInteractor.raycastMask = laserEnabled ? _defaultRayInteractorLayerMask : 0;
            _lineVisual.enabled = laserEnabled;
            _grabInteractor.interactionLayers =
                laserEnabled ? _defaultGrabInteractorInteractionLayers : 0;
        }

        private void UpdateGazePinchEnabled()
        {
            if (_gazePinchInteractor == null)
            {
                return;
            }

            _gazePinchInteractor.gameObject.SetActive(_preferences.GazePinchEnabled.Value
                                                      && _activeTool == ToolType.Laser);
        }

        private void UpdateToolActiveStates()
        {
            bool toolsDisabled = _isHand && !_preferences.HandToolsEnabled.Value;
            ScribbleBrush.gameObject.SetActive(
                _activeTool == ToolType.BrushScribble && !toolsDisabled);
            PolyBrush.gameObject.SetActive(_activeTool == ToolType.BrushPoly && !toolsDisabled);
            Eraser.gameObject.SetActive(_activeTool == ToolType.Eraser && !toolsDisabled);
        }
    }
}