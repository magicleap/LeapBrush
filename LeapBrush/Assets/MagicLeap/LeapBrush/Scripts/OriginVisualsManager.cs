using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    public class OriginVisualsManager : MonoBehaviour
    {
        [Header("External Dependencies")]

        [SerializeField]
        private SpaceLocalizationManager _localizationManager;

        [SerializeField]
        private LeapBrushPreferences _preferences;

        [Header("Internal Dependencies")]

        [SerializeField]
        private GameObject _worldOriginAxis;

        [SerializeField]
        private GameObject _spaceOriginAxis;

        private void OnEnable()
        {
            _localizationManager.OnLocalizationInfoChanged += OnLocalizationInfoChanged;

            _preferences.ShowOrigins.OnChanged += OnShowOriginsPreferenceChanged;
            OnShowOriginsPreferenceChanged();
        }

        private void OnDisable()
        {
            _localizationManager.OnLocalizationInfoChanged -= OnLocalizationInfoChanged;

            _preferences.ShowOrigins.OnChanged -= OnShowOriginsPreferenceChanged;
        }

        private void OnLocalizationInfoChanged(AnchorsApi.LocalizationInfo localizationInfo)
        {
            UpdateSpaceOriginAxisVisibility();
        }

        private void OnShowOriginsPreferenceChanged()
        {
            _worldOriginAxis.SetActive(_preferences.ShowOrigins.Value);
            UpdateSpaceOriginAxisVisibility();
        }

        private void UpdateSpaceOriginAxisVisibility()
        {
            _spaceOriginAxis.SetActive(
                _preferences.ShowOrigins.Value
                && _localizationManager.LocalizationInfo.LocalizationStatus
                == MLAnchors.LocalizationStatus.Localized);
        }
    }
}
