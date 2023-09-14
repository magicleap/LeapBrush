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
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using XRNode = UnityEngine.XR.XRNode;
using InputTrackingState = UnityEngine.XR.InputTrackingState;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;
using System.Collections.Generic;
using MixedReality.Toolkit.Input;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MagicLeap.MRTK.Input
{
    /// <summary>
    /// State structure for a <see cref="MagicLeapAuxiliaryHandDevice"/> device
    /// </summary>
    public struct MagicLeapAuxiliaryHandState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC("MLAH");

        [InputControl(layout = "Integer")]
        public int trackingState;

        [InputControl(layout = "Button")]
        public bool isTracked;

        [InputControl(layout = "Vector3")]
        public Vector3 devicePosition;

        [InputControl(layout = "Quaternion")]
        public Quaternion deviceRotation;

        [InputControl(layout = "Button")]
        public bool pinchPressed;

        [InputControl(layout = "Axis")]
        public float pinch;

        [InputControl(layout = "Vector3")]
        public Vector3 pointerPosition;

        [InputControl(layout = "Quaternion")]
        public Quaternion pointerRotation;
    }

    /// <summary>
    /// InputDevice definition to provide auxiliary input controls for
    /// MagicLeap specialized hand data and algorithms.
    /// This should eventually not be needed when the SDK publicly provides equivalent data.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad] // Call static class constructor in editor.
