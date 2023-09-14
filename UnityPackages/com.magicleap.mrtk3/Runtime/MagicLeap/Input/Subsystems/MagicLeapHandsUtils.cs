// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file is derived from the file HandsUtils.cs of MRTK, Copyright (c) Microsoft Corporation,
// as acquired from https://github.com/microsoft/MixedRealityToolkit-Unity/blob/9941264a4a9c2a7252908a41b9dcdfa422438196/com.microsoft.mrtk.core/Utilities/HandsUtils.cs
// under the terms of the MIT license and modified extensively by Magic Leap, Inc.
// The file, as modified, is here provided to you under the terms of the LICENSE file appearing in the
// top-level directory of this distribution.

using System;
using UnityEngine;
using UnityEngine.XR;
using MixedReality.Toolkit;

namespace MagicLeap.MRTK
{
    /// <summary>
    /// Collection of utility methods to simplify working with the Hands subsystem(s),
    /// on the MagicLeap platform.
    /// </summary>
    public static class MagicLeapHandsUtils
    {
        internal static readonly HandFinger[] HandFingers = Enum.GetValues(typeof(HandFinger)) as HandFinger[];

        internal static readonly InputDeviceCharacteristics LeftHandCharacteristics =
            InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left;
        internal static readonly InputDeviceCharacteristics RightHandCharacteristics =
            InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right;

        /// <summary>
        /// Converts a Unity finger bone into an MRTK hand joint.
        /// </summary>
        /// <param name="finger">The Unity classification of the current finger.</param>
        /// <param name="index">The Unity index of the current finger bone.</param>
        /// <returns>The current Unity finger bone converted into an MRTK joint.</returns>
        internal static TrackedHandJoint ConvertToTrackedHandJoint(HandFinger finger, int index)
        {
            switch (finger)
            {
                case HandFinger.Thumb: return TrackedHandJoint.ThumbTip - index;
                case HandFinger.Index: return TrackedHandJoint.IndexTip - index;
                case HandFinger.Middle: return TrackedHandJoint.MiddleTip - index;
                case HandFinger.Ring: return TrackedHandJoint.RingTip - index;
                case HandFinger.Pinky: return TrackedHandJoint.LittleTip - index;
                default: throw new ArgumentOutOfRangeException(nameof(finger));
            }
        }

        /// <summary>
        /// Converts an MRTK joint id into an index, for use when indexing into the joint pose array.
        /// </summary>
        internal static int ConvertToIndex(TrackedHandJoint joint)
        {
            return (int)joint;
        }

        /// <summary>
        /// Converts a joint index into a TrackedHandJoint enum, for use when indexing into the joint pose array.
        /// </summary>
        internal static TrackedHandJoint ConvertFromIndex(int index)
        {
            return (TrackedHandJoint)(index);
        }

        /// <summary>
        /// Gets the Unity finger identification for a given MRTK TrackedHandJoint.
        /// </summary>
        /// <remarks>Due to provider mappings, the wrist is considered the base of the thumb.</remarks>
        /// <param name="joint">The MRTK joint, for which we will return the Unity finger.</param>
        /// <returns>The HandFinger on which the joint exists.</returns>
        internal static HandFinger GetFingerFromJoint(TrackedHandJoint joint)
        {
            Debug.Assert(joint != TrackedHandJoint.Palm && joint != TrackedHandJoint.Wrist,
                         "GetFingerFromJoint passed a non-finger joint");

            if (joint >= TrackedHandJoint.ThumbMetacarpal && joint <= TrackedHandJoint.ThumbTip)
            {
                return HandFinger.Thumb;
            }
            else if (joint >= TrackedHandJoint.IndexMetacarpal && joint <= TrackedHandJoint.IndexTip)
            {
                return HandFinger.Index;
            }
            else if (joint >= TrackedHandJoint.MiddleMetacarpal && joint <= TrackedHandJoint.MiddleTip)
            {
                return HandFinger.Middle;
            }
            else if (joint >= TrackedHandJoint.RingMetacarpal && joint <= TrackedHandJoint.RingTip)
            {
                return HandFinger.Ring;
            }
            else
            {
                return HandFinger.Pinky;
            }
        }

        /// <summary>
        /// Gets the index of the joint relative to the base of the finger.
        /// </summary>
        /// <param name="joint">The MRTK joint, for which we will return its offset from the base.</param>
        /// <returns>Index offset from the metacarpal/base of the finger.</returns>
        internal static int GetOffsetFromBase(TrackedHandJoint joint)
        {
            Debug.Assert(joint != TrackedHandJoint.Palm && joint != TrackedHandJoint.Wrist,
                         "GetOffsetFromBase passed a non-finger joint");

            if (joint >= TrackedHandJoint.ThumbMetacarpal && joint <= TrackedHandJoint.ThumbTip)
            {
                return TrackedHandJoint.ThumbTip - joint;
            }
            else if (joint >= TrackedHandJoint.IndexMetacarpal && joint <= TrackedHandJoint.IndexTip)
            {
                return TrackedHandJoint.IndexTip - joint;
            }
            else if (joint >= TrackedHandJoint.MiddleMetacarpal && joint <= TrackedHandJoint.MiddleTip)
            {
                return TrackedHandJoint.MiddleTip - joint;
            }
            else if (joint >= TrackedHandJoint.RingMetacarpal && joint <= TrackedHandJoint.RingTip)
            {
                return TrackedHandJoint.RingTip - joint;
            }
            else
            {
                return TrackedHandJoint.LittleTip - joint;
            }
        }
    }
}
