package main

import (
	"context"
	"crypto/tls"
	"crypto/x509"
	"flag"
	"fmt"
	"log"
	"math"
	"math/rand"
	"os"
	"os/signal"
	"strconv"
	"time"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials"
	"google.golang.org/grpc/credentials/insecure"

	pb "gitlab.magicleap.io/ghazen/leap-brush/server/api"
)

const (
	defaultName = "TEST_USER_DEFAULT"
)

var (
	addr               = flag.String("addr", "localhost:8402", "the address to connect to")
	useTLS             = flag.Bool("useTLS", false, "Enable server GRPC TLS")
	userName           = flag.String("name", defaultName, "User name")
	echo               = flag.Bool("echo", false, "Enable server echo")
	foundAnchor        = flag.String("foundAnchor", "", "Anchor to find")
	createBrushStrokes = flag.Bool("createBrushStrokes", false, "Enable creating brush strokes")
	color              = flag.String("color", "", "Set the hex color of the brush stroke")
)

func main() {
	flag.Parse()

	log.Printf("Connecting to server %v...", *addr)

	var opts []grpc.DialOption
	if *useTLS {
		cp, err := x509.SystemCertPool()
		if err != nil {
			log.Fatal("failed to get system cert pool: %v\n", err)
		}
		opts = append(opts, grpc.WithTransportCredentials(credentials.NewTLS(&tls.Config{
			InsecureSkipVerify: false,
			RootCAs:            cp,
		})))
	} else {
		opts = append(opts, grpc.WithTransportCredentials(insecure.NewCredentials()))
	}

	conn, err := grpc.Dial(*addr, opts...)
	if err != nil {
		log.Fatalf("did not connect: %v", err)
	}
	defer conn.Close()
	c := pb.NewLeapBrushApiClient(conn)

	shutDownUpload := make(chan bool, 1)
	shutDownUploadDone := make(chan bool)
	shutDownDownload := make(chan bool, 1)

	go func() {
		random := rand.New(rand.NewSource(time.Now().UnixNano()))
		controlPosition := &pb.Vector3Proto{X: 0, Y: random.Float32()*0.5 - 0.25, Z: random.Float32() + 1.0}

		firstUpload := true
		lastUploadSuccess := false

		var createdBrushIds = make(map[string]string)
		var lastBrushAction = time.Time{}

		identityPose := &pb.PoseProto{Position: &pb.Vector3Proto{}, Rotation: &pb.QuaternionProto{W: 1}}

		var spaceInfo = &pb.SpaceInfoProto{}
		if *foundAnchor != "" {
			spaceInfo.Anchor = append(spaceInfo.Anchor, &pb.AnchorProto{Id: *foundAnchor, Pose: identityPose})
		} else {
			spaceInfo.Anchor = append(spaceInfo.Anchor, &pb.AnchorProto{Id: "FAKE_ANCHOR_0", Pose: identityPose})
			spaceInfo.Anchor = append(spaceInfo.Anchor, &pb.AnchorProto{Id: "FAKE_ANCHOR_1", Pose: identityPose})
		}

		var colorRgba uint32 = 0
		if len(*color) > 0 {
			if colorRgbaUint64, err := strconv.ParseUint(*color, 16, 32); err != nil {
				log.Fatalf("Invalid hex color string %v: %v", *color, err)
			} else {
				colorRgba = uint32(colorRgbaUint64)
			}
		}

		for {
			func() {
				ctx, cancel := context.WithTimeout(context.Background(), time.Second)
				defer cancel()

				controlPosition.X = float32(math.Sin(float64(time.Now().UnixNano()) / 1000000000.0))

				req := &pb.UpdateDeviceRequest{}
				req.UserState = &pb.UserStateProto{UserName: *userName}
				req.UserState.UserDisplayName = *userName
				req.UserState.AnchorId = spaceInfo.Anchor[0].Id
				req.UserState.ControlPose = &pb.PoseProto{
					Position: controlPosition, Rotation: &pb.QuaternionProto{W: 1}}
				if firstUpload {
					req.SpaceInfo = spaceInfo
				}
				req.Echo = *echo

				if *createBrushStrokes && time.Now().After(lastBrushAction.Add(2*time.Second)) {
					if len(createdBrushIds) > 0 {
						for brushId, anchorId := range createdBrushIds {
							req.BrushStrokeRemove = &pb.BrushStrokeRemoveRequest{Id: brushId, AnchorId: anchorId}
							delete(createdBrushIds, brushId)
							break
						}
					} else if len(spaceInfo.Anchor) > 0 {
						var brushId = fmt.Sprintf("B%d", random.Int31())
						var anchorId = spaceInfo.Anchor[0].Id
						var brushStroke = &pb.BrushStrokeProto{Id: brushId, AnchorId: anchorId, UserName: *userName}
						startPosition := &pb.Vector3Proto{
							X: random.Float32()*0.5 - 0.25,
							Y: random.Float32()*0.5 - 0.25,
							Z: random.Float32() + 1.0}
						for t := 0.0; t < 5; t += 0.1 {
							position := &pb.Vector3Proto{
								X: startPosition.X + float32(math.Cos(t*5))*0.1,
								Y: startPosition.Y + float32(math.Sin(t*5))*0.1,
								Z: startPosition.Z + float32(t)/10.0}
							brushStroke.BrushPose = append(brushStroke.BrushPose,
								&pb.PoseProto{Position: position, Rotation: &pb.QuaternionProto{W: 1}})
						}
						if colorRgba != 0 {
							brushStroke.StrokeColorRgb = colorRgba
						}
						req.BrushStrokeAdd = &pb.BrushStrokeAddRequest{BrushStroke: brushStroke}
						createdBrushIds[brushId] = anchorId
					}
					lastBrushAction = time.Now()
				}

				_, err := c.UpdateDevice(ctx, req)
				if err != nil {
					if lastUploadSuccess || firstUpload {
						log.Printf("*** UpdateDevice failing: %v", err)
					}
					lastUploadSuccess = false
				} else {
					if !lastUploadSuccess {
						log.Printf("UpdateDevice started succeeding")
					}
					lastUploadSuccess = true
				}
				firstUpload = false
			}()

			select {
			case <-shutDownUpload:
				log.Print("Shutting down upload")
				shutDownUploadDone <- true
				return
			case <-time.After(15 * time.Millisecond):
				break
			}
		}
	}()

	go func() {
		defer func() {
			log.Printf("Download shut down")
		}()

		firstDownload := true
		lastDownloadSuccess := false
		firstDownloadStream := true
		lastDownloadStreamSuccess := false

		for {
			req := &pb.RegisterDeviceRequest{}
			req.UserName = *userName
			streamResp, err := c.RegisterAndListen(context.Background(), req)
			if err != nil {
				if lastDownloadSuccess || firstDownload {
					log.Printf("*** RegisterAndListen failing: %v", err)
				}
				lastDownloadSuccess = false
			} else {
				if !lastDownloadSuccess {
					log.Printf("RegisterAndListen started succeeding")
				}
				lastDownloadSuccess = true
			}
			firstDownload = false

			if streamResp != nil {
				for {
					resp, err := streamResp.Recv()
					if err != nil {
						if lastDownloadStreamSuccess || firstDownloadStream {
							log.Printf("*** RegisterAndListen stream failing: %v", err)
						}
						lastDownloadStreamSuccess = false
					} else {
						if !lastDownloadStreamSuccess {
							log.Printf("RegisterAndListen stream started succeeding")
						}
						lastDownloadStreamSuccess = true
						for _, brushStrokeAdd := range resp.BrushStrokeAdd {
							log.Printf("Adding brush stroke %s on anchor %s from user %s with %d poses",
								brushStrokeAdd.BrushStroke.Id, brushStrokeAdd.BrushStroke.AnchorId,
								brushStrokeAdd.BrushStroke.UserName, len(brushStrokeAdd.BrushStroke.BrushPose))
						}
						for _, brushStrokeRemove := range resp.BrushStrokeRemove {
							log.Printf("Removing brush stroke %s on anchor %s",
								brushStrokeRemove.Id, brushStrokeRemove.AnchorId)
						}
					}
					firstDownloadStream = false
					if err != nil {
						break
					}
				}
			}

			select {
			case <-shutDownDownload:
				return
			case <-time.After(15 * time.Millisecond):
				break
			}
		}
	}()

	stopSignal := make(chan os.Signal, 1)
	signal.Notify(stopSignal, os.Interrupt)

	<-stopSignal
	log.Printf("Received stop signal...")

	shutDownUpload <- true
	shutDownDownload <- true

	<-shutDownUploadDone
	// TODO: Wait for download shutdown

	log.Printf("Shut down complete")
}
