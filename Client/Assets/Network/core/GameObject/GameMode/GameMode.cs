using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif


namespace core
{
    public enum GameModeType
    {
        None,
        TeamDeathmatch,
        BaseStruggle,
        KillTheKing,
        FreeForAll,

        MaxGameModeType,
    }

    public enum CloseType
    {
        None,
        Timeout,
        NoPlayer,
        Clear,
    }


    public enum Team
    {
        TeamA,
        TeamB,
        TeamC,

        MaxTeam = 4,
    }

    public enum BaseStruggleTeam
    {
        TeamCount = Team.TeamB +1,
    }

    public class GameMode : NetGameObject
    {
        public static readonly int MaxGameModeTypeBits = MathHelpers.countBits((int)GameModeType.MaxGameModeType - 1);
        public static readonly int MaxTeamBits = MathHelpers.countBits((int)Team.MaxTeam - 1);
        public static readonly int MaxGameModeStateBits = MathHelpers.countBits((int)GameModeState.MaxGameModeState - 1);


        public override byte GetClassId() { return (byte)GameObjectClassId.GameMode; }


        protected enum ReplicationState
        {
            Entry = 1 << 0,
            GameMode = 1 << 1,
            PlayTime = 1 << 2,
            GameState = 1 << 3,
            AllState = Entry | GameMode | GameState
        };

        public enum GameModeState
        {
            None,
            Init,
            Ready,
            Play,
            End,

            MaxGameModeState = 8,
        }

        //int[,] team_index = new int[,]
        //{
        //    {0, 1, 2, 3, 4, 5 },
        //    {0, 3, 1, 4, 2, 5 },
        //    {0, 2, 4, 1, 3, 5 },
        //};

