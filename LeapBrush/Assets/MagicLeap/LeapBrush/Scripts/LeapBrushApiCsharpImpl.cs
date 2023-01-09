using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    public class LeapBrushApiCsharpImpl : LeapBrushApiBase
    {
        private class LeapBrushClientCSharp : LeapBrushClient
        {
            private readonly GrpcChannel _channel;
            private readonly LeapBrushApi.LeapBrushApiClient _client;

            public LeapBrushClientCSharp(GrpcChannel channel)
            {
                _channel = channel;
                _client = new LeapBrushApi.LeapBrushApiClient(channel);
            }

            public override UpdateDeviceStream UpdateDeviceStream()
            {
                return new UpdateDeviceStreamCsharp(_client);
            }

            public override ServerStateStream RegisterAndListen(RegisterDeviceRequest request)
            {
                return new ServerStateStreamCsharp(_client.RegisterAndListen(request));
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

            public UpdateDeviceStreamCsharp(LeapBrushApi.LeapBrushApiClient client)
            {
                _client = client;
                _stream = _client.UpdateDeviceStream();
            }

            public override void Write(UpdateDeviceRequest request)
            {
                try
                {
                    Task task = _stream.RequestStream.WriteAsync(request);
                    task.Wait();
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

        public override LeapBrushClient Connect(string serverUrl)
        {
            UriBuilder remappedServerUrl = new UriBuilder(
                serverUrl.Contains("://") ? serverUrl : "http://" + serverUrl);
            if (remappedServerUrl.Scheme == "ssl")
            {
                remappedServerUrl.Scheme = "https";
            }
            if (remappedServerUrl.Port == 8402)
            {
                // The LeapBrush non-android client uses the web grpc port instead of the
                // normal grpc port.
                // TODO(ghazen): Fix the client code to always use normal grpc.
                remappedServerUrl.Port = 8401;
            }

            Debug.LogFormat("Connecting to {0} (remapped to {1} for grpc web)",
                serverUrl, remappedServerUrl.ToString());

            serverUrl = remappedServerUrl.ToString();

            return new LeapBrushClientCSharp(GrpcChannel.ForAddress(serverUrl,
                new GrpcChannelOptions
                {
                    HttpHandler = new GrpcWebHandler(new HttpClientHandler())
                }));
        }
    }
}