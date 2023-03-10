using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Grpc.Core;
using MagicLeap.DesignToolkit.Actions;
using MagicLeap.DesignToolkit.Audio;
using MagicLeap.DesignToolkit.Input.Controller;
using MagicLeap.DesignToolkit.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.MagicLeap;
using TransformExtensions = Unity.XR.CoreUtils.TransformExtensions;

#if !UNITY_EDITOR
using System.IO;
#endif

namespace MagicLeap.LeapBrush
{
    [RequireComponent(typeof(AnchorsManager))]
    [RequireComponent(typeof(SpaceMeshManager))]
    public class LeapBrush : MonoBehaviour
    {
        [SerializeField, Tooltip("The text used to display status information.")]
        private TMP_Text _statusText;

        [Tooltip("Enable to receive back user state from the server")]
        private bool _serverEcho = false;

        [SerializeField]
        private string _defaultServerUrl;

        [SerializeField]
        private string _minServerVersion = ".2";

        [SerializeField]
        private GameObject _otherUserControllerPrefab;

        [SerializeField]
        private GameObject _otherUserWearablePrefab;

        [SerializeField]
        private GameObject _mainMenu;

        [SerializeField]
        private ScribbleBrush _scribbleBrushTool;

        [SerializeField]
        private PolyBrush _polyBrushTool;

        [SerializeField]
        private EraserTool _eraserTool;

        [SerializeField]
        private Transform _toolBasePose;

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
        private GameObject _rayAndCursor;

        [SerializeField]
        private GameObject _spaceOriginAxis;

        [SerializeField]
        private TMP_Text _startDescriptionText;

        [SerializeField]
        private TMP_Text _notConnectedDescriptionText;

        [SerializeField]
        private Interactable _changeUserNameButton;

        [SerializeField]
        private Interactable _openSpacesAppButton;

        [SerializeField]
        private Interactable _chooseServerButton;

        [SerializeField]
        private Interactable _drawSoloButton;

        [SerializeField]
        private DrawSoloAreYourSurePopup _drawSoloAreYourSurePopup;

        [SerializeField]
        private Interactable _drawSoloCancelButton;

        [SerializeField]
        private Interactable _drawSoloContinueButton;

        [SerializeField]
        private Interactable _scribbleBrushButton;

        [SerializeField]
        private Interactable _colorPaletteButton;

        [SerializeField]
        private Interactable _polyBrushButton;

        [SerializeField]
        private Interactable _eraserToolButton;

        [SerializeField]
        private Interactable _laserPointerButton;

        [SerializeField]
        private Interactable _importModelButton;

        [SerializeField]
        private Interactable _settingsButton;

        [SerializeField]
        private ImportModelsPopup _importModelPopup;

        [SerializeField]
        private JoinUserPopup _joinUserPopup;

        [SerializeField]
        private ColorPickerPopup _colorPickerPopup;

        [SerializeField]
        private KeyboardManager _keyboardManager;

        [SerializeField]
        private FloorGrid _floorGrid;

        [SerializeField]
        private ControlInstructions _controlInstructions;

        [SerializeField]
        private SoundDefinition _eraseSound;

        [SerializeField]
        private SoundDefinition _userJoinSound;

        [SerializeField]
        private GameObject _oneoffSpatialSoundBehaviorPrefab;

        private System.Random _random = new();
        private Thread _uploadThread;
        private Thread _downloadThread;
        private IEnumerator _updateStatusTextCoroutine;
        private SpaceLocalizationManager _localizationManager;
        private AnchorsManager _anchorsManager;
        private SpaceMeshManager _spaceMeshManager;
        private External3DModelManager _external3dModelManager;
        private DelayedButtonHandler _delayedButtonHandler;
        private BrushColorManager _brushColorManager;
        private CancellationTokenSource _shutDownTokenSource = new();
        private Dictionary<string, OtherUserWearable> _otherUserWearables = new();
        private Dictionary<string, OtherUserController> _otherUserControllers = new();
        private Dictionary<string, BrushBase> _brushStrokeMap = new();
        private Dictionary<string, External3DModel> _externalModelMap = new();
        private IEnumerator _maybeCreateAnchorAfterLocalizationWithDelayCoroutine;
        private LeapBrushApiFactory _leapBrushApiFactory = new();
        private ServerInfoProto _serverInfo;
        private bool _appTooOld;
        private bool _serverTooOld;

        private object _lock = new();
        private string _userName;
        private string _userDisplayName;
        private string _appVersion;
        private SpaceInfoProto _spaceInfo = new();
        private string _headClosestAnchorId;
        private Pose _controlPoseRelativeToAnchor = Pose.identity;
        private Pose _headPoseRelativeToAnchor = Pose.identity;
        private bool _serverEchoEnabled;
        private bool _lastServerUploadOk;
        private bool _lastServerDownloadOk;
        private string _persistentDataPath;
        private BrushStrokeProto _currentBrushStroke;
        private UserStateProto.Types.ToolState _currentToolState;
        private Color32 _currentToolColor;
        private BatteryStatusProto _batteryStatus = new();
        private LinkedList<BrushStrokeProto> _brushStrokesToUpload = new();
        private LinkedList<BrushStrokeRemoveRequest> _brushStrokesToRemove = new();
        private Dictionary<string, ExternalModelProto> _externalModelsToUpdate = new();
        private LinkedList<ExternalModelRemoveRequest> _externalModelsToRemove = new();
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private bool _drawSolo;

        private object _serverUrlLock = new();
        private string _serverUrl;

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
        private const float MinServerUpdateIntervalSeconds = .03f;
        private const float ServerPingIntervalSeconds = 2.0f;
        private static readonly Vector3 ServerEchoPositionOffset = new(0.1f, 0, 0);
        private static readonly Vector3 ServerEchoWearablePositionOffset = new(.5f, 0, 0);
        private static readonly TimeSpan OtherUserControllerExpirationAge = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan OtherUserWearableExpirationAge = TimeSpan.FromSeconds(5);
        private static readonly Vector3 External3DModelRelativeStartPosition = new(0, 0, 1.5f);

        private void Awake()
        {
            Application.logMessageReceived += OnLogMessageReceived;

            _localizationManager = GetComponent<SpaceLocalizationManager>();
            _anchorsManager = GetComponent<AnchorsManager>();
            _spaceMeshManager = GetComponent<SpaceMeshManager>();
            _external3dModelManager = GetComponent<External3DModelManager>();
            _brushColorManager = GetComponent<BrushColorManager>();

            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();

            SanityCheckValidAppExecution();
        }

