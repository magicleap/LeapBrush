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
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Coordinates the runtime handling of the MagicLeapMRTK3Settings
    /// </summary>
    public class MagicLeapMRTK3SettingsRuntime
    {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
#if UNITY_EDITOR
            if (!ShouldProcessRuntimeSettingsInEditor())
            {
                return;
            }
#endif

            foreach (var settingsObject in MagicLeapMRTK3Settings.Instance.SettingsObjects)
            {
                settingsObject.ProcessOnBeforeSceneLoad();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
#if UNITY_EDITOR
            if (!ShouldProcessRuntimeSettingsInEditor())
            {
                return;
            }
#endif

            foreach (var settingsObject in MagicLeapMRTK3Settings.Instance.SettingsObjects)
            {
                settingsObject.ProcessOnAfterSceneLoad();
            }
        }

#if UNITY_EDITOR
        private static bool ShouldProcessRuntimeSettingsInEditor()
        {
            // In Editor, only provide runtime processing if the Magic Leap XRLoader (App Sim) is active
            if (XRGeneralSettings.Instance.AssignedSettings.activeLoader is not MagicLeapLoader)
            {
                return false;
            }

            return true;
        }
#endif

    }
}