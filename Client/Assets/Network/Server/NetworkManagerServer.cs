using core;
using Hazel;
using Hazel.Tcp;
using Lidgren.Network;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
using Newtonsoft.Json;
#endif
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using uint16_t = System.UInt16;
using uint32_t = System.UInt32;

namespace Server
{
    public class NetworkManagerServer : NetworkManager
    {
        protected TcpConnectionListener listener = null;
        public bool IsBattleAuth = false;
        NetEncryption algo = null;
        public bool IsPermitDebugUser;


#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
        public static NetworkManagerServer sInstance { get; set; }

#else
        public static NetworkManagerServer sInstance {get { return (NetworkManagerServer)Instance; }  set { Instance = value;} }
#endif

        public static bool StaticInit(uint16_t inPort, byte worldCount, float ConnectionTimeout)
        {
            NetworkManagerServer.sInstance = new NetworkManagerServer();

            sInstance.Init(inPort, worldCount, ConnectionTimeout );

            return true;
        }

        void Init(uint16_t inPort, byte worldCount, float ConnectionTimeout)
        {
            base.Init(worldCount);
            mNewNetworkId = new ushort[worldCount];
            mNewPlayerId = new int[worldCount];
            for (byte i = 0; i < worldCount; ++i)
            {
                ResetNewNetworkId(i);
                mNewPlayerId[i] = 1;
            }

            NetPeerConfiguration config = new NetPeerConfiguration("game");
            config.MaximumConnections = 1000;
            config.Port = inPort;
            config.ConnectionTimeout = ConnectionTimeout;
            mNetPeer = new NetServer(config);
            mNetPeer.Start();

            algo = new NetXorEncryption(GetServer(), "AceTopSecret");

            // tcp
            SetListener(inPort);
        }

        void SetListener(uint16_t inPort)
        {
            // tcp server
            listener = new TcpConnectionListener(new NetworkEndPoint(IPAddress.Any, inPort));


            //Setup listener
            listener.NewConnection += delegate (object sender, NewConnectionEventArgs args)
            {
                NetIncomingMessage msg1 = GetServer().CreateIncomingMessage(NetIncomingMessageType.TcpStatusChanged, 4 + 1);
                msg1.isTcp = true;
                msg1.m_senderEndPoint = ((NetworkEndPoint)(args.Connection).EndPoint).EndPoint as IPEndPoint;
                msg1.Write((byte)NetConnectionStatus.Connected);
                msg1.Write(string.Empty);
                GetServer().ReleaseMessage(msg1);

                args.Connection.DataReceived += delegate (object innerSender, DataReceivedEventArgs innerArgs)
                {
                    NetIncomingMessage msg = GetServer().CreateIncomingMessage(NetIncomingMessageType.Data, innerArgs.Bytes.Length);
                    msg.isTcp = true;
                    msg.m_TcpConnecton = (TcpConnection)innerSender;
                    msg.m_senderEndPoint = ((NetworkEndPoint)((Connection)innerSender).EndPoint).EndPoint as IPEndPoint;
                    Buffer.BlockCopy(innerArgs.Bytes, 2, msg.m_data, 0, innerArgs.Bytes.Length - 2);
                    msg.m_bitLength = (ushort)((innerArgs.Bytes[0] << 8) | innerArgs.Bytes[1]);
                    //Log.Information($"TCP recv length {msg.LengthBytes}, session_id {msg.m_TcpConnecton.sessionId}");
                    GetServer().ReleaseMessage(msg);
                };

                args.Connection.Disconnected += delegate (object sender2, DisconnectedEventArgs args2)
                {
                    NetIncomingMessage msg = GetServer().CreateIncomingMessage(NetIncomingMessageType.TcpStatusChanged, 4 + 1);
                    msg.isTcp = true;
                    msg.m_senderEndPoint = ((NetworkEndPoint)((Connection)sender2).EndPoint).EndPoint as IPEndPoint;
                    msg.Write((byte)NetConnectionStatus.Disconnected);
                    msg.Write(string.Empty);
                    GetServer().ReleaseMessage(msg);
                };
            };

            listener.Start();
        }


        public NetPeer GetServer() { return (NetServer)mNetPeer; }


        int[] mNewPlayerId;
        ushort[] mNewNetworkId;

        float mClientDisconnectTimeout;


        Dictionary<Guid, PlayerController> mAddressToClientMap = new Dictionary<Guid, PlayerController>();
        Dictionary<int, PlayerController> mPlayerIdToClientMap = new Dictionary<int, PlayerController>();

        // world id, session_id, count,
        Dictionary<int, ServerCommon.InternalMessage> mPlayerAuth = new Dictionary<int, ServerCommon.InternalMessage>();


        NetworkManagerServer()
        {
            mClientDisconnectTimeout = 5.0f;

            core.NetGameObject.CreateRpcPacketServer = NetworkManagerServer.CreateRpcPacket;
            core.NetGameObject.SendServer = NetworkManagerServer.Send;
        }

        public int GetPlayerCount() { return mAddressToClientMap.Count; }

        public NetGameObject RegisterAndReturn(NetGameObject inGameObject, byte worldId)
        {
            RegisterGameObject(inGameObject, worldId);
            return inGameObject;
        }