        private void Start()
        {
#if UNITY_ANDROID
            MLSegmentedDimmer.Activate();
#endif

            _camera = Camera.main;

            lock (_lock)
            {
                // TODO(ghazen): Use a server-provided session identifier.
                _userName = "U" + _random.Next(1, 1000000);
                _userDisplayName = "User " +  _random.Next(1, 10000);
                _persistentDataPath = Application.persistentDataPath;
                _appVersion = Application.version;
            }

            ThreadDispatcher.ScheduleWork(StartServerConnectionsOnWorkerThread);

            _updateStatusTextCoroutine = UpdateStatusTextPeriodically();
            StartCoroutine(_updateStatusTextCoroutine);

            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;

            ControllerInput.Instance.Events.OnMenuDown += OnMenuButtonDown;
            ControllerInput.Instance.Events.OnTriggerDown += OnTriggerButtonDown;
            ControllerInput.Instance.Events.OnTriggerUp += OnTriggerButtonUp;
            ControllerInput.Instance.Events.OnTouchDelta += OnTouchDelta;

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

            _scribbleBrushButton.Events.OnSelect.AddListener(OnScribbleBrushToolSelected);
            _colorPaletteButton.Events.OnSelect.AddListener(OnColorPaletteSelected);
            _polyBrushButton.Events.OnSelect.AddListener(OnPolyBrushToolSelected);
            _eraserToolButton.Events.OnSelect.AddListener(OnEraserToolSelected);
            _laserPointerButton.Events.OnSelect.AddListener(OnLaserPointerSelected);
            _importModelButton.Events.OnSelect.AddListener(OnImportModelButtonSelected);
            _settingsButton.Events.OnSelect.AddListener(OnSettingsButtonSelected);
            _openSpacesAppButton.Events.OnSelect.AddListener(OnStartSpacesAppButtonSelected);
            _chooseServerButton.Events.OnSelect.AddListener(OnChooseServerButtonSelected);
            _drawSoloButton.Events.OnSelect.AddListener(OnDrawSoloButtonSelected);
            _drawSoloContinueButton.Events.OnSelect.AddListener(OnDrawSoloContinueButtonSelected);
            _drawSoloCancelButton.Events.OnSelect.AddListener(OnDrawSoloCancelButtonSelected);

            _external3dModelManager.OnModelsListUpdated += OnExternal3DModelsListUpdated;
            _importModelPopup.OnPlaceNewExternal3DModel += OnPlaceNewExternal3DModel;

#if !UNITY_ANDROID
            _floorGrid.gameObject.SetActive(true);
#endif

#if !UNITY_ANDROID
            ControllerInput.Instance.gameObject.SetActive(true);
#endif

            _joinUserPopup.OnJoinUserSessionRemotelySelected += OnJoinUserSessionRemotelySelected;

            UpdateUserBrushColors();
            _brushColorManager.OnBrushColorsChanged += UpdateUserBrushColors;

            LoadUserDisplayName();
        }

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

            bool drawSolo;

            lock (_lock)
            {
                Transform controlTransform = ControllerInput.Instance.transform;
                Vector3 controlPosition = controlTransform.position;
                Vector3 headPosition = _camera.transform.position;

                _spaceInfo.SpaceId = _localizationManager.LocalizationInfo.SpaceId ?? "";
                _spaceInfo.SpaceName = _localizationManager.LocalizationInfo.SpaceName ?? "";
                _spaceInfo.MappingMode =  ProtoUtils.ToProto(
                    _localizationManager.LocalizationInfo.MappingMode);
                _spaceInfo.TargetSpaceOrigin = ProtoUtils.ToProto(
                    _localizationManager.LocalizationInfo.TargetSpaceOriginPose);
                _spaceInfo.UsingImportedAnchors = _anchorsManager.IsUsingImportedAnchors;

                AnchorsApi.Anchor anchorClosestToHead = null;
                _headClosestAnchorId = null;
                float minHeadToAnchorDistanceSqr = Mathf.Infinity;
                if (_spaceInfo.Anchor.Count > _anchorsManager.Anchors.Length)
                {
                    _spaceInfo.Anchor.Clear();
                }

                for (int i = 0; i < _anchorsManager.Anchors.Length; ++i)
                {
                    AnchorsApi.Anchor anchor = _anchorsManager.Anchors[i];

                    if (i >= _spaceInfo.Anchor.Count)
                    {
                        _spaceInfo.Anchor.Add(new AnchorProto());
                    }

                    AnchorProto anchorProto = _spaceInfo.Anchor[i];
                    anchorProto.Id = anchor.Id;
                    anchorProto.Pose = ProtoUtils.ToProto(anchor.Pose);

                    float headDistanceToAnchorSqr =
                        (anchor.Pose.position - headPosition).sqrMagnitude;
                    if (headDistanceToAnchorSqr < minHeadToAnchorDistanceSqr)
                    {
                        anchorClosestToHead = anchor;
                        minHeadToAnchorDistanceSqr = headDistanceToAnchorSqr;
                    }
                }

                _batteryStatus.Level = (uint) Mathf.RoundToInt(
                    Mathf.Clamp01(SystemInfo.batteryLevel) * 100);
                _batteryStatus.State = ProtoUtils.ToProto(SystemInfo.batteryStatus);

                GameObject anchorClosestToHeadGameObject = null;
                if (anchorClosestToHead != null)
                {
                    _anchorsManager.TryGetAnchorGameObject(anchorClosestToHead.Id, out anchorClosestToHeadGameObject);
                }

                if (_cameraFollowUserName == null)
                {
                    if (anchorClosestToHeadGameObject != null)
                    {
                        _headClosestAnchorId = anchorClosestToHead.Id;
                        _headPoseRelativeToAnchor = anchorClosestToHeadGameObject.transform
                            .InverseTransformPose(new Pose(headPosition, _camera.transform.rotation));
                        _controlPoseRelativeToAnchor = anchorClosestToHeadGameObject.transform
                            .InverseTransformPose(new Pose(controlPosition, controlTransform.rotation));
                    }
                    else
                    {
                        _headClosestAnchorId = null;
                        _headPoseRelativeToAnchor = Pose.identity;
                        _controlPoseRelativeToAnchor = Pose.identity;
                    }
                }

                _serverEchoEnabled = _serverEcho;

                if (_mainMenu.activeSelf || _keyboardManager.gameObject.activeSelf)
                {
                    _currentToolState = UserStateProto.Types.ToolState.Menu;
                }
                else
                {
                    _currentToolState = ToProto(_currentTool);
                }

                _currentToolColor = _brushColorManager.StrokeColor;

                drawSolo = _drawSolo;
            }

