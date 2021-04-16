using Hazel;
using Hazel.Tcp;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
//using AceStudio.Util;

public class NetworkManagerClient : core.NetworkManager
{
    public enum NetworkClientState
    {
        Uninitialized,
        SayingHello,
        Welcomed,
        Play,
    };

    static readonly float kTimeBetweenHellos = 1.0f;
    static readonly float kTimeBetweenStartPlay = 1.0f;

    static readonly float kTimeBetweenInputPackets = 0.066f; // 15 FPS


    protected Connection connection = null;

    public bool respawn = false;

    public bool IsTcp = false;
    public bool IsUdpOk = false;
    public bool IsTrySend = true;
    public bool IsChangeScene = true;
    public int tryConnectCount = 0;
    NetEncryption algo = null;

    bool IsDebugUser = false;
    int DebugUserTeam;
    int DebugPlayerId;
    string DebugUserName;


    // player id, AI state
    public Dictionary<int, core.AIState> AIStates = new Dictionary<int, core.AIState>();


    public static NetworkManagerClient sInstance { get { return (NetworkManagerClient)Instance; } }


    public static void StaticInit()
    {
        if (core.NetworkManager.Instance == null)
        {
            core.NetworkManager.Instance = new NetworkManagerClient();
        }
    }

    private bool isBattleSceneLoadDone = true;
    public bool _isBattleSceneLoadDone
    {
        get { return isBattleSceneLoadDone; }
        set { isBattleSceneLoadDone = value; }
    }

    private bool isRush = false;
    public bool IsRush { get { return isRush; } }

    core.DeliveryNotificationManager mDeliveryNotificationManager;
    public core.DeliveryNotificationManager DeliveryNotificationManager { get { return mDeliveryNotificationManager; } }

    ReplicationManagerClient mReplicationManagerClient = new ReplicationManagerClient();

    public Dictionary<int, Action> LinkedObject = new Dictionary<int, Action>();


    public NetClient GetClient() { return (NetClient)mNetPeer; }

    System.Net.IPEndPoint mServerAddress;
    public System.Net.IPEndPoint ServerAddress { get { return mServerAddress; } }


    NetworkClientState mState;
    public NetworkClientState State { get { return mState; } }

    float mTimeOfLastHello;
    float mTimeOfLastStartPlay;
    float mTimeOfLastInputPacket;

    string mName;
    int mPlayerId 
    { 
        get;
        set;
    }
    byte mWorldId;

    float mLastMoveProcessedByServerTimestamp;

    public core.WeightedTimedMovingAverage mAvgRoundTripTime;
    float mLastRoundTripTime;

    public INetCharacter LocalCharacter;


    public void RegisterLinkedObjectEvent(int networkId, Action action)
    {
        Action lastAction;
        if (LinkedObject.TryGetValue(networkId, out lastAction))
        {
            LinkedObject[networkId] += action;
        }
        else
        {
            LinkedObject[networkId] = action;
        }
    }

    public void ExecuteLinkedObjectEvent(int networkId)
    {
        Action action = null;
        if (LinkedObject.TryGetValue(networkId, out action))
        {
            Debug.Log($"ready action");
            action();
            LinkedObject.Remove(networkId);
        }
    }

    public void SendOutgoingPackets()
    {
        switch (mState)
        {
            case NetworkClientState.SayingHello:
                UpdateSayingHello();
                break;
            case NetworkClientState.Welcomed:
                UpdateStartPlay();
                break;
            case NetworkClientState.Play:
                UpdateSendingInputPacket();
                break;
        }
    }

    public override bool ReadPacket(NetIncomingMessage inInputStream, System.Net.IPEndPoint inFromAddress)
    {
        var packetType = (core.PacketType)inInputStream.ReadUInt32(PacketTypeLengthBits);
        switch (packetType)
        {
            case core.PacketType.kWelcome:
                HandleWelcomePacket(inInputStream);
                break;
            case core.PacketType.kStartPlay:
                HandleStartPlayPacket(inInputStream);
                break;
            case core.PacketType.kReadyPlay:
                HandleReadyPlayPacket(inInputStream);
                break;
            case core.PacketType.kState:
                if (mDeliveryNotificationManager.ReadAndProcessState(inInputStream))
                {
                    HandleStatePacket(inInputStream);
                }
                break;
            case core.PacketType.kRPC:
                HandleRPCPacket(inInputStream);
                break;
        }
        return true;
    }

