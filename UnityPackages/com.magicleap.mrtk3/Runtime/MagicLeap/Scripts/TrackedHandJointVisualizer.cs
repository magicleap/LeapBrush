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
using UnityEngine;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.XR;
using TMPro;

namespace MagicLeap.MRTK
{
    /// <summary>
    /// Simple MRTK TrackedHandJoint visualizer to verify proper joint bindings.
    /// Utilizes the MRTK hand subsystem to retrieve joint poses.
    /// </summary>
    public class TrackedHandJointVisualizer : MonoBehaviour
    {
        [SerializeField]
        private XRNode handNode = XRNode.RightHand;
        public XRNode HandNode => handNode;

        [SerializeField]
        private GameObject jointPrefab;
        public GameObject JointPrefab => jointPrefab;

        [SerializeField]
        private bool showLabel = true;
        public bool ShowLabel => showLabel;

        private List<GameObject> joints = new List<GameObject>();
        private HandsAggregatorSubsystem HandSubsystem => XRSubsystemHelpers.HandsAggregator as HandsAggregatorSubsystem;

        private void Awake()
        {
            if (jointPrefab == null)
            {
                enabled = false;
                return;
            }
        }

        private void OnValidate()
        {
            // Enforce handNode to be right or left hand in Inspector
            if (!(handNode == XRNode.RightHand || handNode == XRNode.LeftHand))
            {
                handNode = XRNode.RightHand;
            }
        }

        void Start()
        {
            for (int i = 0; i < (int)TrackedHandJoint.TotalJoints; i++)
            {
                GameObject prefab = Instantiate(jointPrefab, this.transform);
                prefab.SetActive(false);
                // Remove colliders on joint visuals so as to not impact raycasting
                foreach (Collider col in prefab.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }
                // Make hand center (palm) a bit bigger
                if (i == (int)TrackedHandJoint.Palm)
                {
                    prefab.transform.localScale *= 1.5f;
                }
                // Set label visible or not
                TextMeshPro tmp = prefab.GetComponentInChildren<TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = ((TrackedHandJoint)i).ToString();
                    tmp.gameObject.SetActive(ShowLabel);
                }
                joints.Add(prefab);
            }
        }

        void Update()
        {
            if (HandSubsystem != null)
            {
                for (int i = 0; i < joints.Count; i++)
                {
                    GameObject joint = joints[i];
                    if (HandSubsystem.TryGetJoint((TrackedHandJoint)i, handNode, out HandJointPose jointPose))
                    {
                        joint.transform.position = jointPose.Position;
                        joint.transform.rotation = jointPose.Rotation;
                        joint.SetActive(true);
                    }
                    else
                    {
                        joint.SetActive(false);
                    }
                    // Billboard label
                    TextMeshPro tmp = joint.GetComponentInChildren<TextMeshPro>();
                    if (tmp != null)
                    {
                        tmp.transform.localPosition = Vector3.zero;
                        tmp.transform.rotation = Camera.main.transform.rotation;
                        tmp.transform.position = tmp.transform.TransformPoint(Vector3.forward * -1.0f);
                    }
                }
            }
        }
    }
}