            if (_appTooOld || _serverTooOld)
            {
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
                _drawSoloAreYourSurePopup.gameObject.SetActive(false);
            }
            else if ((!_lastServerDownloadOk || !_lastServerUploadOk) && !drawSolo)
            {
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
                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(true);
                _startPanel.Hide();
                _mainPanel.gameObject.SetActive(false);
                _settingsPanel.Hide();
                _keyboardManager.gameObject.SetActive(false);
                _drawSoloAreYourSurePopup.gameObject.SetActive(false);
            }
#endif
            else
            {
                _notConnectedPanel.gameObject.SetActive(false);
                _notLocalizedPanel.gameObject.SetActive(false);
                _drawSoloAreYourSurePopup.gameObject.SetActive(false);
                if (!_settingsPanel.isActiveAndEnabled && !_keyboardManager.gameObject.activeSelf)
                {
                    if (_continuedPastStartPanel)
                    {
                        _startPanel.Hide();
                        _mainPanel.gameObject.SetActive(true);
                    }
                    else
                    {
                        string userDisplayName;
                        lock (_lock)
                        {
                            userDisplayName = _userDisplayName;
                        }

                        _startPanel.Show(userDisplayName);
                        _mainPanel.gameObject.SetActive(false);
                    }
                }
            }

            MaybeExpireOtherUserControls();
            MaybeExpireOtherUserWearables();

            ThreadDispatcher.DispatchAll();
        }

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

        private void OnDestroy()
        {
            ControllerInput.Instance.Events.OnMenuUp -= OnMenuButtonDown;
            ControllerInput.Instance.Events.OnTriggerDown -= OnTriggerButtonDown;
            ControllerInput.Instance.Events.OnTriggerUp -= OnTriggerButtonUp;

            StopServerConnections();

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

        private void UpdateUserBrushColors()
        {
            _scribbleBrushTool.SetColors(
                _brushColorManager.StrokeColor, _brushColorManager.FillColor,
                _brushColorManager.FillDimmerAlpha);
            _polyBrushTool.SetColors(
                _brushColorManager.StrokeColor, _brushColorManager.FillColor,
                _brushColorManager.FillDimmerAlpha);
        }

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

        private void UpdateNotConnectedDescriptionText()
        {
            LocalizeStringEvent textLocalized =
                _notConnectedDescriptionText.GetComponent<LocalizeStringEvent>();

            lock (_serverUrlLock)
            {
                ((StringVariable) textLocalized.StringReference["ServerHostAndPort"]).Value
                    = _serverUrl;
            }

            textLocalized.StringReference.RefreshString();
        }

        private void OnEraserCollisionTrigger(Collider collider)
        {
            var collidedBrush = collider.GetComponentInParent<BrushBase>();
            if (collidedBrush != null && !string.IsNullOrEmpty(collidedBrush.Id))
            {
                Debug.Log("Erasing brush stroke " + collidedBrush.Id + " from anchor "
                          + collidedBrush.AnchorId);
                PlayOneoffSpatialSound(_eraseSound, collider.gameObject.transform.position);
                Destroy(collidedBrush.gameObject);

                lock (_lock)
                {
                    _brushStrokesToRemove.AddLast(
                        new BrushStrokeRemoveRequest {Id = collidedBrush.Id, AnchorId = collidedBrush.AnchorId});
                }

                return;
            }

            var externalModel = collider.GetComponentInParent<External3DModel>();
            if (externalModel != null && !string.IsNullOrEmpty(externalModel.Id))
            {
                Debug.Log("Erasing external model " + externalModel.Id + " from anchor "
                          + externalModel.AnchorId);
                PlayOneoffSpatialSound(_eraseSound, collider.gameObject.transform.position);
                Destroy(externalModel.gameObject);

                lock (_lock)
                {
                    _externalModelsToRemove.AddLast(
                        new ExternalModelRemoveRequest() {Id = externalModel.Id, AnchorId = externalModel.AnchorId});
                }
            }
        }

        private void PlayOneoffSpatialSound(SoundDefinition soundDefinition, Vector3 position)
        {
            OneoffSpatialSoundBehavior oneoffSoundBehavior = Instantiate(
                _oneoffSpatialSoundBehaviorPrefab, transform)
                .GetComponent<OneoffSpatialSoundBehavior>();
            oneoffSoundBehavior.Initialize(soundDefinition);
            oneoffSoundBehavior.transform.position = position;
        }

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

        private void OnMenuButtonDown()
        {
            if (_continuedPastStartPanel)
            {
                _mainMenu.SetActive(!_mainMenu.activeSelf);
            }

            _cameraFollowUserName = null;

            UpdateActiveTool();
        }

        private void OnTriggerButtonDown()
        {
            BrushBase brushTool = GetActiveBrushTool();
            if (brushTool != null)
            {
                brushTool.OnTriggerButtonDown();
            }
        }

        private void OnTriggerButtonUp()
        {
            BrushBase brushTool = GetActiveBrushTool();
            if (brushTool != null)
            {
                brushTool.OnTriggerButtonUp();
            }
        }

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

        private void OnScribbleBrushStrokePosesAdded()
        {
            if (_currentBrushStroke == null)
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

                lock (_lock)
                {
                    brushStrokeProto.UserName = _userName;
                    _currentBrushStroke = brushStrokeProto;
                }

                _brushColorManager.SetManuallySelected();
            }

            if (_anchorsManager.TryGetAnchorGameObject(
                    _currentBrushStroke.AnchorId, out GameObject anchorGameObject))
            {
                Transform anchorTransform = anchorGameObject.transform;

                lock (_lock)
                {
                    int brushStrokeStartIndex =
                        _currentBrushStroke.StartIndex + _currentBrushStroke.BrushPose.Count;
                    for (int i = brushStrokeStartIndex; i < _scribbleBrushTool.Poses.Count; ++i)
                    {
                        _currentBrushStroke.BrushPose.Add(ProtoUtils.ToProto(
                            anchorTransform.InverseTransformPose(_scribbleBrushTool.Poses[i])));
                    }
                }
            }
        }

