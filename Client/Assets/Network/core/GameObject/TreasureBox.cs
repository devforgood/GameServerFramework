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
    public class TreasureBox : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.TreasureBox; }
        public UInt16 mapObjectId;
        public JMapObjectData mapData;
        public UInt16 ItemId = 0;
        public bool IsStarted = false;
        protected float mTimeToItemSpawn;


        protected enum ReplicationState
        {
            MapObjectID = 1 << 0,
            SpawnItem = 1 << 1,

            AllState = MapObjectID | SpawnItem
        };

        public TreasureBox()
        {

        }


        public static NetGameObject StaticCreate(byte worldId) { return new TreasureBox(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public virtual void OnAfterDeserialize(UInt32 readState) { }


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

            if ((inDirtyState & (UInt32)ReplicationState.SpawnItem) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(ItemId);
                writtenState |= (UInt32)ReplicationState.SpawnItem;
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
                mapObjectId = inInputStream.ReadUInt16();

                readState |= (UInt32)ReplicationState.MapObjectID;

            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                ItemId = inInputStream.ReadUInt16();

                readState |= (UInt32)ReplicationState.SpawnItem;

            }

            OnAfterDeserialize(readState);
        }


        public virtual void Set(JMapObjectData mapData)
        {
            //Scale = mapData.mapScale;
            Scale.x = 2f;
            Scale.y = 2f;
            widthHalf = 1f;
            heightHalf = 1f;

            SetLocation(mapData.mapPos);
        }
        public void OnStart()
        {
            LogHelper.LogInfo($"TreasureBox start {GetNetworkId()}");
            IsStarted = true;
            mTimeToItemSpawn = mapData.jAutoItemData[0].openStartTime + Timing.sInstance.GetFrameStartTime();
        }
    }
}
