package main

import (
	"flag"
	"fmt"
	"log"
	"net"
	"os"
	"os/signal"

	pb "gitlab.magicleap.io/ghazen/leap-brush/server/api"
	"google.golang.org/grpc"
)

var (
	grpcPort = flag.Int("grpc-port", 8402, "The grpc server port")
	verbose  = flag.Bool("verbose", false, "Whether to enable verbose logging")
)

func main() {
	flag.Parse()

	server := Server{verbose: *verbose}
	server.InitAndStart()

	grpcServer := grpc.NewServer()
	pb.RegisterLeapBrushApiServer(grpcServer, &server)

	grpcServerDone := make(chan bool)
	go func() {
		lis, err := net.Listen("tcp", fmt.Sprintf(":%d", *grpcPort))
		if err != nil {
			log.Fatalf("Failed to listen: %v", err)
		}
		log.Printf("Grpc server v%v listening at %v", serverVersion, lis.Addr())
		if err := grpcServer.Serve(lis); err != nil {
			log.Printf("Grpc server shut down: %v", err)
		}
		grpcServerDone <- true
	}()

	stopSignal := make(chan os.Signal, 1)
	signal.Notify(stopSignal, os.Interrupt)
	<-stopSignal

	log.Printf("Received stop signal...")

	grpcServer.Stop()

	<-grpcServerDone

	server.ShutDown()

	log.Printf("Shut down complete")
}
