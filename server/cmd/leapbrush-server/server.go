package main

import (
	context "context"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/proto"
	"log"
	"sync"
	"time"

	pb "gitlab.magicleap.io/ghazen/leap-brush/server/api"
)

const (
	// Interval between running periodic cleanup checks
	periodicChecksInterval = time.Second

	// Timeout for removing a user who has not sent updates in this period.
	userTimeout = 10 * time.Second

	// Interval for periodic connection health pings from server to client
	periodicServerToClientPingInterval = 1 * time.Second
)

// UserState represents th ttate for each user currently connected and uploading data
type UserState struct {
	// User identifier string
	userName string
	// Time when the user last sent an update
	lastPingTime time.Time
	// Latest state of the user
	stateProto *pb.UserStateProto
	// Latest Space information for ths user
	spaceInfoProto *pb.SpaceInfoProto
}

func (u *UserState) Init() {
}

// AnchorState represents the state for a Spatial Anchor
type AnchorState struct {
	// The spatial anchor identifier
	id string
	// Set of users that have currently found this spatial anchor. Key is userName, Value is ignored.
	userSet map[string]bool
	// Map of Brush strokes attached to this spatial anchor. Key is brush stroke id.
	brushStrokes map[string]*pb.BrushStrokeProto
	// Map of External 3D Models attached to this spatial anchor. Key is model id.
	externalModels map[string]*pb.ExternalModelProto
}

func (a *AnchorState) Init() {
	a.userSet = make(map[string]bool)
	a.brushStrokes = make(map[string]*pb.BrushStrokeProto)
	a.externalModels = make(map[string]*pb.ExternalModelProto)
}

// UserBrushStrokeState represents the state of a particular connected user who has received a
// partial set of poses from a brush stroke.
type UserBrushStrokeState struct {
	// The spatial anchor identifier where this brush stroke is attached.
	anchorId string
	// The number of poses already sent to this user for this brush stroke.
	numPosesSent int
}

// UserConnectionState represents the current state of a connected user's Download thread
type UserConnectionState struct {
	// The user identifier for the connected user.
	userName string
	// The client application version string.
	appVersion string
	// A channel to trigger shutdown of this connection.
	shutDownStart chan bool
	// A channel to receive when shutdown of this channel has completed before a new one starts.
	shutDownDone chan bool
	// Map from brush stroke ids to current UserBrushStrokeState.
	brushStrokeState map[string]*UserBrushStrokeState
	// Set of other users that have had state update changes this user needs to be notified about.
	notifyAboutUsers map[string]bool
	// Set of brush strokes that have been modified that this user needs to be notified about.
	// Key is brush stroke id, value is attached anchor id.
	notifyAboutBrushStrokeAdds map[string]string
	// Set of brush strokes that have been removed that this user needs to be notified about.
	// Key is brush stroke id, value is attached anchor id.
	notifyAboutBrushStrokeRemovals map[string]string
	// Set of 3D models that have been added or modified that this user needs to be notified about.
	// Key is external model id, value is attached anchor id.
	notifyAboutExternalModelAdds map[string]string
	// Set of 3D models that have been removed that this user needs to be notified about.
	// Key is external model id, value is attached anchor id.
	notifyAboutExternalModelRemovals map[string]string
	// A channel to trigger the wake-up of this connection if it was sleeping for work to do.
	wakeUp chan bool
}

func (u *UserConnectionState) Init() {
	u.shutDownStart = make(chan bool, 1)
	u.shutDownDone = make(chan bool, 1)
	u.brushStrokeState = make(map[string]*UserBrushStrokeState)
	u.notifyAboutUsers = make(map[string]bool)
	u.notifyAboutBrushStrokeAdds = make(map[string]string)
	u.notifyAboutBrushStrokeRemovals = make(map[string]string)
	u.notifyAboutExternalModelAdds = make(map[string]string)
	u.notifyAboutExternalModelRemovals = make(map[string]string)
	u.wakeUp = make(chan bool, 1)
}