        public static NetGameObject StaticCreate(byte worldId) { return new GameMode(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public List<Entry> GetEntries() { return mEntries; }

        public GameMode()
        {
            game_mode = CreateGameMode(core.GameModeType.BaseStruggle);
            StartTime = Timing.sInstance.GetFrameStartTime();

            CacheAttributes();
        }

        // 게임이 시작되었을때 연관 이벤트 실행
        Action OnStart = null;
        public static bool EndAble = true;

        protected List<Entry> mEntries = new List<Entry>();
        public IGameMode game_mode;
        public GameModeState state = GameModeState.None;
        public Team LoseTeam;
        public Team WinTeam;
        public bool IsDraw; // 비김
        public int MVPPlayerId;

        public long mMatchId;

        public static float ReadyTimeout = 15f;
        public static float InitTimeout = 5f;
        public static float ExpiredTime = 420f; // 최대 플레이 타임 이후 강제 종료 처리한다.

        //public const float DefaultPlayTime = 3f * 60f; // 기본 플레이 타임
        //public const float DefaultPlayTime = 10f; // 기본 플레이 타임
        public float StartTime; // 경기 시작 시간
        public float ReadyTime;
        public float InitTime;
        public long StartEpochTime;

        public JMapData mMapData;
        public JGameModeData mGameModeData;

        public int ReservedPlayerCount; // 예약된 플레이어 수
        public bool IsSetPlayTime;

        public Statistics statistics = new Statistics();

        public NetworkManager mNetworkManager = null;

        /// <summary>
        ///  경기 종료 시각
        /// </summary>
        public float EndTime
        {
            get
            {
                if (mGameModeData == null)
                    return StartTime;

                return StartTime + mGameModeData.PlayTime;
            }
        }

        /// <summary>
        ///  경기 종료까지 남은 시간
        /// </summary>
        public float RemainTime
        {
            get
            {
                if (mGameModeData == null)
                    return 0;

                if (IsSetPlayTime == false)
                    return mGameModeData.PlayTime;

                if (StartTime + mGameModeData.PlayTime > Timing.sInstance.GetFrameStartTime())
                {
                    var remain = StartTime + mGameModeData.PlayTime - Timing.sInstance.GetFrameStartTime();
                    //TimeSpan timeSpan = new TimeSpan(0, 0, (int)remain);
                    //core.LogHelper.LogInfo($"GamePlay ReaminTime {remain}, {timeSpan.ToString()}, timestamp:{core.Timing.sInstance.GetFrameStartTime()}");
                    return remain;
                }
                return 0;
            }
        }

        public void RegisterStartEvent(Action func)
        {
            if (OnStart == null)
                OnStart = func;
            else
                OnStart += func;
        }

        public void Init(int map_id, long match_id, int player_count)
        {
            // 이전에 등록했던 시작 이벤트 초기화
            OnStart = null;

            // 게임 시작 이벤트 등록
            foreach (var gameObject in World.Instance(WorldId).GetGameObjects())
            {
                if (gameObject.GetClassId() == (byte)GameObjectClassId.Trap)
                    RegisterStartEvent(((Trap)gameObject).OnStart);
                else if (gameObject.GetClassId() == (byte)GameObjectClassId.TreasureBox)
                    RegisterStartEvent(((TreasureBox)gameObject).OnStart);
                else if (gameObject.GetClassId() == (byte)GameObjectClassId.Train)
                    RegisterStartEvent(((Train)gameObject).OnStart);
            }


            mMapData = ACDC.MapData[ map_id ];
            mGameModeData = ACDC.GameModeData[ mMapData.GameMode ];
            game_mode = CreateGameMode((GameModeType)mGameModeData.ID);
            mMatchId = match_id;
            StartTime = Timing.sInstance.GetFrameStartTime();
            state = GameModeState.Init;
            InitTime = Timing.sInstance.GetFrameStartTime();
            ReservedPlayerCount = player_count;
        }

        public virtual IGameMode CreateGameMode(GameModeType mode)
        {
            switch (mode)
            {
                case core.GameModeType.BaseStruggle:
                    return new core.BaseStruggle();
                case core.GameModeType.FreeForAll:
                    return new core.FreeForAll();
                case core.GameModeType.KillTheKing:
                    return new core.KillTheKing();
                case core.GameModeType.TeamDeathmatch:
                    return new core.TeamDeathmatch();
            }
            return null;
        }

        public void Ready()
        {
            if (state == GameModeState.Ready)
                return;

            state = GameModeState.Ready;
            ReadyTime = Timing.sInstance.GetFrameStartTime();
            Dirty((uint)ReplicationState.GameState);

            LogHelper.LogInfo($"GameMode Ready worldid:{WorldId}, networkid:{GetNetworkId()}, timestamp:{Timing.sInstance.GetFrameStartTime()}");
        }

        public void StartPlay(List<int> players)
        {
            StartEpochTime = DateTime.UtcNow.ToEpochTime();
            StartTime = Timing.sInstance.GetFrameStartTime();
            IsSetPlayTime = true;
            Dirty((uint)ReplicationState.PlayTime);
            state = GameModeState.Play;
            Dirty((uint)ReplicationState.GameState);

            LogHelper.LogInfo($"GameMode Start worldid:{WorldId}, networkid:{GetNetworkId()}, timestamp:{Timing.sInstance.GetFrameStartTime()}");

            foreach (var player in World.Instance(WorldId).playerList)
            {
                player.Value.InvokeClientRpcOnClient(player.Value.OnStartPlay, player.Key, players);
            }

            game_mode.OnStartPlay(this);

            // 게임 시작 이벤트 호출
            OnStart?.Invoke();
        }

        public void TimeoutInit()
        {
            if (ReservedPlayerCount <= World.Instance(WorldId).playerList.Count)
            {
                return;
            }

            int remainCount = ReservedPlayerCount - World.Instance(WorldId).playerList.Count;

            LogHelper.LogInfo($"GameMode TimeoutInit worldid:{WorldId}, networkid:{GetNetworkId()}, remainCount:{remainCount}, timestamp:{Timing.sInstance.GetFrameStartTime()}");

            foreach (var player in World.Instance(WorldId).playerList)
            {
                player.Value.InvokeClientRpcOnClient(player.Value.OnTimeoutInit, player.Key, remainCount);
            }
        }


        protected virtual int GetMaxTeamCount() { return 2; }

        /// <summary>
        /// 게임 종료 (서버사이드)
        /// </summary>
        /// <param name="winTeam"></param>
        /// <param name="loseTeam"></param>
        /// <param name="isDraw"></param>
        protected virtual void EndGameServerside(Team winTeam, bool isDraw, long matchId, CloseType closeType) { }

        /// <summary>
        ///  게임이 종료되었을때 처리 (클라사이드)
        /// </summary>
        public virtual void OnGameEnd() { }


        public Entry GetEntry(int inPlayerId)
        {
            foreach (var entry in mEntries)
            {
                if (entry.GetPlayerId() == inPlayerId)
                {
                    return entry;
                }
            }

            return null;
        }

        public Actor GetActor(int inPlayerId)
        {
            Entry entry = GetEntry(inPlayerId);
            if (entry == null)
            {
                LogHelper.LogError($"Can't find entry playerId : {inPlayerId}");
                return null;
            }

            NetGameObject netGameObject = mNetworkManager.GetGameObject(entry.mNetworkId, WorldId);
            if (netGameObject == null)
            {
                LogHelper.LogError($"netGameObject == null or netGameObject is not SActor playerId : {inPlayerId}, worldId : {WorldId}");
                return null;
            }
            return (Actor)netGameObject;
        }

        public List<Actor> GetMyTeam(Team team)
        {
            List<Actor> actors = new List<Actor>();
            foreach(var entry in GetEntries())
            {
                if(entry.GetTeam() != team)
                {
                    continue;
                }

                NetGameObject netGameObject = mNetworkManager.GetGameObject(entry.mNetworkId, WorldId);
                if (netGameObject == null)
                {
                    LogHelper.LogError($"netGameObject == null or netGameObject is not SActor playerId : {entry.GetPlayerId()}, worldId : {WorldId}");
                    continue;
                }

                actors.Add((Actor)netGameObject);
            }
            return actors;
        }

        public Team GetOtherTeam(Team myTeam)
        {
            foreach (var entry in mEntries)
            {
                if (entry.GetTeam() != myTeam)
                {
                    return entry.GetTeam();
                }
            }
            return myTeam;
        }

        public bool RemoveEntry(int inPlayerId)
        {
            foreach (var entry in mEntries)
            {
                if (entry.GetPlayerId() == inPlayerId)
                {
                    Dirty((uint)ReplicationState.Entry);
                    return mEntries.Remove(entry);
                }
            }
            return false;
        }

        public bool IsLeave(int inPlayerId)
        {
            foreach (var entry in mEntries)
            {
                if (entry.GetPlayerId() == inPlayerId)
                {
                    return entry.Leave;
                }
            }
            return true;
        }
        public bool LeaveEntry(int inPlayerId)
        {
            bool ret = false;
            foreach (var entry in mEntries)
            {
                if (entry.GetPlayerId() == inPlayerId)
                {
                    entry.Leave = true;
                    //Dirty((uint)ReplicationState.Entry);
                    ret = true;
                    break;
                }
            }

            // 모든 유저가 나갔는지 체크
            bool isEmpty = true;
            foreach (var entry in mEntries)
            {
                if(entry.Leave == false)
                {
                    isEmpty = false;
                    break;
                }
            }
           
            if(isEmpty)
            {
                LogHelper.LogInfo($"GameMode worldid:{WorldId} entry empty");
                state = GameModeState.Init;
            }

            return ret;
        }

        public bool PauseEntry(int inPlayerId, bool pause)
        {
            foreach (var entry in mEntries)
            {
                if (entry.GetPlayerId() == inPlayerId)
                {
                    entry.Pause = pause;
                    //Dirty((uint)ReplicationState.Entry);
                    return true;
                }
            }
            return false;
        }

        bool IsEmptySeat(int seat)
        {
            foreach (var entry in mEntries)
            {
                if (entry.seat == seat)
                    return false;
            }
            return true;
        }

        public Entry AddEntry(int inPlayerId, string inSessionId, ushort team, int spawn_index, int network_id)
        {
            //if this player id exists already, remove it first- it would be crazy to have two of the same id
            RemoveEntry(inPlayerId);

            int index = 0;
            if (spawn_index < 0)
            {
                for (int i = 0; i < World.spawn_position.Count; ++i)
                {
                    if (World.spawn_position[i].spawnTeam != team)
                        continue;

                    if (IsEmptySeat(i))
                    {
                        index = i;
                        break;
                    }
                }
            }
            else
            {
                index = spawn_index;
            }

            var entry = new Entry(inPlayerId, inSessionId, (Team)World.spawn_position[index].spawnTeam, network_id);
            mEntries.Add(entry);
            entry.seat = index;


            //int currentTeam = mEntries.Count % GetMaxTeamCount();
            //var index = mEntries.Count % (World.spawn_position.Count);

            //var entry = new Entry(inPlayerId, inPlayerName, (Team)currentTeam);
            //mEntries.Add(entry);

            //entry.seat = team_index[(GetMaxTeamCount() - 1), index];


            LogHelper.LogInfo($"GameMode Add Player idx{index}, seat{entry.seat}");


            DirtyExcept((int)inPlayerId, (uint)ReplicationState.Entry);

            return entry;
        }

        public void IncScore(int inPlayerId, int inAmount)
        {
            Entry entry = GetEntry(inPlayerId);
            if (entry != null)
            {
                entry.SetScore(entry.GetScore() + inAmount);
                Dirty((uint)ReplicationState.Entry);
            }
        }



        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;

            if ((inDirtyState & (UInt32)ReplicationState.Entry) != 0)
            {
                inOutputStream.Write((bool)true);

                /////////////////////////
                int entryCount = mEntries.Count;

                //we don't know our player names, so it's hard to check for remaining space in the packet...
                //not really a concern now though
                inOutputStream.Write(entryCount);
                foreach (var entry in mEntries)
                {
                    entry.Write(inOutputStream);
                }
                /////////////////////////
               
                writtenState |= (UInt32)ReplicationState.Entry;

            }
            else
            {
                inOutputStream.Write((bool)false);
            }


            if ((inDirtyState & (UInt32)ReplicationState.GameMode) != 0)
            {
                inOutputStream.Write((bool)true);

                /////////////////////////
                inOutputStream.Write((uint)game_mode.GetMode(), MaxGameModeTypeBits);
                game_mode.Write(inOutputStream);

                /////////////////////////

                writtenState |= (UInt32)ReplicationState.GameMode;

            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.GameState) != 0)
            {
                inOutputStream.Write((bool)true);

                /////////////////////////
                inOutputStream.Write((uint)state, MaxGameModeStateBits);
                if(state == GameModeState.End)
                    game_mode.WriteGameResult(inOutputStream, this);

                /////////////////////////

                writtenState |= (UInt32)ReplicationState.GameState;

            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.PlayTime) != 0)
            {
                inOutputStream.Write((bool)true);

                /////////////////////////
                inOutputStream.Write(StartTime - Timing.sInstance.GetFrameStartTime());
                /////////////////////////

                writtenState |= (UInt32)ReplicationState.PlayTime;

            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            return writtenState;
        }

