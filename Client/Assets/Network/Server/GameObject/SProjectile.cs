using core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SProjectile : Projectile
    {
        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SProjectile(), worldId); }

        public override void HandleDying()
        {

            NetworkManagerServer.sInstance.UnregisterGameObject(this);
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {
            if (inActor.GetPlayerId() != GetPlayerId())
            {
                //kill yourself!
                SetDoesWantToDie(true);

                ((SActor)(inActor)).TakeDamage(GetPlayerId(), inActor.GetDamage(0, GameObjectClassId.Actor));

            }

            return false;
        }

        public override void Update()
        {
            base.Update();

            if (Timing.sInstance.GetFrameStartTime() > mTimeToDie)
            {

                SetDoesWantToDie(true);
            }
        }

        protected SProjectile()
        {
            mTimeToDie = Timing.sInstance.GetFrameStartTime() + 3.0f;

        }

        private float mTimeToDie;
    }
}
