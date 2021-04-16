using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class STrap : Trap
    {
        protected STrap()
        {
            //Log.Information($"create trap {0}", NetworkId));
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new STrap(), worldId); }

        public override void HandleDying()
        {
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
            Log.Information($"remove trap {NetworkId}");
        }


        public override void Update()
        {
            base.Update();

            if (IsStarted && Timing.sInstance.GetFrameStartTime() > mCreateTime)
            {
                var skill = ACDC.SkillData[mapData.jAutoBombData[0].skillID];

                //install bomb
                var bomb = (core.Bomb)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.Bomb, true, WorldId);
                bomb.InitFrom(this, skill);

                //Log.Information($"spawn bomb by trap {NetworkId}");

                mCreateTime = Timing.sInstance.GetFrameStartTime() + mapData.jAutoBombData[0].createTime;
            }
        }
    }


}