// Server implements the leap brush api and stores brush strokes, 3d models, and user state, with dispatch
type Server struct {
	pb.UnimplementedLeapBrushApiServer

	// Whether this server should log verbosely.
	verbose bool

	// A channel to notify that periodic checks should stop.
	periodicChecksShutDownStart chan bool
	// A channel to notify that ths periodic checks have stopped.
	periodicChecksShutDownDone chan bool

	// Lock to protect cross-thread accessed data
	lock sync.Mutex
	// Whether this server is shutting down.
	shutDown bool
	// Map from user identifier to current user state.
	userStateMap map[string]*UserState
	// Map from anchor id to anchor state.
	anchorStateMap map[string]*AnchorState
	// Map from user identifier to connection state.
	userConnectionsMap map[string]*UserConnectionState
}

// InitAndStart initializes and starts the server
func (s *Server) InitAndStart() {
	s.userStateMap = make(map[string]*UserState)
	s.anchorStateMap = make(map[string]*AnchorState)
	s.userConnectionsMap = make(map[string]*UserConnectionState)
	s.periodicChecksShutDownStart = make(chan bool, 1)
	s.periodicChecksShutDownDone = make(chan bool)

	go s.PeriodicChecks()
}

// ShutDown initiates and waits for server shutdown
func (s *Server) ShutDown() {
	func() {
		s.lock.Lock()
		defer s.lock.Unlock()

		s.shutDown = true
		s.periodicChecksShutDownStart <- true

		for _, userConnectionEntry := range s.userConnectionsMap {
			select {
			case userConnectionEntry.shutDownStart <- true:
			default:
			}
		}
	}()

	<-s.periodicChecksShutDownDone
}

// PeriodicChecks runs periodic server cleanup checks until shutdown is initiated.
func (s *Server) PeriodicChecks() {
	for {
		select {
		case <-s.periodicChecksShutDownStart:
			log.Print("Periodic checks shutting down...")
			s.periodicChecksShutDownDone <- true
			return
		case <-time.After(periodicChecksInterval):
			break
		}

		func() {
			s.lock.Lock()
			defer s.lock.Unlock()

			now := time.Now()
			var timedOutUsers []string = nil

			// Look for and clean up users that have not sent updates in a while.
			for userName, userState := range s.userStateMap {
				if now.After(userState.lastPingTime.Add(userTimeout)) {
					if timedOutUsers == nil {
						timedOutUsers = make([]string, 0, len(s.userStateMap))
					}
					s.RemoveUserAnchorsLocked(userState)
					timedOutUsers = append(timedOutUsers, userName)
				}
			}
			if timedOutUsers != nil {
				for _, userName := range timedOutUsers {
					log.Printf("User %v: Expiring due to timeout", userName)
					delete(s.userStateMap, userName)
				}
			}
		}()
	}
}

