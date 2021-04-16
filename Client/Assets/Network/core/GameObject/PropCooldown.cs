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
    public class PropCooldown : MapGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.PropCooldown; }
        public float StartTime;
        public float DefaultPlayTime = 3f; 

        enum ReplicationState
        {
            MapObjectID = 1 << 0,
            AllState = MapObjectID
        };

        public PropCooldown()
        {
        }

        public override int GetMapId() { return mapObjectId; }

        public float RemainTime
        {
            get
            {
                if (StartTime + DefaultPlayTime > Timing.sInstance.GetFrameStartTime())
                {
                    return StartTime + DefaultPlayTime - Timing.sInstance.GetFrameStartTime();
                }
                return 0;
            }
        }

        public static NetGameObject StaticCreate(byte worldId) { return new PropCooldown(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;


            if ((inDirtyState & (UInt32)ReplicationState.MapObjectID) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(mapObjectId);
                inOutputStream.Write(StartTime - Timing.sInstance.GetFrameStartTime());

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
                StartTime = core.Timing.sInstance.GetFrameStartTime() + inInputStream.ReadFloat() - (NetworkManager.Instance.GetRoundTripTimeClientSide() * 0.5f);
            }
        }

        public override bool DetectCollision(float sourceRadius, Vector3 sourceLocation)
        {
            return false;
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }

        public void InitFrom(int map_uid)
        {
            mapObjectId = (ushort)map_uid;
            JMapObjectData mapData;
            World.mapGameObject.TryGetValue(map_uid, out mapData);
            Set(mapData);
            StartTime = Timing.sInstance.GetFrameStartTime();

            LogHelper.LogInfo($"create propCooldown {NetworkId}, {mapObjectId}");
        }

        public void Set(JMapObjectData mapData)
        {
            Init();
            if (mapData.jMapSwitchData.Length > 0)
            {
                if (mapData.jMapSwitchData[0].coolTime > 0)
                {
                    DefaultPlayTime = mapData.jMapSwitchData[0].coolTime;
                }
            }
        }
    }
}
