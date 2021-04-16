using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class STreasureBox : TreasureBox
    {
        int CurrentItemIndex = 0;
        bool IsNotice = false;

        protected STreasureBox()
        {
        }

        public override void Set(JMapObjectData mapData)
        {
            base.Set(mapData);
            this.mapData = mapData;
            mapObjectId = mapData.uID;
            ResetCoolTime(mapData.jAutoItemData[0].openStartTime);
#if UNITY_EDITOR || DEBUG
            Log.Information($"create TreasureBox {NetworkId}");
#endif

        }

        void ResetCoolTime(float coolTime)
        {
            mTimeToItemSpawn = Timing.sInstance.GetFrameStartTime() + coolTime;
            IsNotice = true;
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new STreasureBox(), worldId); }

        public override void HandleDying()
        {
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
#if UNITY_EDITOR || DEBUG
            Log.Information($"remove TreasureBox {NetworkId}");
#endif
        }

        /// <summary>
        /// 캐릭터 충돌시 아이템 획득
        /// </summary>
        /// <param name="inActor"></param>
        /// <returns></returns>
        public override bool HandleCollisionWithActor(Actor inActor)
        {
            if (ItemId != 0)
            {
                // 아이템 획득
                if (((SActor)inActor).GetItem(ItemId))
                {
                    ItemId = 0;
                    ResetCoolTime(mapData.jAutoItemData[0].createCoolTime);

                    NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.SpawnItem);
                }
            }


            return true;
        }


        public override void Update()
        {
            base.Update();

            if (IsStarted==true && ItemId == 0)
            {
                if(IsNotice == true  && mapData.jAutoItemData[0].isSpecial == true && Timing.sInstance.GetFrameStartTime() > (mTimeToItemSpawn-10f))
                {
                    IsNotice = false;

                    foreach(var player in World.Instance(WorldId).playerList)
                    {
                        ((SActor)player.Value).NoticeCreateItem();
                    }
                }

                if (Timing.sInstance.GetFrameStartTime() > mTimeToItemSpawn)
                {
                    if (mapData.jAutoItemData[0].isRandom)
                    {
                        CurrentItemIndex = MathHelpers.GetRandomInt(mapData.jAutoItemData[0].createItems.Length);
                    }
                    else
                    {
                        ++CurrentItemIndex;
                        if (mapData.jAutoItemData[0].createItems.Length <= CurrentItemIndex)
                            CurrentItemIndex = 0;
                    }

                    ItemId = (ushort)mapData.jAutoItemData[0].createItems[CurrentItemIndex];
#if UNITY_EDITOR || DEBUG
                    Log.Information($"spawn Item {ItemId}, TreasureBox {NetworkId}");
#endif

                    NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.SpawnItem);
                }
            }
        }
    }


}

