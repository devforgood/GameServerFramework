using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class AIController : ActorController
    {
        public int mNetworkId;

        /// <summary>
        /// AI 컨트롤러로 위임.
        /// 최초 할당한 유저 값만 의미가 있고 이후 위임한 유저의 값은 의미가 없음
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="original_actor"></param>
        public void Possess(SActor actor, SActor original_actor)
        {
            // 기존 유저 정보를 바탕으로 ai컨트롤러 초기화
            if (original_actor.GetActorController() != null)
            {
                mSessionId = original_actor.GetActorController().mSessionId;
                mSelectedCharacter = original_actor.GetActorController().mSelectedCharacter;
                UserId = original_actor.GetActorController().UserId;
                mCharacterLevel = original_actor.GetActorController().mCharacterLevel;
            }
            mNetworkId = original_actor.GetNetworkId();
            mPlayerId = original_actor.GetPlayerId();
            ControllPlayerId = actor.GetPlayerId();

            Log.Information($"ai Possess original player_id{mPlayerId}, attached player_id{actor.GetPlayerId()}");

            // ai 할당되었음을 동기화
            actor.AIPlayers.Add(mNetworkId);
            NetworkManagerServer.sInstance.SetStateDirty(actor.GetNetworkId(), actor.WorldId, (uint)Actor.ReplicationState.AI);

            original_actor.SetController(this);

            // 순환 참조 방지
            if (actor != original_actor)
            {
                actor.GetActorController().AIControllers.Add(this);
            }

            World.Instance(original_actor.WorldId).aiList[original_actor.GetPlayerId()] = original_actor;
        }

        /// <summary>
        /// 해당 객체에서 컨트롤로를 때어냄
        /// </summary>
        /// <param name="actor"></param>
        public void Unpossess(SActor actor)
        {
            Log.Information($"ai Unpossess original player_id{mPlayerId}, network_id{mNetworkId}");

            actor.AIPlayers.Remove(this.mNetworkId);
            NetworkManagerServer.sInstance.SetStateDirty(actor.GetNetworkId(), actor.WorldId, (uint)Actor.ReplicationState.AI);

            //actor.SetController(null);

            World.Instance(actor.WorldId).aiList.Remove(mPlayerId);
        }

        public void OnStart()
        {
            var otherActor = GetAppropriateActorAIPossess(mWorldId, mPlayerId);
            if(otherActor!=null)
            {
                var ai_actor = (SActor)NetworkManagerServer.sInstance.GetGameObject(this.mNetworkId, this.mWorldId);
                if (ai_actor != null)
                {
                    this.Possess(otherActor, ai_actor);
                }
            }

        }
    }
}