// RegisterAndListen handles the download connection from a client and sends a stream of server updates when
// information changes that that client should be notified about.
func (s *Server) RegisterAndListen(req *pb.RegisterDeviceRequest, listenServer pb.LeapBrushApi_RegisterAndListenServer) error {
	func() {
		var userConnectionEntry *UserConnectionState
		var existingEntryFound bool
		var userName string
		var sentServerInfo bool

		func() {
			s.lock.Lock()
			defer s.lock.Unlock()

			userName = req.UserName
			userConnectionEntry, existingEntryFound = s.userConnectionsMap[userName]
		}()

		// If an existing connection state is present for this user, shut it down and wait for it to exit
		// before continuing with the new connection.
		if existingEntryFound {
			log.Printf("User %v: Shutting down existing listening channel...", userName)
			userConnectionEntry.shutDownStart <- true
			<-userConnectionEntry.shutDownDone
			log.Printf("User %v: Existing listening channel shut down", userName)
		}

		func() {
			s.lock.Lock()
			defer s.lock.Unlock()

			userConnectionEntry = &UserConnectionState{userName: userName, appVersion: req.AppVersion}
			userConnectionEntry.Init()
			s.userConnectionsMap[userName] = userConnectionEntry

			log.Printf("User %v (version %v): Starting listening channel... (%d users now connected)",
				userName, req.AppVersion, len(s.userConnectionsMap))

			// Send the user all brush strokes and 3D models that currently apply.
			s.DistributeMissingBrushStrokesToUserLocked(nil, userConnectionEntry)
			s.DistributeMissingExternalModelsToUserLocked(nil, userConnectionEntry)
		}()

		// Deferred function to perform cleanup and shutdown
		defer func() {
			s.lock.Lock()
			defer s.lock.Unlock()

			delete(s.userConnectionsMap, userName)

			userConnectionEntry.shutDownDone <- true

			log.Printf("User %v: Listening channel shut down (%d users now connected)",
				userName, len(s.userConnectionsMap))
		}()

		// Loop until the connection (download) thread should shut down.
		for {
			// Wait until the next trigger for processing
			select {
			case <-userConnectionEntry.shutDownStart:
				// Shutdown has been requested for this connection, exit now
				return
			case <-userConnectionEntry.wakeUp:
				// This connection should wake up to process a pending state event
				break
			case <-time.After(periodicServerToClientPingInterval):
				// This connection should wake up to send a periodic ping to the client.
				break
			}

			serverStateResponse := &pb.ServerStateResponse{}

			// Send server info to the client one time
			if !sentServerInfo {
				serverStateResponse.ServerInfo = &pb.ServerInfoProto{ServerVersion: serverVersion, MinAppVersion: minAppVersion}
				sentServerInfo = true
			}

			func() {
				s.lock.Lock()
				defer s.lock.Unlock()

				// Notify the client of each other user that has had state changes since last server update.
				for userOfInterest, _ := range userConnectionEntry.notifyAboutUsers {
					if userStateOfInterest, ok := s.userStateMap[userOfInterest]; ok {
						serverStateResponse.UserState = append(serverStateResponse.UserState, userStateOfInterest.stateProto)
					}
				}
				// Clear the notifyAboutUsers map now that all pending notifications for user state have been added.
				userConnectionEntry.notifyAboutUsers = make(map[string]bool)

				// Go through each entry in notifyAboutBrushStrokeAdds and send any new brush strokes. At most one
				// brush stroke (and also missing poses) is sent per server update to avoid very large proto sizes --
				// prefer a streamed approach.
				for brushId, anchorId := range userConnectionEntry.notifyAboutBrushStrokeAdds {
					delete(userConnectionEntry.notifyAboutBrushStrokeAdds, brushId)
					if anchorState, ok := s.anchorStateMap[anchorId]; ok {
						if brushState, ok := anchorState.brushStrokes[brushId]; ok {
							var userBrushStrokeState *UserBrushStrokeState
							if userBrushStrokeState, ok = userConnectionEntry.brushStrokeState[brushId]; !ok {
								userBrushStrokeState = &UserBrushStrokeState{anchorId: anchorId}
								userConnectionEntry.brushStrokeState[brushId] = userBrushStrokeState
							}

							if userBrushStrokeState.numPosesSent < len(brushState.BrushPose) {
								brushStrokeSend := &pb.BrushStrokeProto{}
								if userBrushStrokeState.numPosesSent == 0 {
									proto.Merge(brushStrokeSend, brushState)
								} else {
									brushStrokeSend.Id = brushState.Id
									brushStrokeSend.AnchorId = brushState.AnchorId
									brushStrokeSend.StartIndex = int32(userBrushStrokeState.numPosesSent)
									for i := userBrushStrokeState.numPosesSent; i < len(brushState.BrushPose); i++ {
										brushStrokeSend.BrushPose = append(brushStrokeSend.BrushPose,
											brushState.BrushPose[i])
									}
								}

								if s.verbose {
									log.Printf("User %s: Sending brush stroke %v update from %v: %v new poses, %v total poses",
										userName, brushState.Id, brushState.UserName, len(brushStrokeSend.BrushPose),
										int(brushStrokeSend.StartIndex)+len(brushStrokeSend.BrushPose))
								}
								serverStateResponse.BrushStrokeAdd = append(
									serverStateResponse.BrushStrokeAdd,
									&pb.BrushStrokeAddRequest{BrushStroke: brushStrokeSend})

								userBrushStrokeState.numPosesSent = len(brushState.BrushPose)
							}

							break
						}
					}
				}

				if len(userConnectionEntry.notifyAboutBrushStrokeAdds) > 0 {
					// Pre-wake up the connection thread if more brush strokes are already pending
					select {
					case userConnectionEntry.wakeUp <- true:
					default:
					}
				}

				// Include in the response every brush stroke remove event that this user hasn't
				// been notified about yet.
				for brushId, anchorId := range userConnectionEntry.notifyAboutBrushStrokeRemovals {
					serverStateResponse.BrushStrokeRemove = append(
						serverStateResponse.BrushStrokeRemove, &pb.BrushStrokeRemoveRequest{Id: brushId, AnchorId: anchorId})
					if s.verbose {
						log.Printf("User %s: Sending brush stroke remove for %v", userName, brushId)
					}
				}
				userConnectionEntry.notifyAboutBrushStrokeRemovals = make(map[string]string)

				// Include in the response all added or modified external 3d model states that the user hasn't been
				// notified about yet.
				for modelId, anchorId := range userConnectionEntry.notifyAboutExternalModelAdds {
					if anchorState, ok := s.anchorStateMap[anchorId]; ok {
						if modelState, ok := anchorState.externalModels[modelId]; ok {
							if s.verbose {
								log.Printf("User %s: Sending model %v (%v) update from %v",
									userName, modelState.Id, modelState.FileName, modelState.ModifiedByUserName)
							}
							serverStateResponse.ExternalModelAdd = append(
								serverStateResponse.ExternalModelAdd,
								&pb.ExternalModelAddRequest{Model: modelState})
						}
					}
				}
				userConnectionEntry.notifyAboutExternalModelAdds = make(map[string]string)

				// Include in the response every 3d model remove event that this user hasn't
				// been notified about yet.
				for modelId, anchorId := range userConnectionEntry.notifyAboutExternalModelRemovals {
					serverStateResponse.ExternalModelRemove = append(
						serverStateResponse.ExternalModelRemove, &pb.ExternalModelRemoveRequest{
							Id: modelId, AnchorId: anchorId})
					if s.verbose {
						log.Printf("User %s: Sending model remove for %v", userName, modelId)
					}
				}
				userConnectionEntry.notifyAboutExternalModelRemovals = make(map[string]string)
			}()

			// Send the new server response to the connection stream. It may be empty in the case of a periodic
			// health check ping.
			if err := listenServer.Send(serverStateResponse); err != nil {
				log.Printf("User %v: *** Failed to send server state: %v", userName, err)
				break
			}
		}
	}()

	return nil
}