        // 월드 클리어
        public void Clear(byte worldId)
        {
            foreach(var gameObject in mNetworkIdToGameObjectMap[worldId])
            {
                gameObject.Value.RemoveCacheAttributes();
            }

            mNetworkIdToGameObjectMap[worldId].Clear();


            ResetNewNetworkId(worldId);
        }

        public void ClearAuth(byte worldId)
        {
            mPlayerAuth.Remove((int)worldId);
        }

        public override bool ReadPacket(NetIncomingMessage inInputStream, System.Net.IPEndPoint inFromAddress)
        {
            //System.IO.FileStream fs = new System.IO.FileStream($"out/{inFromAddress.Address.ToString()}_{inFromAddress.Port.ToString()}", System.IO.FileMode.Append, System.IO.FileAccess.Write);
            //System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
            //bw.Write((int)inInputStream.LengthBytes);
            //bw.Write((int)inInputStream.LengthBits);
            //bw.Write(inInputStream.Data, 0, inInputStream.LengthBytes);
            //Log.Information($"record packet {inInputStream.LengthBytes}, {inInputStream.LengthBits}, {inInputStream.Data.Length}");
            //bw.Close();
            //fs.Close();


            inInputStream.Decrypt(algo);

            //try to get the client proxy for this address
            //pass this to the client proxy to process
            uint32_t packetType;
            packetType = inInputStream.PeekUInt32(PacketTypeLengthBits);
            if(packetType == (uint32_t)PacketType.kHello)
            {
                ProcessPacket(null, inInputStream);
                return true;
            }

            PlayerController c = null;
            mAddressToClientMap.TryGetValue(inInputStream.GetSessionId(), out c);
            if (c != null)
            {
                // RPC 패킷은 큐잉하지 않는다.
                if (packetType == (uint32_t)PacketType.kRPC)
                {
                    ProcessPacket(c, inInputStream);
                }
                else
                {
                    c.MessageQueue.Enqueue(inInputStream);
                    //Log.Warning($"MessageQueue enqueue playerId:{c.GetPlayerId()}, msgCount:{c.MessageQueue.Count}");
                    return false;
                }
            }

            return true;
        }

        public override void ProcessPacket()
        {
            foreach(var player in mAddressToClientMap)
            {
                if (player.Value.MessageQueue.Count == 0)
                    continue;

                // todo : 큐에 제한 사이즈 초과시 클라이언트 강제 종료 처리 필요
                if (player.Value.MessageQueue.Count > kMaxPacketsPerFrameCount)
                {
                    LogHelper.LogWarning($"MessageQueue playerId:{player.Value.GetPlayerId()}, msgCount:{player.Value.MessageQueue.Count}");
                }

                for(int i=0;i<kMaxPacketsPerFrameCount;++i)
                {
                    if (player.Value.MessageQueue.Count == 0)
                        break;

                    NetIncomingMessage inInputStream = player.Value.MessageQueue.Dequeue();

                    ProcessPacket(player.Value, inInputStream);

                    GetServer().Recycle(inInputStream);
                }
            }

        }

        public AIController CreateAI(byte character_id, string user_id, byte world_id, Guid session_id, int team, int player_id, int spawn_index, int characterLevel)
        {
            AIController aiController = new AIController();
            aiController.mSelectedCharacter = character_id;
            aiController.UserId = user_id;
            aiController.mSessionId = session_id;
            aiController.mPlayerId = player_id != 0 ? player_id : GetNewPlayerId(world_id);
            aiController.mWorldId = world_id;
            aiController.mCharacterLevel = characterLevel;
            Log.Information($"Create AI playerId:{aiController.mPlayerId}, character:{aiController.mSelectedCharacter}, userId:{aiController.UserId}, session:{aiController.mSessionId}");
            var actor = aiController.SpawnActor(aiController.mPlayerId, world_id, team, spawn_index);
            aiController.Possess(actor, actor);

            return aiController;
        }

        public ActorController CreateActor(byte character_id, string user_id, byte world_id, Guid session_id, int team, int player_id, int spawn_index, int characterLevel)
        {
            var actorController = new ActorController();
            actorController.mSelectedCharacter = character_id;
            actorController.UserId = user_id;
            actorController.mSessionId = session_id;
            actorController.mPlayerId = player_id != 0 ? player_id : GetNewPlayerId(world_id);
            actorController.mWorldId = world_id;
            actorController.mCharacterLevel = characterLevel;
            Log.Information($"Create Actor playerId:{actorController.mPlayerId}, character:{actorController.mSelectedCharacter}, userId:{actorController.UserId}, session:{actorController.mSessionId}");
            actorController.SpawnActor(player_id, world_id, team, spawn_index);

            return actorController;
        }

