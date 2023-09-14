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
using UnityEngine.InputSystem;
using MixedReality.Toolkit.Input;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Settings
{
    /// <summary>
    /// Provides settings for the runtime configuration of the MRTK XR Rig in order to get inputs
    /// working on the Magic Leap 2.  This allows the default MRTK XR Rig to work on Magic Leap 2,
    /// offering a different option to swapping the rig in the scene with the Magic Leap rig variant.
    /// 
    /// Once Magic Leap 2 utilizes a full OpenXR plugin, this may no longer be necessary.
    /// </summary>
    public sealed class MagicLeapMRTK3SettingsRigConfig : MagicLeapMRTK3SettingsObject
    {
        private const uint RigConfigFileVersion = 1;

#if UNITY_EDITOR

        private const string RigConfigHelpBoxMessage =
            "The Runtime MRTK XR Rig Configuration is an optional way to configure the MRTK XR Rig to be compatible " +
            "with Magic Leap 2 input without needing to modify your scene by swapping in the Magic Leap variant of the rig.  " +
            "In Editor Play Mode, this will only be active if the Magic Leap App Simulator is active in XR Plug-in Management.";

#endif

        [SerializeField]
        [HideInInspector]
        private uint version = RigConfigFileVersion;
        public uint Version => version;

        [SerializeField]
        [Tooltip("Whether the Runtime MRTK XR Rig Configuration is enabled overall")]
        private bool runtimeRigConfigEnabled = false;
        public bool RuntimeRigConfigEnabled => runtimeRigConfigEnabled;

        [SerializeField]
        [Tooltip("List of controller prefabs to add to the MRTK XR Rig.  The prefab names must not match any pre-existing rig " +
                 "controller in order to be added.")]
        private List<GameObject> controllerPrefabsToAdd = new List<GameObject>();
        public List<GameObject> ControllerPrefabsToAdd => controllerPrefabsToAdd;

        [SerializeField]
        [Tooltip("List of InputActionAssets to add to the MRTK XR Rig's InputActionManager")]
        private List<InputActionAsset> inputActionAssetsToAdd = new List<InputActionAsset>();
        public List<InputActionAsset> InputActionAssetsToAdd => inputActionAssetsToAdd;

        [SerializeField]
        [Tooltip("The prefab model to set for the left hand controller's model")]
        private GameObject leftHandModelPrefab = null;
        public GameObject LeftHandModelPrefab => leftHandModelPrefab;

        [SerializeField]
        [Tooltip("The prefab model to set for the right hand controller's model")]
        private GameObject rightHandModelPrefab = null;
        public GameObject RightHandModelPrefab => rightHandModelPrefab;

        [SerializeField]
        [Tooltip("Whether to override the default MRTK InputActions with Magic Leap paths")]
        private bool overrideInputActionPaths = false;
        public bool OverrideInputActionPaths => overrideInputActionPaths;

        [Serializable]
        public struct InputActionOverride
        {
            public InputActionReference actionRef;
            public string overridePath;
        }

        [SerializeField]
        [Tooltip("List of InputActionReferences and corresponding override paths for each")]
        private List<InputActionOverride> actionPathOverrides = new List<InputActionOverride>();
        public List<InputActionOverride> ActionPathOverrides => actionPathOverrides;

        private HashSet<int> configuredRigIDs = new HashSet<int>();

#if UNITY_EDITOR

        /// <inheritdoc/>
        public override void OnGUI()
        {
            serializedObject.Update();

            GUILayout.BeginVertical("Runtime MRTK XR Rig Configuration", new GUIStyle("Window"));
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("runtimeRigConfigEnabled"),
                    new GUIContent("Runtime Config Enabled"));
                DrawGUILineSeparator(4);
                EditorGUILayout.HelpBox(RigConfigHelpBoxMessage, MessageType.Info);
                EditorGUILayout.Space(8);

                // Controller prefabs to add to Rig
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("controllerPrefabsToAdd"),
                        new GUIContent("Controller Prefabs to add to the Rig"));
                    EditorGUILayout.Space(4);
                }

                // InputActionAssets to add to Rig's InputActionManager
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inputActionAssetsToAdd"),
                        new GUIContent("InputActionAssets to add to the Rig's InputActionManager"));
                }

                // Hand mesh model prefab to set for the left and right hand controllers
                {
                    EditorGUILayout.Space(8);
                    const string modelPrefabLabel = "Set the hand mesh model prefab for each hand.  Set to none to leave as is.";
                    EditorGUILayout.LabelField(modelPrefabLabel);
                    float labelWidth = EditorStyles.label.CalcSize(new GUIContent(modelPrefabLabel)).x;
                    DrawGUILineSeparator(2, labelWidth);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("leftHandModelPrefab"),
                        new GUIContent("Left Hand Model Prefab"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rightHandModelPrefab"),
                        new GUIContent("Right Hand Model Prefab"));
                }

                // MRTK InputAction path overrides
                {
                    EditorGUILayout.Space(8);
                    const string overridePathLabel = "Override the default MRTK InputAction paths with Magic Leap input paths.";
                    EditorGUILayout.LabelField(overridePathLabel);
                    float labelWidth = EditorStyles.label.CalcSize(new GUIContent(overridePathLabel)).x;
                    DrawGUILineSeparator(2, labelWidth);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideInputActionPaths"), new GUIContent("Enabled"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("actionPathOverrides"), new GUIContent("InputAction Path Overrides"));
                }

            }
            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

