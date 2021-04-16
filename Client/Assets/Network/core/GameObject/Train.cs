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
    public class Train : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Train; }


        protected enum ReplicationState
        {
            Pose = 1 << 0,
            MapObjectID = 1 << 1,
            State = 1 << 2,

            AllState = Pose | MapObjectID | State
        };

        public enum TrainState
        {
            None,
            Init,
            Ready,
            Run,
        }

        public static NetGameObject StaticCreate(byte worldId) { return new Train(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }


        float speed;
        protected Vector3 mVelocity;


        public UInt16 mapObjectId;

        public JMapObjectData mapData;

        public float mCreateTime;

        public TrainState mState = TrainState.None;
        public TrainState mLastState = TrainState.None;

        public bool IsStarted = false;
        public bool IsRun = false;
        public bool IsResetLocation = false;

        protected Train()
        {
        }


        public void SetVelocity(Vector3 inVelocity) { mVelocity = inVelocity; }
        public ref Vector3 GetVelocity() { return ref mVelocity; }


        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;

            if ((inDirtyState & (UInt32)ReplicationState.Pose) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(ref GetLocation());

                inOutputStream.Write(ref GetVelocity());

                inOutputStream.Write(IsResetLocation);

                writtenState |= (UInt32)ReplicationState.Pose;

                //core.LogHelper.LogInfo($"train pos x{GetLocation().x}, y{GetLocation().y}, z{GetLocation().z}, IsResetLocation{IsResetLocation}");

            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.MapObjectID) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(mapObjectId);
                writtenState |= (UInt32)ReplicationState.MapObjectID;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.State) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write((UInt32)mState, 3);

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
            UInt32 readState = 0;

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                Vector3 location = default(Vector3);
                inInputStream.Read(ref location);


                Vector3 velocity = default(Vector3);
                inInputStream.Read(ref velocity);
                SetVelocity(velocity);

                SetLocation(location + velocity * (NetworkManager.Instance.GetRoundTripTimeClientSide() * 0.5f));

                IsResetLocation = inInputStream.ReadBoolean();

                //core.LogHelper.LogInfo($"train pos x{GetLocation().x}, y{GetLocation().y}, z{GetLocation().z}, IsResetLocation{IsResetLocation}");

                readState |= (UInt32)ReplicationState.Pose;
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mapObjectId = inInputStream.ReadUInt16();

                readState |= (UInt32)ReplicationState.MapObjectID;
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mLastState = mState;
                mState = (TrainState)inInputStream.ReadUInt32(3);

                readState |= (UInt32)ReplicationState.State;
            }

            OnAfterDeserialize(readState);
        }

        public virtual void OnAfterDeserialize(UInt32 readState) { }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }

        public void Set(JMapObjectData mapData)
        {
            LogHelper.LogInfo($"train set networkId:{GetNetworkId()}, worldId:{WorldId}, mapUid:{mapData.uID}");

            mapObjectId = mapData.uID;
            this.mapData = mapData;
            SetLocation(mapData.jMapMovePathData[0].moveStartPos);
            speed = mapData.jMapMovePathData[0].moveSpeed;
            mVelocity = mapData.jMapMovePathData[0].moveEndPos - mapData.jMapMovePathData[0].moveStartPos;
            mVelocity.Normalize();
            mVelocity = mVelocity * speed;

            // todo : 테스트용
            //IsStarted = true;
            //IsRun = true;
            //mCreateTime = Timing.sInstance.GetFrameStartTime();
        }

        /// <summary>
        /// 게임 모드에 의해 시작됨, 게임 시작 이후 최초 한번 호출
        /// </summary>
        public void OnStart()
        {
            LogHelper.LogInfo($"train start {GetNetworkId()}");
            IsStarted = true;
            mCreateTime = mapData.jMapMovePathData[0].firstStartDelayTime + Timing.sInstance.GetFrameStartTime();
            IsRun = true;
            mState = TrainState.Init;
        }
    }
}
