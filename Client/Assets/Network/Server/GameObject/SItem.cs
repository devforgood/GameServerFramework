using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace Server
{
    public class SItem : Item
    {
        protected SItem()
        {
        }

        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SItem(), worldId); }

        public override void HandleDying()
        {
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
#if UNITY_EDITOR || DEBUG
            Log.Information($"remove Item {NetworkId}");
#endif
        }

        /// <summary>
        /// 캐릭터 충돌시 아이템 획득
        /// </summary>
        /// <param name="inActor"></param>
        /// <returns></returns>
        public override bool HandleCollisionWithActor(Actor inActor)
        {
            // 아이템 획득
            if (((SActor)inActor).GetItem(ItemId))
            {
                // 아이템 획득 이후 월드에서 아이템 제거
                SetDoesWantToDie(true);
            }
            return false;
        }

        /// <summary>
        /// 폭발로 인한 아아템 제거
        /// </summary>
        /// <param name="player_id"></param>
        /// <param name="parentNetworkId"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            //kill yourself!
            SetDoesWantToDie(true);

            return 0;
        }

        public void InitFrom(ushort itemId, Vector3 Pos)
        {
#if UNITY_EDITOR || DEBUG
            Log.Information($"create item {GetNetworkId()}, itemId {itemId} Location {Pos}");
#endif

            SetLocation(Pos);
            ItemId = itemId;
        }
    }


}