        public override void Read(NetIncomingMessage inInputStream)
        {
            bool stateBit;
            //Debug.Log($"GameMode read");

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mEntries.Clear();
                int entryCount = inInputStream.ReadInt32();
                //just replace everything that's here, it don't matter...
                for (int i = 0; i < entryCount; ++i)
                {
                    var entry = new core.Entry();
                    entry.Read(inInputStream);
                    mEntries.Add(entry);
                }
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                var mode = (core.GameModeType)inInputStream.ReadUInt32(MaxGameModeTypeBits);
                game_mode = CreateGameMode(mode);
                game_mode.Read(inInputStream);
                mGameModeData = ACDC.GameModeData[(int)mode];
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                state = (GameModeState)inInputStream.ReadUInt32(MaxGameModeStateBits);
                if (state == GameModeState.End)
                {
                    game_mode.ReadGameResult(inInputStream, this);
                    OnGameEnd();
                }
                //Debug.Log($"GameMode state {state}");
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                // 최대 RTT 보정
                if (NetworkManager.Instance.GetRoundTripTimeClientSide() > 1f)
                    StartTime = core.Timing.sInstance.GetFrameStartTime() + inInputStream.ReadFloat() - (1f*0.5f);
                else
                    StartTime = core.Timing.sInstance.GetFrameStartTime() + inInputStream.ReadFloat() - (NetworkManager.Instance.GetRoundTripTimeClientSide() * 0.5f);
                Debug.Log($"GameMode Start StartTime:{StartTime}, RemainTime:{RemainTime}, timestamp:{core.Timing.sInstance.GetFrameStartTime()}, RTT:{NetworkManager.Instance.GetRoundTripTimeClientSide()}");
                IsSetPlayTime = true;
            }
        }