#endif
    [InputControlLayout(stateType = typeof(MagicLeapAuxiliaryHandState),
                        displayName = "MagicLeapAuxiliaryHandDevice",
                        commonUsages = new[] { "LeftHand", "RightHand" })]
    public class MagicLeapAuxiliaryHandDevice : TrackedDevice, IInputUpdateCallbackReceiver
    {
#if UNITY_EDITOR
        static MagicLeapAuxiliaryHandDevice()
        {
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
        
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterInputLayouts()
        {
            UnityEngine.InputSystem.InputSystem.RegisterLayout<MagicLeapAuxiliaryHandDevice>(
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

            // Create new instances only if running on MagicLeap (device or app sim) and hand tracking is granted
            if (MLPermissions.CheckPermission(MLPermission.HandTracking).IsOk)
            {
                MagicLeapAuxiliaryHandDevice leftHand = InputSystem.AddDevice<MagicLeapAuxiliaryHandDevice>(
                    $"{nameof(MagicLeapAuxiliaryHandDevice)} - {CommonUsages.LeftHand}");
                leftHand.HandNode = XRNode.LeftHand;

                MagicLeapAuxiliaryHandDevice rightHand = InputSystem.AddDevice<MagicLeapAuxiliaryHandDevice>(
                    $"{nameof(MagicLeapAuxiliaryHandDevice)} - {CommonUsages.RightHand}");
                rightHand.HandNode = XRNode.RightHand;
            }
        }

        private XRNode handNode;
        /// <summary>
        /// The <see cref="XRNode"/> hand node assigned to this device
        /// </summary>
        public XRNode HandNode {
            get => handNode;
            private set
            {
                handNode = value;
                InputSystem.SetDeviceUsage(this, value == XRNode.RightHand ?
                                                          CommonUsages.RightHand :
                                                          CommonUsages.LeftHand);
            }
        }

        [Preserve]
        [InputControl]
        public ButtonControl pinchPressed { get; private set; }

        [Preserve]
        [InputControl]
        public AxisControl pinch { get; private set; }

        [Preserve]
        [InputControl]
        public Vector3Control pointerPosition { get; private set; }

        [Preserve]
        [InputControl]
        public QuaternionControl pointerRotation { get; private set; }


        private static HandsAggregatorSubsystem HandSubsystem => XRSubsystemHelpers.HandsAggregator as HandsAggregatorSubsystem;
        private static readonly Vector3 HandRayOriginOffset = new Vector3(-0.15f, 0.07f, 0.12f);
        private static readonly Vector3 HandRayAngleOffset = new Vector2(-22.0f, -11.0f);
        private bool pinchedLastFrame = false;
        private bool wasTrackedLastFrame = false;
        private static readonly List<MagicLeapAuxiliaryHandDevice> AuxHandDevices = new();
        private HandRay handRay = new HandRay();

        protected override void FinishSetup()
        {
            base.FinishSetup();

            pinchPressed = GetChildControl<ButtonControl>(nameof(pinchPressed));
            pinch = GetChildControl<AxisControl>(nameof(pinch));
            pointerPosition = GetChildControl<Vector3Control>(nameof(pointerPosition));
            pointerRotation = GetChildControl<QuaternionControl>(nameof(pointerRotation));
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            AuxHandDevices.Add(this);
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            AuxHandDevices.Remove(this);
        }

        public void OnUpdate()
        {
            if (HandSubsystem == null)
                return;

            // Only update state if, at a minimum, we get a valid tracked hand Palm
            if (HandSubsystem.TryGetJoint(TrackedHandJoint.Palm, HandNode, out HandJointPose pose))
            {
                wasTrackedLastFrame = true;

                var state = new MagicLeapAuxiliaryHandState();
                state.isTracked = true;
                state.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);

                // Device Position/Rotation
                // Input actions expected in XR scene-origin-space
                HandJointPose xrDevicePose = PlayspaceUtilities.InverseTransformPose(pose);
                state.devicePosition = xrDevicePose.Position;
                state.deviceRotation = xrDevicePose.Rotation;

                // Select & Select Value (progress)
                if (HandSubsystem.TryGetPinchProgress(HandNode,
                                                      out bool isPinchReady,
                                                      out bool isPinching,
                                                      out float pinchAmount))
                {
                    // Debounce pinch
                    bool isPinched = pinchAmount >= (pinchedLastFrame ? 0.85f : 1.0f);

                    state.pinchPressed = isPinched;
                    state.pinch = pinchAmount;

                    pinchedLastFrame = isPinched;
                }

                // Pointer Position/Rotation (Hand Ray)
                if (TryGetHandRayPose(out Pose handRayPose))
                {
                    // Input actions expected in XR scene-origin-space
                    Pose xrHandRayPose = PlayspaceUtilities.InverseTransformPose(handRayPose);
                    state.pointerPosition = xrHandRayPose.position;
                    state.pointerRotation = xrHandRayPose.rotation;
                }

                InputSystem.QueueStateEvent(this, state);
            }
            else if (wasTrackedLastFrame)
            {
                // If the hand is no longer tracked, reset the state once until tracked again
                InputSystem.QueueStateEvent(this, new MagicLeapAuxiliaryHandState());
                wasTrackedLastFrame = false;
            }
        }

        /// <summary>
        /// Gets hand ray pose in world space
        /// This is based on MRTK's PolyfillHandRayPoseSource::TryGetPose
        /// </summary>
        private bool TryGetHandRayPose(out Pose pose)
        {
            // Tick the hand ray generator function. Uses index knuckle for position.
            if (HandSubsystem.TryGetJoint(TrackedHandJoint.IndexProximal, HandNode, out HandJointPose knuckle) &&
                HandSubsystem.TryGetJoint(TrackedHandJoint.Palm, HandNode, out HandJointPose palm))
            {
                handRay.Update(knuckle.Position, -palm.Up, Camera.main.transform, handNode.ToHandedness());
                pose = new Pose(handRay.Ray.origin,
                                Quaternion.LookRotation(handRay.Ray.direction, palm.Up));
                return true;
            }

            pose = Pose.identity;
            return false;
        }

#if UNITY_EDITOR
        private static void CleanupEditorDeviceInstances()
        {
            for (int i = AuxHandDevices.Count - 1; i >= 0; i--)
            {
                InputSystem.RemoveDevice(AuxHandDevices[i]);
            }
        }
#endif
    }
}