// UpdateDeviceStream handles a stream of updates from clients.
func (s *Server) UpdateDeviceStream(updateServer pb.LeapBrushApi_UpdateDeviceStreamServer) error {
	for {
		// Receive the next update message
		req, err := updateServer.Recv()
		if err != nil {
			return err
		}
		resp := s.HandleUpdateDevice(req)
		if resp != nil {
			log.Printf("*** Error: unexpected UpdateDeviceResponse generated: %v", resp)
			return status.Errorf(codes.Internal, "unexpected UpdateDeviceResponse generated")
		}
	}
}

// Rpc handles an out-of-band remote procedure call from a client
func (s *Server) Rpc(ctx context.Context, req *pb.RpcRequest) (*pb.RpcResponse, error) {
	s.lock.Lock()
	defer s.lock.Unlock()

	resp := &pb.RpcResponse{}

	if req.QueryUsersRequest != nil {
		resp.QueryUsersResponse = s.HandleQueryUsersLocked(req.UserName, req.QueryUsersRequest)
	}

	return resp, nil
}

// HandleUpdateDevice handles a single update device request from a client stream.
func (s *Server) HandleUpdateDevice(req *pb.UpdateDeviceRequest) *pb.UpdateDeviceResponse {
	s.lock.Lock()
	defer s.lock.Unlock()

	var resp *pb.UpdateDeviceResponse = nil

	userName := req.UserState.UserName

	// Create a new entry in userStateMap if this user doesn't have a record yet.
	userStateEntry, ok := s.userStateMap[userName]
	if !ok {
		log.Printf("User %s (%s): First state update received", userName, req.UserState.UserDisplayName)
		userStateEntry = &UserState{userName: userName}
		userStateEntry.Init()
		s.userStateMap[userName] = userStateEntry
	}

	// Note the time the user state was last received in order to time out disconnected clients.
	userStateEntry.lastPingTime = time.Now()

	if userStateEntry.stateProto != nil && req.UserState.UserDisplayName != userStateEntry.stateProto.UserDisplayName {
		log.Printf("User %s (%s): Display named updated from %s",
			userName, req.UserState.UserDisplayName, userStateEntry.stateProto.UserDisplayName)
	}

	userStateEntry.stateProto = req.UserState

	if req.SpaceInfo != nil {
		if !AnchorIdsEqual(req.SpaceInfo, userStateEntry.spaceInfoProto) {
			// The user's found anchor ids have changed: remove and re-add the user to the anchor state maps.
			s.RemoveUserAnchorsLocked(userStateEntry)
			userStateEntry.spaceInfoProto = req.SpaceInfo
			for _, anchor := range userStateEntry.spaceInfoProto.Anchor {
				anchorState, ok := s.anchorStateMap[anchor.Id]
				if !ok {
					anchorState = &AnchorState{id: anchor.Id}
					anchorState.Init()
					s.anchorStateMap[anchor.Id] = anchorState
				}
				anchorState.userSet[userName] = true
			}

			log.Printf("User %s (%s): Found anchors updated: %v (space %v: %v)",
				userName, req.UserState.UserDisplayName, req.SpaceInfo.Anchor, req.SpaceInfo.SpaceName,
				req.SpaceInfo.SpaceId)

			// Now that the user has found a different set of anchors, send any brush strokes and 3d model information
			// that they haven't recieved yet.
			s.DistributeMissingBrushStrokesToUserLocked(userStateEntry, nil)
			s.DistributeMissingExternalModelsToUserLocked(userStateEntry, nil)
		} else {
			if s.verbose {
				log.Printf("User %s (%s): Found anchors updated (no ids changed): %v (space %v: %v)",
					userName, req.UserState.UserDisplayName, req.SpaceInfo.Anchor, req.SpaceInfo.SpaceName,
					req.SpaceInfo.SpaceId)
			}
			userStateEntry.spaceInfoProto = req.SpaceInfo
		}
	}

	// Distribute the change for the current user to any other users that should be notified (by having the same
	// anchors).
	s.DistributeUserChangesLocked(userStateEntry, req.Echo)

	// Process an added or modified brush stroke by the user
	if req.BrushStrokeAdd != nil {
		if anchorState, ok := s.anchorStateMap[req.BrushStrokeAdd.BrushStroke.AnchorId]; ok {
			if existingBrushStroke, ok := anchorState.brushStrokes[req.BrushStrokeAdd.BrushStroke.Id]; ok {
				if req.BrushStrokeAdd.BrushStroke.StartIndex < int32(len(existingBrushStroke.BrushPose)) {
					existingBrushStroke.BrushPose =
						existingBrushStroke.BrushPose[:req.BrushStrokeAdd.BrushStroke.StartIndex]
				}
				for _, pose := range req.BrushStrokeAdd.BrushStroke.BrushPose {
					existingBrushStroke.BrushPose = append(existingBrushStroke.BrushPose, pose)
				}
			} else {
				if req.BrushStrokeAdd.BrushStroke.StartIndex != 0 {
					log.Printf("*** Warning: added brush stroke has unexpected start index, data loss likely")
					req.BrushStrokeAdd.BrushStroke.StartIndex = 0
				}
				anchorState.brushStrokes[req.BrushStrokeAdd.BrushStroke.Id] = req.BrushStrokeAdd.BrushStroke
			}
			s.DistributeBrushStrokeAddLocked(anchorState, req.BrushStrokeAdd.BrushStroke.Id,
				int(req.BrushStrokeAdd.BrushStroke.StartIndex), userName, req.Echo)
			if s.verbose {
				if req.BrushStrokeAdd.BrushStroke.StartIndex > 0 {
					log.Printf("User %s: Appended to brush stroke %s for anchor %s, %d new poses, %d total poses",
						userName, req.BrushStrokeAdd.BrushStroke.Id, anchorState.id,
						len(req.BrushStrokeAdd.BrushStroke.BrushPose),
						len(req.BrushStrokeAdd.BrushStroke.BrushPose)+int(req.BrushStrokeAdd.BrushStroke.StartIndex))
				} else {
					log.Printf("User %s: Started new brush stroke %s for anchor %s, %d poses",
						userName, req.BrushStrokeAdd.BrushStroke.Id, anchorState.id,
						len(req.BrushStrokeAdd.BrushStroke.BrushPose))
				}
			}
		}
	}

	// Process a removed brush stroke from the user.
	if req.BrushStrokeRemove != nil {
		if anchorState, ok := s.anchorStateMap[req.BrushStrokeRemove.AnchorId]; ok {
			delete(anchorState.brushStrokes, req.BrushStrokeRemove.Id)
			s.DistributeBrushStrokeRemoveLocked(anchorState, req.BrushStrokeRemove.Id, userName, req.Echo)
			if s.verbose {
				log.Printf("User %s: Removed brush stroke %s from anchor %s",
					userName, req.BrushStrokeRemove.Id, anchorState.id)
			}
		}
	}

	// Process an added or modified external 3d model from the user.
	if req.ExternalModelAdd != nil {
		if anchorState, ok := s.anchorStateMap[req.ExternalModelAdd.Model.AnchorId]; ok {
			anchorState.externalModels[req.ExternalModelAdd.Model.Id] = req.ExternalModelAdd.Model
			s.DistributeExternalModelAddLocked(anchorState, req.ExternalModelAdd.Model.Id, userName, req.Echo)
			if s.verbose {
				log.Printf("User %s: Create or update model %s (%v) for anchor %s",
					userName, req.ExternalModelAdd.Model.Id, req.ExternalModelAdd.Model.FileName, anchorState.id)
			}
		}
	}

	// Process a removed 3d model from the user.
	if req.ExternalModelRemove != nil {
		if anchorState, ok := s.anchorStateMap[req.ExternalModelRemove.AnchorId]; ok {
			delete(anchorState.externalModels, req.ExternalModelRemove.Id)
			s.DistributeExternalModelRemoveLocked(anchorState, req.ExternalModelRemove.Id, userName, req.Echo)
			if s.verbose {
				log.Printf("User %s: Removed model %s from anchor %s",
					userName, req.ExternalModelRemove.Id, anchorState.id)
			}
		}
	}

	return resp
}

