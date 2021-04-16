using core;
using Lidgren.Network;
using Serilog;
using Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public partial class SProp
    {
        HashSet<byte> ActionTokens = new HashSet<byte>();


        [ServerRPC(RequireOwnership = false)]
        public override void PropExplodeResult(List<int> objectList)
        {
#if UNITY_EDITOR || DEBUG
            if (objectList.Count > 0)
                Log.Information($"Prop PropExplodeResult ({string.Join(",", objectList)})");
#endif

            // 프랍이 제거될때 폭발하는 데미지 얻어서 처리
            JMapObjectData mapData;
            int damage = 0;
            int siegeAtk = 0;
            if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == true)
            {
                if (mapData.jDestroyBombData.Length > 0)
                {
                    if (mapData.jDestroyBombData[0].skillID != 0)
                    {
                        JSkillData skillData = ACDC.SkillData[mapData.jDestroyBombData[0].skillID];
                        if(skillData != null)
                        {
                            damage = (int)skillData.Damage;
                            siegeAtk = (int)skillData.SiegeAtk;
                        }
                    }
                }
            }

            NetGameObject obj;
            for (int i = 0; i < objectList.Count; ++i)
            {
                obj = NetworkManagerServer.sInstance.GetGameObject(objectList[i], WorldId);
                if (obj == null)
                    continue;

                if(obj.GetClassId() == (byte)GameObjectClassId.Actor
                    || obj.GetClassId() == (byte)GameObjectClassId.Castle)
                    obj.OnExplode((int)ReservedPlayerId.Trap, GetNetworkId(), damage);
                else
                    obj.OnExplode((int)ReservedPlayerId.Trap, GetNetworkId(), siegeAtk);
            }

            SetDoesWantToDie(true);
        }
    }
}
