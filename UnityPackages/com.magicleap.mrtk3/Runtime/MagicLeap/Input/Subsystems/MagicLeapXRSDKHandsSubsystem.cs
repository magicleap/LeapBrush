// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file is derived from the file XRSDKHandsSubsystem.cs of MRTK, Copyright (c) Microsoft Corporation,
// as acquired from https://github.com/microsoft/MixedRealityToolkit-Unity/blob/9941264a4a9c2a7252908a41b9dcdfa422438196/com.microsoft.mrtk.input/Subsystems/Hands/XRSDKHandsSubsystem.cs
// under the terms of the MIT license and modified extensively by Magic Leap, Inc.
// The file, as modified, is here provided to you under the terms of the LICENSE file appearing in the
// top-level directory of this distribution.

using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit.Input;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
using InputDevice = UnityEngine.XR.InputDevice;
using MagicLeap.MRTK.Settings;

namespace MagicLeap.MRTK.Input
{
    [Preserve]
    [MRTKSubsystem(
        Name = "com.magicleap.xr.xrsdkhands",
        DisplayName = "MagicLeap Subsystem for XRSDK Hands API",
        Author = "MagicLeap",
        ProviderType = typeof(XRSDKProvider),
        SubsystemTypeOverride = typeof(MagicLeapXRSDKHandsSubsystem),
        ConfigType = typeof(BaseSubsystemConfig))]
    public class MagicLeapXRSDKHandsSubsystem : HandsSubsystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            // Fetch subsystem metadata from the attribute.
            var cinfo = XRSubsystemHelpers.ConstructCinfo<MagicLeapXRSDKHandsSubsystem, HandsSubsystemCinfo>();

            // Populate remaining cinfo field.
            cinfo.IsPhysicalData = true;

