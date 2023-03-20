using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MagicLeap.DesignToolkit.Audio;
using MagicLeap.DesignToolkit.Input.Controller;
using MagicLeap.DesignToolkit.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;
using TransformExtensions = Unity.XR.CoreUtils.TransformExtensions;

#if !UNITY_EDITOR
using System.IO;
#endif

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
        private DrawSoloAreYourSurePopup _drawSoloAreYourSurePopup;

        [SerializeField]
        private ImportModelsPopup _importModelPopup;

        [SerializeField]
        private JoinUserPopup _joinUserPopup;

        [SerializeField]
        private ColorPickerPopup _colorPickerPopup;

        [SerializeField]
        private KeyboardManager _keyboardManager;

        [Header("Tools")]

        [SerializeField]
        private ScribbleBrush _scribbleBrushTool;

        [SerializeField]
        private PolyBrush _polyBrushTool;

        [SerializeField]
        private EraserTool _eraserTool;

        [SerializeField, Tooltip(
             "The base pose for tools that can have a relative offset from the Controller")]
        private Transform _toolBasePose;

        [SerializeField, Tooltip("The laser pointer ray and cursor game object")]
        private GameObject _rayAndCursor;

        [Header("Sounds")]

        [SerializeField]
        private SoundDefinition _eraseSound;

        [SerializeField]
        private SoundDefinition _userJoinSound;

        [SerializeField]
        private GameObject _oneoffSpatialSoundBehaviorPrefab;

        [Header("Miscellaneous")]

        [SerializeField, Tooltip("The text used to display status information.")]
        private TMP_Text _statusText;

        [SerializeField, Tooltip("Prefab to use for other user Controllers being displayed")]
        private GameObject _otherUserControllerPrefab;

        [SerializeField, Tooltip(
             "Prefab to use for other user Headsets (Wearables) being displayed")]
        private GameObject _otherUserWearablePrefab;

        [SerializeField]
        private GameObject _spaceOriginAxis;

        [SerializeField]
        private FloorGrid _floorGrid;

        [SerializeField]
        private ControlInstructions _controlInstructions;

        private System.Random _random = new();
        private UploadThread _uploadThread;
        private DownloadThread _downloadThread;
        private IEnumerator _updateStatusTextCoroutine;
        private SpaceLocalizationManager _localizationManager;
        private AnchorsManager _anchorsManager;
        private SpaceMeshManager _spaceMeshManager;
        private External3DModelManager _external3dModelManager;
        private DelayedButtonHandler _delayedButtonHandler;
        private BrushColorManager _brushColorManager;
        private ServerConnectionManager _serverConnectionManager;
        private CancellationTokenSource _shutDownTokenSource = new();
        private Dictionary<string, OtherUserWearable> _otherUserWearables = new();
        private Dictionary<string, OtherUserController> _otherUserControllers = new();
        private Dictionary<string, BrushBase> _brushStrokeMap = new();
        private Dictionary<string, External3DModel> _externalModelMap = new();
        private IEnumerator _maybeCreateAnchorAfterLocalizationWithDelayCoroutine;
        private ServerInfoProto _serverInfo;
        private bool _appTooOld;
        private bool _serverTooOld;
        private string _userName;
        private string _userDisplayName;
        private string _headClosestAnchorId;
        private string _appVersion;
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private bool _drawSolo;

        /// <summary>
        /// Enumeration of tools that user can select.
        /// </summary>
        private enum Tool
        {
            Eraser,
            Laser,
            BrushScribble,
            BrushPoly
        }

        private Tool _currentTool = Tool.BrushScribble;
        private int _errorsLogged = 0;
        private int _exceptionsOrAssertsLogged = 0;
        private bool _continuedPastStartPanel;
        private Camera _camera;
        private string _cameraFollowUserName;

        private const float StatusTextUpdateDelaySeconds = .1f;
        private static readonly Vector3 ServerEchoPositionOffset = new(0.1f, 0, 0);
        private static readonly Vector3 ServerEchoWearablePositionOffset = new(.5f, 0, 0);
        private static readonly TimeSpan OtherUserControllerExpirationAge = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan OtherUserWearableExpirationAge = TimeSpan.FromSeconds(5);
        private static readonly Vector3 External3DModelRelativeStartPosition = new(0, 0, 1.5f);

        /// <summary>
        /// Unity lifecycle for the LeapBrush component first initializing
        /// </summary>
        private void Awake()
        {
            Application.logMessageReceived += OnLogMessageReceived;

            _localizationManager = GetComponent<SpaceLocalizationManager>();
            _anchorsManager = GetComponent<AnchorsManager>();
            _spaceMeshManager = GetComponent<SpaceMeshManager>();
            _external3dModelManager = GetComponent<External3DModelManager>();
            _brushColorManager = GetComponent<BrushColorManager>();
            _serverConnectionManager = GetComponent<ServerConnectionManager>();

            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();

            SanityCheckValidAppExecution();
        }

        /// <summary>
        /// Unity lifecycle for the LeapBrush component first starting
        /// </summary>
        private void Start()
        {
#if UNITY_ANDROID
            MLSegmentedDimmer.Activate();
#endif

            _camera = Camera.main;

            // TODO(ghazen): Use a server-provided session identifier.
            _userName = "U" + _random.Next(1, 1000000);
            _userDisplayName = "User " +  _random.Next(1, 10000);
            _appVersion = Application.version;

            _updateStatusTextCoroutine = UpdateStatusTextPeriodically();
            StartCoroutine(_updateStatusTextCoroutine);

            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;

            ControllerInput.Instance.Events.OnMenuDown += OnMenuButtonDown;
            ControllerInput.Instance.Events.OnTriggerDown += OnTriggerButtonDown;
            ControllerInput.Instance.Events.OnTriggerUp += OnTriggerButtonUp;
            ControllerInput.Instance.Events.OnTouchDelta += OnTouchDelta;

            _settingsPanel.Init();
            _settingsPanel.OnShowHeadsetsChanged += OnShowHeadsetsChanged;
            _settingsPanel.OnShowControllersChanged += OnShowControllersChanged;
            _settingsPanel.OnClearAllContentSelected += OnClearAllContentSelected;
            _settingsPanel.OnSettingsPanelHidden += OnSettingsPanelHidden;

            _startPanel.OnContinueSelected += OnStartPanelContinueSelected;
            _startPanel.OnSetUserDisplayName += OnSetUserDisplayName;

            _scribbleBrushTool.OnPosesAdded += OnScribbleBrushStrokePosesAdded;
            _scribbleBrushTool.OnDrawingCompleted += OnScribbleBrushDrawingCompleted;
            _polyBrushTool.OnPosesUpdated += OnPolyBrushStrokePosesUpdated;
            _polyBrushTool.OnDrawingCompleted += OnPolyBrushDrawingCompleted;
            _eraserTool.OnTriggerEnterEvent += OnEraserCollisionTrigger;

            _mainPanel.OnScribbleBrushToolSelected += OnScribbleBrushToolSelected;
            _mainPanel.OnColorPaletteSelected += OnColorPaletteSelected;
            _mainPanel.OnPolyBrushToolSelected += OnPolyBrushToolSelected;
            _mainPanel.OnEraserToolSelected += OnEraserToolSelected;
            _mainPanel.OnLaserPointerSelected += OnLaserPointerSelected;
            _mainPanel.OnImportModelSelected += OnImportModelSelected;
            _mainPanel.OnSettingsSelected += OnSettingsSelected;

            _external3dModelManager.OnModelsListUpdated += OnExternal3DModelsListUpdated;
            _importModelPopup.OnPlaceNewExternal3DModel += OnPlaceNewExternal3DModel;

            _notConnectedPanel.OnChooseServerSelected += OnChooseServerSelected;
            _notConnectedPanel.OnDrawSoloSelected += OnDrawSoloSelected;

            _drawSoloAreYourSurePopup.OnConfirmSelected += OnDrawSoloConfirmButtonSelected;

#if !UNITY_ANDROID
            _floorGrid.gameObject.SetActive(true);
            ControllerInput.Instance.gameObject.SetActive(true);
#endif

            _joinUserPopup.OnJoinUserSessionRemotelySelected += OnJoinUserSessionRemotelySelected;

            UpdateUserBrushColors();
            _brushColorManager.OnBrushColorsChanged += UpdateUserBrushColors;

            LoadUserDisplayName();

            _serverConnectionManager.OnServerUrlChanged += OnServerUrlChanged;
            _serverConnectionManager.LoadServerUrl();
        }

        /// <summary>
        /// Unity lifecycle method run every render frame.
        /// </summary>
        private void Update()
        {
            if (_cameraFollowUserName != null)
            {
                if (_otherUserWearables.TryGetValue(_cameraFollowUserName,
                        out OtherUserWearable otherUserWearable))
                {
                    _camera.transform.SetPositionAndRotation(
                        otherUserWearable.transform.position, otherUserWearable.transform.rotation);
                }
            }

            UpdateUploadThread();
            UpdatePanelVisibility();

            MaybeExpireOtherUserControls();
            MaybeExpireOtherUserWearables();

            ThreadDispatcher.DispatchAll();
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

            Transform controlTransform = ControllerInput.Instance.transform;
            Vector3 controlPosition = controlTransform.position;
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
                _uploadThread.SetHeadPoseRelativeToAnchor(
                    anchorClosestToHeadGameObject.transform
                    .InverseTransformPose(new Pose(headPosition, _camera.transform.rotation)));
                _uploadThread.SetControlPoseRelativeToAnchor(
                    anchorClosestToHeadGameObject.transform
                    .InverseTransformPose(
                        new Pose(controlPosition, controlTransform.rotation)));
            }
            else
            {
                _headClosestAnchorId = null;
                _uploadThread.SetHeadPoseRelativeToAnchor(Pose.identity);
                _uploadThread.SetControlPoseRelativeToAnchor(Pose.identity);
            }

            _uploadThread.SetHeadClosestAnchorId(_headClosestAnchorId);
            _uploadThread.SetBatteryStatus(SystemInfo.batteryLevel, SystemInfo.batteryStatus);
            _uploadThread.SetServerEchoEnabled(_serverConnectionManager.ServerEcho);
            _uploadThread.SetUserDisplayName(_userDisplayName);

            if (_mainMenu.activeSelf || _keyboardManager.gameObject.activeSelf)
            {
                // The menu is being shown to the user -- advertise that the user is using
                // the menu instead of a normal tool.
                _uploadThread.SetCurrentToolState(UserStateProto.Types.ToolState.Menu,
                    _brushColorManager.StrokeColor);
            }
            else
            {
                _uploadThread.SetCurrentToolState(ToProto(_currentTool),
                    _brushColorManager.StrokeColor);
            }
        }

        /// <summary>
        /// Update which panels and UI should be shown.
        /// </summary>
        private void UpdatePanelVisibility()
        {
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
                _keyboardManager.gameObject.SetActive(false);
                _drawSoloAreYourSurePopup.Hide();
            }
            else if (((!_downloadThread?.LastDownloadOk ?? false)
                      || (!_uploadThread?.LastUploadOk ?? false)) && !_drawSolo)
            {
                // The server and client appear to have been disconnected, show the not connected
                // panel.

                if (!_keyboardManager.gameObject.activeSelf)
                {
                    _notConnectedPanel.gameObject.SetActive(true);
                }
                _notLocalizedPanel.gameObject.SetActive(false);
                _startPanel.Hide();
                _mainPanel.gameObject.SetActive(false);
                _settingsPanel.Hide();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            else if (_localizationManager.LocalizationInfo.LocalizationStatus == MLAnchors.LocalizationStatus.NotLocalized ||
                       _localizationManager.LocalizationInfo.LocalizationStatus == MLAnchors.LocalizationStatus.LocalizationPending ||
                       (string.IsNullOrEmpty(_localizationManager.LocalizationInfo.SpaceName) &&
                        string.IsNullOrEmpty(_localizationManager.LocalizationInfo.SpaceId)))
            {
                // The ML2 client doesn't appear to have localized to a space yet. Show the not
                // localized panel which helps them open the Spaces tool to select or create a
                // space.

                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(true);
                _startPanel.Hide();
                _mainPanel.gameObject.SetActive(false);
                _settingsPanel.Hide();
                _keyboardManager.gameObject.SetActive(false);
                _drawSoloAreYourSurePopup.Hide();
            }
#endif
            else
            {
                // Proceed to the start or main panel depending on if the user has continued through
                // yet.

                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(false);
                _drawSoloAreYourSurePopup.Hide();
                if (!_settingsPanel.isActiveAndEnabled && !_keyboardManager.gameObject.activeSelf)
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
        /// Look for other user's Controls that haven't had their poses updated recently, and
        /// remove them when found.
        /// </summary>
        private void MaybeExpireOtherUserControls()
        {
            List<string> userNamesToRemove = null;
            List<GameObject> gameObjectsToDestroy = null;

            DateTimeOffset now = DateTimeOffset.Now;
            foreach (var entry in _otherUserControllers)
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

                if (entry.Value.LastUpdateTime < now - OtherUserControllerExpirationAge)
                {
                    if (gameObjectsToDestroy == null)
                    {
                        gameObjectsToDestroy = new ();
                    }
                    gameObjectsToDestroy.Add(entry.Value.gameObject);
                }
            }

            if (userNamesToRemove != null)
            {
                foreach (string userName in userNamesToRemove)
                {
                    _otherUserControllers.Remove(userName);
                }
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
        /// Look for other user's Headsets (Wearables) that haven't had their poses updated
        /// recently, and remove them when found.
        /// </summary>
        private void MaybeExpireOtherUserWearables()
        {
            List<string> userNamesToRemove = null;
            List<GameObject> gameObjectsToDestroy = null;

            DateTimeOffset now = DateTimeOffset.Now;
            foreach (var entry in _otherUserWearables)
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

                if (entry.Value.LastUpdateTime < now - OtherUserWearableExpirationAge)
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
                    _otherUserWearables.Remove(userName);
                }

                _startPanel.OnOtherUserCountChanged(_otherUserWearables.Count);
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
            ControllerInput.Instance.Events.OnMenuUp -= OnMenuButtonDown;
            ControllerInput.Instance.Events.OnTriggerDown -= OnTriggerButtonDown;
            ControllerInput.Instance.Events.OnTriggerUp -= OnTriggerButtonUp;

            StopServerConnections();
            ThreadDispatcher.DispatchAllAndShutdown();

            StopCoroutine(_updateStatusTextCoroutine);
            if (_maybeCreateAnchorAfterLocalizationWithDelayCoroutine != null)
            {
                StopCoroutine(_maybeCreateAnchorAfterLocalizationWithDelayCoroutine);
            }

            _localizationManager.OnLocalizationInfoChanged -= OnLocalizationInfoChanged;

            _settingsPanel.OnShowHeadsetsChanged -= OnShowHeadsetsChanged;
            _settingsPanel.OnShowControllersChanged -= OnShowControllersChanged;

            _eraserTool.OnTriggerEnterEvent -= OnEraserCollisionTrigger;
            _scribbleBrushTool.OnPosesAdded -= OnScribbleBrushStrokePosesAdded;
            _scribbleBrushTool.OnDrawingCompleted -= OnScribbleBrushDrawingCompleted;
            _polyBrushTool.OnPosesUpdated -= OnPolyBrushStrokePosesUpdated;
            _polyBrushTool.OnDrawingCompleted -= OnPolyBrushDrawingCompleted;

            Application.logMessageReceived -= OnLogMessageReceived;
        }

        /// <summary>
        /// Update the tool colors to match the user's color selection in the Palette menu.
        /// </summary>
        private void UpdateUserBrushColors()
        {
            _scribbleBrushTool.SetColors(
                _brushColorManager.StrokeColor, _brushColorManager.FillColor,
                _brushColorManager.FillDimmerAlpha);
            _polyBrushTool.SetColors(
                _brushColorManager.StrokeColor, _brushColorManager.FillColor,
                _brushColorManager.FillDimmerAlpha);
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
                string userNamePath = Path.Join(
                    persistentDataPath, "userName.txt");

                try
                {
                    using (StreamReader reader = new StreamReader(userNamePath))
                    {
                        string userDisplayName = reader.ReadToEnd().Trim();
                        ThreadDispatcher.ScheduleMain(() =>
                        {
                            SetUserDisplayName(userDisplayName);
                        });
                    }
                }
                catch (IOException e)
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
                string userNamePath = Path.Join(
                    persistentDataPath, "userName.txt");

                try
                {
                    using (StreamWriter writer = new StreamWriter(userNamePath))
                    {
                        writer.Write(userDisplayName);
                    }
                }
                catch (IOException e)
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
                PlayOneoffSpatialSound(_eraseSound, collider.gameObject.transform.position);
                Destroy(collidedBrush.gameObject);

                _uploadThread.RemoveBrushStroke(collidedBrush.Id, collidedBrush.AnchorId);
                return;
            }

            var externalModel = collider.GetComponentInParent<External3DModel>();
            if (externalModel != null && !string.IsNullOrEmpty(externalModel.Id))
            {
                Debug.Log("Erasing external model " + externalModel.Id + " from anchor "
                          + externalModel.AnchorId);
                PlayOneoffSpatialSound(_eraseSound, collider.gameObject.transform.position);
                Destroy(externalModel.gameObject);

                _uploadThread.RemoveExternalModel(externalModel.Id, externalModel.AnchorId);
            }
        }

        /// <summary>
        /// Play a oneoff spatial sound at a particular location.
        /// </summary>
        /// <param name="soundDefinition">The sound definition for the sound to play.</param>
        /// <param name="position">The world position where the sound should play.</param>
        private void PlayOneoffSpatialSound(SoundDefinition soundDefinition, Vector3 position)
        {
            OneoffSpatialSoundBehavior oneoffSoundBehavior = Instantiate(
                _oneoffSpatialSoundBehaviorPrefab, transform)
                .GetComponent<OneoffSpatialSoundBehavior>();
            oneoffSoundBehavior.Initialize(soundDefinition);
            oneoffSoundBehavior.transform.position = position;
        }

        /// <summary>
        /// Handler for map localization changes. The user may have lost or regained localization.
        /// </summary>
        /// <param name="info">The updated localization info.</param>
        private void OnLocalizationInfoChanged(AnchorsApi.LocalizationInfo info)
        {
            if (info.LocalizationStatus == MLAnchors.LocalizationStatus.Localized)
            {
                if (_maybeCreateAnchorAfterLocalizationWithDelayCoroutine == null)
                {
                    _maybeCreateAnchorAfterLocalizationWithDelayCoroutine
                        = MaybeCreateAnchorAfterLocalizationWithDelay();
                    StartCoroutine(_maybeCreateAnchorAfterLocalizationWithDelayCoroutine);
                }

                TransformExtensions.SetWorldPose(
                    _spaceOriginAxis.transform, info.TargetSpaceOriginPose);

                _spaceMeshManager.UpdateSpaceMesh(info.SpaceId, info.TargetSpaceOriginPose);
            }
            else if (_maybeCreateAnchorAfterLocalizationWithDelayCoroutine != null)
            {
                StopCoroutine(_maybeCreateAnchorAfterLocalizationWithDelayCoroutine);
                _maybeCreateAnchorAfterLocalizationWithDelayCoroutine = null;
            }
        }

        /// <summary>
        /// Handler for the user tapping the menu button on their Controller.
        /// </summary>
        private void OnMenuButtonDown()
        {
            if (_continuedPastStartPanel)
            {
                _mainMenu.SetActive(!_mainMenu.activeSelf);
            }

            _cameraFollowUserName = null;

            UpdateActiveTool();
        }

        /// <summary>
        /// Handler for the user pushing the trigger button down on their Controller.
        /// </summary>
        private void OnTriggerButtonDown()
        {
            BrushBase brushTool = GetActiveBrushTool();
            if (brushTool != null)
            {
                brushTool.OnTriggerButtonDown();
            }
        }

        /// <summary>
        /// Handler for the user releasing the trigger button on their Controller.
        /// </summary>
        private void OnTriggerButtonUp()
        {
            BrushBase brushTool = GetActiveBrushTool();
            if (brushTool != null)
            {
                brushTool.OnTriggerButtonUp();
            }
        }

        /// <summary>
        /// Handler for the user swiping on the Controller's touchpad
        /// </summary>
        /// <param name="touchDelta">The 2D delta of the swipe</param>
        private void OnTouchDelta(Vector2 touchDelta)
        {
            BrushBase brushTool = GetActiveBrushTool();
            if (brushTool != null || _eraserTool.isActiveAndEnabled)
            {
                Vector3 newPosition = _toolBasePose.localPosition;
                newPosition.z = Mathf.Clamp(newPosition.z + touchDelta.y * 0.25f, 0.00f, 1.5f);
                _toolBasePose.localPosition = newPosition;
            }
        }

        /// <summary>
        /// Handler for new poses being added to the scribble brush tool while drawing.
        /// </summary>
        private void OnScribbleBrushStrokePosesAdded()
        {
            // Hold the upload thread lock while manipulating _uploadThread.CurrentBrushStroke
            lock (_uploadThread.Lock)
            {
                if (_uploadThread.CurrentBrushStroke == null)
                {
                    if (!_anchorsManager.TryGetAnchorGameObject(_headClosestAnchorId, out _))
                    {
                        return;
                    }

                    BrushStrokeProto brushStrokeProto = new BrushStrokeProto
                    {
                        Id = "B" + _random.Next(0, Int32.MaxValue),
                        AnchorId = _headClosestAnchorId,
                        StrokeColorRgb = ColorUtils.ToRgbaUint(_brushColorManager.StrokeColor),
                    };

                    brushStrokeProto.Type = BrushStrokeProto.Types.BrushType.Scribble;
                    brushStrokeProto.UserName = _userName;
                    _uploadThread.CurrentBrushStroke = brushStrokeProto;

                    _brushColorManager.SetManuallySelected();
                }

                if (_anchorsManager.TryGetAnchorGameObject(
                        _uploadThread.CurrentBrushStroke.AnchorId, out GameObject anchorGameObject))
                {
                    Transform anchorTransform = anchorGameObject.transform;

                    int brushStrokeStartIndex =
                        _uploadThread.CurrentBrushStroke.StartIndex
                        + _uploadThread.CurrentBrushStroke.BrushPose.Count;
                    for (int i = brushStrokeStartIndex; i < _scribbleBrushTool.Poses.Count; ++i)
                    {
                        _uploadThread.CurrentBrushStroke.BrushPose.Add(ProtoUtils.ToProto(
                            anchorTransform.InverseTransformPose(_scribbleBrushTool.Poses[i])));
                    }
                }
            }
        }

        /// <summary>
        /// Handler for new poses being added to the polygon brush while drawing.
        /// </summary>
        /// <param name="startIndex">The start index from where poses were modified.</param>
        private void OnPolyBrushStrokePosesUpdated(int startIndex)
        {
            // Hold the upload thread lock while manipulating _uploadThread.CurrentBrushStroke
            lock (_uploadThread.Lock)
            {
                if (_uploadThread.CurrentBrushStroke == null)
                {
                    if (!_anchorsManager.TryGetAnchorGameObject(_headClosestAnchorId, out _))
                    {
                        return;
                    }

                    BrushStrokeProto brushStrokeProto = new BrushStrokeProto
                    {
                        Id = "B" + _random.Next(0, Int32.MaxValue),
                        AnchorId = _headClosestAnchorId,
                        StrokeColorRgb = ColorUtils.ToRgbaUint(_brushColorManager.StrokeColor),
                        FillColorRgba = ColorUtils.ToRgbaUint(_brushColorManager.FillColor),
                        FillDimmerA = (uint) Math.Round(_brushColorManager.FillDimmerAlpha * 255)
                    };

                    brushStrokeProto.Type = BrushStrokeProto.Types.BrushType.Poly;
                    brushStrokeProto.UserName = _userName;
                    _uploadThread.CurrentBrushStroke = brushStrokeProto;

                    _brushColorManager.SetManuallySelected();
                }

                GameObject anchorGameObject;
                if (_anchorsManager.TryGetAnchorGameObject(
                        _uploadThread.CurrentBrushStroke.AnchorId, out anchorGameObject))
                {
                    Transform anchorTransform = anchorGameObject.transform;

                    if (_uploadThread.CurrentBrushStroke.StartIndex >= startIndex)
                    {
                        _uploadThread.CurrentBrushStroke.StartIndex = startIndex;
                        _uploadThread.CurrentBrushStroke.BrushPose.Clear();
                    }

                    while (startIndex - _uploadThread.CurrentBrushStroke.StartIndex
                           < _uploadThread.CurrentBrushStroke.BrushPose.Count)
                    {
                        _uploadThread.CurrentBrushStroke.BrushPose.RemoveAt(
                            _uploadThread.CurrentBrushStroke.BrushPose.Count - 1);
                    }

                    int brushStrokeStartIndex =
                        _uploadThread.CurrentBrushStroke.StartIndex
                        + _uploadThread.CurrentBrushStroke.BrushPose.Count;
                    for (int i = brushStrokeStartIndex; i < _polyBrushTool.Poses.Count; ++i)
                    {
                        _uploadThread.CurrentBrushStroke.BrushPose.Add(ProtoUtils.ToProto(
                            anchorTransform.InverseTransformPose(_polyBrushTool.Poses[i])));
                    }
                }
            }
        }

        private void OnScribbleBrushDrawingCompleted()
        {
            OnBrushDrawingCompleted(_scribbleBrushTool);
        }

        private void OnPolyBrushDrawingCompleted()
        {
            OnBrushDrawingCompleted(_polyBrushTool);
        }

        /// <summary>
        /// Handler for a brush drawing being completed.
        /// </summary>
        /// <param name="brushTool">The brush tool that completed the drawing.</param>
        private void OnBrushDrawingCompleted(BrushBase brushTool)
        {
            BrushStrokeProto brushStrokeProto;
            // Hold the upload thread lock while manipulating _uploadThread.CurrentBrushStroke
            lock (_uploadThread.Lock)
            {
                // Pull the current brush stroke from the upload thread and clear it.
                brushStrokeProto = _uploadThread.CurrentBrushStroke;
                _uploadThread.CurrentBrushStroke = null;
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
            Pose[] poses = new Pose[brushTool.Poses.Count];
            for (int i = 0; i < poses.Length; i++)
            {
                poses[i] = anchorTransform.InverseTransformPose(brushTool.Poses[i]);
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
            brushStroke.SetColors(_brushColorManager.StrokeColor, _brushColorManager.FillColor,
                _brushColorManager.FillDimmerAlpha);

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
        private void OnShowHeadsetsChanged()
        {
            foreach (var entry in _otherUserWearables)
            {
                entry.Value.gameObject.SetActive(_settingsPanel.IsShowHeadsets);
            }
        }

        /// <summary>
        /// Handler for the user toggling whether other user's Controllers should be displayed.
        /// </summary>
        private void OnShowControllersChanged()
        {
            foreach (var entry in _otherUserControllers)
            {
                entry.Value.gameObject.SetActive(_settingsPanel.IsShowControllers);
            }
        }

        /// <summary>
        /// Handler for the scribble brush being selected as the current tool.
        /// </summary>
        private void OnScribbleBrushToolSelected()
        {
            _mainMenu.SetActive(false);

            _currentTool = Tool.BrushScribble;
            UpdateActiveTool();
        }

        /// <summary>
        /// Handler for the Color Palette menu being selected to open.
        /// </summary>
        private void OnColorPaletteSelected()
        {
            _colorPickerPopup.Show();
        }

        /// <summary>
        /// Handler for the poly brush being selected as the current tool.
        /// </summary>
        private void OnPolyBrushToolSelected()
        {
            _mainMenu.SetActive(false);

            _currentTool = Tool.BrushPoly;
            UpdateActiveTool();
        }

        /// <summary>
        /// Handler for the eraser being selected as the current tool.
        /// </summary>
        private void OnEraserToolSelected()
        {
            _mainMenu.SetActive(false);
            _currentTool = Tool.Eraser;
            UpdateActiveTool();
        }

        /// <summary>
        /// Handler for the laser pointer being selected as the current tool.
        /// </summary>
        private void OnLaserPointerSelected()
        {
            _mainMenu.SetActive(false);
            _currentTool = Tool.Laser;
            UpdateActiveTool();
        }

        /// <summary>
        /// Handler for the import model popup to be displayed.
        /// </summary>
        private void OnImportModelSelected()
        {
            _external3dModelManager.RefreshModelList();
            _importModelPopup.Show();
        }

        /// <summary>
        /// Handler for the settings menu to be displayed.
        /// </summary>
        private void OnSettingsSelected()
        {
            _settingsPanel.Show(_userName, _leapBrushClient);
            _mainPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Handler for the list of external 3D models being refreshed.
        /// </summary>
        private void OnExternal3DModelsListUpdated()
        {
            _importModelPopup.OnExternal3DModelsListUpdated(_external3dModelManager.Models);
        }

        /// <summary>
        /// Handler for the user placing a new 3D model.
        /// </summary>
        /// <param name="modelInfo">The information about the 3D model to place.</param>
        private void OnPlaceNewExternal3DModel(External3DModelManager.ModelInfo modelInfo)
        {
            _importModelPopup.Hide();
            _mainMenu.SetActive(false);

            _currentTool = Tool.Laser;
            UpdateActiveTool();

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
                _cameraFollowUserName = userResult.UserName;
#endif

                _joinUserPopup.Hide();
                _mainMenu.SetActive(false);
                _currentTool = Tool.Laser;
                UpdateActiveTool();
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

        /// <summary>
        /// Handler for the user deciding to select a new server. Opens the keyboard for entry.
        /// </summary>
        private void OnChooseServerSelected()
        {
            _keyboardManager.gameObject.SetActive(true);
            _keyboardManager.OnKeyboardClose.AddListener(OnChooseServerKeyboardClosed);
            _keyboardManager.PublishKeyEvent.AddListener(OnChooseServerKeyboardKeyPressed);

            string serverUrl = _serverConnectionManager.ServerUrl;
            _keyboardManager.TypedContent = serverUrl;
            _keyboardManager.InputField.text = serverUrl;

            _mainMenu.SetActive(false);
        }

        private void OnChooseServerKeyboardKeyPressed(
            string textToType, KeyType keyType, bool doubleClicked, string typedContent)
        {
            if (keyType == KeyType.kEnter || keyType == KeyType.kJPEnter)
            {
                // The enter key was pressed, accept the new server url.
                _serverConnectionManager.SetServerUrl(typedContent.Trim());
            }
        }

        private void OnChooseServerKeyboardClosed()
        {
            _keyboardManager.PublishKeyEvent.RemoveListener(OnChooseServerKeyboardKeyPressed);
            _keyboardManager.OnKeyboardClose.RemoveListener(OnChooseServerKeyboardClosed);

            _serverConnectionManager.SetServerUrl(_keyboardManager.TypedContent.Trim());

            _keyboardManager.gameObject.SetActive(false);
            _mainMenu.SetActive(true);
        }

        private void OnServerUrlChanged(string newServerUrl)
        {
            Debug.LogFormat("Selected the new server {0}", newServerUrl);

            _notConnectedPanel.OnServerUrlChanged(newServerUrl);

            RestartServerConnections();
        }

        /// <summary>
        /// Update the visibility of various tools based on UI state.
        /// </summary>
        private void UpdateActiveTool()
        {
            _scribbleBrushTool.gameObject.SetActive(!_mainMenu.activeSelf && _currentTool == Tool.BrushScribble);
            _polyBrushTool.gameObject.SetActive(!_mainMenu.activeSelf && _currentTool == Tool.BrushPoly);
            _eraserTool.gameObject.SetActive(!_mainMenu.activeSelf && _currentTool == Tool.Eraser);
            _rayAndCursor.gameObject.SetActive(_mainMenu.activeSelf || _currentTool == Tool.Laser);

            if (_mainMenu.activeSelf)
            {
                _controlInstructions.SetInstructionSet(
                    ControlInstructions.InstructionType.MainMenu);
            }
            else
            {
                switch (_currentTool)
                {
                    case Tool.Eraser:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.Eraser);
                        break;
                    case Tool.Laser:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.LaserPointer);
                        break;
                    case Tool.BrushScribble:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.Brush);
                        break;
                    case Tool.BrushPoly:
                        _controlInstructions.SetInstructionSet(
                            ControlInstructions.InstructionType.PolyTool);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Get the currently active brush tool.
        /// </summary>
        /// <returns>The currently active brush tool or null if a brush is not active</returns>
        private BrushBase GetActiveBrushTool()
        {
            if (_scribbleBrushTool.isActiveAndEnabled)
            {
                return _scribbleBrushTool;
            }
            if (_polyBrushTool.isActiveAndEnabled)
            {
                return _polyBrushTool;
            }

            return null;
        }

        /// <summary>
        /// Handler for the user picking the draw solo option -- a confirmation dialog is displayed.
        /// </summary>
        public void OnDrawSoloSelected()
        {
            _drawSoloAreYourSurePopup.Show();
        }

        /// <summary>
        /// Handler for the user confirming they want to use the draw solo option. Server
        /// connections are severed and the user is now drawing offline.
        /// </summary>
        private void OnDrawSoloConfirmButtonSelected()
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

            if (_anchorsManager.Anchors.Length == 0)
            {
                AnchorsApi.Anchor anchor;
                MLResult result = AnchorsApi.CreateAnchor(
                    Pose.identity, (ulong) TimeSpan.FromDays(365).TotalSeconds, out anchor);
                if (result.IsOk)
                {
                    result = anchor.Publish();
                    if (!result.IsOk)
                    {
                        Debug.LogError("Failed to publish new anchor " + anchor.Id + ": " + result);
                    }
                }
                else
                {
                    Debug.LogError("Failed to create new anchor: " + result);
                }
            }
        }

        /// <summary>
        /// Coroutine to periodically up the the status text.
        /// </summary>
        private IEnumerator UpdateStatusTextPeriodically()
        {
            while (true)
            {
                UpdateStatusText();

                yield return new WaitForSeconds(StatusTextUpdateDelaySeconds);
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

            string serverUrlAndVersionString;
            if (_drawSolo)
            {
                serverUrlAndVersionString = "<color=#ffa500>Drawing Solo</color>";
            }
            else if (_serverInfo != null)
            {
                serverUrlAndVersionString = string.Format(
                    "{0} (v{1})", _serverConnectionManager.ServerUrl, _serverInfo.ServerVersion);
            }
            else
            {
                serverUrlAndVersionString = _serverConnectionManager.ServerUrl;
            }

            string serverDetailsString;
            if (_drawSolo)
            {
                serverDetailsString = "<color=#ffa500>Drawing Solo</color>";
            }
            else if ((_downloadThread?.LastDownloadOk ?? false)
                     && (_uploadThread?.LastUploadOk ?? false))
            {
                serverDetailsString = string.Format(
                    "{0}: {1}", serverUrlAndVersionString, "<color=#00ff00>Connected</color>");
            }
            else
            {
                serverDetailsString = string.Format(
                    "{0}: {1}", serverUrlAndVersionString, "<color=#ff0000>Disconnected</color>");
            }

            string localizationString = _localizationManager.LocalizationInfo.ToString();
            if (_localizationManager.LocalizationInfo.LocalizationStatus == MLAnchors.LocalizationStatus.Localized &&
                _localizationManager.LocalizationInfo.MappingMode == MLAnchors.MappingMode.ARCloud)
            {
                localizationString = "<color=#00ff00>" + localizationString + "</color>";
            }
            else
            {
                localizationString = "<color=#ffa500>" + localizationString + "</color>";
            }

            StringBuilder statusTextBuilder = new StringBuilder();
            statusTextBuilder.AppendFormat(
                "<color=#dbfb76><b>Leap Brush v{0}</b></color>\n" +
                "UserName: {1}\n" +
                "Server Connection: {2}\n" +
                "Map Localization: <i>{3}</i>\n",
                Application.version,
                _userDisplayName,
                serverDetailsString,
                localizationString);

            if (_exceptionsOrAssertsLogged > 0)
            {
                statusTextBuilder.AppendFormat(
                    "<color=#ee0000><b>{0} errors and {1} exceptions logged</b></color>\n",
                    _errorsLogged,
                    _exceptionsOrAssertsLogged);
            }
            else if (_errorsLogged > 0)
            {
                statusTextBuilder.AppendFormat(
                    "<color=#dbfb76><b>{0} errors logged</b></color>\n",
                    _errorsLogged);
            }

            _statusText.text = statusTextBuilder.ToString();
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
                        _leapBrushClient, _shutDownTokenSource, _userName);
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
            if (!HandleServerInfo(resp.ServerInfo))
            {
                return;
            }

            foreach (UserStateProto otherUserState in resp.UserState)
            {
                HandleOtherUserStateReceived(otherUserState);
            }

            foreach (BrushStrokeAddRequest brushAdd in resp.BrushStrokeAdd)
            {
                HandleBrushStrokeAddReceived(brushAdd);
            }

            foreach (BrushStrokeRemoveRequest brushRemove in resp.BrushStrokeRemove)
            {
                HandleBrushStrokeRemoveReceived(brushRemove);
            }

            foreach (ExternalModelAddRequest externalModelAdd in resp.ExternalModelAdd)
            {
                HandleExteralModelAddReceived(externalModelAdd);
            }

            foreach (ExternalModelRemoveRequest externalModelRemove in resp.ExternalModelRemove)
            {
                HandlExternalModelRemoveReceived(externalModelRemove);
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
                HandleOtherUserWearableReceived(otherUserState, anchorGameObject, isServerEcho);
            }
            if (otherUserState.ControlPose != null)
            {
                HandleOtherUserControllerReceived(otherUserState, anchorGameObject, isServerEcho);
            }
        }

        /// <summary>
        /// Handler another user's Wearable pose being received.
        /// </summary>
        /// <param name="otherUserState">The other user's updated state</param>
        /// <param name="anchorGameObject">
        /// The anchor game object that the other user's wearable pose is attached to.
        /// </param>
        /// <param name="isServerEcho">
        /// Whether this event was a server echo from the current user. (Useful for testing
        /// networking).
        /// </param>
        private void HandleOtherUserWearableReceived(UserStateProto otherUserState,
            GameObject anchorGameObject, bool isServerEcho)
        {
            OtherUserWearable otherUserWearable;
            bool newlyJoinedUser = false;
            if (!_otherUserWearables.TryGetValue(otherUserState.UserName, out otherUserWearable))
            {
                newlyJoinedUser = true;
                Debug.Log("Found other user " + otherUserState.UserName);
                otherUserWearable = Instantiate(_otherUserWearablePrefab,
                        anchorGameObject.transform)
                    .GetComponent<OtherUserWearable>();
                otherUserWearable.gameObject.SetActive(_settingsPanel.IsShowHeadsets);

                _otherUserWearables[otherUserState.UserName] = otherUserWearable;
                otherUserWearable.OnDestroyed += () =>
                {
                    _otherUserWearables.Remove(otherUserState.UserName);
                    _startPanel.OnOtherUserCountChanged(_otherUserWearables.Count);
                };

                _startPanel.OnOtherUserCountChanged(_otherUserWearables.Count);
            }

            if (otherUserWearable.transform.parent != anchorGameObject.transform)
            {
                otherUserWearable.transform.SetParent(anchorGameObject.transform);
            }

            TransformExtensions.SetLocalPose(otherUserWearable.transform,
                ProtoUtils.FromProto(otherUserState.HeadPose));
            if (isServerEcho)
            {
                otherUserWearable.transform.localPosition += ServerEchoWearablePositionOffset;
            }

            if (otherUserState.UserDisplayName != null)
            {
                otherUserWearable.SetUserDisplayName(otherUserState.UserDisplayName);
            }

            if (otherUserState.HeadsetBattery != null)
            {
                otherUserWearable.SetHeadsetBattery(otherUserState.HeadsetBattery);
            }

            otherUserWearable.LastUpdateTime = DateTimeOffset.Now;

            if (newlyJoinedUser && !isServerEcho)
            {
                PlayOneoffSpatialSound(_userJoinSound, otherUserWearable.transform.position);
            }

            _floorGrid.FoundContentAtPosition(otherUserWearable.transform.position);
        }

        /// <summary>
        /// Handler another user's Controller pose being received.
        /// </summary>
        /// <param name="otherUserState">The other user's updated state</param>
        /// <param name="anchorGameObject">
        /// The anchor game object that the other user's Controller pose is attached to.
        /// </param>
        /// <param name="isServerEcho">
        /// Whether this event was a server echo from the current user. (Useful for testing
        /// networking).
        /// </param>
        private void HandleOtherUserControllerReceived(UserStateProto otherUserState,
            GameObject anchorGameObject, bool isServerEcho)
        {
            OtherUserController otherUserController;
            if (!_otherUserControllers.TryGetValue(otherUserState.UserName,
                    out otherUserController))
            {
                otherUserController = Instantiate(_otherUserControllerPrefab,
                        anchorGameObject.transform)
                    .GetComponent<OtherUserController>();
                otherUserController.gameObject.SetActive(_settingsPanel.IsShowControllers);

                _otherUserControllers[otherUserState.UserName] = otherUserController;
                otherUserController.OnDestroyed += () =>
                    _otherUserControllers.Remove(otherUserState.UserName);
            }

            if (otherUserController.transform.parent != anchorGameObject.transform)
            {
                otherUserController.transform.SetParent(anchorGameObject.transform);
            }

            TransformExtensions.SetLocalPose(otherUserController.transform,
                ProtoUtils.FromProto(otherUserState.ControlPose));
            if (isServerEcho)
            {
                otherUserController.transform.localPosition += ServerEchoPositionOffset;
            }

            otherUserController.LastUpdateTime = DateTimeOffset.Now;

            _floorGrid.FoundContentAtPosition(otherUserController.transform.position);
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
                        _polyBrushTool.Prefab : _scribbleBrushTool.Prefab;
                brushStroke = Instantiate(brushStrokePrefab, anchorGameObject.transform)
                    .GetComponent<BrushBase>();
                brushStroke.AnchorId = brushAdd.BrushStroke.AnchorId;
                brushStroke.Id = brushAdd.BrushStroke.Id;
                brushStroke.UserName = brushAdd.BrushStroke.UserName;
                brushStroke.IsServerEcho = isServerEcho;

                Color32 strokeColor = brushAdd.BrushStroke.StrokeColorRgb != 0 ?
                    ColorUtils.FromRgbaUint(brushAdd.BrushStroke.StrokeColorRgb) :
                    _brushColorManager.FallbackBrushColor;
                Color32 fillColor = brushAdd.BrushStroke.FillColorRgba != 0 ?
                    ColorUtils.FromRgbaUint(brushAdd.BrushStroke.FillColorRgba) :
                    Color.clear;
                float fillDimmerAlpha = brushAdd.BrushStroke.FillDimmerA != 0 ?
                    (float) brushAdd.BrushStroke.FillDimmerA / 255 : 0;
                brushStroke.SetColors(strokeColor, fillColor, fillDimmerAlpha);

                if (brushAdd.BrushStroke.StrokeColorRgb != 0 && !isServerEcho)
                {
                    _brushColorManager.OtherUserBrushColorObserved(strokeColor);
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

                foreach (Pose pose in poses)
                {
                    _floorGrid.FoundContentAtPosition(
                        pose.GetTransformedBy(anchorGameObject.transform).position);
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
                PlayOneoffSpatialSound(_eraseSound, brushStroke.gameObject.transform.position);
                Destroy(brushStroke.gameObject);
            }
        }

        /// <summary>
        /// Handle a new or updated 3D model being received from the server.
        /// </summary>
        /// <param name="externalModelAdd">The 3D model to add or update.</param>
        private void HandleExteralModelAddReceived(ExternalModelAddRequest externalModelAdd)
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

            TransformExtensions.SetLocalPose(externalModel.gameObject.transform, poseAndScale.Pose);
            if (!isServerEcho)
            {
                externalModel.gameObject.transform.localScale = poseAndScale.Scale;
            }
        }

        /// <summary>
        /// Handle a 3D model being removed from the server
        /// </summary>
        /// <param name="externalModelRemove">The 3D model to remove.</param>
        private void HandlExternalModelRemoveReceived(
            ExternalModelRemoveRequest externalModelRemove)
        {
            External3DModel externalModel;
            if (_externalModelMap.TryGetValue(externalModelRemove.Id, out externalModel))
            {
                Debug.Log("Deleting external model " + externalModelRemove.Id
                          + " from anchor " + externalModelRemove.AnchorId);
                PlayOneoffSpatialSound(_eraseSound, externalModel.gameObject.transform.position);
                Destroy(externalModel.gameObject);
            }
        }

        /// <summary>
        /// Convert a current tool enum value to the protocol buffer equivalent.
        /// </summary>
        private static UserStateProto.Types.ToolState ToProto(Tool currentTool)
        {
            switch (currentTool)
            {
                case Tool.Eraser:
                    return UserStateProto.Types.ToolState.Eraser;
                case Tool.Laser:
                    return UserStateProto.Types.ToolState.Laser;
                case Tool.BrushScribble:
                    return UserStateProto.Types.ToolState.BrushScribble;
                case Tool.BrushPoly:
                    return UserStateProto.Types.ToolState.BrushPoly;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentTool), currentTool, null);
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
                    _exceptionsOrAssertsLogged += 1;
                    break;
            }
        }
    }
}