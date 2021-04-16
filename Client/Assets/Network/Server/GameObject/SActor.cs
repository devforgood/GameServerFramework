using core;
using Lidgren.Network;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace Server
{
    public partial class SActor : Actor
    {
        float mTimeOfNextShot;
        float mTimeBetweenShots;

        float mTimeOfNextBomb;
        float mTimeBetweenBomb;

        public ActorController mActorController = null;

        public Vector3 SpawnPoint;
        public UInt32 SpawnAngle;
        public bool IsIgnoreInput = false;
        public bool IsReady = false;
        bool IsDieFall = false;

        public ActorStateMachine mActorStateMachine;

        public void SetController(ActorController controller)
        {
            mActorController = controller;
        }

        public ActorController GetActorController()
        {
            return mActorController;
        }

        public bool SetCharacter(byte selectedCharacter, int characterLevel, string user_id)
        {
            if (SetCharacterData(selectedCharacter, characterLevel) ==false)
            {
                Log.Information($"Invaild Character {SelectedCharacter}");
                return false;
            }

            Log.Information($"Setting Character {characterData.Remark}, UserID:{user_id}");

            ResetHealth(GetCharacterHp(), null);

            OnChangedCharacter();

            UserId = user_id;

            return true;
        }



        public Guid GetSessionId()
        {
            if(mActorController != null)
            {
                return mActorController.mSessionId;
            }
            return default(Guid);
        }

        void HandleShooting()
        {
            float time = Timing.sInstance.GetFrameStartTime();
            if (mIsShooting && Timing.sInstance.GetFrameStartTime() > mTimeOfNextShot)
            {
                //not exact, but okay
                mTimeOfNextShot = time + mTimeBetweenShots;

                //fire!
                Projectile bullet = (Projectile)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.Projectile, true, WorldId);
                bullet.InitFromShooter(this);
            }
        }

        void HandleBomb()
        {
            if (mIsBomb == false)
                return;

            float time = Timing.sInstance.GetFrameStartTime();
            if (Timing.sInstance.GetFrameStartTime() <= mTimeOfNextBomb)
            {
                //Log.Information("It's still too early");
                return;
            }

            foreach (var game_object in World.Instance(WorldId).GetGameObjects())
            {
                if (game_object.GetClassId() != (uint)GameObjectClassId.Bomb)
                    continue;

                // 같은 층이 아닌 경우는 충돌 검사에서 제외
                if (NetGameObject.IsSameFloor(floor, game_object) == false)
                    continue;

                if(MathHelpers.circlesColliding(GetLocation().x, GetLocation().z, core.Bomb.DefaultCollisionRadius, game_object.GetLocation().x, game_object.GetLocation().z, game_object.GetCollisionRadius()))
                {
                    Log.Information("already installed bomb");
                    return;
                }
            }

            if (BasicAttack.skillType != (int)SkillType.InstallSkill)
            {
                Log.Information($"skill type error {BasicAttack.skillType}");
                return;
            }

            //not exact, but okay
            mTimeOfNextBomb = time + mTimeBetweenBomb;





            //install bomb
            var bomb = (core.Bomb)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.Bomb, true, WorldId);
            bomb.InitFrom(this, BasicAttack);

            Log.Information("installed bomb success!");

        }

        public static new NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SActor(worldId), worldId); }
        public override void HandleDying()
        {
            base.HandleDying();
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
        }


        void UpdateMove()
        {

            MoveList moveList = mActorController.GetUnprocessedMoveList();
            foreach (var unprocessedMove in moveList.mMoves)
            {
                if (IsIgnoreInput)
                    break;

                var currentState = unprocessedMove.GetInputState();

                float deltaTime = unprocessedMove.GetDeltaTime();

                ProcessInput(deltaTime, currentState);

#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT

                // 서버만 y 축 좌표를 업데이트한다. 서버에서는 물리 연산이 없으므로
                if (currentState.mIsChangeY)
                {
                    //Log.Information($"changeY {GetPlayerId()}, {GetNetworkId()}, y{currentState.mYaxis}");

                    // todo : y 좌표값 변경 허가 여부 체크
                    // raycast를 사용하여 위, 아래
                    // up 근처에 경사로가 있는지 
                    // down 낙하 지점인지

                    GetLocation().y = currentState.mYaxis;
                    if (currentState.mYaxis < World.Map.DieFall)
                    {
                        Log.Information($"died from falling out {GetNetworkId()}, y{currentState.mYaxis}");
                        TakeDamage((int)ReservedPlayerId.Fall, int.MaxValue);
                    }

                    SetFloor(GetLocation().y);
                }
#endif

                // 최대 델타 타임 초과시 쪼개서 여러번 시뮬레이션 한다.
                // 벽을 뚫고 가지 않도록 하기 위함
                while (true)
                {
                    if(MaxDeltaTime < deltaTime)
                    {
                        SimulateMovement(deltaTime);
                        break;
                    }
                    else
                    {
                        SimulateMovement(MaxDeltaTime);
                        deltaTime -= MaxDeltaTime;
                    }
                }

                //log.InfoFormat( "Server Move Time: {0} deltaTime: {1} location:{2}, old_location{3}, player_id{4}", unprocessedMove.GetTimestamp(), deltaTime, GetLocation(), oldLocation, GetPlayerId() );
                //Log.Information("Location:" + GetLocation() + ", Velocity:" + GetVelocity() + ", player_id:" + GetPlayerId());

#if MOVEMENT_DEBUG
                                    debug_x += GetLocation().x - oldLocation.x;
                                    debug_delta += deltaTime;
                                    debug_cnt += 1;

                                    Log.Information($"Location x{GetLocation().x}, debug_x{debug_x}, debug_delta{debug_delta}, debug_cnt{debug_cnt}, debugSeq{currentState.mDebugSeq}, timestamp{unprocessedMove.GetTimestamp()}");
#endif
            }
            IsIgnoreInput = false;

            moveList.Clear();
        }

        public void UpdateState()
        {
            if (mActorController != null && mActorController.mCharacterState != null)
            {
                if (mActorController.mCharacterState.location.y != GetLocation().y)
                {
                    //Log.Information($"UpdateState  clientY:{mActorController.mCharacterState.location.y}, serverY{GetLocation().y}, player_id{GetPlayerId()}, floor{floor}");

                    SetFloor(mActorController.mCharacterState.location.y);

                    if (mActorController.mCharacterState.location.y < World.Map.DieFall && IsDieFall == false)
                    {
                        Log.Information($"died from falling out {GetNetworkId()}, y{GetLocation().y}");
                        TakeDamage((int)ReservedPlayerId.Fall, int.MaxValue);
                        IsDieFall = true;
                    }
                    else
                    {
                        IsDieFall = false;
                    }
                }

                SetLocation(mActorController.mCharacterState.location);
                SetVelocity(mActorController.mCharacterState.velocity);

                //Log.Information( $"Server Move  location:{GetLocation()}, velocity{GetVelocity()}, player_id{GetPlayerId()}, floor{floor}" );
            }

            ProcessCollisions();

        }



        public override void Update()
        {
            base.Update();

            oldLocation.Copy(GetLocation());
            oldVelocity.Copy(GetVelocity());
            oldRotation.Copy(GetRotation());

            //are you controlled by a player?
            //if so, is there a move we haven't processed yet?
            mActorStateMachine.Update(this);

            HandleShooting();
            HandleBomb();
            HandleDotDamage();
            Awake();

            //Log.Information($"Server old  location:{oldLocation}, velocity{oldVelocity}, player_id{GetPlayerId()}");
            //Log.Information($"Server new  location:{GetLocation()}, velocity{GetVelocity()}, player_id{GetPlayerId()}");

            if (!oldLocation.Equals(GetLocation()) ||
                !oldVelocity.Equals(GetVelocity()) ||
                !oldRotation.Equals(GetRotation())
                )
            {
                //Log.Information( $"Server Move  location:{GetLocation()}, velocity{GetVelocity()}, player_id{GetPlayerId()}" );
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Pose);
            }
        }

        public override bool LateUpdate()
        {
#if _USE_BEPU_PHYSICS || _USE_BULLET_SHARP || _USE_BEPU_PHYSICS_V2
            if (base.LateUpdate())
            {
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Pose);
            }
