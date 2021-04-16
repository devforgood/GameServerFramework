using core;
using Hazel.Tcp;
using Lidgren.Network;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class PlayerController : ActorController
    {
        static readonly float kRespawnDelay = 3.0f;
        public static bool EnableAiSwitch;
        public static float mTimeBetweenStatePackets;

        public NetConnection mConnection;
        public TcpConnection mTcpConnecton;
        public bool IsTcp = false;

        DeliveryNotificationManager mDeliveryNotificationManager;
        ReplicationManagerServer mReplicationManagerServer = new ReplicationManagerServer();

        //System.Net.IPEndPoint mSocketAddress;
        //public byte mIndex;
        public byte mTeam;
        public int mSpawnIndex;

        //going away!
        InputState mInputState;

        float mLastPacketFromClientTime;
        float mTimeToRespawn;

        bool mIsLastMoveTimestampDirty;

        float mTimeOfLastSatePacket;
        public bool IsStartedPlay;
        public float mExpirePlayTime;

        public Queue<NetIncomingMessage> MessageQueue = new Queue<NetIncomingMessage>();


        // ai 상태
        public AIStateManager mAIState = new AIStateManager();


        public PlayerController(Guid session_id, int inPlayerId)
        {
            mSessionId = session_id;
            mPlayerId = inPlayerId;
        }

        public void Set(NetConnection udp, TcpConnection tcp, bool isTcp, string userId, byte team, byte worldId, byte selectedCharacter, int spawnIndex, int characterLevel)
        {
            mConnection = udp;
            mTcpConnecton = tcp;
            IsTcp = isTcp;
            UserId = userId;
            mTeam = team;
            mSpawnIndex = spawnIndex;

            mReplicationManagerServer = new ReplicationManagerServer();
            mDeliveryNotificationManager = new DeliveryNotificationManager(false, true);
            mIsLastMoveTimestampDirty = false;
            mTimeToRespawn = 0.0f;
            mWorldId = worldId;
            mSelectedCharacter = selectedCharacter;
            mCharacterLevel = characterLevel;


            // todo : 임시코드 데모 시연용
            //mSelectedCharacter = (byte)(inPlayerId % 2);

            UpdateLastPacketTime();

            mTimeOfLastSatePacket = 0.0f;

            IsStartedPlay = false;
            
            // 게임 종료후 일정 시간 여유를 두고 강제 종료 처리를 한다.
            mExpirePlayTime = World.Instance(worldId).GameMode.EndTime + GameMode.ExpiredTime;

            mCharacterState = null;

            Log.Information($"PlayerController set last_user_id:{UserId}, cur_user_id:{userId}, team{team}, session:{mSessionId.ToString()}, ip:{GetIpAddress()}, lv:{characterLevel}");
        }


        public System.Net.EndPoint GetIpAddressRef()
        {
            if (IsTcp && mTcpConnecton != null)
                return mTcpConnecton.RemoteEndPoint;
            else if (IsTcp == false && mConnection != null)
                return mConnection.RemoteEndPoint;
            return null;
        }

        public string GetIpAddress()
        {
            if (IsTcp && mTcpConnecton != null)
                return mTcpConnecton.RemoteEndPoint.ToString();
            else if (IsTcp == false && mConnection != null)
                return mConnection.RemoteEndPoint.ToString();
            return "";
        }

        public bool UpdateSendingStatePacket()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

            return true;

#else
            float time = core.Timing.sInstance.GetTimef();

            if (time > mTimeOfLastSatePacket + mTimeBetweenStatePackets)
            {
                mTimeOfLastSatePacket = time;
                return true;
            }
            return false;
#endif
        }
        public void UpdateLastPacketTime()
        {
            mLastPacketFromClientTime = Timing.sInstance.GetTimef();
        }

        public void HandleActorDied()
        {
            mTimeToRespawn = Timing.sInstance.GetFrameStartTime() + kRespawnDelay;
         }

        //public void RespawnActorIfNecessary()
        //{
        //    if (mTimeToRespawn != 0.0f && Timing.sInstance.GetFrameStartTime() > mTimeToRespawn)
        //    {
        //        // 리스폰시 이전 키입력은 초기화 한다.
        //        GetUnprocessedMoveList().Clear();
        //        var actor = this.SpawnActor(mPlayerId, mWorldId, mIndex);
        //        if (actor != null)
        //        {
        //            World.Instance(mWorldId).playerList[mPlayerId] = actor;
        //        }
        //        mTimeToRespawn = 0.0f;
        //    }
        //}

        public int GetPlayerId() { return mPlayerId; }
        public byte GetWorldId() { return mWorldId; }

        public void SetInputState(InputState inInputState) { mInputState = inInputState; }
        public InputState GetInputState() { return mInputState; }

        public float GetLastPacketFromClientTime() { return mLastPacketFromClientTime; }

        public DeliveryNotificationManager GetDeliveryNotificationManager() { return mDeliveryNotificationManager; }
        public ReplicationManagerServer GetReplicationManagerServer() { return mReplicationManagerServer; }


        public void SetIsLastMoveTimestampDirty(bool inIsDirty) { mIsLastMoveTimestampDirty = inIsDirty; }
        public bool IsLastMoveTimestampDirty() { return mIsLastMoveTimestampDirty; }


        public bool SendMessage(NetOutgoingMessage msg, NetDeliveryMethod method)
        {
            bool result = true;
            if (IsTcp)
            {
                try
                {
                    mTcpConnecton.SendBytes(msg.Data, msg.LengthBits, Hazel.SendOption.Reliable);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    result = false;
                }
                NetworkManagerServer.sInstance.GetServer().Recycle(msg);
            }
            else
            {
                try
                {
                    var ret = NetworkManagerServer.sInstance.GetServer().SendMessage(msg, mConnection, method);
                    //Log.Information($"packet send to {mConnection.RemoteEndPoint}, ret {ret}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    result = false;
                }
            }
            return result;
        }

        public static void SendMessage(NetConnection con, TcpConnection tcp_con, bool isTcp, NetOutgoingMessage msg, NetDeliveryMethod method)
        {
            if (isTcp)
            {
                try
                {
                    tcp_con.SendBytes(msg.Data, msg.LengthBits, Hazel.SendOption.Reliable);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
                NetworkManagerServer.sInstance.GetServer().Recycle(msg);
            }
            else
            {
                NetworkManagerServer.sInstance.GetServer().SendMessage(msg, con, method);
            }
        }

        public void AbuseDisconnect()
        {
            Log.Information($"AbuseDisconnect playerId:{mPlayerId}");
        }

        public int HandleNewClient()
        {

            int playerId = this.GetPlayerId();
            byte worldId = this.GetWorldId();

            if (World.Instance(worldId).playerList.ContainsKey(playerId))
            {
                Log.Information($"already exist player {playerId}");
                return 0;
            }

            if (World.Instance(worldId).playerList.Count(x => ((SActor)x.Value).GetSessionId() == this.mSessionId) > 0)
            {
                Log.Information($"already exist player {playerId}, {this.mSessionId}");
                return 0;
            }

            SActor actor = null;
            if (EnableAiSwitch)
            {
                // 다른 플레이어가 AI로 제어중이라면 제어권을 다시 가져온다.
                actor = RestoreAI();
            }

            if(actor == null)
            {
                foreach(var entity in World.Instance(worldId).GameMode.GetEntries())
                {
                    if(entity.GetPlayerId() == playerId)
                    {
                        actor = (SActor)NetworkManagerServer.sInstance.GetGameObject(entity.mNetworkId, worldId);
                        Log.Information($"found reserved actor player id : {playerId}, session id : {this.mSessionId}, network id : {entity.mNetworkId}");
                        Possess(actor);
                        break;
                    }
                }
            }

            // AI 컨트롤러가 없었다면 새로운 객체 할당
            if (actor == null)
            {
                actor = SpawnActor(playerId, worldId, this.mTeam, this.mSpawnIndex);
                Possess(actor);
                //if (Possess(actor) == false)
                //{
                //    NetworkManagerServer.sInstance.UnregisterGameObject(actor);
                //    NetworkManagerServer.sInstance.CancelGameObject(actor);
                //    World.Instance(worldId).RemoveGameObject(actor);
                //    Log.Information($"SpawnActor error player_id{playerId}, network_id{actor.NetworkId}, world{worldId}, pos{actor.GetLocation()}");

                //    World.Instance(worldId).GameMode.LeaveEntry(playerId);
                //    Log.Information($"HandleNewClient error {playerId}, {this.mSessionId}");
                //    return 0;
                //}
            }

            World.Instance(worldId).playerList.Add(playerId, actor);

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
            Cache.sInstance.AddUser(worldId);
#endif

            this.IsStartedPlay = true;

            return actor.GetNetworkId();
        }

        public int HandleLostClient(PlayerController newPlayerController)
        {
            int networkId = 0;
            //kill client's actor
            //remove client from scoreboard
            int playerId = this.GetPlayerId();

            // 게임 모드에서 플레이어 제거
            //if (World.Instance(this.GetWorldId()).GameMode.LeaveEntry(playerId) == false)
            //{
            //    Log.Information($"HandleLostClient cannot find player {playerId} in world {this.GetWorldId()}");
            //}

            // 월드에서 플레이어 제거
            Actor actor = null;
            World.Instance(this.GetWorldId()).playerList.TryGetValue(playerId, out actor);
            if (World.Instance(this.GetWorldId()).playerList.Remove(playerId))
            {
                // 앱이 중단 상태에서 서버와 접속이 끈기면 강제 접속 해지로 판단
                if (this.IsPause)
                {
                    this.AbuseDisconnect();
                }

                // 모니터링에서 유저수 감소
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else

                Cache.sInstance.DelUser(this.GetWorldId());
#endif
            }

            // NetworkManager에서 Actor 제거
            //SActor actor = GetActorForPlayer(playerId, this.GetWorldId());
            if (actor != null)
            {
                actor.InvokeClientRpc(actor.Disconnected, World.Instance(this.GetWorldId()).playerList.Keys.ToList());

                networkId = actor.NetworkId;

                if (EnableAiSwitch)
                {

                    Unpossess((SActor)actor, newPlayerController, null);


                    if (((SActor)actor).GetActorController() == null)
                        actor.SetDoesWantToDie(true);

                    // 자신이 소유하고 있던 ai컨트롤러를 다른 유저에 위임
                    foreach (var aiController in AIControllers)
                    {
                        var ai_actor = (SActor)NetworkManagerServer.sInstance.GetGameObject(aiController.mNetworkId, this.mWorldId);

                        // 이미 다른 유저가 AI컨트롤러로 위임중인 상태를 그대로 받아오는 경우
                        // 기존것에서 때어내고
                        aiController.Unpossess((SActor)actor);

                        // 이전 정보 유지를 위해 ai컨트롤러를 새로 만들지 않고 그대로 넘겨준다. 
                        Unpossess((SActor)ai_actor, null, aiController);

                        if (((SActor)ai_actor).GetActorController() == null)
                            ai_actor.SetDoesWantToDie(true);
                    }
                }
                else
                {
                    // 액터를 지우지 않고 연결만 끊는다.
                    //actor.SetDoesWantToDie(true);
                    Unpossess((SActor)actor, newPlayerController, null);
                }
            }

            // 월드에 남은 유저 체크후 월드 클로즈
            if (World.Instance(this.GetWorldId()).playerList.Count == 0)
            {
                Log.Information($"Close World {this.GetWorldId()}");

                World.Instance(this.GetWorldId()).GameMode.Close(CloseType.NoPlayer);
                NetworkManagerServer.sInstance.Clear(this.GetWorldId());
                NetworkManagerServer.sInstance.ClearAuth(this.GetWorldId());
                World.Instance(this.GetWorldId()).Reset(true, NetworkManagerServer.sInstance);
            }

            return networkId;
        }

        [Obsolete("This function is obsolete")]
        public SActor GetActorForPlayer(int inPlayerId, byte worldId)
        {
            //run through the objects till we find the actor...
            //it would be nice if we kept a pointer to the actor on the PlayerController
            //but then we'd have to clean it up when the actor died, etc.
            //this will work for now until it's a perf issue
            var gameObjects = World.Instance(worldId).GetGameObjects();
            foreach (var go in gameObjects)
            {
                SActor actor = (SActor)go.GetAsActor();
                if (actor != null && actor.GetPlayerId() == inPlayerId)
                {
                    return (SActor)go;
                }
            }

            return null;
        }



        public void Possess(SActor actor)
        {
            Log.Information($"player Possess player_id{mPlayerId}, network_id{actor.NetworkId}, world{actor.WorldId}");

            ControllPlayerId = mPlayerId;
            actor.SetPlayerId(mPlayerId);
            actor.SetController(this);

            var entry = World.Instance(actor.WorldId).GameMode.GetEntry(mPlayerId);
            if(entry != null)
            {
                entry.Leave = false;
            }
        }

        public void Unpossess(SActor actor, PlayerController newPlayerController, AIController aiController)
        {
            Log.Information($"player Unpossess player_id{actor.GetPlayerId()}, network_id{actor.NetworkId}, world{actor.WorldId}");

            // 새로운 플레이어컨트롤로 교체인 경우 
            if (newPlayerController != null)
            {
                actor.SetController(newPlayerController);
                return;
            }

            World.Instance(actor.WorldId).GameMode.LeaveEntry(mPlayerId);

            if (EnableAiSwitch)
            {
                // 남은 플레이어중에서 ai가 가장 적게 할당된 유저에게 할당
                var otherActor = GetAppropriateActorAIPossess(mWorldId, GetPlayerId());
                if (otherActor != null)
                {
                    if (aiController == null)
                    {
                        // 최초 AI컨트롤러로 위임
                        aiController = new AIController();
                        aiController.Possess(otherActor, actor);
                    }
                    else
                    {
                        // 새로운 곳에 붙임
                        aiController.Possess(otherActor, actor);
                    }

                    // AI 교체 완료
                    return;
                }
            }

            // 기본적으로 해당 객체에 컨트롤러를 널로 초기화
            actor.SetController(null);
        }

        public SActor RestoreAI()
        {
            SActor actor = null;
            var actors = World.Instance(mWorldId).playerList.Values.Where(x => x.GetPlayerId() != GetPlayerId() && ((SActor)x).GetActorController() != null).Select(x=>(SActor)x).ToList();
            foreach (var otherActor in actors)
            {
                var aiController = otherActor.GetActorController().GetAIController(this.mSessionId);
                if (aiController != null && aiController != default(AIController))
                {
                    // AI로 대신 플레이 해주었던 유저에게서 때어 내고
                    actor = (SActor)NetworkManagerServer.sInstance.GetGameObject(aiController.mNetworkId, this.mWorldId);
                    if (actor != null)
                    {
                        aiController.Unpossess(actor);

                        actor.GetActorController().AIControllers.RemoveAll(x => x.mSessionId == this.mSessionId);

                        // 원래 유저에게 다시 붙인다.
                        this.Possess(actor);
                    }
                }
            }
            return actor;
        }

        public void RefreshAIState()
        {
            if (mAIState.Set() == false)
            {
                // 상태 변경이 없었다.
                return;
            }

            for(int i=0;i<AIControllers.Count;++i)
            {
                var state = mAIState.GetState(AIControllers[i].mPlayerId);
                if (state != null)
                {
                    AIControllers[i].mCharacterState = state;
                    //Log.Information($"update ai:{AIControllers[i].mPlayerId}, state location:{state.location}, velocity:{state.velocity}");
                }
            }
        }

    }
}
