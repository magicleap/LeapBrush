using System;
using Google.Protobuf.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class OtherUserVisual : MonoBehaviour
    {
        public event Action OnDestroyed;

        [SerializeField]
        private OtherUserWearable _wearable;

        [SerializeField]
        private OtherUserController _controller;

        [SerializeField]
        private OtherUserToolManager _leftHandTools;

        [SerializeField]
        private OtherUserToolManager _rightHandTools;

        [SerializeField]
        private OtherUserToolManager _controllerTools;

        public Transform WearableTransform => _wearable.transform;
        public Transform ControllerTransform => _controller.transform;

        public DateTimeOffset LastUpdateTime = DateTimeOffset.Now;

        public static readonly Vector3 ServerEchoPositionOffset = new(0.1f, 0, 0);

        private static readonly Vector3 ServerEchoWearablePositionOffset = new(.5f, 0, 0);

        private bool _showHandsAndControls;
        private bool _isControllerPoseValid;
        private bool _showHeadsets;
        private bool _cameraFollowingUser;

        public void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        public void SetShowHeadset(bool showHeadsets)
        {
            _showHeadsets = showHeadsets;

            UpdateHeadsetVisibility();
        }

        public void SetCameraFollowingUser(bool cameraFollowingUser)
        {
            _cameraFollowingUser = cameraFollowingUser;

            UpdateHeadsetVisibility();
        }

        public void ShowHandsAndControls(bool showHandsAndControls)
        {
            _showHandsAndControls = showHandsAndControls;
            _controller.gameObject.SetActive(showHandsAndControls);
        }

        public void HandleStateUpdate(UserStateProto userState, bool isServerEcho)
        {
            TransformExtensions.SetLocalPose(_wearable.transform,
                ProtoUtils.FromProto(userState.HeadPose));
            if (isServerEcho)
            {
                _wearable.transform.localPosition += ServerEchoWearablePositionOffset;
            }

            if (userState.UserDisplayName != null)
            {
                _wearable.SetUserDisplayName(userState.UserDisplayName);
            }

            if (userState.HeadsetBattery != null)
            {
                _wearable.SetHeadsetBattery(userState.HeadsetBattery);
            }

            if (userState.ControllerState != null)
            {
                TransformExtensions.SetLocalPose(_controller.transform,
                    ProtoUtils.FromProto(userState.ControllerState.Pose));
                if (isServerEcho)
                {
                    _controller.transform.localPosition += ServerEchoPositionOffset;
                }

                _isControllerPoseValid = true;
            }
            else
            {
                _isControllerPoseValid = false;
            }
            UpdateControllerVisibility();

            HandleControllerTools(_controllerTools, userState,
                userState.ControllerState, isServerEcho);
            HandleHandTools(_leftHandTools, userState,
                userState.LeftHandState, isServerEcho);
            HandleHandTools(_rightHandTools, userState,
                userState.RightHandState, isServerEcho);

            LastUpdateTime = DateTimeOffset.Now;
        }

        private void HandleControllerTools(OtherUserToolManager toolManager,
            UserStateProto userState,
            ControllerStateProto controllerStateProto, bool isServerEcho)
        {
            Pose toolOffsetPose = Pose.identity;
            if (controllerStateProto != null)
            {
                toolOffsetPose = ProtoUtils.FromProto(controllerStateProto.Pose);
                toolOffsetPose.position = toolOffsetPose.ApplyOffsetTo(
                    Vector3.forward * controllerStateProto.ToolOffsetZ);
                if (isServerEcho)
                {
                    toolOffsetPose.position += ServerEchoPositionOffset;
                }
                toolManager.ShowTool(userState, toolOffsetPose);
            }
            else
            {
                toolManager.HideTool();
            }

            if (controllerStateProto != null && controllerStateProto.RayPoints.Count > 0)
            {
                toolManager.ShowRay(userState, controllerStateProto.SelectProgress,
                    controllerStateProto.RayPoints, isServerEcho);
            }
            else
            {
                toolManager.HideRay();
            }
        }

        private void HandleHandTools(OtherUserToolManager toolManager,
            UserStateProto userState,
            HandStateProto handStateProto, bool isServerEcho)
        {
            if (handStateProto != null)
            {
                Pose toolOffsetPose = ProtoUtils.FromProto(handStateProto.ToolPose);
                if (isServerEcho)
                {
                    toolOffsetPose.position += ServerEchoPositionOffset;
                }
                toolManager.ShowTool(userState, toolOffsetPose);
            }
            else
            {
                toolManager.HideTool();
            }

            if (handStateProto != null && handStateProto.RayPoints.Count > 0)
            {
                toolManager.ShowRay(userState, handStateProto.SelectProgress,
                    handStateProto.RayPoints, isServerEcho);
            }
            else
            {
                toolManager.HideRay();
            }
        }

        private void UpdateControllerVisibility()
        {
            _controller.gameObject.SetActive(_showHandsAndControls && _isControllerPoseValid);
        }

        private void UpdateHeadsetVisibility()
        {
            _wearable.gameObject.SetActive(_showHeadsets && !_cameraFollowingUser);
        }
    }
}