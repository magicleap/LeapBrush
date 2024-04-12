using System;
using System.Collections;
using MagicLeap.OpenXR.Features.LocalizationMaps;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manager for checking the latest state of map localization. Periodically checks the
    /// state and fires events when it changes.
    /// </summary>
    public class LocalizationMapManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Localization state to return when on a non-ML2 device or in the unity editor.")]
        private LocalizationMapInfo _defaultLocalizationInfo = new(
            "Default", "DEFAULT_SPACE_ID",  LocalizationMapType.OnDevice,
            LocalizationMapState.Localized, Pose.identity);

        public LocalizationMapInfo LocalizationInfo => _localizationInfo;

        public event Action<LocalizationMapInfo> OnLocalizationInfoChanged;

        private const float LocalizationStatusUpdateDelaySeconds = .05f;

        private static readonly ProfilerMarker OnLocalizationChangedEventPerfMarker =
            new("MapLocalizationManager.OnLocalizationChangedEvent");

        private bool _isReady;
        private LocalizationMapInfo _localizationInfo;
        private IEnumerator _updateLocalizationStatusCoroutine;
        private MagicLeapLocalizationMapFeature _localizationMapFeature;

#if UNITY_ANDROID && !UNITY_EDITOR
        private const bool IsUnityAndroidAndNotEditor = true;
#else
        private const bool IsUnityAndroidAndNotEditor = false;
#endif

        [Serializable]
        public struct LocalizationMapInfo
        {
            /// <summary>
            /// The localization map name
            /// </summary>
            public string MapName;

            /// <summary>
            /// The localization map UUID
            /// </summary>
            public string MapUUID;

            /// <summary>
            /// The localization map type
            /// </summary>
            public LocalizationMapType MapType;

            /// <summary>
            /// The localization map state
            /// </summary>
            public LocalizationMapState MapState;

            /// <summary>
            /// The map origin for the purposes of 3D mesh alignment, etc.
            /// </summary>
            public Pose OriginPose;

            public LocalizationMapInfo(string mapName, string mapUUID, LocalizationMapType mapType,
                LocalizationMapState mapState, Pose originPose)
            {
                MapName = mapName;
                MapUUID = mapUUID;
                MapType = mapType;
                MapState = mapState;
                OriginPose = originPose;
            }

            public LocalizationMapInfo Clone()
            {
                return new LocalizationMapInfo
                {
                    MapName = MapName,
                    MapUUID = MapUUID,
                    MapType = MapType,
                    MapState = MapState,
                    OriginPose = OriginPose
                };
            }

            public override string ToString() => $"MapState: {MapState}, MapType: {MapType},\nMapName: {MapName}, MapUUID: {MapUUID}";
        }

        private IEnumerator Start()
        {
            if (IsUnityAndroidAndNotEditor)
            {
                yield return new WaitUntil(AreOpenXRSubsystemsLoaded);
                yield return new WaitUntil(AreOpenXRFeaturesEnabled);
                yield return new WaitUntil(AreOpenXRLocalizationEventsEnabled);

                MagicLeapLocalizationMapFeature.OnLocalizationChangedEvent +=
                    OnLocalizationChangedEvent;
            }

            _updateLocalizationStatusCoroutine = UpdateLocalizationStatusPeriodically();
            StartCoroutine(_updateLocalizationStatusCoroutine);

            _isReady = true;
        }

        void OnDestroy()
        {
            if (_isReady)
            {
                MagicLeapLocalizationMapFeature.OnLocalizationChangedEvent -=
                    OnLocalizationChangedEvent;

                StopCoroutine(_updateLocalizationStatusCoroutine);
            }
        }

        private bool AreOpenXRSubsystemsLoaded()
        {
            if (XRGeneralSettings.Instance == null ||
                XRGeneralSettings.Instance.Manager == null ||
                XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                return false;
            }

            return true;
        }

        private bool AreOpenXRFeaturesEnabled()
        {
            _localizationMapFeature = OpenXRSettings.Instance.GetFeature<MagicLeapLocalizationMapFeature>();

            if (_localizationMapFeature == null || !_localizationMapFeature.enabled)
            {
                Debug.LogError("The OpenXR localization map features are not enabled.");
                return false;
            }

            return true;
        }

        private bool AreOpenXRLocalizationEventsEnabled()
        {
            XrResult result = _localizationMapFeature.EnableLocalizationEvents(true);
            if (result != XrResult.Success)
            {
                Debug.LogError($"MagicLeapLocalizationMapFeature.EnableLocalizationEvents failed, result = {result}");
                return false;
            }

            return true;
        }

        private IEnumerator UpdateLocalizationStatusPeriodically()
        {
            YieldInstruction localizationUpdateDelay = new WaitForSeconds(
                LocalizationStatusUpdateDelaySeconds);

            while (true)
            {
                if (!IsUnityAndroidAndNotEditor)
                {
                    MaybeUpdateLocalizationInfoAndDispatch(_defaultLocalizationInfo.Clone());
                }
                else
                {
                    if (_localizationMapFeature != null &&
                        _localizationMapFeature.GetLatestLocalizationMapData(
                            out LocalizationEventData data))
                    {
                        OnLocalizationChangedEvent(data);
                    }
                    else
                    {
                        Debug.LogError("Error querying localization.");
                    }
                }

                // Wait before querying again for localization status
                yield return localizationUpdateDelay;
            }
        }

        private void OnLocalizationChangedEvent(LocalizationEventData data)
        {
            using (OnLocalizationChangedEventPerfMarker.Auto())
            {
                var info = new LocalizationMapInfo(data.Map.Name, data.Map.MapUUID, data.Map.MapType,
                    data.State, _localizationMapFeature.GetMapOrigin());

                MaybeUpdateLocalizationInfoAndDispatch(info);
            }
        }

        private void MaybeUpdateLocalizationInfoAndDispatch(LocalizationMapInfo info)
        {
            if (!_localizationInfo.Equals(info))
            {
                _localizationInfo = info;
                OnLocalizationInfoChanged?.Invoke(info.Clone());
            }
        }
    }
}