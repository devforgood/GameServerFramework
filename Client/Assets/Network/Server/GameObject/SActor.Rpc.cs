using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using core;
using Lidgren.Network;
using Serilog;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif


namespace Server
{
    public partial class SActor
    {
        public class ActionInfo
        {
            public int SkillId;
            public HashSet<byte> hitIndex;
        }


        Dictionary<byte, ActionInfo> ActionTokens = new Dictionary<byte, ActionInfo>();
        public static bool EnableDebugCommand;

        /// <summary>
        ///   RPC 선언
        /// </summary>
        /// <param name="number"></param>
        [ServerRPC(RequireOwnership = false)]
        public override void PingServer(int number)
        {
            InvokeClientRpcOnClient(PingClient, (int)GetPlayerId(), number);
        }

        [ServerRPC(RequireOwnership = false)]
        public override void DebugCommand(string cmd, string param1, string param2, string param3, string param4)
        {
            if (EnableDebugCommand == false)
            {
                return;
            }

            try
            {
                Log.Information($"DebugCommand networkID:{GetNetworkId()}, playerID:{GetPlayerId()}, cmd{cmd}, param1{param1}, param2{param2}, param3{param3}, param4{param4}");
                Battle.DebugCommand.Execute(this, cmd, param1, param2, param3, param4);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        [ServerRPC(RequireOwnership = false)]
        public override void JumpServer(int power)
        {
#if _USE_BEPU_PHYSICS
            mCharacterController.Jump();

            InvokeClientRpcOnClient(JumpClient, (int)GetPlayerId(), power);

#else
            var location = GetLocation();
            location.y += power;
            location += mDirection * power;
            SetLocation(location);
#endif
        }

        [ServerRPC(RequireOwnership = false)]
        public override void TeleportServer(int map_uid)
        {
            if (StateServerSide != ActorState.Idle)
            {
                //Log.Information($"TeleportServer wrong state {StateServerSide}, networkID{GetNetworkId()}");
                return;
            }

            JMapObjectData mapData;
            if (World.mapGameObject.TryGetValue(map_uid, out mapData) == false)
            {
                Debug.Log($"cannot find map object {map_uid}");
                return;
            }

            var pos = mapData.jumpLandingPos;
            pos.y += 1;

            var duration = mapData.jumpDuration;
            var power = mapData.jumpPower;

            //Log.Information("TeleportServer old location {0} new location {1}", GetLocation(), pos);
            if (StateServerSide == ActorState.Teleport)
            {
                Log.Information($"already teleport pos {pos.ToString()}");
                return;
            }

            // check cooldown
            if (World.Instance(WorldId).GetNetGameObject(GameObjectClassId.PropCooldown, map_uid) != null)
            {
                Log.Information($"cooldown... {map_uid}");
                return;
            }

            mActorStateMachine.ChangeState(this, mActorStateMachine.UpdateTeleport, ActorState.Teleport, duration);
            TargetPos = pos;
            JumpPower = power;
            JumpDuration = duration;
            Vector2 direction = new Vector2(pos.x - GetLocation().x, pos.z - GetLocation().z);
            degree = direction.Angle();
            lastVelocity = new Vector3(direction.x, 0, direction.y) * 0.1f;
            LogHelper.LogInfo($"start teleport pos {pos.ToString()}, power{power}, duration{duration}, degree{degree}, direction{direction}");

            // 이후 인풋값은 무시
            IsIgnoreInput = true;

            //set cooldown
            PropCooldown cooldown = (PropCooldown)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.PropCooldown, true, WorldId);
            cooldown.InitFrom(map_uid);


            //InvokeClientRpc(TeleportClient, World.Instance(WorldId).playerList.Keys.ToList(), pos, power, duration);

        }

        [ServerRPC(RequireOwnership = false)]
        public override void ShootServer(byte actionToken, bool IsBasicAttack, Vector3 targetPos, float fDistance, float fForce, float fFireAngle, float fHeightAngle)
        {
            if (StateServerSide != ActorState.Idle)
            {
                Log.Information($"ShootServer wrong state {StateServerSide}, networkID{GetNetworkId()}");
                return;
            }

            // 패킷을 송신한 유저를 제외하고 리스트 구성
            var player_list = World.Instance(WorldId).playerList.Keys.Where(x => x != mActorController.ControllPlayerId).ToList();

            //Log.Information($"ShootServer player ({string.Join(",", player_list)}), {targetPos}, {fDistance}, {fForce}, {fFireAngle}, {fHeightAngle}");

            // 검증을 위해 ActionToken을 등록해둔다.
            ActionTokens[actionToken] = new ActionInfo() { SkillId = GetSkillId(IsBasicAttack) };

            InvokeClientRpc(ShootClient, player_list, actionToken, IsBasicAttack, targetPos, fDistance, fForce, fFireAngle, fHeightAngle);


            //ChangeStateServerSide(ActorState.Dash, 0.3f);
            //TargetPos = targetPos;
            //Vector2 direction = new Vector2(targetPos.x - GetLocation().x, targetPos.z - GetLocation().z);
            //degree = direction.Angle();
            //lastVelocity = new Vector3(direction.x, 0, direction.y) * 0.1f;

        }

        [ServerRPC(RequireOwnership = false)]
        public override void ShootPushServer(byte actionToken, bool IsBasicAttack, Vector3 targetPos)
        {
            if (StateServerSide != ActorState.Idle)
            {
                LogHelper.LogInfo($"ShootPushServer wrong state {StateServerSide}, networkID{GetNetworkId()}");
                return;
            }

            // 패킷을 송신한 유저를 제외하고 리스트 구성
            var player_list = World.Instance(WorldId).playerList.Keys.Where(x => x != mActorController.ControllPlayerId).ToList();

            //Log.Information($"ShootPushServer player ({string.Join(",", player_list)}), {targetPos}");

            // 검증을 위해 ActionToken을 등록해둔다.
            ActionTokens[actionToken] = new ActionInfo() { SkillId = GetSkillId(IsBasicAttack) };

            InvokeClientRpc(ShootPushClient, player_list, actionToken, IsBasicAttack, targetPos);

            JSkillData skillData = ACDC.SkillData[ActionTokens[actionToken].SkillId];
            // Link Skill
            if (skillData != null && skillData.linkSkillId != 0)
            {
                var linkSkillData = ACDC.SkillData[skillData.linkSkillId];
                // 폭팔 아이디가 있는 경우만 폭탄 설치
                // 스킬에 폭탄 구분이 어려워 차후 추가되면 해당 값으로 예외처리
                if (linkSkillData != null && linkSkillData.skillType == (int)SkillType.InstallSkill)
                {
                    var bomb = (core.Bomb)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.Bomb, true, WorldId);
                    bomb.InitFrom(this, linkSkillData);
                }
            }
        }

