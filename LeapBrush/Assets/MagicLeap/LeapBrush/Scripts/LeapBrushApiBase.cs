using System;
using System.Threading;

namespace MagicLeap.LeapBrush
{
    public abstract class LeapBrushApiBase
    {
        public abstract class LeapBrushClient
        {
            public abstract UpdateDeviceStream UpdateDeviceStream();

            public abstract ServerStateStream RegisterAndListen(RegisterDeviceRequest request);

            public abstract RpcResponse Rpc(RpcRequest request);

            public abstract void CloseAndWait();
        }

        public abstract class UpdateDeviceStream : IDisposable
        {
            public abstract void Write(UpdateDeviceRequest request);

            public abstract void Dispose();
        }

        public abstract class ServerStateStream : IDisposable
        {
            public abstract ServerStateResponse GetNext(CancellationToken cancellationToken);

            public abstract void Dispose();
        }
    }
}
