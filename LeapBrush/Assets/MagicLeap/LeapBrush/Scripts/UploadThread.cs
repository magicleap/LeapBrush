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

        private const float MinServerUpdateIntervalSeconds = .03f;
        private const float ServerPingIntervalSeconds = 2.0f;

        private readonly Thread _thread;
        private readonly LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private readonly CancellationTokenSource _shutDownTokenSource;
        private readonly string _userName;

        private readonly object _lock = new();
        private bool _lastUploadOk;
        private string _headClosestAnchorId;
        private Pose _controlPoseRelativeToAnchor = Pose.identity;
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
        private BrushStrokeProto _currentBrushStroke;

        public object Lock => _lock;
        public BrushStrokeProto CurrentBrushStroke
        {
            get => _currentBrushStroke;
            set => _currentBrushStroke = value;
        }

        public UploadThread(LeapBrushApiBase.LeapBrushClient leapBrushClient,
            CancellationTokenSource shutDownTokenSource,
            string userName)
        {
            _thread = new Thread(Run);
            _leapBrushClient = leapBrushClient;
            _shutDownTokenSource = shutDownTokenSource;
            _userName = userName;
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

        public void SetControlPoseRelativeToAnchor(Pose pose)
        {
            lock (_lock)
            {
                _controlPoseRelativeToAnchor = pose;
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

        public void SetSpaceInfo(AnchorsApi.LocalizationInfo localizationInfo,
            bool isUsingImportedAnchors, AnchorsApi.Anchor[] anchors)
        {
            lock (_lock)
            {
                _spaceInfo.SpaceId = localizationInfo.SpaceId ?? "";
                _spaceInfo.SpaceName = localizationInfo.SpaceName ?? "";
                _spaceInfo.MappingMode = ProtoUtils.ToProto(localizationInfo.MappingMode);
                _spaceInfo.TargetSpaceOrigin = ProtoUtils.ToProto(
                    localizationInfo.TargetSpaceOriginPose);
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
                    anchorProto.Pose = ProtoUtils.ToProto(anchor.Pose);
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

                        if (UpdateUserStateGetWasChanged(userState, _userDisplayName, _headClosestAnchorId,
                                _headPoseRelativeToAnchor, _controlPoseRelativeToAnchor,
                                _currentToolState, _currentToolColor, _batteryStatus))
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

                        if (_currentBrushStroke != null && _currentBrushStroke.BrushPose.Count > 0)
                        {
                            sendUpdate = true;
                            BrushStrokeProto brushStrokeProto = new();
                            if (_currentBrushStroke.StartIndex == 0)
                            {
                                brushStrokeProto.MergeFrom(_currentBrushStroke);
                            }
                            else
                            {
                                brushStrokeProto.Id = _currentBrushStroke.Id;
                                brushStrokeProto.AnchorId = _currentBrushStroke.AnchorId;
                                brushStrokeProto.StartIndex = _currentBrushStroke.StartIndex;
                                brushStrokeProto.BrushPose.AddRange(_currentBrushStroke.BrushPose);
                            }

                            updateRequest.BrushStrokeAdd = new BrushStrokeAddRequest {BrushStroke = brushStrokeProto};
                            _currentBrushStroke.StartIndex += _currentBrushStroke.BrushPose.Count;
                            _currentBrushStroke.BrushPose.Clear();
                        }
                        else if (_brushStrokesToUpload.Count > 0)
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

                    try
                    {
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
                }

                Debug.Log("Upload thread: Shutting down");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static bool UpdateUserStateGetWasChanged(UserStateProto userState,
            string userDisplayName, string headClosestAnchorId, Pose headPoseRelativeToAnchor,
            Pose controlPoseRelativeToAnchor, UserStateProto.Types.ToolState currentToolState,
            Color32 currentToolColor, BatteryStatusProto batteryStatus)
        {
            if (userState.AnchorId.Length > 0 && headClosestAnchorId == null)
            {
                userState.AnchorId = "";
                userState.HeadPose = null;
                userState.ControlPose = null;
                return true;
            }
            if (userState.AnchorId.Length == 0 && headClosestAnchorId != null)
            {
                userState.AnchorId = headClosestAnchorId;
                userState.HeadPose = ProtoUtils.ToProto(headPoseRelativeToAnchor);
                userState.ControlPose = ProtoUtils.ToProto(controlPoseRelativeToAnchor);
                return true;
            }
            if (userState.AnchorId.Length == 0 && headClosestAnchorId == null)
            {
                return false;
            }

            uint currentToolColorUint = ColorUtils.ToRgbaUint(currentToolColor);

            if (ProtoUtils.EpsilonEquals(headPoseRelativeToAnchor, userState.HeadPose)
                && ProtoUtils.EpsilonEquals(controlPoseRelativeToAnchor, userState.ControlPose)
                && currentToolState == userState.ToolState
                && currentToolColorUint == userState.ToolColorRgb
                && userDisplayName == userState.UserDisplayName
                && batteryStatus.Equals(userState.HeadsetBattery))
            {
                return false;
            }

            userState.AnchorId = headClosestAnchorId;
            userState.HeadPose = ProtoUtils.ToProto(headPoseRelativeToAnchor);
            userState.ControlPose = ProtoUtils.ToProto(controlPoseRelativeToAnchor);
            userState.ToolState = currentToolState;
            userState.ToolColorRgb = currentToolColorUint;
            userState.UserDisplayName = userDisplayName;
            userState.HeadsetBattery = batteryStatus.Clone();
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