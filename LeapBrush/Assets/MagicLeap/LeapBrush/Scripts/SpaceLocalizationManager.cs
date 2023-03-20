using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manager for checking the latest state of map localization. Periodically checks the
    /// state and fires events when it changes.
    /// </summary>
    public class SpaceLocalizationManager : MonoBehaviour
    {
        public AnchorsApi.LocalizationInfo LocalizationInfo => _localizationInfo;

        public event Action<AnchorsApi.LocalizationInfo> OnLocalizationInfoChanged;

        private const float LocalizationStatusUpdateDelaySeconds = .05f;

        private AnchorsApi.LocalizationInfo _localizationInfo;
        private IEnumerator _updateLocalizationStatusCoroutine;

        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            // Side effect: Ensure MLDevice is initialized early
            Debug.Log("MapLocalizationManager Awake: MLDevice Platform Level: "
                      + MLDevice.PlatformLevel);
#endif
        }

        void Start()
        {
            _updateLocalizationStatusCoroutine = UpdateLocalizationStatusPeriodically();
            StartCoroutine(_updateLocalizationStatusCoroutine);
        }

        void OnDestroy()
        {
            StopCoroutine(_updateLocalizationStatusCoroutine);
        }

        private IEnumerator UpdateLocalizationStatusPeriodically()
        {
            while (true)
            {
                ThreadDispatcher.ScheduleWork(UpdateLocalizationStatusOnWorkerThread);

                // Wait before querying again for localization status
                yield return new WaitForSeconds(LocalizationStatusUpdateDelaySeconds);
            }
        }

        private void UpdateLocalizationStatusOnWorkerThread()
        {
            AnchorsApi.LocalizationInfo localizationInfo;
            MLResult result = AnchorsApi.GetLocalizationInfo(out localizationInfo);
            if (result.IsOk)
            {
                ThreadDispatcher.ScheduleMain(() => UpdateLocalizationStatusOnMainThread(
                    localizationInfo));
            }
        }

        private void UpdateLocalizationStatusOnMainThread(AnchorsApi.LocalizationInfo info)
        {
            if (!_localizationInfo.Equals(info))
            {
                _localizationInfo = info;
                OnLocalizationInfoChanged?.Invoke(info);
            }
        }
    }
}