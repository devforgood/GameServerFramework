using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace Server
{
    public class ActorController
    {
        public byte mSelectedCharacter;
        public int mCharacterLevel;
        public string UserId;
        public Guid mSessionId;
        public int mPlayerId;
        public byte mWorldId;
        public int ControllPlayerId;


        MoveList mUnprocessedMoveList = new MoveList();
        public MoveList GetUnprocessedMoveList() { return mUnprocessedMoveList; }


        public CharacterState mCharacterState = null;
        public CharacterState mReadCharacterState = new CharacterState();
        public CharacterState mTempCharacterState = null;

        public void SwapCharacterState()
        {
            mTempCharacterState = mReadCharacterState;
            mReadCharacterState = mCharacterState;
            mCharacterState = mTempCharacterState;
        }


        public bool IsPause = false;

        public List<AIController> AIControllers = new List<AIController>();


        public int GetAttachedAIControllerCount() { return AIControllers.Count; }

        public AIController GetAIController(Guid session_id) 
        { 
            return AIControllers.Where(x => x.mSessionId == session_id).FirstOrDefault(); 
        }

        public ActorController()
        {

        }

        public SActor SpawnActor(int inPlayerId, byte worldId, int team, int spawn_index)
        {

            SActor actor = (SActor)GameObjectRegistry.sInstance.CreateGameObject((UInt32)GameObjectClassId.Actor, true, worldId);

            var entry = World.Instance(worldId).GameMode.AddEntry(inPlayerId, this.mSessionId.ToString(), (ushort)team, spawn_index, actor.GetNetworkId());

            actor.SpawnPoint = World.spawn_position[entry.seat].mapPos;
            actor.SpawnAngle = new Vector2(World.spawn_position[entry.seat].startVector.x, World.spawn_position[entry.seat].startVector.z).Angle();
            actor.degree = actor.SpawnAngle;
            actor.SetWorldId(worldId);
            actor.SetLocation(actor.SpawnPoint);
            actor.SetPhysics();
            actor.Team = entry.GetTeam();
            actor.SetPlayerId(inPlayerId);

            //gotta pick a better spawn location than this...

            //actor.SetLocation(core.Utility.GetRandomVector(-10, 10, 0));
            actor.SetCharacter(mSelectedCharacter, mCharacterLevel, UserId);

            Log.Information($"SpawnActor player_id{inPlayerId}, UserID:{actor.UserId}, network_id{actor.NetworkId}, world{worldId}, idx{entry.seat}, pos{actor.GetLocation()}, angle{actor.degree}, direction({World.spawn_position[entry.seat].startVector.x},{World.spawn_position[entry.seat].startVector.z})");

            return actor;
        }

        public SActor GetAppropriateActorAIPossess(byte worldId, int playerId)
        {
            var actors = World.Instance(worldId).playerList.Values.Where(x => x.GetPlayerId() != playerId && ((SActor)x).GetActorController() != null).Select(x => (SActor)x).ToList();
            if (actors.Count > 0)
            {
                var minValue = actors.Min(x => x.GetActorController().GetAttachedAIControllerCount());
                foreach (var otherActor in actors)
                {
                    if (otherActor.GetActorController().GetAttachedAIControllerCount() == minValue)
                    {
                        return otherActor;
                    }
                }
            }
            return null;
        }
    }
}
