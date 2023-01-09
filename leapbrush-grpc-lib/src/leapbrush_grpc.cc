#include <grpc++/grpc++.h>
#include <android/log.h>
#include <unistd.h>

#define LOG_TAG "LeapBrushAPI"
#define ALOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__);

#include "leap_brush_api.grpc.pb.h"

using grpc::Channel;
using grpc::ClientContext;
using grpc::Server;
using grpc::ServerBuilder;
using grpc::ServerContext;
using grpc::Status;
using leapbrush::LeapBrushApi;
using leapbrush::RegisterDeviceRequest;
using leapbrush::RpcRequest;
using leapbrush::RpcResponse;
using leapbrush::ServerStateResponse;
using leapbrush::UpdateDeviceRequest;
using leapbrush::UpdateDeviceResponse;

namespace {

class UpdateDeviceStream {
public:
    UpdateDeviceStream(std::unique_ptr<ClientContext> context,
                       std::unique_ptr<grpc::ClientWriter<UpdateDeviceRequest>> stream)
            : context_{std::move(context)},
              stream_{std::move(stream)} {
    }

    grpc::ClientWriter<UpdateDeviceRequest>& getStream() {
        return *stream_;
    }

private:
    std::unique_ptr<ClientContext> context_;
    std::unique_ptr<grpc::ClientWriter<UpdateDeviceRequest>> stream_;
};

class ServerStateStream {
public:
    ServerStateStream(std::unique_ptr<ClientContext> context,
                      std::unique_ptr<grpc::ClientReader<ServerStateResponse>> stream)
            : context_{std::move(context)},
              stream_{std::move(stream)} {
    }

    grpc::ClientReader<ServerStateResponse>& getStream() {
        return *stream_;
    }

private:
    std::unique_ptr<ClientContext> context_;
    std::unique_ptr<grpc::ClientReader<ServerStateResponse>> stream_;
};

}

