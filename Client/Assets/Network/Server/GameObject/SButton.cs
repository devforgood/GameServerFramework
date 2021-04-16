using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SButton : Button
    {
        protected SButton()
        {
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SButton(), worldId); }

        public override void HandleDying()
        {
            NetworkManagerServer.sInstance.UnregisterGameObject(this);

#if UNITY_EDITOR || DEBUG
            Log.Information($"remove SButton {NetworkId}, {mapObjectId}");
#endif
        }

        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            Log.Information($"OnExplode SButton {NetworkId}, {mapObjectId}, {damage}");
            if (mapData.jMapSwitchData[0].coolTime > 0)
            {
                if(World.Instance(WorldId).GetNetGameObject(GameObjectClassId.PropCooldown, GetMapId())!=null)
                {
                    Log.Information($"button cooldown... {GetMapId()}");
                    return 0;
                }
            }

            bool isDie = false;
            if (mapData.objectHP != 0)
            {
                PropHealth propHealth = (PropHealth)World.Instance(WorldId).GetNetGameObject(GameObjectClassId.PropHealth, mapObjectId);
                if (propHealth != null)
                {
                    if(propHealth.mHealth == 0)
                    {
                        // 이미 hp가 0이면 이후 처리 하지 않는다.
                    }
                    else if (propHealth.OnExplode(player_id, parentNetworkId, damage) == 0)
                    {
                        isDie = true;
                        // 스위치 오브젝트를 제거하면 이후 상태를 알수 없어서 스페셜 오브젝트 처리가 불가하다.
                        //SetDoesWantToDie(true);
                    }
                }
            }
            else
            {
                isDie = true;
            }

            if (isDie)
            {
                //SetDoesWantToDie(true);

                // 버튼(스위치) 상태 변경
                mOnOff = !mOnOff;
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.OnOff);

                if (mapData.objectHP != 0)
                {
                    // hp가 있는 경우 쿨타임 적용하지 않는다.
                }
                else if (mapData.jMapSwitchData[0].coolTime > 0)
                {
                    //set cooldown
                    PropCooldown cooldown = (PropCooldown)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.PropCooldown, true, WorldId);
                    cooldown.InitFrom(GetMapId());
                }

            }


            return 0;
        }
    }


}

