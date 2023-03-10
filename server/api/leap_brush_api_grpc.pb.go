// Code generated by protoc-gen-go-grpc. DO NOT EDIT.
// versions:
// - protoc-gen-go-grpc v1.2.0
// - protoc             v3.19.4
// source: leap_brush_api.proto

package api

import (
	context "context"
	grpc "google.golang.org/grpc"
	codes "google.golang.org/grpc/codes"
	status "google.golang.org/grpc/status"
)

// This is a compile-time assertion to ensure that this generated file
// is compatible with the grpc package it is being compiled against.
// Requires gRPC-Go v1.32.0 or later.
const _ = grpc.SupportPackageIsVersion7

// LeapBrushApiClient is the client API for LeapBrushApi service.
//
// For semantics around ctx use and closing/ending streaming RPCs, please refer to https://pkg.go.dev/google.golang.org/grpc/?tab=doc#ClientConn.NewStream.
type LeapBrushApiClient interface {
	RegisterAndListen(ctx context.Context, in *RegisterDeviceRequest, opts ...grpc.CallOption) (LeapBrushApi_RegisterAndListenClient, error)
	// Deprecated: Do not use.
	UpdateDevice(ctx context.Context, in *UpdateDeviceRequest, opts ...grpc.CallOption) (*UpdateDeviceResponse, error)
	UpdateDeviceStream(ctx context.Context, opts ...grpc.CallOption) (LeapBrushApi_UpdateDeviceStreamClient, error)
	Rpc(ctx context.Context, in *RpcRequest, opts ...grpc.CallOption) (*RpcResponse, error)
}

type leapBrushApiClient struct {
	cc grpc.ClientConnInterface
}

func NewLeapBrushApiClient(cc grpc.ClientConnInterface) LeapBrushApiClient {
	return &leapBrushApiClient{cc}
}

func (c *leapBrushApiClient) RegisterAndListen(ctx context.Context, in *RegisterDeviceRequest, opts ...grpc.CallOption) (LeapBrushApi_RegisterAndListenClient, error) {
	stream, err := c.cc.NewStream(ctx, &LeapBrushApi_ServiceDesc.Streams[0], "/leapbrush.LeapBrushApi/RegisterAndListen", opts...)
	if err != nil {
		return nil, err
	}
	x := &leapBrushApiRegisterAndListenClient{stream}
	if err := x.ClientStream.SendMsg(in); err != nil {
		return nil, err
	}
	if err := x.ClientStream.CloseSend(); err != nil {
		return nil, err
	}
	return x, nil
}

type LeapBrushApi_RegisterAndListenClient interface {
	Recv() (*ServerStateResponse, error)
	grpc.ClientStream
}

type leapBrushApiRegisterAndListenClient struct {
	grpc.ClientStream
}

func (x *leapBrushApiRegisterAndListenClient) Recv() (*ServerStateResponse, error) {
	m := new(ServerStateResponse)
	if err := x.ClientStream.RecvMsg(m); err != nil {
		return nil, err
	}
	return m, nil
}

// Deprecated: Do not use.
func (c *leapBrushApiClient) UpdateDevice(ctx context.Context, in *UpdateDeviceRequest, opts ...grpc.CallOption) (*UpdateDeviceResponse, error) {
	out := new(UpdateDeviceResponse)
	err := c.cc.Invoke(ctx, "/leapbrush.LeapBrushApi/UpdateDevice", in, out, opts...)
	if err != nil {
		return nil, err
	}
	return out, nil
}

func (c *leapBrushApiClient) UpdateDeviceStream(ctx context.Context, opts ...grpc.CallOption) (LeapBrushApi_UpdateDeviceStreamClient, error) {
	stream, err := c.cc.NewStream(ctx, &LeapBrushApi_ServiceDesc.Streams[1], "/leapbrush.LeapBrushApi/UpdateDeviceStream", opts...)
	if err != nil {
		return nil, err
	}
	x := &leapBrushApiUpdateDeviceStreamClient{stream}
	return x, nil
}

