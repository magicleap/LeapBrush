// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Project Settings for Magic Leap MRTK3 specific settings
    /// </summary>
    class MagicLeapMRTK3SettingsProvider : SettingsProvider
    {
        private const string projectSettingsPath = "Project/MRTK3/Magic Leap Settings";

        MagicLeapMRTK3SettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) { }

        [SettingsProvider]
        static SettingsProvider Create()
        {
            var provider = new MagicLeapMRTK3SettingsProvider(projectSettingsPath, SettingsScope.Project,
                new HashSet<string>(new[] { "MRTK", "MRTK3", "Rig", "Hand", "Controller" }));

            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var magicLeapMRTK3Settings = MagicLeapMRTK3Settings.Instance;
            magicLeapMRTK3Settings.Initialize();
        }

        public override void OnGUI(string searchContext)
        {
            if (GUILayout.Button("Load Defaults", GUILayout.Width(100)))
            {
                MagicLeapMRTK3Settings.Instance.LoadDefaults();
            }

            MagicLeapMRTK3Settings.Instance.OnGUI();
        }
    }

}