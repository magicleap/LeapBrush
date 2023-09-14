// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEngine.XR.MagicLeap;
using static UnityEngine.XR.MagicLeap.InputSubsystem.Extensions;
using MLHands = UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.MLHandTracking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Provides configuration for Magic Leap permissions to auto request and start where applicable.
    /// </summary>
    public sealed class MagicLeapMRTK3SettingsPermissionsConfig : MagicLeapMRTK3SettingsObject
    {
        private const uint PermissionsFileVersion = 1;

#if UNITY_EDITOR

        private const string PermssionsHelpBoxMessage =
            "The Runtime Permissions Configuration is an optional way to handle application permission on the Magic Leap platform. " +
            "Enabling the feature will auto request the permission and/or start any tracking associated with the permission, if applicable.";
        private const string DangerousPermissionsTooltip =
            "Dangerous permissions require user approval.  Enabling the auto request of a dangerous permission will invoke the request and " +
            "obtain the user's response.  If approved, any applicable tracking associated with the permission will be started, if simple. " +
            "Use the callback handler prefab, interface IPermissionCallbackHandler, to receive request status callbacks and to setup more " +
            "complex permissions, like voice input.";
        private const string NormalPermissionsTooltip =
            "Normal permissions do not require a request and can be started automatically when enabled.";
        private string manifestExceptionError = string.Empty;

#endif

        [SerializeField]
        [HideInInspector]
        private uint version = PermissionsFileVersion;
        public uint Version => version;

        [SerializeField]
        [Tooltip("Whether the selected dangerous permissions will be automatically requested at runtime.")]
        private bool autoRequestDangerousPermissionsEnabled = false;
        public bool AutoRequestDangerousPermissionsEnabled => autoRequestDangerousPermissionsEnabled;


        [Serializable]
        public struct PermissionConfig
        {
            public string permission;
            public string label;
            public bool enabled;
        }

        [SerializeField]
        [HideInInspector]
        private List<PermissionConfig> dangerousPermissions = new List<PermissionConfig>()
        {
            new PermissionConfig(){ permission = MLPermission.Camera,            label = "CAMERA",              enabled = false },
            new PermissionConfig(){ permission = MLPermission.RecordAudio,       label = "RECORD_AUDIO",        enabled = false },
            new PermissionConfig(){ permission = MLPermission.EyeTracking,       label = "EYE_TRACKING",        enabled = false },
            new PermissionConfig(){ permission = MLPermission.VoiceInput,        label = "VOICE_INPUT",         enabled = false },
            new PermissionConfig(){ permission = MLPermission.PupilSize,         label = "PUPIL_SIZE",          enabled = false },
            new PermissionConfig(){ permission = MLPermission.SpatialMapping,    label = "SPATIAL_MAPPING",     enabled = false },
            new PermissionConfig(){ permission = MLPermission.DepthCamera,       label = "DEPTH_CAMERA",        enabled = false },
            new PermissionConfig(){ permission = MLPermission.EyeCamera,         label = "EYE_CAMERA",          enabled = false },
            new PermissionConfig(){ permission = MLPermission.SpaceImportExport, label = "SPACE_IMPORT_EXPORT", enabled = false },
        };
        public IList<PermissionConfig> DangerousPermissions => dangerousPermissions.AsReadOnly();

        [SerializeField]
        [HideInInspector]
        private List<PermissionConfig> normalPermissions = new List<PermissionConfig>()
        {
            new PermissionConfig(){ permission = MLPermission.HandTracking,   label = "HAND_TRACKING",   enabled = true },
            //new PermissionConfig(){ permission = MLPermission.MarkerTracking, label = "MARKER_TRACKING", enabled = false },
            //new PermissionConfig(){ permission = MLPermission.SpatialAnchors, label = "SPATIAL_ANCHOR",  enabled = false },
            // No simple startTracking() call for these.

        };
        public IList<PermissionConfig> NormalPermissions => normalPermissions.AsReadOnly();

        [SerializeField]
        [Tooltip("Optional prefab that contains a Component that implements IPermissionCallbackHandler, to be instantiated at runtime." +
                 "  This provides API to handle permission callbacks.")]
        private GameObject permissionCallbackHandler = null;
        public GameObject PermissionCallbackHandler => permissionCallbackHandler;


        private MLPermissions.Callbacks permissionCallbacks = null;
        private IPermissionCallbackHandler permissionCallbackHandlerInstance = null;

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override void OnGUI()
        {
            serializedObject.Update();

            var manifestPermissions = getPermissionsInManifest();

            // Local function to draw the UI for a permissions group
            void DrawPermissions(string title, string tooltip, string propertyName)
            {
                GUILayout.Label(new GUIContent(title, tooltip), EditorStyles.boldLabel);
                DrawGUILineSeparator(1);
                // Draw Permissions Group
                {
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 175;
                    SerializedProperty dangerousSerialized = serializedObject.FindProperty(propertyName);
                    for (int i = 0; i < dangerousSerialized.arraySize; ++i)
                    {
                        SerializedProperty permission = dangerousSerialized.GetArrayElementAtIndex(i).FindPropertyRelative("permission");
                        SerializedProperty label = dangerousSerialized.GetArrayElementAtIndex(i).FindPropertyRelative("label");
                        SerializedProperty enabled = dangerousSerialized.GetArrayElementAtIndex(i).FindPropertyRelative("enabled");
                        EditorGUILayout.PropertyField(enabled, new GUIContent(label.stringValue, "Request " + permission.stringValue));
                        if (enabled.boolValue && manifestExceptionError == string.Empty &&
                            !manifestPermissions.Contains(permission.stringValue))
                        {
                            EditorGUI.indentLevel++;
                            string message = $"The {permission.stringValue} permission was not found in the AndroidManifest.xml file.\n" +
                                                $"Add <uses-permission android:name=\"{permission.stringValue}\" /> to the AndroidManifest.xml file,\n" +
                                                "or use Project Settings -> Magicleap -> Permissions, if present, and select the permission there.";
                            EditorGUILayout.HelpBox(message, MessageType.Warning);
                            EditorGUI.indentLevel--;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }

            // Begin Permission config UI window
            GUILayout.BeginVertical("Runtime Permissions Configuration", new GUIStyle("Window"));
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(PermssionsHelpBoxMessage, MessageType.Info);
                if (manifestExceptionError != string.Empty)
                {
                    // AndroidManifest.xml read, threw an exception. Display the error.
                    EditorGUILayout.HelpBox(manifestExceptionError, MessageType.Warning);
                    GUI.enabled = false;
                }
                EditorGUILayout.Space(16);

                float originalLabelWidth = EditorGUIUtility.labelWidth;
                const string dangerousEnableLabel = "Auto Request Dangerous Permissions";
                EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(new GUIContent(dangerousEnableLabel)).x + 10.0f;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRequestDangerousPermissionsEnabled"),
                                                                            new GUIContent(dangerousEnableLabel));
                EditorGUIUtility.labelWidth = originalLabelWidth;
                {
                    // Draw Dangerous Permissions group and associated callback handler field
                    DrawPermissions("Dangerous Permissions", DangerousPermissionsTooltip, "dangerousPermissions");
                    EditorGUIUtility.labelWidth = originalLabelWidth;
                    EditorGUILayout.Space(4);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("permissionCallbackHandler"),
                        new GUIContent("Callback Handler"));
                }
                EditorGUILayout.Space(16);
                {
                    // Draw Normal Permissions group
                    DrawPermissions("Normal Permissions", NormalPermissionsTooltip, "normalPermissions");
                    EditorGUIUtility.labelWidth = originalLabelWidth;
                }
                EditorGUILayout.Space(16);
            }
            GUILayout.EndVertical();
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        private HashSet<String> getPermissionsInManifest()
        {
            // Gets all permissions from the Android Manifest
            HashSet<string> permissionsInManifest = new HashSet<string>();
            const string manifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
            XmlDocument androidManifest = new XmlDocument();
            try
            {
                androidManifest.Load(manifestPath);
                manifestExceptionError = string.Empty;
            }
            catch (Exception e)
            {
                manifestExceptionError = "Error reading AndroidManifest.xml. Please go to \"Project Settings -> Player -> Publishing Settings\" " +
                                         "and select \"Custom Main Manifest\" to generate the permission manifest for your project: " + e.Message;
                return permissionsInManifest;
            }

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(androidManifest.NameTable);
            const string androidXmlNamespace = "http://schemas.android.com/apk/res/android";
            nsMgr.AddNamespace("android", androidXmlNamespace);

            XmlNodeList permissionNodes = androidManifest.SelectNodes("manifest/uses-permission", nsMgr);
            foreach (XmlNode permissionNode in permissionNodes)
            {
                permissionsInManifest.Add(permissionNode.Attributes["android:name"].Value);
            }
            return permissionsInManifest;
        }

#endif // UNITY_EDITOR

        /// <inheritdoc/>
        public override void ProcessOnBeforeSceneLoad()
        {
            // Nothing to do here, permissions need to be handled later after first scene load.
        }

        /// <inheritdoc/>
        public override void ProcessOnAfterSceneLoad()
        {
            // Handle dangerous permissions
            if (AutoRequestDangerousPermissionsEnabled)
            {
                // Instantiate the callback prefab
                if (permissionCallbackHandler != null && permissionCallbackHandlerInstance == null)
                {
                    GameObject permissionPrefab = GameObject.Instantiate(PermissionCallbackHandler, Camera.main.transform);
                    if (permissionPrefab != null)
                    {
                        permissionCallbackHandlerInstance = permissionPrefab.GetComponent<IPermissionCallbackHandler>();
                    }
                }
                foreach (PermissionConfig dangerousPermission in DangerousPermissions)
                {
                    if (dangerousPermission.enabled)
                    {
                        // Setup callbacks for dangerous permission request
                        if (permissionCallbacks == null)
                        {
                            permissionCallbacks = new MLPermissions.Callbacks();
                            permissionCallbacks.OnPermissionGranted += OnDangerousPermissionGranted;
                            permissionCallbacks.OnPermissionDenied += OnDangerousPermissionDenied;
                            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnDangerousPermissionDeniedAndDontAskAgain;
                        }
                        // Request the permission
                        RequestDangerousPermission(dangerousPermission.permission);
                    }
                }
            }

            // Handle normal permissions
            foreach (PermissionConfig normalPermission in NormalPermissions)
            {
                if (normalPermission.enabled)
                {
                    HandleNormalPermissionRequest(normalPermission.permission);
                }
            }
        }

        private void RequestDangerousPermission(string permission)
        {
            MLPermissions.RequestPermission(permission, permissionCallbacks);
        }

        private void OnDangerousPermissionGranted(string permission)
        {
            // Initially only supporting auto start tracking of eyes
            switch (permission)
            {
                case MLPermission.EyeTracking:
                    MLEyes.StartTracking();
                    break;
                default: break;
            }
            if (permissionCallbackHandlerInstance != null)
            {
                permissionCallbackHandlerInstance.OnPermissionGranted(permission);
            }
            Debug.Log("Permission Granted: " + permission);
        }

        private void OnDangerousPermissionDenied(string permission)
        {
            if (permissionCallbackHandlerInstance != null)
            {
                permissionCallbackHandlerInstance.OnPermissionDenied(permission);
            }
            Debug.LogWarning("Permission Denied: " + permission);
        }

        private void OnDangerousPermissionDeniedAndDontAskAgain(string permission)
        {
            if (permissionCallbackHandlerInstance != null)
            {
                permissionCallbackHandlerInstance.OnPermissionDeniedAndDontAskAgain(permission);
            }
            Debug.Log("Permission Denied and Dont Ask Again: " + permission);
        }

        private void HandleNormalPermissionRequest(string permission)
        {
            // Initially only supporting auto start tracking of hands
            switch (permission)
            {
                case MLPermission.HandTracking:
                    MLHands.StartTracking();
                    break;
                default: break;
            }
        }
    }
}