#endif // UNITY_EDITOR

        /// <inheritdoc/>
        public override void ProcessOnBeforeSceneLoad()
        {
            // If Runtime Rig Configuration is not enabled, no need to proceed
            if (!RuntimeRigConfigEnabled)
            {
                return;
            }

            // Override the InputAction paths if enabled
            // This can be done once during application startup
            if (OverrideInputActionPaths)
            {
                foreach (var actionOverride in ActionPathOverrides)
                {
                    actionOverride.actionRef?.action?.ApplyBindingOverride(actionOverride.overridePath);
                }
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <inheritdoc/>
        public override void ProcessOnAfterSceneLoad()
        {
            // Nothing to do here, instead handle each scene loaded in OnSceneLoaded.
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            // Find the MRTK XR Rig and XROrigin within the scene
            // The MRTK XR Rig is assumed to be the top level object of the main Camera
            GameObject mrtkRig = Camera.main.transform.root.gameObject;
            XROrigin xrOrigin = mrtkRig != null ? mrtkRig.GetComponentInChildren<XROrigin>() : null;

            if (mrtkRig == null || xrOrigin == null)
            {
                Debug.LogWarning("Requested to modify the MRTK XR Rig at runtime, " +
                                 "but unable to detect the Rig and XROrigin");
                return;
            }

            // Avoid configuring the same Rig multiple times in case the Rig is set for DontDestroyOnLoad
            if (!configuredRigIDs.Contains(mrtkRig.GetInstanceID()))
            {
                configuredRigIDs.Add(mrtkRig.GetInstanceID());

                // Set hand mesh model prefabs if specified
                // Do this first so as to not potentially alter added Controllers
                ArticulatedHandController[] handControllers = mrtkRig.GetComponentsInChildren<ArticulatedHandController>();
                SetHandControllerModelPrefab(handControllers, LeftHandModelPrefab, XRNode.LeftHand);
                SetHandControllerModelPrefab(handControllers, RightHandModelPrefab, XRNode.RightHand);

                void SetHandControllerModelPrefab(ArticulatedHandController[] handControllers, GameObject prefab, XRNode handNode)
                {
                    if (prefab != null)
                    {
                        foreach (ArticulatedHandController handController in handControllers)
                        {
                            if (handController.HandNode == handNode)
                            {
                                handController.modelPrefab = prefab.transform;
                            }
                        }
                    }
                }

                // Add XR Controllers to the rig
                if (ControllerPrefabsToAdd.Count > 0)
                {
                    GameObject controllerParent = xrOrigin.CameraFloorOffsetObject != null ?
                        xrOrigin.CameraFloorOffsetObject : xrOrigin.gameObject;
                    // Obtain all original child names under controllerParent to avoid adding duplicates
                    HashSet<string> childNames = new HashSet<string>();
                    foreach (Transform child in controllerParent.transform)
                    {
                        childNames.Add(child.gameObject.name);
                    }
                    foreach (GameObject controllerToAdd in ControllerPrefabsToAdd)
                    {
                        if (controllerToAdd == null)
                        {
                            continue;
                        }

                        if (!childNames.Contains(controllerToAdd.name))
                        {
                            GameObject.Instantiate(controllerToAdd, controllerParent.transform);
                        }
                        else
                        {
                            Debug.LogWarning("Controller to be added to the rig has matching name to pre-existing " +
                                             $"controller, '{controllerToAdd.name}', skipping.");
                        }
                    }

                    // Now that XRControllers have been dynamically added to the rig, find the
                    // InteractionModeManager and have it re-initialize interaction mode detectors.
                    InteractionModeManager modeManager = GameObject.FindObjectOfType<InteractionModeManager>();
                    if (modeManager != null)
                    {
                        modeManager.InitializeInteractionModeDetectors();
                    }
                }

                // Add InputActionAssets to the rig's InputActionManager
                InputActionManager inputActionManager = mrtkRig.GetComponentInChildren<InputActionManager>();
                if (inputActionManager != null)
                {
                    foreach (InputActionAsset inputActionAsset in InputActionAssetsToAdd)
                    {
                        if (inputActionAsset == null)
                        {
                            continue;
                        }

                        if (!inputActionManager.actionAssets.Contains(inputActionAsset))
                        {
                            inputActionManager.actionAssets.Add(inputActionAsset);
                        }
                    }
                    inputActionManager.EnableInput();
                }
                else
                {
                    Debug.LogWarning("Attempting to add InputActionAssets to the MRTK XR Rig, " +
                                     "but unable to locate the InputActionManager within the rig.");
                }
            }
        }
    }
}