// HandleQueryUsersLocked handles an rpc from a user to fetch the list of other connected users.
// s.lock must be held while calling this function.
func (s *Server) HandleQueryUsersLocked(userName string, _ *pb.QueryUsersRequest) *pb.QueryUsersResponse {
	resp := &pb.QueryUsersResponse{}

	for userName, userState := range s.userStateMap {
		result := &pb.QueryUsersResponse_Result{UserName: userName, UserDisplayName: userState.stateProto.UserDisplayName,
			DeviceType: userState.stateProto.DeviceType}
		result.SpaceInfo = userState.spaceInfoProto
		resp.Results = append(resp.Results, result)
	}
	if s.verbose {
		log.Printf("User %s: Queried users, %d results returned",
			userName, len(resp.Results))
	}

	return resp
}

// AnchorIdsEqual checks if the anchor ids are equal between two space info protos.
func AnchorIdsEqual(spaceInfo1 *pb.SpaceInfoProto, spaceInfo2 *pb.SpaceInfoProto) bool {
	if (spaceInfo1 == nil) != (spaceInfo2 == nil) {
		return false
	} else if spaceInfo1 == nil {
		return true
	}

	if len(spaceInfo1.Anchor) != len(spaceInfo2.Anchor) {
		return false
	}

	for i, anchor := range spaceInfo1.Anchor {
		if spaceInfo2.Anchor[i].Id != anchor.Id {
			return false
		}
	}

	return true
}

