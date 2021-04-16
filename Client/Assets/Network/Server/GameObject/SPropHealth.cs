using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SPropHealth : PropHealth
    {
        protected SPropHealth()
        {
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SPropHealth(), worldId); }

        public override void HandleDying()
        {
            NetworkManagerServer.sInstance.UnregisterGameObject(this);

#if UNITY_EDITOR || DEBUG
            Log.Information($"remove PropHealth {NetworkId}, {mapObjectId}");
#endif
        }

        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            var lastHealth = mHealth;
            if (mHealth < damage)
            {
                mHealth = 0;
            }
            else
            {
                mHealth -= (ushort)damage;
            }

            if (lastHealth != mHealth)
            {
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Health);
            }

            return mHealth;
        }
    }


}

