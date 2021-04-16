using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public partial class SProp : Prop
    {
        protected SProp()
        {
            //Log.Information($"create prop {0}", NetworkId));
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SProp(), worldId); }

        public override void HandleDying()
        {
#if _USE_BEPU_PHYSICS
            if (collision != null)
                core.World.Instance(WorldId).space.Remove(collision);
#endif

            JMapObjectData mapData;
            if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == true)
            {
                if ((mapData.mapTypes & (1 << (int)MapPropertyType.MapItem)) != 0
                    && mapData.jMapItemData.Length > 0
                    && mapData.jMapItemData[0].createItems.Length > 0
                    )
                {
                    var create_rand = MathHelpers.GetRandomInt(100);
                    int select_rand = 0;
                    // 아이템이 생성될 확률
                    if (mapData.jMapItemData[0].createItemPer > create_rand)
                    {
                        select_rand = MathHelpers.GetRandomInt(mapData.jMapItemData[0].createItems.Length);
                        var item_id = mapData.jMapItemData[0].createItems[select_rand];


                        var item = (SItem)GameObjectRegistry.sInstance.CreateGameObject((uint)GameObjectClassId.Item,true, WorldId);
                        item.InitFrom((ushort)item_id, GetLocation());
                    }

                    LogHelper.LogInfo($"Item random create{create_rand}, select{select_rand}");
                }
            }


            base.HandleDying();
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
            //Log.Information($"remove prop {NetworkId}");
        }

        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            bool isDie = false;
            PropHealth propHealth = (PropHealth)World.Instance(WorldId).GetNetGameObject(GameObjectClassId.PropHealth, mapObjectId);
            if (propHealth != null)
            {
                if (propHealth.OnExplode(player_id, parentNetworkId, damage) == 0)
                {
                    isDie = true;
                }
            }
            else
            {
                isDie = true;
            }

            if (isDie)
            {
                // 프랍이 제거될때 폭발하는 유형인지 판단
                JMapObjectData mapData;
                bool explodeProp = false;
                if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == true)
                {
                    if(mapData.jDestroyBombData.Length > 0)
                    {
                        if(mapData.jDestroyBombData[0].skillID != 0)
                        {
                            explodeProp = true;
                        }
                    }
                }

                if (explodeProp == false)
                {
                    //kill yourself!
                    SetDoesWantToDie(true);
                }
                else
                {
                    IsDie = true;
                    NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Die);
                }

                // 맵 오브젝트를 파괴한 유저에게 플레이포인트를 지급
                var player = (SActor)World.Instance(WorldId).GameMode.GetActor(player_id);
                if(player != null)
                {
                    World.Instance(WorldId).GameMode.OnTrigger(player.GetPlayerId(), PlayPointID.MapObjectDestroy);
                }
            }

            return 0;
        }
    }


}

