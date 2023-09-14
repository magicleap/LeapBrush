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
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.XR;

namespace MagicLeap.MRTK
{
    /// <summary>
    /// Component to manage the enabled state of ArticulatedHandControllers that may
    /// be holding the MagicLeap Controller.
    /// The closest ArticulatedHandController, that is within the proximity
    /// threshold, will be disabled.
    /// Place this component on the ActionBasedController representing the MagicLeap Controller.
    /// </summary>
    [RequireComponent(typeof(ActionBasedController))]
    public class MagicLeapControllerHandProximityDisabler : MonoBehaviour
    {
        [SerializeField]
        private float handProximityThreshold = .2f;
        /// <summary>
        /// The hand proximity threshold under which an ArticulatedHandController is
        /// considered possibly holding the Controller, in meters.
        /// </summary>
        public float HandProximityThreshold
        {
            get => handProximityThreshold;
            set => handProximityThreshold = value;
        }

        private ArticulatedHandController[] articulatedHandControllers;
        private ArticulatedHandController currentClosestHandController = null;
        private ArticulatedHandController targetClosestHandController = null;
        private float closestHandSwitchTimer = 0.0f;
        private const float HandSwitchTimeThreshold = 0.25f;
        private ActionBasedController magicLeapController = null;

        private HandsAggregatorSubsystem HandSubsystem => XRSubsystemHelpers.HandsAggregator as HandsAggregatorSubsystem;

        private void Awake()
        {
            magicLeapController = GetComponent<ActionBasedController>();
            articulatedHandControllers = FindObjectsOfType<ArticulatedHandController>();
        }

        private void OnDisable()
        {
            SetClosestHandController(null);
        }

        void Update()
        {
            ArticulatedHandController newClosestHandController = null;

            bool magicLeapControllerTracking =
                magicLeapController.currentControllerState.inputTrackingState != InputTrackingState.None;

            if (magicLeapControllerTracking && HandSubsystem != null)
            {
                float closestDistance = float.MaxValue;
                Vector3 mlControllerPosition = transform.position;

                foreach (ArticulatedHandController controller in articulatedHandControllers)
                {
                    // Note: Using palm joint position for hand position
                    if (HandSubsystem.TryGetJoint(TrackedHandJoint.Palm,
                                                  controller.HandNode,
                                                  out HandJointPose pose))
                    {
                        Vector3 handPosition = pose.Position;
                        float distance = Vector3.Distance(handPosition, mlControllerPosition);
                        // Favor the current closest hand
                        float compareDistance = distance * (currentClosestHandController == controller ? .8f : 1.0f);
                        if (distance < HandProximityThreshold && compareDistance < closestDistance)
                        {
                            newClosestHandController = controller;
                            closestDistance = compareDistance;
                        }
                    }
                }
            }

            // Filter quick successive changes by requiring the new closest hand controller target
            // to be consistent beyond a time threshold.
            if (newClosestHandController == targetClosestHandController)
            {
                closestHandSwitchTimer += Time.deltaTime;
            }
            else
            {
                targetClosestHandController = newClosestHandController;
                closestHandSwitchTimer = 0.0f;
            }

            // If the new closest hand controller target is a change and the time threshold is met,
            // make the switch.
            if (targetClosestHandController != currentClosestHandController &&
                closestHandSwitchTimer > HandSwitchTimeThreshold)
            {
                SetClosestHandController(targetClosestHandController);
            }
        }

        private void SetClosestHandController(ArticulatedHandController newClosestHandController)
        {
            if (currentClosestHandController == newClosestHandController)
            {
                return;
            }

            if (currentClosestHandController != null)
            {
                currentClosestHandController.gameObject.SetActive(true);
            }

            currentClosestHandController = newClosestHandController;

            if (currentClosestHandController != null)
            {
                currentClosestHandController.gameObject.SetActive(false);
            }
        }
    }
}