        [ServerRPC(RequireOwnership = false)]
        public override void ShootSkillServer(byte actionToken, bool IsBasicAttack, Vector3 origin, Vector3 dir)
        {
            if (StateServerSide != ActorState.Idle)
            {
                Log.Information($"ShootSkillServer wrong state {StateServerSide}, networkID{GetNetworkId()}");
                return;
            }

            // 패킷을 송신한 유저를 제외하고 리스트 구성
            var player_list = World.Instance(WorldId).playerList.Keys.Where(x => x != mActorController.ControllPlayerId).ToList();

            //Log.Information($"ShootSkillServer player ({string.Join(",", player_list)}), {actionToken}, {IsBasicAttack}, {origin}, {dir}");

            // 검증을 위해 ActionToken을 등록해둔다.
            ActionTokens[actionToken] = new ActionInfo() { SkillId = GetSkillId(IsBasicAttack), hitIndex = new HashSet<byte>() };

            InvokeClientRpc(ShootSkillClient, player_list, actionToken, IsBasicAttack, origin, dir);
        }

        [ServerRPC(RequireOwnership = false)]
        public override void ActionResult(byte actionToken, List<int> objectList, Vector3 bombedPos)
        {
            if (ActionTokens.ContainsKey(actionToken) == false)
            {
                //Log.Information($"ActionResult already execute ({string.Join(",", objectList)}), {actionToken}");
                return;
            }

            //if(objectList.Count > 0)
            //    Log.Information($"ActionResult ({string.Join(",", objectList)}), {actionToken}");

            JSkillData skillData = ACDC.SkillData[ActionTokens[actionToken].SkillId];

            NetGameObject obj;
            for (int i = 0; i < objectList.Count; ++i)
            {
                obj = NetworkManagerServer.sInstance.GetGameObject(objectList[i], WorldId);
                if (obj == null)
                    continue;

                var applyDamage = obj.OnExplode((int)GetPlayerId(), GetNetworkId(), GetDamage(ActionTokens[actionToken].SkillId, (GameObjectClassId)obj.GetClassId()));

                // add spell
                SActor actor = obj as SActor;
                if (actor != null && skillData != null)
                {
                    //actor.AddableSpell((int)GetPlayerId(), skillData.spellId);

                    //Log.Information($"ActionResult AddSpell {skillData.spellId}");
                    foreach (int nSpellID in skillData.spellId)
                    {
                        actor.AddableSpell(this, (int)GetPlayerId(), nSpellID, applyDamage);
                    }


                }
            }

            // Link Skill
            if (skillData != null && skillData.linkSkillId != 0)
            {
                var linkSkillData = ACDC.SkillData[skillData.linkSkillId];

                if (linkSkillData != null && linkSkillData.skillType == (int)SkillType.InstallAreaOfEffect)
                {
                    AreaOfEffect aoe = (AreaOfEffect)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.AreaOfEffect, true, WorldId);
                    aoe.InitFrom(this, (ushort)linkSkillData.skillId, bombedPos);
                }
            }