        private void OnPolyBrushStrokePosesUpdated(int startIndex)
        {
            if (_currentBrushStroke == null)
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

                lock (_lock)
                {
                    brushStrokeProto.UserName = _userName;
                    _currentBrushStroke = brushStrokeProto;
                }

                _brushColorManager.SetManuallySelected();
            }

            GameObject anchorGameObject;
            if (_anchorsManager.TryGetAnchorGameObject(
                    _currentBrushStroke.AnchorId, out anchorGameObject))
            {
                Transform anchorTransform = anchorGameObject.transform;

                lock (_lock)
                {
                    if (_currentBrushStroke.StartIndex >= startIndex)
                    {
                        _currentBrushStroke.StartIndex = startIndex;
                        _currentBrushStroke.BrushPose.Clear();
                    }

                    while (startIndex - _currentBrushStroke.StartIndex < _currentBrushStroke.BrushPose.Count)
                    {
                        _currentBrushStroke.BrushPose.RemoveAt(_currentBrushStroke.BrushPose.Count - 1);
                    }

                    int brushStrokeStartIndex =
                        _currentBrushStroke.StartIndex + _currentBrushStroke.BrushPose.Count;
                    for (int i = brushStrokeStartIndex; i < _polyBrushTool.Poses.Count; ++i)
                    {
                        _currentBrushStroke.BrushPose.Add(ProtoUtils.ToProto(
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

        private void OnBrushDrawingCompleted(BrushBase brushTool)
        {
            BrushStrokeProto brushStrokeProto;
            lock (_lock)
            {
                brushStrokeProto = _currentBrushStroke;
                _currentBrushStroke = null;
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
                lock (_lock)
                {
                    _brushStrokesToRemove.AddLast(
                        new BrushStrokeRemoveRequest
                        {
                            Id = brushStrokeProto.Id,
                            AnchorId = brushStrokeProto.AnchorId
                        });
                }
                return;
            }

            BrushBase brushStroke = Instantiate(
                brushTool.Prefab, anchorGameObject.transform).GetComponent<BrushBase>();
            brushStroke.SetPosesAndTruncate(0, poses, false);
            brushStroke.SetColors(_brushColorManager.StrokeColor, _brushColorManager.FillColor,
                _brushColorManager.FillDimmerAlpha);

            lock (_lock)
            {
                brushStroke.AnchorId = brushStrokeProto.AnchorId;
                brushStroke.Id = brushStrokeProto.Id;
                brushStroke.UserName = _userName;

                _brushStrokeMap[brushStrokeProto.Id] = brushStroke;
                brushStroke.OnDestroyed += OnBrushStrokeDestroyed;

                _brushStrokesToUpload.AddLast(brushStrokeProto);
            }
        }

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

        private void OnExternalModelTransformChanged(External3DModel externalModel)
        {
            GameObject anchorGameObject;
            if (_anchorsManager.TryGetAnchorGameObject(externalModel.AnchorId, out anchorGameObject))
            {
                lock (_lock)
                {
                    ExternalModelProto modelProto;
                    if (!_externalModelsToUpdate.TryGetValue(externalModel.Id, out modelProto))
                    {
                        modelProto = new ExternalModelProto()
                        {
                            Id = externalModel.Id,
                            AnchorId = externalModel.AnchorId,
                            FileName = externalModel.FileName,
                        };
                        _externalModelsToUpdate[externalModel.Id] = modelProto;
                    }

                    modelProto.ModifiedByUserName = _userName;
                    modelProto.Transform = externalModel.TransformProto;
                }
            }
        }

        private void OnShowHeadsetsChanged()
        {
            foreach (var entry in _otherUserWearables)
            {
                entry.Value.gameObject.SetActive(_settingsPanel.IsShowHeadsets);
            }
        }

        private void OnShowControllersChanged()
        {
            foreach (var entry in _otherUserControllers)
            {
                entry.Value.gameObject.SetActive(_settingsPanel.IsShowControllers);
            }
        }

        public void OnScribbleBrushToolSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _mainMenu.SetActive(false);

                _currentTool = Tool.BrushScribble;
                UpdateActiveTool();
            });
        }

        public void OnColorPaletteSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _colorPickerPopup.Show();
            });
        }

        public void OnPolyBrushToolSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _mainMenu.SetActive(false);

                _currentTool = Tool.BrushPoly;
                UpdateActiveTool();
            });
        }

        public void OnEraserToolSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _mainMenu.SetActive(false);
                _currentTool = Tool.Eraser;
                UpdateActiveTool();
            });
        }

        public void OnLaserPointerSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _mainMenu.SetActive(false);
                _currentTool = Tool.Laser;
                UpdateActiveTool();
            });
        }

        public void OnImportModelButtonSelected(Interactor interactor)
        {
            _external3dModelManager.RefreshModelList();

            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _importModelPopup.Show();
            });
        }

        public void OnExternal3DModelsListUpdated()
        {
            _importModelPopup.OnExternal3DModelsListUpdated(_external3dModelManager.Models);
        }

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

                lock (_lock)
                {
                    _externalModelsToUpdate[externalModel.Id] = new ExternalModelProto
                    {
                        Id = externalModel.Id,
                        AnchorId = externalModel.AnchorId,
                        FileName = modelInfo.FileName,
                        ModifiedByUserName = _userName,
                        Transform = ProtoUtils.ToProto(externalModel.transform)
                    };
                }
            }
        }

        public void OnSettingsButtonSelected(Interactor interactor)
        {
            string userName;
            lock (_lock)
            {
                userName = _userName;
            }

            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _settingsPanel.Show(userName, _leapBrushClient);
                _mainPanel.gameObject.SetActive(false);
            });
        }

        private void OnJoinUserSessionRemotelySelected(QueryUsersResponse.Types.Result userResult)
        {
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

        private void OnClearAllContentSelected()
        {
            foreach (var entry in _brushStrokeMap)
            {
                Destroy(entry.Value.gameObject);

                lock (_lock)
                {
                    _brushStrokesToRemove.AddLast(
                        new BrushStrokeRemoveRequest
                        {
                            Id = entry.Value.Id,
                            AnchorId = entry.Value.AnchorId
                        });
                }
            }

            foreach (var entry in _externalModelMap)
            {
                Destroy(entry.Value.gameObject);

                lock (_lock)
                {
                    _externalModelsToRemove.AddLast(
                        new ExternalModelRemoveRequest()
                        {
                            Id = entry.Value.Id,
                            AnchorId = entry.Value.AnchorId
                        });
                }
            }
        }

        private void OnSettingsPanelHidden()
        {
            _mainPanel.gameObject.SetActive(true);
        }

        private void OnStartPanelContinueSelected()
        {
            _continuedPastStartPanel = true;
            _mainPanel.gameObject.SetActive(true);

            _anchorsManager.SetContentShown(true);
        }

        private void OnSetUserDisplayName(string userDisplayName)
        {
            SetUserDisplayName(userDisplayName);
        }

        private void SetUserDisplayName(string userDisplayName)
        {
            if (string.IsNullOrWhiteSpace(userDisplayName))
            {
                return;
            }

            lock (_lock)
            {
                _userDisplayName = userDisplayName;
            }

            SaveUserDisplayName(userDisplayName);
            _startPanel.OnUserDisplayNameChanged(userDisplayName);
        }

        private void OnChooseServerButtonSelected(Interactor interactor)
        {
            _keyboardManager.gameObject.SetActive(true);
            _keyboardManager.OnKeyboardClose.AddListener(OnChooseServerKeyboardClosed);
            _keyboardManager.PublishKeyEvent.AddListener(OnChooseServerKeyboardKeyPressed);

            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                lock (_serverUrlLock)
                {
                    _keyboardManager.TypedContent = _serverUrl;
                    _keyboardManager.InputField.text = _serverUrl;
                }

                _mainMenu.SetActive(false);
            });
        }

        private void OnChooseServerKeyboardKeyPressed(
            string textToType, KeyType keyType, bool doubleClicked, string typedContent)
        {
            if (keyType == KeyType.kEnter || keyType == KeyType.kJPEnter)
            {
                OnNewServerChosen(typedContent.Trim());
            }
        }

        private void OnChooseServerKeyboardClosed()
        {
            _keyboardManager.PublishKeyEvent.RemoveListener(OnChooseServerKeyboardKeyPressed);
            _keyboardManager.OnKeyboardClose.RemoveListener(OnChooseServerKeyboardClosed);

            OnNewServerChosen(_keyboardManager.TypedContent.Trim());

            _keyboardManager.gameObject.SetActive(false);
            _mainMenu.SetActive(true);
        }

        private void OnNewServerChosen(string newServerUrl)
        {
            if (string.IsNullOrEmpty(newServerUrl))
            {
                return;
            }

            lock (_serverUrlLock)
            {
                if (_serverUrl == newServerUrl)
                {
                    return;
                }
                _serverUrl = newServerUrl;
            }

            Debug.LogFormat("Selected the new server {0}", _serverUrl);

            SaveServerUrl(_serverUrl);
            UpdateNotConnectedDescriptionText();

            RestartServerConnections();
        }

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

        public void OnStartSpacesAppButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                SpacesAppApi.StartApp();
            });
        }

        public void OnDrawSoloButtonSelected(Interactor interactor)
        {
            _drawSoloAreYourSurePopup.gameObject.SetActive(true);
        }

        public void OnDrawSoloContinueButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _drawSoloAreYourSurePopup.gameObject.SetActive(false);

                lock (_lock)
                {
                    _drawSolo = true;
                }

                _settingsPanel.OnDrawSolo();
                _startPanel.OnDrawSolo();

                RestartServerConnections();
            });
        }

        public void OnDrawSoloCancelButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                _drawSoloAreYourSurePopup.gameObject.SetActive(false);
            });
        }

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

        private IEnumerator UpdateStatusTextPeriodically()
        {
            while (true)
            {
                UpdateStatusText();

                yield return new WaitForSeconds(StatusTextUpdateDelaySeconds);
            }
        }

        private void UpdateStatusText()
        {
            if (!_statusText.gameObject.activeInHierarchy)
            {
                return;
            }

            string userDisplayName;
            bool lastServerUploadOk;
            bool lastServerDownloadOk;
            bool drawSolo;
            lock (_lock)
            {
                userDisplayName = _userDisplayName;
                lastServerUploadOk = _lastServerUploadOk;
                lastServerDownloadOk = _lastServerDownloadOk;
                drawSolo = _drawSolo;
            }

            string serverUrlAndVersionString;
            lock (_serverUrlLock)
            {
                if (drawSolo)
                {
                    serverUrlAndVersionString = "<color=#ffa500>Drawing Solo</color>";
                }
                else if (_serverInfo != null)
                {
                    serverUrlAndVersionString = string.Format(
                        "{0} (v{1})", _serverUrl, _serverInfo.ServerVersion);
                }
                else
                {
                    serverUrlAndVersionString = _serverUrl;
                }
            }

            string serverDetailsString;
            if (drawSolo)
            {
                serverDetailsString = "<color=#ffa500>Drawing Solo</color>";
            }
            else if (lastServerDownloadOk && lastServerUploadOk)
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
                userDisplayName,
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

        private void StartServerConnectionsOnWorkerThread()
        {
            bool drawSolo;
            string persistentDataPath;
            lock (_lock)
            {
                drawSolo = _drawSolo;
                persistentDataPath = _persistentDataPath;
            }

            _shutDownTokenSource = new();
            _leapBrushClient = _leapBrushApiFactory.Connect(
                GetOrLoadServerUrl(), drawSolo, persistentDataPath);

            _uploadThread = new Thread(() => UploadThreadBody(
                _leapBrushClient, _shutDownTokenSource));
            _uploadThread.Start();

            _downloadThread = new Thread(() => DownloadThreadBody(
                _leapBrushClient, _shutDownTokenSource));
            _downloadThread.Start();
        }

        private void StopServerConnections()
        {
            _shutDownTokenSource.Cancel();
            if (_leapBrushClient != null)
            {
                Debug.Log("Shutting down leap brush client channel...");
                _leapBrushClient.CloseAndWait();
            }

            if (_uploadThread != null)
            {
                Debug.Log("Joining upload thread...");
                _uploadThread.Join();
                Debug.Log("Upload thread joined");
            }

            if (_downloadThread != null)
            {
                Debug.Log("Joining download thread...");
                _downloadThread.Join();
                Debug.Log("Upload download joined");
            }
        }

        private void RestartServerConnections()
        {
            ThreadDispatcher.ScheduleWork(() =>
            {
                StopServerConnections();
                StartServerConnectionsOnWorkerThread();
            });
        }

        private void UploadThreadBody(LeapBrushApiBase.LeapBrushClient leapBrushClient,
            CancellationTokenSource shutDownTokenSource)
        {
            try
            {
                LeapBrushApiBase.UpdateDeviceStream updateDeviceStream =
                    leapBrushClient.UpdateDeviceStream();

                UserStateProto userState = new UserStateProto();
                lock (_lock)
                {
                    userState.UserName = _userName;
                }
#if UNITY_ANDROID && !UNITY_EDITOR
                userState.DeviceType = UserStateProto.Types.DeviceType.MagicLeap;
#else
                userState.DeviceType = UserStateProto.Types.DeviceType.DesktopSpectator;
                #endif

                SpaceInfoProto spaceInfo = new SpaceInfoProto();

                UpdateDeviceRequest updateRequest = new UpdateDeviceRequest();
                updateRequest.UserState = userState;

                DateTimeOffset lastUpdateTime = DateTimeOffset.MinValue;
                while (!shutDownTokenSource.IsCancellationRequested)
                {
                    TimeSpan sleepTime = (lastUpdateTime + TimeSpan.FromSeconds(MinServerUpdateIntervalSeconds)
                                          - DateTimeOffset.Now);
                    if (sleepTime.Milliseconds > 0)
                    {
                        Thread.Sleep(sleepTime);
                    }

                    bool sendUpdate;

                    updateRequest.SpaceInfo = null;
                    updateRequest.BrushStrokeAdd = null;
                    updateRequest.BrushStrokeRemove = null;
                    updateRequest.ExternalModelAdd = null;
                    updateRequest.ExternalModelRemove = null;

                    lock (_lock)
                    {
                        sendUpdate = !_lastServerUploadOk || lastUpdateTime
                            + TimeSpan.FromSeconds(ServerPingIntervalSeconds) < DateTimeOffset.Now;

                        if (UpdateUserStateGetWasChanged(userState, _userDisplayName, _headClosestAnchorId,
                                _headPoseRelativeToAnchor, _controlPoseRelativeToAnchor,
                                _currentToolState, _currentToolColor, _batteryStatus))
                        {
                            sendUpdate = true;
                        }

                        if (!_lastServerUploadOk || UpdateSpaceInfoGetWasChanged(spaceInfo, _spaceInfo))
                        {
                            sendUpdate = true;
                            updateRequest.SpaceInfo = spaceInfo;
                        }

                        if (updateRequest.Echo != _serverEchoEnabled)
                        {
                            sendUpdate = true;
                            updateRequest.Echo = _serverEchoEnabled;
                        }

                        if (_currentBrushStroke != null && _currentBrushStroke.BrushPose.Count > 0)
                        {
                            sendUpdate = true;
                            BrushStrokeProto brushStrokeProto = new();
                            if (_currentBrushStroke.StartIndex == 0)
                            {
                                brushStrokeProto.MergeFrom(_currentBrushStroke);
                            }
                            else
                            {
                                brushStrokeProto.Id = _currentBrushStroke.Id;
                                brushStrokeProto.AnchorId = _currentBrushStroke.AnchorId;
                                brushStrokeProto.StartIndex = _currentBrushStroke.StartIndex;
                                brushStrokeProto.BrushPose.AddRange(_currentBrushStroke.BrushPose);
                            }

                            updateRequest.BrushStrokeAdd = new BrushStrokeAddRequest {BrushStroke = brushStrokeProto};
                            _currentBrushStroke.StartIndex += _currentBrushStroke.BrushPose.Count;
                            _currentBrushStroke.BrushPose.Clear();
                        }
                        else if (_brushStrokesToUpload.Count > 0)
                        {
                            sendUpdate = true;
                            updateRequest.BrushStrokeAdd = new BrushStrokeAddRequest {BrushStroke = _brushStrokesToUpload.First.Value};
                            _brushStrokesToUpload.RemoveFirst();
                        }

                        if (_brushStrokesToRemove.Count > 0)
                        {
                            sendUpdate = true;
                            updateRequest.BrushStrokeRemove = _brushStrokesToRemove.First.Value;
                            _brushStrokesToRemove.RemoveFirst();
                        }

                        if (_externalModelsToUpdate.Count > 0)
                        {
                            sendUpdate = true;
                            string firstModelId = null;
                            foreach (string modelId in _externalModelsToUpdate.Keys)
                            {
                                firstModelId = modelId;
                                break;
                            }
                            updateRequest.ExternalModelAdd = new ExternalModelAddRequest()
                            {
                                Model = _externalModelsToUpdate[firstModelId]
                            };
                            _externalModelsToUpdate.Remove(firstModelId);
                        }

                        if (_externalModelsToRemove.Count > 0)
                        {
                            sendUpdate = true;
                            updateRequest.ExternalModelRemove = _externalModelsToRemove.First.Value;
                            _externalModelsToRemove.RemoveFirst();
                        }
                    }

                    if (!sendUpdate)
                    {
                        continue;
                    }

                    lastUpdateTime = DateTimeOffset.Now;

                    try
                    {
                        updateDeviceStream.Write(updateRequest);

                        lock (_lock)
                        {
                            if (!_lastServerUploadOk)
                            {
                                Debug.Log("UpdateDevice started succeeding");
                            }

                            _lastServerUploadOk = true;
                        }
                    }
                    catch (RpcException e)
                    {
                        lock (_lock)
                        {
                            if (_lastServerUploadOk)
                            {
                                Debug.LogWarning("UpdateDevice started failing: " + e);
                            }

                            _lastServerUploadOk = false;
                        }
                    }
                }

                Debug.Log("Upload thread: Shutting down");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static bool UpdateUserStateGetWasChanged(UserStateProto userState,
            string userDisplayName, string headClosestAnchorId, Pose headPoseRelativeToAnchor,
            Pose controlPoseRelativeToAnchor, UserStateProto.Types.ToolState currentToolState,
            Color32 currentToolColor, BatteryStatusProto batteryStatus)
        {
            if (userState.AnchorId.Length > 0 && headClosestAnchorId == null)
            {
                userState.AnchorId = "";
                userState.HeadPose = null;
                userState.ControlPose = null;
                return true;
            }
            if (userState.AnchorId.Length == 0 && headClosestAnchorId != null)
            {
                userState.AnchorId = headClosestAnchorId;
                userState.HeadPose = ProtoUtils.ToProto(headPoseRelativeToAnchor);
                userState.ControlPose = ProtoUtils.ToProto(controlPoseRelativeToAnchor);
                return true;
            }
            if (userState.AnchorId.Length == 0 && headClosestAnchorId == null)
            {
                return false;
            }

            uint currentToolColorUint = ColorUtils.ToRgbaUint(currentToolColor);

            if (ProtoUtils.EpsilonEquals(headPoseRelativeToAnchor, userState.HeadPose)
                && ProtoUtils.EpsilonEquals(controlPoseRelativeToAnchor, userState.ControlPose)
                && currentToolState == userState.ToolState
                && currentToolColorUint == userState.ToolColorRgb
                && userDisplayName == userState.UserDisplayName
                && batteryStatus.Equals(userState.HeadsetBattery))
            {
                return false;
            }

            userState.AnchorId = headClosestAnchorId;
            userState.HeadPose = ProtoUtils.ToProto(headPoseRelativeToAnchor);
            userState.ControlPose = ProtoUtils.ToProto(controlPoseRelativeToAnchor);
            userState.ToolState = currentToolState;
            userState.ToolColorRgb = currentToolColorUint;
            userState.UserDisplayName = userDisplayName;
            userState.HeadsetBattery = batteryStatus.Clone();
            return true;
        }

        private static bool UpdateSpaceInfoGetWasChanged(SpaceInfoProto toSpaceInfo,
            SpaceInfoProto fromSpaceInfo)
        {
            bool modified = false;

            if (toSpaceInfo.SpaceId != fromSpaceInfo.SpaceId ||
                toSpaceInfo.SpaceName != fromSpaceInfo.SpaceName ||
                toSpaceInfo.MappingMode != fromSpaceInfo.MappingMode)
            {
                modified = true;
                toSpaceInfo.SpaceId = fromSpaceInfo.SpaceId;
                toSpaceInfo.SpaceName = fromSpaceInfo.SpaceName;
                toSpaceInfo.MappingMode = fromSpaceInfo.MappingMode;
            }
            if (toSpaceInfo.TargetSpaceOrigin == null || !ProtoUtils.EpsilonEquals(
                    toSpaceInfo.TargetSpaceOrigin, fromSpaceInfo.TargetSpaceOrigin))
            {
                modified = true;
                toSpaceInfo.TargetSpaceOrigin = fromSpaceInfo.TargetSpaceOrigin;
            }

            if (toSpaceInfo.UsingImportedAnchors != fromSpaceInfo.UsingImportedAnchors)
            {
                modified = true;
                toSpaceInfo.UsingImportedAnchors = fromSpaceInfo.UsingImportedAnchors;
            }

            if (toSpaceInfo.Anchor.Count > fromSpaceInfo.Anchor.Count)
            {
                modified = true;
                toSpaceInfo.Anchor.Clear();
            }

            for (int i = 0; i < fromSpaceInfo.Anchor.Count; ++i)
            {
                AnchorProto fromAnchor = fromSpaceInfo.Anchor[i];
                if (i >= toSpaceInfo.Anchor.Count)
                {
                    modified = true;
                    toSpaceInfo.Anchor.Add(new AnchorProto());
                }
                AnchorProto toAnchor = toSpaceInfo.Anchor[i];
                if (fromAnchor.Id != toAnchor.Id)
                {
                    modified = true;
                    toAnchor.Id = fromAnchor.Id;
                }

                if (toAnchor.Pose == null
                    || !ProtoUtils.EpsilonEquals(toAnchor.Pose, fromAnchor.Pose))
                {
                    modified = true;
                    toAnchor.Pose = fromAnchor.Pose;
                }
            }

            return modified;
        }

        private void DownloadThreadBody(LeapBrushApiBase.LeapBrushClient leapBrushClient,
            CancellationTokenSource shutDownTokenSource)
        {
            try
            {
                while (!shutDownTokenSource.IsCancellationRequested)
                {
                    RegisterDeviceRequest registerDeviceRequest = new RegisterDeviceRequest();
                    lock (_lock)
                    {
                        registerDeviceRequest.UserName = _userName;
                        registerDeviceRequest.AppVersion = _appVersion;
                    }

                    using var call = leapBrushClient.RegisterAndListen(registerDeviceRequest);

                    try
                    {
                        while (!shutDownTokenSource.IsCancellationRequested)
                        {
                            ServerStateResponse resp = call.GetNext(shutDownTokenSource.Token);
                            lock (_lock)
                            {
                                if (!_lastServerDownloadOk)
                                {
                                    Debug.Log("Downloads started succeeding");
                                    _lastServerDownloadOk = true;
                                }
                            }

                            ThreadDispatcher.ScheduleMain(() => HandleServerStateOnMainThread(resp));
                        }
                    }
                    catch (RpcException e)
                    {
                        lock (_lock)
                        {
                            if (_lastServerDownloadOk)
                            {
                                Debug.LogWarning("Downloads started failing: " + e);
                                _lastServerDownloadOk = false;
                            }
                        }
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }

                Debug.Log("Download thread: Shutting down");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void HandleServerStateOnMainThread(ServerStateResponse resp)
        {
            if (resp.ServerInfo != null)
            {
                bool drawSolo;
                lock (_lock)
                {
                    drawSolo = _drawSolo;
                }

                _serverInfo = resp.ServerInfo;
                if (!VersionUtil.IsGreatorOrEqual(_appVersion, _serverInfo.MinAppVersion) && !_appTooOld)
                {
                    _appTooOld = true;
                    Debug.LogErrorFormat("Shutting down due to outdated app ({0} < {1})",
                        _appVersion, _serverInfo.MinAppVersion);
                    _shutDownTokenSource.Cancel();
                    return;
                }
                if (!VersionUtil.IsGreatorOrEqual(_serverInfo.ServerVersion, _minServerVersion)
                    && !_serverTooOld && !drawSolo)
                {
                    _serverTooOld = true;
                    Debug.LogErrorFormat("Shutting down due to outdated server ({0} < {1})",
                        _serverInfo.ServerVersion, _minServerVersion);
                    _shutDownTokenSource.Cancel();
                    return;
                }

                Debug.LogFormat("Server: version {0}, min app version {1}",
                    _serverInfo.ServerVersion, _serverInfo.MinAppVersion);
            }
            else if (_serverInfo == null && !_serverTooOld)
            {
                _serverTooOld = true;
                Debug.LogError("Shutting down due to outdated server (no version found)");
                _shutDownTokenSource.Cancel();
                return;
            }

            if (_serverTooOld || _appTooOld)
            {
                return;
            }

            foreach (UserStateProto otherUserState in resp.UserState)
            {
                if (otherUserState.AnchorId.Length == 0)
                {
                    continue;
                }

                GameObject anchorGameObject;
                if (!_anchorsManager.TryGetAnchorGameObject(otherUserState.AnchorId, out anchorGameObject))
                {
                    continue;
                }

                bool isServerEcho;
                lock (_lock)
                {
                    isServerEcho = (_serverEcho && otherUserState.UserName == _userName);
                }

                if (otherUserState.HeadPose != null)
                {
                    HandleOtherUserWearableReceived(otherUserState, anchorGameObject, isServerEcho);
                }
                if (otherUserState.ControlPose != null)
                {
                    HandleOtherUserControllerReceived(otherUserState, anchorGameObject, isServerEcho);
                }
            }

            foreach (BrushStrokeAddRequest brushAdd in resp.BrushStrokeAdd)
            {
                GameObject anchorGameObject;
                if (!_anchorsManager.TryGetAnchorGameObject(brushAdd.BrushStroke.AnchorId, out anchorGameObject))
                {
                    continue;
                }

                BrushBase brushStroke;
                _brushStrokeMap.TryGetValue(brushAdd.BrushStroke.Id, out brushStroke);

                bool isServerEcho;
                lock (_lock)
                {
                    if (brushStroke != null)
                    {
                        isServerEcho = _serverEcho && brushStroke.UserName == _userName;
                    }
                    else
                    {
                        isServerEcho = _serverEcho && brushAdd.BrushStroke.UserName == _userName;
                    }
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

            foreach (BrushStrokeRemoveRequest brushRemove in resp.BrushStrokeRemove)
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

            foreach (ExternalModelAddRequest externalModelAdd in resp.ExternalModelAdd)
            {
                GameObject anchorGameObject;
                if (!_anchorsManager.TryGetAnchorGameObject(externalModelAdd.Model.AnchorId, out anchorGameObject))
                {
                    continue;
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

                bool isServerEcho;
                lock (_lock)
                {
                    isServerEcho = (_serverEcho && externalModelAdd.Model.ModifiedByUserName == _userName);
                }

                ProtoUtils.PoseAndScale poseAndScale = ProtoUtils.FromProto(externalModelAdd.Model.Transform);

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

            foreach (ExternalModelRemoveRequest externalModelRemove in resp.ExternalModelRemove)
            {
                External3DModel externalModel;
                if (_externalModelMap.TryGetValue(externalModelRemove.Id, out externalModel))
                {
                    Debug.Log("Deleting external model " + externalModelRemove.Id + " from anchor "
                              + externalModelRemove.AnchorId);
                    PlayOneoffSpatialSound(_eraseSound, externalModel.gameObject.transform.position);
                    Destroy(externalModel.gameObject);
                }
            }
        }

        private void HandleOtherUserWearableReceived(UserStateProto otherUserState,
            GameObject anchorGameObject, bool isServerEcho)
        {
            OtherUserWearable otherUserWearable;
            bool newlyJoinedUser = false;
            if (!_otherUserWearables.TryGetValue(otherUserState.UserName, out otherUserWearable))
            {
                newlyJoinedUser = true;
                Debug.Log("Found other user " + otherUserState.UserName);
                otherUserWearable = Instantiate(_otherUserWearablePrefab, anchorGameObject.transform)
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

        private void HandleOtherUserControllerReceived(UserStateProto otherUserState,
            GameObject anchorGameObject, bool isServerEcho)
        {
            OtherUserController otherUserController;
            if (!_otherUserControllers.TryGetValue(otherUserState.UserName, out otherUserController))
            {
                otherUserController = Instantiate(_otherUserControllerPrefab, anchorGameObject.transform)
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

            TransformExtensions.SetLocalPose(otherUserController.transform, ProtoUtils.FromProto(otherUserState.ControlPose));
            if (isServerEcho)
            {
                otherUserController.transform.localPosition += ServerEchoPositionOffset;
            }

            otherUserController.LastUpdateTime = DateTimeOffset.Now;

            _floorGrid.FoundContentAtPosition(otherUserController.transform.position);
        }

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

        private string GetOrLoadServerUrl()
        {
            lock (_serverUrlLock)
            {
                if (!string.IsNullOrEmpty(_serverUrl))
                {
                    return _serverUrl;
                }

#if !UNITY_EDITOR
                string serverHostPortPrefPath;
                lock (_lock)
                {
                    serverHostPortPrefPath = Path.Join(_persistentDataPath, "serverHostPort.txt");
                }

                try
                {
                    using (StreamReader reader = new StreamReader(serverHostPortPrefPath))
                    {
                        _serverUrl = reader.ReadToEnd().Trim();
                    }
                }
                catch (FileNotFoundException)
                {
                    Debug.Log(string.Format(
                        "LeapBrush server host:port not configured! Set a url by placing it in the file {0}",
                        serverHostPortPrefPath));
                }
#endif

                if (string.IsNullOrEmpty(_serverUrl))
                {
                    if (!string.IsNullOrEmpty(_defaultServerUrl))
                    {
                        _serverUrl = _defaultServerUrl;
                    }
                    else
                    {
                        _serverUrl = "localhost:8402";
                    }
                }

                ThreadDispatcher.ScheduleMain(UpdateNotConnectedDescriptionText);
                return _serverUrl;
            }
        }


        private static void SaveServerUrl(string serverUrl)
        {
#if !UNITY_EDITOR
            string persistentDataPath = Application.persistentDataPath;

            ThreadDispatcher.ScheduleWork(() =>
            {
                string userNamePath = Path.Join(persistentDataPath, "serverHostPort.txt");

                try
                {
                    using (StreamWriter writer = new StreamWriter(userNamePath))
                    {
                        writer.Write(serverUrl);
                    }
                }
                catch (IOException e)
                {
                }
            });
#endif
        }

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