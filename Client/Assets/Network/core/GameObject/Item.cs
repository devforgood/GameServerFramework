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
    public class Item : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Item; }
        public UInt16 ItemId;


        enum ReplicationState
        {
            ItemID = 1 << 0,
            Pose = 1 << 1,
            AllState = ItemID | Pose
        };

        public Item()
        {
            SetScale(GetScale() * 0.5f);
            SetCollisionRadius(0.25f);
        }


        public static NetGameObject StaticCreate(byte worldId) { return new Item(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;


            if ((inDirtyState & (UInt32)ReplicationState.ItemID) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(ItemId);
                writtenState |= (UInt32)ReplicationState.ItemID;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Pose) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(ref GetLocation());

                writtenState |= (UInt32)ReplicationState.Pose;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            //LogHelper.LogInfo($"ItemId{ItemId}");

            return writtenState;
        }
        public override void Read(NetIncomingMessage inInputStream)
        {
            bool stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                ItemId = inInputStream.ReadUInt16();
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                Vector3 location = default(Vector3);
                inInputStream.Read(ref location);

                //dead reckon ahead by rtt, since this was spawned a while ago!
                SetLocation(location);
            }
        }
    }
}
