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
    public class PropHealth : MapGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.PropHealth; }
        public UInt16 mHealth;

        protected enum ReplicationState
        {
            MapObjectID = 1 << 0,
            Health = 1 << 1,
            AllState = MapObjectID | Health
        };

        public PropHealth()
        {
        }

        public override int GetMapId() { return mapObjectId; }

        public static NetGameObject StaticCreate(byte worldId) { return new PropHealth(); }

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

            if ((inDirtyState & (UInt32)ReplicationState.Health) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(mHealth);

                writtenState |= (UInt32)ReplicationState.Health;
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
                mHealth = inInputStream.ReadUInt16();

                readState |= (UInt32)ReplicationState.Health;
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

        public void InitFrom(JMapObjectData mapData)
        {
            mapObjectId = (ushort)mapData.uID;
            mHealth = mapData.objectHP;
            Set(mapData);
            //LogHelper.LogInfo($"create propHealth {NetworkId}, {mapObjectId}");
        }

        public void Set(JMapObjectData mapData)
        {
            Init();
        }
    }
}
