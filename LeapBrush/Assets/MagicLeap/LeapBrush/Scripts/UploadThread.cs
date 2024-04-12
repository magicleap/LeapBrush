using System;
using System.Collections.Generic;
using System.Threading;
using Grpc.Core;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Thread for uploading changes to the server.
    /// </summary>
    /// <remarks>
    /// This thread runs for the duration of a connection to the server and sends up events
    /// when user state changes or the current user modifies or creates brush strokes and 3D
    /// models.
    /// </remarks>
    public class UploadThread
    {
        public bool LastUploadOk
        {
            get
            {
                lock (_lock)
                {
                    return _lastUploadOk;
                }
            }
        }

        public enum HandType
        {
            Right,
            Left
        }

        private const float MinServerUpdateIntervalSeconds = .03f;
        private const float ServerPingIntervalSeconds = 2.0f;

        private readonly Thread _thread;
        private readonly LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private readonly CancellationTokenSource _shutDownTokenSource;
        private readonly string _userName;

        private readonly object _lock = new();
        private bool _lastUploadOk;
        private string _headClosestAnchorId;
        private ControllerStateProto _controllerState;
        private Dictionary<HandType, HandStateProto> _handStateMap = new();
        private Pose _headPoseRelativeToAnchor = Pose.identity;
        private bool _serverEchoEnabled;
        private string _userDisplayName;
        private UserStateProto.Types.ToolState _currentToolState;
        private Color32 _currentToolColor;
        private BatteryStatusProto _batteryStatus = new();
        private SpaceInfoProto _spaceInfo = new();
        private LinkedList<BrushStrokeProto> _brushStrokesToUpload = new();
        private LinkedList<BrushStrokeRemoveRequest> _brushStrokesToRemove = new();
        private Dictionary<string, ExternalModelProto> _externalModelsToUpdate = new();
        private LinkedList<ExternalModelRemoveRequest> _externalModelsToRemove = new();
        private Dictionary<BrushBase, BrushStrokeProto> _currentBrushStrokes = new();

        public object Lock => _lock;

        /// <summary>
        /// Get the current brush stroke proto in use for <paramref name="brushTool" />
        /// The caller must hold <see cref="Lock"/> when using this return value.
        /// </summary>
        public BrushStrokeProto GetCurrentBrushStroke(BrushBase brushTool)
        {
            return _currentBrushStrokes.GetValueOrDefault(brushTool, null);
        }

        /// <summary>
        /// Set the current brush stroke proto in use for <paramref name="brushTool" />
        /// The caller must hold <see cref="Lock"/> while calling this method.
        /// </summary>
        public void SetCurrentBrushStroke(BrushBase brushTool, BrushStrokeProto brushStrokeProto)
        {
            _currentBrushStrokes[brushTool] = brushStrokeProto;
        }

        public UploadThread(LeapBrushApiBase.LeapBrushClient leapBrushClient,
            CancellationTokenSource shutDownTokenSource,
            string userName, string userDisplayName)
        {
            _thread = new Thread(Run);
            _leapBrushClient = leapBrushClient;
            _shutDownTokenSource = shutDownTokenSource;
            _userName = userName;
            _userDisplayName = userDisplayName;
        }

        public void Start() => _thread.Start();
        public void Join() => _thread.Join();

        public void SetHeadClosestAnchorId(string headClosestAnchorId)
        {
            lock (_lock)
            {
                _headClosestAnchorId = headClosestAnchorId;
            }
        }

        public void ClearAnchorAttachedStates()
        {
            lock (_lock)
            {
                _headClosestAnchorId = null;
                _headPoseRelativeToAnchor = Pose.identity;
                _controllerState = null;
                _handStateMap.Clear();
            }
        }

        public void SetControllerStateRelativeToAnchor(Pose pose, float toolOffsetZ,
            float selectProgress, Vector3[] rayPoints)
        {
            lock (_lock)
            {
                if (_controllerState == null)
                {
                    _controllerState = new();
                }
                _controllerState.Pose = ProtoUtils.ToProto(pose, _controllerState.Pose);
                _controllerState.ToolOffsetZ = toolOffsetZ;
                _controllerState.SelectProgress = selectProgress;
                ProtoUtils.ToProto(rayPoints, _controllerState.RayPoints);
            }
        }

        public void ClearControllerState()
        {
            lock (_lock)
            {
                _controllerState = null;
            }
        }

        public void SetHandStateRelativeToAnchor(HandType hand, Pose toolPose,
            float selectProgress, Vector3[] rayPoints)
        {
            lock (_lock)
            {
                if (!_handStateMap.TryGetValue(hand, out HandStateProto handState))
                {
                    handState = _handStateMap[hand] = new HandStateProto();
                }
                handState.ToolPose = ProtoUtils.ToProto(toolPose, handState.ToolPose);
                handState.SelectProgress = selectProgress;
                ProtoUtils.ToProto(rayPoints, handState.RayPoints);
            }
        }

        public void ClearHandState(HandType hand)
        {
            lock (_lock)
            {
                _handStateMap.Remove(hand);
            }
        }

        public void SetHeadPoseRelativeToAnchor(Pose pose)
        {
            lock (_lock)
            {
                _headPoseRelativeToAnchor = pose;
            }
        }

        public void SetServerEchoEnabled(bool enabled)
        {
            lock (_lock)
            {
                _serverEchoEnabled = enabled;
            }
        }

        public void SetUserDisplayName(string userDisplayName)
        {
            lock (_lock)
            {
                _userDisplayName = userDisplayName;
            }
        }

        public void SetCurrentToolState(UserStateProto.Types.ToolState toolState,
            Color32 toolColor)
        {
            lock (_lock)
            {
                _currentToolState = toolState;
                _currentToolColor = toolColor;
            }
        }

        public void SetBatteryStatus(float batteryLevel, BatteryStatus batteryStatus)
        {
            lock (_lock)
            {
                _batteryStatus.Level = (uint) Mathf.RoundToInt(
                    Mathf.Clamp01(batteryLevel) * 100);
                _batteryStatus.State = ProtoUtils.ToProto(batteryStatus);
            }
        }

        public void SetSpaceInfo(LocalizationMapManager.LocalizationMapInfo localizationInfo,
            bool isUsingImportedAnchors, AnchorsApi.Anchor[] anchors)
        {
            lock (_lock)
            {
                _spaceInfo.SpaceId = localizationInfo.MapUUID ?? "";
                _spaceInfo.SpaceName = localizationInfo.MapName ?? "";
                _spaceInfo.MappingMode = ProtoUtils.ToProto(localizationInfo.MapType);
                _spaceInfo.TargetSpaceOrigin = ProtoUtils.ToProto(
                    localizationInfo.OriginPose, _spaceInfo.TargetSpaceOrigin);
                _spaceInfo.UsingImportedAnchors = isUsingImportedAnchors;

                if (_spaceInfo.Anchor.Count > anchors.Length)
                {
                    _spaceInfo.Anchor.Clear();
                }

                for (int i = 0; i < anchors.Length; ++i)
                {
                    AnchorsApi.Anchor anchor = anchors[i];

                    if (i >= _spaceInfo.Anchor.Count)
                    {
                        _spaceInfo.Anchor.Add(new AnchorProto());
                    }

                    AnchorProto anchorProto = _spaceInfo.Anchor[i];
                    anchorProto.Id = anchor.Id;
                    anchorProto.Pose = ProtoUtils.ToProto(anchor.Pose, anchorProto.Pose);
                }
            }
        }

        public void AddBrushStroke(BrushStrokeProto brushStrokeProto)
        {
            lock (_lock)
            {
                _brushStrokesToUpload.AddLast(brushStrokeProto);
            }
        }

        public void RemoveBrushStroke(string id, string anchorId)
        {
            lock (_lock)
            {
                _brushStrokesToRemove.AddLast(
                    new BrushStrokeRemoveRequest
                    {
                        Id = id,
                        AnchorId = anchorId
                    });
            }
        }

        public void UpdateExternalModel(string modelId, string anchorId, string modelFileName,
            TransformProto transform)
        {
            lock (_lock)
            {
                ExternalModelProto modelProto;
                if (!_externalModelsToUpdate.TryGetValue(modelId, out modelProto))
                {
                    modelProto = new ExternalModelProto()
                    {
                        Id = modelId,
                        AnchorId = anchorId,
                        FileName = modelFileName,
                    };
                    _externalModelsToUpdate[modelId] = modelProto;
                }

                modelProto.ModifiedByUserName = _userName;
                modelProto.Transform = transform;
            }
        }

        public void RemoveExternalModel(string modelId, string anchorId)
        {
            lock (_lock)
            {
                _externalModelsToRemove.AddLast(
                    new ExternalModelRemoveRequest() {Id = modelId, AnchorId = anchorId});
            }
        }

        private void Run()
        {
            try
            {
                while (!_shutDownTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        RunUpdateDeviceLoop();
                    }
                    catch (RpcException e)
                    {
                        lock (_lock)
                        {
                            if (_lastUploadOk)
                            {
                                Debug.LogWarning("UpdateDevice started failing: " + e);
                            }

                            _lastUploadOk = false;
                        }
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }

                Debug.Log("Upload thread: Shutting down");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void RunUpdateDeviceLoop()
        {
            LeapBrushApiBase.UpdateDeviceStream updateDeviceStream =
                _leapBrushClient.UpdateDeviceStream(_shutDownTokenSource.Token);

            UserStateProto userState = new UserStateProto();
            userState.UserName = _userName;

#if UNITY_ANDROID && !UNITY_EDITOR
            userState.DeviceType = UserStateProto.Types.DeviceType.MagicLeap;
#else
            userState.DeviceType = UserStateProto.Types.DeviceType.DesktopSpectator;
#endif

            SpaceInfoProto spaceInfo = new SpaceInfoProto();

            UpdateDeviceRequest updateRequest = new UpdateDeviceRequest();
            updateRequest.UserState = userState;

            DateTimeOffset lastUpdateTime = DateTimeOffset.MinValue;
            while (!_shutDownTokenSource.IsCancellationRequested)
            {
                TimeSpan sleepTime = (lastUpdateTime + TimeSpan.FromSeconds(MinServerUpdateIntervalSeconds)
                                      - DateTimeOffset.Now);
                if (sleepTime.Milliseconds > 0)
                {
                    Thread.Sleep(sleepTime);
                }

                bool sendUpdate;

                updateRequest.SpaceInfo = null;
                updateRequest.BrushStrokeAdd = null;
                updateRequest.BrushStrokeRemove = null;
                updateRequest.ExternalModelAdd = null;
                updateRequest.ExternalModelRemove = null;

                lock (_lock)
                {
                    sendUpdate = !_lastUploadOk || lastUpdateTime
                        + TimeSpan.FromSeconds(ServerPingIntervalSeconds) < DateTimeOffset.Now;

                    if (UpdateUserStateGetWasChangedLocked(userState))
                    {
                        sendUpdate = true;
                    }

                    if (!_lastUploadOk || UpdateSpaceInfoGetWasChanged(spaceInfo, _spaceInfo))
                    {
                        sendUpdate = true;
                        updateRequest.SpaceInfo = spaceInfo;
                    }

                    if (updateRequest.Echo != _serverEchoEnabled)
                    {
                        sendUpdate = true;
                        updateRequest.Echo = _serverEchoEnabled;
                    }

                    int maxPendingBrushPoses = 0;
                    foreach (var entry in _currentBrushStrokes)
                    {
                        if (entry.Value != null)
                        {
                            maxPendingBrushPoses = Math.Max(
                                maxPendingBrushPoses, entry.Value.BrushPose.Count);
                        }
                    }

                    // Send the pending brush stroke that has the largest number of
                    // pending brush poses present. This will ensure each upload cycle picks
                    // off the largest pending brush avoiding starvation.
                    foreach (var entry in _currentBrushStrokes)
                    {
                        if (entry.Value == null
                            || entry.Value.BrushPose.Count < maxPendingBrushPoses)
                        {
                            continue;
                        }

                        sendUpdate = true;

                        BrushStrokeProto brushStrokeProto = new();
                        if (entry.Value.StartIndex == 0)
                        {
                            brushStrokeProto.MergeFrom(entry.Value);
                        }
                        else
                        {
                            brushStrokeProto.Id = entry.Value.Id;
                            brushStrokeProto.AnchorId = entry.Value.AnchorId;
                            brushStrokeProto.StartIndex = entry.Value.StartIndex;
                            brushStrokeProto.BrushPose.AddRange(entry.Value.BrushPose);
                        }

                        updateRequest.BrushStrokeAdd = new BrushStrokeAddRequest {BrushStroke = brushStrokeProto};
                        entry.Value.StartIndex += entry.Value.BrushPose.Count;
                        entry.Value.BrushPose.Clear();
                        break;
                    }

                    if (updateRequest.BrushStrokeAdd == null && _brushStrokesToUpload.Count > 0)
                    {
                        sendUpdate = true;
                        updateRequest.BrushStrokeAdd = new BrushStrokeAddRequest {BrushStroke = _brushStrokesToUpload.First.Value};
                        _brushStrokesToUpload.RemoveFirst();
                    }

                    if (_brushStrokesToRemove.Count > 0)
                    {
                        sendUpdate = true;
                        updateRequest.BrushStrokeRemove = _brushStrokesToRemove.First.Value;
                        _brushStrokesToRemove.RemoveFirst();
                    }

                    if (_externalModelsToUpdate.Count > 0)
                    {
                        sendUpdate = true;
                        string firstModelId = null;
                        foreach (string modelId in _externalModelsToUpdate.Keys)
                        {
                            firstModelId = modelId;
                            break;
                        }

                        updateRequest.ExternalModelAdd = new ExternalModelAddRequest()
                        {
                            Model = _externalModelsToUpdate[firstModelId]
                        };
                        _externalModelsToUpdate.Remove(firstModelId);
                    }

                    if (_externalModelsToRemove.Count > 0)
                    {
                        sendUpdate = true;
                        updateRequest.ExternalModelRemove = _externalModelsToRemove.First.Value;
                        _externalModelsToRemove.RemoveFirst();
                    }
                }

                if (!sendUpdate)
                {
                    continue;
                }

                lastUpdateTime = DateTimeOffset.Now;

                updateDeviceStream.Write(updateRequest, _shutDownTokenSource.Token);

                lock (_lock)
                {
                    if (!_lastUploadOk)
                    {
                        Debug.Log("UpdateDevice started succeeding");
                    }

                    _lastUploadOk = true;
                }
            }
        }

        private bool UpdateUserStateGetWasChangedLocked(UserStateProto userState)
        {
            if (userState.AnchorId.Length > 0 && _headClosestAnchorId == null)
            {
                userState.AnchorId = "";
                userState.HeadPose = null;
                userState.ControllerState = null;
                userState.LeftHandState = null;
                userState.RightHandState = null;
                return true;
            }
            if (userState.AnchorId.Length == 0 && _headClosestAnchorId == null)
            {
                return false;
            }

            bool changed = UpdateControllerStateGetWasChangedLocked(userState);
            changed = UpdateLeftHandStateGetWasChangedLocked(userState) || changed;
            changed = UpdateRightHandStateGetWasChangedLocked(userState) || changed;

            uint currentToolColorUint = ColorUtils.ToRgbaUint(_currentToolColor);
            if (!changed
                && userState.AnchorId == _headClosestAnchorId
                && ProtoUtils.EpsilonEquals(_headPoseRelativeToAnchor, userState.HeadPose)
                && _currentToolState == userState.ToolState
                && currentToolColorUint == userState.ToolColorRgb
                && _userDisplayName == userState.UserDisplayName
                && _batteryStatus.Equals(userState.HeadsetBattery))
            {
                return false;
            }

            userState.AnchorId = _headClosestAnchorId;
            userState.HeadPose = ProtoUtils.ToProto(_headPoseRelativeToAnchor, userState.HeadPose);
            userState.ToolState = _currentToolState;
            userState.ToolColorRgb = currentToolColorUint;
            userState.UserDisplayName = _userDisplayName;
            userState.HeadsetBattery = _batteryStatus.Clone();
            return true;
        }

        private bool UpdateControllerStateGetWasChangedLocked(UserStateProto userState)
        {
            if ((userState.ControllerState == null) != (_controllerState == null))
            {
                userState.ControllerState = _controllerState?.Clone();
                return true;
            }
            if (_controllerState == null)
            {
                return false;
            }

            if (ProtoUtils.EpsilonEquals(_controllerState.Pose, userState.ControllerState.Pose)
                && ProtoUtils.EpsilonEquals(_controllerState.ToolOffsetZ,
                    userState.ControllerState.ToolOffsetZ)
                && ProtoUtils.EpsilonEquals(
                    _controllerState.RayPoints, userState.ControllerState.RayPoints))
            {
                return false;
            }

            userState.ControllerState = _controllerState.Clone();
            return true;
        }

        private bool UpdateLeftHandStateGetWasChangedLocked(UserStateProto userState)
        {
            _handStateMap.TryGetValue(HandType.Left, out HandStateProto handState);

            if ((userState.LeftHandState == null) != (handState == null))
            {
                userState.LeftHandState = handState?.Clone();
                return true;
            }
            if (handState == null)
            {
                return false;
            }

            if (ProtoUtils.EpsilonEquals(handState.ToolPose, userState.LeftHandState.ToolPose)
                && ProtoUtils.EpsilonEquals(
                    handState.RayPoints, userState.LeftHandState.RayPoints))
            {
                return false;
            }

            userState.LeftHandState = handState.Clone();
            return true;
        }

        private bool UpdateRightHandStateGetWasChangedLocked(UserStateProto userState)
        {
            _handStateMap.TryGetValue(HandType.Right, out HandStateProto handState);

            if ((userState.RightHandState == null) != (handState == null))
            {
                userState.RightHandState = handState?.Clone();
                return true;
            }
            if (handState == null)
            {
                return false;
            }

            if (ProtoUtils.EpsilonEquals(handState.ToolPose, userState.RightHandState.ToolPose)
                && ProtoUtils.EpsilonEquals(
                    handState.RayPoints, userState.RightHandState.RayPoints))
            {
                return false;
            }

            userState.RightHandState = handState.Clone();
            return true;
        }

        private static bool UpdateSpaceInfoGetWasChanged(SpaceInfoProto toSpaceInfo,
            SpaceInfoProto fromSpaceInfo)
        {
            bool modified = false;

            if (toSpaceInfo.SpaceId != fromSpaceInfo.SpaceId ||
                toSpaceInfo.SpaceName != fromSpaceInfo.SpaceName ||
                toSpaceInfo.MappingMode != fromSpaceInfo.MappingMode)
            {
                modified = true;
                toSpaceInfo.SpaceId = fromSpaceInfo.SpaceId;
                toSpaceInfo.SpaceName = fromSpaceInfo.SpaceName;
                toSpaceInfo.MappingMode = fromSpaceInfo.MappingMode;
            }
            if (toSpaceInfo.TargetSpaceOrigin == null || !ProtoUtils.EpsilonEquals(
                    toSpaceInfo.TargetSpaceOrigin, fromSpaceInfo.TargetSpaceOrigin))
            {
                modified = true;
                toSpaceInfo.TargetSpaceOrigin = fromSpaceInfo.TargetSpaceOrigin;
            }

            if (toSpaceInfo.UsingImportedAnchors != fromSpaceInfo.UsingImportedAnchors)
            {
                modified = true;
                toSpaceInfo.UsingImportedAnchors = fromSpaceInfo.UsingImportedAnchors;
            }

            if (toSpaceInfo.Anchor.Count > fromSpaceInfo.Anchor.Count)
            {
                modified = true;
                toSpaceInfo.Anchor.Clear();
            }

            for (int i = 0; i < fromSpaceInfo.Anchor.Count; ++i)
            {
                AnchorProto fromAnchor = fromSpaceInfo.Anchor[i];
                if (i >= toSpaceInfo.Anchor.Count)
                {
                    modified = true;
                    toSpaceInfo.Anchor.Add(new AnchorProto());
                }
                AnchorProto toAnchor = toSpaceInfo.Anchor[i];
                if (fromAnchor.Id != toAnchor.Id)
                {
                    modified = true;
                    toAnchor.Id = fromAnchor.Id;
                }

                if (toAnchor.Pose == null
                    || !ProtoUtils.EpsilonEquals(toAnchor.Pose, fromAnchor.Pose))
                {
                    modified = true;
                    toAnchor.Pose = fromAnchor.Pose;
                }
            }

            return modified;
        }
    }
}