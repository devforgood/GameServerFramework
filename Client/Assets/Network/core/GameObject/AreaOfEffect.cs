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
    public class AreaOfEffect : NetGameObject
    {
        public JSkillData mSkillData;
        public JSpellData mFinishSpell = null;
        public JSpellData mMiddleSpell = null;

		public		JExplosionData		m_kExplosionData	= null;
        public float mDurationTime;
        public eExplosionType mExplosionType = eExplosionType.eNONE;

        public override byte GetClassId() { return (byte)GameObjectClassId.AreaOfEffect; }
        public UInt16 SkillId;


        enum ReplicationState
        {
            SkillID = 1 << 0,
            Pose = 1 << 1,
            PlayerId = 1 << 2,
            Parent = 1 << 3,
            DurationTime = 1 << 4,
            Team = 1<< 5,
            AllState = SkillID | Pose | PlayerId | Parent | DurationTime
        };

        protected int mPlayerId;
        public int mParentNetworkId;
        protected int mDamage;
        protected int mSiegeAtk;
        public byte mTeam;

        public AreaOfEffect()
        {
            LogHelper.LogInfo($"AreaOfEffect Create Time {Timing.sInstance.GetFrameStartTime()} Pos {GetLocation()}");
            SetScale(GetScale() * 0.5f);
            SetCollisionRadius(2f);
            mPlayerId = 0;
            mParentNetworkId = 0;
            mDamage = 0;
            mTeam = 0;
        }

        public override AreaOfEffect GetAsAreaOfEffect() { return this; }
        public void SetPlayerId(int inPlayerId) { mPlayerId = inPlayerId; }
        public int GetPlayerId() { return mPlayerId; }


        public static NetGameObject StaticCreate(byte worldId) { return new AreaOfEffect(); }

        public override UInt32 GetAllStateMask() { return (UInt32)ReplicationState.AllState; }

        public override UInt32 Write(NetOutgoingMessage inOutputStream, UInt32 inDirtyState)
        {
            UInt32 writtenState = 0;


            if ((inDirtyState & (UInt32)ReplicationState.SkillID) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(SkillId);
                writtenState |= (UInt32)ReplicationState.SkillID;
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

            if ((inDirtyState & (UInt32)ReplicationState.PlayerId) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(mPlayerId);

                writtenState |= (UInt32)ReplicationState.PlayerId;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Parent) != 0)
            {
                inOutputStream.Write((bool)true);

                inOutputStream.Write(mParentNetworkId);

                writtenState |= (UInt32)ReplicationState.Parent;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.DurationTime) != 0)
            {
                inOutputStream.Write((bool)true);

                var remainTime = mDurationTime - Timing.sInstance.GetFrameStartTime();
                if (remainTime < 0f)
                    remainTime = 0f;
                inOutputStream.Write(remainTime);

                writtenState |= (UInt32)ReplicationState.DurationTime;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }

            if ((inDirtyState & (UInt32)ReplicationState.Team) != 0)
            {
                inOutputStream.Write((bool)true);
                inOutputStream.Write(mTeam, GameMode.MaxTeamBits);

                writtenState |= (UInt32)ReplicationState.Team;
            }
            else
            {
                inOutputStream.Write((bool)false);
            }


            //LogHelper.LogInfo($"SkillId{SkillId}");

            return writtenState;
        }

        public override void Read(NetIncomingMessage inInputStream)
        {
            bool stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                SkillId = inInputStream.ReadUInt16();
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                Vector3 location = default(Vector3);
                inInputStream.Read(ref location);

                //dead reckon ahead by rtt, since this was spawned a while ago!
                LogHelper.LogInfo($"CArea Loc {location}");
                SetLocation(location);
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mPlayerId = inInputStream.ReadInt32();
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mParentNetworkId = inInputStream.ReadInt32();
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mDurationTime = inInputStream.ReadFloat() - NetworkManager.Instance.GetRoundTripTimeClientSide() * 0.5f;
                // 남은 시간이 0초 이하면 즉시 터트린다.
                if (mDurationTime < 0f)
                    mDurationTime = 0f;
            }

            stateBit = inInputStream.ReadBoolean();
            if (stateBit)
            {
                mTeam = inInputStream.ReadByte(GameMode.MaxTeamBits);
            }
        }

        public void InitFrom(Actor inShooter, ushort skillId, Vector3 bomedPos)
        {
            Vector3 loc = bomedPos;
            LogHelper.LogInfo($"Area bombed {bomedPos.y} Loc {loc.y}");
            SetLocation(loc);
            SkillId = skillId;
            SetPlayerId((int)inShooter.GetPlayerId());
            mParentNetworkId = inShooter.GetNetworkId();

            mSkillData = ACDC.SkillData[ skillId ];
			m_kExplosionData	= ACDC.ExplosionData[ mSkillData.explosionID ];
            mDamage = inShooter.GetDamage(skillId, GameObjectClassId.Actor);
            mSiegeAtk = inShooter.GetDamage(skillId, GameObjectClassId.Prop);
            mTeam = (byte)inShooter.Team;
            // 주의 사항 ---- 같은 서브타입이 두개 이상 존재하면 마지막 것으로 초기화 됨
            if (mSkillData.spellId != null)
            {
                for (int i = 0; i < mSkillData.spellId.Length; ++i)
                {
                    var spell = ACDC.SpellData[mSkillData.spellId[i]];
                    if (spell == null)
                        continue;

                    if (spell.SpellSubType == (int)SpellSubType.FinishTime)
                        mFinishSpell = spell;
                    else if (spell.SpellSubType == (int)SpellSubType.MiddleTime)
                        mMiddleSpell = spell;
                }
            }

            mDirection = inShooter.GetRotation();
            mDurationTime = Timing.sInstance.GetFrameStartTime() + mSkillData.durationTime;
            mExplosionType = (eExplosionType)m_kExplosionData.type;
            SetCollisionRadius(m_kExplosionData.range[0]);
            if (m_kExplosionData.range.Count() > 1)
            {
                heightHalf = m_kExplosionData.range[1];
            }
            LogHelper.LogInfo($"InitFrom NID {GetNetworkId()} bomedPos {bomedPos.y} Pos {GetLocation().y} explosionType {mExplosionType}");
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            return false;
        }

        public override bool DetectCollision(float sourceRadius, Vector3 sourceLocation)
        {
            if(mExplosionType == eExplosionType.eCIRCLE)
            {
                //LogHelper.LogInfo($"circle {MathHelpers.circlesColliding(sourceLocation.x, sourceLocation.z, sourceRadius, this.GetLocation().x, this.GetLocation().z, this.GetCollisionRadius())}");
                return MathHelpers.circlesColliding(sourceLocation.x, sourceLocation.z, sourceRadius, this.GetLocation().x, this.GetLocation().z, this.GetCollisionRadius());
            }
            else
            {
                //LogHelper.LogInfo($"rect {MathHelpers.circleRect(sourceLocation.x, sourceLocation.z, sourceRadius, this.rx, this.ry, this.rw, this.rh)}");
                return MathHelpers.circleRect(sourceLocation.x, sourceLocation.z, sourceRadius, this.rx, this.ry, this.rw, this.rh);
            }
        }

        [ClientRPC]
        public virtual void CreateAOEClient(Vector3 bomedPos)
        { 
        }
    }
}
