using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SPropCooldown : PropCooldown
    {
        protected SPropCooldown()
        {
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SPropCooldown(), worldId); }

        public override void HandleDying()
        {
            NetworkManagerServer.sInstance.UnregisterGameObject(this);

#if UNITY_EDITOR || DEBUG
            Log.Information($"remove propCooldown {NetworkId}, {mapObjectId}");
#endif
        }

        public override void Update()
        {
            base.Update();

            if ((RemainTime > 0)==false)
            {
                SetDoesWantToDie(true);
            }
        }
    }


}