type LeapBrushApi_UpdateDeviceStreamClient interface {
	Send(*UpdateDeviceRequest) error
	CloseAndRecv() (*UpdateDeviceResponse, error)
	grpc.ClientStream
}

type leapBrushApiUpdateDeviceStreamClient struct {
	grpc.ClientStream
}

func (x *leapBrushApiUpdateDeviceStreamClient) Send(m *UpdateDeviceRequest) error {
	return x.ClientStream.SendMsg(m)
}

func (x *leapBrushApiUpdateDeviceStreamClient) CloseAndRecv() (*UpdateDeviceResponse, error) {
	if err := x.ClientStream.CloseSend(); err != nil {
		return nil, err
	}
	m := new(UpdateDeviceResponse)
	if err := x.ClientStream.RecvMsg(m); err != nil {
		return nil, err
	}
	return m, nil
}

func (c *leapBrushApiClient) Rpc(ctx context.Context, in *RpcRequest, opts ...grpc.CallOption) (*RpcResponse, error) {
	out := new(RpcResponse)
	err := c.cc.Invoke(ctx, "/leapbrush.LeapBrushApi/Rpc", in, out, opts...)
	if err != nil {
		return nil, err
	}
	return out, nil
}

// LeapBrushApiServer is the server API for LeapBrushApi service.
// All implementations must embed UnimplementedLeapBrushApiServer
// for forward compatibility
type LeapBrushApiServer interface {
	RegisterAndListen(*RegisterDeviceRequest, LeapBrushApi_RegisterAndListenServer) error
	// Deprecated: Do not use.
	UpdateDevice(context.Context, *UpdateDeviceRequest) (*UpdateDeviceResponse, error)
	UpdateDeviceStream(LeapBrushApi_UpdateDeviceStreamServer) error
	Rpc(context.Context, *RpcRequest) (*RpcResponse, error)
	mustEmbedUnimplementedLeapBrushApiServer()
}

// UnimplementedLeapBrushApiServer must be embedded to have forward compatible implementations.
type UnimplementedLeapBrushApiServer struct {
}

func (UnimplementedLeapBrushApiServer) RegisterAndListen(*RegisterDeviceRequest, LeapBrushApi_RegisterAndListenServer) error {
	return status.Errorf(codes.Unimplemented, "method RegisterAndListen not implemented")
}
func (UnimplementedLeapBrushApiServer) UpdateDevice(context.Context, *UpdateDeviceRequest) (*UpdateDeviceResponse, error) {
	return nil, status.Errorf(codes.Unimplemented, "method UpdateDevice not implemented")
}
func (UnimplementedLeapBrushApiServer) UpdateDeviceStream(LeapBrushApi_UpdateDeviceStreamServer) error {
	return status.Errorf(codes.Unimplemented, "method UpdateDeviceStream not implemented")
}
func (UnimplementedLeapBrushApiServer) Rpc(context.Context, *RpcRequest) (*RpcResponse, error) {
	return nil, status.Errorf(codes.Unimplemented, "method Rpc not implemented")
}
func (UnimplementedLeapBrushApiServer) mustEmbedUnimplementedLeapBrushApiServer() {}

// UnsafeLeapBrushApiServer may be embedded to opt out of forward compatibility for this service.
// Use of this interface is not recommended, as added methods to LeapBrushApiServer will
// result in compilation errors.
type UnsafeLeapBrushApiServer interface {
	mustEmbedUnimplementedLeapBrushApiServer()
}

func RegisterLeapBrushApiServer(s grpc.ServiceRegistrar, srv LeapBrushApiServer) {
	s.RegisterService(&LeapBrushApi_ServiceDesc, srv)
}

func _LeapBrushApi_RegisterAndListen_Handler(srv interface{}, stream grpc.ServerStream) error {
	m := new(RegisterDeviceRequest)
	if err := stream.RecvMsg(m); err != nil {
		return err
	}
	return srv.(LeapBrushApiServer).RegisterAndListen(m, &leapBrushApiRegisterAndListenServer{stream})
}

type LeapBrushApi_RegisterAndListenServer interface {
	Send(*ServerStateResponse) error
	grpc.ServerStream
}