// DistributeUserChangesLocked sets notification bits for user connections so that they are sent updates in the next
// server state message. s.lock must be held while calling this function.
func (s *Server) DistributeUserChangesLocked(userStateEntry *UserState, echo bool) {
	if userStateEntry.spaceInfoProto == nil {
		return
	}

	usersToNotify := make(map[string]bool)

	// Build up a set of users to notify by looking at all the current user's anchors (users may share multiple
	// anchors).
	for _, anchor := range userStateEntry.spaceInfoProto.Anchor {
		for userToNotify := range s.anchorStateMap[anchor.Id].userSet {
			usersToNotify[userToNotify] = true
		}
	}

	// Set notify bits for each of the users to notify, and try to wake up their connection threads if sleeping.
	for userToNotify, _ := range usersToNotify {
		if userToNotify == userStateEntry.userName && !echo {
			continue
		}

		if userConnectionEntry, ok := s.userConnectionsMap[userToNotify]; ok {
			userConnectionEntry.notifyAboutUsers[userStateEntry.userName] = true
			select {
			case userConnectionEntry.wakeUp <- true:
			default:
			}
		}
	}
}

// DistributeBrushStrokeAddLocked sets notification bits for user connections, for a brush stroke that was added
// or modified. s.lock must be held while calling this function.
func (s *Server) DistributeBrushStrokeAddLocked(anchorState *AnchorState, brushStrokeId string, brushStartIndex int, senderUserName string, echo bool) {
	for userToNotify, _ := range anchorState.userSet {
		if userToNotify == senderUserName && !echo {
			continue
		}

		if userConnectionEntry, ok := s.userConnectionsMap[userToNotify]; ok {
			if userBrushStrokeState, ok := userConnectionEntry.brushStrokeState[brushStrokeId]; ok {
				if brushStartIndex < userBrushStrokeState.numPosesSent {
					userBrushStrokeState.numPosesSent = brushStartIndex
				}
			}

			userConnectionEntry.notifyAboutBrushStrokeAdds[brushStrokeId] = anchorState.id
			select {
			case userConnectionEntry.wakeUp <- true:
			default:
			}
		}
	}
}

