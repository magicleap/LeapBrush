// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR;

namespace MagicLeap.MRTK.Samples.EyeTracking
{
    public class EyeDeviceStatus : MonoBehaviour
    {
        [SerializeField]
        private GameObject deviceStatusVisual;
        [SerializeField]
        private GameObject statusMessage;

        private const string deviceCreatedStatusMessage = "OK";
        private const string deviceFailedStatusMessage = "Not Valid, did you check EYE_TRACKING?";

        void Start()
        {
            checkEyeDeviceStatus();
            StartCoroutine(pollStatus());
        }

        private IEnumerator pollStatus()
        {
            while (true)
            {
                checkEyeDeviceStatus();
                yield return new WaitForSeconds(1f);
            }
        }

        private void checkEyeDeviceStatus()
        {
            var eyesDevice = InputSubsystem.Utils.FindMagicLeapDevice(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.TrackedDevice);
            bool deviceValid = true;
            if (eyesDevice == null || !eyesDevice.isValid)
            {
                deviceValid = false;
            }
            setVisualColor(deviceValid);
            setStatusMessage(deviceValid);
        }

        private void setVisualColor(bool deviceValid)
        {
            if (deviceStatusVisual != null)
            {
                MeshRenderer meshRenderer = deviceStatusVisual.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.material.color = deviceValid ? Color.green : Color.red;
                }
            }
        }

        private void setStatusMessage(bool deviceValid)
        {
            if (statusMessage != null)
            {
                TextMeshProUGUI statusText = statusMessage.GetComponent<TextMeshProUGUI>();
                statusText.text = deviceValid ? deviceCreatedStatusMessage : deviceFailedStatusMessage;
            }
        }
    }
}
