using System;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The settings panel UI.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        public event Action OnClearAllContentSelected;
        public event Action OnSettingsPanelHidden;

        public PopupTracker PopupTracker => _popupTracker;

        [Header("External Dependencies")]

        [SerializeField]
        private JoinUserPopup _joinUserPopup;

        [SerializeField]
        private AboutPopup _aboutPopup;

        [SerializeField]
        private LeapBrushPreferences _preferences;

        [Header("Internal Dependencies")]

        [SerializeField]
        private StatefulInteractable _settingsBackButton;

        [SerializeField]
        private PressableButton _displayTabButton;

        [SerializeField]
        private PressableButton _inputTabButton;

        [SerializeField]
        private PressableButton _advancedTabButton;

        [SerializeField]
        private GameObject _advancedSettingsSection;

        [SerializeField]
        private PressableButton _showSpatialAnchorsToggle;

        [SerializeField]
        private PressableButton _showOriginsToggle;

        [SerializeField]
        private PressableButton _showOtherHeadsetsToggle;

        [SerializeField]
        private PressableButton _showOtherHandsAndControlsToggle;

        [SerializeField]
        private PressableButton _showFloorGridToggle;

        [SerializeField]
        private PressableButton _showSpaceMeshToggle;

        [SerializeField]
        private PressableButton _handLasersToggle;

        [SerializeField]
        private PressableButton _handToolsToggle;

        [SerializeField]
        private PressableButton _gazePinchToggle;

        [SerializeField]
        private PressableButton _phoneSpectatorToggle;

        [SerializeField]
        private StatefulInteractable _joinSessionButton;

        [SerializeField]
        private StatefulInteractable _leaveSessionButton;

        [SerializeField]
        private StatefulInteractable _clearAllContentButton;

        [SerializeField]
        private StatefulInteractable _aboutButton;

        private string _userName;
        private LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private DelayedButtonHandler _delayedButtonHandler;
        private PopupTracker _popupTracker;

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
            _popupTracker = gameObject.AddComponent<PopupTracker>();
        }

        private void Start()
        {
            _showSpatialAnchorsToggle.IsToggled.OnEntered.AddListener(OnToggleShowSpatialAnchors);
            _showSpatialAnchorsToggle.IsToggled.OnExited.AddListener(OnToggleShowSpatialAnchors);
            _showOriginsToggle.IsToggled.OnEntered.AddListener(OnToggleShowOrigins);
            _showOriginsToggle.IsToggled.OnExited.AddListener(OnToggleShowOrigins);
            _showOtherHeadsetsToggle.IsToggled.OnEntered.AddListener(OnToggleShowOtherHeadsets);
            _showOtherHeadsetsToggle.IsToggled.OnExited.AddListener(OnToggleShowOtherHeadsets);
            _showOtherHandsAndControlsToggle.IsToggled.OnEntered.AddListener(
                OnToggleShowOtherHandsAndControls);
            _showOtherHandsAndControlsToggle.IsToggled.OnExited.AddListener(
                OnToggleShowOtherHandsAndControls);
            _showFloorGridToggle.IsToggled.OnEntered.AddListener(OnToggleShowFloorGrid);
            _showFloorGridToggle.IsToggled.OnExited.AddListener(OnToggleShowFloorGrid);
            _showSpaceMeshToggle.IsToggled.OnEntered.AddListener(OnToggleShowSpaceMesh);
            _showSpaceMeshToggle.IsToggled.OnExited.AddListener(OnToggleShowSpaceMesh);

            _handLasersToggle.IsToggled.OnEntered.AddListener(OnToggleHandLasers);
            _handLasersToggle.IsToggled.OnExited.AddListener(OnToggleHandLasers);
            _handToolsToggle.IsToggled.OnEntered.AddListener(OnToggleHandTools);
            _handToolsToggle.IsToggled.OnExited.AddListener(OnToggleHandTools);
            _gazePinchToggle.IsToggled.OnEntered.AddListener(OnToggleGazePinch);
            _gazePinchToggle.IsToggled.OnExited.AddListener(OnToggleGazePinch);

            _phoneSpectatorToggle.IsToggled.OnEntered.AddListener(OnTogglePhoneSpectator);
            _phoneSpectatorToggle.IsToggled.OnExited.AddListener(OnTogglePhoneSpectator);

            _settingsBackButton.OnClicked.AddListener(OnSettingsBackButtonClicked);
            _clearAllContentButton.OnClicked.AddListener(OnClearAllContentButtonClicked);
            _aboutButton.OnClicked.AddListener(OnAboutButtonClicked);

            _joinSessionButton.OnClicked.AddListener(OnJoinSessionButtonClicked);
            _leaveSessionButton.OnClicked.AddListener(OnLeaveSessionButtonClicked);
            _leaveSessionButton.gameObject.SetActive(false);

#if UNITY_ANDROID
            _showFloorGridToggle.gameObject.SetActive(false);
            _showSpaceMeshToggle.gameObject.SetActive(false);
#endif

#if !UNITY_ANDROID || UNITY_EDITOR
            _advancedTabButton.gameObject.SetActive(false);
            _advancedSettingsSection.gameObject.SetActive(false);
            _phoneSpectatorToggle.gameObject.SetActive(false);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            _clearAllContentButton.gameObject.SetActive(false);
#endif
        }

        private void OnEnable()
        {
            _popupTracker.TrackPopup(_joinUserPopup);
            _popupTracker.TrackPopup(_aboutPopup);
            _popupTracker.OnPopupsShownChanged += OnPopupsShownChanged;
            OnPopupsShownChanged(_popupTracker.PopupsShown);

            _preferences.ShowSpatialAnchors.OnChanged += OnShowSpatialAnchorsPreferenceChanged;
            _preferences.ShowOrigins.OnChanged += OnShowOriginsPreferenceChanged;
            _preferences.ShowOtherHeadsets.OnChanged += OnShowOtherHeadsetsPreferenceChanged;
            _preferences.ShowOtherHandsAndControls.OnChanged
                += OnShowOtherHandsAndControlsPreferenceChanged;
            _preferences.ShowFloorGrid.OnChanged += OnShowFloorGridPreferenceChanged;
            _preferences.ShowSpaceMesh.OnChanged += OnShowSpaceMeshPreferenceChanged;
            _preferences.HandLasersEnabled.OnChanged += OnHandLasersPreferenceChanged;
            _preferences.HandToolsEnabled.OnChanged += OnHandToolsPreferenceChanged;
            _preferences.GazePinchEnabled.OnChanged += OnGazePinchPreferenceChanged;
            _preferences.PhoneSpecatorEnabled.OnChanged += OnPhoneSpectatorPreferenceChanged;

            OnShowSpatialAnchorsPreferenceChanged();
            OnShowOriginsPreferenceChanged();
            OnShowOtherHeadsetsPreferenceChanged();
            OnShowOtherHandsAndControlsPreferenceChanged();
            OnShowFloorGridPreferenceChanged();
            OnShowSpaceMeshPreferenceChanged();
            OnHandLasersPreferenceChanged();
            OnHandToolsPreferenceChanged();
            OnGazePinchPreferenceChanged();
            OnPhoneSpectatorPreferenceChanged();
        }

        private void OnDisable()
        {
            _popupTracker.OnPopupsShownChanged -= OnPopupsShownChanged;

            _preferences.ShowSpatialAnchors.OnChanged -= OnShowSpatialAnchorsPreferenceChanged;
            _preferences.ShowOrigins.OnChanged -= OnShowOriginsPreferenceChanged;
            _preferences.ShowOtherHeadsets.OnChanged -= OnShowOtherHeadsetsPreferenceChanged;
            _preferences.ShowOtherHandsAndControls.OnChanged
                -= OnShowOtherHandsAndControlsPreferenceChanged;
            _preferences.ShowFloorGrid.OnChanged -= OnShowFloorGridPreferenceChanged;
            _preferences.ShowSpaceMesh.OnChanged -= OnShowSpaceMeshPreferenceChanged;
            _preferences.HandLasersEnabled.OnChanged -= OnHandLasersPreferenceChanged;
            _preferences.HandToolsEnabled.OnChanged -= OnHandToolsPreferenceChanged;
            _preferences.GazePinchEnabled.OnChanged -= OnGazePinchPreferenceChanged;
            _preferences.PhoneSpecatorEnabled.OnChanged -= OnPhoneSpectatorPreferenceChanged;
        }

        private void OnPopupsShownChanged(bool popupsShown)
        {
            _settingsBackButton.enabled = !popupsShown;
            _displayTabButton.enabled = !popupsShown;
            _inputTabButton.enabled = !popupsShown;
            _advancedTabButton.enabled = !popupsShown;
            _showSpatialAnchorsToggle.enabled = !popupsShown;
            _showOriginsToggle.enabled = !popupsShown;
            _showOtherHeadsetsToggle.enabled = !popupsShown;
            _showOtherHandsAndControlsToggle.enabled = !popupsShown;
            _showFloorGridToggle.enabled = !popupsShown;
            _showSpaceMeshToggle.enabled = !popupsShown;
            _handLasersToggle.enabled = !popupsShown;
            _handToolsToggle.enabled = !popupsShown;
            _gazePinchToggle.enabled = !popupsShown;
            _joinSessionButton.enabled = !popupsShown;
            _leaveSessionButton.enabled = !popupsShown;
            _clearAllContentButton.enabled = !popupsShown;
            _aboutButton.enabled = !popupsShown;
        }

        private void OnShowSpatialAnchorsPreferenceChanged()
        {
            _showSpatialAnchorsToggle.ForceSetToggled(
                _preferences.ShowSpatialAnchors.Value, false);
        }

        private void OnShowOriginsPreferenceChanged()
        {
            _showOriginsToggle.ForceSetToggled(_preferences.ShowOrigins.Value, false);
        }

        private void OnShowOtherHeadsetsPreferenceChanged()
        {
            _showOtherHeadsetsToggle.ForceSetToggled(_preferences.ShowOtherHeadsets.Value, false);
        }

        private void OnShowOtherHandsAndControlsPreferenceChanged()
        {
            _showOtherHandsAndControlsToggle.ForceSetToggled(
                _preferences.ShowOtherHandsAndControls.Value, false);
        }

        private void OnShowFloorGridPreferenceChanged()
        {
            _showFloorGridToggle.ForceSetToggled(_preferences.ShowFloorGrid.Value, false);
        }

        private void OnShowSpaceMeshPreferenceChanged()
        {
            _showSpaceMeshToggle.ForceSetToggled(_preferences.ShowSpaceMesh.Value, false);
        }

        private void OnHandLasersPreferenceChanged()
        {
            _handLasersToggle.ForceSetToggled(_preferences.HandLasersEnabled.Value, false);
        }

        private void OnHandToolsPreferenceChanged()
        {
            _handToolsToggle.ForceSetToggled(_preferences.HandToolsEnabled.Value, false);
        }

        private void OnGazePinchPreferenceChanged()
        {
            _gazePinchToggle.ForceSetToggled(_preferences.GazePinchEnabled.Value, false);
        }

        private void OnPhoneSpectatorPreferenceChanged()
        {
            _phoneSpectatorToggle.ForceSetToggled(_preferences.PhoneSpecatorEnabled.Value, false);
        }

        public void OnToggleShowSpatialAnchors(float _)
        {
            _preferences.ShowSpatialAnchors.Value = _showSpatialAnchorsToggle.IsToggled;
        }

        private void OnToggleShowOrigins(float _)
        {
            _preferences.ShowOrigins.Value = _showOriginsToggle.IsToggled;
        }

        private void OnToggleShowOtherHeadsets(float _)
        {
            _preferences.ShowOtherHeadsets.Value = _showOtherHeadsetsToggle.IsToggled;
        }

        private void OnToggleShowOtherHandsAndControls(float _)
        {
            _preferences.ShowOtherHandsAndControls.Value
                = _showOtherHandsAndControlsToggle.IsToggled;
        }

        private void OnToggleShowFloorGrid(float _)
        {
            _preferences.ShowFloorGrid.Value = _showFloorGridToggle.IsToggled;
        }

        private void OnToggleShowSpaceMesh(float _)
        {
            _preferences.ShowSpaceMesh.Value = _showSpaceMeshToggle.IsToggled;
        }

        private void OnToggleHandLasers(float _)
        {
            _preferences.HandLasersEnabled.Value = _handLasersToggle.IsToggled;
        }

        private void OnToggleHandTools(float _)
        {
            _preferences.HandToolsEnabled.Value = _handToolsToggle.IsToggled;
        }

        private void OnToggleGazePinch(float _)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_gazePinchToggle.IsToggled
                && !Android.Permissions.CheckPermission(
                    UnityEngine.XR.MagicLeap.MLPermission.EyeTracking))
            {
                _gazePinchToggle.ForceSetToggled(false, false);

                Android.Permissions.RequestPermission(
                    UnityEngine.XR.MagicLeap.MLPermission.EyeTracking, _ =>
                {
                    _gazePinchToggle.ForceSetToggled(true, false);
                    _preferences.GazePinchEnabled.Value = true;
                });
                return;
            }
#endif

            _preferences.GazePinchEnabled.Value = _gazePinchToggle.IsToggled;
        }

        private void OnTogglePhoneSpectator(float _)
        {
            _preferences.PhoneSpecatorEnabled.Value = _phoneSpectatorToggle.IsToggled;
        }

        private void OnSettingsBackButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                Hide();
            });
        }

        private void OnJoinSessionButtonClicked()
        {
            _joinUserPopup.Show(_userName, _leapBrushClient);
        }

        private void OnLeaveSessionButtonClicked()
        {
            _delayedButtonHandler.InvokeAfterDelayExclusive(() =>
            {
                AnchorsApi.ClearImportedAnchors();
                _leaveSessionButton.gameObject.SetActive(false);
            });
        }

        private void OnClearAllContentButtonClicked()
        {
            OnClearAllContentSelected?.Invoke();
        }

        private void OnAboutButtonClicked()
        {
            _aboutPopup.Show();
        }
    }
}