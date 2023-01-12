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
	periodicChecksInterval             = time.Second
	userTimeout                        = 10 * time.Second
	periodicServerToClientPingInterval = 1 * time.Second
)

type UserState struct {
	userName       string
	lastPingTime   time.Time
	stateProto     *pb.UserStateProto
	spaceInfoProto *pb.SpaceInfoProto
}

func (u *UserState) Init() {
}

type AnchorState struct {
	id             string
	userSet        map[string]bool
	brushStrokes   map[string]*pb.BrushStrokeProto
	externalModels map[string]*pb.ExternalModelProto
}

func (a *AnchorState) Init() {
	a.userSet = make(map[string]bool)
	a.brushStrokes = make(map[string]*pb.BrushStrokeProto)
	a.externalModels = make(map[string]*pb.ExternalModelProto)
}

type UserBrushStrokeState struct {
	anchorId     string
	numPosesSent int
}

type UserConnectionState struct {
	userName                         string
	appVersion                       string
	shutDownStart                    chan bool
	shutDownDone                     chan bool
	brushStrokeState                 map[string]*UserBrushStrokeState
	notifyAboutUsers                 map[string]bool
	notifyAboutBrushStrokeAdds       map[string]string
	notifyAboutBrushStrokeRemovals   map[string]string
	notifyAboutExternalModelAdds     map[string]string
	notifyAboutExternalModelRemovals map[string]string
	wakeUp                           chan bool
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

type Server struct {
	pb.UnimplementedLeapBrushApiServer

	verbose bool

	periodicChecksShutDownStart chan bool
	periodicChecksShutDownDone  chan bool

	lock               sync.Mutex
	shutDown           bool
	userStateMap       map[string]*UserState
	anchorStateMap     map[string]*AnchorState
	userConnectionsMap map[string]*UserConnectionState
}

func (s *Server) InitAndStart() {
	s.userStateMap = make(map[string]*UserState)
	s.anchorStateMap = make(map[string]*AnchorState)
	s.userConnectionsMap = make(map[string]*UserConnectionState)
	s.periodicChecksShutDownStart = make(chan bool, 1)
	s.periodicChecksShutDownDone = make(chan bool)

	go s.PeriodicChecks()
}

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

			s.DistributeMissingBrushStrokesToUserLocked(nil, userConnectionEntry)
		}()

		defer func() {
			s.lock.Lock()
			defer s.lock.Unlock()

			delete(s.userConnectionsMap, userName)

			userConnectionEntry.shutDownDone <- true

			log.Printf("User %v: Listening channel shut down (%d users now connected)",
				userName, len(s.userConnectionsMap))
		}()

		for {
			select {
			case <-userConnectionEntry.shutDownStart:
				return
			case <-userConnectionEntry.wakeUp:
				break
			case <-time.After(periodicServerToClientPingInterval):
				break
			}

			serverStateResponse := &pb.ServerStateResponse{}

			if !sentServerInfo {
				serverStateResponse.ServerInfo = &pb.ServerInfoProto{ServerVersion: serverVersion, MinAppVersion: minAppVersion}
				sentServerInfo = true
			}

			func() {
				s.lock.Lock()
				defer s.lock.Unlock()

				for userOfInterest, _ := range userConnectionEntry.notifyAboutUsers {
					if userStateOfInterest, ok := s.userStateMap[userOfInterest]; ok {
						serverStateResponse.UserState = append(serverStateResponse.UserState, userStateOfInterest.stateProto)
					}
				}
				userConnectionEntry.notifyAboutUsers = make(map[string]bool)

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

				for brushId, anchorId := range userConnectionEntry.notifyAboutBrushStrokeRemovals {
					serverStateResponse.BrushStrokeRemove = append(
						serverStateResponse.BrushStrokeRemove, &pb.BrushStrokeRemoveRequest{Id: brushId, AnchorId: anchorId})
					if s.verbose {
						log.Printf("User %s: Sending brush stroke remove for %v", userName, brushId)
					}
				}
				userConnectionEntry.notifyAboutBrushStrokeRemovals = make(map[string]string)

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

			if err := listenServer.Send(serverStateResponse); err != nil {
				log.Printf("User %v: *** Failed to send server state: %v", userName, err)
				break
			}
		}
	}()

	return nil
}

func (s *Server) UpdateDevice(ctx context.Context, req *pb.UpdateDeviceRequest) (*pb.UpdateDeviceResponse, error) {
	resp := s.HandleUpdateDevice(req)
	if resp == nil {
		resp = &pb.UpdateDeviceResponse{}
	}

	return resp, nil
}

func (s *Server) UpdateDeviceStream(updateServer pb.LeapBrushApi_UpdateDeviceStreamServer) error {
	for {
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

func (s *Server) Rpc(ctx context.Context, req *pb.RpcRequest) (*pb.RpcResponse, error) {
	s.lock.Lock()
	defer s.lock.Unlock()

	resp := &pb.RpcResponse{}

	if req.QueryUsersRequest != nil {
		resp.QueryUsersResponse = s.HandleQueryUsersLocked(req.UserName, req.QueryUsersRequest)
	}

	return resp, nil
}

func (s *Server) HandleUpdateDevice(req *pb.UpdateDeviceRequest) *pb.UpdateDeviceResponse {
	s.lock.Lock()
	defer s.lock.Unlock()

	var resp *pb.UpdateDeviceResponse = nil

	userName := req.UserState.UserName

	userStateEntry, ok := s.userStateMap[userName]
	if !ok {
		log.Printf("User %s (%s): First state update received", userName, req.UserState.UserDisplayName)
		userStateEntry = &UserState{userName: userName}
		userStateEntry.Init()
		s.userStateMap[userName] = userStateEntry
	}

	userStateEntry.lastPingTime = time.Now()

	if userStateEntry.stateProto != nil && req.UserState.UserDisplayName != userStateEntry.stateProto.UserDisplayName {
		log.Printf("User %s (%s): Display named updated from %s",
			userName, req.UserState.UserDisplayName, userStateEntry.stateProto.UserDisplayName)
	}

	userStateEntry.stateProto = req.UserState

	if req.SpaceInfo != nil {
		if !AnchorIdsEqual(req.SpaceInfo, userStateEntry.spaceInfoProto) {
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

	s.DistributeUserChangesLocked(userStateEntry, req.Echo)

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

	if req.QueryUsersRequest != nil {
		if resp == nil {
			resp = &pb.UpdateDeviceResponse{}
		}
		resp.QueryUsersResponse = s.HandleQueryUsersLocked(userName, req.QueryUsersRequest)
	}

	return resp
}

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

func (s *Server) DistributeUserChangesLocked(userStateEntry *UserState, echo bool) {
	if userStateEntry.spaceInfoProto == nil {
		return
	}

	usersToNotify := make(map[string]bool)

	for _, anchor := range userStateEntry.spaceInfoProto.Anchor {
		for userToNotify := range s.anchorStateMap[anchor.Id].userSet {
			usersToNotify[userToNotify] = true
		}
	}

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
