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
        public AnchorsApi.LocalizationInfo LocalizationInfo
        {
            get
            {
                lock (_localizationInfoLock)
                {
                    return _localizationInfo;
                }
            }
        }

        public event Action<AnchorsApi.LocalizationInfo> OnLocalizationInfoChanged;

        private const float LocalizationStatusUpdateDelaySeconds = .05f;

        private AnchorsApi.LocalizationInfo _pendingLocalizationInfo;
        private AnchorsApi.LocalizationInfo _localizationInfo;
        private object _localizationInfoLock = new();
        private IEnumerator _updateLocalizationStatusCoroutine;

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
            Action updateLocalizationAction = UpdateLocalizationStatusOnWorkerThread;
            YieldInstruction localizationUpdateDelay = new WaitForSeconds(
                LocalizationStatusUpdateDelaySeconds);

            while (true)
            {
                ThreadDispatcher.ScheduleWork(updateLocalizationAction);

                // Wait before querying again for localization status
                yield return localizationUpdateDelay;
            }
        }

        private void UpdateLocalizationStatusOnWorkerThread()
        {
            MLResult result = AnchorsApi.GetLocalizationInfo(ref _pendingLocalizationInfo);
            if (!result.IsOk)
            {
                return;
            }

            lock (_localizationInfoLock)
            {
                if (!_localizationInfo.Equals(_pendingLocalizationInfo))
                {
                    _localizationInfo = _pendingLocalizationInfo.Clone();
                    ThreadDispatcher.ScheduleMain(DispatchLocalizationInfoChangeOnMainThread);
                }
            }
        }

        private void DispatchLocalizationInfoChangeOnMainThread()
        {
            lock (_localizationInfoLock)
            {
                OnLocalizationInfoChanged?.Invoke(_localizationInfo);
            }
        }
    }
}