            if (!Register(cinfo))
            {
                Debug.LogError($"Failed to register the {cinfo.Name} subsystem.");
            }
        }

        private class XRSDKHandContainer : HandDataContainer
        {
            // The cached reference to the XRSDK tracked hand.
            // Is re-queried/TryGetFeatureValue'd each frame,
            // as the presence (or absence) of this reference
            // indicates tracking state.
            private Hand? handDevice;
            private InputDevice? inputDevice;

            private MagicLeapMRTK3SettingsGeneral magicLeapMRTK3GeneralSettings;

            public XRSDKHandContainer(XRNode handNode) : base(handNode)
            {
                handDevice = GetTrackedHand();

                magicLeapMRTK3GeneralSettings =
                    MagicLeapMRTK3Settings.Instance.GetSettingsObject<MagicLeapMRTK3SettingsGeneral>();
            }

            private static readonly ProfilerMarker TryGetEntireHandPerfMarker =
                new ProfilerMarker("[MRTK] MLXRSDKHandsSubsystem.TryGetEntireHand");

            /// <inheritdoc/>
            public override bool TryGetEntireHand(out IReadOnlyList<HandJointPose> result)
            {
                if (!magicLeapMRTK3GeneralSettings.MRTK3HandInteractionsEnabled)
                {
                    result = HandJoints;
                    return false;
                }

                using (TryGetEntireHandPerfMarker.Auto())
                {
                    if (!AlreadyFullQueried)
                    {
                        TryCalculateEntireHand();
                    }

                    result = HandJoints;
                    return FullQueryValid;
                }
            }

            private static readonly ProfilerMarker TryGetJointPerfMarker =
                new ProfilerMarker("[MRTK] XRSDKHandsSubsystem.TryGetJoint");

            /// <inheritdoc/>
            public override bool TryGetJoint(TrackedHandJoint joint, out HandJointPose pose)
            {
                if (!magicLeapMRTK3GeneralSettings.MRTK3HandInteractionsEnabled)
                {
                    pose = HandJoints[MagicLeapHandsUtils.ConvertToIndex(joint)];
                    return false;
                }

                using (TryGetJointPerfMarker.Auto())
                {
                    bool thisQueryValid = false;

                    // If we happened to have already queried the entire
                    // hand data this frame, we don't need to re-query for
                    // just the joint. If we haven't, we do still need to
                    // query for the single joint.
                    if (!AlreadyFullQueried)
                    {
                        handDevice = GetTrackedHand();

                        // If the tracked hand is null, we obviously have no data,
                        // and return immediately.
                        if (!handDevice.HasValue)
                        {
                            pose = HandJoints[MagicLeapHandsUtils.ConvertToIndex(joint)];
                            return false;
                        }

                        // Joints are relative to the camera floor offset object.
                        Transform origin = PlayspaceUtilities.XROrigin.CameraFloorOffsetObject.transform;
                        if (origin == null)
                        {
                            pose = HandJoints[MagicLeapHandsUtils.ConvertToIndex(joint)];
                            return false;
                        }

                        // Otherwise, we need to deal with palm/root & wrist vs finger separately
                        if (joint == TrackedHandJoint.Palm)
                        {
                            if (inputDevice.Value.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                                inputDevice.Value.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                            {
                                thisQueryValid |= UpdateJoint(TrackedHandJoint.Palm, position, rotation, origin);
                            }
                        }
                        else if(joint == TrackedHandJoint.Wrist)
                        {
                            if (inputDevice.Value.TryGetFeatureValue(WristCenter, out Bone wristBone))
                            {
                                thisQueryValid |= TryUpdateJoint(TrackedHandJoint.Wrist, wristBone, origin);
                            }
                        }
                        else
                        {
                            HandFinger finger = MagicLeapHandsUtils.GetFingerFromJoint(joint);
                            if (handDevice.Value.TryGetFingerBones(finger, fingerBones))
                            {
                                Bone bone = fingerBones[MagicLeapHandsUtils.GetOffsetFromBase(joint)];
                                thisQueryValid |= TryUpdateJoint(joint, bone, origin);
                            }
                        }
                    }
                    else
                    {
                        // If we've already run a full-hand query, this single joint query
                        // is just as valid as the full query.
                        thisQueryValid = FullQueryValid;
                    }

                    pose = HandJoints[MagicLeapHandsUtils.ConvertToIndex(joint)];
                    return thisQueryValid;
                }
            }

            // Scratchpad for reading out devices, to reduce allocs.
            private readonly List<InputDevice> handDevices = new List<InputDevice>(2);

            private static readonly ProfilerMarker GetTrackedHandPerfMarker =
                new ProfilerMarker("[MRTK] XRSDKHandsSubsystem.GetTrackedHand");

            /// <summary>
            /// Obtains a reference to the actual Hand object representing the tracked hand
            /// functionality present on handNode. Returns null if no Hand reference available.
            /// </summary>
            private Hand? GetTrackedHand()
            {
                using (GetTrackedHandPerfMarker.Auto())
                {
                    InputDevices.GetDevicesWithCharacteristics(HandNode == XRNode.LeftHand ? MagicLeapHandsUtils.LeftHandCharacteristics : MagicLeapHandsUtils.RightHandCharacteristics, handDevices);

                    if (handDevices.Count == 0)
                    {
                        // No hand devices detected at this hand.
                        return null;
                    }
                    else
                    {
                        foreach (InputDevice device in handDevices)
                        {
                            if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked)
                                && isTracked
                                && device.TryGetFeatureValue(CommonUsages.handData, out Hand handRef))
                            {
                                inputDevice = device;
                                // We've found our device that supports CommonUsages.handData, and
                                // the specific Hand object that we can return.
                                return handRef;
                            }
                        }

                        // None of the devices on this hand are tracked and/or support CommonUsages.handData.
                        // This will happen when the platform doesn't support hand tracking,
                        // or the hand is not visible enough to return a tracking solution.
                        return null;
                    }
                }
            }

            // Scratchpad for reading out finger bones, to reduce allocs.
            private readonly List<Bone> fingerBones = new List<Bone>();

            private static readonly ProfilerMarker TryCalculateEntireHandPerfMarker =
                new ProfilerMarker("[MRTK] XRSDKHandsSubsystem.TryCalculateEntireHand");

            public static InputFeatureUsage<Bone> WristCenter = new InputFeatureUsage<Bone>("MLHandWristCenter");

            /// <summary/>
            /// For a certain hand, query every Bone in the hand, and write all results to the
            /// HandJoints collection. This will also mark handsQueriedThisFrame[handNode] = true.
            /// </summary>
            private void TryCalculateEntireHand()
            {
                using (TryCalculateEntireHandPerfMarker.Auto())
                {
                    handDevice = GetTrackedHand();

                    if (!handDevice.HasValue)
                    {
                        // No articulated hand device available this frame.
                        FullQueryValid = false;
                        AlreadyFullQueried = true;
                        return;
                    }

                    // Null checks against Unity objects can be expensive, especially when you do
                    // it 52 times per frame (26 hand joints across 2 hands). Instead, we manage
                    // the playspace transformation internally for hand joints.
                    // Joints are relative to the camera floor offset object.
                    Transform origin = PlayspaceUtilities.XROrigin.CameraFloorOffsetObject.transform;
                    if (origin == null)
                    {
                        return;
                    }

                    FullQueryValid = true;
                    foreach (HandFinger finger in MagicLeapHandsUtils.HandFingers)
                    {
                        if (handDevice.Value.TryGetFingerBones(finger, fingerBones))
                        {
                            for (int i = 0; i < fingerBones.Count; i++)
                            {
                                FullQueryValid &= TryUpdateJoint(MagicLeapHandsUtils.ConvertToTrackedHandJoint(finger, i), fingerBones[i], origin);
                            }
                        }
                    }

                    // Wrist
                    if (inputDevice.Value.TryGetFeatureValue(WristCenter, out Bone bone))
                    {
                        FullQueryValid &= TryUpdateJoint(TrackedHandJoint.Wrist, bone, origin);
                    }

                    // Palm
                    if (inputDevice.Value.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                        inputDevice.Value.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                    {
                        FullQueryValid &= UpdateJoint(TrackedHandJoint.Palm, position, rotation, origin);
                    }

                    // Mark this hand as having been fully queried this frame.
                    // If any joint is queried again this frame, we'll reuse the
                    // information to avoid extra work.
                    AlreadyFullQueried = true;
                }
            }

            private static readonly ProfilerMarker TryUpdateJointPerfMarker =
                new ProfilerMarker("[MRTK] XRSDKHandsSubsystem.TryUpdateJoint");

            /// <summary/>
            /// Given a destination jointID, apply the Bone info to the correct struct
            /// in the HandJoints collection.
            /// </summary>
            private bool TryUpdateJoint(TrackedHandJoint jointID, Bone bone, Transform playspaceTransform)
            {
                using (TryUpdateJointPerfMarker.Auto())
                {
                    bool gotData = true;
                    gotData &= bone.TryGetPosition(out Vector3 position);
                    gotData &= bone.TryGetRotation(out Quaternion rotation);

                    if (!gotData)
                    {
                        return false;
                    }

                    UpdateJoint(jointID, position, rotation, playspaceTransform);

                    return true;
                }
            }

            private bool UpdateJoint(TrackedHandJoint jointID, Vector3 position, Quaternion rotation, Transform playspaceTransform)
            {
                // XRSDK does not return joint radius. 0.5cm default.
                HandJoints[MagicLeapHandsUtils.ConvertToIndex(jointID)] = new HandJointPose(
                    playspaceTransform.TransformPoint(position),
                    playspaceTransform.rotation * rotation,
                    0.005f);

                return true;
            }
        }

        [Preserve]
        private class XRSDKProvider : Provider
        {
            private Dictionary<XRNode, XRSDKHandContainer> hands = null;

            public override void Start()
            {
                base.Start();

                hands ??= new Dictionary<XRNode, XRSDKHandContainer>
                {
                    { XRNode.LeftHand, new XRSDKHandContainer(XRNode.LeftHand) },
                    { XRNode.RightHand, new XRSDKHandContainer(XRNode.RightHand) }
                };

                InputSystem.onBeforeUpdate += ResetHands;
            }

            public override void Stop()
            {
                ResetHands();
                InputSystem.onBeforeUpdate -= ResetHands;
                base.Stop();
            }

            private void ResetHands()
            {
                hands[XRNode.LeftHand].Reset();
                hands[XRNode.RightHand].Reset();
            }

            #region IHandsSubsystem implementation

            /// <inheritdoc/>
            public override bool TryGetEntireHand(XRNode handNode, out IReadOnlyList<HandJointPose> jointPoses)
            {
                Debug.Assert(handNode == XRNode.LeftHand || handNode == XRNode.RightHand, "Non-hand XRNode used in TryGetEntireHand query");

                return hands[handNode].TryGetEntireHand(out jointPoses);
            }

            /// <inheritdoc/>
            public override bool TryGetJoint(TrackedHandJoint joint, XRNode handNode, out HandJointPose jointPose)
            {
                Debug.Assert(handNode == XRNode.LeftHand || handNode == XRNode.RightHand, "Non-hand XRNode used in TryGetJoint query");

                return hands[handNode].TryGetJoint(joint, out jointPose);
            }

            #endregion IHandsSubsystem implementation
        }
    }
}
