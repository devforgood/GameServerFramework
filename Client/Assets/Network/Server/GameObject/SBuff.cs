using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SBuff : Buff
    {
        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SBuff(), worldId); }

        public override void HandleDying()
        {
            //Log.Information($"HandleDying buff {GetNetworkId()}");

            NetworkManagerServer.sInstance.UnregisterGameObject(this);
        }

        public override bool HandleCollisionWithActor(Actor inActor)
        {

            return false;
        }

        public override void Update()
        {
            // 만료시간 보다 조금 여유를 두어 즉시 삭제되는 버프를 처리하도록 함
            if (RemainTime+1f <= Timing.sInstance.GetFrameStartTime())
            {
                SetDoesWantToDie(true);
            }
        }

        protected override void Dirty(uint state)
        {
            //Log.Information($"dirty buff {GetNetworkId()}");

            NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, state);
        }
    }
}
