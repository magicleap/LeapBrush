package main

import (
	context "context"
	"flag"
	"fmt"
	"log"
	"net"
	"net/http"
	"os"
	"os/signal"
	"time"

	"github.com/improbable-eng/grpc-web/go/grpcweb"

	pb "gitlab.magicleap.io/ghazen/leap-brush/server/api"
	"google.golang.org/grpc"
)

var (
	grpcPort    = flag.Int("grpc-port", 8402, "The grpc server port")
	grpcWebPort = flag.Int("grpc-web-port", 8401, "The grpc web server port")
	verbose     = flag.Bool("verbose", false, "Whether to enable verbose logging")
)

func main() {
	flag.Parse()

	server := Server{verbose: *verbose}
	server.InitAndStart()

	grpcServer := grpc.NewServer()
	pb.RegisterLeapBrushApiServer(grpcServer, &server)

	// Deprecated: last used by client v47-2023-02-09
	grpcWebServer := grpcweb.WrapServer(grpcServer)

	webServer := &http.Server{Addr: fmt.Sprintf(":%d", *grpcWebPort), Handler: grpcWebServer}

	webServerDone := make(chan bool)
	go func() {
		log.Printf("Grpc web server v%v listening at %v", serverVersion, webServer.Addr)
		if err := webServer.ListenAndServe(); err != nil {
			log.Printf("Grpc web server shut down: %v", err)
		}
		webServerDone <- true
	}()

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

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	if err := webServer.Shutdown(ctx); err != nil {
		log.Printf("*** Failed to shut down web server: %v", err)
	}

	grpcServer.Stop()

	<-webServerDone
	<-grpcServerDone

	server.ShutDown()

	log.Printf("Shut down complete")
}
