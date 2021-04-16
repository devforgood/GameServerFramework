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

    public class Buff : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Buff; }

        public int mParentNetworkId;
        public int mSpellId;
        float mRemainTime; // 버프 효과가 끝나는 시간 (동기화시 남은 시간을 보냄)

        public float mAddStatus; // shield health, addspeed 등
        float mNextTickTime;

        public JSpellData mSpellData;
        public float RemainTime { get { return mRemainTime; } }

        public int SpellId { get { return mSpellId; } }

        protected enum ReplicationState
        {
            Base = 1 << 0,
            AddStatus = 1 << 1,
            AllState = Base | AddStatus
        };

        public Buff()
        {
        }


        public static NetGameObject StaticCreate(byte worldId) { return new Buff(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;


            if ((inDirtyState & (UInt32)ReplicationState.Base) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(mParentNetworkId, NetGameObject.NetworkIdSize);
                inOutputStream.Write((byte)mSpellId);
                inOutputStream.Write(mRemainTime - Timing.sInstance.GetFrameStartTime());

                writtenState |= (UInt32)ReplicationState.Base;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.AddStatus) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(mAddStatus);

                writtenState |= (UInt32)ReplicationState.AddStatus;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            //LogHelper.LogInfo($"mapObjectId{mapObjectId}");

            return writtenState;
        }
        public override void Read(NetIncomingMessage inInputStream)
        {
            UInt32 readState = 0;

            bool stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mParentNetworkId = inInputStream.ReadInt32(NetGameObject.NetworkIdSize);
                mSpellId = inInputStream.ReadByte();
                mRemainTime = Timing.sInstance.GetFrameStartTime() + inInputStream.ReadFloat() - (NetworkManager.Instance.GetRoundTripTimeClientSide()*0.5f);

                readState |= (UInt32)ReplicationState.Base;

            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mAddStatus = inInputStream.ReadFloat();

                readState |= (UInt32)ReplicationState.AddStatus;
            }

            OnAfterDeserialize(readState);
        }

        public virtual void OnAfterDeserialize(UInt32 readState) { }


        public override bool DetectCollision(float sourceRadius, Vector3 sourceLocation)
        {
            return false;
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }

        public BuffType GetBuffType() 
        {
            return (BuffType)mSpellData.BuffID;
        }

        public int GetResetId()
        {
            return mSpellData.ResetID;
        }

        public void Set(JSpellData data, int parentNetworkId, float value)
        {
            LogHelper.LogInfo($"create Buff {NetworkId}");

            mParentNetworkId = parentNetworkId;
            mSpellData = data;
            mSpellId = data.Index;

            if(mSpellData.AddStatusType== (int)AddStatusType.Absolute)
                mAddStatus = mSpellData.AddStatus;
            else if(mSpellData.AddStatusType == (int)AddStatusType.Relative)
                mAddStatus = value * mSpellData.AddStatus;

            mRemainTime = Timing.sInstance.GetFrameStartTime() + mSpellData.RetentionTime;
            mNextTickTime = Timing.sInstance.GetFrameStartTime() + mSpellData.TickTime;
        }

        public bool NextTickTime()
        {
            if (mNextTickTime <= Timing.sInstance.GetFrameStartTime())
            {
                mNextTickTime = mNextTickTime + mSpellData.TickTime;
                return true;
            }
            return false;
        }

        public bool IsExpired()
        {
            if (mRemainTime <= Timing.sInstance.GetFrameStartTime())
                return true;

            return false;
        }

        public void SetAddStatus(float addStatus)
        {
            mAddStatus = addStatus;
            Dirty((int)ReplicationState.AddStatus);
        }
    }
}
