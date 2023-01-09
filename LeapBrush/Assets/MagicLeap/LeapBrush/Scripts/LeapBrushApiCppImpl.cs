using Google.Protobuf;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Grpc.Core;

namespace MagicLeap.LeapBrush
{
    public class LeapBrushApiCppImpl : LeapBrushApiBase
    {
        internal class Native
        {
            private const string LeapBrushGrpcDll = "leapbrush_grpc";

            [StructLayout(LayoutKind.Sequential)]
            public struct ProtoBytesCpp : IDisposable
            {
                public IntPtr Bytes;

                public int Size;

                public void Dispose()
                {
                    LeapBrushApi_ProtoBytesDestroy(ref this);
                    Bytes = IntPtr.Zero;
                    Size = 0;
                }

                public byte[] ToByteArray()
                {
                    if (Bytes == IntPtr.Zero)
                    {
                        return null;
                    }

                    byte[] bytes = new byte[Size];
                    Marshal.Copy(Bytes, bytes, 0, bytes.Length);
                    return bytes;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct ProtoBytesCSharp : IDisposable
            {
                public IntPtr Bytes;

                public int Size;

                public ProtoBytesCSharp(Google.Protobuf.IMessage message)
                {
                    byte[] bytes = message.ToByteArray();
                    Bytes = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.Copy(bytes, 0, Bytes, bytes.Length);
                    Size = bytes.Length;
                }

                public void Dispose()
                {
                    Marshal.FreeHGlobal(Bytes);
                    Bytes = IntPtr.Zero;
                }
            }

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            public static extern void LeapBrushApi_ProtoBytesDestroy(
                ref ProtoBytesCpp protoBytes);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            public static extern void LeapBrushApi_Client_Connect(
                [MarshalAs(UnmanagedType.LPStr)] string address, out ulong clientHandle);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            [return:MarshalAs(UnmanagedType.I1)]
            public static extern bool LeapBrushApi_Client_UpdateDeviceStream(ulong clientHandle, out ulong streamHandle);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            [return:MarshalAs(UnmanagedType.I1)]
            public static extern bool LeapBrushApi_UpdateDeviceStream_Write(ulong streamHandle, ref ProtoBytesCSharp req);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            [return:MarshalAs(UnmanagedType.I1)]
            public static extern bool LeapBrushApi_UpdateDeviceStream_GetNext(ulong streamHandle, ref ProtoBytesCpp resp);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            [return:MarshalAs(UnmanagedType.I1)]
            public static extern bool LeapBrushApi_Client_RegisterAndListen(ulong clientHandle, ref ProtoBytesCSharp req, out ulong streamHandle);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            [return:MarshalAs(UnmanagedType.I1)]
            public static extern bool LeapBrushApi_ServerStateStream_GetNext(ulong streamHandle, ref ProtoBytesCpp resp);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            [return:MarshalAs(UnmanagedType.I1)]
            public static extern bool LeapBrushApi_Client_Rpc(ulong clientHandle, ref ProtoBytesCSharp req, ref ProtoBytesCpp resp);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            public static extern void LeapBrushApi_UpdateDeviceStreamDestroy(ulong streamHandle);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            public static extern void LeapBrushApi_ServerStateStreamDestroy(ulong streamHandle);

            [DllImport(LeapBrushGrpcDll, CallingConvention = CallingConvention.Cdecl)]
            public static extern void LeapBrushApi_Client_CloseAndWait(ulong clientHandle);
        }

        private class LeapBrushClientCpp : LeapBrushClient
        {
            private ulong _clientHandle;

            public LeapBrushClientCpp(string serverUrl)
            {
                Native.LeapBrushApi_Client_Connect(serverUrl, out _clientHandle);
            }

            public override UpdateDeviceStream UpdateDeviceStream()
            {
                ulong streamHandle;
                if (!Native.LeapBrushApi_Client_UpdateDeviceStream(_clientHandle, out streamHandle))
                {
                    throw new RpcException(new Status(StatusCode.Unknown, "LeapBrushApi_Client_UpdateDeviceStream failed"));
                }

                return new UpdateDeviceStreamCpp(streamHandle);
            }

            public override ServerStateStream RegisterAndListen(RegisterDeviceRequest request)
            {
                Native.ProtoBytesCSharp reqData = new Native.ProtoBytesCSharp(request);
                ulong streamHandle;
                if (!Native.LeapBrushApi_Client_RegisterAndListen(_clientHandle, ref reqData, out streamHandle))
                {
                    reqData.Dispose();
                    throw new RpcException(new Status(StatusCode.Unknown, "LeapBrushApi_Client_RegisterAndListen failed"));
                }
                reqData.Dispose();

                return new ServerStateStreamCpp(streamHandle);
            }

            public override RpcResponse Rpc(RpcRequest request)
            {
                Native.ProtoBytesCSharp reqData = new Native.ProtoBytesCSharp(request);
                Native.ProtoBytesCpp respData = new Native.ProtoBytesCpp();
                if (!Native.LeapBrushApi_Client_Rpc(_clientHandle, ref reqData, ref respData))
                {
                    reqData.Dispose();
                    respData.Dispose();
                    throw new RpcException(new Status(StatusCode.Unknown, "LeapBrushApi_Client_Rpc failed"));
                }
                reqData.Dispose();

                RpcResponse resp = new RpcResponse();
                resp.MergeFrom(respData.ToByteArray());
                respData.Dispose();
                return resp;
            }

            public override void CloseAndWait()
            {
                Native.LeapBrushApi_Client_CloseAndWait(_clientHandle);
            }
        }

        private class UpdateDeviceStreamCpp : UpdateDeviceStream
        {
            private readonly ulong _streamHandle;

            public UpdateDeviceStreamCpp(ulong streamHandle)
            {
                _streamHandle = streamHandle;
            }

            public override void Write(UpdateDeviceRequest request)
            {
                Native.ProtoBytesCSharp reqData = new Native.ProtoBytesCSharp(request);
                if (!Native.LeapBrushApi_UpdateDeviceStream_Write(_streamHandle, ref reqData))
                {
                    reqData.Dispose();
                    throw new RpcException(new Status(StatusCode.Unknown, "LeapBrushApi_UpdateDeviceStream_Write failed"));
                }
                reqData.Dispose();
            }

            public override void Dispose()
            {
                Native.LeapBrushApi_UpdateDeviceStreamDestroy(_streamHandle);
            }
        }

        private class ServerStateStreamCpp : ServerStateStream
        {
            private readonly ulong _streamHandle;

            public ServerStateStreamCpp(ulong streamHandle)
            {
                _streamHandle = streamHandle;
            }

            public override ServerStateResponse GetNext(CancellationToken cancellationToken)
            {
                Native.ProtoBytesCpp respData = new Native.ProtoBytesCpp();
                if (!Native.LeapBrushApi_ServerStateStream_GetNext(_streamHandle, ref respData))
                {
                    respData.Dispose();
                    throw new RpcException(new Status(StatusCode.Unknown, "LeapBrushApi_ServerStateStream_GetNext failed"));
                }

                ServerStateResponse resp = new ServerStateResponse();
                resp.MergeFrom(respData.ToByteArray());
                respData.Dispose();
                return resp;
            }

            public override void Dispose()
            {
                Native.LeapBrushApi_ServerStateStreamDestroy(_streamHandle);
            }
        }

        public override LeapBrushClient Connect(string serverUrl)
        {
            return new LeapBrushClientCpp(serverUrl);
        }
    }
}