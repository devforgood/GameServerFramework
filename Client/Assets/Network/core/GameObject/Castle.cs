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
    public class Castle : NetGameObject
    {
        public override byte GetClassId() { return (byte)GameObjectClassId.Castle; }

        protected enum ReplicationState
        {
            Health = 1 << 0,
            Buff = 1 << 1,
            MapObjectID = 1 << 2,
            AllState = Health | Buff | MapObjectID
        };

        public Castle()
        {
            // RPC
            CacheAttributes();
        }

        public override void HandleDying()
        {
            RemoveCacheAttributes();
        }

        public static NetGameObject StaticCreate(byte worldId) { return new Castle(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }
        public virtual void OnAfterDeserialize(UInt32 readState) { }

        // 기지 HP
        public ushort hp = 0;
        public ushort hp_max = 0;

        // 방어막 HP
        public ushort shield_hp = 0;
        public ushort shield_hp_max = ushort.MaxValue;

        // 방어막 유지 시간
        public float shield_time = 0f;

        // 맵오브젝트 아이디
        // 팀정보등을 얻을수 있다.
        public UInt16 mapObjectId;

        public JMapObjectData mapData;

        public byte Team;
        protected bool IsHealthUp = false;

#if _USE_BEPU_PHYSICS
        public BEPUphysics.ISpaceObject collision;
#endif

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
                inOutputStream.Write(hp);
                writtenState |= (UInt32)ReplicationState.Health;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Buff) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(shield_hp);
                inOutputStream.Write(shield_time - Timing.sInstance.GetFrameStartTime() ); // 방어막 남은 시간
                writtenState |= (UInt32)ReplicationState.Buff;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }


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
                var last_hp = hp;
                hp = inInputStream.ReadUInt16();

                IsHealthUp = false;
                if (last_hp<=hp && (readState & (UInt32)ReplicationState.MapObjectID) == 0)
                {
                    IsHealthUp = true;
                }
                readState |= (UInt32)ReplicationState.Health;
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                shield_hp = inInputStream.ReadUInt16();
                // 클라 서버간 시간차 조정 필요
                shield_time = Timing.sInstance.GetFrameStartTime() + inInputStream.ReadFloat() - NetworkManager.Instance.GetRoundTripTimeClientSide();

                readState |= (UInt32)ReplicationState.Buff;
            }

            OnAfterDeserialize(readState);

        }

        /// <summary>
        /// 기지 충돌 여부
        /// 기지 충돌은 하지 않고 승패를 위한 정보만 처리
        /// 충돌 처리는 스테틱오브젝트에서 하도록 한다.
        /// </summary>
        /// <param name="inActor"></param>
        /// <returns></returns>
        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }

        /// <summary>
        /// 방어막이 유효한가
        /// </summary>
        /// <returns></returns>
        public bool VaildShield()
        {
            return shield_hp > 0 && shield_time > Timing.sInstance.GetFrameStartTime();
        }

        /// <summary>
        /// 방어막 남은 시간
        /// </summary>
        /// <returns></returns>
        public float GetShieldRaminTime()
        {
            return shield_time - Timing.sInstance.GetFrameStartTime();
        }

        public void BuffShield(JSpellData data)
        {
            shield_time = Timing.sInstance.GetFrameStartTime() + data.RetentionTime;
            if(data.AddStatusType== (int)AddStatusType.Absolute)
                shield_hp = (ushort)data.AddStatus;
            else if(data.AddStatusType == (int)AddStatusType.Relative)
                shield_hp = (ushort)(hp * data.AddStatus);

            Dirty((uint)ReplicationState.Buff);
        }
        public void BuffRecovery(JSpellData data)
        {
            if (data.AddStatusType == (int)AddStatusType.Absolute)
                hp += (ushort)data.AddStatus;
            else if(data.AddStatusType == (int)AddStatusType.Relative)
                hp += (ushort)(hp * data.AddStatus);

            // max hp 보다 높을 경우
            if (hp > (ushort)mapData.jCastleData[0].castleHP)
                hp = (ushort)mapData.jCastleData[0].castleHP;

            Dirty((uint)ReplicationState.Health);
        }

        public void Set(JMapObjectData mapData)
        {
            this.mapData = mapData;
            mapObjectId = mapData.uID;
            hp = (ushort)mapData.jCastleData[0].castleHP;
            hp_max = (ushort)mapData.jCastleData[0].castleHP;
            Team = (byte)mapData.jCastleData[0].castleTeam;
        }

        [ClientRPC]
        public virtual void NoticeHealth(int health)
        {

        }
    }
}
