using System;
using MagicLeap.DesignToolkit.Actions;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The settings panel UI.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public bool IsShowHeadsets => _showHeadsetsToggle.IsOn;
        public bool IsShowControllers => _showControllersToggle.IsOn;

        public event Action OnShowHeadsetsChanged;
        public event Action OnShowControllersChanged;

        public event Action OnClearAllContentSelected;
        public event Action OnSettingsPanelHidden;

        [Header("External Dependencies")]

        [SerializeField]
        private AnchorsManager _anchorsManager;

        [SerializeField]
        private SpaceLocalizationManager _localizationManager;

        [SerializeField]
        private SpaceMeshManager _spaceMeshManager;

        [SerializeField]
        private GameObject _worldOriginAxis;

        [SerializeField]
        private GameObject _spaceOriginAxis;

        [SerializeField]
        private FloorGrid _floorGrid;

        [SerializeField]
        private JoinUserPopup _joinUserPopup;

        [Header("Internal Dependencies")]

        [SerializeField]
        private Interactable _settingsBackButton;

        [SerializeField]
        private Toggleable _showSpatialAnchorsToggle;

        [SerializeField]
        private Toggleable _showOriginsToggle;

        [SerializeField]
        private Toggleable _showHeadsetsToggle;

        [SerializeField]
        private Toggleable _showControllersToggle;

        [SerializeField]
        private Toggleable _showFloorGridToggle;

        [SerializeField]
        private Toggleable _showSpaceMeshToggle;

        [SerializeField]
        private Interactable _joinSessionButton;

        [SerializeField]
        private Interactable _leaveSessionButton;

        [SerializeField]
        private Interactable _clearAllContentButton;

        private string _userName;
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private DelayedButtonHandler _delayedButtonHandler;

        public void Show(string userName, LeapBrushApiBase.LeapBrushClient leapBrushClient)
        {
            _userName = userName;
            _leapBrushClient = leapBrushClient;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                OnSettingsPanelHidden?.Invoke();
            }
        }

        public void OnSessionJoined()
        {
            _leaveSessionButton.gameObject.SetActive(true);
        }

        public void OnDrawSolo()
        {
            _joinSessionButton.gameObject.SetActive(false);
            _leaveSessionButton.gameObject.SetActive(false);
        }

        private void Awake()
        {
            _delayedButtonHandler = gameObject.AddComponent<DelayedButtonHandler>();
        }

        public void Init()
        {
            // TODO: it would be cleaner to create a settings manager class that was separate
            // from UI logic

            _showControllersToggle.On();

#if !UNITY_ANDROID
            _showHeadsetsToggle.On();
            _showSpaceMeshToggle.On();
#endif
        }

        private void Start()
        {
            _showSpatialAnchorsToggle.Events.On.AddListener(OnToggleShowAnchors);
            _showSpatialAnchorsToggle.Events.Off.AddListener(OnToggleShowAnchors);

            _showOriginsToggle.Events.On.AddListener(OnToggleShowOrigins);
            _showOriginsToggle.Events.Off.AddListener(OnToggleShowOrigins);

            _showHeadsetsToggle.Events.On.AddListener(OnToggleShowHeadsets);
            _showHeadsetsToggle.Events.Off.AddListener(OnToggleShowHeadsets);

            _showControllersToggle.Events.On.AddListener(OnToggleShowControllers);
            _showControllersToggle.Events.Off.AddListener(OnToggleShowControllers);

            _showFloorGridToggle.Events.On.AddListener(OnToggleShowFloorGrid);
            _showFloorGridToggle.Events.Off.AddListener(OnToggleShowFloorGrid);

            _showSpaceMeshToggle.Events.On.AddListener(OnToggleShowSpaceMesh);
            _showSpaceMeshToggle.Events.Off.AddListener(OnToggleShowSpaceMesh);

            _settingsBackButton.Events.OnSelect.AddListener(OnSettingsBackButtonSelected);
            _clearAllContentButton.Events.OnSelect.AddListener(OnClearAllContentButtonSelected);

            _joinSessionButton.Events.OnSelect.AddListener(OnJoinSessionButtonSelected);
            _leaveSessionButton.Events.OnSelect.AddListener(OnLeaveSessionButtonSelected);
            _leaveSessionButton.gameObject.SetActive(false);

#if UNITY_ANDROID
            _showFloorGridToggle.gameObject.SetActive(false);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            _clearAllContentButton.gameObject.SetActive(false);
#endif

            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;
        }

        private void OnDestroy()
        {
            _localizationManager.OnLocalizationInfoChanged -= OnLocalizationInfoChanged;
        }

        private void OnLocalizationInfoChanged(AnchorsApi.LocalizationInfo localizationInfo)
        {
            UpdateSpaceOriginAxisVisibility();
        }

        public void OnToggleShowAnchors()
        {
            _anchorsManager.SetShown(_showSpatialAnchorsToggle.IsOn);
        }

        private void OnToggleShowOrigins()
        {
            _worldOriginAxis.SetActive(_showOriginsToggle.IsOn);
            UpdateSpaceOriginAxisVisibility();
        }

        private void OnToggleShowHeadsets()
        {
            OnShowHeadsetsChanged?.Invoke();
        }

        private void OnToggleShowControllers()
        {
            OnShowControllersChanged?.Invoke();
        }

        private void OnToggleShowFloorGrid()
        {
            _floorGrid.gameObject.SetActive(_showFloorGridToggle.IsOn);
        }

        private void OnToggleShowSpaceMesh()
        {
            _spaceMeshManager.SetShown(_showSpaceMeshToggle.IsOn);
        }

        private void OnSettingsBackButtonSelected(Interactor interactor)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        private void OnJoinSessionButtonSelected(Interactor _)
        {
            _joinUserPopup.Show(_userName, _leapBrushClient);
        }

        private void OnLeaveSessionButtonSelected(Interactor _)
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                AnchorsApi.ClearImportedAnchors();
                _leaveSessionButton.gameObject.SetActive(false);
            });
        }

        private void OnClearAllContentButtonSelected(Interactor interactor)
        {
            OnClearAllContentSelected?.Invoke();
        }

        private void UpdateSpaceOriginAxisVisibility()
        {
            _spaceOriginAxis.SetActive(
                _showOriginsToggle.IsOn
                && _localizationManager.LocalizationInfo.LocalizationStatus
                == MLAnchors.LocalizationStatus.Localized);
        }
    }
}