extern "C" {

typedef struct ProtoBytes {
    char *bytes;
    int size;
} ProtoBytes;

void LeapBrushApi_ProtoBytesDestroy(ProtoBytes* protoBytes) {
    if (protoBytes != nullptr && protoBytes->bytes != nullptr) {
        free(protoBytes->bytes);
        protoBytes->bytes = nullptr;
        protoBytes->size = 0;
    }
}

void LeapBrushApi_Client_Connect(char* address, uint64_t* out_clientHandle) {
    if (out_clientHandle == nullptr) {
        ALOGE("LeapBrushApi_Client_Connect: Invalid Parameters");
        return;
    }

    std::string addressStr{address};
    std::string hostPort;
    bool isSecure{false};
    if (addressStr.rfind("ssl://", 0) == 0) {
      hostPort = addressStr.substr(6);
      isSecure = true;
    } else {
      hostPort = addressStr;
    }

    std::shared_ptr<Channel> channel{
      grpc::CreateChannel(
        hostPort,
        isSecure ? grpc::SslCredentials(grpc::SslCredentialsOptions()) : grpc::InsecureChannelCredentials())};
    std::unique_ptr<LeapBrushApi::Stub> stub{LeapBrushApi::NewStub(channel)};

    *out_clientHandle = reinterpret_cast<uint64_t>(stub.release());
}

bool LeapBrushApi_Client_UpdateDevice(uint64_t clientHandle, ProtoBytes* reqBytes, ProtoBytes* respBytes) {
    auto* clientStub = reinterpret_cast<LeapBrushApi::Stub *>(clientHandle);
    if (clientStub == nullptr || reqBytes == nullptr || reqBytes->bytes == nullptr ||
        respBytes == nullptr) {
        ALOGE("LeapBrushApi_Client_UpdateDevice: Invalid Parameters");
        return false;
    }

    UpdateDeviceRequest req;
    req.ParseFromArray(reqBytes->bytes, reqBytes->size);
    UpdateDeviceResponse resp;

    ClientContext context;
    Status status = clientStub->UpdateDevice(&context, req, &resp);

    if (!status.ok()) {
        ALOGE("LeapBrushApi_Client_UpdateDevice Failed: %s", status.error_message().c_str());
        return false;
    }

    std::string respString = resp.SerializeAsString();
    respBytes->bytes = (char *) malloc(sizeof(char) * respString.size());
    memcpy(respBytes->bytes, respString.data(), respString.size());
    respBytes->size = respString.size();
    return true;
}

bool LeapBrushApi_Client_UpdateDeviceStream(uint64_t clientHandle, uint64_t* out_streamHandle) {
    auto* clientStub = reinterpret_cast<LeapBrushApi::Stub *>(clientHandle);
    if (clientStub == nullptr || out_streamHandle == nullptr) {
        ALOGE("LeapBrushApi_Client_UpdateDevice: Invalid Parameters");
        return false;
    }

    std::unique_ptr<ClientContext> context{new ClientContext()};
    std::unique_ptr<grpc::ClientWriter<UpdateDeviceRequest>> stream =
        clientStub->UpdateDeviceStream(context.get(), nullptr);

    if (stream == nullptr) {
        ALOGE("LeapBrushApi_Client_UpdateDeviceStream Failed: stream is nullptr");
        return false;
    }

    *out_streamHandle = reinterpret_cast<uint64_t>(
            new UpdateDeviceStream(std::move(context), std::move(stream)));
    return true;
}

bool LeapBrushApi_UpdateDeviceStream_Write(uint64_t streamHandle, ProtoBytes* reqBytes) {
    auto* stream = reinterpret_cast<UpdateDeviceStream *>(streamHandle);
    if (stream == nullptr || reqBytes == nullptr) {
        ALOGE("LeapBrushApi_UpdateDeviceStream_Write: Invalid params");
        return false;
    }

    UpdateDeviceRequest req;
    req.ParseFromArray(reqBytes->bytes, reqBytes->size);

    if (!stream->getStream().Write(req)) {
        ALOGE("LeapBrushApi_UpdateDeviceStream_Write: Read failed");
        return false;
    }

    return true;
}

bool LeapBrushApi_Client_RegisterAndListen(uint64_t clientHandle, ProtoBytes* reqBytes, uint64_t* out_streamHandle) {
    auto* clientStub = reinterpret_cast<LeapBrushApi::Stub *>(clientHandle);
    if (clientStub == nullptr || reqBytes == nullptr || reqBytes->bytes == nullptr ||
            out_streamHandle == nullptr) {
        ALOGE("LeapBrushApi_Client_RegisterAndListen: Invalid Parameters");
        return false;
    }

    RegisterDeviceRequest req;
    req.ParseFromArray(reqBytes->bytes, reqBytes->size);

    std::unique_ptr<ClientContext> context{new ClientContext()};
    std::unique_ptr<grpc::ClientReader<ServerStateResponse>> stream =
            clientStub->RegisterAndListen(context.get(), req);

    if (stream == nullptr) {
        ALOGE("LeapBrushApi_Client_RegisterAndListen Failed: stream is nullptr");
        return false;
    }

    *out_streamHandle = reinterpret_cast<uint64_t>(
            new ServerStateStream(std::move(context), std::move(stream)));

    return true;
}

bool LeapBrushApi_ServerStateStream_GetNext(uint64_t streamHandle, ProtoBytes* respBytes) {
    auto* stream = reinterpret_cast<ServerStateStream *>(streamHandle);
    if (stream == nullptr || respBytes == nullptr) {
        ALOGE("LeapBrushApi_ServerStateStream_GetNext: Invalid params");
        return false;
    }

    ServerStateResponse resp;
    if (!stream->getStream().Read(&resp)) {
        ALOGE("LeapBrushApi_ServerStateStream_GetNext: Read failed");
        return false;
    }

    std::string respString = resp.SerializeAsString();
    respBytes->bytes = (char *) malloc(sizeof(char) * respString.size());
    memcpy(respBytes->bytes, respString.data(), respString.size());
    respBytes->size = respString.size();
    return true;
}

bool LeapBrushApi_Client_Rpc(uint64_t clientHandle, ProtoBytes* reqBytes, ProtoBytes* respBytes) {
    auto* clientStub = reinterpret_cast<LeapBrushApi::Stub *>(clientHandle);
    if (clientStub == nullptr || reqBytes == nullptr || reqBytes->bytes == nullptr ||
        respBytes == nullptr) {
        ALOGE("LeapBrushApi_Client_Rpc: Invalid Parameters");
        return false;
    }

    RpcRequest req;
    req.ParseFromArray(reqBytes->bytes, reqBytes->size);
    RpcResponse resp;

    ClientContext context;
    Status status = clientStub->Rpc(&context, req, &resp);

    if (!status.ok()) {
        ALOGE("LeapBrushApi_Client_Rpc Failed: %s", status.error_message().c_str());
        return false;
    }

    std::string respString = resp.SerializeAsString();
    respBytes->bytes = (char *) malloc(sizeof(char) * respString.size());
    memcpy(respBytes->bytes, respString.data(), respString.size());
    respBytes->size = respString.size();
    return true;
}

void LeapBrushApi_UpdateDeviceStreamDestroy(uint64_t streamHandle) {
    auto* stream = reinterpret_cast<UpdateDeviceStream *>(streamHandle);
    delete stream;
}

void LeapBrushApi_ServerStateStreamDestroy(uint64_t streamHandle) {
    auto* stream = reinterpret_cast<ServerStateStream *>(streamHandle);
    delete stream;
}

void LeapBrushApi_Client_CloseAndWait(uint64_t clientHandle) {
    auto* clientStub = reinterpret_cast<LeapBrushApi::Stub *>(clientHandle);
    delete clientStub;
}

}