type leapBrushApiRegisterAndListenServer struct {
	grpc.ServerStream
}

func (x *leapBrushApiRegisterAndListenServer) Send(m *ServerStateResponse) error {
	return x.ServerStream.SendMsg(m)
}

func _LeapBrushApi_UpdateDevice_Handler(srv interface{}, ctx context.Context, dec func(interface{}) error, interceptor grpc.UnaryServerInterceptor) (interface{}, error) {
	in := new(UpdateDeviceRequest)
	if err := dec(in); err != nil {
		return nil, err
	}
	if interceptor == nil {
		return srv.(LeapBrushApiServer).UpdateDevice(ctx, in)
	}
	info := &grpc.UnaryServerInfo{
		Server:     srv,
		FullMethod: "/leapbrush.LeapBrushApi/UpdateDevice",
	}
	handler := func(ctx context.Context, req interface{}) (interface{}, error) {
		return srv.(LeapBrushApiServer).UpdateDevice(ctx, req.(*UpdateDeviceRequest))
	}
	return interceptor(ctx, in, info, handler)
}

func _LeapBrushApi_UpdateDeviceStream_Handler(srv interface{}, stream grpc.ServerStream) error {
	return srv.(LeapBrushApiServer).UpdateDeviceStream(&leapBrushApiUpdateDeviceStreamServer{stream})
}

type LeapBrushApi_UpdateDeviceStreamServer interface {
	SendAndClose(*UpdateDeviceResponse) error
	Recv() (*UpdateDeviceRequest, error)
	grpc.ServerStream
}

type leapBrushApiUpdateDeviceStreamServer struct {
	grpc.ServerStream
}

func (x *leapBrushApiUpdateDeviceStreamServer) SendAndClose(m *UpdateDeviceResponse) error {
	return x.ServerStream.SendMsg(m)
}

func (x *leapBrushApiUpdateDeviceStreamServer) Recv() (*UpdateDeviceRequest, error) {
	m := new(UpdateDeviceRequest)
	if err := x.ServerStream.RecvMsg(m); err != nil {
		return nil, err
	}
	return m, nil
}

func _LeapBrushApi_Rpc_Handler(srv interface{}, ctx context.Context, dec func(interface{}) error, interceptor grpc.UnaryServerInterceptor) (interface{}, error) {
	in := new(RpcRequest)
	if err := dec(in); err != nil {
		return nil, err
	}
	if interceptor == nil {
		return srv.(LeapBrushApiServer).Rpc(ctx, in)
	}
	info := &grpc.UnaryServerInfo{
		Server:     srv,
		FullMethod: "/leapbrush.LeapBrushApi/Rpc",
	}
	handler := func(ctx context.Context, req interface{}) (interface{}, error) {
		return srv.(LeapBrushApiServer).Rpc(ctx, req.(*RpcRequest))
	}
	return interceptor(ctx, in, info, handler)
}

// LeapBrushApi_ServiceDesc is the grpc.ServiceDesc for LeapBrushApi service.
// It's only intended for direct use with grpc.RegisterService,
// and not to be introspected or modified (even as a copy)
var LeapBrushApi_ServiceDesc = grpc.ServiceDesc{
	ServiceName: "leapbrush.LeapBrushApi",
	HandlerType: (*LeapBrushApiServer)(nil),
	Methods: []grpc.MethodDesc{
		{
			MethodName: "UpdateDevice",
			Handler:    _LeapBrushApi_UpdateDevice_Handler,
		},
		{
			MethodName: "Rpc",
			Handler:    _LeapBrushApi_Rpc_Handler,
		},
	},
	Streams: []grpc.StreamDesc{
		{
			StreamName:    "RegisterAndListen",
			Handler:       _LeapBrushApi_RegisterAndListen_Handler,
			ServerStreams: true,
		},
		{
			StreamName:    "UpdateDeviceStream",
			Handler:       _LeapBrushApi_UpdateDeviceStream_Handler,
			ClientStreams: true,
		},
	},
	Metadata: "leap_brush_api.proto",
}
