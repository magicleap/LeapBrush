using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MagicLeap.OpenXR.Features;
using MagicLeap.OpenXR.Features.LocalizationMaps;
using MagicLeap.Spectator;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit.Input.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.NativeTypes;
using TransformExtensions = Unity.XR.CoreUtils.TransformExtensions;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The main LeapBrush application.
    /// </summary>
    /// <remarks>
    /// Contains the startup and shutdown logic, brush-stroke and network events handling,
    /// error monitoring, and main panels and application flow control.
    /// </remarks>
    [RequireComponent(typeof(AnchorsManager))]
    [RequireComponent(typeof(SpaceMeshManager))]
    [RequireComponent(typeof(ServerConnectionManager))]
    [RequireComponent(typeof(LeapBrushPreferences))]
    [RequireComponent(typeof(ToolManager))]
    public class LeapBrush : MonoBehaviour
    {
        [Header("UI Panels and Popups")]

        [SerializeField]
        private GameObject _mainMenu;

        [SerializeField]
        private StartPanel _startPanel;

        [SerializeField]
        private MainPanel _mainPanel;

        [SerializeField]
        private SettingsPanel _settingsPanel;

        [SerializeField]
        private NotLocalizedPanel _notLocalizedPanel;

        [SerializeField]
        private NotConnectedPanel _notConnectedPanel;

        [SerializeField]
        private AppTooOldPanel _appTooOldPanel;

        [SerializeField]
        private ServerTooOldPanel _serverTooOldPanel;

        [SerializeField]
        private ImportModelsPopup _importModelsPopup;

        [SerializeField]
        private JoinUserPopup _joinUserPopup;

        [SerializeField]
        private LeapBrushKeyboard _keyboard;

        [SerializeField]
        private HandMenu _handMenu;

        [Header("Input")]

        [SerializeField]
        private InputActionReference _toggleMenuInputAction;

        [SerializeField]
        private InputActionReference _touchpadPosition;

        [SerializeField]
        private ActionBasedController _ml2Controller;

        [SerializeField]
        private GazeInteractor _gazeInteractor;

        [SerializeField]
        private InputSimulator _inputSimulator;

        [Header("Sounds")]

        [SerializeField]
        private GameObject _eraseAudioPrefab;

        [SerializeField]
        private GameObject _userJoinAudioPrefab;

        [Header("Miscellaneous")]

        [SerializeField, Tooltip("The text used to display status information.")]
        private TMP_Text _statusText;

        [SerializeField, Tooltip("Prefab to use for other users being displayed")]
        private GameObject _otherUserPrefab;

        [SerializeField]
        private GameObject _spaceOriginAxis;

        [SerializeField]
        private FloorGrid _floorGrid;

        [SerializeField]
        private ControlInstructions _controlInstructions;

        [SerializeField]
        private MLSpectator _phoneSpectator;

        private System.Random _random = new();
        private UploadThread _uploadThread;
        private DownloadThread _downloadThread;
        private IEnumerator _updateStatusTextCoroutine;
        private IEnumerator _updateBatteryStatusCoroutine;
        private LocalizationMapManager _localizationManager;
        private AnchorsManager _anchorsManager;
        private SpaceMeshManager _spaceMeshManager;
        private External3DModelManager _external3dModelManager;
        private DelayedButtonHandler _delayedButtonHandler;
        private LeapBrushPreferences _preferences;
        private ToolManager _toolManager;
        private ServerConnectionManager _serverConnectionManager;
        private CancellationTokenSource _shutDownTokenSource = new();
        private Dictionary<string, OtherUserVisual> _otherUsersMap = new();
        private Dictionary<string, BrushBase> _brushStrokeMap = new();
        private Dictionary<string, External3DModel> _externalModelMap = new();
        private IEnumerator _maybeCreateAnchorAfterLocalizationWithDelayCoroutine;
        private ServerInfoProto _serverInfo;
        private LinkedList<ServerStateResponse> _pendingServerStateResponses = new();
        private bool _appTooOld;
        private bool _serverTooOld;
        private string _userName;
        private string _userDisplayName;
        private string _headClosestAnchorId;
        private string _appVersion;
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private bool _drawSolo;
        private int _errorsLogged;
        private int _exceptionsOrAssertsLogged;
        private bool _continuedPastStartPanel;
        private Camera _camera;
        private string _cameraFollowUserName;
        private bool _startupPerformanceDelayCompleted;
        private Vector2 _lastTouchpadPosition;
        private StringBuilder _statusTextBuilder = new();

        private const float StatusTextUpdateDelaySeconds = .5f;
        private const float BatteryStatusUpdateDelaySeconds = 1;
        private static readonly Vector3 ServerEchoPositionOffset = new(0.1f, 0, 0);
        private static readonly TimeSpan OtherUserExpirationAge = TimeSpan.FromSeconds(5);
        private static readonly Vector3 External3DModelRelativeStartPosition = new(0, 0, 1.5f);

        /// <summary>
        /// Show the main menu.
        /// </summary>
        public void ShowMainMenu()
        {
            _mainMenu.SetActive(true);
            HandleActiveToolOrMenuVisibilityChanged();
        }

        /// <summary>
        /// Unity lifecycle for the LeapBrush component first initializing
        /// </summary>
        private void Awake()
        {
            Application.logMessageReceived += OnLogMessageReceived;

            _localizationManager = GetComponent<LocalizationMapManager>();
            _anchorsManager = GetComponent<AnchorsManager>();
            _spaceMeshManager = GetComponent<SpaceMeshManager>();
            _external3dModelManager = GetComponent<External3DModelManager>();
            _toolManager = GetComponent<ToolManager>();
            _serverConnectionManager = GetComponent<ServerConnectionManager>();
            _preferences = GetComponent<LeapBrushPreferences>();

            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();

            SanityCheckValidAppExecution();

            // TODO(ghazen): Use a server-provided session identifier.
            _userName = "U" + _random.Next(1, 1000000);
            _userDisplayName = "User " +  _random.Next(1, 10000);
            _appVersion = Application.version;

            _serverConnectionManager.OnServerUrlChanged += OnServerUrlChanged;
            _serverConnectionManager.LoadServerUrl();
        }

        /// <summary>
        /// Unity lifecycle for the LeapBrush component first starting
        /// </summary>
        private void Start()
        {
            MagicLeapRenderingExtensionsFeature renderFeature =
                OpenXRSettings.Instance.GetFeature<MagicLeapRenderingExtensionsFeature>();
            if (renderFeature != null)
            {
                renderFeature.BlendMode = XrEnvironmentBlendMode.AlphaBlend;
            }

            _camera = Camera.main;

            _updateStatusTextCoroutine = UpdateStatusTextPeriodically();
            StartCoroutine(_updateStatusTextCoroutine);

            _updateBatteryStatusCoroutine = UpdateBatteryStatusCoroutine();
            StartCoroutine(_updateBatteryStatusCoroutine);

            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;

            _toggleMenuInputAction.action.performed += OnMenuActionPerformed;
            _touchpadPosition.action.started += OnTouchpadPositionStart;
            _touchpadPosition.action.performed += OnTouchpadPositionPerformed;

            _preferences.ShowOtherHeadsets.OnChanged += OnShowOtherHeadsetsPreferenceChanged;
            _preferences.ShowOtherHandsAndControls.OnChanged +=
                OnShowOtherHandsAndControlsPreferenceChanged;
            _preferences.ShowFloorGrid.OnChanged += OnShowFloorGridPreferenceChanged;
            _preferences.GazePinchEnabled.OnChanged += OnGazePinchPreferenceChanged;
            _preferences.PhoneSpecatorEnabled.OnChanged += OnPhoneSpectatorPreferenceChanged;

            _settingsPanel.OnClearAllContentSelected += OnClearAllContentSelected;
            _settingsPanel.OnSettingsPanelHidden += OnSettingsPanelHidden;

            _startPanel.OnContinueSelected += OnStartPanelContinueSelected;
            _startPanel.OnSetUserDisplayName += OnSetUserDisplayName;

            _toolManager.OnActiveToolChanged += OnActiveToolChanged;
            foreach (InteractorToolManager toolContainer in _toolManager.InteractorToolManagers)
            {
                toolContainer.ScribbleBrush.OnPosesAdded += OnScribbleBrushStrokePosesAdded;
                toolContainer.ScribbleBrush.OnDrawingCompleted += OnBrushDrawingCompleted;
                toolContainer.PolyBrush.OnPosesUpdated += OnPolyBrushStrokePosesUpdated;
                toolContainer.PolyBrush.OnDrawingCompleted += OnBrushDrawingCompleted;
                toolContainer.Eraser.OnTriggerEnterEvent += OnEraserCollisionTrigger;
            }

            _mainPanel.OnSettingsSelected += OnSettingsSelected;
            _mainPanel.OnToolSelected += OnMainPanelOrHandMenuToolSelected;

            _external3dModelManager.OnModelsListUpdated += OnExternal3DModelsListUpdated;
            _importModelsPopup.OnPlaceNewExternal3DModel += OnPlaceNewExternal3DModel;

            _notConnectedPanel.OnDrawSoloSelected += OnDrawSoloSelected;

            _joinUserPopup.OnJoinUserSessionRemotelySelected += OnJoinUserSessionRemotelySelected;

            _handMenu.OnEnabledChanged += OnHandMenuEnabledChanged;
            _handMenu.OnShowMainMenuClicked += ShowMainMenu;
            _handMenu.OnToolSelected += OnMainPanelOrHandMenuToolSelected;

#if !UNITY_ANDROID || UNITY_EDITOR
            _ml2Controller.gameObject.SetActive(false);
#endif

            OnShowOtherHeadsetsPreferenceChanged();
            OnShowOtherHandsAndControlsPreferenceChanged();
            OnShowFloorGridPreferenceChanged();
            OnGazePinchPreferenceChanged();
            OnPhoneSpectatorPreferenceChanged();
            HandleActiveToolOrMenuVisibilityChanged();

            LoadUserDisplayName();
        }

        /// <summary>
        /// Unity lifecycle method run every render frame.
        /// </summary>
        private void Update()
        {
            CheckStartupPerformanceDelay();
            UpdateCameraFollow();
            UpdateUploadThread();
            ProcessReceivedServerStates();
            UpdatePanelVisibility();
            MaybeExpireOtherUsers();

            ThreadDispatcher.DispatchAll();
        }

        /// <summary>
        /// Monitor for a rendering performance goal at app startup. Once completed,
        /// allow the main menu to be shown and start preloading the keyboard.
        /// </summary>
        private void CheckStartupPerformanceDelay()
        {
            if (_startupPerformanceDelayCompleted)
            {
                return;
            }

            _startupPerformanceDelayCompleted =
                Time.frameCount > 10 && Time.deltaTime < 1.5f / 60f;

            _mainMenu.SetActive(_startupPerformanceDelayCompleted);

            if (_startupPerformanceDelayCompleted)
            {
                _keyboard.Preload();
            }
        }

        /// <summary>
        /// Update the camera to follow a particular joined user if set.
        /// </summary>
        private void UpdateCameraFollow()
        {
            if (_cameraFollowUserName == null)
            {
                return;
            }

            if (_otherUsersMap.TryGetValue(_cameraFollowUserName,
                out OtherUserVisual otherUserVisual))
            {
                var otherUserWearableTransform = otherUserVisual.WearableTransform;
                _camera.transform.SetPositionAndRotation(
                    otherUserWearableTransform.position, otherUserWearableTransform.rotation);

                _inputSimulator.gameObject.SetActive(false);
            }
            else
            {
                _inputSimulator.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Update the server upload thread with the latest state information, e.g. the user's
        /// Controller and Headset poses.
        /// </summary>
        private void UpdateUploadThread()
        {
            if (_uploadThread == null)
            {
                return;
            }

            Transform controlTransform = _ml2Controller.transform;
            Vector3 headPosition = _camera.transform.position;

            _uploadThread.SetSpaceInfo(_localizationManager.LocalizationInfo,
                _anchorsManager.IsUsingImportedAnchors, _anchorsManager.Anchors);

            // Determine which Anchor is closest to the user's head currently.
            AnchorsApi.Anchor anchorClosestToHead = null;
            float minHeadToAnchorDistanceSqr = Mathf.Infinity;
            for (int i = 0; i < _anchorsManager.Anchors.Length; ++i)
            {
                AnchorsApi.Anchor anchor = _anchorsManager.Anchors[i];

                float headDistanceToAnchorSqr =
                    (anchor.Pose.position - headPosition).sqrMagnitude;
                if (headDistanceToAnchorSqr < minHeadToAnchorDistanceSqr)
                {
                    anchorClosestToHead = anchor;
                    minHeadToAnchorDistanceSqr = headDistanceToAnchorSqr;
                }
            }

            GameObject anchorClosestToHeadGameObject = null;
            if (anchorClosestToHead != null)
            {
                _anchorsManager.TryGetAnchorGameObject(anchorClosestToHead.Id, out anchorClosestToHeadGameObject);
            }

            if (_cameraFollowUserName == null && anchorClosestToHeadGameObject != null)
            {
                // The camera is not set to follow a particular remote user and a closest anchor
                // was found, update the upload thread with the closest anchor, Headset and
                // Controller poses. These poses are relative to the anchor pose.
                _headClosestAnchorId = anchorClosestToHead.Id;
                _uploadThread.SetHeadClosestAnchorId(_headClosestAnchorId);
                _uploadThread.SetHeadPoseRelativeToAnchor(
                    anchorClosestToHeadGameObject.transform
                    .InverseTransformPose(new Pose(headPosition, _camera.transform.rotation)));
                if (_toolManager.ControllerManager.IsTrackingActive)
                {
                    _uploadThread.SetControllerStateRelativeToAnchor(
                        anchorClosestToHeadGameObject.transform
                            .InverseTransformPose(
                                new Pose(controlTransform.position, controlTransform.rotation)),
                        controlTransform.transform
                            .InverseTransformPose(TransformExtensions.GetWorldPose(
                                _toolManager.ControllerManager.ToolOffset)).position.z,
                        _toolManager.ControllerManager.SelectProgress,
                        _toolManager.ControllerManager.GetRayLinePointsRelative(
                            anchorClosestToHeadGameObject.transform));
                }
                else
                {
                    _uploadThread.ClearControllerState();
                }
                if (_toolManager.LeftHandManager.IsTrackingActive &&
                    _toolManager.LeftHandManager.ToolsVisible)
                {
                    _uploadThread.SetHandStateRelativeToAnchor(UploadThread.HandType.Left,
                        anchorClosestToHeadGameObject.transform
                            .InverseTransformPose(TransformExtensions.GetWorldPose(
                                _toolManager.LeftHandManager.ToolOffset)),
                        _toolManager.LeftHandManager.SelectProgress,
                        _toolManager.LeftHandManager.GetRayLinePointsRelative(
                            anchorClosestToHeadGameObject.transform));
                }
                else
                {
                    _uploadThread.ClearHandState(UploadThread.HandType.Left);
                }
                if (_toolManager.RightHandManager.IsTrackingActive &&
                    _toolManager.RightHandManager.ToolsVisible)
                {
                    _uploadThread.SetHandStateRelativeToAnchor(UploadThread.HandType.Right,
                        anchorClosestToHeadGameObject.transform
                            .InverseTransformPose(TransformExtensions.GetWorldPose(
                                _toolManager.RightHandManager.ToolOffset)),
                        _toolManager.RightHandManager.SelectProgress,
                        _toolManager.RightHandManager.GetRayLinePointsRelative(
                            anchorClosestToHeadGameObject.transform));
                }
                else
                {
                    _uploadThread.ClearHandState(UploadThread.HandType.Right);
                }
            }
            else
            {
                _headClosestAnchorId = null;
                _uploadThread.ClearAnchorAttachedStates();
            }

            _uploadThread.SetServerEchoEnabled(_serverConnectionManager.ServerEcho);
            _uploadThread.SetUserDisplayName(_userDisplayName);

            if (_mainMenu.activeSelf || _keyboard.IsShown ||
                _handMenu.isActiveAndEnabled)
            {
                // The menu is being shown to the user -- advertise that the user is using
                // the menu instead of a normal tool.
                _uploadThread.SetCurrentToolState(UserStateProto.Types.ToolState.Menu,
                    _toolManager.StrokeColor);
            }
            else
            {
                _uploadThread.SetCurrentToolState(ProtoUtils.ToProto(_toolManager.ActiveTool),
                    _toolManager.StrokeColor);
            }
        }

        /// <summary>
        /// Update which panels and UI should be shown.
        /// </summary>
        private void UpdatePanelVisibility()
        {
            if (!_startupPerformanceDelayCompleted)
            {
                return;
            }

            if (_appTooOld || _serverTooOld)
            {
                // The application and server are not incompatible. Show an error panel indicating
                // the issue.

                if (_appTooOld)
                {
                    _appTooOldPanel.gameObject.SetActive(true);
                }
                else
                {
                    _serverTooOldPanel.gameObject.SetActive(true);
                }
                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(false);
                _startPanel.Hide();
                _mainPanel.gameObject.SetActive(false);
                _settingsPanel.Hide();
                _keyboard.Hide();
            }
            else if (((!_downloadThread?.LastDownloadOk ?? false)
                      || (!_uploadThread?.LastUploadOk ?? false)) && !_drawSolo)
            {
                // The server and client appear to have been disconnected, show the not connected
                // panel.

                if (!_keyboard.IsShown)
                {
                    _notConnectedPanel.gameObject.SetActive(true);
                }
                _notLocalizedPanel.gameObject.SetActive(false);
                _startPanel.Hide();
                _mainPanel.gameObject.SetActive(false);
                _settingsPanel.Hide();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            else if (_localizationManager.LocalizationInfo.MapState
                     == LocalizationMapState.NotLocalized ||
                       _localizationManager.LocalizationInfo.MapState
                       == LocalizationMapState.LocalizationPending ||
                       (string.IsNullOrEmpty(_localizationManager.LocalizationInfo.MapName) &&
                        string.IsNullOrEmpty(_localizationManager.LocalizationInfo.MapUUID)))
            {
                // The ML2 client doesn't appear to have localized to a space yet. Show the not
                // localized panel which helps them open the Spaces tool to select or create a
                // space.

                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(true);
                _startPanel.Hide();
                _mainPanel.gameObject.SetActive(false);
                _settingsPanel.Hide();
                _keyboard.Hide();
            }
#endif
            else
            {
                // Proceed to the start or main panel depending on if the user has continued through
                // yet.

                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(false);
                if (!_settingsPanel.gameObject.activeSelf && !_keyboard.IsShown)
                {
                    if (_continuedPastStartPanel)
                    {
                        _startPanel.Hide();
                        _mainPanel.gameObject.SetActive(true);
                    }
                    else
                    {
                        _startPanel.Show(_userDisplayName);
                        _mainPanel.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Sanity check that the current app is in a valid state. Throw an exception and quit
        /// if not.
        /// </summary>
        private void SanityCheckValidAppExecution()
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                StartCoroutine(QuitApplicationEndOfFrame());
                throw new Exception(
                    "**** FATAL ERROR: Leap Brush requires an x64 process architecture due "
                    + "to dependencies on the unity usd package which has this limitation. "
                    + "On Mac, make sure to build the application for Intel 64-bit only "
                    + "and use the Intel version of the Unity editor");
            }

#if UNITY_STANDALONE
            if (Type.GetType("Mono.Runtime") == null)
            {
                StartCoroutine(QuitApplicationEndOfFrame());
                throw new Exception(
                    "**** FATAL ERROR: Leap Brush standalone app must run using the "
                    + "mono runtime. It does not support IL2CPP. Please re-build the application.");
            }
#endif
        }

        private IEnumerator QuitApplicationEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            Application.Quit(-1);
        }

        /// <summary>
        /// Look for other users that haven't had their poses updated recently, and remove
        /// them when found.
        /// </summary>
        private void MaybeExpireOtherUsers()
        {
            List<string> userNamesToRemove = null;
            List<GameObject> gameObjectsToDestroy = null;

            DateTimeOffset now = DateTimeOffset.Now;
            foreach (var entry in _otherUsersMap)
            {
                if (entry.Value == null)
                {
                    if (userNamesToRemove == null)
                    {
                        userNamesToRemove = new();
                    }
                    userNamesToRemove.Add(entry.Key);
                    continue;
                }

                if (entry.Value.LastUpdateTime < now - OtherUserExpirationAge)
                {
                    if (gameObjectsToDestroy == null)
                    {
                        gameObjectsToDestroy = new();
                    }
                    gameObjectsToDestroy.Add(entry.Value.gameObject);
                }
            }

            if (userNamesToRemove != null)
            {
                foreach (string userName in userNamesToRemove)
                {
                    _otherUsersMap.Remove(userName);
                }

                _startPanel.OnOtherUserCountChanged(_otherUsersMap.Count);
            }

            if (gameObjectsToDestroy != null)
            {
                foreach (GameObject gameObject in gameObjectsToDestroy)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Unity lifecycle event for this component being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            _toggleMenuInputAction.action.performed -= OnMenuActionPerformed;
            _touchpadPosition.action.started -= OnTouchpadPositionStart;
            _touchpadPosition.action.performed -= OnTouchpadPositionPerformed;

            StopServerConnections();
            ThreadDispatcher.DispatchAllAndShutdown();

            StopCoroutine(_updateStatusTextCoroutine);
            StopCoroutine(_updateBatteryStatusCoroutine);
            if (_maybeCreateAnchorAfterLocalizationWithDelayCoroutine != null)
            {
                StopCoroutine(_maybeCreateAnchorAfterLocalizationWithDelayCoroutine);
            }

            _localizationManager.OnLocalizationInfoChanged -= OnLocalizationInfoChanged;

            _preferences.ShowOtherHeadsets.OnChanged -= OnShowOtherHeadsetsPreferenceChanged;
            _preferences.ShowOtherHandsAndControls.OnChanged -=
                OnShowOtherHandsAndControlsPreferenceChanged;
            _preferences.ShowFloorGrid.OnChanged -= OnShowFloorGridPreferenceChanged;
            _preferences.GazePinchEnabled.OnChanged -= OnGazePinchPreferenceChanged;
            _preferences.PhoneSpecatorEnabled.OnChanged -= OnPhoneSpectatorPreferenceChanged;

            _toolManager.OnActiveToolChanged -= OnActiveToolChanged;
            foreach (InteractorToolManager toolContainer in _toolManager.InteractorToolManagers)
            {
                toolContainer.ScribbleBrush.OnPosesAdded -= OnScribbleBrushStrokePosesAdded;
                toolContainer.ScribbleBrush.OnDrawingCompleted -= OnBrushDrawingCompleted;
                toolContainer.PolyBrush.OnPosesUpdated -= OnPolyBrushStrokePosesUpdated;
                toolContainer.PolyBrush.OnDrawingCompleted -= OnBrushDrawingCompleted;
                toolContainer.Eraser.OnTriggerEnterEvent -= OnEraserCollisionTrigger;
            }

            Application.logMessageReceived -= OnLogMessageReceived;
        }

        /// <summary>
        /// Try to load the last saved user display name from application storage.
        /// </summary>
        private void LoadUserDisplayName()
        {
#if !UNITY_EDITOR
            string persistentDataPath = Application.persistentDataPath;

            ThreadDispatcher.ScheduleWork(() =>
            {
                string userNamePath = System.IO.Path.Join(
                    persistentDataPath, "userName.txt");

                try
                {
                    using (System.IO.StreamReader reader = new(userNamePath))
                    {
                        string userDisplayName = reader.ReadToEnd().Trim();
                        ThreadDispatcher.ScheduleMain(() =>
                        {
                            SetUserDisplayName(userDisplayName);
                        });
                    }
                }
                catch (System.IO.IOException e)
                {
                }
            });
#endif
        }

        /// <summary>
        /// Try to persist an updated user display name choice to application data storage.
        /// </summary>
        /// <param name="userDisplayName">The new user display name to save.</param>
        private static void SaveUserDisplayName(string userDisplayName)
        {
#if !UNITY_EDITOR
            string persistentDataPath = Application.persistentDataPath;

            ThreadDispatcher.ScheduleWork(() =>
            {
                string userNamePath = System.IO.Path.Join(
                    persistentDataPath, "userName.txt");

                try
                {
                    using (System.IO.StreamWriter writer = new(userNamePath))
                    {
                        writer.Write(userDisplayName);
                    }
                }
                catch (System.IO.IOException e)
                {
                }
            });
#endif
        }

        /// <summary>
        /// Handler for a collision between the eraser 3D model and another scene object.
        /// </summary>
        /// <param name="collider">The collider that intersected with the eraser.</param>
        private void OnEraserCollisionTrigger(Collider collider)
        {
            var collidedBrush = collider.GetComponentInParent<BrushBase>();
            if (collidedBrush != null && !string.IsNullOrEmpty(collidedBrush.Id))
            {
                Debug.Log("Erasing brush stroke " + collidedBrush.Id + " from anchor "
                          + collidedBrush.AnchorId);
                PlayOneoffSpatialSound(_eraseAudioPrefab,
                    collider.gameObject.transform.position);
                Destroy(collidedBrush.gameObject);

                _uploadThread.RemoveBrushStroke(collidedBrush.Id, collidedBrush.AnchorId);
                return;
            }

            var externalModel = collider.GetComponentInParent<External3DModel>();
            if (externalModel != null && !string.IsNullOrEmpty(externalModel.Id))
            {
                Debug.Log("Erasing external model " + externalModel.Id + " from anchor "
                          + externalModel.AnchorId);
                PlayOneoffSpatialSound(_eraseAudioPrefab,
                    collider.gameObject.transform.position);
                Destroy(externalModel.gameObject);

                _uploadThread.RemoveExternalModel(externalModel.Id, externalModel.AnchorId);
            }
        }

        /// <summary>
        /// Play a oneoff spatial sound at a particular location.
        /// </summary>
        /// <param name="audioPrefab">The audio source prefab for the sound to play.</param>
        /// <param name="position">The world position where the sound should play.</param>
        private void PlayOneoffSpatialSound(GameObject audioPrefab, Vector3 position)
        {
            OneoffAudioSource oneoffAudioSource = Instantiate(
                audioPrefab, transform)
                .GetComponent<OneoffAudioSource>();
            oneoffAudioSource.transform.position = position;
        }

        /// <summary>
        /// Handler for map localization changes. The user may have lost or regained localization.
        /// </summary>
        /// <param name="info">The updated localization info.</param>
        private void OnLocalizationInfoChanged(LocalizationMapManager.LocalizationMapInfo info)
        {
            if (info.MapState == LocalizationMapState.Localized)
            {
                if (_maybeCreateAnchorAfterLocalizationWithDelayCoroutine == null)
                {
                    _maybeCreateAnchorAfterLocalizationWithDelayCoroutine
                        = MaybeCreateAnchorAfterLocalizationWithDelay();
                    StartCoroutine(_maybeCreateAnchorAfterLocalizationWithDelayCoroutine);
                }

                TransformExtensions.SetWorldPose(_spaceOriginAxis.transform, info.OriginPose);

                _spaceMeshManager.UpdateSpaceMesh(info.MapUUID, info.OriginPose);
            }
            else if (_maybeCreateAnchorAfterLocalizationWithDelayCoroutine != null)
            {
                StopCoroutine(_maybeCreateAnchorAfterLocalizationWithDelayCoroutine);
                _maybeCreateAnchorAfterLocalizationWithDelayCoroutine = null;
            }
        }

        /// <summary>
        /// Handler for the user triggering the menu action.
        /// </summary>
        private void OnMenuActionPerformed(InputAction.CallbackContext callbackContext)
        {
            if (_continuedPastStartPanel)
            {
                if (!_mainMenu.activeSelf)
                {
                    _mainMenu.SetActive(true);
                }
                else if (_mainPanel.gameObject.activeInHierarchy
                         || _settingsPanel.gameObject.activeInHierarchy)
                {
                    _mainMenu.SetActive(false);
                }
            }

            StopCameraFollow();
            HandleActiveToolOrMenuVisibilityChanged();
        }

        private void OnTouchpadPositionStart(InputAction.CallbackContext callbackContext)
        {
            _lastTouchpadPosition = callbackContext.action.ReadValue<Vector2>();
        }

        private void OnTouchpadPositionPerformed(InputAction.CallbackContext callbackContext)
        {
            Vector2 touchpadPosition = callbackContext.action.ReadValue<Vector2>();
            Vector2 touchpadDelta = touchpadPosition - _lastTouchpadPosition;
            _lastTouchpadPosition = touchpadPosition;

            if (_toolManager.ActiveTool == ToolType.Laser || _toolManager.LaserToolOverride)
            {
                return;
            }

            _toolManager.AdjustToolOffset(touchpadDelta.y * 0.25f);
        }

        /// <summary>
        /// Start following a particular user in first person follow mode.
        /// </summary>
        /// <param name="cameraFollowUserName"></param>
        private void StartCameraFollow(string cameraFollowUserName)
        {
            _cameraFollowUserName = cameraFollowUserName;
            _mainMenu.SetActive(false);
            _toolManager.SetActiveTool(ToolType.Laser);

            if (_otherUsersMap.TryGetValue(_cameraFollowUserName,
                    out OtherUserVisual otherUserVisual))
            {
                otherUserVisual.SetCameraFollowingUser(true);
            }
        }

        /// <summary>
        /// Stop following any user in first person follow mode.
        /// </summary>
        private void StopCameraFollow()
        {
            if (_cameraFollowUserName == null)
            {
                return;
            }

            if (_otherUsersMap.TryGetValue(_cameraFollowUserName,
                    out OtherUserVisual otherUserVisual))
            {
                otherUserVisual.SetCameraFollowingUser(false);
            }

            _cameraFollowUserName = null;
            _inputSimulator.gameObject.SetActive(true);
        }

        /// <summary>
        /// Handler for new poses being added to the scribble brush tool while drawing.
        /// </summary>
        private void OnScribbleBrushStrokePosesAdded(ScribbleBrushTool scribbleBrush)
        {
            // Hold the upload thread lock while manipulating _uploadThread.CurrentBrushStroke
            lock (_uploadThread.Lock)
            {
                BrushStrokeProto currentBrushStroke = _uploadThread.GetCurrentBrushStroke(
                    scribbleBrush.Brush);
                if (currentBrushStroke == null)
                {
                    if (!_anchorsManager.TryGetAnchorGameObject(_headClosestAnchorId, out _))
                    {
                        return;
                    }

                    currentBrushStroke = new BrushStrokeProto
                    {
                        Id = "B" + _random.Next(0, Int32.MaxValue),
                        AnchorId = _headClosestAnchorId,
                        StrokeColorRgb = ColorUtils.ToRgbaUint(_toolManager.StrokeColor),
                    };

                    currentBrushStroke.Type = BrushStrokeProto.Types.BrushType.Scribble;
                    currentBrushStroke.UserName = _userName;
                    _uploadThread.SetCurrentBrushStroke(scribbleBrush.Brush, currentBrushStroke);

                    _toolManager.SetBrushColorManuallySelected();
                }

                if (_anchorsManager.TryGetAnchorGameObject(
                        currentBrushStroke.AnchorId, out GameObject anchorGameObject))
                {
                    Transform anchorTransform = anchorGameObject.transform;

                    int brushStrokeStartIndex =
                        currentBrushStroke.StartIndex
                        + currentBrushStroke.BrushPose.Count;
                    for (int i = brushStrokeStartIndex; i < scribbleBrush.Brush.Poses.Count; ++i)
                    {
                        currentBrushStroke.BrushPose.Add(ProtoUtils.ToProto(
                            anchorTransform.InverseTransformPose(scribbleBrush.Brush.Poses[i])));
                    }
                }
            }
        }

        /// <summary>
        /// Handler for new poses being added to the polygon brush while drawing.
        /// </summary>
        /// <param name="startIndex">The start index from where poses were modified.</param>
        private void OnPolyBrushStrokePosesUpdated(PolyBrushTool polyBrush, int startIndex)
        {
            // Hold the upload thread lock while manipulating _uploadThread.CurrentBrushStroke
            lock (_uploadThread.Lock)
            {
                BrushStrokeProto currentBrushStroke = _uploadThread.GetCurrentBrushStroke(
                    polyBrush.Brush);
                if (currentBrushStroke == null)
                {
                    if (!_anchorsManager.TryGetAnchorGameObject(_headClosestAnchorId, out _))
                    {
                        return;
                    }

                    currentBrushStroke = new BrushStrokeProto
                    {
                        Id = "B" + _random.Next(0, Int32.MaxValue),
                        AnchorId = _headClosestAnchorId,
                        StrokeColorRgb = ColorUtils.ToRgbaUint(_toolManager.StrokeColor),
                        FillColorRgba = ColorUtils.ToRgbaUint(_toolManager.FillColor),
                        FillDimmerA = (uint) Math.Round(_toolManager.FillDimmerAlpha * 255)
                    };

                    currentBrushStroke.Type = BrushStrokeProto.Types.BrushType.Poly;
                    currentBrushStroke.UserName = _userName;
                    _uploadThread.SetCurrentBrushStroke(polyBrush.Brush, currentBrushStroke);

                    _toolManager.SetBrushColorManuallySelected();
                }

                GameObject anchorGameObject;
                if (_anchorsManager.TryGetAnchorGameObject(
                        currentBrushStroke.AnchorId, out anchorGameObject))
                {
                    Transform anchorTransform = anchorGameObject.transform;

                    if (currentBrushStroke.StartIndex >= startIndex)
                    {
                        currentBrushStroke.StartIndex = startIndex;
                        currentBrushStroke.BrushPose.Clear();
                    }

                    while (startIndex - currentBrushStroke.StartIndex
                           < currentBrushStroke.BrushPose.Count)
                    {
                        currentBrushStroke.BrushPose.RemoveAt(
                            currentBrushStroke.BrushPose.Count - 1);
                    }

                    int brushStrokeStartIndex = currentBrushStroke.StartIndex
                        + currentBrushStroke.BrushPose.Count;
                    for (int i = brushStrokeStartIndex; i < polyBrush.Brush.Poses.Count; ++i)
                    {
                        currentBrushStroke.BrushPose.Add(ProtoUtils.ToProto(
                            anchorTransform.InverseTransformPose(polyBrush.Brush.Poses[i])));
                    }
                }
            }
        }

        /// <summary>
        /// Handler for a brush drawing being completed.
        /// </summary>
        /// <param name="brushTool">The brush tool that completed the drawing.</param>
        private void OnBrushDrawingCompleted(BrushToolBase brushTool)
        {
            BrushStrokeProto brushStrokeProto;
            // Hold the upload thread lock while manipulating _uploadThread.CurrentBrushStroke
            lock (_uploadThread.Lock)
            {
                // Pull the current brush stroke from the upload thread and clear it.
                brushStrokeProto = _uploadThread.GetCurrentBrushStroke(brushTool.Brush);
                _uploadThread.SetCurrentBrushStroke(brushTool.Brush, null);
            }

            if (brushStrokeProto == null)
            {
                return;
            }

            GameObject anchorGameObject;
            if (!_anchorsManager.TryGetAnchorGameObject(brushStrokeProto.AnchorId, out anchorGameObject))
            {
                return;
            }

            Transform anchorTransform = anchorGameObject.transform;
            Pose[] poses = new Pose[brushTool.Brush.Poses.Count];
            for (int i = 0; i < poses.Length; i++)
            {
                poses[i] = anchorTransform.InverseTransformPose(brushTool.Brush.Poses[i]);
            }

            if (_brushStrokeMap.TryGetValue(brushStrokeProto.Id, out BrushBase oldBrush))
            {
                Destroy(oldBrush.gameObject);
            }

            if (brushStrokeProto.StartIndex + brushStrokeProto.BrushPose.Count < 2)
            {
                // The brush did not create a sufficient number of poses to be a drawing in the end.
                // Remove the brush stroke.
                _uploadThread.RemoveBrushStroke(brushStrokeProto.Id, brushStrokeProto.AnchorId);
                return;
            }

            BrushBase brushStroke = Instantiate(
                brushTool.Prefab, anchorGameObject.transform).GetComponent<BrushBase>();
            brushStroke.SetPosesAndTruncate(0, poses, false);
            brushStroke.SetColors(_toolManager.StrokeColor, _toolManager.FillColor,
                _toolManager.FillDimmerAlpha);

            brushStroke.AnchorId = brushStrokeProto.AnchorId;
            brushStroke.Id = brushStrokeProto.Id;
            brushStroke.UserName = _userName;

            _brushStrokeMap[brushStrokeProto.Id] = brushStroke;
            brushStroke.OnDestroyed += OnBrushStrokeDestroyed;

            // Upload the completed brush stroke to the server.
            _uploadThread.AddBrushStroke(brushStrokeProto);
        }

        /// <summary>
        /// Handler for a brush stroke object being destroyed.
        /// </summary>
        /// <param name="brushStroke">The brush stroke component that was destroyed.</param>
        private void OnBrushStrokeDestroyed(BrushBase brushStroke)
        {
            if (_brushStrokeMap.TryGetValue(brushStroke.Id, out BrushBase existingBrushStroke))
            {
                if (ReferenceEquals(brushStroke, existingBrushStroke))
                {
                    _brushStrokeMap.Remove(brushStroke.Id);
                }
            }
        }

        /// <summary>
        /// Handler for the pose of a 3D model being changed.
        /// </summary>
        /// <param name="externalModel">The 3D model that had its pose changed.</param>
        private void OnExternalModelTransformChanged(External3DModel externalModel)
        {
            GameObject anchorGameObject;
            if (_anchorsManager.TryGetAnchorGameObject(externalModel.AnchorId, out anchorGameObject))
            {
                // Update the 3D model pose in the server.
                _uploadThread.UpdateExternalModel(externalModel.Id,
                    externalModel.AnchorId, externalModel.FileName, externalModel.TransformProto);
            }
        }

        /// <summary>
        /// Handler for the user toggling whether other user's Headsets should be displayed.
        /// </summary>
        private void OnShowOtherHeadsetsPreferenceChanged()
        {
            foreach (var entry in _otherUsersMap)
            {
                entry.Value.SetShowHeadset(_preferences.ShowOtherHeadsets.Value);
            }
        }

        /// <summary>
        /// Handler for the user toggling whether other user's hands and Controllers should be
        /// displayed.
        /// </summary>
        private void OnShowOtherHandsAndControlsPreferenceChanged()
        {
            foreach (var entry in _otherUsersMap)
            {
                entry.Value.ShowHandsAndControls(_preferences.ShowOtherHandsAndControls.Value);
            }
        }

        /// <summary>
        /// Handler for the user toggling whether the floor grid should be displayed.
        /// </summary>
        private void OnShowFloorGridPreferenceChanged()
        {
            _floorGrid.gameObject.SetActive(_preferences.ShowFloorGrid.Value);
        }

        /// <summary>
        /// Handler for the user toggling the gaze pinch preference.
        /// </summary>
        private void OnGazePinchPreferenceChanged()
        {
            _gazeInteractor.gameObject.SetActive(_preferences.GazePinchEnabled.Value);
        }

        /// <summary>
        /// Handler for the user toggling the phone spectator preference.
        /// </summary>
        private void OnPhoneSpectatorPreferenceChanged()
        {
            if (_preferences.PhoneSpecatorEnabled.Value)
            {
                _phoneSpectator.Enable();
            }
            else
            {
                _phoneSpectator.Disable();
            }
        }

        /// <summary>
        /// Handler for the hand menu toggling.
        /// </summary>
        private void OnHandMenuEnabledChanged()
        {
            HandleActiveToolOrMenuVisibilityChanged();
        }

        /// <summary>
        /// Handler for the settings menu to be displayed.
        /// </summary>
        private void OnSettingsSelected()
        {
            _settingsPanel.Show(_userName, _leapBrushClient);
            _mainPanel.gameObject.SetActive(false);
        }

        private void OnMainPanelOrHandMenuToolSelected()
        {
            if (_mainPanel.gameObject.activeInHierarchy
                || _settingsPanel.gameObject.activeInHierarchy)
            {
                _mainMenu.SetActive(false);
                HandleActiveToolOrMenuVisibilityChanged();
            }
        }

        /// <summary>
        /// Handler for the list of external 3D models being refreshed.
        /// </summary>
        private void OnExternal3DModelsListUpdated()
        {
            _importModelsPopup.OnExternal3DModelsListUpdated(_external3dModelManager.Models);
        }

        /// <summary>
        /// Handler for the user placing a new 3D model.
        /// </summary>
        /// <param name="modelInfo">The information about the 3D model to place.</param>
        private void OnPlaceNewExternal3DModel(External3DModelManager.ModelInfo modelInfo)
        {
            _importModelsPopup.Hide();
            _mainMenu.SetActive(false);
            _toolManager.SetActiveTool(ToolType.Laser);
            HandleActiveToolOrMenuVisibilityChanged();

            Vector3 modelPosition = _camera.transform.TransformPoint(
                External3DModelRelativeStartPosition);
            GameObject anchorGameObject;
            if (_anchorsManager.TryGetAnchorGameObject(_headClosestAnchorId, out anchorGameObject))
            {
                External3DModel externalModel = _external3dModelManager.LoadModelAsync(
                    modelInfo.FileName, anchorGameObject.transform);
                externalModel.Id = "M" + _random.Next(0, Int32.MaxValue);
                externalModel.AnchorId = _headClosestAnchorId;
                externalModel.OnTransformChanged += OnExternalModelTransformChanged;
                externalModel.RestrictInitialModelDimensions = true;

                _externalModelMap[externalModel.Id] = externalModel;
                externalModel.OnDestroyed += () => _externalModelMap.Remove(externalModel.Id);

                Vector3 modelLookDir = _camera.transform.position - modelPosition;
                modelLookDir.y = 0;

                Pose modelPose = new Pose(modelPosition,
                    Quaternion.LookRotation(modelLookDir.normalized, Vector3.up));
                TransformExtensions.SetWorldPose(externalModel.transform, modelPose);

                _uploadThread.UpdateExternalModel(externalModel.Id, externalModel.AnchorId,
                    modelInfo.FileName, ProtoUtils.ToProto(externalModel.transform));
            }
        }

        /// <summary>
        /// Handler for the user deciding to join another user remotely.
        /// </summary>
        /// <param name="userResult">The user information for the user to be joined.</param>
        private void OnJoinUserSessionRemotelySelected(QueryUsersResponse.Types.Result userResult)
        {
            // Import the anchors from the selected user into the Anchor Manager.
            var anchors = new AnchorsApi.Anchor[userResult.SpaceInfo.Anchor.Count];
            for (int i = 0; i < anchors.Length; i++)
            {
                AnchorProto anchorProto = userResult.SpaceInfo.Anchor[i];
                anchors[i] = new AnchorsApi.ImportedAnchor
                {
                    Id = anchorProto.Id,
                    Pose = ProtoUtils.FromProto(anchorProto.Pose)
                };
            }

            AnchorsApi.SetImportedAnchors(anchors);
            _settingsPanel.OnSessionJoined();

            _spaceMeshManager.UpdateSpaceMesh(userResult.SpaceInfo.SpaceId,
                ProtoUtils.FromProto(userResult.SpaceInfo.TargetSpaceOrigin));

            Debug.LogFormat("Joined user {0}, {1} anchors",
                userResult.UserName, userResult.SpaceInfo.Anchor.Count);

            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
#if !UNITY_ANDROID || UNITY_EDITOR
                StartCameraFollow(userResult.UserName);
#endif

                _joinUserPopup.Hide();
            });
        }

        /// <summary>
        /// Handler for the user picking to clear all brushes and 3d models at the current
        /// location.
        /// </summary>
        private void OnClearAllContentSelected()
        {
            foreach (var entry in _brushStrokeMap)
            {
                Destroy(entry.Value.gameObject);

                _uploadThread.RemoveBrushStroke(entry.Value.Id, entry.Value.AnchorId);
            }

            foreach (var entry in _externalModelMap)
            {
                Destroy(entry.Value.gameObject);

                _uploadThread.RemoveExternalModel(entry.Value.Id, entry.Value.AnchorId);
            }
        }

        private void OnSettingsPanelHidden()
        {
            _mainPanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// Handler for the user selecting to continue past the start panel.
        /// </summary>
        private void OnStartPanelContinueSelected()
        {
            _continuedPastStartPanel = true;
            _mainPanel.gameObject.SetActive(true);

            _anchorsManager.SetContentShown(true);
        }

        /// <summary>
        /// Handler for a new user display name being picked.
        /// </summary>
        /// <param name="userDisplayName">The updated display name</param>
        private void OnSetUserDisplayName(string userDisplayName)
        {
            SetUserDisplayName(userDisplayName);
        }

        /// <summary>
        /// Set the new user display name
        /// </summary>
        /// <param name="userDisplayName">The updated display name</param>
        private void SetUserDisplayName(string userDisplayName)
        {
            if (string.IsNullOrWhiteSpace(userDisplayName))
            {
                return;
            }

            _userDisplayName = userDisplayName;

            SaveUserDisplayName(userDisplayName);
            _startPanel.OnUserDisplayNameChanged(userDisplayName);
        }

        private void OnServerUrlChanged(string newServerUrl)
        {
            Debug.LogFormat("Selected the new server {0}", newServerUrl);

            _notConnectedPanel.OnServerUrlChanged(newServerUrl);

            RestartServerConnections();
        }

        private void OnActiveToolChanged()
        {
            HandleActiveToolOrMenuVisibilityChanged();
        }

        /// <summary>
        /// Update the visibility of various tools based on UI state.
        /// </summary>
        private void HandleActiveToolOrMenuVisibilityChanged()
        {
            bool menusActive = _mainMenu.activeSelf || _handMenu.isActiveAndEnabled
                                                    || _keyboard.IsShown;
            _toolManager.SetLaserToolOverride(menusActive);

            if (_controlInstructions == null)
            {
                return;
            }

            if (menusActive)
            {
                _controlInstructions.SetInstructionSet(
                    ControlInstructions.InstructionType.MainMenu);
            }
            else
            {
                switch (_toolManager.ActiveTool)
                {
                    case ToolType.Eraser:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.Eraser);
                        break;
                    case ToolType.Laser:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.LaserPointer);
                        break;
                    case ToolType.BrushScribble:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.Brush);
                        break;
                    case ToolType.BrushPoly:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.PolyTool);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Handler for the user confirming they want to use the draw solo option. Server
        /// connections are severed and the user is now drawing offline.
        /// </summary>
        private void OnDrawSoloSelected()
        {
            _drawSolo = true;

            _settingsPanel.OnDrawSolo();
            _startPanel.OnDrawSolo();

            RestartServerConnections();
        }

        /// <summary>
        /// Coroutine to wait a brief duration and then attempt to create an anchor for the
        /// current space. A Space must have at least one anchor in order to persist content.
        /// </summary>
        /// <returns></returns>
        private IEnumerator MaybeCreateAnchorAfterLocalizationWithDelay()
        {
            yield return new WaitForSeconds(1.0f);

            if (_anchorsManager.Anchors.Length == 0 && _anchorsManager.QueryReceivedOk)
            {
                if (!AnchorsApi.CreateAnchor(
                        new Pose(_camera.transform.position, Quaternion.identity)))
                {
                    Debug.LogError("Failed to create new anchor.");
                }
            }

            _maybeCreateAnchorAfterLocalizationWithDelayCoroutine = null;
        }

        /// <summary>
        /// Coroutine to periodically up the the status text.
        /// </summary>
        private IEnumerator UpdateStatusTextPeriodically()
        {
            YieldInstruction updateDelay = new WaitForSeconds(StatusTextUpdateDelaySeconds);

            while (true)
            {
                UpdateStatusText();

                yield return updateDelay;
            }
        }

        /// <summary>
        /// Refresh the status text display.
        /// </summary>
        private void UpdateStatusText()
        {
            if (!_statusText.gameObject.activeInHierarchy)
            {
                return;
            }

            _statusTextBuilder.Length = 0;

            _statusTextBuilder.AppendFormat(
                "<color=#dbfb76><b>{0} v{1}</b></color>\n", Application.productName,
                Application.version);
            _statusTextBuilder.AppendFormat("UserName: {0}\n", _userDisplayName);

            {
                _statusTextBuilder.Append("Server Connection: ");

                if (_drawSolo)
                {
                    _statusTextBuilder.Append("<color=#ffa500>Drawing Solo</color>");
                }
                else
                {
                    _statusTextBuilder.Append(_serverConnectionManager.ServerUrl);
                    if (_serverInfo != null)
                    {
                        _statusTextBuilder.AppendFormat(" (v{0})", _serverInfo.ServerVersion);
                    }

                    _statusTextBuilder.Append(": ");
                    if ((_downloadThread?.LastDownloadOk ?? false)
                        && (_uploadThread?.LastUploadOk ?? false))
                    {
                        _statusTextBuilder.Append("<color=#00ff00>Connected</color>");
                    }
                    else
                    {
                        _statusTextBuilder.Append("<color=#ff0000>Disconnected</color>");
                    }
                }

                _statusTextBuilder.Append("\n");
            }

            {
                _statusTextBuilder.Append("Map Localization: <i>");

                if (_localizationManager.LocalizationInfo.MapState
                    == LocalizationMapState.Localized &&
                    _localizationManager.LocalizationInfo.MapType == LocalizationMapType.Cloud)
                {
                    _statusTextBuilder.AppendFormat(
                        "<color=#00ff00>{0}</color>",
                        _localizationManager.LocalizationInfo.ToString());
                }
                else
                {
                    _statusTextBuilder.AppendFormat(
                        "<color=#ffa500>{0}</color>",
                        _localizationManager.LocalizationInfo.ToString());
                }

                _statusTextBuilder.Append("</i>\n");
            }

            if (_exceptionsOrAssertsLogged > 0 || _errorsLogged > 0)
            {
                if (_exceptionsOrAssertsLogged > 0)
                {
                    _statusTextBuilder.AppendFormat(
                        "<color=#ee0000><b>{0} exceptions; ", _exceptionsOrAssertsLogged);
                }
                else
                {
                    _statusTextBuilder.Append("<color=#dbfb76><b>");
                }

                if (_errorsLogged > 0)
                {
                    _statusTextBuilder.AppendFormat("{0} errors; ", _errorsLogged);
                }

                _statusTextBuilder.Length -= 2;
                _statusTextBuilder.Append("</b></color>\n");
            }

            _statusText.text = _statusTextBuilder.ToString();
        }

        private IEnumerator UpdateBatteryStatusCoroutine()
        {
            YieldInstruction updateDelay = new WaitForSeconds(BatteryStatusUpdateDelaySeconds);

            while (true)
            {
                yield return updateDelay;

                if (_uploadThread == null)
                {
                    continue;
                }

                // Unity creates a bunch of log spam when querying battery status. Do this
                // only periodically.
                _uploadThread.SetBatteryStatus(SystemInfo.batteryLevel, SystemInfo.batteryStatus);
            }
        }

        /// <summary>
        /// Start server connections asynchronously
        /// </summary>
        private void StartServerConnections()
        {
            bool drawSolo = _drawSolo;

            _shutDownTokenSource = new();

            ThreadDispatcher.ScheduleWork(() =>
            {
                LeapBrushApiBase.LeapBrushClient leapBrushClient =
                    _serverConnectionManager.Connect(drawSolo);

                ThreadDispatcher.ScheduleMain(() =>
                {
                    _leapBrushClient = leapBrushClient;

                    _uploadThread = new UploadThread(
                        _leapBrushClient, _shutDownTokenSource, _userName, _userDisplayName);
                    _uploadThread.Start();

                    _downloadThread = new DownloadThread(
                        _leapBrushClient, _shutDownTokenSource, _userName, _appVersion);
                    _downloadThread.OnServerStateReceived += HandleServerStateReceived;
                    _downloadThread.Start();
                });
            });
        }

        /// <summary>
        /// Stop server connections asynchronously.
        /// </summary>
        private void StopServerConnections()
        {
            _shutDownTokenSource.Cancel();

            LeapBrushApiBase.LeapBrushClient leapBrushClient = _leapBrushClient;
            UploadThread uploadThread = _uploadThread;
            DownloadThread downloadThread = _downloadThread;

            ThreadDispatcher.ScheduleWork(() =>
            {
                if (leapBrushClient != null)
                {
                    Debug.Log("Shutting down leap brush client channel...");
                    leapBrushClient.CloseAndWait();
                }

                if (uploadThread != null)
                {
                    Debug.Log("Joining upload thread...");
                    uploadThread.Join();
                    Debug.Log("Upload thread joined");
                }

                if (downloadThread != null)
                {
                    Debug.Log("Joining download thread...");
                    downloadThread.Join();
                    Debug.Log("Upload download joined");
                }
            });
        }

        /// <summary>
        /// Restart server connections.
        /// </summary>
        private void RestartServerConnections()
        {
            StopServerConnections();
            StartServerConnections();
        }

        /// <summary>
        /// Handle a new update from the server.
        /// </summary>
        /// <param name="resp">The server state response received from the server</param>
        private void HandleServerStateReceived(ServerStateResponse resp)
        {
            _pendingServerStateResponses.AddLast(resp);
        }

        private void ProcessReceivedServerStates()
        {
            int remainingBrushStrokesToAddThisFrame = 50;

            while (_pendingServerStateResponses.Count > 0)
            {
                ServerStateResponse resp = _pendingServerStateResponses.First.Value;
                _pendingServerStateResponses.RemoveFirst();

                if (!HandleServerInfo(resp.ServerInfo))
                {
                    return;
                }

                for (var i = 0; i < resp.UserState.Count; i++)
                {
                    HandleOtherUserStateReceived(resp.UserState[i]);
                }

                for (var i = 0; i < resp.BrushStrokeAdd.Count; i++)
                {
                    HandleBrushStrokeAddReceived(resp.BrushStrokeAdd[i]);
                    remainingBrushStrokesToAddThisFrame--;
                }

                for (var i = 0; i < resp.BrushStrokeRemove.Count; i++)
                {
                    HandleBrushStrokeRemoveReceived(resp.BrushStrokeRemove[i]);
                }

                for (var i = 0; i < resp.ExternalModelAdd.Count; i++)
                {
                    HandleExternalModelAddReceived(resp.ExternalModelAdd[i]);
                }

                for (var i = 0; i < resp.ExternalModelRemove.Count; i++)
                {
                    HandleExternalModelRemoveReceived(resp.ExternalModelRemove[i]);
                }

                if (remainingBrushStrokesToAddThisFrame <= 0 || resp.ExternalModelAdd.Count > 0)
                {
                    // Defer additional processing until next frame
                    return;
                }
            }
        }

        /// <summary>
        /// Handle the optional server info proto within the server response.
        /// </summary>
        /// <param name="serverInfo">The optional server info resp or null if not present</param>
        /// <returns>
        /// True if processing of the server response should continue, False if the server version
        /// is incompatible with the application.
        /// </returns>
        private bool HandleServerInfo(ServerInfoProto serverInfo)
        {
            if (serverInfo != null)
            {
                _serverInfo = serverInfo;
                if (!VersionUtil.IsGreatorOrEqual(_appVersion, _serverInfo.MinAppVersion)
                    && !_appTooOld)
                {
                    _appTooOld = true;
                    Debug.LogErrorFormat("Shutting down due to outdated app ({0} < {1})",
                        _appVersion, _serverInfo.MinAppVersion);
                    _shutDownTokenSource.Cancel();
                    return false;
                }
                if (!VersionUtil.IsGreatorOrEqual(_serverInfo.ServerVersion,
                        _serverConnectionManager.MinServerVersion)
                    && !_serverTooOld && !_drawSolo)
                {
                    _serverTooOld = true;
                    Debug.LogErrorFormat("Shutting down due to outdated server ({0} < {1})",
                        _serverInfo.ServerVersion,
                        _serverConnectionManager.MinServerVersion);
                    _shutDownTokenSource.Cancel();
                    return false;
                }

                Debug.LogFormat("Server: version {0}, min app version {1}",
                    _serverInfo.ServerVersion, _serverInfo.MinAppVersion);
            }
            else if (_serverInfo == null && !_serverTooOld)
            {
                _serverTooOld = true;
                Debug.LogError("Shutting down due to outdated server (no version found)");
                _shutDownTokenSource.Cancel();
                return false;
            }

            return !_serverTooOld && !_appTooOld;
        }

        /// <summary>
        /// Handle an update from another user with their user state.
        /// </summary>
        /// <param name="otherUserState">The other user's state that was updated</param>
        private void HandleOtherUserStateReceived(UserStateProto otherUserState)
        {
            if (otherUserState.AnchorId.Length == 0)
            {
                return;
            }

            GameObject anchorGameObject;
            if (!_anchorsManager.TryGetAnchorGameObject(
                    otherUserState.AnchorId, out anchorGameObject))
            {
                return;
            }

            bool isServerEcho = _serverConnectionManager.ServerEcho
                                && otherUserState.UserName == _userName;

            if (otherUserState.HeadPose != null)
            {
                HandleValidOtherUserReceived(otherUserState, anchorGameObject, isServerEcho);
            }
        }

        /// <summary>
        /// Handler another user's pose and state being received.
        /// </summary>
        /// <param name="otherUserState">The other user's updated state</param>
        /// <param name="anchorGameObject">
        /// The anchor game object that the other user's poses are attached to.
        /// </param>
        /// <param name="isServerEcho">
        /// Whether this event was a server echo from the current user. (Useful for testing
        /// networking).
        /// </param>
        private void HandleValidOtherUserReceived(UserStateProto otherUserState,
            GameObject anchorGameObject, bool isServerEcho)
        {
            OtherUserVisual otherUserVisual;
            bool newlyJoinedUser = false;
            if (!_otherUsersMap.TryGetValue(otherUserState.UserName, out otherUserVisual))
            {
                newlyJoinedUser = true;
                Debug.Log("Found other user " + otherUserState.UserName);
                otherUserVisual = Instantiate(_otherUserPrefab, anchorGameObject.transform)
                    .GetComponent<OtherUserVisual>();
                otherUserVisual.SetShowHeadset(_preferences.ShowOtherHeadsets.Value);
                otherUserVisual.ShowHandsAndControls(_preferences.ShowOtherHandsAndControls.Value);

                if (otherUserState.UserName == _cameraFollowUserName)
                {
                    otherUserVisual.SetCameraFollowingUser(true);
                }

                _otherUsersMap[otherUserState.UserName] = otherUserVisual;
                otherUserVisual.OnDestroyed += () =>
                {
                    _otherUsersMap.Remove(otherUserState.UserName);
                    if (_startPanel != null)
                    {
                        _startPanel.OnOtherUserCountChanged(_otherUsersMap.Count);
                    }
                };

                _startPanel.OnOtherUserCountChanged(_otherUsersMap.Count);
            }

            if (otherUserVisual.transform.parent != anchorGameObject.transform)
            {
                otherUserVisual.transform.SetParent(anchorGameObject.transform);
            }

            otherUserVisual.HandleStateUpdate(otherUserState, isServerEcho);

            if (newlyJoinedUser && !isServerEcho)
            {
                PlayOneoffSpatialSound(_userJoinAudioPrefab, otherUserVisual.transform.position);
            }

            _floorGrid.FoundContentAtPosition(otherUserVisual.WearableTransform.position);

            if (otherUserState.ControllerState != null)
            {
                _floorGrid.FoundContentAtPosition(otherUserVisual.ControllerTransform.position);
            }
        }

        /// <summary>
        /// Handle a new or updated brush stroke being received from the server.
        /// </summary>
        /// <param name="brushAdd">The brush stroke to add or update.</param>
        private void HandleBrushStrokeAddReceived(BrushStrokeAddRequest brushAdd)
        {
            GameObject anchorGameObject;
            if (!_anchorsManager.TryGetAnchorGameObject(brushAdd.BrushStroke.AnchorId,
                    out anchorGameObject))
            {
                return;
            }

            BrushBase brushStroke;
            _brushStrokeMap.TryGetValue(brushAdd.BrushStroke.Id, out brushStroke);

            bool isServerEcho;
            if (brushStroke != null)
            {
                isServerEcho = _serverConnectionManager.ServerEcho
                               && brushStroke.UserName == _userName;
            }
            else
            {
                isServerEcho = _serverConnectionManager.ServerEcho
                               && brushAdd.BrushStroke.UserName == _userName;
            }

            if (brushStroke == null)
            {
                GameObject brushStrokePrefab =
                    brushAdd.BrushStroke.Type == BrushStrokeProto.Types.BrushType.Poly ?
                        _toolManager.PolyBrushPrefab : _toolManager.ScribbleBrushPrefab;
                brushStroke = Instantiate(brushStrokePrefab, anchorGameObject.transform)
                    .GetComponent<BrushBase>();
                brushStroke.AnchorId = brushAdd.BrushStroke.AnchorId;
                brushStroke.Id = brushAdd.BrushStroke.Id;
                brushStroke.UserName = brushAdd.BrushStroke.UserName;
                brushStroke.IsServerEcho = isServerEcho;

                Color32 strokeColor = brushAdd.BrushStroke.StrokeColorRgb != 0 ?
                    ColorUtils.FromRgbaUint(brushAdd.BrushStroke.StrokeColorRgb) :
                    _toolManager.FallbackBrushColor;
                Color32 fillColor = brushAdd.BrushStroke.FillColorRgba != 0 ?
                    ColorUtils.FromRgbaUint(brushAdd.BrushStroke.FillColorRgba) :
                    Color.clear;
                float fillDimmerAlpha = brushAdd.BrushStroke.FillDimmerA != 0 ?
                    (float) brushAdd.BrushStroke.FillDimmerA / 255 : 0;
                brushStroke.SetColors(strokeColor, fillColor, fillDimmerAlpha);

                if (brushAdd.BrushStroke.StrokeColorRgb != 0 && !isServerEcho)
                {
                    _toolManager.OtherUserBrushColorObserved(strokeColor);
                }

                _brushStrokeMap[brushAdd.BrushStroke.Id] = brushStroke;
                brushStroke.OnDestroyed += OnBrushStrokeDestroyed;
            }

            if (isServerEcho == brushStroke.IsServerEcho)
            {
                Pose[] poses = ProtoUtils.FromProto(brushAdd.BrushStroke.BrushPose);
                if (isServerEcho)
                {
                    for (int i = 0; i < poses.Length; ++i)
                    {
                        poses[i].position += ServerEchoPositionOffset;
                    }
                }

                brushStroke.SetPosesAndTruncate(brushAdd.BrushStroke.StartIndex, poses, true);

                for (var i = 0; i < poses.Length; i++)
                {
                    _floorGrid.FoundContentAtPosition(
                        poses[i].GetTransformedBy(anchorGameObject.transform).position);
                }
            }
        }

        /// <summary>
        /// Handle a brush stroke being removed from the server
        /// </summary>
        /// <param name="brushRemove">The brush stroke to remove.</param>
        private void HandleBrushStrokeRemoveReceived(BrushStrokeRemoveRequest brushRemove)
        {
            BrushBase brushStroke;
            if (_brushStrokeMap.TryGetValue(brushRemove.Id, out brushStroke))
            {
                Debug.Log("Deleting brush stroke " + brushRemove.Id + " from anchor "
                          + brushRemove.AnchorId);
                PlayOneoffSpatialSound(_eraseAudioPrefab,
                    brushStroke.gameObject.transform.position);
                Destroy(brushStroke.gameObject);
            }
        }

        /// <summary>
        /// Handle a new or updated 3D model being received from the server.
        /// </summary>
        /// <param name="externalModelAdd">The 3D model to add or update.</param>
        private void HandleExternalModelAddReceived(ExternalModelAddRequest externalModelAdd)
        {
            GameObject anchorGameObject;
            if (!_anchorsManager.TryGetAnchorGameObject(externalModelAdd.Model.AnchorId,
                    out anchorGameObject))
            {
                return;
            }

            External3DModel externalModel;
            if (!_externalModelMap.TryGetValue(externalModelAdd.Model.Id, out externalModel))
            {
                externalModel = _external3dModelManager.LoadModelAsync(
                    externalModelAdd.Model.FileName, anchorGameObject.transform);
                externalModel.Id = externalModelAdd.Model.Id;
                externalModel.AnchorId = externalModelAdd.Model.AnchorId;
                externalModel.OnTransformChanged += OnExternalModelTransformChanged;

                _externalModelMap[externalModel.Id] = externalModel;
                externalModel.OnDestroyed += () => _externalModelMap.Remove(externalModel.Id);
            }

            bool isServerEcho = _serverConnectionManager.ServerEcho
                                && externalModelAdd.Model.ModifiedByUserName == _userName;

            ProtoUtils.PoseAndScale poseAndScale = ProtoUtils.FromProto(
                externalModelAdd.Model.Transform);

            if (isServerEcho)
            {
                poseAndScale.Pose.position += ServerEchoPositionOffset;
            }

            externalModel.SetPoseAndScale(poseAndScale);
        }

        /// <summary>
        /// Handle a 3D model being removed from the server
        /// </summary>
        /// <param name="externalModelRemove">The 3D model to remove.</param>
        private void HandleExternalModelRemoveReceived(
            ExternalModelRemoveRequest externalModelRemove)
        {
            External3DModel externalModel;
            if (_externalModelMap.TryGetValue(externalModelRemove.Id, out externalModel))
            {
                Debug.Log("Deleting external model " + externalModelRemove.Id
                          + " from anchor " + externalModelRemove.AnchorId);
                PlayOneoffSpatialSound(_eraseAudioPrefab,
                    externalModel.gameObject.transform.position);
                Destroy(externalModel.gameObject);
            }
        }

        /// <summary>
        /// Handle a Unity log messing being posted. Keep track of errors an exceptions.
        /// </summary>
        private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                    _errorsLogged += 1;
                    break;
                case LogType.Assert:
                case LogType.Exception:
#if UNITY_STANDALONE && !UNITY_EDITOR
                    if (condition.StartsWith("DllNotFoundException: MagicLeapXrProvider")
                        // TODO: Support compilation of the japaneseime_unity for standalone apps
                        || condition.StartsWith("DllNotFoundException: japaneseime_unity")
                        // TODO: Remove condition once SDKUNITY-6770 is fixed
                        || condition.StartsWith("Exception: Field currentActivity or type"))
                    {
                        break;
                    }
#endif

                    _exceptionsOrAssertsLogged += 1;
                    break;
            }
        }
    }
}