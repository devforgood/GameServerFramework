using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif 

namespace core
{
    public class StaticGameObject : NetGameObject
    {
        public int Uid { get; set; }
        public bool IsCollision { get; set; }

        public JMapObjectData MapData { get; set; }

        public override int GetMapId() { return MapData.uID; }


        public override bool HandleCollisionWithActor(Actor inActor)
        {

            //점프대, 텔레포트 유니티 물리 충돌에서 체크 하게 변경.
            //if ((MapData.mapTypes & (1 << (int)MapPropertyType.Jump)) != 0
            //    || (MapData.mapTypes & (1 << (int)MapPropertyType.WarpZone)) != 0
            //    )
            //{
            //    inActor.TryTeleport(MapData.jumpLandingPos, MapData.jumpPower, MapData.jumpDuration, MapData.uID);
            //}

            if ((MapData.mapTypes & (1 << (int)MapPropertyType.IceFloor)) != 0 && inActor.IsIce == false)
            {
                inActor.IsIce = true;
            }

            if ((MapData.mapTypes & (1 << (int)MapPropertyType.SlowFloor)) != 0 && inActor.IsSlow == false)
            {
                inActor.tileSpeed = MapData.tileSpeed;
                Vector3 slowVelocity = inActor.GetVelocity() * MapData.tileSpeed;

                //LogHelper.LogInfo($"slow floor last velocity{inActor.GetVelocity()}, location{inActor.GetLocation()}, slow {slowVelocity}");
                inActor.SetVelocity(inActor.GetVelocity() + slowVelocity);
                inActor.SetLocation(inActor.GetLocation() + slowVelocity);
                //LogHelper.LogInfo($"slow floor current velocity{inActor.GetVelocity()}, location{inActor.GetLocation()}");

                inActor.IsSlow = true;
            }

            return IsCollision;
        }
    }
}