        public void BuffShield(Actor actor, JSpellData data)
        {
            var entry = GetEntry(actor.GetPlayerId());
            if (entry == null)
            {
                LogHelper.LogError($"BuffShield find entry error {actor.GetPlayerId()}");
                return;
            }
            LogHelper.LogInfo($"BuffShield { World.Instance(actor.WorldId)?.castleList?.Count}, {entry.GetTeam()}");
            World.Instance(actor.WorldId).GetCastle(entry.GetTeam())?.BuffShield(data);
        }

        public void BuffRecovery(Actor actor, JSpellData data)
        {
            var entry = GetEntry(actor.GetPlayerId());
            if (entry == null)
            {
                LogHelper.LogError($"BuffRecovery find entry error {actor.GetPlayerId()}");
                return;
            }
            LogHelper.LogInfo($"BuffRecovery { World.Instance(actor.WorldId)?.castleList?.Count}, {entry.GetTeam()}");
            World.Instance(actor.WorldId).GetCastle(entry.GetTeam())?.BuffRecovery(data);
        }

        public bool TakableDamage(int inPlayerIdA, int inPlayerIdB)
        {
            //트랩인 경우는 무조건 true
            if (inPlayerIdA == (int)core.ReservedPlayerId.Trap)
                return true;
            var entryA = GetEntry(inPlayerIdA);
            var entryB = GetEntry(inPlayerIdB);
            if (entryA == null)
            {
                LogHelper.LogError($"find error {inPlayerIdA}");
                return false;
            }
            if (entryB == null)
            {
                LogHelper.LogError($"find error {inPlayerIdB}");
                return false;
            }

            return game_mode.TakableDamage(entryA, entryB);
        }

