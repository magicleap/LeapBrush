using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap.LeapBrush
{
    public class MapLocalizationManager : MonoBehaviour
    {
        private const float LocalizationStatusUpdateDelaySeconds = .05f;

        public delegate void OnLocalizationInfoChangedDelegate(
            AnchorsApi.LocalizationInfo info);

        public event OnLocalizationInfoChangedDelegate OnLocalizationInfoChanged;

        private AnchorsApi.LocalizationInfo _localizationInfo
            = new AnchorsApi.LocalizationInfo();

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

        public AnchorsApi.LocalizationInfo LocalizationInfo
        {
            get { return _localizationInfo; }
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