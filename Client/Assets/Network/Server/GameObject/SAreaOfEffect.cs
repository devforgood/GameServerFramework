using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ActorInAreaTime
    {
        public Actor _actor;
        public float _damagedTime;
        public float _tickTime;
    }

    public partial class SAreaOfEffect : AreaOfEffect
    {        
        private Dictionary<int, ActorInAreaTime> mActorInAreaList = new Dictionary<int, ActorInAreaTime>();
        private List<int> mRemoveDic = new List<int>();
        bool _isLife = false;

        protected SAreaOfEffect()
        {
            _isLife = true;
#if UNITY_EDITOR || DEBUG
            Log.Information($"SAreaOfEffect Create Time {Timing.sInstance.GetFrameStartTime()} Pos {GetLocation()}");
#endif
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SAreaOfEffect(), worldId); }

        public override void HandleDying()
        {
            _isLife = false;
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
#if UNITY_EDITOR || DEBUG
            Log.Information($"SAreaOfEffect handle dying Time {Timing.sInstance.GetFrameStartTime()}");
#endif
        }

        bool CheckSkillTarget(Actor inActor)
        {
            if(mSkillData.skillTarget == (int)SkillTarget.SelfAndAlly)
            {
                if (inActor.Team != (Team)mTeam)
                    return false;
            }
            else if(mSkillData.skillTarget == (int)SkillTarget.Enemy)
            {
                if (inActor.Team == (Team)mTeam)
                    return false;
            }

            return true;
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            if (_isLife == true && mActorInAreaList.ContainsKey(inActor.NetworkId) == false
                && CheckSkillTarget(inActor)
                )
            {
                ActorInAreaTime data = new ActorInAreaTime();
                data._actor = inActor;
                data._damagedTime = Timing.sInstance.GetFrameStartTime();
                data._tickTime = Timing.sInstance.GetFrameStartTime() + m_kExplosionData.tickTime;
                mActorInAreaList.Add(inActor.NetworkId, data);
                //((SActor)inActor).TakeDamage(id, damage);

#if UNITY_EDITOR || DEBUG
                Log.Information($"SAreaOfEffect HandleCollisionWithActor Damage {data._actor.GetNetworkId()} Time {data._tickTime}");
#endif
            }

            return false;
        }

        public override void HandleExitCollisionWithActor(Actor inActor)
        {
            if (mActorInAreaList.ContainsKey(inActor.NetworkId) == true)
            {
#if UNITY_EDITOR || DEBUG
                Log.Information($"SAreaOfEffect HandleExitCollisionWithActor 2 {mActorInAreaList.Count} isLife { _isLife}");
#endif
                //if (mSkillData.spellId != 0)
                //{
                //    // 장판 영역밖으로 나가면 스펠 걸어줌.
                //    AddSpell(inActor);
                //}
                //mRemoveDic.Add(inActor.NetworkId);
                if (mFinishSpell != null)
                {
                    // 장판 영역밖으로 나가면 스펠 걸어줌.
                    AddSpell(inActor, mFinishSpell);
                }
                mRemoveDic.Add(inActor.NetworkId);

            }
        }

        public override void Update()
        {
            base.Update();

            if (Timing.sInstance.GetFrameStartTime() >= mDurationTime)
            {
                _isLife = false;
                SetDoesWantToDie(true);
            }

            if (_isLife == false)
            {
                // 장판 영역안에 있으면서 장판이 소멸 되었을때 스펠 있으면 걸어줌.
                //if (mSkillData.spellId != 0)
                //{
                //    foreach (var actorInAreaTime in mActorInAreaList)
                //    {
                //        AddSpell(actorInAreaTime.Value._actor);
                //    }
                //}

                if (mFinishSpell != null)
                {
                    foreach (var actorInAreaTime in mActorInAreaList)
                    {
                        AddSpell(actorInAreaTime.Value._actor, mFinishSpell);
                    }
                }


                mActorInAreaList.Clear();
                mRemoveDic.Clear();
                return;
            }

            if( mActorInAreaList.Count > 0 )
            {
                foreach (KeyValuePair<int, ActorInAreaTime> pair in mActorInAreaList)
                {
                    if (Timing.sInstance.GetFrameStartTime() > pair.Value._tickTime)
                    {
#if UNITY_EDITOR || DEBUG
                        Log.Information($"SAreaOfEffect Update ID {pair.Value._actor.GetNetworkId()} Damage {mDamage} Time {pair.Value._tickTime}");
#endif
                        // change time

                        if (mMiddleSpell != null)
                        {
                            ((SActor)pair.Value._actor).IncreHealth((int)mMiddleSpell.AddStatus, mMiddleSpell);
                        }
                        else
                        {
                            ((SActor)pair.Value._actor).TakeDamage(mPlayerId, mDamage);
                        }
                        //((SActor)pair.Value._actor).AddableSpell(mPlayerId, mSkillData.spellId);

                        pair.Value._damagedTime = Timing.sInstance.GetFrameStartTime();
                        pair.Value._tickTime = Timing.sInstance.GetFrameStartTime() + m_kExplosionData.tickTime;
                    }
                }

                foreach (int key in mRemoveDic)
                {
                    mActorInAreaList.Remove(key);
                }
            }
        }

        void AddSpell(Actor inActor, JSpellData spellData)
        {
#if UNITY_EDITOR || DEBUG
            Log.Information($"SAreaOfEffect AddSpell {inActor.GetPlayerId()} spellId {mSkillData.spellId}");
#endif

            SActor sActor = inActor as SActor;
            if (sActor != null)
            {
                sActor.AddableSpell(sActor, sActor.GetPlayerId(), spellData, 0);
            }
        }
    }
}