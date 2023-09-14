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

namespace MagicLeap.MRTK.Samples.SpatialAwareness
{
    public class MeshingOptionsMenu : MonoBehaviour
    {
        public MeshingController MeshingController;

        void Start()
        {
            if (MeshingController == null)
            {
                enabled = false;
                Debug.LogWarning("MeshingController is null, disabling script");
                return;
            }
        }

        public void OnToggleChanged(int selectedToggle)
        {
            if (MeshingController == null)
            {
                return;
            }
            if (selectedToggle == 0)
            {
                MeshingController.SetRenderer(MeshingController.RenderMode.Wireframe);
            }
            else if (selectedToggle == 1)
            {
                MeshingController.SetRenderer(MeshingController.RenderMode.Colored);
            }
            else
            {
                MeshingController.SetRenderer(MeshingController.RenderMode.Occlusion);
            }
        }
    }
}