            ActionTokens.Remove(actionToken);
        }

        [ServerRPC(RequireOwnership = false)]
        public override void ActionResult2(byte actionToken, List<int> objectList, int hitProjectileIdx)
        {
            ActionInfo actionInfo = null;

            //Log.Information($"ActionResult2 ({string.Join(",", objectList)}), {actionToken}, {hitProjectileIdx}");

            if (ActionTokens.TryGetValue(actionToken, out actionInfo))
            {
                if (actionInfo.hitIndex == null)
                {
                    //Log.Information($"ActionResult2 hitIndex is null ({string.Join(",", objectList)}), {actionToken}, {hitProjectileIdx}");
                    return;
                }

                if (actionInfo.hitIndex.Contains((byte)hitProjectileIdx))
                {
                    //Log.Information($"ActionResult2 already hit execute ({string.Join(",", objectList)}), {actionToken}, {hitProjectileIdx}, hitIndex:({string.Join(",", actionInfo.hitIndex)})");
                    return;
                }
            }
            else
            {
                //Log.Information($"ActionResult2 already execute ({string.Join(",", objectList)}), {actionToken}, {hitProjectileIdx}");
                return;
            }

            //Log.Information($"ActionResult2 OK objectList:({string.Join(",", objectList)}), actionToken:{actionToken}, {hitProjectileIdx}, ({string.Join(",", actionInfo.hitIndex)})");

            JSkillData skillData = ACDC.SkillData[actionInfo.SkillId];

            NetGameObject obj;
            for (int i = 0; i < objectList.Count; ++i)
            {
                obj = NetworkManagerServer.sInstance.GetGameObject(objectList[i], WorldId);
                if (obj == null)
                    continue;

                var applyDamage = obj.OnExplode((int)GetPlayerId(), GetNetworkId(), GetDamage(ActionTokens[actionToken].SkillId, (GameObjectClassId)obj.GetClassId()));

                // add spell
                SActor actor = obj as SActor;
                if (actor != null && skillData != null)
                {
                    //Log.Information($"ActionResult AddSpell {skillData.spellId}");
                    //actor.AddableSpell((int)GetPlayerId(), skillData.spellId);
                    foreach (int nSpellID in skillData.spellId)
                    {
                        actor.AddableSpell(this, (int)GetPlayerId(), nSpellID, applyDamage);
                    }
                }
            }

            actionInfo.hitIndex.Add((byte)hitProjectileIdx);
            if (skillData.projectileNum.Aggregate(1, (a, b) => a * b) <= actionInfo.hitIndex.Count)
            {
                //Log.Information($"ActionResult2 remove ({string.Join(",", objectList)}), {actionToken}, {hitProjectileIdx}, {string.Join(", ", actionInfo.hitIndex)}");
                ActionTokens.Remove(actionToken);
            }
        }

