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

namespace MagicLeap.MRTK.Samples.HandAndControllerInteraction
{
    /// <summary>
    /// Simple script to reset a RigidBody to its initial pose if it falls
    /// below a certain threshold.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallingRigidbodyReset : MonoBehaviour
    {
        [SerializeField, Tooltip("The vertical threshold, below which the Rigidbody will be reset.")]
        private float verticalThreshold = -10.0f;

        private Rigidbody rigidBody;
        private Pose initialLocalPose;

        void Start()
        {
            rigidBody = GetComponent<Rigidbody>();
            initialLocalPose = new Pose(transform.localPosition, transform.localRotation);
        }

        private void FixedUpdate()
        {
            if (rigidBody.position.y < verticalThreshold)
            {
                rigidBody.velocity = Vector3.zero;
                rigidBody.angularVelocity = Vector3.zero;
                transform.localPosition = initialLocalPose.position;
                transform.localRotation = initialLocalPose.rotation;
            }
        }
    }
}
