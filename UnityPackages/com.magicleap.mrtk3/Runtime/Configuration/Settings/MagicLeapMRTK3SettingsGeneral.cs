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
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Provides general settings for Magic Leap within MRTK3.
    /// </summary>
    public sealed class MagicLeapMRTK3SettingsGeneral : MagicLeapMRTK3SettingsObject
    {
        private const uint PermissionsFileVersion = 1;

        [SerializeField]
        [HideInInspector]
        private uint version = PermissionsFileVersion;
        public uint Version => version;

        [SerializeField]
        [Tooltip("Whether to observe the OS Setting for enabling hand navigation for hand interactions in MRTK3.  " +
                 "If this flag is enabled, hand interactions will be available within MRTK3 based on the OS Setting " +
                 "for hand navigation, at Settings > Magic Leap Inputs > Gestures > Hand Navigation.")]
        private bool observeOSHandNavigationSetting = false;

        /// <summary>
        /// Whether to observe the OS Setting for enabling of hand navigation for hand interactions in MRTK3.
        /// 
        /// If this flag is set to true, and the OS Setting for hand navigation is disabled, then hands will
        /// be disabled within MRTK3 on the ML2.
        /// </summary>
        public bool ObserveOSHandNavigationSetting => observeOSHandNavigationSetting;

        /// <summary>
        /// Whether MRTK3 hand interactions are enabled based on the OS hand navigation setting and whether that
        /// setting should be observed or not.  Defaults to not observing the OS setting, so hands are enabled
        /// by default in MRTK3 on the ML2 platform.
        /// </summary>
        public bool MRTK3HandInteractionsEnabled => !observeOSHandNavigationSetting || osHandNavigationEnabled;


        private const string handNavigationSettingsKey = "enable_pinch_gesture_inputs";
        private bool osHandNavigationEnabled = true;

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override void OnGUI()
        {
            serializedObject.Update();

            // Begin General Settings UI window
            GUILayout.BeginVertical("General Settings", new GUIStyle("Window"));
            {
                EditorGUILayout.Space(8);

                // Observe OS Setting for Enabling Hand Navigation
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                const string osHandNavigationLabel = "Observe OS Setting for Enabling Hand Navigation";
                EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent(osHandNavigationLabel)).x + 10.0f;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("observeOSHandNavigationSetting"),
                                                                            new GUIContent(osHandNavigationLabel));
                EditorGUIUtility.labelWidth = originalLabelWidth;

                EditorGUILayout.Space(8);
            }
            GUILayout.EndVertical();
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

#endif // UNITY_EDITOR

        /// <inheritdoc/>
        public override void ProcessOnBeforeSceneLoad()
        {
            // Nothing to do here yet for general settings.
        }

        /// <inheritdoc/>
        public override void ProcessOnAfterSceneLoad()
        {
            // OS Setting for Hand Navigation
            // Only on device
#if !UNITY_EDITOR
            // Only need to monitor the OS hand navigation setting if the ObserveOSHandNavigationSetting is set
            if (ObserveOSHandNavigationSetting)
            {
                osHandNavigationEnabled = JavaUtils.GetSystemSetting<int>("getInt", handNavigationSettingsKey) > 0;

                // Start timer checking hand navigation settings option, every 2 seconds
                SynchronizationContext mainSyncContext = SynchronizationContext.Current;
                System.Timers.Timer timer = new System.Timers.Timer(2000);
                timer.Start();
                timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                {
                    mainSyncContext.Post(_ =>
                    {
                        osHandNavigationEnabled = JavaUtils.GetSystemSetting<int>("getInt", handNavigationSettingsKey) > 0;

                    }, null);
                };
            }
#endif
        }

    }
}