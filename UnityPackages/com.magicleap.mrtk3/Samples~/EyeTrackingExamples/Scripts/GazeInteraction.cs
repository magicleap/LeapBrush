// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using UnityEngine;
using TMPro;

namespace MagicLeap.MRTK.Samples.EyeTracking
{
    public class GazeInteraction : MonoBehaviour
    {
        public void OnGazeEnter()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.red;
            }
            TextMeshPro text = GetComponentInChildren<TextMeshPro>();
            if (text != null)
            {
                text.text = "Gaze Enter";
            }
        }

        public void OnGazeExit()
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = Color.white;
            }
            TextMeshPro text = GetComponentInChildren<TextMeshPro>();
            if (text != null)
            {
                text.text = "Gaze Exit";
            }
        }
    }
}
