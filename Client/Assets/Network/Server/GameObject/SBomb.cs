using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace Server
{
    partial class SBomb : core.Bomb
    {
        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SBomb(), worldId); }


        public static readonly float BombPeriod = 7.0f;
        public static readonly int MaxExplodeCount = 5;
        public static readonly float NextExplodePeriod = 0.05f;

        private float mTimeToBomb;
        private int mExplodeCount = 0;
        private int default_damage = 10;



        public override void HandleDying()
        {
            base.HandleDying();
            NetworkManagerServer.sInstance.UnregisterGameObject(this);
        }

        /// <summary>
        /// 다른 폭탄에 의해 터지는 경우 호출
        /// </summary>
        public override int OnExplode(int player_id, int parentNetworkId, int damage)
        {
            // 이미 터진 폭탄이다.
            if (mIsExplode == true)
                return 0;

            // 폭탄 터지는 시간을 현재 시간으로 맞추고
            mTimeToBomb = Timing.sInstance.GetFrameStartTime();
            // 터트림
            Setoff();

            return 0;
        }

        void Explode(Vector3 pos /*Tile tile*/)
        {
            float current_floor = (float)Math.Round(pos.y);

            foreach ( var game_object in World.Instance(WorldId).GetGameObjects())
            {
                // 같은 층이 아닌 경우는 충돌 검사에서 제외
                if (NetGameObject.IsSameFloor(current_floor, game_object)==false)
                    continue;

                if (NetworkId != game_object.NetworkId && MathHelpers.circleRect(pos.x, pos.z, GetCollisionRadius(), game_object.rx, game_object.ry, game_object.rw, game_object.rh)
                    /*game_object.GetLocation().Round().Equals(pos)*/)
                {
                    game_object.OnExplode(mPlayerId, NetworkId, default_damage);
                }
            }

            //if (tile == null)
            //    return;

            //foreach(var game_object in tile.gameObjects)
            //{
            //    if(NetworkId != game_object.NetworkId)
            //        game_object.OnExplode(mPlayerId, NetworkId, default_damage);
            //}
        }

        void Explode()
        {
            Explode(GetLocation() + (Vector3.forward * mExplodeCount));
            Explode(GetLocation() + (Vector3.back * mExplodeCount));
            Explode(GetLocation() + (Vector3.right * mExplodeCount));
            Explode(GetLocation() + (Vector3.left * mExplodeCount));

            //Explode(World.Instance(WorldId).mWorldMap.GetTile(GetLocation() + (Vector3.forward * mExplodeCount)));
            //Explode(World.Instance(WorldId).mWorldMap.GetTile(GetLocation() + (Vector3.back * mExplodeCount)));
            //Explode(World.Instance(WorldId).mWorldMap.GetTile(GetLocation() + (Vector3.right * mExplodeCount)));
            //Explode(World.Instance(WorldId).mWorldMap.GetTile(GetLocation() + (Vector3.left * mExplodeCount)));
        }

        public override void Update()
        {
            base.Update();

            //if (Timing.sInstance.GetFrameStartTime() > mTimeToBomb && mExplodeCount <= MaxExplodeCount)
            //{
            //    Setoff();
            //}

            //if (mExplodeCount == MaxExplodeCount + 1)
            //{
            //    SetDoesWantToDie(true);
            //}


            // 클라이언트로 인해 폭발 하지만 불발할수 있으므로 서버가 이후 제거
            if (Timing.sInstance.GetFrameStartTime() > mExplodeTime + 2f)
            {
                SetDoesWantToDie(true);
            }
        }

        private void Setoff()
        {
            // 해당 시간 뒤에 추가 폭발이 일어난다.
            mTimeToBomb += NextExplodePeriod;

            // 첫 폭발만 클라와 동기화를 맞춘다.
            if (mExplodeCount == 0)
            {
                mIsExplode = true;
                //NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Explode);

                Explode(GetLocation());
                //Explode(World.Instance(WorldId).mWorldMap.GetTile(GetLocation()));
            }
            else
            {
                Explode();
            }

            ++mExplodeCount;
        }

        protected SBomb()
        {
            mTimeToBomb = Timing.sInstance.GetFrameStartTime() + BombPeriod;

        }



    }
}
