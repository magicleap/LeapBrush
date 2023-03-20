using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Google.Protobuf;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Implementation of the leap brush api for use against a local on-device database.
    /// </summary>
    public class LeapBrushApiOnDevice : LeapBrushApiBase
    {
        private static readonly Regex ValidIdRegex = new (@"^[a-zA-Z0-9_-]+$");
        private static readonly TimeSpan UpdateDeviceRateLimitDelay = TimeSpan.FromMilliseconds(100);
        private static readonly int ServerStateStreamRateLimitDelayMs = 16;

        private readonly Database _db;

        public LeapBrushApiOnDevice(string persistentDataPath)
        {
            _db = new Database(persistentDataPath);
        }

        private class Session
        {
            public SpaceInfoProto SpaceInfo;
            public bool AnchorsChanged;
        }

        private class Database
        {
            private const string BRUSH_FILENAME_PREFIX = "brush-";
            private const string EXTERNAL_MODEL_FILENAME_PREFIX = "model-";

            private string _dbDirectory;
            private bool _dbDirectoryCreated;

            public Database(string parentDir)
            {
                _dbDirectory = Path.Join(parentDir, "on-device-db");
            }

            public void GetContentForAnchor(string anchorId,
                out List<BrushStrokeProto> brushStrokes,
                out List<ExternalModelProto> externalModels)
            {
                brushStrokes = new();
                externalModels = new();

                string anchorDir = GetAnchorDir(anchorId, false);
                foreach (string path in Directory.GetFiles(anchorDir))
                {
                    string filename = Path.GetFileName(path);
                    if (filename.StartsWith(BRUSH_FILENAME_PREFIX))
                    {
                        try
                        {
                            using (var reader = new FileStream(path, FileMode.Open))
                            {
                                brushStrokes.Add(BrushStrokeProto.Parser.ParseFrom(reader));
                            }
                        }
                        catch (IOException e)
                        {
                            Debug.LogException(e);
                        }
                    }
                    if (filename.StartsWith(EXTERNAL_MODEL_FILENAME_PREFIX))
                    {
                        try
                        {
                            using (var reader = new FileStream(path, FileMode.Open))
                            {
                                externalModels.Add(ExternalModelProto.Parser.ParseFrom(reader));
                            }
                        }
                        catch (IOException e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            public void AddBrushStroke(BrushStrokeAddRequest request)
            {
                BrushStrokeProto updated = null;

                if (request.BrushStroke.StartIndex > 0)
                {
                    updated = LoadBrushStroke(
                        request.BrushStroke.AnchorId, request.BrushStroke.Id);
                    if (updated != null)
                    {
                        while (updated.BrushPose.Count > request.BrushStroke.StartIndex)
                        {
                            updated.BrushPose.RemoveAt(updated.BrushPose.Count - 1);
                        }

                        updated.BrushPose.AddRange(request.BrushStroke.BrushPose);
                    }
                }

                if (updated == null)
                {
                    updated = request.BrushStroke;
                }

                SaveBrushStroke(updated);
            }

            public void RemoveBrushStroke(BrushStrokeRemoveRequest request)
            {
                string path = GetBrushStrokePath(request.AnchorId, request.Id, false);
                try
                {
                    File.Delete(path);
                }
                catch (IOException e)
                {
                    Debug.LogException(e);
                }
            }

            public void AddExternalModel(ExternalModelAddRequest request)
            {
                try
                {
                    string path = GetExternalModelPath(request.Model.AnchorId, request.Model.Id, true);
                    using (var writer = new FileStream(path, FileMode.Create))
                    {
                        request.Model.WriteTo(writer);
                    }
                }
                catch (IOException e)
                {
                    Debug.LogException(e);
                }
            }

            public void RemoveExternalModel(ExternalModelRemoveRequest request)
            {
                string path = GetExternalModelPath(request.AnchorId, request.Id, false);
                try
                {
                    File.Delete(path);
                }
                catch (IOException e)
                {
                    Debug.LogException(e);
                }
            }

            private string GetAnchorDir(string anchorId, bool ensureDirsCreated)
            {
                if (ensureDirsCreated)
                {
                    EnsureDbDirectoryCreated();
                    CheckIdSafeForFilesystemPath(anchorId);
                }
                string anchorDir = Path.Join(_dbDirectory, "anchor-" + anchorId);
                if (ensureDirsCreated && !Directory.Exists(anchorDir))
                {
                    Directory.CreateDirectory(anchorDir);
                }

                return anchorDir;
            }

            private void CheckIdSafeForFilesystemPath(string id)
            {
                if (!ValidIdRegex.IsMatch(id))
                {
                    throw new Exception("ID not safe for database usage: " + id);
                }
            }

            private void EnsureDbDirectoryCreated()
            {
                if (_dbDirectoryCreated)
                {
                    return;
                }

                if (!Directory.Exists(_dbDirectory))
                {
                    Directory.CreateDirectory(_dbDirectory);
                }

                _dbDirectoryCreated = true;
            }

            private string GetBrushStrokePath(string anchorId, string id, bool ensureDirsCreated)
            {
                string anchorDir = GetAnchorDir(anchorId, ensureDirsCreated);
                CheckIdSafeForFilesystemPath(id);
                return Path.Join(anchorDir,
                    BRUSH_FILENAME_PREFIX + id + ".pbdata");
            }

            private string GetExternalModelPath(string anchorId, string id, bool ensureDirsCreated)
            {
                string anchorDir = GetAnchorDir(anchorId, ensureDirsCreated);
                CheckIdSafeForFilesystemPath(id);
                return Path.Join(anchorDir,
                    EXTERNAL_MODEL_FILENAME_PREFIX + id + ".pbdata");
            }

            private void SaveBrushStroke(BrushStrokeProto brushStroke)
            {
                try
                {
                    string path = GetBrushStrokePath(brushStroke.AnchorId, brushStroke.Id, true);
                    using (var writer = new FileStream(path, FileMode.Create))
                    {
                        brushStroke.WriteTo(writer);
                    }
                }
                catch (IOException e)
                {
                    Debug.LogException(e);
                }
            }

            private BrushStrokeProto LoadBrushStroke(string anchorId, string id)
            {
                try
                {
                    using (var reader = new FileStream(
                               GetBrushStrokePath(anchorId, id, false), FileMode.Open))
                    {
                        return BrushStrokeProto.Parser.ParseFrom(reader);
                    }
                }
                catch (IOException e)
                {
                    Debug.LogException(e);
                    return null;
                }
            }
        }

        private class LeapBrushClientOnDevice : LeapBrushClient
        {
            private Database _db;
            private readonly Session _session = new();
            private AutoResetEvent _signalServerStateStream = new(false);

            public LeapBrushClientOnDevice(Database db)
            {
                _db = db;
            }

            public override UpdateDeviceStream UpdateDeviceStream(
                CancellationToken cancellationToken)
            {
                return new UpdateDeviceStreamOnDevice(_db, _session, _signalServerStateStream);
            }

            public override ServerStateStream RegisterAndListen(RegisterDeviceRequest request,
                CancellationToken cancellationToken)
            {
                return new ServerStateStreamOnDevice(_db, _session, _signalServerStateStream);
            }

            public override RpcResponse Rpc(RpcRequest request)
            {
                RpcResponse resp = new();
                if (request.QueryUsersRequest != null)
                {
                    resp.QueryUsersResponse = new();
                }

                return resp;
            }

            public override void CloseAndWait()
            {
            }
        }

        private class UpdateDeviceStreamOnDevice : UpdateDeviceStream
        {
            private readonly Database _db;
            private readonly Session _session;
            private readonly AutoResetEvent _signalServerStateStream;

            public UpdateDeviceStreamOnDevice(Database db, Session session,
                AutoResetEvent signalServerStateStream)
            {
                _db = db;
                _session = session;
                _signalServerStateStream = signalServerStateStream;
            }

            public override void Write(UpdateDeviceRequest request,
                CancellationToken cancellationToken)
            {
                if (request.BrushStrokeAdd != null)
                {
                    _db.AddBrushStroke(request.BrushStrokeAdd);
                }

                if (request.BrushStrokeRemove != null)
                {
                    _db.RemoveBrushStroke(request.BrushStrokeRemove);
                }

                if (request.ExternalModelAdd != null)
                {
                    _db.AddExternalModel(request.ExternalModelAdd);
                }

                if (request.ExternalModelRemove != null)
                {
                    _db.RemoveExternalModel(request.ExternalModelRemove);
                }

                bool anchorsChanged = false;
                lock (_session)
                {
                    if (request.SpaceInfo != null)
                    {
                        if (!AnchorIdsAreEqual(
                                _session.SpaceInfo, request.SpaceInfo))
                        {
                            anchorsChanged = _session.AnchorsChanged = true;
                        }
                        _session.SpaceInfo = request.SpaceInfo.Clone();
                    }
                }

                if (anchorsChanged)
                {
                    _signalServerStateStream.Set();
                }

                // Sleep for a rate limit delay to avoid too many writes to the database.
                Thread.Sleep(UpdateDeviceRateLimitDelay);
            }

            private bool AnchorIdsAreEqual(SpaceInfoProto spaceInfo1, SpaceInfoProto spaceInfo2)
            {
                if ((spaceInfo1 == null) != (spaceInfo2 == null))
                {
                    return false;
                }

                if (spaceInfo1 == null)
                {
                    return true;
                }

                if (spaceInfo1.Anchor.Count != spaceInfo2.Anchor.Count)
                {
                    return false;
                }

                for (int i = 0; i < spaceInfo1.Anchor.Count; i++)
                {
                    if (spaceInfo1.Anchor[i].Id != spaceInfo2.Anchor[i].Id)
                    {
                        return false;
                    }
                }

                return true;
            }

            public override void Dispose()
            {
            }
        }

        private class ServerStateStreamOnDevice : ServerStateStream
        {
            private readonly Database _db;
            private readonly Session _session;
            private readonly AutoResetEvent _signalServerStateStream;
            private bool _sentServerInfo;
            private Queue<BrushStrokeProto> _brushStrokesToSend = new();
            private Queue<ExternalModelProto> _externalModelsToSend = new();

            public ServerStateStreamOnDevice(Database db, Session session,
                AutoResetEvent signalServerStateStream)
            {
                _db = db;
                _session = session;
                _signalServerStateStream = signalServerStateStream;
            }

            public override ServerStateResponse GetNext(CancellationToken cancellationToken)
            {
                ServerStateResponse resp = new();

                if (!_sentServerInfo)
                {
                    _sentServerInfo = true;

                    resp.ServerInfo = new()
                    {
                        ServerVersion = "0",
                        MinAppVersion = "0"
                    };
                };

                WaitHandle.WaitAny(
                    new[] {_signalServerStateStream, cancellationToken.WaitHandle},
                    ServerStateStreamRateLimitDelayMs);
                if (!cancellationToken.IsCancellationRequested)
                {
                    AnchorProto[] anchorsToSend = null;
                    lock (_session)
                    {
                        if (_session.AnchorsChanged)
                        {
                            anchorsToSend = _session.SpaceInfo.Anchor.ToArray();
                            _session.AnchorsChanged = false;
                        }
                    }

                    if (anchorsToSend != null)
                    {
                        _brushStrokesToSend.Clear();
                        _externalModelsToSend.Clear();

                        foreach (AnchorProto anchor in anchorsToSend)
                        {
                            _db.GetContentForAnchor(anchor.Id,
                                out List<BrushStrokeProto> brushStrokes,
                                out List<ExternalModelProto> externalModels);
                            foreach (BrushStrokeProto brushStroke in brushStrokes)
                            {
                                _brushStrokesToSend.Enqueue(brushStroke);
                            }
                            foreach (ExternalModelProto externalModel in externalModels)
                            {
                                _externalModelsToSend.Enqueue(externalModel);
                            }
                        }
                    }

                    if (_brushStrokesToSend.TryDequeue(out BrushStrokeProto brushStrokeToSend))
                    {
                        resp.BrushStrokeAdd.Add(new BrushStrokeAddRequest
                        {
                            BrushStroke = brushStrokeToSend
                        });
                    }

                    if (_externalModelsToSend.TryDequeue(
                            out ExternalModelProto externalModelToSend))
                    {
                        resp.ExternalModelAdd.Add(new ExternalModelAddRequest()
                        {
                            Model = externalModelToSend
                        });
                    }
                }

                return resp;
            }

            public override void Dispose()
            {
            }
        }

        public LeapBrushClient Connect()
        {
            return new LeapBrushClientOnDevice(_db);
        }
    }
}