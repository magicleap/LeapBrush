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
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using InputTrackingState = UnityEngine.XR.InputTrackingState;
using UnityEngine.XR.MagicLeap;
using System.Threading;
using Unity.XR.CoreUtils;
using MixedReality.Toolkit;
using System.Collections.Generic;
using UnityEngine.XR.Management;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Input
{
    /// <summary>
    /// InputDevice definition to provide auxiliary input controls for
    /// MagicLeap specialized eye data and algorithms.
    /// This should eventually not be needed when the SDK publicly provides equivalent data.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad] // Call static class constructor in editor.
#endif
    public class MagicLeapAuxiliaryEyeDevice : InputDevice, IInputUpdateCallbackReceiver
    {
#if UNITY_EDITOR
        static MagicLeapAuxiliaryEyeDevice()
        {
            // This is a workaround for a Unity bug:
            // Optional validation rule to switch to use InputSystem.XR.PoseControl instead of OpenXR.Input.PoseControl,
            // which fixed PoseControl conflicts in InputSystem.XR and OpenXR.Input.
#if !USE_INPUT_SYSTEM_POSE_CONTROL
            InputSystem.RegisterLayout<PoseControl>("InputSystemPose");
#endif
            // Trigger our RegisterLayout code in the editor.
            RegisterInputLayouts();

            // In Editor, listen for changes in play mode to clean up devices
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    CleanupEditorDeviceInstances();
                }
            };
        }

        ~MagicLeapAuxiliaryEyeDevice()
        {
#if !USE_INPUT_SYSTEM_POSE_CONTROL
            // Remove/unregister the layout that we added as a workaround for the Unity bug.
            InputSystem.RemoveLayout("InputSystemPose");
#endif
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterInputLayouts()
        {
            UnityEngine.InputSystem.InputSystem.RegisterLayout<MagicLeapAuxiliaryEyeDevice>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
            );
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void CreateRuntimeInstances()
        {
#if UNITY_EDITOR
            // Only provide runtime ML auxiliary device if the Magic Leap XRLoader (App Sim) is active
            if (XRGeneralSettings.Instance.AssignedSettings.activeLoader is not MagicLeapLoader)
            {
                return;
            }
            CleanupEditorDeviceInstances();
#endif
            CheckPermissionAndCreateDeviceIfOK();
            if (!deviceCreated)
            {
                // If the permission hasn't been granted at this time, poll its status
                // every second. Only create the device, when granted permission.
                SynchronizationContext mainSyncContext = SynchronizationContext.Current;
                System.Timers.Timer timer = new System.Timers.Timer(1000);
                timer.Start();
                timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
                {
                    mainSyncContext.Post(_ =>
                    {
#if UNITY_EDITOR
                        if (!EditorApplication.isPlaying)
                        {
                            timer.Stop();
                            return;
                        }
#endif
                        CheckPermissionAndCreateDeviceIfOK();
                        if (deviceCreated)
                        {
                            timer.Stop();
                        }
                    }, null);
                };
            }
        }

        // PoseControl is a default Input System control, that wraps a Pose structure,
        // which represents a tracked object in real-word space. Instead of needing to
        // define our own state struct, we can use the PoseState struct directly.
        // This allows us to bind to this device with the usage wildcard */{gaze}/position,
        // *{gaze}/rotation, etc. These usage bindings will also match any OpenXR-compliant real-world eye device.
        [Preserve, InputControl(offset = 0, usage = "gaze")]
        public PoseControl Pose { get; private set; }

        private static MagicLeapInputs.EyesActions eyesActions;
        private static bool deviceCreated = false;
        private static readonly List<MagicLeapAuxiliaryEyeDevice> AuxEyeDevices = new();
        private PoseState poseState;

        protected override void FinishSetup()
        {
            base.FinishSetup();
            Pose = GetChildControl<PoseControl>(nameof(Pose));
        }

        public static void CreateDevice()
        {
            // Ensure we don't create the device more than once.
            // SynchronizationContext.Post has a slight delay on startup, and our timer
            // could enqueue multiple Post calls before they're run.
            if (!deviceCreated)
            {
                MagicLeapInputs mlInputs = new MagicLeapInputs();
                mlInputs.Enable();
                eyesActions = new MagicLeapInputs.EyesActions(mlInputs);
                InputSystem.AddDevice<MagicLeapAuxiliaryEyeDevice>($"{nameof(MagicLeapAuxiliaryEyeDevice)}");
                deviceCreated = true;
            }
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            AuxEyeDevices.Add(this);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            AuxEyeDevices.Remove(this);
        }

        public void OnUpdate()
        {
            if (!eyesActions.Data.IsInProgress())
            {
                return;
            }
            Eyes eyes = eyesActions.Data.ReadValue<Eyes>();

            // Transform the eyeOrigin into XR Camera Offset space, which is the space
            // of the eye fixation point.
            XROrigin xrOrigin = PlayspaceUtilities.XROrigin;
            Vector3 eyeOrigin = Camera.main.transform.position;
            if (xrOrigin != null)
            {
                eyeOrigin = xrOrigin.CameraFloorOffsetObject.transform.InverseTransformPoint(eyeOrigin);
            }

            Vector3 eyeFixationPoint = eyes.fixationPoint;
            if (eyeFixationPoint == eyeOrigin)
            {
                // Protect against zero look rotation viewing vector
                return;
            }
            // Find the direction and rotation
            Vector3 eyeDirection = (eyeFixationPoint - eyeOrigin).normalized;
            Quaternion eyeRotation = Quaternion.LookRotation(eyeDirection);

            // Set the pose state
            poseState.position = eyeOrigin;
            poseState.rotation = eyeRotation;
            poseState.trackingState = InputTrackingState.Position | InputTrackingState.Rotation;
            InputState.Change(this.Pose, poseState);
        }

        private static void CheckPermissionAndCreateDeviceIfOK()
        {
            if (MLPermissions.CheckPermission(MLPermission.EyeTracking).IsOk)
            {
                // Permission is granted, create the eye input device.
                CreateDevice();
            }
        }

#if UNITY_EDITOR
        private static void CleanupEditorDeviceInstances()
        {
            for (int i = AuxEyeDevices.Count - 1; i >= 0; i--)
            {
                InputSystem.RemoveDevice(AuxEyeDevices[i]);
            }
        }
#endif
    }
}