#else
#endif
            return false;
        }

        public int TakeDamage(int inDamagingPlayerId, int damage = 1)
        {
            // 추락으로 데미지가 발생할 경우 무적 상태 예외 처리를 하지 않고 무조건 데미지를 준다.
            if (inDamagingPlayerId != (int)ReservedPlayerId.Fall)
            {
                if (StateServerSide != ActorState.Idle)
                {
                    //Log.Information($"can't do damage in state {StateServerSide}");
                    return 0;
                }

                if (buff.IsExist(BuffType.Invincible))
                {
                    // 무적 상태.
                    //Log.Information($"can't do damage in state Invincible ");
                    return 0;
                }
            }

            // 게임 모드별로 데미지 적용 여부
            if (inDamagingPlayerId >= 0) // 추락, 트랩등으로 데미지를 얻을 경우는 제외
            {
                if (World.Instance(WorldId).GameMode.TakableDamage(inDamagingPlayerId, GetPlayerId()) == false)
                {
                    return 0;
                }
            }

            SActor damagingPlayer = null;
            if (inDamagingPlayerId >= 0) // 추락, 트랩등으로 데미지를 얻을 경우는 제외
            {
                damagingPlayer = (SActor)World.Instance(WorldId).GameMode.GetActor(inDamagingPlayerId);

            }

            int applyDamage = _TakeDamage(inDamagingPlayerId, damagingPlayer, damage);

            if(applyDamage != 0)
            {
                if(damagingPlayer != null)
                {
                    var entry = World.Instance(WorldId).GameMode.GetEntry(inDamagingPlayerId);
                    if(entry != null)
                        entry.Missions.Increment((int)MissionType.Mission_Damage, applyDamage);
                }

                List<int> player_list = new List<int>();
                player_list.Add(GetPlayerId());
                if (damagingPlayer != null)
                    player_list.Add(damagingPlayer.GetPlayerId());

                LogHelper.LogInfo($"TakeDamage playerId:{GetPlayerId()}, hp:{-1 * applyDamage}");
                InvokeClientRpc(NoticeHealth, player_list, -1 * applyDamage);
            }

            return applyDamage;
        }


        private int _TakeDamage(int inDamagingPlayerId, SActor damagingPlayer, int damage)
        {
            GameMode gameMode = World.Instance(WorldId).GameMode;
            var lastBuffHealth = buff.HP;
            // 버프로 받은 HP가 있는 경우
            if (buff.HP>0)
            {
                if (buff.HP >= damage)
                {
                    // 버프 HP만 감소
                    buff.HP -= damage;
                    return damage;
                }
                else
                {
                    // 버프 HP 0 및 actor HP 감소
                    damage -= buff.HP;
                    buff.HP = 0;
                }
            }

            int applyDamage;
            var lastHealth = mHealth;
            mHealth -= damage;
            if (mHealth <= 0)
            {
                if (mHealth < 0)
                    mHealth = 0;

                // die 함수에서 mHealth 값을 수정하여 미리 적용 데미지를 구해놓는다.
                applyDamage = lastHealth - mHealth + lastBuffHealth;

                //score one for damaging player...
                //World.Instance(WorldId).GameMode.IncScore(inDamagingPlayerId, 1);

                //and you want to die
                //SetDoesWantToDie(true);
                TryDie(SpawnPoint, inDamagingPlayerId);

                // 리스폰
                //PlayerController PlayerController = NetworkManagerServer.sInstance.GetPlayerController((int)GetPlayerId());
                //if (PlayerController != null)
                //{
                //    PlayerController.HandleActorDied();
                //}

                bool isKing = gameMode.game_mode.GetMode() == GameModeType.KillTheKing && (gameMode.game_mode as KillTheKing).IsKing(GetPlayerId());
                bool isKingKiller = false;
                if (damagingPlayer != null)
                {
                    isKingKiller = gameMode.game_mode.GetMode() == GameModeType.KillTheKing && (gameMode.game_mode as KillTheKing).IsKing(damagingPlayer.GetPlayerId());

                    PlayPointID pointId = isKing ? PlayPointID.KillTheKing : PlayPointID.EnemyKill;
                    gameMode.OnTrigger(damagingPlayer.GetPlayerId(), pointId);
                    //Log.Information($"give point for player({damagingPlayer.UserId}), pointId : {pointId}");

                    var entry = World.Instance(WorldId).GameMode.GetEntry(inDamagingPlayerId);
                    if (entry != null)
                        entry.Missions.Increment((int)MissionType.Mission_KillCount, 1);

                    ++World.Instance(WorldId).GameMode.statistics.attacked_death;
                }
                else if(inDamagingPlayerId == (int)ReservedPlayerId.Fall)
                {
                    ++World.Instance(WorldId).GameMode.statistics.fall_death;
                }
                else if(inDamagingPlayerId == (int)ReservedPlayerId.Train)
                {
                    ++World.Instance(WorldId).GameMode.statistics.train_death;
                }
                else
                {
                    ++World.Instance(WorldId).GameMode.statistics.other_death;
                }


                gameMode.OnTrigger(GetPlayerId(), PlayPointID.PlayerDeath);

                var player_list = World.Instance(WorldId).playerList.Keys.ToList();
                //Log.Information($"NoticeKillDeath {inDamagingPlayerId}, {GetPlayerId()}, players{string.Join(", ", player_list)}");
                InvokeClientRpc(NoticeKillDeath, player_list, inDamagingPlayerId, GetPlayerId(), isKingKiller, isKing);

            }
            else
            {
                applyDamage = lastHealth - mHealth + lastBuffHealth;

                var player = (SActor)World.Instance(WorldId).GameMode.GetActor(inDamagingPlayerId);
                if (player != null)
                {
                    World.Instance(WorldId).GameMode.OnTrigger(player.GetPlayerId(), PlayPointID.EnemyAttack);
                }
            }

            if (lastHealth != mHealth)
            {
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Health);
            }

            return applyDamage;
        }

        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            return TakeDamage(player_id, damage);
        }

        public override void TryTeleport(Vector3 pos, float power, float duration, int map_uid)
        {
            //// 텔레포트 사용, 시도 요청시 이전 키 입력 무시
            //if (mActorController != null)
            //    mActorController.GetUnprocessedMoveList().Clear();

            //if (StateServerSide == ActorState.Teleport)
            //{
            //    Log.Information($"already teleport pos {pos.ToString()}");
            //    return;
            //}

            //pos.y += 1;

            //ChangeStateServerSide(ActorState.Teleport, duration);
            //TargetPos = pos;
            //JumpPower = power;
            //JumpDuration = duration;
            //Vector2 direction = new Vector2(pos.x - GetLocation().x, pos.z - GetLocation().z);
            //degree = direction.Angle();
            //lastVelocity = new Vector3(direction.x, 0, direction.y) * 0.1f;
            //Log.Information($"start teleport pos {pos.ToString()}, power{power}, duration{duration}, degree{degree}, direction{direction}");

            //// 이후 인풋값은 무시
            //IsIgnoreInput = true;
        }

        public void TryDie(Vector3 pos, int inDamagingPlayerId)
        {
            //// 텔레포트 사용, 시도 요청시 이전 키 입력 무시
            //if (mActorController != null)
            //    mActorController.GetUnprocessedMoveList().Clear();
            if (StateServerSide == ActorState.Ghost)
            {
                Log.Information($"already ghost pos {pos.ToString()}");
                return;
            }

            //Log.Information($"start ghost pos {pos.ToString()}");

            mActorStateMachine.ChangeState(this, mActorStateMachine.UpdateGhost, ActorState.Ghost, Actor.GhostDuration);
            TargetPos = pos;

            killPlayerId = inDamagingPlayerId;

            // 이후 인풋값은 무시
            IsIgnoreInput = true;

            // 버프 초기화
            buff.Clear();

            // 케릭터 정보 초기화
            ResetHealth(GetCharacterHp(), null);
            OnChangedCharacter();
        }

        protected SActor(byte worldId) : base(worldId)
        {

            mTimeOfNextShot = 0.0f;
            mTimeBetweenShots = 0.2f;

            mTimeOfNextBomb = 0.0f;
            mTimeBetweenBomb = 0.2f;

            //CacheAttributes();

            mActorStateMachine = new ActorStateMachine();
        }

        protected override void Dirty(uint state)
        {
            //Log.Information($"dirty actor {GetNetworkId()}");

            NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, state);
        }

        public bool GetItem(int item_id)
        {
            //Log.Information($"GetItem item_id:{item_id}, network_id{GetNetworkId()}");
            if(StateServerSide == ActorState.Ghost)
            {
                Log.Information($"character state is ghost. item_id:{item_id}, network_id{GetNetworkId()}");
                return false;
            }

            if (LastStateServerSide == ActorState.Ghost && BeginState + 0.5f > Timing.sInstance.GetFrameStartTime())
            {
                Log.Information($"character state is ghost. item_id:{item_id}, network_id{GetNetworkId()}");
                return false;
            }


            var itemData = ACDC.ItemData[item_id];
            if (itemData == null)
            {
                Log.Information($"cannot find error GetItem item_id:{item_id}, network_id{GetNetworkId()}");
                return false;
            }

            JSpellData spellData = null;
            if ((ITEM_ID_TYPE)item_id == ITEM_ID_TYPE.ADD_ITEM)
            {
                spellData = ACDC.SpellData[itemData.SpellID[0]];
            }
            else
            {
                spellData = ACDC.SpellData[itemData.SpellID[0]];
            }

            if (spellData == null)
            {
                Log.Information($"cannot find error GetSpellData spellId:{itemData.SpellID}, network_id{GetNetworkId()}");
                return false;
            }

            // 아이템 획득시 플레이포인트 지급
            World.Instance(WorldId).GameMode.OnTrigger(this.GetPlayerId(), PlayPointID.ItemGain);

            var entry = World.Instance(WorldId).GameMode.GetEntry(this.GetPlayerId());
            if (entry != null)
            {
                // 일반, 전락 구분
                if (itemData.ItemType == (int)ItemType.Normal)
                {
                    entry.Missions.Increment((int)MissionType.Mission_Get_NorItem, 1);
                    ++World.Instance(WorldId).GameMode.statistics.normal_item;
                }
                else if (itemData.ItemType == (int)ItemType.Tactic)
                {
                    entry.Missions.Increment((int)MissionType.Mission_Get_TacItem, 1);
                    ++World.Instance(WorldId).GameMode.statistics.tactic_item;
                }
            }


            AddSpell(spellData, 0);

            if (spellData.ApplyObject == (int)ApplyObject.OurTeam || itemData.ItemType == (int)ItemType.Tactic)
            {
                var player_list = World.Instance(WorldId).GameMode.GetMyTeam(this.Team);
                foreach (var player in player_list)
                {
                    if (player.StateServerSide == ActorState.Ghost)
                        continue;

                    player.InvokeClientRpcOnClient(player.GetItemClient, (int)player.GetPlayerId(), item_id);
                }
            }
            else
            {
                // 획득한 아이템 클라이언트에 알림
                InvokeClientRpcOnClient(GetItemClient, (int)GetPlayerId(), item_id);
            }

            return true;
        }

        public void AddSpell(JSpellData data, int damage)
        {
            var status = data.AddStatus;
            switch ((StatusType)data.StatusType)
            {
                case StatusType.Damage:
                    {
                        status = damage;
                    }
                    break;
                case StatusType.Health:
                    {
                        status = mHealth;
                    }
                    break;
                case StatusType.CharacterSpeed:
                    {
                        status = (int)characterData.Speed;
                    }
                    break;
                case StatusType.CharacterSkillCooldown:
                    {
                        status = (int)characterData.Bomb1_installCoolTime;
                    }
                    break;
                case StatusType.CharacterDamage:
                    {
                        status = 1f;
                    }
                    break;

            }


            switch ((core.BuffType)data.BuffID)
            {
                case core.BuffType.Shield: //방어막
                    {
                        if (data.ApplyObject == (int)ApplyObject.Castle) // 거점.
                        {
                            World.Instance(WorldId).GameMode.BuffShield(this, data);
                        }
                        else if (data.ApplyObject == (int)ApplyObject.OurTeam)
                        {
                            var player_list = World.Instance(WorldId).GameMode.GetMyTeam(this.Team);
                            foreach (var player in player_list)
                            {
                                if (player.StateServerSide == ActorState.Ghost)
                                    continue;

                                player.buff.AddBuff((Buff)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Buff, true, (byte)WorldId), data, player.GetNetworkId(), mHealth);
                            }
                        }
                        else if(data.ApplyObject == (int)ApplyObject.Oneself)
                        {
                            buff.AddBuff((Buff)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Buff, true, (byte)WorldId), data, GetNetworkId(), mHealth);
                        }
                    }
                    break;
                case core.BuffType.RecoveryHp: // HP회복
                    {
                        if(data.AddStatusType == (int)AddStatusType.Absolute)
                        {

                        }
                        else if(data.AddStatusType == (int)AddStatusType.Relative)
                        {
                            status = (int)(status * data.AddStatus);
                        }


                        if (data.ApplyObject == (int)ApplyObject.Castle) // 거점.
                        {
                            World.Instance(WorldId).GameMode.BuffRecovery(this, data);
                        }
                        else if(data.ApplyObject == (int)ApplyObject.OurTeam) // 팀 전체 HP 회복
                        {
                            var player_list = World.Instance(WorldId).GameMode.GetMyTeam(this.Team);
                            foreach (var player in player_list)
                            {
                                if (player.StateServerSide == ActorState.Ghost)
                                    continue;

                                player.IncreHealth((int)status, data);
                            }
                        }
                        else if(data.ApplyObject == (int)ApplyObject.Oneself)
                        {
                            IncreHealth((int)status, data);
                        }
                    }
                    break;
                default:
                    {
                        if (data.ApplyObject == (int)ApplyObject.OurTeam)
                        {
                            var player_list = World.Instance(WorldId).GameMode.GetMyTeam(this.Team);
                            foreach (var player in player_list)
                            {
                                if (player.StateServerSide == ActorState.Ghost)
                                    continue;

                                player.buff.AddBuff((Buff)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Buff, true, (byte)WorldId), data, player.GetNetworkId(), status);
                            }
                        }
                        else
                        {
                            buff.AddBuff((Buff)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Buff, true, (byte)WorldId), data, GetNetworkId(), status);
                        }
                    }
                    break;
            }
        }

        public void AddableSpell(SActor attackPlayer, int attackPlayerId, int spellID, int applyDamage)
        {
            var data = ACDC.SpellData[spellID];
            AddableSpell(attackPlayer, attackPlayerId, data, applyDamage);
        }

        public void AddableSpell(SActor attackPlayer, int attackPlayerId, JSpellData spellData, int applyDamage)
        {
            if (StateServerSide != ActorState.Idle)
            {
                //Log.Information($"can't add spell in state {StateServerSide}");
                return;
            }

            if (buff.IsExist(BuffType.Invincible))
            {
                // 무적일때 스펠도 안걸리고 대미지도 안걸림.
                return;
            }

            // 게임 모드별로 데미지 적용 여부
            // 데미지 안받으면 스펠도 안걸림.
            if (attackPlayerId >= 0) // 추락, 트랩등으로 데미지를 얻을 경우는 제외
            {
                if (World.Instance(WorldId).GameMode.TakableDamage(attackPlayerId, GetPlayerId()) == false)
                {
                    return;
                }
            }

            if (spellData != null)
            {
                if (spellData.ApplyObject == (int)ApplyObject.Oneself)
                {
                    attackPlayer.AddSpell(spellData, applyDamage);
                }
                else
                {
                    AddSpell(spellData, applyDamage);
                }
            }
        }

        public void AddSpellMySelf(core.BuffType type)
        {
            var data = ACDC.SpellData.Values.Where(x=>x.BuffID==(int)type).FirstOrDefault();
            if (data != null && data != default(JSpellData))
            {
                AddSpell(data, 0);
            }
        }

        public void HandleDotDamage()
        {
            var burnDotDamage = buff.GetBuffFirstRef(BuffType.BurnDotDamage);
            //burn
            if (burnDotDamage != null)
            {
                if (burnDotDamage.NextTickTime())
                {
                    TakeDamage((int)ReservedPlayerId.DotDamage, (int)burnDotDamage.mAddStatus);
                    Log.Information($"Burn Dot {(int)burnDotDamage.mAddStatus} Tick {Timing.sInstance.GetFrameStartTime()}");
                }
            }

            // poison
            var poisonDotDamage = buff.GetBuffFirstRef(BuffType.PoisonDotDamage);
            if (poisonDotDamage != null)
            {
                if (poisonDotDamage.NextTickTime())
                {
                    TakeDamage((int)ReservedPlayerId.DotDamage, (int)poisonDotDamage.mAddStatus);
                    Log.Information($"Poison Dot {(int)poisonDotDamage.mAddStatus} Tick {Timing.sInstance.GetFrameStartTime()}");
                }
            }
        }

        public void NoticeCreateItem()
        {
            InvokeClientRpcOnClient(OnNoticeCreateItem, (int)GetPlayerId());
        }
    }
}
