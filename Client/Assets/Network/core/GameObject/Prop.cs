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
    public class Prop : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Prop; }
        public UInt16 mapObjectId;
        public bool IsDie = false;

#if _USE_BEPU_PHYSICS
        public BEPUphysics.ISpaceObject collision;
#endif

        protected enum ReplicationState
        {
            MapObjectID = 1 << 0,
            Die = 1 << 1,
            AllState = MapObjectID | Die
        };

        public Prop()
        {
            SetScale(GetScale() * 0.5f);
            SetCollisionRadius(0.25f);

            // RPC
            CacheAttributes();
        }

        ~Prop()
        {
            // RPC
            RemoveCacheAttributes();
        }

        public override void HandleDying()
        {
            // RPC
            RemoveCacheAttributes();
        }

        public static NetGameObject StaticCreate(byte worldId) { return new Prop(); }

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

            if ((inDirtyState & (UInt32)ReplicationState.Die) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(IsDie);
                writtenState |= (UInt32)ReplicationState.Die;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            //LogHelper.LogInfo($"mapObjectId{mapObjectId}");

            return writtenState;
        }
        public override void Read(NetIncomingMessage inInputStream )
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
                IsDie = inInputStream.ReadBoolean();

                readState |= (UInt32)ReplicationState.MapObjectID;

            }

            OnAfterDeserialize(readState);
        }

        public virtual void OnAfterDeserialize(UInt32 readState) { }


        (Vector3, Vector2) RelocateBlock(JMapObjectData map_object)
        {
            Vector3 pos = map_object.mapPos;
            Vector2 scale = map_object.mapScale;
            if (map_object.mapScale.x > 1)
            {
                if (map_object.mapRotY == 90f)
                {
                    pos.z -= 1;
                    scale.y = map_object.mapScale.x;
                    scale.x = 1;
                    return (pos, scale);
                }
                else if (map_object.mapRotY == 270f || map_object.mapRotY == -90f)
                {
                    //pos.z -= 1;
                    scale.y = map_object.mapScale.x;
                    scale.x = 1;
                    return (pos, scale);
                }
                else if (map_object.mapRotY == 180f)
                    pos.x -= 1;
                else if (map_object.mapRotY == 0.0f)
                {

                }
                else
                {
                    throw new System.Exception("error angle value");
                }
            }

            if (map_object.mapScale.y > 1)
            {
                if (map_object.mapRotY == 90f)
                {

                }
                else if (map_object.mapRotY == 270f || map_object.mapRotY == -90f)
                    pos.x -= 1;
                else if (map_object.mapRotY == 180f)
                    pos.z -= 1;
                else if (map_object.mapRotY == 0.0f)
                {

                }
                else
                {
                    throw new System.Exception("error angle value");
                }
            }
            return (pos, scale);
        }

        public void Set(JMapObjectData map_object)
        {
            (var pos, var scale) = RelocateBlock(map_object);

            SetLocation(pos);
            mapObjectId = map_object.uID;
            Scale = scale;
        }

        [ServerRPC(RequireOwnership = false)]
        public virtual void PropExplodeResult(List<int> objectList)
        {

        }
    }
}