    public core.WeightedTimedMovingAverage GetAvgRoundTripTime() { return mAvgRoundTripTime; }
    public float GetRoundTripTime() { return mAvgRoundTripTime.GetValue(); }

    public override float GetRoundTripTimeClientSide() { return GetRoundTripTime(); }


    public int GetPlayerId() { return mPlayerId; }
    public float GetLastMoveProcessedByServerTimestamp() { return mLastMoveProcessedByServerTimestamp; }

    NetworkManagerClient()
    {
        mState = NetworkClientState.Uninitialized;
        mDeliveryNotificationManager = new core.DeliveryNotificationManager(true, false);
        mLastRoundTripTime = 0.0f;

        core.NetGameObject.CreateRpcPacketClient = NetworkManagerClient.CreateRpcPacket;
        core.NetGameObject.SendClient = NetworkManagerClient.Send;
    }
    public void Init(System.Net.IPEndPoint inServerAddress, string inName, byte inWorldId)
    {
        base.Init(core.World.DefaultWorldCount);

        // client
        NetPeerConfiguration config = new NetPeerConfiguration("game", inServerAddress.AddressFamily);

#if DEBUG
        // 디버깅 환경에서 타임 아웃 처리 조정
        config.ConnectionTimeout = 300f;

        //if (Configuration.Instance.EnableLatencySimulation)
        //{
        //    config.SimulatedLoss = Configuration.Instance.SimulatedLoss;
        //    config.SimulatedRandomLatency = Configuration.Instance.SimulatedRandomLatency;
        //    config.SimulatedMinimumLatency = Configuration.Instance.SimulatedMinimumLatency;
        //    config.SimulatedDuplicatesChance = Configuration.Instance.SimulatedDuplicatesChance;
        //}
#endif 

        //config.AutoFlushSendQueue = false;
        mNetPeer = new NetClient(config);
        mNetPeer.Start();
        mDeliveryNotificationManager = new core.DeliveryNotificationManager(true, false);
        mReplicationManagerClient = new ReplicationManagerClient();

        algo = new NetXorEncryption(GetClient(), "AceTopSecret");

        mLastRoundTripTime = 0.0f;
        mTimeOfLastInputPacket = 0f;


        mServerAddress = inServerAddress;
        mState = NetworkClientState.SayingHello;
        mTimeOfLastHello = 0.0f;
        mTimeOfLastStartPlay = 0.0f;
        mName = inName;
        mWorldId = inWorldId;
        tryConnectCount = 0;

        mAvgRoundTripTime = new core.WeightedTimedMovingAverage(1.0f);

        NetOutgoingMessage hail = GetClient().CreateMessage("hail");
        GetClient().Connect(mServerAddress, hail);
        IsTcp = false;
        IsUdpOk = false;
        IsTrySend = true;

        respawn = false;

        // tcp
        SetConnector(inServerAddress);

        LinkedObject.Clear();
    }


    public void SetDebugUser(bool is_debug_user, string debug_user_name, byte debug_user_team, int debug_player_id)
    {
        IsDebugUser = is_debug_user;
        DebugUserName = debug_user_name;
        DebugUserTeam = debug_user_team;
        DebugPlayerId = debug_player_id;
    }

