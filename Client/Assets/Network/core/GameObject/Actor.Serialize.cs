using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif
#if _USE_BEPU_PHYSICS
#elif _USE_BULLET_SHARP
#elif _USE_BEPU_PHYSICS_V2
using BepuPhysics;
using BepuPhysics.Collidables;
#endif 


namespace core
{
    public enum AIPlayer
    {
        MaxAIPlayer = 1 << MaxAIPlayerBit,
        MaxAIPlayerBit = 4,
    }

    public partial class Actor
    {
        public enum ReplicationState
        {
            Pose = 1 << 0,
            PlayerId = 1 << 1,
            Health = 1 << 2,
            State = 1 << 3,
            Character = 1 << 4,
            Spawn = 1 << 5,
            AI = 1 << 6,
            Hide = 1 << 7,

            AllState = Pose | PlayerId | Health | State | Character | Spawn | AI | Hide
        };


        protected bool IsInterpolate;
        protected Vector3 oldRotation = new Vector3();
        protected Vector3 oldLocation = new Vector3();
        protected Vector3 oldVelocity = new Vector3();
        protected bool IsHealthUp = false;
        protected int mLastHealth = 0;


        public virtual void OnAfterDeserialize(UInt32 readState) { }
        


        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {

            UInt32 writtenState = 0;

            if ((inDirtyState & (UInt32)ReplicationState.PlayerId) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(GetPlayerId());
                inOutputStream.Write((uint)Team, (int)GameMode.MaxTeamBits);
                inOutputStream.Write(UserId);

                writtenState |= (UInt32)ReplicationState.PlayerId;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }


            if ((inDirtyState & (UInt32)ReplicationState.Pose) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(ref mVelocity);

                inOutputStream.Write(ref GetLocation());

                //inOutputStream.Write(is_move);
                //if (is_move)
                //    inOutputStream.Write(degree, 9);

                writtenState |= (UInt32)ReplicationState.Pose;

                //LogHelper.LogInfo($"send degree{degree}, is_move{is_move}");
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Health) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write((uint)mHealth, 12);

                writtenState |= (UInt32)ReplicationState.Health;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }


            if ((inDirtyState & (UInt32)ReplicationState.State) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write((int)StateServerSide, 4);
                inOutputStream.Write(ref TargetPos);
                inOutputStream.Write(JumpPower);
                inOutputStream.Write(JumpDuration);
                inOutputStream.Write((UInt32)World.Instance(WorldId).GameMode.game_mode.GetMode(), GameMode.MaxGameModeTypeBits);
                World.Instance(WorldId).GameMode.game_mode.WriteState(inOutputStream, this);

                writtenState |= (UInt32)ReplicationState.State;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }


            if ((inDirtyState & (UInt32)ReplicationState.Character) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write((byte)SelectedCharacter);
                inOutputStream.Write((uint)CharacterLevel, 6);

                writtenState |= (UInt32)ReplicationState.Character;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Spawn) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(degree, 9);

                writtenState |= (UInt32)ReplicationState.Spawn;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.AI) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write((UInt32)AIPlayers.Count, (int)AIPlayer.MaxAIPlayerBit);
                //LogHelper.LogInfo($"ai count {AIPlayers.Count}");
                for (int i = 0; i < AIPlayers.Count && i< (int)AIPlayer.MaxAIPlayer ; ++i)
                {
                    inOutputStream.Write(AIPlayers[i]);
                    LogHelper.LogInfo($"ai player {AIPlayers[i]}");
                }

