using System;
using System.Threading;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Base class for LeapBrushApi implementations.
    /// </summary>
    public abstract class LeapBrushApiBase
    {
        public abstract class LeapBrushClient
        {
            public abstract UpdateDeviceStream UpdateDeviceStream(
                CancellationToken cancellationToken);

            public abstract ServerStateStream RegisterAndListen(RegisterDeviceRequest request,
                CancellationToken cancellationToken);

            public abstract RpcResponse Rpc(RpcRequest request);

            public abstract void CloseAndWait();
        }

        public abstract class UpdateDeviceStream : IDisposable
        {
            public abstract void Write(UpdateDeviceRequest request,
                CancellationToken cancellationToken);

            public abstract void Dispose();
        }

        public abstract class ServerStateStream : IDisposable
        {
            public abstract ServerStateResponse GetNext(CancellationToken cancellationToken);

            public abstract void Dispose();
        }
    }
}
