using MagicLeap.OpenXR.Features.LocalizationMaps;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class OriginVisualsManager : MonoBehaviour
    {
        [Header("External Dependencies")]

        [SerializeField]
        private LocalizationMapManager _localizationManager;

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

        private void OnLocalizationInfoChanged(
            LocalizationMapManager.LocalizationMapInfo localizationInfo)
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
                && _localizationManager.LocalizationInfo.MapState
                == LocalizationMapState.Localized);
        }
    }
}