        /// <summary>
        /// 플레이어 준비 완료
        /// </summary>
        [ServerRPC(RequireOwnership = false)]
        public override void Ready()
        {
            if (IsReady == true)
            {
                Log.Information($"Ready already is set networkID:{GetNetworkId()}, playerID:{GetPlayerId()}");
                return;
            }

            Log.Information($"Ready networkID:{GetNetworkId()}, playerID:{GetPlayerId()}");

            IsReady = true;
            World.Instance(WorldId).GameMode.Ready();

            Log.Information($"Start Play Condition Reserved Count:{NetworkManagerServer.sInstance.GetReservedPlayerCount(WorldId)}, World Count:{World.Instance(WorldId).playerList.Count}");

            // 접속 예약 대기중인 유저와 월드에 입장한 유저가 같을 경우
            if (NetworkManagerServer.sInstance.GetReservedPlayerCount(WorldId) == World.Instance(WorldId).playerList.Count)
            {
                // 참가중인 모든 유저가 준비 완료
                if (World.Instance(WorldId).playerList.Count(x => ((SActor)x.Value).IsReady == false) == 0)
                {
                    World.Instance(WorldId).GameMode.StartPlay(World.Instance(WorldId).playerList.Select(x => x.Key).ToList());
                }
            }
        }


        [ServerRPC(RequireOwnership = false)]
        public override void ShootDashSkillServer(byte actionToken, bool IsBasicAttack, Vector3 targetPos, Vector3 dir)
        {
            if (StateServerSide != ActorState.Idle)
            {
                Log.Information($"ShootDashSkillServer wrong state {StateServerSide}, networkID{GetNetworkId()}");
                return;
            }

            // 패킷을 송신한 유저를 제외하고 리스트 구성
            var player_list = World.Instance(WorldId).playerList.Keys.Where(x => x != mActorController.ControllPlayerId).ToList();


            // 검증을 위해 ActionToken을 등록해둔다.
            ActionTokens[actionToken] = new ActionInfo() { SkillId = GetSkillId(IsBasicAttack), hitIndex = new HashSet<byte>() };

            JSkillData data = ACDC.SkillData[ActionTokens[actionToken].SkillId];

            float duration = 0f;
            if (data != null)
            {
                duration = data.durationTime;
            }

            //Log.Information($"ShootDashSkillServer player ({string.Join(",", player_list)}), {targetPos}, {dir} duration {duration}");

            mActorStateMachine.ChangeState(this, mActorStateMachine.UpdateDash, ActorState.Dash, duration);
            TargetPos = targetPos;

            Vector2 direction = new Vector2(targetPos.x - GetLocation().x, targetPos.z - GetLocation().z);
            degree = direction.Angle();
            lastVelocity = new Vector3(direction.x, 0, direction.y) * 0.1f;

            InvokeClientRpc(ShootDashRamClient, player_list, actionToken, IsBasicAttack, targetPos, dir);
        }


        [ServerRPC(RequireOwnership = false)]
        public override void KnockbackServer(Vector3 impact, int network_id)
        {
            var obj = NetworkManagerServer.sInstance.GetGameObject(network_id, WorldId);
            if (obj != null)
            {
                if (obj.GetClassId() == (byte)GameObjectClassId.Train)
                {
                    TakeDamage((int)ReservedPlayerId.Train, ((STrain)obj).mapData.jMapMovePathData[0].ObjectDamage);
                }
            }

            // 패킷을 송신한 유저를 제외하고 리스트 구성
            var player_list = World.Instance(WorldId).playerList.Keys.Where(x => x != mActorController.ControllPlayerId).ToList();
            InvokeClientRpc(KnockbackClient, player_list, impact);

        }

        [ServerRPC(RequireOwnership = false)]
        public override void ApplicationQuit(bool pause)
        {
            Log.Information($"ApplicationQuit pause:{pause}, networkID:{GetNetworkId()}");

            // 게임 종료 이후에는 pause 값 변경이 불가
            if (World.Instance(WorldId).GameMode.state != GameMode.GameModeState.End)
            {
                mActorController.IsPause = pause;
                World.Instance(WorldId).GameMode.PauseEntry(GetPlayerId(), pause);
            }
        }

        [ServerRPC(RequireOwnership = false)]
        public override void Hide(bool isHide, ushort map_object_uid)
        {
            //Log.Information($"Hide {isHide}, networkID:{GetNetworkId()}, map_uid:{map_object_uid}, HiddenMapObjects:{string.Join(", ", HiddenMapObjects)}");

            if (HiddenMapObjects.Count > 20)
            {
                Log.Error($"Hide error {isHide}, networkID:{GetNetworkId()}, map_uid{map_object_uid}");
                HiddenMapObjects.Clear();
            }

            if (isHide)
            {
                if (HiddenMapObjects.Contains(map_object_uid) == false)
                {
                    HiddenMapObjects.Add(map_object_uid);
                }
            }
            else
            {
                HiddenMapObjects.Remove(map_object_uid);
            }

            Dirty((uint)ReplicationState.Hide);
        }
    }
}