// DistributeBrushStrokeRemoveLocked sets notification bits for user connections, for a brush stroke that was removed.
// s.lock must be held while calling this function.
func (s *Server) DistributeBrushStrokeRemoveLocked(anchorState *AnchorState, brushStrokeId string, senderUserName string, echo bool) {
	for userToNotify, _ := range anchorState.userSet {
		if userToNotify == senderUserName && !echo {
			continue
		}

		if userConnectionEntry, ok := s.userConnectionsMap[userToNotify]; ok {
			userConnectionEntry.notifyAboutBrushStrokeRemovals[brushStrokeId] = anchorState.id
			select {
			case userConnectionEntry.wakeUp <- true:
			default:
			}
		}
	}
}

// DistributeMissingBrushStrokesToUserLocked sets notify bits for all brush strokes that a user hasn't been notified
// about yet, and also cleans up obsolete brush stroke states. s.lock must be held while calling this function.
func (s *Server) DistributeMissingBrushStrokesToUserLocked(userStateEntry *UserState, userConnectionState *UserConnectionState) {
	if userStateEntry == nil {
		userStateEntry = s.userStateMap[userConnectionState.userName]
	}
	if userConnectionState == nil {
		userConnectionState = s.userConnectionsMap[userStateEntry.userName]
	}
	if userStateEntry == nil || userConnectionState == nil || userStateEntry.spaceInfoProto == nil {
		return
	}

	anchorSet := make(map[string]bool)
	for _, anchor := range userStateEntry.spaceInfoProto.Anchor {
		anchorSet[anchor.Id] = true
		if anchorState, ok := s.anchorStateMap[anchor.Id]; ok {
			for brushStrokeId, _ := range anchorState.brushStrokes {
				userConnectionState.notifyAboutBrushStrokeAdds[brushStrokeId] = anchor.Id
			}
		}
	}

	// Delete obsolete brush states for anchors no longer found by the user.

	var obsoleteBrushStates []string = nil
	for brushId, brushStrokeState := range userConnectionState.brushStrokeState {
		if _, ok := anchorSet[brushStrokeState.anchorId]; !ok {
			if obsoleteBrushStates == nil {
				obsoleteBrushStates = make([]string, 0, len(userConnectionState.brushStrokeState))
			}
			obsoleteBrushStates = append(obsoleteBrushStates, brushId)
		}
	}
	if obsoleteBrushStates != nil {
		for _, brushId := range obsoleteBrushStates {
			delete(userConnectionState.brushStrokeState, brushId)
		}
	}
}

