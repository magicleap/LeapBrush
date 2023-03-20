using System;
using System.Threading;
using Grpc.Core;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Thread for downloading updates from the server.
    /// </summary>
    /// <remarks>
    /// This thread runs for the duration of a connection to the server and fires events on
    /// the main application thread when state is received.
    /// </remarks>
    public class DownloadThread
    {
        public bool LastDownloadOk
        {
            get
            {
                lock (_lock)
                {
                    return _lastDownloadOk;
                }
            }
        }

        public event Action<ServerStateResponse> OnServerStateReceived;

        private readonly Thread _thread;
        private readonly LeapBrushApiBase.LeapBrushClient _leapBrushClient;
        private readonly CancellationTokenSource _shutDownTokenSource;
        private readonly string _userName;
        private readonly string _appVersion;

        private readonly object _lock = new();
        private bool _lastDownloadOk;

        public DownloadThread(LeapBrushApiBase.LeapBrushClient leapBrushClient,
            CancellationTokenSource shutDownTokenSource,
            string userName, string appVersion)
        {
            _thread = new Thread(Run);
            _leapBrushClient = leapBrushClient;
            _shutDownTokenSource = shutDownTokenSource;
            _userName = userName;
            _appVersion = appVersion;
        }

        public void Start() => _thread.Start();
        public void Join() => _thread.Join();

        private void Run()
        {
            try
            {
                while (!_shutDownTokenSource.IsCancellationRequested)
                {
                    RegisterDeviceRequest registerDeviceRequest = new RegisterDeviceRequest();
                    registerDeviceRequest.UserName = _userName;
                    registerDeviceRequest.AppVersion = _appVersion;

                    using var call = _leapBrushClient.RegisterAndListen(
                        registerDeviceRequest, _shutDownTokenSource.Token);

                    try
                    {
                        while (!_shutDownTokenSource.IsCancellationRequested)
                        {
                            ServerStateResponse resp = call.GetNext(_shutDownTokenSource.Token);
                            lock (_lock)
                            {
                                if (!_lastDownloadOk)
                                {
                                    Debug.Log("Downloads started succeeding");
                                    _lastDownloadOk = true;
                                }
                            }

                            ThreadDispatcher.ScheduleMain(
                                () => OnServerStateReceived?.Invoke(resp));
                        }
                    }
                    catch (RpcException e)
                    {
                        lock (_lock)
                        {
                            if (_lastDownloadOk)
                            {
                                Debug.LogWarning("Downloads started failing: " + e);
                                _lastDownloadOk = false;
                            }
                        }
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }

                Debug.Log("Download thread: Shutting down");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}