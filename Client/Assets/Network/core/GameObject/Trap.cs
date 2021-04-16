using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif 
using System.Threading.Tasks;



namespace core
{
    public class Trap : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Trap; }
        public UInt16 mapObjectId;
        public JMapObjectData mapData;
        public float mCreateTime;
        public bool IsStarted = false;


        protected enum ReplicationState
        {
            MapObjectID = 1 << 0,

            AllState = MapObjectID
        };

        public Trap()
        {

        }


        public static NetGameObject StaticCreate(byte worldId) { return new Trap(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;


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


            //LogHelper.LogInfo($"mapObjectId{mapObjectId}");

            return writtenState;
        }
        public override void Read(NetIncomingMessage inInputStream)
        {
            bool stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mapObjectId = inInputStream.ReadUInt16();

                if (mapData == null)
                {
                    if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == false)
                    {
                        Debug.Log($"cannot find map object {mapObjectId}");
                        return;
                    }
                    Set(mapData);
                }
            }

        }

        public virtual void Set(JMapObjectData mapData)
        {
            mapObjectId = mapData.uID;
            this.mapData = mapData;
            mCreateTime = mapData.jAutoBombData[0].createTime + Timing.sInstance.GetFrameStartTime();
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }

        public void OnStart()
        {
            LogHelper.LogInfo($"trap start {GetNetworkId()}");
            IsStarted = true;
            mCreateTime = mapData.jAutoBombData[0].firstStartDelayTime + Timing.sInstance.GetFrameStartTime();
        }
    }
}