        public void Close(CloseType closeType)
        {
            game_mode.OnClose(WorldId, this, closeType);
        }

        public void EndGame(Team winTeam, Team loseTeam, bool isDraw, CloseType closeType)
        {
            if (EndAble == false)
                return;

            state = GameModeState.End;

            if (isDraw)
                MVPPlayerId = 0;
            else
                MVPPlayerId = GetMVP(winTeam);

            LogHelper.LogInfo($"GameMode End Game {winTeam}, {loseTeam}, {isDraw}, World{WorldId}, mvp{MVPPlayerId}");

            WinTeam = winTeam;
            LoseTeam = loseTeam;
            IsDraw = isDraw;

            Dirty((uint)ReplicationState.GameState);
            Dirty((uint)ReplicationState.Entry);

            EndGameServerside(winTeam, isDraw, mMatchId, closeType);
        }

        public virtual void OnTrigger(int playerId, PlayPointID id) { }

        public int GetTeamScore(Team team)
        {
            return mEntries.Where(x => x.GetTeam() == team).Sum(x=>x.GetScore());
        }

        int GetMVP(Team winTeam)
        {
            try
            {
                var maxValue = mEntries.Where(x => x.GetTeam() == winTeam).Max(x => x.GetScore());
                if (maxValue == 0)
                    return 0;

                var list = mEntries.Where(x => x.GetTeam() == winTeam && x.GetScore() == maxValue).ToList();
                if (list.Count == 0)
                    return 0;

                return list[0].GetPlayerId();
            }
            catch (Exception ex)
            {
                LogHelper.LogError($"GetMVP {ex.ToString()}");
            }
            return 0;

        }

        [ClientRPC]
        public virtual void SwitchKing(Team team, int beforeKing, int currentKing)
        {

        }

    }
}
