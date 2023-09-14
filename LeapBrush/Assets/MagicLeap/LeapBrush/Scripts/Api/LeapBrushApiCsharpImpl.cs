using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Implementation of the LeapBrushApi for use against a grpc server using Grpc.Core.
    /// </summary>
    public class LeapBrushApiCsharpImpl : LeapBrushApiBase
    {
        static LeapBrushApiCsharpImpl()
        {
#if !UNITY_EDITOR
            // TODO(ghazen): Determine why the dll for grpc is having trouble loading
            // in the normal way via unity plugin
            LoadGrpcLibrary();
#endif
        }

        private class LeapBrushClientCSharp : LeapBrushClient
        {
            private readonly Channel _channel;
            private readonly LeapBrushApi.LeapBrushApiClient _client;

            public LeapBrushClientCSharp(Channel channel)
            {
                _channel = channel;
                _client = new LeapBrushApi.LeapBrushApiClient(channel);
            }

            public override UpdateDeviceStream UpdateDeviceStream(
                CancellationToken cancellationToken)
            {
                return new UpdateDeviceStreamCsharp(_client, cancellationToken);
            }

            public override ServerStateStream RegisterAndListen(RegisterDeviceRequest request,
                CancellationToken cancellationToken)
            {
                return new ServerStateStreamCsharp(
                    _client.RegisterAndListen(request, cancellationToken:cancellationToken));
            }

            public override RpcResponse Rpc(RpcRequest request)
            {
                return _client.Rpc(request);
            }

            public override void CloseAndWait()
            {
                _channel.ShutdownAsync().Wait();
            }
        }

        private class UpdateDeviceStreamCsharp : UpdateDeviceStream
        {
            private readonly LeapBrushApi.LeapBrushApiClient _client;
            private readonly AsyncClientStreamingCall<UpdateDeviceRequest, UpdateDeviceResponse> _stream;

            public UpdateDeviceStreamCsharp(LeapBrushApi.LeapBrushApiClient client,
                CancellationToken cancellationToken)
            {
                _client = client;
                _stream = _client.UpdateDeviceStream(cancellationToken:cancellationToken);
            }

            public override void Write(UpdateDeviceRequest request,
                CancellationToken cancellationToken)
            {
                try
                {
                    Task task = _stream.RequestStream.WriteAsync(request);
                    task.Wait(cancellationToken);
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        if (e is RpcException)
                        {
                            throw e;
                        }
                    }

                    throw ae;
                }
            }

            public override void Dispose()
            {
                _stream.Dispose();
            }
        }

        private class ServerStateStreamCsharp : ServerStateStream
        {
            private readonly AsyncServerStreamingCall<ServerStateResponse> _stream;

            public ServerStateStreamCsharp(AsyncServerStreamingCall<ServerStateResponse> stream)
            {
                _stream = stream;
            }

            public override ServerStateResponse GetNext(CancellationToken cancellationToken)
            {
                try
                {
                    Task<bool> task = _stream.ResponseStream.MoveNext(cancellationToken);
                    task.Wait();
                    return _stream.ResponseStream.Current.Clone();
                }
                catch (AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        if (e is RpcException)
                        {
                            throw e;
                        }
                    }

                    throw ae;
                }
            }

            public override void Dispose()
            {
                _stream.Dispose();
            }
        }

        public LeapBrushClient Connect(string serverUrl)
        {
            Debug.LogFormat("Connecting to {0}...", serverUrl);

            bool isSecure = false;
            if (serverUrl.StartsWith("ssl://"))
            {
                serverUrl = serverUrl.Substring(6);
                isSecure = true;
            }

            return new LeapBrushClientCSharp(new Channel(
                serverUrl, isSecure ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure));
        }

        static void LoadGrpcLibrary()
        {
            // TODO(ghazen): Figure out why the grpc plugin in searching in unexpected locations
            // to load this dll. For now, simply copy the dll to the right place
#if UNITY_STANDALONE_WIN
            string expectedDllPath = Path.Join(Application.dataPath,
                "Managed\\runtimes\\win-x64\\native\\grpc_csharp_ext.x64.dll");
            if (!File.Exists(expectedDllPath))
            {
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Managed"));
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Managed\\runtimes"));
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Managed\\runtimes\\win-x64"));
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Managed\\runtimes\\win-x64\\native"));
                File.Copy(Path.Join(Application.dataPath,
                        "Plugins\\x86_64\\grpc_csharp_ext.x64.dll"), expectedDllPath);
            }
#elif UNITY_STANDALONE_OSX
            string expectedDllPath = Path.Join(Application.dataPath,
                "Resources/Data/Managed/libgrpc_csharp_ext.x64.dylib");
            if (!File.Exists(expectedDllPath))
            {
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Resources"));
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Resources/Data"));
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Resources/Data/Managed"));
                File.Copy(Path.Join(Application.dataPath,
                    "PlugIns/libgrpc_csharp_ext.x64.dylib"), expectedDllPath);
            }
#elif UNITY_STANDALONE_LINUX
            string expectedDllPath = Path.Join(Application.dataPath,
                "Managed/libgrpc_csharp_ext.x64.so");
            if (!File.Exists(expectedDllPath))
            {
                Directory.CreateDirectory(
                    Path.Join(Application.dataPath, "Managed"));
                File.Copy(Path.Join(Application.dataPath,
                        "Plugins/libgrpc_csharp_ext.x64.so"), expectedDllPath);
            }
#endif
        }
    }
}
