using System;
using System.Collections.Generic;
using System.Text;
using core;
using Serilog;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif


namespace Server
{
    public class ActorStateMachine
    {
        public delegate void UpdateStateMachine(SActor actor);
        public UpdateStateMachine updateStateMachine;

        public ActorStateMachine()
        {
            updateStateMachine = UpdateIdle;
        }


        public void ChangeState(SActor actor, UpdateStateMachine updateStateMachine, ActorState state, float duration)
        {
            actor.ChangeStateServerSide(state, duration);
            actor.BeginState = Timing.sInstance.GetFrameStartTime();
            NetworkManagerServer.sInstance.SetStateDirty(actor.GetNetworkId(), actor.WorldId, (uint)Actor.ReplicationState.State);
            this.updateStateMachine = updateStateMachine;
        }

        public void Update(SActor actor)
        {
            updateStateMachine.Invoke(actor);
        }

        public void UpdateIdle(SActor actor)
        {
#if _USE_INPUT_SYNC
            actor.UpdateMove();
#else
            actor.UpdateState();
#endif
        }

        public void UpdateTeleport(SActor actor)
        {
            if (actor.EndState <= Timing.sInstance.GetFrameStartTime())
            {
                actor.mActorController?.GetUnprocessedMoveList().Clear();

                actor.SetLocation(actor.TargetPos);
                LogHelper.LogInfo($"end teleport pos {actor.TargetPos.ToString()}, degree{actor.degree}");
                ChangeState(actor, UpdateIdle, ActorState.Idle, 0f);
            }
        }

        public void UpdateGhost(SActor actor)
        {
            if (actor.EndState <= Timing.sInstance.GetFrameStartTime())
            {
                actor.mActorController?.GetUnprocessedMoveList().Clear();

                actor.SetLocation(actor.TargetPos);
                actor.SetVelocity(Vector3.zero);
                actor.degree = actor.SpawnAngle;
                actor.HiddenMapObjects.Clear();
                actor.killPlayerId = 0;

                LogHelper.LogInfo($"end ghost pos {actor.TargetPos.ToString()}");
                ChangeState(actor, UpdateIdle, ActorState.Idle, 0f);
                NetworkManagerServer.sInstance.SetStateDirty(actor.GetNetworkId(), actor.WorldId, (uint)Actor.ReplicationState.Spawn);

                actor.AddSpellMySelf(core.BuffType.Invincible);
                World.Instance(actor.WorldId).GameMode.game_mode.OnTrigger(actor.WorldId, actor.GetPlayerId(), PlayPointID.PlayerReborn);
            }
        }
        public void UpdateDash(SActor actor)
        {
            if (actor.EndState <= Timing.sInstance.GetFrameStartTime())
            {
                actor.mActorController?.GetUnprocessedMoveList().Clear();

                actor.SetLocation(actor.TargetPos);
                actor.SetVelocity(Vector3.zero);
                Log.Information($"end dash pos {actor.TargetPos.ToString()}");
                ChangeState(actor, UpdateIdle, ActorState.Idle, 0f);
                NetworkManagerServer.sInstance.SetStateDirty(actor.GetNetworkId(), actor.WorldId, (uint)Actor.ReplicationState.Pose);
            }
        }
    }
}