    void SetConnector(System.Net.IPEndPoint inServerAddress)
    {
        IPMode mode = IPMode.IPv4;
        if (inServerAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            mode = IPMode.IPv6;

        // tcp connect
        NetworkEndPoint ep = new NetworkEndPoint(inServerAddress.Address, inServerAddress.Port, mode);
        connection = new Hazel.Tcp.TcpConnection(ep);
        connection.Connect();

        connection.DataReceived += delegate (object innerSender, DataReceivedEventArgs innerArgs)
        {
            NetIncomingMessage msg = GetClient().CreateIncomingMessage(NetIncomingMessageType.Data, innerArgs.Bytes.Length);
            msg.isTcp = true;
            msg.m_TcpConnecton = (TcpConnection)innerSender;
            msg.m_senderEndPoint = ((NetworkEndPoint)((Connection)innerSender).EndPoint).EndPoint as IPEndPoint;
            Buffer.BlockCopy(innerArgs.Bytes, 2, msg.m_data, 0, innerArgs.Bytes.Length - 2);
            msg.m_bitLength = (ushort)((innerArgs.Bytes[0] << 8) | innerArgs.Bytes[1]);

            //core.LogHelper.LogInfo($"TCP recv message length {msg.m_bitLength}");
            GetClient().ReleaseMessage(msg);
        };

        connection.Disconnected += delegate (object sender2, DisconnectedEventArgs args2)
        {
            NetIncomingMessage msg = GetClient().CreateIncomingMessage(NetIncomingMessageType.TcpStatusChanged, 4 + 1);
            msg.isTcp = true;
            msg.m_senderEndPoint = ((NetworkEndPoint)((Connection)sender2).EndPoint).EndPoint as IPEndPoint;
            msg.Write((byte)NetConnectionStatus.Disconnected);
            msg.Write(string.Empty);

            //core.LogHelper.LogInfo($"TCP disconnected");

            GetClient().ReleaseMessage(msg);
        };
    }

    public void SendMessage(NetOutgoingMessage msg, NetDeliveryMethod method)
    {
        msg.Encrypt(algo);
        if (IsTcp)
        {
            connection.SendBytes(msg.Data, msg.LengthBits, Hazel.SendOption.Reliable);
            GetClient().Recycle(msg);
        }
        else
        {
            GetClient().SendMessage(msg, method);
        }
    }

    public void Reset()
    {
        if (connection != null)
        {
            connection.Close();
        }

        Debug.Log("try disconnect");
        if (GetClient() != null)
            GetClient().Shutdown("");
        else
            Debug.LogWarning("GetClient() is Null");
    }

    public override void OnDisconnected(NetIncomingMessage inInputStream)
    {
        if(IsTcp != inInputStream.isTcp)
        {
            return;
        }

        // todo : 재접속 화면으로 이동
        Debug.LogError($"OnDisconnected {mName}, {mWorldId}");

		//ACScrambleBattle.NETWORK_CONNECTOR.PushEvent( ACCMD_NETWORK_EVENT.EACEvent.DISCONNECTED );
		//ACScrambleBattle.SetState( EACBattleState.DISCONNECTED );
    }


    void UpdateSayingHello()
    {
        if (IsTrySend == false)
            return;

        float time = core.Timing.sInstance.GetTimef();

        if (time > mTimeOfLastHello + kTimeBetweenHellos)
        {
            SendHelloPacket();
            mTimeOfLastHello = time;
            ++tryConnectCount;
        }

    }

    void UpdateStartPlay()
    {
        if (IsTrySend == false)
            return;

        if (_isBattleSceneLoadDone == false)
        {
            Debug.Log("please waiting ... loading battle scene");
            return;
        }

        float time = core.Timing.sInstance.GetTimef();

        if (time > mTimeOfLastStartPlay + kTimeBetweenStartPlay)
        {
            SendStartPlayPacket();
            mTimeOfLastStartPlay = time;
        }

    }
    void SendHelloPacket()
    {
        NetOutgoingMessage helloPacket = GetClient().CreateMessage();

        helloPacket.Write((UInt32)core.PacketType.kHello, PacketTypeLengthBits);
        helloPacket.Write(mName);
        helloPacket.Write(mWorldId);
        //helloPacket.Write((byte)Configuration.Instance.eCharacterType);
        helloPacket.Write((byte)1);

#if USE_TICK_COUNTER
        helloPacket.Write(core.Timing.sInstance.GetTimef());
#endif

        helloPacket.Write(IsDebugUser);
        if (IsDebugUser)
        {
            helloPacket.Write((byte)DebugUserTeam);
            helloPacket.Write(DebugPlayerId);
            helloPacket.Write(DebugUserName);
            helloPacket.Write(1);
        }


        Debug.Log($"Send hello {mName}, {mWorldId}");
        SendMessage(helloPacket, NetDeliveryMethod.Unreliable);

#if UNITY_EDITOR && USE_CLIENT_STATE_RECORD

        System.IO.FileStream fs = new System.IO.FileStream($"client_record", System.IO.FileMode.Append, System.IO.FileAccess.Write);
        System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
        bw.Write((int)core.PacketType.kHello);
        bw.Write((byte)Configuration.Instance.eCharacterType);
        bw.Write(ACScrambleBattle.ENTRY.LocalCharacter.EntryChracterInfo.Team);
        bw.Write(ACScrambleBattle.ENTRY.LocalCharacter.EntryChracterInfo.SpawnIndex);
        bw.Close();
        fs.Close();

#endif








    }

    void SendStartPlayPacket()
    {
        NetOutgoingMessage helloPacket = GetClient().CreateMessage();

        helloPacket.Write((UInt32)core.PacketType.kStartPlay, PacketTypeLengthBits);
        helloPacket.Write(mName);
        helloPacket.Write(mWorldId);
        //helloPacket.Write((byte)Configuration.Instance.eCharacterType);
        helloPacket.Write((byte)1);

        Debug.Log($"Send start play {mName}, {mWorldId}");
        SendMessage(helloPacket, NetDeliveryMethod.Unreliable);
    }

    void HandleWelcomePacket(NetIncomingMessage inInputStream)
    {
        if (mState == NetworkClientState.SayingHello)
        {
            var code = (core.ErrorCode)inInputStream.ReadByte();
            switch (code)
            {
                case core.ErrorCode.Auth:
                    {
                        // 인증 실패는 즉시 실패 처리한다.
                        Debug.Log("Auth error");
                        //LobbyController.Instance.CancelWaitStartPlay();
                        IsTrySend = false;
                    }
                    break;
                case core.ErrorCode.Wait:
                    {
                        Debug.Log("Wait");
                        IsUdpOk = true;
                    }
                    break;
                case core.ErrorCode.Success:
                    {
                        //if we got a player id, we've been welcomed!
                        int playerId = (int)inInputStream.ReadUInt32();
#if USE_TICK_COUNTER
                        var tickNumber = inInputStream.ReadUInt32();
                        var rtt = core.Timing.sInstance.GetTimef() - inInputStream.ReadFloat();
                        core.World.Instance().mTickCounter.Correct(tickNumber, rtt);
#endif
                        isRush = inInputStream.ReadBoolean();
                        mPlayerId = playerId;
                        mState = NetworkClientState.Welcomed;
                        core.Engine.sInstance.ServerClientId = mPlayerId;
                        Debug.Log($"'{mName}' was welcomed on client as player playerId:{mPlayerId}, isRush:{isRush}");

                        //LobbyController.Instance.CancelWaitStartPlay();

						//_isBattleSceneLoadDone = false;

						//ACScrambleCommand.Instance.Execute( new ACCMD_NETWORK_EVENT( ACCMD_NETWORK_EVENT.EACEvent.PACKET_WELCOME_SUCCESS ) );
                    }
                    break;
            }

        }
    }

    void HandleStartPlayPacket(NetIncomingMessage inInputStream)
    {
        if (mState == NetworkClientState.Welcomed)
        {
            var code = (core.ErrorCode)inInputStream.ReadByte();
            switch (code)
            {
                case core.ErrorCode.Success:
                    {
                        //if we got a player id, we've been welcomed!
                        int playerId = (int)inInputStream.ReadUInt32();
                        mPlayerId = playerId;
                        mState = NetworkClientState.Play;
                        core.Engine.sInstance.ServerClientId = mPlayerId;
                        Debug.Log($"'{mName}' was welcomed on client as player {mPlayerId}");

                    }
                    break;
                default:
                    Debug.LogError($"HandleStartPlayPacket error {code}");
                    break;
            }

        }
    }

    void HandleReadyPlayPacket(NetIncomingMessage inInputStream)
    {
        Debug.Log($"Ready Play PlayerId:{mPlayerId}, PlayerCount:{core.World.Instance().playerList.Count}");
        core.World.Instance().ReadyPlay = true;
    }

    void HandleStatePacket(NetIncomingMessage inInputStream)
    {
        //Debug.Log($"HandleStatePacket PlayerId:{mPlayerId}, mState:{mState}");

        if (mState == NetworkClientState.Play)
        {
            ReadLastMoveProcessedOnServerTimestamp(inInputStream);

            //tell the replication manager to handle the rest...
            mReplicationManagerClient.Read(inInputStream);
        }
    }

    void HandleRPCPacket(NetIncomingMessage inInputStream)
    {
        Debug.Log("Handle RPC Packet ");

        int networkId = inInputStream.ReadInt32();
        ulong hash = inInputStream.ReadUInt64();

        core.NetGameObject obj;
        if (mNetworkIdToGameObjectMap[core.World.DefaultWorldIndex].TryGetValue(networkId, out obj) == true)
        {
            obj.OnRemoteClientRPC(hash, 0, inInputStream);
        }

    }

    void ReadLastMoveProcessedOnServerTimestamp(NetIncomingMessage inInputStream)
    {
        bool isTimestampDirty = inInputStream.ReadBoolean();
        if (isTimestampDirty)
        {
            mLastMoveProcessedByServerTimestamp = inInputStream.ReadFloat();

            float rtt = core.Timing.sInstance.GetFrameStartTime() - mLastMoveProcessedByServerTimestamp;
            mLastRoundTripTime = rtt;
            mAvgRoundTripTime.Update(rtt);

            //Debug.Log($"ReadLastMoveProcessedOnServerTimestamp rtt {mAvgRoundTripTime.GetValue()}");
        }
    }

    void UpdateSendingInputPacket()
    {
        float time = core.Timing.sInstance.GetTimef();

        if (time > mTimeOfLastInputPacket + kTimeBetweenInputPackets)
        {
#if _USE_INPUT_SYNC
            SendInputPacket();
#else
            SendStatePacket();
#endif
            mTimeOfLastInputPacket = time;
            //Debug.Log($"UpdateSendingInputPacket {time}");
        }
    }

    static int debug_index = 0;
	
    void SendStatePacket()
    {

        NetOutgoingMessage inputPacket = GetClient().CreateMessage();

        inputPacket.Write((UInt32)core.PacketType.kState, PacketTypeLengthBits);
        mDeliveryNotificationManager.WriteState(inputPacket);


        var characterState = new core.CharacterState();

        if (LocalCharacter != null)
        {
            characterState.location = LocalCharacter.GetPosition();
            characterState.velocity = LocalCharacter.GetVelocity();
        }
        characterState.timeStamp = core.Timing.sInstance.GetFrameStartTime();

        characterState.Write(inputPacket);

#if UNITY_EDITOR && USE_CLIENT_STATE_RECORD
            if(ACScrambleBattle.GetState().State == EACBattleState.PLAY)
            {
                System.IO.FileStream fs = new System.IO.FileStream($"client_record", System.IO.FileMode.Append, System.IO.FileAccess.Write);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                bw.Write((int)core.PacketType.kState);
                characterState.Write(bw);
                bw.Close();
                fs.Close();
            }
#endif

        inputPacket.Write((UInt32)AIStates.Count, (int)core.AIPlayer.MaxAIPlayerBit);
        foreach (var state in AIStates.Values)
        {
            state.Write(inputPacket);
        }

        //Debug.Log($"send location{characterState.location}, velocity{characterState.velocity}");


        SendMessage(inputPacket, NetDeliveryMethod.Unreliable);
    }

    public static NetBuffer CreateRpcPacket(int clientId)
    {
        //build state packet
        NetOutgoingMessage rpcPacket = NetworkManagerClient.sInstance.GetClient().CreateMessage();

        //it's rpc!
        rpcPacket.Write((UInt32)core.PacketType.kRPC, PacketTypeLengthBits);

        return rpcPacket;
    }

    public static void Send(int clientId, NetBuffer inOutputStream)
    {
        NetworkManagerClient.sInstance.SendMessage((NetOutgoingMessage)inOutputStream, NetDeliveryMethod.ReliableSequenced);
    }

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Destroy()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public static void Destroy()
	{
		Instance	= null;
	}
}