// DistributeExternalModelAddLocked sets notification bits for user connections, for a 3d model that was added
// or modified. s.lock must be held while calling this function.
func (s *Server) DistributeExternalModelAddLocked(anchorState *AnchorState, modelId string, senderUserName string, echo bool) {
	for userToNotify, _ := range anchorState.userSet {
		if userToNotify == senderUserName && !echo {
			continue
		}

		if userConnectionEntry, ok := s.userConnectionsMap[userToNotify]; ok {
			userConnectionEntry.notifyAboutExternalModelAdds[modelId] = anchorState.id
			select {
			case userConnectionEntry.wakeUp <- true:
			default:
			}
		}
	}
}

// DistributeExternalModelRemoveLocked sets notification bits for user connections, for a 3d model that was removed.
// s.lock must be held while calling this function.
func (s *Server) DistributeExternalModelRemoveLocked(anchorState *AnchorState, modelId string, senderUserName string, echo bool) {
	for userToNotify, _ := range anchorState.userSet {
		if userToNotify == senderUserName && !echo {
			continue
		}

		if userConnectionEntry, ok := s.userConnectionsMap[userToNotify]; ok {
			userConnectionEntry.notifyAboutExternalModelRemovals[modelId] = anchorState.id
			select {
			case userConnectionEntry.wakeUp <- true:
			default:
			}
		}
	}
}

// DistributeMissingExternalModelsToUserLocked sets notify bits for all 3d models that a user hasn't been notified
// about yet. s.lock must be held while calling this function.
func (s *Server) DistributeMissingExternalModelsToUserLocked(userStateEntry *UserState, userConnectionState *UserConnectionState) {
	if userStateEntry == nil {
		userStateEntry = s.userStateMap[userConnectionState.userName]
	}
	if userConnectionState == nil {
		userConnectionState = s.userConnectionsMap[userStateEntry.userName]
	}
	if userStateEntry == nil || userConnectionState == nil {
		return
	}

	for _, anchor := range userStateEntry.spaceInfoProto.Anchor {
		if anchorState, ok := s.anchorStateMap[anchor.Id]; ok {
			for modelId, _ := range anchorState.externalModels {
				userConnectionState.notifyAboutExternalModelAdds[modelId] = anchor.Id
			}
		}
	}
}

// RemoveUserAnchorsLocked removes the user from all anchor user sets.
// s.lock must be held while calling this function.
func (s *Server) RemoveUserAnchorsLocked(userStateEntry *UserState) {
	if userStateEntry.spaceInfoProto == nil {
		return
	}

	for _, anchor := range userStateEntry.spaceInfoProto.Anchor {
		if anchorState, ok := s.anchorStateMap[anchor.Id]; ok {
			delete(anchorState.userSet, userStateEntry.userName)
		}
	}
}