                writtenState |= (UInt32)ReplicationState.AI;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Hide) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write((UInt32)HiddenMapObjects.Count, 4);
                foreach(ushort object_uid in HiddenMapObjects)
                    inOutputStream.Write(object_uid);

                writtenState |= (UInt32)ReplicationState.Hide;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }
            return writtenState;
        }

        public override void Read(NetIncomingMessage inInputStream)
        {
            bool stateBit = inInputStream.ReadBoolean();

            UInt32 readState = 0;

            if (stateBit)
            {
                int playerId = inInputStream.ReadInt32();
                SetPlayerId(playerId);
                Team = (core.Team)inInputStream.ReadUInt32(GameMode.MaxTeamBits);
                UserId = inInputStream.ReadString();

                readState |= (UInt32)ReplicationState.PlayerId;
            }

            oldRotation = GetRotation();
            oldLocation = GetLocation();
            oldVelocity = GetVelocity();

            Vector3 replicatedLocation = default(Vector3);
            Vector3 replicatedVelocity = default(Vector3);

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                inInputStream.Read(ref replicatedVelocity);
                //replicatedVelocity.y = GetVelocity().y;
                SetVelocity(replicatedVelocity);
                //Debug.Log("replicatedVelocity : " + replicatedVelocity + ", player_id :" + GetPlayerId());

                inInputStream.Read(ref replicatedLocation);
                //replicatedLocation.y = GetLocation().y;
                SetLocation(replicatedLocation);
                //LogHelper.LogInfo($"replicatedLocation : {replicatedLocation.x},  {replicatedLocation.y}, {replicatedLocation.z}, player_id : {GetPlayerId()}");

                //if (replicatedVelocity.IsZero())
                //    mThrustDir = 0f;
                //else
                //    mThrustDir = 1.0f;

                //is_move = inInputStream.ReadBoolean();
                //if (is_move)
                //{
                //    degree = inInputStream.ReadUInt32(9);
                //    mDirection = core.MathHelpers.DegreeToVector3Cached((int)degree);
                //}

                if(replicatedVelocity.x != 0 || replicatedVelocity.z != 0)
                    mDirection = new Vector3(replicatedVelocity.x, 0, replicatedVelocity.z);

                //Debug.Log($"degree{degree}, mDirection : {mDirection}, player_id :{GetPlayerId()}");

                readState |= (UInt32)ReplicationState.Pose;
            }
            // Health
            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                IsHealthUp = false;
                var HP = (int)inInputStream.ReadUInt32(12);
                if(mHealth <= HP)
                {
                    IsHealthUp = true;
                }
                mLastHealth = mHealth;
                mHealth = HP;

                readState |= (UInt32)ReplicationState.Health;
            }

            // State
            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                IsInterpolate = true;
                LastStateServerSide = StateServerSide;
                StateServerSide = (core.ActorState)inInputStream.ReadInt32(4);
                LogHelper.LogInfo($"state serverside {StateServerSide}");

                inInputStream.Read(ref TargetPos);
                JumpPower = inInputStream.ReadFloat();
                JumpDuration = inInputStream.ReadFloat();

                GameModeType mode = (GameModeType)inInputStream.ReadUInt32(GameMode.MaxGameModeTypeBits);
                switch(mode)
                {
                    case GameModeType.KillTheKing:
                        {
                            isLastKilled = inInputStream.ReadBoolean();
                            if (isLastKilled)
                            {
                                killPlayerId = inInputStream.ReadInt32();
                                //Debug.Log($"public bool ReadState(NetIncomingMessage inOutputStream, Actor actor) killPlayerId : {killPlayerId}");
                            }
                        }
                        break;
                    default:
                        break;
                }

                readState |= (UInt32)ReplicationState.State;

            }

            // Character
            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                SelectedCharacter = inInputStream.ReadByte();
                CharacterLevel = (int)inInputStream.ReadUInt32(6);
                SetCharacterData(SelectedCharacter, CharacterLevel);
                LogHelper.LogInfo($"Setting Character {characterData.Remark}");

                readState |= (UInt32)ReplicationState.Character;
            }

            // Spawn
            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                degree = inInputStream.ReadUInt32(9);
                mDirection = core.MathHelpers.DegreeToVector3Cached((int)degree);

                readState |= (UInt32)ReplicationState.Spawn;
            }

            // AI
            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                AIPlayers.Clear();
                var cnt = inInputStream.ReadUInt32((int)AIPlayer.MaxAIPlayerBit);
                //LogHelper.LogInfo($"ai count {cnt}");
                for (int i = 0; i < cnt; ++i)
                {
                    AIPlayers.Add(inInputStream.ReadInt32());
                    LogHelper.LogInfo($"ai player network_id:{AIPlayers[i]}");
                }

                readState |= (UInt32)ReplicationState.AI;
            }

            // Hide
            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                var size = (int)inInputStream.ReadUInt32(4);
                HiddenMapObjects.Clear();
                for (int i=0;i<size;++i)
                    HiddenMapObjects.Add(inInputStream.ReadUInt16());
                if (HiddenMapObjects.Count > 0)
                    IsHide = true;
                else
                    IsHide = false;

                readState |= (UInt32)ReplicationState.Hide;
                //LogHelper.LogInfo($"hide {IsHide}");
            }

            OnAfterDeserialize(readState);
        }
    }
}
