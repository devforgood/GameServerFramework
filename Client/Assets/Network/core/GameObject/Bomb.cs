using Lidgren.Network;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#else
using core;
#endif


namespace core
{
    public class Bomb : NetGameObject
    {
        public static readonly float DefaultCollisionRadius = 0.45f;
        public override byte GetClassId() { return (byte)GameObjectClassId.Bomb; }


        public enum ReplicationState
        {
            Parent = 1 << 0,

            AllState = Parent 
        };

        public static NetGameObject StaticCreate(byte worldId) { return new Bomb(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }


        protected int mPlayerId;
        public int mParentNetworkId;
        public bool mIsExplode;
        public ushort mapObjectId;  // 맵에서 생성된 폭탄
        public float mExplodeTime; // 폭발까지 남은 시간
        public int mSkillId;
        public bool mIsHidden; // 숨겨진 폭탄 인가
        public byte mTeam;
        public int mDamage;
        public int mSiegeAtk;

        protected Bomb()
        {
            mPlayerId = 0;
            mParentNetworkId = 0;
            mIsExplode = false;
            SetCollisionRadius(DefaultCollisionRadius);
            mapObjectId = 0;
            mExplodeTime = 0f;
            mSkillId = 0;
            mIsHidden = false;
            mTeam = 0;
            mDamage = 0;

            // RPC
            CacheAttributes();
        }
        ~Bomb()
        {
            RemoveCacheAttributes();
        }

        public override void HandleDying()
        {
            RemoveCacheAttributes();
        }


        public void SetPlayerId(int inPlayerId) { mPlayerId = inPlayerId; }
        public int GetPlayerId() { return mPlayerId; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;


            if ((inDirtyState & (UInt32)ReplicationState.Parent) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(mapObjectId);
                if(mapObjectId == 0) // 유저가 설치한 폭탄
                {
                    inOutputStream.Write(mParentNetworkId);
                    inOutputStream.Write(mPlayerId);
                    inOutputStream.Write(ref GetLocation());
                    var remainTime = mExplodeTime - Timing.sInstance.GetFrameStartTime();
                    if (remainTime < 0f)
                        remainTime = 0f;
                    inOutputStream.Write(remainTime);
                    inOutputStream.Write(mSkillId);
                    inOutputStream.Write(mIsHidden);
                    inOutputStream.Write(mTeam);
                }

                writtenState |= (UInt32)ReplicationState.Parent;
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

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mapObjectId = inInputStream.ReadUInt16();
                if (mapObjectId == 0) // 유저가 설치한 폭탄
                {
                    mParentNetworkId = inInputStream.ReadInt32();
                    mPlayerId = inInputStream.ReadInt32();
                    Vector3 location = default(Vector3);
                    inInputStream.Read(ref location);
                    SetLocation(location);
                    mExplodeTime = inInputStream.ReadFloat() - NetworkManager.Instance.GetRoundTripTimeClientSide() * 0.5f;
                    // 남은 시간이 0초 이하면 즉시 터트린다.
                    if (mExplodeTime < 0f)
                        mExplodeTime = 0f;
                    mSkillId = inInputStream.ReadInt32();
                    mIsHidden = inInputStream.ReadBoolean();
                    mTeam = inInputStream.ReadByte();
                }
            }
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            // 맵에서 생성한 폭탄은 충돌 처리를 하지 않는다.
            if (mapObjectId != 0)
            {
                return false;
            }

            if (inActor.LastBombNetworkId == GetNetworkId())
            {
                //LogHelper.LogInfo($"bomb enter {GetNetworkId()}, {inActor.LastBombNetworkId}");
                return false;
            }


            return true;
        }
        public override void HandleExitCollisionWithActor(Actor inActor)
        {
            if (inActor.LastBombNetworkId == GetNetworkId())
            {
                //LogHelper.LogInfo($"bomb exit {GetNetworkId()}, {inActor.LastBombNetworkId}");
                inActor.LastBombNetworkId = 0;
            }
        }



        public void InitFrom(Actor inShooter, JSkillData skillData)
        {
            mDamage = inShooter.GetDamage(skillData.skillId, GameObjectClassId.Actor);
            mSiegeAtk = inShooter.GetDamage(skillData.skillId, GameObjectClassId.Prop);

            inShooter.LastBombNetworkId = GetNetworkId();
            LogHelper.LogInfo($"create enter {GetNetworkId()}, {inShooter.LastBombNetworkId}, skill_id:{skillData.skillId}, explosionID:{skillData.explosionID}");

            SetPlayerId(inShooter.GetPlayerId());
            mParentNetworkId = inShooter.GetNetworkId();

            Vector3 loc = inShooter.GetLocation();
            loc.y = inShooter.floor;
            SetLocation(loc);

            mDirection = inShooter.GetRotation();

			JExplosionData		a_kExplosionData	= ACDC.ExplosionData[ skillData.explosionID ];
            mExplodeTime = Timing.sInstance.GetFrameStartTime() + a_kExplosionData.time;
            mSkillId = skillData.skillId;

            var player = World.Instance(WorldId).GameMode.GetEntry(inShooter.GetPlayerId());
            if(player != null)
            {
                mTeam = (byte)player.GetTeam();
            }
        }

        //public void InitFrom(Trap inShooter, JSkillData skillData)
		public void InitFrom(Trap inShooter, JSkillData skillData)
        {
            mDamage = (int)skillData.Damage;
            mParentNetworkId = inShooter.GetNetworkId();

            Vector3 loc = inShooter.GetLocation();
            loc.y = inShooter.GetLocation().y + 1;
            SetLocation(loc);
            // 맵에서 생성시 (int)ReservedPlayerId.Trap 로 사용
            mPlayerId = (int)ReservedPlayerId.Trap;
            mapObjectId = inShooter.mapObjectId;

            mDirection = inShooter.GetRotation();

			JExplosionData		a_kExplosionData	=  ACDC.ExplosionData[ skillData.explosionID ];
            mExplodeTime = Timing.sInstance.GetFrameStartTime() + a_kExplosionData.time;
        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void BombResult(List<int> objectList)
        {

        }
    }
}
