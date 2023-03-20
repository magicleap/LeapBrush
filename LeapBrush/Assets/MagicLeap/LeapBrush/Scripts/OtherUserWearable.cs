using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Component managing the display of other user's Headsets present in the area.
    /// </summary>
    public class OtherUserWearable : MonoBehaviour
    {
        [SerializeField]
        private GameObject _nameTag;

        [SerializeField]
        private TMP_Text _userNameText;

        [SerializeField]
        private GameObject _batteryIndicator;

        [SerializeField]
        private Image _batteryChargeLevelImage;

        [SerializeField]
        private GameObject _batteryChargingGameObject;

        public DateTimeOffset LastUpdateTime = DateTimeOffset.Now;

        public event Action OnDestroyed;

        private float _nameTagVerticalOffset;
        private float _batteryChargeLevelRectFullXScale;

        private const float BatteryChargeLevelRectFullXScale = 0.7f;

        private const int LowToMidBatteryThreshold = 5;
        private const int MidToFullBatteryThreshold = 20;

        private readonly Color LowBatteryColor = new(0.80784315f, 0.16862738f, 0.26274505f);
        private readonly Color MidBatteryColor = new(0.99215686f, 0.7921569f, 0.18039216f);
        private readonly Color HighBatteryColor = new(0.3372549f, 0.7058824f, 0.36078432f);

        private BatteryStatusProto _headsetBattery;

        private void Start()
        {
            _nameTagVerticalOffset = (_nameTag.transform.position - transform.position).magnitude;

            if (_headsetBattery != null)
            {
                UpdateHeadsetBatteryDisplay();
            }
            else
            {
                _batteryIndicator.SetActive(false);
            }
        }

        private void Update()
        {
            Transform nameTagTransform = _nameTag.transform;
            Vector3 lookDir = (
                nameTagTransform.position - Camera.main.transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                nameTagTransform.SetPositionAndRotation(
                    transform.position + new Vector3(0, _nameTagVerticalOffset, 0),
                    Quaternion.LookRotation(lookDir, Vector3.up));
            }
        }

        public void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        public void SetUserDisplayName(string userDisplayName)
        {
            _userNameText.text = userDisplayName;
        }

        public void SetHeadsetBattery(BatteryStatusProto headsetBattery)
        {
            _headsetBattery = headsetBattery;

            if (isActiveAndEnabled)
            {
                UpdateHeadsetBatteryDisplay();
            }
        }

        private void UpdateHeadsetBatteryDisplay()
        {
            _batteryIndicator.SetActive(true);
            _batteryChargingGameObject.SetActive(
                _headsetBattery.State == BatteryStatusProto.Types.BatteryState.Charging ||
                _headsetBattery.State == BatteryStatusProto.Types.BatteryState.Full);

            if (_headsetBattery.Level <= LowToMidBatteryThreshold)
            {
                _batteryChargeLevelImage.color = LowBatteryColor;
            }
            else if (_headsetBattery.Level <= MidToFullBatteryThreshold)
            {
                _batteryChargeLevelImage.color = MidBatteryColor;
            }
            else
            {
                _batteryChargeLevelImage.color = HighBatteryColor;
            }

            Vector3 newLevelImageScale = _batteryChargeLevelImage.transform.localScale;
            newLevelImageScale.x = _headsetBattery.Level / 100.0f
                                   * BatteryChargeLevelRectFullXScale;
            _batteryChargeLevelImage.transform.localScale = newLevelImageScale;
        }
    }
}