        public override void ProcessInternalMessage(string msg)
        {
            ServerCommon.InternalMessage ret = null;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
            ret = JsonConvert.DeserializeObject<ServerCommon.InternalMessage>(msg);
#endif
            //Log.Information("ProcessInternalMessage {0}", msg);

            switch ((ServerCommon.InternalMessageType)ret.message_type)
            {
                case ServerCommon.InternalMessageType.Participant:
                    {
                        mPlayerAuth.Remove(ret.world_id);
                        mPlayerAuth.Add(ret.world_id, ret);

                        int player_count = ret.players.Where(x => x.Value.is_ai == false).Count();

                        Clear(ret.world_id);
                        World.Instance(ret.world_id).Reset(true, NetworkManagerServer.sInstance);
                        World.Instance(ret.world_id).GameMode.Init(ret.map_id, ret.match_id, player_count);
                        World.Instance(ret.world_id).GameMode.game_mode.SetModeData(ret);
                        mNewPlayerId[ret.world_id] = ret.players.Count + 1;

                        foreach (var player in ret.players)
                        {
                            if(player.Value.is_ai == true)
                            {
                                AIController aiController = CreateAI(player.Value.character_type, player.Value.user_id, ret.world_id, new System.Guid(player.Key), player.Value.team, player.Value.player_id, player.Value.spawn_index, player.Value.character_level);

                                // 게임 시작시 적당한 유저에게 할당
                                World.Instance(ret.world_id).GameMode.RegisterStartEvent(aiController.OnStart);
                            }
                            else
                            {
                                CreateActor(player.Value.character_type, player.Value.user_id, ret.world_id, new System.Guid(player.Key), player.Value.team, player.Value.player_id, player.Value.spawn_index, player.Value.character_level);
                            }

                        }
                    }
                    break;
                case ServerCommon.InternalMessageType.DebugCommand:
                    {
                        try
                        {
                            var actor = (SActor)World.Instance(ret.world_id).GetPlayer(ret.debug_command.ingame_player_id);
                            if(actor == null)
                            {
                                Log.Error($"not found player {ret.debug_command.ingame_player_id}, world_id {ret.world_id}");
                                return;
                            }

                            Battle.DebugCommand.Execute(actor, ret.debug_command.cmd, ret.debug_command.param1, ret.debug_command.param2, ret.debug_command.param3, ret.debug_command.param4);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }

                    }
                    break;
                case ServerCommon.InternalMessageType.SuspendChannel:
                    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
                        Cache.sInstance.Suspend();
#endif
                        Log.Information($"SuspendChannel");
                    }
                    break;
                case ServerCommon.InternalMessageType.ResumeChannel:
                    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
                        Cache.sInstance.Resume();
#endif
                        Log.Information($"ResumeChannel");
                    }
                    break;
            }
        }

        public ErrorCode CheckAuth(int world_id, string session_id)
        {
            ServerCommon.InternalMessage match_info;
            if (mPlayerAuth.TryGetValue(world_id, out match_info) == false)
            {
                Log.Information($"CheckAuth not exist world {world_id}");
                return ErrorCode.Auth;
            }

            ServerCommon.PlayerInfo player;
            if (match_info.players.TryGetValue(session_id, out player) == false)
            {
                Log.Information($"CheckAuth not exist session {session_id}");
                return ErrorCode.Auth;
            }

            //match_info.players[session_id].count = 1;

            //foreach (var p in match_info.players)
            //{
            //    if (p.Value.is_ai == true)
            //        continue;

            //    if (p.Value.count == 0)
            //    {
            //        Log.Information("CheckAuth not yet {0}", p.Key);
            //        return ErrorCode.Wait;
            //    }
            //}
            return ErrorCode.Success;
        }

        public ServerCommon.PlayerInfo GetPlayerInfo(int world_id, string session_id)
        {
            ServerCommon.InternalMessage match_info;
            if (mPlayerAuth.TryGetValue(world_id, out match_info) == false)
            {
                Log.Information($"GetPlayerInfo cannot find world {world_id}");
                return null;
            }

            ServerCommon.PlayerInfo player;
            if (match_info.players.TryGetValue(session_id, out player) == false)
            {
                Log.Information($"GetPlayerInfo cannot find session {session_id}");
                return null;
            }
            return player;
        }

        public int GetReservedPlayerCount(int world_id)
        {
            ServerCommon.InternalMessage match_info;
            if (mPlayerAuth.TryGetValue(world_id, out match_info) == false)
            {
                return -1;
            }
            return match_info.players.Count;
        }

        public string GetChannelId(int world_id)
        {
            ServerCommon.InternalMessage match_info;
            if (mPlayerAuth.TryGetValue(world_id, out match_info) == false)
            {
                return "";
            }
            return match_info.channel_id;
        }

        public void SendOutgoingPackets()
        {
            float time = Timing.sInstance.GetTimef();
            //Log.Information($"mAddressToClientMap count{mAddressToClientMap.Count}");

            //let's send a client a state packet whenever their move has come in...
            foreach (var pair in mAddressToClientMap)
            {
                //process any timed out packets while we're going through the list
                pair.Value.GetDeliveryNotificationManager().ProcessTimedOutPackets();
                //Log.Information($"SendOutgoingPackets {pair.Value.IsLastMoveTimestampDirty()}, {pair.Value.UpdateSendingStatePacket()}");
                if (pair.Value.IsLastMoveTimestampDirty() && pair.Value.UpdateSendingStatePacket())
                {
                    SendStatePacketToClient(pair.Value);
                }
            }
        }

        [Obsolete("This function is obsolete; use function OnDisconnected")]
        public void CheckForDisconnects()
        {
            List<PlayerController> clientsToDC = new List<PlayerController>();

            float minAllowedLastPacketFromClientTime = Timing.sInstance.GetTimef() - mClientDisconnectTimeout;
            foreach (var pair in mAddressToClientMap)
            {
                if (pair.Value.GetLastPacketFromClientTime() < minAllowedLastPacketFromClientTime)
                {
                    //can't remove from map while in iterator, so just remember for later...
                    clientsToDC.Add(pair.Value);
                }
            }

            foreach (var client in clientsToDC)
            {
                HandleClientDisconnected(client);
            }
        }

        public override void OnDisconnected(NetIncomingMessage inInputStream)
        {
            // todo : 여러 소켓이 하나의 세션아이디를 가지고 있을 경우 이전 소켓으로 인해 현재 세션이 날라갈수 있다
            // 레퍼런스 카운트등의 처리로 세션 종료 유무를 판단해야 할 것으로 보임
            PlayerController c = null;
            mAddressToClientMap.TryGetValue(inInputStream.GetSessionId(), out c);
            if (c != null)
            {
                if (c.IsTcp != inInputStream.isTcp)
                {
                    Log.Information($"disconnect is diff protocol ctr:{c.GetIpAddress()}, msg{inInputStream.m_senderEndPoint}");
                    return;
                }

                if (c.GetIpAddressRef() != inInputStream.m_senderEndPoint)
                {
                    Log.Information($"disconnect is diff session {c.GetIpAddress()}, msg{inInputStream.m_senderEndPoint}");
                    return;
                }

                HandleClientDisconnected(c);
            }
        }


        public void RegisterGameObject(NetGameObject inGameObject, byte worldId)
        {
            if (CheckWorldId(inGameObject.WorldId) == false)
            {
                Log.Error($"RegisterGameObject error worldId{inGameObject.WorldId}");
                return;
            }

            //assign network id
            int newNetworkId = GetNewNetworkId(worldId);
            inGameObject.SetNetworkId(newNetworkId);
            inGameObject.WorldId = worldId;

            //add mapping from network id to game object
            mNetworkIdToGameObjectMap[worldId][newNetworkId] = inGameObject;

            //tell all client proxies this is new...
            foreach (var pair in mAddressToClientMap)
            {
                if (pair.Value.IsStartedPlay && pair.Value.GetWorldId() == worldId)
                {
                    pair.Value.GetReplicationManagerServer().ReplicateCreate(newNetworkId, inGameObject.GetAllStateMask());
                }
            }
        }
        public void UnregisterGameObject(NetGameObject inGameObject)
        {
            if(CheckWorldId(inGameObject.WorldId)==false)
            {
                Log.Error($"UnregisterGameObject error networID{inGameObject.GetNetworkId()}, worldId{inGameObject.WorldId}");
                return;
            }

            int networkId = inGameObject.GetNetworkId();
            mNetworkIdToGameObjectMap[inGameObject.WorldId].Remove(networkId);

            //Log.Information($"remove game object {networkId}");

            //tell all client proxies to STOP replicating!
            //tell all client proxies this is new...
            foreach (var pair in mAddressToClientMap)
            {
                if (pair.Value.IsStartedPlay && inGameObject.WorldId == pair.Value.GetWorldId())
                {
                    pair.Value.GetReplicationManagerServer().ReplicateDestroy(networkId);
                }
            }
        }

        public void CancelGameObject(NetGameObject inGameObject)
        {
            int networkId = inGameObject.GetNetworkId();
            foreach (var pair in mAddressToClientMap)
            {
                if (pair.Value.IsStartedPlay && inGameObject.WorldId == pair.Value.GetWorldId())
                {
                    pair.Value.GetReplicationManagerServer().RemoveFromReplication(networkId);
                }
            }
        }

        public void SetStateDirty(int inNetworkId, byte worldId, uint32_t inDirtyState)
        {
            //tell everybody this is dirty
            foreach (var pair in mAddressToClientMap)
            {
                if (pair.Value.IsStartedPlay && worldId == pair.Value.GetWorldId())
                {
                    pair.Value.GetReplicationManagerServer().SetStateDirty(inNetworkId, inDirtyState);
                }
            }
        }

        public void SetStateDirtyExcept(int ignorePlayerId, int inNetworkId, byte worldId, uint32_t inDirtyState)
        {
            //tell everybody this is dirty
            foreach (var pair in mAddressToClientMap)
            {
                if (pair.Value.IsStartedPlay && worldId == pair.Value.GetWorldId() && ignorePlayerId != pair.Value.GetPlayerId())
                {
                    pair.Value.GetReplicationManagerServer().SetStateDirty(inNetworkId, inDirtyState);
                }
            }
        }

        public void UpdatePlayer()
        {
            List<PlayerController> players = null;

            foreach (var c in mAddressToClientMap)
            {
                //c.Value.RespawnActorIfNecessary();
                if(c.Value.mExpirePlayTime < Timing.sInstance.GetFrameStartTime())
                {
                    if(players == null)
                        players = new List<PlayerController>();

                    players.Add(c.Value);
                }

            }

            if (players != null)
            {
                foreach (var client in players)
                {
                    Log.Information($"expried time disconnect {client.GetIpAddress()}");
                    HandleClientDisconnected(client);
                }
            }
        }

        public PlayerController GetPlayerController(int inPlayerId)
        {
            PlayerController c = null;
            if (mPlayerIdToClientMap.TryGetValue(inPlayerId, out c) == true)
            {
                return c;
            }

            return null;
        }

        public void SendPacket(int inPlayerId, NetBuffer inOutputStream)
        {
            var c = GetPlayerController(inPlayerId);
            if (c != null)
            {
                if(c.SendMessage((NetOutgoingMessage)inOutputStream, NetDeliveryMethod.ReliableOrdered) ==false)
                {
                    HandleClientDisconnected(c);
                }
            }
        }


        void ProcessPacket(PlayerController inPlayerController, NetIncomingMessage inInputStream)
        {
            //remember we got a packet so we know not to disconnect for a bit

            uint32_t packetType;
            packetType = inInputStream.ReadUInt32(PacketTypeLengthBits);
            //Log.Information($"ProcessPacket {(PacketType)packetType}");
            switch ((PacketType)packetType)
            {
                case PacketType.kHello:
                    //need to resend welcome. to be extra safe we should check the name is the one we expect from this address,
                    //otherwise something weird is going on...
                    //SendWelcomePacket(inPlayerController);
                    HandlePacketFromNewClient(inInputStream);
                    break;
                case PacketType.kStartPlay:
                    if (inPlayerController != null)
                    {
                        inPlayerController.IsTcp = inInputStream.isTcp;
                        HandlePacketStartPlay(inPlayerController, inInputStream);
                    }
                    break;
#if _USE_INPUT_SYNC
                case PacketType.kInput:
                    if (inPlayerController != null)
                    {
                        inPlayerController.IsTcp = inInputStream.isTcp;

                        inPlayerController.UpdateLastPacketTime();

                        if (inPlayerController.GetDeliveryNotificationManager().ReadAndProcessState(inInputStream))
                        {
                            HandleInputPacket(inPlayerController, inInputStream);

                            if(inPlayerController.GetReplicationManagerServer().IsCompleteCreateDelivery(inPlayerController))
                            {
                                SendReadyPlayPacket(inPlayerController);
                            }
                        }
                    }
                    break;
#else
                case PacketType.kState:
                    if (inPlayerController != null)
                    {
                        inPlayerController.IsTcp = inInputStream.isTcp;

                        inPlayerController.UpdateLastPacketTime();

                        if (inPlayerController.GetDeliveryNotificationManager().ReadAndProcessState(inInputStream))
                        {
                            HandleStatePacket(inPlayerController, inInputStream);

                            if (inPlayerController.GetReplicationManagerServer().IsCompleteCreateDelivery(inPlayerController))
                            {
                                SendReadyPlayPacket(inPlayerController);
                            }
                        }
                    }
                    break;
#endif
                case PacketType.kRPC:
                    if (inPlayerController != null)
                    {
                        inPlayerController.IsTcp = inInputStream.isTcp;

                        HandleRemoteProcedureCall(inPlayerController, inInputStream);
                    }
                    break;
                default:
                    //LOG("Unknown packet type received from %s", inPlayerController.GetSocketAddress().ToString().c_str());
                    break;
            }

#if USE_STATISTICS
            processedBytes += inInputStream.LengthBytes;
            processedPackets += 1;
#endif
        }

        string GetProtocol(NetIncomingMessage inInputStream)
        {
            return inInputStream.isTcp ? "TCP" : "UDP";
        }

        void HandlePacketFromNewClient(NetIncomingMessage inInputStream)
        {
            try
            {
                //read the name
                string str_session_id = inInputStream.ReadString();
                byte worldId = inInputStream.ReadByte();
                byte selectedCharacter = inInputStream.ReadByte();
                string UserId = "";
                byte team = (byte)MathHelpers.GetRandomInt((int)core.BaseStruggleTeam.TeamCount);
                int player_id = 0;
                int spawnIndex = -1;
                int characterLevel = 1;

#if USE_TICK_COUNTER
                float clientTimeStamp = inInputStream.ReadFloat();
#endif
                bool IsDebugUser = inInputStream.ReadBoolean();
                if(IsDebugUser)
                {
                    var in_team = inInputStream.ReadByte();
                    var in_player_id = inInputStream.ReadInt32();
                    var in_UserId = inInputStream.ReadString();
                    var in_spawnIndex = inInputStream.ReadInt32();

                    if(IsPermitDebugUser)
                    {
                        team = in_team;
                        player_id = in_player_id;
                        UserId = in_UserId;
                        spawnIndex = in_spawnIndex;

                        foreach (var player in World.Instance(worldId).playerList.Values)
                        {
                            player.InvokeClientRpcOnClient(player.JoinDebugUser, (int)player.GetPlayerId(), selectedCharacter, in_team, in_player_id, in_UserId, in_spawnIndex);
                        }
                    }
                }



                // 테스트용 접속을 위해 인증은 잠시 꺼둔다.
                if (IsBattleAuth)
                {
                    var ret = CheckAuth((int)worldId, str_session_id);
                    if (ret != ErrorCode.Success)
                    {
                        Log.Information($"HandlePacketFromNewClient auth fail session {str_session_id} world {worldId}");

                        var welcomePacket = GetServer().CreateMessage();
                        welcomePacket.Write((uint32_t)PacketType.kWelcome, PacketTypeLengthBits);
                        welcomePacket.Write((byte)ErrorCode.Wait);
                        PlayerController.SendMessage(inInputStream.SenderConnection, inInputStream.m_TcpConnecton, inInputStream.isTcp, welcomePacket, NetDeliveryMethod.Unreliable);
                        return;
                    }
                }

                // 로비에서 세팅한 유저 정보 얻기
                var playerInfo = GetPlayerInfo((int)worldId, str_session_id);
                if (playerInfo != null)
                {
                    selectedCharacter = playerInfo.character_type;
                    UserId = playerInfo.user_id;
                    team = playerInfo.team;
                    player_id = playerInfo.player_id;
                    spawnIndex = playerInfo.spawn_index;
                    characterLevel = playerInfo.character_level;
                }

                // 월드 아이디로 체널 아이디 세팅, 게임 종료시 해당 키값으로 게임 결과를 송신
                SGameMode gameMode = (SGameMode)World.Instance(worldId).GameMode;
                gameMode.channel_id = GetChannelId(worldId);

                var session_id = new Guid(str_session_id);
                PlayerController newPlayerController = null;
                PlayerController existPlayerController = null;

                // 세션 아이디로 기존에 접속했던 유저인지 확인
                mAddressToClientMap.TryGetValue(session_id, out existPlayerController); 
                if (newPlayerController== null)
                {
                    newPlayerController = new PlayerController(session_id, player_id != 0 ? player_id : GetNewPlayerId(worldId));
                    mAddressToClientMap[session_id] = newPlayerController;
                    mPlayerIdToClientMap[newPlayerController.GetPlayerId()] = newPlayerController;

                    Log.Information($"new connect session {str_session_id}, playerID{newPlayerController.GetPlayerId()}, userID{UserId}, world {worldId}, stream {inInputStream.SenderConnection?.RemoteUniqueIdentifier.ToString()}, protocol {GetProtocol(inInputStream)}");
                }
                else
                {
                    newPlayerController = new PlayerController(session_id, player_id != 0 ? player_id : GetNewPlayerId(worldId));

                    // 이전 플레이어컨트롤러 제거
                    HandleClientDisconnected(existPlayerController, newPlayerController);

                    mAddressToClientMap[session_id] = newPlayerController;
                    mPlayerIdToClientMap[newPlayerController.GetPlayerId()] = newPlayerController;

                    Log.Information($"reconnect session {str_session_id}, playerID{newPlayerController.GetPlayerId()}, userID{UserId}, world {worldId}, stream {inInputStream.SenderConnection?.RemoteUniqueIdentifier.ToString()}, protocol {GetProtocol(inInputStream)}");
                }

                // 소켓과 세션 아이디 매칭
                if(inInputStream.SetSessionId(session_id)==false)
                {
                    Log.Error($"cannot find session {session_id.ToString()}");
                }

                newPlayerController.Set(inInputStream.SenderConnection, inInputStream.m_TcpConnecton, inInputStream.isTcp, UserId, team, worldId, selectedCharacter, spawnIndex, characterLevel);

                //and welcome the client...
                SendWelcomePacket(newPlayerController
#if USE_TICK_COUNTER
                    , clientTimeStamp
#endif
                    , gameMode.state == GameMode.GameModeState.Play
                    );
            }
            catch (Exception ex)
            {
                Log.Information(ex.ToString());
            }


        }

        void HandlePacketStartPlay(PlayerController inPlayerController, NetIncomingMessage inInputStream)
        {
            try
            {
                //read the name
                string name = inInputStream.ReadString();
                byte worldId = inInputStream.ReadByte();
                byte selectedCharacter = inInputStream.ReadByte();



                //tell the server about this client, spawn a cat, etc...
                //if we had a generic message system, this would be a good use for it...
                //instead we'll just tell the server directly
                int networkId = inPlayerController.HandleNewClient();



                //and now init the replication manager with everything we know about!
                foreach (var pair in mNetworkIdToGameObjectMap[worldId])
                {
                    inPlayerController.GetReplicationManagerServer().ReplicateCreate(pair.Key, pair.Value.GetAllStateMask());
                }

                inPlayerController.GetReplicationManagerServer().isFirst = false;
                inPlayerController.GetReplicationManagerServer().firstReplicationTimeout = Timing.sInstance.GetFrameStartTime() + ReplicationManagerServer.DefaultFirstReplicationTimeout;

                SendStartPlayPacket(inPlayerController);
            }
            catch (Exception ex)
            {
                Log.Information(ex.ToString());
            }


        }

        void SendWelcomePacket(PlayerController inPlayerController
#if USE_TICK_COUNTER
            , float clientTimeStamp
#endif
            , bool isRush
            )
        {
            var welcomePacket = GetServer().CreateMessage();

            welcomePacket.Write((uint32_t)PacketType.kWelcome, PacketTypeLengthBits);
            welcomePacket.Write((byte)ErrorCode.Success);
            welcomePacket.Write(inPlayerController.GetPlayerId());
#if USE_TICK_COUNTER
            welcomePacket.Write(World.Instance(inPlayerController.GetWorldId()).mTickCounter.TickNumber);
            welcomePacket.Write(clientTimeStamp);
#endif
            welcomePacket.Write(isRush);

            Log.Information($"Server Welcoming, new client {inPlayerController.mSessionId} as player {inPlayerController.GetPlayerId()}");

            inPlayerController.SendMessage(welcomePacket, NetDeliveryMethod.Unreliable);
        }

        void SendStartPlayPacket(PlayerController inPlayerController)
        {
            var welcomePacket = GetServer().CreateMessage();

            welcomePacket.Write((uint32_t)PacketType.kStartPlay, PacketTypeLengthBits);
            welcomePacket.Write((byte)ErrorCode.Success);
            welcomePacket.Write(inPlayerController.GetPlayerId());

            Log.Information($"Server StartPlay, new client {inPlayerController.mSessionId} as player {inPlayerController.GetPlayerId()}");

            inPlayerController.SendMessage(welcomePacket, NetDeliveryMethod.Unreliable);
        }

        void SendReadyPlayPacket(PlayerController inPlayerController)
        {
            var welcomePacket = GetServer().CreateMessage();

            welcomePacket.Write((uint32_t)PacketType.kReadyPlay, PacketTypeLengthBits);


            Log.Information($"Server ReadyPlay, new client {inPlayerController.mSessionId} as player {inPlayerController.GetPlayerId()}");

            inPlayerController.SendMessage(welcomePacket, NetDeliveryMethod.ReliableOrdered);
        }

        void UpdateAllClients()
        {
            foreach (var it in mAddressToClientMap)
            {
                //process any timed out packets while we're going throug hthe list
                it.Value.GetDeliveryNotificationManager().ProcessTimedOutPackets();

                SendStatePacketToClient(it.Value);
            }
        }

        void AddWorldStateToPacket(byte worldId, NetOutgoingMessage inOutputStream)
        {
            var gameObjects = World.Instance(worldId).GetGameObjects();

            //now start writing objects- do we need to remember how many there are? we can check first...
            inOutputStream.Write(gameObjects.Count);

            foreach (var gameObject in gameObjects)
            {
                inOutputStream.Write(gameObject.GetNetworkId());
                inOutputStream.Write(gameObject.GetClassId());
                gameObject.Write(inOutputStream, 0xffffffff);
            }
        }

        void SendStatePacketToClient(PlayerController inPlayerController)
        {
            int network_id = 0;
            do
            {
                //Log.Information($"SendStatePacketToClient");
                //build state packet
                var statePacket = GetServer().CreateMessage();

                //it's state!
                statePacket.Write((uint32_t)PacketType.kState, PacketTypeLengthBits);

                InFlightPacket ifp = inPlayerController.GetDeliveryNotificationManager().WriteState(statePacket);

                WriteLastMoveTimestampIfDirty(statePacket, inPlayerController);

                var rmtd = new ReplicationManagerTransmissionData(inPlayerController.GetReplicationManagerServer());
                network_id = inPlayerController.GetReplicationManagerServer().Write(statePacket, rmtd, network_id, inPlayerController.GetWorldId());
                ifp.SetTransmissionData((int)TransmissionDataType.kReplicationManager, rmtd);

                inPlayerController.SendMessage(statePacket, NetDeliveryMethod.Unreliable);


                //Debug.Log($"SendStatePacketToClient PlayerId:{inPlayerController.GetPlayerId()}, network_id:{network_id}");


            } while (network_id != 0);
        }


        void WriteLastMoveTimestampIfDirty(NetOutgoingMessage inOutputStream, PlayerController inPlayerController)
        {
            //first, dirty?
            bool isTimestampDirty = inPlayerController.IsLastMoveTimestampDirty();
            inOutputStream.Write(isTimestampDirty);
            if (isTimestampDirty)
            {
#if _USE_INPUT_SYNC
                inOutputStream.Write(inPlayerController.GetUnprocessedMoveList().GetLastMoveTimestamp());
#else

                // 서버측에 가장 최근 처리된 메시지의 타임 스템프 클라이언트에 알림 (RTT 구하는 용도) 
                if(inPlayerController.mCharacterState!=null)
                    inOutputStream.Write(inPlayerController.mCharacterState.timeStamp);
                else
                    inOutputStream.Write(0f);
#endif

                inPlayerController.SetIsLastMoveTimestampDirty(false);
            }
        }

        void HandleInputPacket(PlayerController inPlayerController, NetIncomingMessage inInputStream)
        {
            uint32_t moveCount = inInputStream.ReadUInt32(2);
            if (moveCount == 0)
            {
                inPlayerController.SetIsLastMoveTimestampDirty(true);
                return;
            }

            for (; moveCount > 0; --moveCount)
            {
                Move move = new Move();
                if (move.Read(inInputStream))
                {
                    //log.InfoFormat("recv move {0}, {1}, {2}, {3}", move.GetDeltaTime(), move.GetInputState(), move.GetTimestamp(), moveCount);
                    //Log.Information("recv move {0}",  move.GetInputState());
                    if (inPlayerController.GetUnprocessedMoveList().AddMoveIfNew(move))
                    {
                        inPlayerController.SetIsLastMoveTimestampDirty(true);

                        //Log.Information($"move list size {inPlayerController.GetUnprocessedMoveList().mMoves.Count}");
                    }
                }
            }
        }

        void HandleStatePacket(PlayerController inPlayerController, NetIncomingMessage inInputStream)
        {
            try
            {
                //Log.Information($"recv0 location{inPlayerController?.mCharacterState?.location}");

                //bool changeState = inInputStream.ReadBoolean();
                //if (changeState == false)
                //{
                //    inPlayerController.SetIsLastMoveTimestampDirty(true);
                //    return;
                //}

                inPlayerController.mReadCharacterState.Read(inInputStream);

                // AI 상태 정보 읽기
                inPlayerController.mAIState.Read(inInputStream);


                if (inPlayerController.mCharacterState == null)
                {
                    inPlayerController.mCharacterState = new CharacterState();
                    inPlayerController.SwapCharacterState();
                    inPlayerController.SetIsLastMoveTimestampDirty(true);
                    //Log.Information($"recv1 location{inPlayerController?.mCharacterState?.location}");

                    inPlayerController.RefreshAIState();
                }
                else if (inPlayerController.mReadCharacterState.timeStamp > inPlayerController.mCharacterState.timeStamp)
                {
                    inPlayerController.SwapCharacterState();
                    inPlayerController.SetIsLastMoveTimestampDirty(true);
                    //Log.Information($"recv2 location{inPlayerController?.mCharacterState?.location}");

                    inPlayerController.RefreshAIState();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        void HandleRemoteProcedureCall(PlayerController inPlayerController, NetIncomingMessage inInputStream)
        {
            try
            {
                int networkId = inInputStream.ReadInt32();
                ulong hash = inInputStream.ReadUInt64();
                int senderClientId = inPlayerController.GetPlayerId();
                //core.LogHelper.LogInfo($"HandleRemoteProcedureCall {networkId}, {hash}, {senderClientId}");

                NetGameObject obj;
                if (mNetworkIdToGameObjectMap[inPlayerController.GetWorldId()].TryGetValue(networkId, out obj) == true)
                {
                    obj.OnRemoteServerRPC(hash, senderClientId, inInputStream);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        void HandleClientDisconnected(PlayerController inPlayerController, PlayerController newPlayerController = null)
        {

            mPlayerIdToClientMap.Remove(inPlayerController.GetPlayerId());
            mAddressToClientMap.Remove(inPlayerController.mSessionId);
            int networkId = inPlayerController.HandleLostClient(newPlayerController);

            Log.Information($"HandleClientDisconnected client networkID{networkId}, sessionID{inPlayerController.mSessionId} as player {inPlayerController.GetPlayerId()}, ip{inPlayerController.GetIpAddress()}, addr_map{mAddressToClientMap.Count}, id_map{mPlayerIdToClientMap.Count}");


            //was that the last client? if so, bye!
            if (mAddressToClientMap.Count == 0)
            {
                //Engine.sInstance.SetShouldKeepRunning(false);
            }
        }

        int GetNewNetworkId(byte worldId)
        {
            int toRet = mNewNetworkId[worldId]++;
            if (mNewNetworkId[worldId] == 0)
            {
                // todo : 네트워크 아이디 풀링이 필요
                // 넘어가는게 확인 된다면
                mNewNetworkId[worldId]++;
                Log.Error($"Network ID overflow");
            }

            return toRet;
        }
        void ResetNewNetworkId(byte worldId)
        {
            mNewNetworkId[worldId] = 1;
        }

        int GetNewPlayerId(byte worldId)
        {
            return core.MathHelpers.MakeDWord(worldId, (ushort)mNewPlayerId[worldId]++);
        }

        public static NetBuffer CreateRpcPacket(int clientId)
        {
            //build state packet
            var rpcPacket = NetworkManagerServer.sInstance.GetServer().CreateMessage();

            //it's rpc!
            rpcPacket.Write((UInt32)PacketType.kRPC, PacketTypeLengthBits);

            return rpcPacket;
        }

        public static void Send(int clientId, NetBuffer inOutputStream)
        {
            NetworkManagerServer.sInstance.SendPacket(clientId, inOutputStream);
        }

#if USE_STATISTICS
        float lastTime = 0.0f;
        int lastReceivedBytes = 0;
        int lastReceivedMessages = 0;
        int lastReceivedPackets = 0;
        int lastSentBytes = 0;
        int lastSentMessages = 0;
        int lastSentPackets = 0;
        int processedBytes = 0;
        int processedPackets = 0;
        public void ShowStatistics()
        {
            lastTime += Timing.sInstance.GetDeltaTime();
            if (lastTime >= 1.0f)
            {
                int receivedBytesPerSecond = mNetPeer.Statistics.ReceivedBytes - lastReceivedBytes;
                int receivedMessagesPerSecond = mNetPeer.Statistics.ReceivedMessages - lastReceivedMessages;
                int receivedPacketsPerSecond = mNetPeer.Statistics.ReceivedPackets - lastReceivedPackets;
                int sentBytesPerSecond = mNetPeer.Statistics.SentBytes - lastSentBytes;
                int sentMessagesPerSecond = mNetPeer.Statistics.SentMessages - lastSentMessages;
                int sentPacketsPerSecond = mNetPeer.Statistics.SentPackets - lastSentPackets;

                Log.Information($"timeInterval {lastTime}, Received {receivedBytesPerSecond} bytes, {receivedMessagesPerSecond} messages, {receivedPacketsPerSecond} packets, Sent {sentBytesPerSecond} bytes, {sentMessagesPerSecond} messages, {sentPacketsPerSecond} packets, Processed {processedBytes} bytes, {processedPackets} packets per second, storage has been allocated {mNetPeer.Statistics.StorageBytesAllocated} bytes, recycled pool has {mNetPeer.Statistics.BytesInRecyclePool} bytes, {mNetPeer.ConnectionsCount} clients has been connected");

                lastTime = 0.0f;
                lastReceivedBytes = mNetPeer.Statistics.ReceivedBytes;
                lastReceivedMessages = mNetPeer.Statistics.ReceivedMessages;
                lastReceivedPackets = mNetPeer.Statistics.ReceivedPackets;
                lastSentBytes = mNetPeer.Statistics.SentBytes;
                lastSentMessages = mNetPeer.Statistics.SentMessages;
                lastSentPackets = mNetPeer.Statistics.SentPackets;
                processedBytes = 0;
                processedPackets = 0;
            }
                
        }
#endif
    }
}
