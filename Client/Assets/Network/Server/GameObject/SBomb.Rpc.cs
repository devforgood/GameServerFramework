using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using core;
using Lidgren.Network;
using Serilog;

namespace Server
{
    public partial class SBomb
    {
        HashSet<byte> ActionTokens = new HashSet<byte>();


        [ServerRPC(RequireOwnership = false)]
        public override void BombResult(List<int> objectList)
        {
#if UNITY_EDITOR || DEBUG
            if (objectList.Count > 0)
                Log.Information($"Bomb ActionResult ({string.Join(",", objectList)})");
#endif

            NetGameObject obj;
            for (int i = 0; i < objectList.Count; ++i)
            {
                obj = NetworkManagerServer.sInstance.GetGameObject(objectList[i], WorldId);
                if (obj == null)
                    continue;
                if(obj.GetClassId() == (byte)GameObjectClassId.Actor
                    || obj.GetClassId() == (byte)GameObjectClassId.Castle
                    )
                    obj.OnExplode((int)GetPlayerId(), GetNetworkId(), mDamage);
                else
                    obj.OnExplode((int)GetPlayerId(), GetNetworkId(), mSiegeAtk);
            }

            SetDoesWantToDie(true);
        }
    }
}
